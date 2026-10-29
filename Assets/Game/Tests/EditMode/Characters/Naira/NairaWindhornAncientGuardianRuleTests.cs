using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.Shields;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Naira;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Naira
{
    public sealed class NairaWindhornAncientGuardianRuleTests
    {
        [TestCase(0)]
        [TestCase(5)]
        public void AwakeningBelowSixDoesNotRegisterOrGrant(int awakening)
        {
            using (var battle = Battle(awakening, initialShield: 800L))
            {
                Assert.That(battle.Registration.TriggerRules.Count, Is.Zero);

                battle.ExecuteMatch(BoardMatchTier.FiveOrMore);

                Assert.That(battle.Party.Effects.FindByEffectId(
                    NairaWindhornRules.AncientGuardianEffectId).Count,
                    Is.Zero);
            }
        }

        [Test]
        public void UnderchargedFiveMatchDoesNotGrant()
        {
            using (var battle = Battle(6, initialShield: 799L))
            {
                CombatActionExecutionResult execution =
                    battle.ExecuteMatch(BoardMatchTier.FiveOrMore);

                Assert.That(execution.ActionResults.Count, Is.EqualTo(2));
                Assert.That(battle.Party.Shields.TotalShield,
                    Is.EqualTo(999L));
                Assert.That(battle.Party.Effects.Count, Is.Zero);
            }
        }

        [Test]
        public void FullChargeGrantsBeforeFiveMatchDamageWithLineage()
        {
            using (var battle = Battle(6, initialShield: 800L))
            {
                CombatActionExecutionResult execution =
                    battle.ExecuteMatch(BoardMatchTier.FiveOrMore);

                Assert.That(execution.ActionResults.Count, Is.EqualTo(3));
                ShieldAction shield = (ShieldAction)execution
                    .ActionResults[0].Action;
                ApplyEffectAction apply = (ApplyEffectAction)execution
                    .ActionResults[1].Action;
                Assert.That(execution.ActionResults[2],
                    Is.TypeOf<DamageActionResult>());
                Assert.That(apply.Origin, Is.EqualTo(ActionOrigin.System));
                Assert.That(apply.RootActionId,
                    Is.EqualTo(shield.RootActionId));
                Assert.That(apply.SourceActionId,
                    Is.EqualTo(shield.ActionId));
                Assert.That(apply.ActionId, Is.EqualTo(3L));
                Assert.That(battle.ActionIds.LastIssuedId, Is.EqualTo(3L));
                AssertAncientGuardian(battle.AncientGuardian());
            }
        }

        [Test]
        public void FullShieldCapAndZeroGrantStillGrantsGuardian()
        {
            using (var battle = Battle(6, initialShield: 1000L))
            {
                CombatActionExecutionResult execution =
                    battle.ExecuteMatch(BoardMatchTier.FiveOrMore);
                ShieldActionResult shield = execution.ActionResults
                    .OfType<ShieldActionResult>()
                    .Single();

                Assert.That(shield.ApplicationResult.GrantedShieldAmount,
                    Is.Zero);
                Assert.That(shield.ApplicationResult.TotalShieldAfter,
                    Is.EqualTo(1000L));
                AssertAncientGuardian(battle.AncientGuardian());
            }
        }

        [TestCase(BoardMatchTier.Three)]
        [TestCase(BoardMatchTier.Four)]
        public void ThreeAndFourMatchNeverGrantAtFullCharge(
            BoardMatchTier tier)
        {
            using (var battle = Battle(6, initialShield: 1000L))
            {
                battle.ExecuteMatch(tier);

                Assert.That(battle.Party.Effects.Count, Is.Zero);
            }
        }

        [Test]
        public void ActiveNeverGrantsAtFullCharge()
        {
            using (var battle = Battle(6, initialShield: 1000L))
            {
                battle.ExecuteActive();

                Assert.That(battle.Party.Effects.FindByEffectId(
                    NairaWindhornRules.AncientGuardianEffectId).Count,
                    Is.Zero);
                Assert.That(battle.Party.Effects.FindByEffectId(
                    NairaWindhornRules.WhiteStagGraceEffectId).Count,
                    Is.EqualTo(1));
            }
        }

        [Test]
        public void OtherCharacterShieldWithMatchFiveIdDoesNotGrant()
        {
            using (var battle = Battle(
                6,
                initialShield: 1000L,
                includeOtherPartyMember: true))
            {
                CharacterBattleState other = battle.OtherCharacter;
                Assert.That(other, Is.Not.Null);
                long actionId = battle.ActionIds.Next();
                var action = new ShieldAction(
                    actionId,
                    ActionOrigin.Match,
                    new ShieldGenerationContextBuildRequest(
                        other,
                        battle.Party,
                        0.20d,
                        appliesCombo: true,
                        finalComboCount: 1),
                    new ShieldGrantRequest(
                        actionId,
                        NairaWindhornRules.Match5AbilityId,
                        createdTurn: 1,
                        remainingTurns: 3,
                        creationOrder: actionId));

                CombatActionExecutionResult execution = battle.Execute(
                    new CombatAction[] { action });
                ShieldActionResult shield = (ShieldActionResult)execution
                    .ActionResults[0];

                Assert.That(shield.ApplicationResult.GrantedShieldAmount,
                    Is.EqualTo(200L));
                Assert.That(shield.ApplicationResult.TotalShieldAfter,
                    Is.EqualTo(1200L));
                Assert.That(shield.ApplicationResult.TotalShieldAfter,
                    Is.GreaterThanOrEqualTo(battle.Character.MaxHp));
                Assert.That(battle.Party.Effects.Count, Is.Zero);
            }
        }

        [TestCase(959L, false)]
        [TestCase(960L, true)]
        public void UsesNairaFinalMaxHpAsChargeDenominator(
            long initialShield,
            bool expectedGrant)
        {
            using (var battle = Battle(
                6,
                maxHp: 1200L,
                initialShield: initialShield))
            {
                battle.ExecuteMatch(BoardMatchTier.FiveOrMore);

                Assert.That(battle.Party.Effects.FindByEffectId(
                        NairaWindhornRules.AncientGuardianEffectId).Count,
                    Is.EqualTo(expectedGrant ? 1 : 0));
            }
        }

        [Test]
        public void ReacquisitionKeepsExistingUniqueInstance()
        {
            using (var battle = Battle(6, initialShield: 1000L))
            {
                battle.ExecuteMatch(BoardMatchTier.FiveOrMore);
                EffectInstance first = battle.AncientGuardian();

                battle.ExecuteMatch(BoardMatchTier.FiveOrMore);

                Assert.That(battle.Party.Effects.Count, Is.EqualTo(1));
                Assert.That(battle.AncientGuardian(), Is.SameAs(first));
                Assert.That(first.RemainingTurns, Is.Null);
            }
        }

        [Test]
        public void GuardianPersistsAcrossTurnEndThenDirectHitConsumesIt()
        {
            using (var battle = Battle(6, initialShield: 1000L))
            {
                battle.ExecuteMatch(BoardMatchTier.FiveOrMore);
                battle.Party.Effects.ProcessTurnEnd();
                Assert.That(battle.Party.Effects.Count, Is.EqualTo(1));

                BossDamageActionResult hit = battle.ExecuteBossHit(
                    BossDamageHitType.Direct);

                Assert.That(hit.DamageResult.FinalDamageBeforeShield,
                    Is.EqualTo(850L));
                Assert.That(battle.Party.Effects.Count, Is.Zero);
            }
        }

        [Test]
        public void FullShieldAbsorptionStillConsumesGuardian()
        {
            using (var battle = Battle(6, initialShield: 1000L))
            {
                battle.ExecuteMatch(BoardMatchTier.FiveOrMore);

                BossDamageActionResult hit = battle.ExecuteBossHit(
                    BossDamageHitType.Direct);

                Assert.That(hit.ApplicationResult.HpDamage, Is.Zero);
                Assert.That(hit.ApplicationResult.ShieldAbsorbedDamage,
                    Is.EqualTo(850L));
                Assert.That(battle.Party.Effects.Count, Is.Zero);
            }
        }

        [TestCase(BossDamageHitType.DamageOverTime)]
        [TestCase(BossDamageHitType.Reflected)]
        [TestCase(BossDamageHitType.SelfInflicted)]
        public void ExcludedDamageDoesNotApplyOrConsumeGuardian(
            BossDamageHitType hitType)
        {
            using (var battle = Battle(6, initialShield: 1000L))
            {
                battle.ExecuteMatch(BoardMatchTier.FiveOrMore);

                BossDamageActionResult hit = battle.ExecuteBossHit(hitType);

                Assert.That(hit.DamageResult.FinalDamageBeforeShield,
                    Is.EqualTo(1000L));
                Assert.That(battle.Party.Effects.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void WhiteStagGraceAndGuardianConsumeFortyThenFifteenThenZero()
        {
            using (var battle = Battle(6, initialShield: 1000L))
            {
                battle.ExecuteActive();
                battle.ExecuteMatch(BoardMatchTier.FiveOrMore);
                Assert.That(battle.Party.Effects.Count, Is.EqualTo(2));

                long[] damages = new long[3];
                for (int index = 0; index < damages.Length; index++)
                {
                    damages[index] = battle.ExecuteBossHit(
                        BossDamageHitType.Direct).DamageResult
                        .FinalDamageBeforeShield;
                }

                Assert.That(damages,
                    Is.EqualTo(new long[] { 600L, 850L, 1000L }));
                Assert.That(battle.Party.Effects.Count, Is.Zero);
            }
        }

        [Test]
        public void SpiritWardAndGuardianExistOnTheirIntendedTargets()
        {
            using (var battle = Battle(6, initialShield: 800L))
            using (battle.CreateSynchronizer())
            {
                battle.ExecuteMatch(BoardMatchTier.FiveOrMore);

                Assert.That(battle.Boss.Effects.FindByEffectId(
                        NairaWindhornRules.SpiritWardEffectId).Count,
                    Is.EqualTo(1));
                Assert.That(battle.Party.Effects.FindByEffectId(
                        NairaWindhornRules.AncientGuardianEffectId).Count,
                    Is.EqualTo(1));
            }
        }

        [Test]
        public void GuardianCombinesWithGeneralReductionAndSeventyPercentCap()
        {
            using (var battle = Battle(6, initialShield: 1000L))
            {
                battle.Party.Effects.ApplyEffect(new EffectInstance(
                    9001L,
                    "general_reduction",
                    "test_source",
                    EffectCategory.Buff,
                    EffectModifierType.PartyDamageReduction,
                    0.60d,
                    remainingTurns: null,
                    creationOrder: 9001L,
                    stackPolicy: EffectStackPolicy.Unique));
                battle.ExecuteMatch(BoardMatchTier.FiveOrMore);

                BossDamageActionResult hit = battle.ExecuteBossHit(
                    BossDamageHitType.Direct);

                Assert.That(hit.Context.PartyDamageReductionRateSum,
                    Is.EqualTo(0.75d));
                Assert.That(hit.DamageResult
                    .EffectivePartyDamageReductionRate, Is.EqualTo(0.70d));
                Assert.That(hit.DamageResult.FinalDamageBeforeShield,
                    Is.EqualTo(300L));
                Assert.That(battle.Party.Effects.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void TerminalBattleBeforeShieldSkipsTriggerAndRemainingActions()
        {
            using (var battle = Battle(
                6,
                initialShield: 1000L,
                bossHp: 1L))
            {
                IReadOnlyList<CombatAction> match = battle.CreateMatch(
                    BoardMatchTier.FiveOrMore);
                var lethal = new DamageAction(
                    battle.ActionIds.Next(),
                    ActionOrigin.System,
                    new DamageContextBuildRequest(
                        battle.Character,
                        battle.Party,
                        battle.Boss,
                        ElementType.Grass,
                        AttackType.Match,
                        AttackTag.None,
                        skillCoefficient: 1d,
                        appliesCombo: false,
                        finalComboCount: 0,
                        canCritical: false));
                CombatAction[] actions = new[] { lethal }
                    .Concat(match)
                    .ToArray();

                CombatActionExecutionResult execution =
                    battle.Execute(actions);

                Assert.That(execution.CompletedActionCount, Is.EqualTo(1));
                Assert.That(execution.ClearedRemainingActions, Is.True);
                Assert.That(battle.Party.Effects.Count, Is.Zero);
            }
        }

        private static NairaWindhornCombatProviderRegistrarTests
            .RegistrationBattle Battle(
                int awakening,
                long maxHp = 1000L,
                long initialShield = 0L,
                long bossHp = 1000000L,
                bool includeOtherPartyMember = false)
        {
            var battle = new NairaWindhornCombatProviderRegistrarTests
                .RegistrationBattle(
                    awakening,
                    maxHp,
                    bossHp,
                    includeOtherPartyMember);
            if (initialShield > 0L)
            {
                battle.AddShield(
                    initialShield,
                    9000L,
                    remainingTurns: 5);
            }

            return battle;
        }

        private static void AssertAncientGuardian(EffectInstance effect)
        {
            Assert.That(effect.EffectId,
                Is.EqualTo(NairaWindhornRules.AncientGuardianEffectId));
            Assert.That(effect.SourceId,
                Is.EqualTo(NairaWindhornRules.CharacterId));
            Assert.That(effect.ModifierType,
                Is.EqualTo(
                    EffectModifierType.NextBossHitDamageReduction));
            Assert.That(effect.Magnitude, Is.EqualTo(0.15d));
            Assert.That(effect.StackPolicy,
                Is.EqualTo(EffectStackPolicy.Unique));
            Assert.That(effect.MaxStackCount, Is.EqualTo(1));
            Assert.That(effect.RemainingTurns, Is.Null);
        }
    }

    internal static class NairaWindhornAncientGuardianBattleExtensions
    {
        public static EffectInstance AncientGuardian(
            this NairaWindhornCombatProviderRegistrarTests
                .RegistrationBattle battle)
        {
            return battle.Party.Effects.FindByEffectId(
                NairaWindhornRules.AncientGuardianEffectId).Single();
        }

        public static IReadOnlyList<CombatAction> CreateMatch(
            this NairaWindhornCombatProviderRegistrarTests
                .RegistrationBattle battle,
            BoardMatchTier tier)
        {
            battle.MatchProviders.TryResolve(
                NairaWindhornRules.CharacterId,
                out IMatchEventActionProvider provider);
            return ((NairaWindhornMatchActionProvider)provider).CreateActions(
                battle.Character,
                battle.Party,
                battle.Boss,
                tier,
                finalComboCount: 1,
                playerTurnNumber: 1,
                battle.ActionIds);
        }

        public static BossDamageActionResult ExecuteBossHit(
            this NairaWindhornCombatProviderRegistrarTests
                .RegistrationBattle battle,
            BossDamageHitType hitType)
        {
            var action = new BossDamageAction(
                battle.ActionIds.Next(),
                new BossDamageContextBuildRequest(
                    battle.Boss,
                    battle.Party,
                    attackCoefficient: 1d,
                    attackTags: AttackTag.None,
                    hitType));
            return (BossDamageActionResult)battle.Execute(
                new CombatAction[] { action }).ActionResults[0];
        }
    }
}
