using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Actions
{
    public sealed class CombatTriggerTerminationTests
    {
        [Test]
        public void BossDefeat_PreventsTriggerEvaluationAndClearsQueue()
        {
            var battle = new CombatTriggerTestBattle(
                bossHp: 50,
                characterAttack: 100d);
            int evaluationCount = 0;
            var rule = new DelegateCombatTriggerRule(context =>
            {
                evaluationCount++;
                return new CombatAction[]
                {
                    battle.Damage(
                        10,
                        ActionOrigin.Additional,
                        0.10d,
                        context.RootActionId,
                        context.ActionId)
                };
            });
            var queue = new CombatActionQueue(new CombatAction[]
            {
                battle.Damage(1, ActionOrigin.Match),
                battle.Damage(2, ActionOrigin.Active)
            });

            CombatActionExecutionResult result =
                battle.Executor(rule).Execute(queue);

            Assert.That(result.CompletedActionCount, Is.EqualTo(1));
            Assert.That(result.BossDefeated, Is.True);
            Assert.That(result.ClearedRemainingActions, Is.True);
            Assert.That(evaluationCount, Is.Zero);
            Assert.That(queue.Count, Is.Zero);
        }

        [Test]
        public void PartyIncapacitation_PreventsTriggerEvaluationAndClearsQueue()
        {
            var battle = new CombatTriggerTestBattle(
                partyHp: 50,
                bossAttack: 100d);
            int evaluationCount = 0;
            var rule = new DelegateCombatTriggerRule(context =>
            {
                evaluationCount++;
                return Array.Empty<CombatAction>();
            });
            var queue = new CombatActionQueue(new CombatAction[]
            {
                new BossDamageAction(
                    1,
                    new BossDamageContextBuildRequest(
                        battle.Boss,
                        battle.Party,
                        1d,
                        AttackTag.None)),
                battle.Damage(2, ActionOrigin.Active)
            });

            CombatActionExecutionResult result =
                battle.Executor(rule).Execute(queue);

            Assert.That(result.CompletedActionCount, Is.EqualTo(1));
            Assert.That(result.PartyIncapacitated, Is.True);
            Assert.That(result.ClearedRemainingActions, Is.True);
            Assert.That(evaluationCount, Is.Zero);
            Assert.That(queue.Count, Is.Zero);
        }
    }
}
