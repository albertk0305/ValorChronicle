using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.Shields;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Actions
{
    public sealed class NextBossHitDamageReductionTests
    {
        [Test]
        public void DirectHitWithoutOneTimeEffectUsesExistingDamagePath()
        {
            CreateBattle(out PartyBattleState party, out BossBattleState boss);

            BossDamageActionResult result = ExecuteHit(party, boss, 1);

            Assert.That(result.Context.PartyDamageReductionRateSum, Is.Zero);
            Assert.That(result.DamageResult.FinalDamageBeforeShield,
                Is.EqualTo(1000L));
            Assert.That(result.ApplicationResult.HpDamage,
                Is.EqualTo(1000L));
        }

        [TestCase(0.35d, 650L)]
        [TestCase(0.40d, 600L)]
        [TestCase(0.15d, 850L)]
        public void DirectHitAppliesAndConsumesSingleEffect(
            double reductionRate,
            long expectedDamage)
        {
            CreateBattle(out PartyBattleState party, out BossBattleState boss);
            party.Effects.ApplyEffect(OneTimeEffect(
                1,
                "one_time_guard",
                reductionRate));

            BossDamageActionResult result = ExecuteHit(party, boss, 1);

            Assert.That(result.DamageResult.FinalDamageBeforeShield,
                Is.EqualTo(expectedDamage));
            Assert.That(party.Effects.GetActiveEffects(), Is.Empty);
        }

        [Test]
        public void StrongestEffectIsConsumedAndWeakerEffectRemains()
        {
            CreateBattle(out PartyBattleState party, out BossBattleState boss);
            EffectInstance weaker = OneTimeEffect(1, "ancient_guard", 0.15d);
            EffectInstance stronger = OneTimeEffect(2, "white_deer", 0.40d);
            party.Effects.ApplyEffect(weaker);
            party.Effects.ApplyEffect(stronger);

            BossDamageActionResult result = ExecuteHit(party, boss, 1);

            Assert.That(result.Context.PartyDamageReductionRateSum,
                Is.EqualTo(0.40d));
            Assert.That(result.DamageResult.FinalDamageBeforeShield,
                Is.EqualTo(600L));
            Assert.That(party.Effects.GetActiveEffects(),
                Is.EqualTo(new[] { weaker }));
        }

        [Test]
        public void EqualRatesConsumeEarlierCreatedEffectFirst()
        {
            CreateBattle(out PartyBattleState party, out BossBattleState boss);
            EffectInstance earlier = OneTimeEffect(1, "earlier", 0.40d);
            EffectInstance later = OneTimeEffect(2, "later", 0.40d);
            party.Effects.ApplyEffect(later);
            party.Effects.ApplyEffect(earlier);

            ExecuteHit(party, boss, 1);

            Assert.That(party.Effects.GetActiveEffects(),
                Is.EqualTo(new[] { later }));
        }

        [Test]
        public void ThreeDirectHitsConsumeFortyThenFifteenThenNone()
        {
            CreateBattle(
                out PartyBattleState party,
                out BossBattleState boss,
                partyHp: 10000);
            party.Effects.ApplyEffect(OneTimeEffect(
                1,
                "ancient_guard",
                0.15d));
            party.Effects.ApplyEffect(OneTimeEffect(
                2,
                "white_deer",
                0.40d));
            var queue = new CombatActionQueue(new CombatAction[]
            {
                Hit(1, boss, party),
                Hit(2, boss, party),
                Hit(3, boss, party)
            });

            CombatActionExecutionResult execution =
                Executor(party, boss).Execute(queue);

            Assert.That(execution.CompletedActionCount, Is.EqualTo(3));
            Assert.That(Damages(execution),
                Is.EqualTo(new long[] { 600, 850, 1000 }));
            Assert.That(party.Effects.GetActiveEffects(), Is.Empty);
        }

        [Test]
        public void FullShieldAbsorptionStillConsumesSelectedEffect()
        {
            CreateBattle(out PartyBattleState party, out BossBattleState boss);
            party.Shields.Add(Shield(1, 1000));
            party.Effects.ApplyEffect(OneTimeEffect(
                2,
                "white_deer",
                0.40d));

            BossDamageActionResult result = ExecuteHit(party, boss, 1);

            Assert.That(result.ApplicationResult.ShieldAbsorbedDamage,
                Is.EqualTo(600L));
            Assert.That(result.ApplicationResult.HpDamage, Is.Zero);
            Assert.That(party.Effects.GetActiveEffects(), Is.Empty);
        }

        [Test]
        public void PartialShieldAbsorptionAppliesRemainingDamageToHp()
        {
            CreateBattle(out PartyBattleState party, out BossBattleState boss);
            party.Shields.Add(Shield(1, 200));
            party.Effects.ApplyEffect(OneTimeEffect(
                2,
                "white_deer",
                0.40d));

            BossDamageActionResult result = ExecuteHit(party, boss, 1);

            Assert.That(result.ApplicationResult.ShieldAbsorbedDamage,
                Is.EqualTo(200L));
            Assert.That(result.ApplicationResult.HpDamage,
                Is.EqualTo(400L));
            Assert.That(party.Effects.GetActiveEffects(), Is.Empty);
        }

        [Test]
        public void NonDamageActionDoesNotConsumeEffect()
        {
            CreateBattle(out PartyBattleState party, out BossBattleState boss);
            EffectInstance effect = OneTimeEffect(
                1,
                "white_deer",
                0.40d);
            party.Effects.ApplyEffect(effect);
            boss.Resources.Register("marker", 1);

            Executor(party, boss).Execute(new CombatActionQueue(
                new CombatAction[]
                {
                    new AddResourceAction(
                        1,
                        ActionOrigin.System,
                        boss,
                        "marker",
                        1)
                }));

            Assert.That(party.Effects.GetActiveEffects(),
                Is.EqualTo(new[] { effect }));
        }

        [TestCase(BossDamageHitType.DamageOverTime)]
        [TestCase(BossDamageHitType.Reflected)]
        [TestCase(BossDamageHitType.SelfInflicted)]
        public void NonDirectDamageDoesNotApplyOrConsumeEffect(
            BossDamageHitType hitType)
        {
            CreateBattle(out PartyBattleState party, out BossBattleState boss);
            EffectInstance effect = OneTimeEffect(
                1,
                "white_deer",
                0.40d);
            party.Effects.ApplyEffect(effect);

            BossDamageActionResult result = ExecuteHit(
                party,
                boss,
                1,
                hitType);

            Assert.That(result.DamageResult.FinalDamageBeforeShield,
                Is.EqualTo(1000L));
            Assert.That(party.Effects.GetActiveEffects(),
                Is.EqualTo(new[] { effect }));
        }

        [Test]
        public void UniqueReacquisitionKeepsExistingEffectUnchanged()
        {
            CreateBattle(out PartyBattleState party, out _);
            EffectInstance existing = OneTimeEffect(
                1,
                "white_deer",
                0.35d);
            party.Effects.ApplyEffect(existing);

            EffectInstance active = party.Effects.ApplyEffect(
                OneTimeEffect(2, "white_deer", 0.40d));

            Assert.That(active, Is.SameAs(existing));
            Assert.That(active.Magnitude, Is.EqualTo(0.35d));
            Assert.That(party.Effects.GetActiveEffects(),
                Is.EqualTo(new[] { existing }));
        }

        [Test]
        public void OneTimeEffectRequiresUniqueIndefiniteConfiguration()
        {
            Assert.Throws<System.ArgumentException>(() =>
                new EffectInstance(
                    1,
                    "invalid_policy",
                    "test_source",
                    EffectCategory.Buff,
                    EffectModifierType.NextBossHitDamageReduction,
                    0.40d,
                    null,
                    1,
                    EffectStackPolicy.RefreshDuration));
            Assert.Throws<System.ArgumentException>(() =>
                new EffectInstance(
                    2,
                    "invalid_duration",
                    "test_source",
                    EffectCategory.Buff,
                    EffectModifierType.NextBossHitDamageReduction,
                    0.40d,
                    3,
                    2,
                    EffectStackPolicy.Unique));
        }

        [Test]
        public void IndefiniteEffectSurvivesMultipleTurnEndsWithoutHit()
        {
            CreateBattle(out PartyBattleState party, out BossBattleState boss);
            EffectInstance effect = OneTimeEffect(
                1,
                "white_deer",
                0.40d);
            party.Effects.ApplyEffect(effect);
            var turnEnd = new BattleTurnEndProcessor(party, boss);

            turnEnd.ProcessTurnEnd();
            turnEnd.ProcessTurnEnd();
            turnEnd.ProcessTurnEnd();

            Assert.That(effect.RemainingTurns, Is.Null);
            Assert.That(party.Effects.GetActiveEffects(),
                Is.EqualTo(new[] { effect }));
        }

        [Test]
        public void OneTimeAndGeneralReductionAddBeforeDamageCalculation()
        {
            CreateBattle(out PartyBattleState party, out BossBattleState boss);
            EffectInstance general = GeneralReduction(1, 0.25d);
            party.Effects.ApplyEffect(general);
            party.Effects.ApplyEffect(OneTimeEffect(
                2,
                "white_deer",
                0.35d));

            BossDamageActionResult result = ExecuteHit(party, boss, 1);

            Assert.That(result.Context.PartyDamageReductionRateSum,
                Is.EqualTo(0.60d).Within(0.000000001d));
            Assert.That(result.DamageResult.FinalDamageBeforeShield,
                Is.EqualTo(400L));
            Assert.That(party.Effects.GetActiveEffects(),
                Is.EqualTo(new[] { general }));
        }

        [Test]
        public void CombinedReductionUsesExistingSeventyPercentCap()
        {
            CreateBattle(out PartyBattleState party, out BossBattleState boss);
            party.Effects.ApplyEffect(GeneralReduction(1, 0.50d));
            party.Effects.ApplyEffect(OneTimeEffect(
                2,
                "white_deer",
                0.40d));

            BossDamageActionResult result = ExecuteHit(party, boss, 1);

            Assert.That(result.Context.PartyDamageReductionRateSum,
                Is.EqualTo(0.90d));
            Assert.That(result.DamageResult.EffectivePartyDamageReductionRate,
                Is.EqualTo(0.70d));
            Assert.That(result.DamageResult.FinalDamageBeforeShield,
                Is.EqualTo(300L));
        }

        [Test]
        public void TerminalBattleClearsUnexecutedHitWithoutConsumption()
        {
            CreateBattle(out PartyBattleState party, out BossBattleState boss);
            PartyDamageApplier.Apply(
                party,
                BossDamageCalculator.Calculate(new BossDamageContext(
                    1000d,
                    0d,
                    0d,
                    1d,
                    0d,
                    0d,
                    0d,
                    0d)));
            EffectInstance effect = OneTimeEffect(
                1,
                "white_deer",
                0.40d);
            party.Effects.ApplyEffect(effect);

            CombatActionExecutionResult execution = Executor(party, boss)
                .Execute(new CombatActionQueue(new CombatAction[]
                {
                    Hit(1, boss, party)
                }));

            Assert.That(execution.CompletedActionCount, Is.Zero);
            Assert.That(execution.ClearedRemainingActions, Is.True);
            Assert.That(party.Effects.GetActiveEffects(),
                Is.EqualTo(new[] { effect }));
        }

        [Test]
        public void ZeroDamageDirectHitStillConsumesSelectedEffect()
        {
            CreateBattle(out PartyBattleState party, out BossBattleState boss);
            party.Effects.ApplyEffect(OneTimeEffect(
                1,
                "white_deer",
                0.40d));

            BossDamageActionResult result = ExecuteHit(
                party,
                boss,
                1,
                BossDamageHitType.Direct,
                attackCoefficient: 0.001d);

            Assert.That(result.DamageResult.FinalDamageBeforeShield, Is.Zero);
            Assert.That(party.Effects.GetActiveEffects(), Is.Empty);
        }

        private static BossDamageActionResult ExecuteHit(
            PartyBattleState party,
            BossBattleState boss,
            long actionId,
            BossDamageHitType hitType = BossDamageHitType.Direct,
            double attackCoefficient = 1d)
        {
            return (BossDamageActionResult)Executor(party, boss).Execute(
                new CombatActionQueue(new CombatAction[]
                {
                    Hit(
                        actionId,
                        boss,
                        party,
                        hitType,
                        attackCoefficient)
                })).ActionResults[0];
        }

        private static BossDamageAction Hit(
            long actionId,
            BossBattleState boss,
            PartyBattleState party,
            BossDamageHitType hitType = BossDamageHitType.Direct,
            double attackCoefficient = 1d)
        {
            return new BossDamageAction(
                actionId,
                new BossDamageContextBuildRequest(
                    boss,
                    party,
                    attackCoefficient,
                    AttackTag.None,
                    hitType));
        }

        private static CombatActionExecutor Executor(
            PartyBattleState party,
            BossBattleState boss)
        {
            return new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)));
        }

        private static long[] Damages(CombatActionExecutionResult execution)
        {
            var damages = new List<long>();
            for (int index = 0;
                index < execution.ActionResults.Count;
                index++)
            {
                damages.Add(((BossDamageActionResult)execution
                    .ActionResults[index]).DamageResult
                    .FinalDamageBeforeShield);
            }

            return damages.ToArray();
        }

        private static EffectInstance OneTimeEffect(
            long runtimeId,
            string effectId,
            double magnitude)
        {
            return new EffectInstance(
                runtimeId,
                effectId,
                "test_source",
                EffectCategory.Buff,
                EffectModifierType.NextBossHitDamageReduction,
                magnitude,
                null,
                runtimeId,
                EffectStackPolicy.Unique);
        }

        private static EffectInstance GeneralReduction(
            long runtimeId,
            double magnitude)
        {
            return new EffectInstance(
                runtimeId,
                $"general_reduction_{runtimeId}",
                "test_source",
                EffectCategory.Buff,
                EffectModifierType.PartyDamageReduction,
                magnitude,
                null,
                runtimeId,
                EffectStackPolicy.Unique);
        }

        private static ShieldInstance Shield(long runtimeId, long amount)
        {
            return new ShieldInstance(
                runtimeId,
                "test_source",
                amount,
                1,
                3,
                runtimeId);
        }

        private static void CreateBattle(
            out PartyBattleState party,
            out BossBattleState boss,
            long partyHp = 1000)
        {
            party = new PartyBattleState(new[]
            {
                new CharacterBattleState(
                    "hero",
                    0,
                    ElementType.Fire,
                    partyHp,
                    100d)
            });
            boss = new BossBattleState(
                "boss",
                ElementType.Grass,
                10000,
                1000d);
        }
    }
}
