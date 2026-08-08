using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Actions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Actions
{
    public sealed class CombatTriggerResolverTests
    {
        [Test]
        public void Execute_RulesAndDerivedBatchesKeepDeterministicOrder()
        {
            var battle = new CombatTriggerTestBattle();
            var firstRule = new DelegateCombatTriggerRule(context =>
                context.ActionId == 1
                    ? new CombatAction[]
                    {
                        battle.Damage(
                            10,
                            ActionOrigin.Additional,
                            0.10d,
                            context.RootActionId,
                            context.ActionId),
                        battle.Damage(
                            11,
                            ActionOrigin.Additional,
                            0.10d,
                            context.RootActionId,
                            context.ActionId)
                    }
                    : Array.Empty<CombatAction>());
            var secondRule = new DelegateCombatTriggerRule(context =>
                context.ActionId == 1
                    ? new CombatAction[]
                    {
                        battle.Damage(
                            12,
                            ActionOrigin.Chase,
                            0.10d,
                            context.RootActionId,
                            context.ActionId)
                    }
                    : Array.Empty<CombatAction>());
            var queue = new CombatActionQueue(new CombatAction[]
            {
                battle.Damage(1, ActionOrigin.Match),
                battle.Damage(2, ActionOrigin.Active)
            });

            CombatActionExecutionResult result =
                battle.Executor(firstRule, secondRule).Execute(queue);

            AssertActionOrder(result, 1, 10, 11, 12, 2);
            Assert.That(result.ActionResults[1].Action.Origin,
                Is.EqualTo(ActionOrigin.Additional));
            Assert.That(result.ActionResults[3].Action.Origin,
                Is.EqualTo(ActionOrigin.Chase));
            Assert.That(result.ActionResults[1].Action.RootActionId,
                Is.EqualTo(1L));
            Assert.That(result.ActionResults[1].Action.SourceActionId,
                Is.EqualTo(1L));
        }

        [TestCase(ActionOrigin.Additional)]
        [TestCase(ActionOrigin.Chase)]
        public void Execute_DerivedDamageCannotRecursivelyTrigger(
            ActionOrigin derivedOrigin)
        {
            var battle = new CombatTriggerTestBattle();
            int nextActionId = 10;
            var rule = new DelegateCombatTriggerRule(context =>
            {
                if (!(context.CompletedResult is DamageActionResult)
                    || !context.CanTriggerAttackFollowUp)
                {
                    return Array.Empty<CombatAction>();
                }

                return new CombatAction[]
                {
                    battle.Damage(
                        nextActionId++,
                        derivedOrigin,
                        0.10d,
                        context.RootActionId,
                        context.ActionId)
                };
            });
            var queue = new CombatActionQueue(new CombatAction[]
            {
                battle.Damage(1, ActionOrigin.Match)
            });

            CombatActionExecutionResult result =
                battle.Executor(rule).Execute(queue);

            AssertActionOrder(result, 1, 10);
            Assert.That(result.ActionResults[1].Action.CanTriggerFollowUp,
                Is.False);
        }

        [Test]
        public void TriggerContext_ProvidesImmutableHistoryAndSourceLookup()
        {
            var battle = new CombatTriggerTestBattle();
            CombatActionTriggerContext derivedContext = null;
            var createRule = new DelegateCombatTriggerRule(context =>
                context.ActionId == 1
                    ? new CombatAction[]
                    {
                        battle.Damage(
                            10,
                            ActionOrigin.Additional,
                            0.10d,
                            context.RootActionId,
                            context.ActionId)
                    }
                    : Array.Empty<CombatAction>());
            var observeRule = new DelegateCombatTriggerRule(context =>
            {
                if (context.ActionId == 10)
                {
                    derivedContext = context;
                }

                return Array.Empty<CombatAction>();
            });

            battle.Executor(createRule, observeRule).Execute(
                new CombatActionQueue(new CombatAction[]
                {
                    battle.Damage(1, ActionOrigin.Match)
                }));

            Assert.That(derivedContext, Is.Not.Null);
            Assert.That(derivedContext.History.Count, Is.EqualTo(2));
            Assert.That(derivedContext.TryGetSourceActionResult(
                out CombatActionResult sourceResult), Is.True);
            Assert.That(sourceResult.Action.ActionId, Is.EqualTo(1L));
            Assert.That(sourceResult.Action.CanTriggerFollowUp, Is.True);
        }

        [Test]
        public void ResolverFailure_DoesNotPartiallyInsertDerivedBatch()
        {
            var battle = new CombatTriggerTestBattle();
            var validRule = new DelegateCombatTriggerRule(context =>
                context.ActionId == 1
                    ? new CombatAction[]
                    {
                        battle.Damage(
                            10,
                            ActionOrigin.Additional,
                            0.10d,
                            context.RootActionId,
                            context.ActionId)
                    }
                    : Array.Empty<CombatAction>());
            var invalidRule = new DelegateCombatTriggerRule(context =>
                context.ActionId == 1
                    ? new CombatAction[] { null }
                    : Array.Empty<CombatAction>());
            CombatAction secondRoot =
                battle.Damage(2, ActionOrigin.Active);
            var queue = new CombatActionQueue(new CombatAction[]
            {
                battle.Damage(1, ActionOrigin.Match),
                secondRoot
            });

            Assert.Throws<InvalidOperationException>(() =>
                battle.Executor(validRule, invalidRule).Execute(queue));

            Assert.That(queue.Count, Is.EqualTo(1));
            Assert.That(queue.TryDequeue(out CombatAction remaining), Is.True);
            Assert.That(remaining, Is.SameAs(secondRoot));
        }

        [Test]
        public void EnqueueNextRange_PreservesBatchOrderAndIsAtomic()
        {
            var battle = new CombatTriggerTestBattle();
            CombatAction root = battle.Damage(2, ActionOrigin.Active);
            var queue = new CombatActionQueue(new[] { root });
            CombatAction first = battle.Damage(
                10,
                ActionOrigin.Additional,
                0.1d,
                1,
                1);
            CombatAction second = battle.Damage(
                11,
                ActionOrigin.Chase,
                0.1d,
                1,
                1);

            queue.EnqueueNextRange(new[] { first, second });

            Assert.That(queue.TryDequeue(out CombatAction action), Is.True);
            Assert.That(action, Is.SameAs(first));
            Assert.That(queue.TryDequeue(out action), Is.True);
            Assert.That(action, Is.SameAs(second));
            Assert.That(queue.TryDequeue(out action), Is.True);
            Assert.That(action, Is.SameAs(root));

            queue.Enqueue(root);
            Assert.Throws<ArgumentException>(() =>
                queue.EnqueueNextRange(new[]
                {
                    battle.Damage(
                        20,
                        ActionOrigin.Additional,
                        0.1d,
                        1,
                        1),
                    root
                }));
            Assert.That(queue.Count, Is.EqualTo(1));
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
                Assert.That(result.ActionResults[index].ExecutionOrder,
                    Is.EqualTo(index + 1));
            }
        }
    }
}
