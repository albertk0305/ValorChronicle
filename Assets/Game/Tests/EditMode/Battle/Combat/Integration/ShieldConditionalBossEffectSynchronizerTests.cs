using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.Modifiers;
using ValorChronicle.Battle.Combat.Shields;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Integration
{
    public sealed class ShieldConditionalBossEffectSynchronizerTests
    {
        [Test]
        public void ZeroShieldStartsWithoutConditionalEffect()
        {
            CreateBattle(out _, out PartyBattleState party,
                out BossBattleState boss);
            var synchronizer = Synchronizer(party, boss);

            Assert.That(ConditionalEffects(boss), Is.Empty);

            synchronizer.Dispose();
        }

        [Test]
        public void FirstShieldAddsEffectAndAdditionalShieldDoesNotDuplicate()
        {
            CreateBattle(out _, out PartyBattleState party,
                out BossBattleState boss);
            var synchronizer = Synchronizer(party, boss);

            party.Shields.Add(Shield(1, 100, 3));
            EffectInstance first = ConditionalEffects(boss)[0];
            party.Shields.Add(Shield(2, 200, 3));

            Assert.That(ConditionalEffects(boss),
                Is.EqualTo(new[] { first }));
            Assert.That(first.RemainingTurns, Is.Null);
            Assert.That(first.ModifierGroupId, Is.EqualTo(
                CombatModifierGroupRules
                    .AllyTargetTakenDamageIncreaseGroupId));
            Assert.That(first.ModifierGroupCap, Is.EqualTo(
                CombatModifierGroupRules
                    .AllyTargetTakenDamageIncreaseCap));
            synchronizer.Dispose();
        }

        [Test]
        public void ConditionalEffectParticipatesInSharedCapAndRemoval()
        {
            CreateBattle(out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss);
            boss.Effects.ApplyEffect(GroupedVulnerability(
                runtimeId: 1,
                effectId: "other_ally_vulnerability",
                sourceId: "other_character",
                magnitude: 0.24d));
            var synchronizer = Synchronizer(party, boss);

            party.Shields.Add(Shield(1, 100, 3));
            Assert.That(CollectTakenIncrease(character, party, boss),
                Is.EqualTo(0.25d));

            party.Shields.Absorb(100);
            Assert.That(CollectTakenIncrease(character, party, boss),
                Is.EqualTo(0.24d));
            synchronizer.Dispose();
        }

        [Test]
        public void PartialAbsorptionKeepsEffectAndFullAbsorptionRemovesIt()
        {
            CreateBattle(out _, out PartyBattleState party,
                out BossBattleState boss);
            var synchronizer = Synchronizer(party, boss);
            party.Shields.Add(Shield(1, 500, 3));

            party.Shields.Absorb(200);
            Assert.That(ConditionalEffects(boss), Has.Count.EqualTo(1));

            party.Shields.Absorb(300);
            Assert.That(ConditionalEffects(boss), Is.Empty);
            synchronizer.Dispose();
        }

        [Test]
        public void ExpirationRemovesOnlyWhenLastShieldExpires()
        {
            CreateBattle(out _, out PartyBattleState party,
                out BossBattleState boss);
            var synchronizer = Synchronizer(party, boss);
            party.Shields.Add(Shield(1, 100, 1));
            party.Shields.Add(Shield(2, 200, 2));

            party.Shields.ProcessTurnEnd();
            Assert.That(party.Shields.TotalShield, Is.EqualTo(200L));
            Assert.That(ConditionalEffects(boss), Has.Count.EqualTo(1));

            party.Shields.ProcessTurnEnd();
            Assert.That(party.Shields.TotalShield, Is.Zero);
            Assert.That(ConditionalEffects(boss), Is.Empty);
            synchronizer.Dispose();
        }

        [Test]
        public void EffectIsRecreatedAfterShieldReturnsFromZero()
        {
            CreateBattle(out _, out PartyBattleState party,
                out BossBattleState boss);
            long nextIdentity = 100;
            var synchronizer = new ShieldConditionalBossEffectSynchronizer(
                party.Shields,
                boss.Effects,
                Definition(),
                () => nextIdentity++);
            party.Shields.Add(Shield(1, 100, 3));
            long firstRuntimeId = ConditionalEffects(boss)[0].RuntimeId;
            party.Shields.Absorb(100);

            party.Shields.Add(Shield(2, 100, 3));

            Assert.That(ConditionalEffects(boss), Has.Count.EqualTo(1));
            Assert.That(ConditionalEffects(boss)[0].RuntimeId,
                Is.Not.EqualTo(firstRuntimeId));
            synchronizer.Dispose();
        }

        [Test]
        public void CappedZeroGrantDoesNotDuplicateOrReplaceEffect()
        {
            CreateBattle(out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss);
            party.Shields.Add(Shield(1, party.MaxHp, 3));
            var synchronizer = Synchronizer(party, boss);
            EffectInstance existing = ConditionalEffects(boss)[0];
            int notificationCount = 0;
            party.Shields.TotalShieldChanged += _ => notificationCount++;
            ShieldActionResult result = (ShieldActionResult)Executor(
                party,
                boss).Execute(new CombatActionQueue(new CombatAction[]
                {
                    ShieldAction(2, character, party, 0.50d)
                })).ActionResults[0];

            Assert.That(result.ApplicationResult.GrantedShieldAmount,
                Is.Zero);
            Assert.That(notificationCount, Is.Zero);
            Assert.That(ConditionalEffects(boss),
                Is.EqualTo(new[] { existing }));
            synchronizer.Dispose();
        }

        [Test]
        public void SynchronizationPreservesOtherBossEffects()
        {
            CreateBattle(out _, out PartyBattleState party,
                out BossBattleState boss);
            EffectInstance other = OtherBossEffect(1);
            boss.Effects.ApplyEffect(other);
            var synchronizer = Synchronizer(party, boss);

            party.Shields.Add(Shield(1, 100, 3));
            party.Shields.Absorb(100);

            Assert.That(boss.Effects.GetActiveEffects(),
                Is.EqualTo(new[] { other }));
            synchronizer.Dispose();
        }

        [Test]
        public void ShieldActionImmediatelyAffectsFollowingDamageAction()
        {
            CreateBattle(out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss);
            var synchronizer = Synchronizer(party, boss);
            var queue = new CombatActionQueue(new CombatAction[]
            {
                ShieldAction(1, character, party, 0.10d),
                new DamageAction(
                    2,
                    ActionOrigin.Match,
                    DamageRequest(character, party, boss))
            });

            CombatActionExecutionResult execution =
                Executor(party, boss).Execute(queue);
            var damage = (DamageActionResult)execution.ActionResults[1];

            Assert.That(damage.Context.TargetTakenDamageIncreaseRateSum,
                Is.EqualTo(0.05d));
            Assert.That(damage.DamageResult.FinalDamage,
                Is.EqualTo(1050L));
            Assert.That(ConditionalEffects(boss), Has.Count.EqualTo(1));
            synchronizer.Dispose();
        }

        [Test]
        public void DamageWithoutShieldDoesNotReceiveConditionalEffect()
        {
            CreateBattle(out CharacterBattleState character,
                out PartyBattleState party,
                out BossBattleState boss);
            var synchronizer = Synchronizer(party, boss);

            var damage = (DamageActionResult)Executor(party, boss).Execute(
                new CombatActionQueue(new CombatAction[]
                {
                    new DamageAction(
                        1,
                        ActionOrigin.Match,
                        DamageRequest(character, party, boss))
                })).ActionResults[0];

            Assert.That(damage.Context.TargetTakenDamageIncreaseRateSum,
                Is.Zero);
            Assert.That(damage.DamageResult.FinalDamage,
                Is.EqualTo(1000L));
            synchronizer.Dispose();
        }

        [Test]
        public void ExistingShieldIsAppliedDuringInitialSynchronization()
        {
            CreateBattle(out _, out PartyBattleState party,
                out BossBattleState boss);
            party.Shields.Add(Shield(1, 100, 3));

            var synchronizer = Synchronizer(party, boss);

            Assert.That(ConditionalEffects(boss), Has.Count.EqualTo(1));
            synchronizer.Dispose();
        }

        [Test]
        public void DisposeRemovesEffectAndUnsubscribesFromShieldChanges()
        {
            CreateBattle(out _, out PartyBattleState party,
                out BossBattleState boss);
            var synchronizer = Synchronizer(party, boss);
            party.Shields.Add(Shield(1, 100, 3));

            synchronizer.Dispose();
            synchronizer.Dispose();
            party.Shields.Add(Shield(2, 100, 3));

            Assert.That(ConditionalEffects(boss), Is.Empty);
        }

        [Test]
        public void SeparateBattleDoesNotShareEffectOrSubscriptionState()
        {
            CreateBattle(out _, out PartyBattleState firstParty,
                out BossBattleState firstBoss);
            var first = Synchronizer(firstParty, firstBoss);
            firstParty.Shields.Add(Shield(1, 100, 3));
            first.Dispose();
            CreateBattle(out _, out PartyBattleState secondParty,
                out BossBattleState secondBoss);
            var second = Synchronizer(secondParty, secondBoss);

            firstParty.Shields.Add(Shield(2, 100, 3));

            Assert.That(ConditionalEffects(firstBoss), Is.Empty);
            Assert.That(ConditionalEffects(secondBoss), Is.Empty);
            second.Dispose();
        }

        [Test]
        public void BattleWithoutSynchronizerKeepsExistingBehavior()
        {
            CreateBattle(out _, out PartyBattleState party,
                out BossBattleState boss);

            party.Shields.Add(Shield(1, 100, 3));

            Assert.That(boss.Effects.GetActiveEffects(), Is.Empty);
        }

        private static ShieldConditionalBossEffectSynchronizer Synchronizer(
            PartyBattleState party,
            BossBattleState boss)
        {
            long nextIdentity = 100;
            return new ShieldConditionalBossEffectSynchronizer(
                party.Shields,
                boss.Effects,
                Definition(),
                () => nextIdentity++);
        }

        private static ShieldConditionalBossEffectDefinition Definition()
        {
            return new ShieldConditionalBossEffectDefinition(
                "test_shield_conditional_debuff",
                "test_character",
                EffectCategory.Debuff,
                EffectModifierType.TargetTakenDamageIncrease,
                0.05d,
                CombatModifierGroupRules
                    .AllyTargetTakenDamageIncreaseGroupId,
                CombatModifierGroupRules
                    .AllyTargetTakenDamageIncreaseCap);
        }

        private static double CollectTakenIncrease(
            CharacterBattleState character,
            PartyBattleState party,
            BossBattleState boss)
        {
            return CombatModifierCollector.CollectAllyDamage(
                character,
                party,
                boss,
                ElementType.Fire).TargetTakenDamageIncreaseRateSum;
        }

        private static EffectInstance GroupedVulnerability(
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

        private static IReadOnlyList<EffectInstance> ConditionalEffects(
            BossBattleState boss)
        {
            return boss.Effects.FindByEffectId(
                "test_shield_conditional_debuff");
        }

        private static EffectInstance OtherBossEffect(long runtimeId)
        {
            return new EffectInstance(
                runtimeId,
                "other_boss_effect",
                "other_source",
                EffectCategory.Buff,
                EffectModifierType.BossAttackIncrease,
                0.10d,
                remainingTurns: null,
                creationOrder: runtimeId,
                EffectStackPolicy.Unique);
        }

        private static ShieldInstance Shield(
            long runtimeId,
            long amount,
            int remainingTurns)
        {
            return new ShieldInstance(
                runtimeId,
                "test_source",
                amount,
                1,
                remainingTurns,
                runtimeId);
        }

        private static ShieldAction ShieldAction(
            long actionId,
            CharacterBattleState character,
            PartyBattleState party,
            double coefficient)
        {
            return new ShieldAction(
                actionId,
                ActionOrigin.Match,
                new ShieldGenerationContextBuildRequest(
                    character,
                    party,
                    coefficient,
                    false,
                    0),
                new ShieldGrantRequest(
                    actionId,
                    character.CharacterId,
                    1,
                    3,
                    actionId));
        }

        private static DamageContextBuildRequest DamageRequest(
            CharacterBattleState character,
            PartyBattleState party,
            BossBattleState boss)
        {
            return new DamageContextBuildRequest(
                character,
                party,
                boss,
                ElementType.Fire,
                AttackType.Match,
                AttackTag.Match4,
                1d,
                false,
                0,
                false);
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

        private static void CreateBattle(
            out CharacterBattleState character,
            out PartyBattleState party,
            out BossBattleState boss)
        {
            character = new CharacterBattleState(
                "test_character",
                0,
                ElementType.Fire,
                1000,
                1000d);
            party = new PartyBattleState(new[] { character });
            boss = new BossBattleState(
                "test_boss",
                ElementType.Fire,
                10000,
                100d);
        }
    }
}
