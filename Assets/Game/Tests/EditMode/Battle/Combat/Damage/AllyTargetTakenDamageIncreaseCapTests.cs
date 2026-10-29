using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Modifiers;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Damage
{
    public sealed class AllyTargetTakenDamageIncreaseCapTests
    {
        [TestCase(0.05d, 1050L)]
        [TestCase(0.06d, 1060L)]
        public void SingleAllyDebuffUsesOriginalMagnitude(
            double magnitude,
            long expectedDamage)
        {
            CreateStates(out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            boss.Effects.ApplyEffect(GroupedEffect(
                1,
                "ally_vulnerability",
                "ally_one",
                magnitude));

            AssertDamage(
                attacker,
                party,
                boss,
                magnitude,
                expectedDamage);
        }

        [TestCase(0.15d, 0.05d, 0.20d, 1200L)]
        [TestCase(0.15d, 0.10d, 0.25d, 1250L)]
        [TestCase(0.15d, 0.15d, 0.25d, 1250L)]
        public void DifferentAlliesShareOneCap(
            double firstMagnitude,
            double secondMagnitude,
            double expectedIncrease,
            long expectedDamage)
        {
            CreateStates(out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            EffectInstance first = GroupedEffect(
                1,
                "first_vulnerability",
                "ally_one",
                firstMagnitude);
            EffectInstance second = GroupedEffect(
                2,
                "second_vulnerability",
                "ally_two",
                secondMagnitude);
            boss.Effects.ApplyEffect(first);
            boss.Effects.ApplyEffect(second);

            AssertDamage(
                attacker,
                party,
                boss,
                expectedIncrease,
                expectedDamage);
            Assert.That(first.Magnitude, Is.EqualTo(firstMagnitude));
            Assert.That(second.Magnitude, Is.EqualTo(secondMagnitude));
        }

        [Test]
        public void RemovingEffectRecalculatesFromRemainingGroupMembers()
        {
            CreateStates(out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            boss.Effects.ApplyEffect(GroupedEffect(
                1,
                "first_vulnerability",
                "ally_one",
                0.15d));
            boss.Effects.ApplyEffect(GroupedEffect(
                2,
                "second_vulnerability",
                "ally_two",
                0.15d));
            Assert.That(Collect(attacker, party, boss), Is.EqualTo(0.25d));

            Assert.That(boss.Effects.RemoveEffect(1), Is.True);

            Assert.That(Collect(attacker, party, boss), Is.EqualTo(0.15d));
        }

        [TestCase(0d, 0.30d, 1300L)]
        [TestCase(0.05d, 0.35d, 1350L)]
        [TestCase(0.06d, 0.36d, 1360L)]
        [TestCase(0.15d, 0.45d, 1450L)]
        [TestCase(0.30d, 0.55d, 1550L)]
        public void KragmorCoreExposureAddsOutsideAllyCap(
            double allyMagnitude,
            double expectedIncrease,
            long expectedDamage)
        {
            CreateStates(out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            KragmorCombatConfig config = KragmorTestConfig.Create();
            try
            {
                EffectInstance coreExposure =
                    KragmorDefenseEffectFactory.Create(
                        KragmorDefenseState.CoreExposure,
                        runtimeId: 1,
                        config);
                boss.Effects.ApplyEffect(coreExposure);
                if (allyMagnitude > 0d)
                {
                    AddAllyMagnitude(boss, allyMagnitude);
                }

                Assert.That(coreExposure.ModifierGroupId, Is.Null);
                AssertDamage(
                    attacker,
                    party,
                    boss,
                    expectedIncrease,
                    expectedDamage);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void CapDoesNotChangeOtherModifierCategories()
        {
            CreateStates(out CharacterBattleState attacker,
                out PartyBattleState party,
                out BossBattleState boss);
            boss.Effects.ApplyEffect(GroupedEffect(
                1,
                "first_vulnerability",
                "ally_one",
                0.15d));
            boss.Effects.ApplyEffect(GroupedEffect(
                2,
                "second_vulnerability",
                "ally_two",
                0.15d));
            boss.Effects.ApplyEffect(UngroupedEffect(
                3,
                "target_reduction",
                EffectModifierType.TargetTakenDamageReduction,
                0.05d));
            attacker.Effects.ApplyEffect(UngroupedEffect(
                4,
                "dealt_increase",
                EffectModifierType.DealtDamageIncrease,
                0.10d));

            DamageContext context = Build(attacker, party, boss);
            DamageResult result = DamageCalculator.Calculate(context);

            Assert.That(context.TargetTakenDamageIncreaseRateSum,
                Is.EqualTo(0.25d));
            Assert.That(context.TargetTakenDamageReductionRateSum,
                Is.EqualTo(0.05d));
            Assert.That(context.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.10d));
            Assert.That(result.FinalDamage, Is.EqualTo(1320L));
        }

        private static void AddAllyMagnitude(
            BossBattleState boss,
            double magnitude)
        {
            double first = magnitude > 0.15d ? 0.15d : magnitude;
            boss.Effects.ApplyEffect(GroupedEffect(
                2,
                "first_vulnerability",
                "ally_one",
                first));
            double remaining = magnitude - first;
            if (remaining > 0d)
            {
                boss.Effects.ApplyEffect(GroupedEffect(
                    3,
                    "second_vulnerability",
                    "ally_two",
                    remaining));
            }
        }

        private static void AssertDamage(
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss,
            double expectedIncrease,
            long expectedDamage)
        {
            AllyDamageModifierSnapshot collected =
                CombatModifierCollector.CollectAllyDamage(
                    attacker,
                    party,
                    boss,
                    ElementType.Fire);
            DamageContext context = Build(attacker, party, boss);
            DamageResult result = DamageCalculator.Calculate(context);

            Assert.That(collected.TargetTakenDamageIncreaseRateSum,
                Is.EqualTo(expectedIncrease).Within(0.000000001d));
            Assert.That(context.TargetTakenDamageIncreaseRateSum,
                Is.EqualTo(expectedIncrease).Within(0.000000001d));
            Assert.That(result.FinalDamage, Is.EqualTo(expectedDamage));
        }

        private static double Collect(
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss)
        {
            return CombatModifierCollector.CollectAllyDamage(
                attacker,
                party,
                boss,
                ElementType.Fire).TargetTakenDamageIncreaseRateSum;
        }

        private static DamageContext Build(
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss)
        {
            return new DamageContextFactory(new SeededRandomSource(1)).Build(
                new DamageContextBuildRequest(
                    attacker,
                    party,
                    boss,
                    ElementType.Fire,
                    AttackType.Match,
                    AttackTag.None,
                    skillCoefficient: 1d,
                    appliesCombo: false,
                    finalComboCount: 0,
                    canCritical: false));
        }

        private static EffectInstance GroupedEffect(
            long runtimeId,
            string effectId,
            string sourceId,
            double magnitude)
        {
            return new EffectInstance(
                runtimeId,
                effectId,
                sourceId,
                EffectCategory.Debuff,
                EffectModifierType.TargetTakenDamageIncrease,
                magnitude,
                remainingTurns: null,
                creationOrder: runtimeId,
                EffectStackPolicy.Unique,
                modifierGroupId: CombatModifierGroupRules
                    .AllyTargetTakenDamageIncreaseGroupId,
                modifierGroupCap: CombatModifierGroupRules
                    .AllyTargetTakenDamageIncreaseCap);
        }

        private static EffectInstance UngroupedEffect(
            long runtimeId,
            string effectId,
            EffectModifierType modifierType,
            double magnitude)
        {
            return new EffectInstance(
                runtimeId,
                effectId,
                "test_source",
                EffectCategory.Debuff,
                modifierType,
                magnitude,
                remainingTurns: null,
                creationOrder: runtimeId,
                EffectStackPolicy.Unique);
        }

        private static void CreateStates(
            out CharacterBattleState attacker,
            out PartyBattleState party,
            out BossBattleState boss)
        {
            attacker = new CharacterBattleState(
                "attacker",
                0,
                ElementType.Fire,
                1000,
                1000d);
            party = new PartyBattleState(new[] { attacker });
            boss = new BossBattleState(
                KragmorRules.BossId,
                ElementType.Fire,
                100000,
                100d);
        }
    }
}
