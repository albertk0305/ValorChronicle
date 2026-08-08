using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Actions
{
    public sealed class ResourceConsumptionTriggerTests
    {
        [Test]
        public void MatchSource_ConsumeAllCreatesOneChaseBeforeNextRoot()
        {
            var battle = new CombatTriggerTestBattle();
            battle.Boss.Resources.Register("water", 10);
            battle.Boss.Resources.Add("water", 5);
            int consumptionEventCount = 0;
            ResourceConsumptionRecord observedRecord = null;
            var consumeRule = new DelegateCombatTriggerRule(context =>
                context.ActionId == 1
                    ? new CombatAction[]
                    {
                        ConsumeAll(
                            10,
                            battle,
                            context.RootActionId,
                            context.ActionId)
                    }
                    : Array.Empty<CombatAction>());
            var chaseRule = new DelegateCombatTriggerRule(context =>
            {
                if (!(context.CompletedResult is
                        ConsumeResourceActionResult consumption)
                    || consumption.ConsumptionRecord == null)
                {
                    return Array.Empty<CombatAction>();
                }

                consumptionEventCount++;
                observedRecord = consumption.ConsumptionRecord;
                if (!context.CanTriggerAttackFollowUp)
                {
                    return Array.Empty<CombatAction>();
                }

                return new CombatAction[]
                {
                    battle.Damage(
                        20,
                        ActionOrigin.Chase,
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
                battle.Executor(consumeRule, chaseRule).Execute(queue);

            AssertActionOrder(result, 1, 10, 20, 2);
            Assert.That(consumptionEventCount, Is.EqualTo(1));
            Assert.That(observedRecord, Is.Not.Null);
            Assert.That(observedRecord.ResourceId, Is.EqualTo("water"));
            Assert.That(observedRecord.ConsumerId, Is.EqualTo("hero"));
            Assert.That(observedRecord.TargetId, Is.EqualTo("boss"));
            Assert.That(observedRecord.ConsumedAmount, Is.EqualTo(5));
            CombatAction chase = result.ActionResults[2].Action;
            Assert.That(chase.RootActionId, Is.EqualTo(1L));
            Assert.That(chase.SourceActionId, Is.EqualTo(10L));
            Assert.That(chase.CanTriggerFollowUp, Is.False);
        }

        [Test]
        public void ChaseSource_ConsumptionCannotCreateAnotherChase()
        {
            var battle = new CombatTriggerTestBattle();
            battle.Boss.Resources.Register("water", 10);
            battle.Boss.Resources.Add("water", 3);
            int chaseCreationCount = 0;
            var consumeRule = new DelegateCombatTriggerRule(context =>
                context.ActionId == 2
                    ? new CombatAction[]
                    {
                        ConsumeAll(
                            10,
                            battle,
                            context.RootActionId,
                            context.ActionId)
                    }
                    : Array.Empty<CombatAction>());
            var chaseRule = ChaseRule(battle, () => chaseCreationCount++);
            var queue = new CombatActionQueue(new CombatAction[]
            {
                battle.Damage(
                    2,
                    ActionOrigin.Chase,
                    0.10d,
                    1,
                    1)
            });

            CombatActionExecutionResult result =
                battle.Executor(consumeRule, chaseRule).Execute(queue);

            AssertActionOrder(result, 2, 10);
            Assert.That(chaseCreationCount, Is.Zero);
            Assert.That(battle.Boss.Resources.GetAmount("water"), Is.Zero);
        }

        [Test]
        public void MissingSourceInHistory_BlocksConsumptionFollowUp()
        {
            var battle = new CombatTriggerTestBattle();
            battle.Boss.Resources.Register("water", 10);
            battle.Boss.Resources.Add("water", 2);
            int chaseCreationCount = 0;
            var queue = new CombatActionQueue(new CombatAction[]
            {
                ConsumeAll(10, battle, 1, 999)
            });

            CombatActionExecutionResult result = battle.Executor(
                ChaseRule(battle, () => chaseCreationCount++)).Execute(queue);

            AssertActionOrder(result, 10);
            Assert.That(chaseCreationCount, Is.Zero);
        }

        [Test]
        public void ZeroConsumptionRecord_DoesNotCreateTriggerEvent()
        {
            var battle = new CombatTriggerTestBattle();
            battle.Boss.Resources.Register("water", 10);
            int consumptionEventCount = 0;
            var observeRule = new DelegateCombatTriggerRule(context =>
            {
                if (context.CompletedResult is
                        ConsumeResourceActionResult consumption
                    && consumption.ConsumptionRecord != null)
                {
                    consumptionEventCount++;
                }

                return Array.Empty<CombatAction>();
            });

            CombatActionExecutionResult result = battle.Executor(
                observeRule).Execute(new CombatActionQueue(
                    new CombatAction[]
                    {
                        new ConsumeResourceAction(
                            10,
                            ActionOrigin.System,
                            battle.Boss,
                            "water",
                            "hero",
                            ResourceConsumptionMode.All)
                    }));

            var consumption =
                (ConsumeResourceActionResult)result.ActionResults[0];
            Assert.That(consumption.ConsumptionRecord, Is.Null);
            Assert.That(consumptionEventCount, Is.Zero);
        }

        [Test]
        public void ResolverRejectsAttackWhenConsumptionSourceCannotFollowUp()
        {
            var battle = new CombatTriggerTestBattle();
            battle.Boss.Resources.Register("water", 10);
            battle.Boss.Resources.Add("water", 1);
            var invalidRule = new DelegateCombatTriggerRule(context =>
            {
                if (!(context.CompletedResult is
                    ConsumeResourceActionResult))
                {
                    return Array.Empty<CombatAction>();
                }

                return new CombatAction[]
                {
                    battle.Damage(
                        20,
                        ActionOrigin.Chase,
                        0.10d,
                        context.RootActionId,
                        context.ActionId)
                };
            });
            var queue = new CombatActionQueue(new CombatAction[]
            {
                ConsumeAll(10, battle, 1, 999)
            });

            Assert.Throws<InvalidOperationException>(() =>
                battle.Executor(invalidRule).Execute(queue));
            Assert.That(queue.Count, Is.Zero);
        }

        private static DelegateCombatTriggerRule ChaseRule(
            CombatTriggerTestBattle battle,
            Action onCreate)
        {
            return new DelegateCombatTriggerRule(context =>
            {
                if (!(context.CompletedResult is
                        ConsumeResourceActionResult consumption)
                    || consumption.ConsumptionRecord == null
                    || !context.CanTriggerAttackFollowUp)
                {
                    return Array.Empty<CombatAction>();
                }

                onCreate();
                return new CombatAction[]
                {
                    battle.Damage(
                        20,
                        ActionOrigin.Chase,
                        0.10d,
                        context.RootActionId,
                        context.ActionId)
                };
            });
        }

        private static ConsumeResourceAction ConsumeAll(
            long actionId,
            CombatTriggerTestBattle battle,
            long rootActionId,
            long sourceActionId)
        {
            return new ConsumeResourceAction(
                actionId,
                ActionOrigin.System,
                battle.Boss,
                "water",
                battle.Character.CharacterId,
                ResourceConsumptionMode.All,
                rootActionId: rootActionId,
                sourceActionId: sourceActionId);
        }

        private static void AssertActionOrder(
            CombatActionExecutionResult result,
            params long[] actionIds)
        {
            Assert.That(result.CompletedActionCount,
                Is.EqualTo(actionIds.Length));
            for (int index = 0; index < actionIds.Length; index++)
            {
                Assert.That(result.ActionResults[index].Action.ActionId,
                    Is.EqualTo(actionIds[index]));
            }
        }
    }
}
