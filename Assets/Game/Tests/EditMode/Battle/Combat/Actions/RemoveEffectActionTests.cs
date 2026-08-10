using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Actions
{
    public sealed class RemoveEffectActionTests
    {
        [Test]
        public void ExecutorRemovesOnlyRequestedRuntimeEffectAndRecordsResult()
        {
            CharacterBattleState character = Character();
            var party = new PartyBattleState(new[] { character });
            BossBattleState boss = Boss();
            EffectInstance removed = Effect(10, "effect_remove_me");
            EffectInstance retained = Effect(11, "effect_keep_me");
            boss.Effects.ApplyEffect(removed);
            boss.Effects.ApplyEffect(retained);
            var action = new RemoveEffectAction(
                actionId: 1,
                ActionOrigin.System,
                boss,
                removed.RuntimeId);

            CombatActionExecutionResult execution = Executor(boss, party)
                .Execute(new CombatActionQueue(new CombatAction[]
                {
                    action
                }));

            var result = (RemoveEffectActionResult)
                execution.ActionResults[0];
            Assert.That(result.Action, Is.SameAs(action));
            Assert.That(result.ExecutionOrder, Is.EqualTo(1));
            Assert.That(result.WasRemoved, Is.True);
            Assert.That(boss.Effects.FindByEffectId(removed.EffectId),
                Is.Empty);
            Assert.That(boss.Effects.FindByEffectId(retained.EffectId),
                Has.Count.EqualTo(1));
        }

        [Test]
        public void MissingRuntimeEffectProducesExplicitNoRemovalResult()
        {
            CharacterBattleState character = Character();
            var party = new PartyBattleState(new[] { character });
            BossBattleState boss = Boss();
            var action = new RemoveEffectAction(
                actionId: 1,
                ActionOrigin.System,
                boss,
                effectRuntimeId: 999);

            CombatActionExecutionResult execution = Executor(boss, party)
                .Execute(new CombatActionQueue(new CombatAction[]
                {
                    action
                }));

            Assert.That(((RemoveEffectActionResult)
                execution.ActionResults[0]).WasRemoved, Is.False);
        }

        private static CombatActionExecutor Executor(
            BossBattleState boss,
            PartyBattleState party)
        {
            return new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)));
        }

        private static EffectInstance Effect(long runtimeId, string effectId)
        {
            return new EffectInstance(
                runtimeId,
                effectId,
                "test_source",
                EffectCategory.Debuff,
                EffectModifierType.TargetTakenDamageIncrease,
                magnitude: 0.15d,
                remainingTurns: null,
                creationOrder: runtimeId,
                EffectStackPolicy.Unique);
        }

        private static CharacterBattleState Character()
        {
            return new CharacterBattleState(
                "test_character",
                partySlotIndex: 0,
                ElementType.Fire,
                maxHp: 10000,
                attack: 100d);
        }

        private static BossBattleState Boss()
        {
            return new BossBattleState(
                "test_boss",
                ElementType.Fire,
                maxHp: 10000,
                attack: 100d);
        }
    }
}
