using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Healing;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Tests.EditMode.Battle.Flow;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Integration
{
    public sealed class PreMatchQueueCombatTests
    {
        [Test]
        public void ProvidersRunInSlotOrderAndTriggersFinishBeforeMatchBegins()
        {
            CharacterBattleState first = Character("first", 0);
            CharacterBattleState second = Character("second", 3);
            var party = new PartyBattleState(new[] { second, first });
            BossBattleState boss = Boss(10000);
            BattleFlowCoordinator coordinator = StartMatchResolution();
            var actionIds = new CombatActionIdSequence();
            var order = new List<string>();
            var firstProvider = new DelegatePreMatchProvider(context =>
            {
                AssertContext(context, first, party, boss, coordinator);
                return new CombatAction[] { Heal(context, ActionOrigin.System) };
            });
            var secondProvider = new DelegatePreMatchProvider(context =>
            {
                AssertContext(context, second, party, boss, coordinator);
                return new CombatAction[] { Heal(context, ActionOrigin.System) };
            });
            var trigger = new DelegateIntegrationTriggerRule(context =>
            {
                if (context.CompletedAction.Origin != ActionOrigin.System)
                {
                    return Array.Empty<CombatAction>();
                }

                var root = (HealAction)context.CompletedAction;
                return new CombatAction[]
                {
                    new HealAction(
                        actionIds.Next(),
                        ActionOrigin.Additional,
                        root.ContextRequest,
                        context.RootActionId,
                        context.ActionId)
                };
            });
            BattleFlowCombatBridge bridge = Bridge(
                coordinator,
                party,
                boss,
                actionIds,
                new ICombatTriggerRule[] { trigger },
                new[]
                {
                    new PreMatchQueueActionProviderBinding(first, firstProvider),
                    new PreMatchQueueActionProviderBinding(second, secondProvider)
                });
            bridge.PreMatchQueueCombatActionStepApplied += step =>
                order.Add($"pre-{step.PartySlotIndex}-{step.ActionStep.Action.Origin}");
            coordinator.MatchEventExecuting += _ => order.Add("match");

            Assert.That(bridge.TryBeginPreMatchQueueCombat(), Is.True);
            while (bridge.TryGetNextPreMatchQueueCombatAction(out _, out _))
            {
                Assert.That(
                    bridge.TryApplyNextPreMatchQueueCombatAction(out _),
                    Is.True);
            }
            Assert.That(bridge.TryCompletePreMatchQueueCombat(), Is.True);
            Assert.That(
                coordinator.TryBeginNextMatchEvent(out _),
                Is.True);

            Assert.That(order, Is.EqualTo(new[]
            {
                "pre-0-System",
                "pre-0-Additional",
                "pre-3-System",
                "pre-3-Additional",
                "match"
            }));
            Assert.That(
                ActionIds(bridge.LastPreMatchQueueExecutionResult),
                Is.EqualTo(new long[] { 1, 3, 2, 4 }));
        }

        [Test]
        public void EmptyProviderSetCompletesWithoutChangingMatchOrder()
        {
            CharacterBattleState character = Character("hero", 0);
            var party = new PartyBattleState(new[] { character });
            BattleFlowCoordinator coordinator = StartMatchResolution();
            BattleFlowCombatBridge bridge = Bridge(
                coordinator,
                party,
                Boss(10000),
                new CombatActionIdSequence(),
                Array.Empty<ICombatTriggerRule>(),
                Array.Empty<PreMatchQueueActionProviderBinding>());

            Assert.That(bridge.TryBeginPreMatchQueueCombat(), Is.True);
            Assert.That(
                bridge.TryGetNextPreMatchQueueCombatAction(out _, out _),
                Is.False);
            Assert.That(bridge.TryCompletePreMatchQueueCombat(), Is.True);
            Assert.That(bridge.LastPreMatchQueueExecutionResult,
                Is.Not.Null);
            Assert.That(
                bridge.LastPreMatchQueueExecutionResult.CompletedActionCount,
                Is.Zero);
            Assert.That(
                coordinator.TryBeginNextMatchEvent(out _),
                Is.True);
        }

        [Test]
        public void SameQueueCannotBeginTwiceButNextQueueCanBegin()
        {
            CharacterBattleState character = Character("hero", 0);
            var party = new PartyBattleState(new[] { character });
            BattleFlowCoordinator coordinator = StartMatchResolution(2);
            int providerCalls = 0;
            var provider = new DelegatePreMatchProvider(context =>
            {
                providerCalls++;
                return Array.Empty<CombatAction>();
            });
            BattleFlowCombatBridge bridge = Bridge(
                coordinator,
                party,
                Boss(10000),
                new CombatActionIdSequence(),
                Array.Empty<ICombatTriggerRule>(),
                new[]
                {
                    new PreMatchQueueActionProviderBinding(character, provider)
                });
            long firstQueueId = coordinator.CurrentMatchQueueId;

            Assert.That(bridge.TryBeginPreMatchQueueCombat(), Is.True);
            Assert.That(bridge.TryBeginPreMatchQueueCombat(), Is.False);
            Assert.That(bridge.TryCompletePreMatchQueueCombat(), Is.True);
            Assert.That(bridge.TryBeginPreMatchQueueCombat(), Is.False);
            coordinator.TryBeginNextMatchEvent(out MatchEventExecution execution);
            coordinator.CompleteCurrentMatchEvent(execution.ExecutionId);
            coordinator.CompleteBossAction();
            coordinator.TryBeginBoardResolution();
            coordinator.NotifyBoardActionResolved(SingleMatchCascade(), true);

            Assert.That(coordinator.CurrentMatchQueueId,
                Is.GreaterThan(firstQueueId));
            Assert.That(bridge.TryBeginPreMatchQueueCombat(), Is.True);
            Assert.That(bridge.TryCompletePreMatchQueueCombat(), Is.True);
            Assert.That(providerCalls, Is.EqualTo(2));
        }

        [Test]
        public void InvalidEmptyAndTerminalQueuesDoNotBegin()
        {
            CharacterBattleState character = Character("hero", 0);
            var party = new PartyBattleState(new[] { character });
            BossBattleState boss = Boss(10000);
            var coordinator = new BattleFlowCoordinator(2);
            BattleFlowCombatBridge bridge = Bridge(
                coordinator,
                party,
                boss,
                new CombatActionIdSequence(),
                Array.Empty<ICombatTriggerRule>(),
                Array.Empty<PreMatchQueueActionProviderBinding>());

            Assert.That(bridge.TryBeginPreMatchQueueCombat(), Is.False);
            coordinator.StartBattle();
            coordinator.TryBeginBoardResolution();
            coordinator.NotifyBoardActionResolved(EmptyCascade(), true);
            Assert.That(coordinator.PendingMatchEventCount, Is.Zero);
            Assert.That(bridge.TryBeginPreMatchQueueCombat(), Is.False);
            coordinator.TryBeginNextMatchEvent(out _);
            coordinator.NotifyBossDefeated();
            Assert.That(bridge.TryBeginPreMatchQueueCombat(), Is.False);
        }

        [Test]
        public void TerminalPreMatchActionStopsRemainingActionsAndEndsBattle()
        {
            CharacterBattleState character = Character("hero", 0);
            var party = new PartyBattleState(new[] { character });
            BossBattleState boss = Boss(1);
            BattleFlowCoordinator coordinator = StartMatchResolution();
            var actionIds = new CombatActionIdSequence();
            var provider = new DelegatePreMatchProvider(context =>
                new CombatAction[]
                {
                    new DamageAction(
                        context.ActionIds.Next(),
                        ActionOrigin.System,
                        new DamageContextBuildRequest(
                            context.Character,
                            context.Party,
                            context.Boss,
                            ElementType.Fire,
                            AttackType.Active,
                            AttackTag.None,
                            1d,
                            false,
                            0,
                            false)),
                    Heal(context, ActionOrigin.System)
                });
            BattleFlowCombatBridge bridge = Bridge(
                coordinator,
                party,
                boss,
                actionIds,
                Array.Empty<ICombatTriggerRule>(),
                new[]
                {
                    new PreMatchQueueActionProviderBinding(character, provider)
                });

            Assert.That(bridge.TryBeginPreMatchQueueCombat(), Is.True);
            Assert.That(
                bridge.TryApplyNextPreMatchQueueCombatAction(out _),
                Is.True);
            Assert.That(
                bridge.TryGetNextPreMatchQueueCombatAction(out _, out _),
                Is.False);
            Assert.That(bridge.TryCompletePreMatchQueueCombat(), Is.True);
            Assert.That(coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.Victory));
            Assert.That(
                bridge.LastPreMatchQueueExecutionResult.StoppedEarly,
                Is.True);
            Assert.That(
                bridge.LastPreMatchQueueExecutionResult.CompletedActionCount,
                Is.EqualTo(1));
        }

        [Test]
        public void NewBattleBridgeDoesNotInheritProcessedQueueState()
        {
            int providerCalls = 0;
            var provider = new DelegatePreMatchProvider(_ =>
            {
                providerCalls++;
                return Array.Empty<CombatAction>();
            });

            for (int battleIndex = 0; battleIndex < 2; battleIndex++)
            {
                CharacterBattleState character = Character("hero", 0);
                var party = new PartyBattleState(new[] { character });
                BattleFlowCoordinator coordinator = StartMatchResolution();
                BattleFlowCombatBridge bridge = Bridge(
                    coordinator,
                    party,
                    Boss(10000),
                    new CombatActionIdSequence(),
                    Array.Empty<ICombatTriggerRule>(),
                    new[]
                    {
                        new PreMatchQueueActionProviderBinding(
                            character,
                            provider)
                    });

                Assert.That(bridge.TryBeginPreMatchQueueCombat(), Is.True);
                Assert.That(bridge.TryCompletePreMatchQueueCombat(), Is.True);
            }

            Assert.That(providerCalls, Is.EqualTo(2));
        }

        private static BattleFlowCombatBridge Bridge(
            BattleFlowCoordinator coordinator,
            PartyBattleState party,
            BossBattleState boss,
            CombatActionIdSequence actionIds,
            IReadOnlyList<ICombatTriggerRule> rules,
            IReadOnlyList<PreMatchQueueActionProviderBinding> bindings)
        {
            return new BattleFlowCombatBridge(
                coordinator,
                party,
                boss,
                new CombatActionExecutor(
                    boss,
                    party,
                    new DamageContextFactory(new SeededRandomSource(1)),
                    new CombatTriggerResolver(rules)),
                new DelegateMatchEventActionProvider(
                    _ => Array.Empty<CombatAction>()),
                new DelegateBossCombatActionProvider(
                    _ => Array.Empty<CombatAction>()),
                actionIds,
                Array.Empty<ActiveAbilityBinding>(),
                new ActiveAbilityActionProviderRegistry(),
                boardMutationSink: null,
                difficultyId: null,
                resultBalance: null,
                boardElementConversionSink: null,
                preMatchQueueBindings: bindings);
        }

        private static HealAction Heal(
            PreMatchQueueActionContext context,
            ActionOrigin origin)
        {
            return new HealAction(
                context.ActionIds.Next(),
                origin,
                new HealingContextBuildRequest(
                    context.Character,
                    context.Party,
                    0d,
                    false,
                    0));
        }

        private static void AssertContext(
            PreMatchQueueActionContext context,
            CharacterBattleState character,
            PartyBattleState party,
            BossBattleState boss,
            BattleFlowCoordinator coordinator)
        {
            Assert.That(context.Character, Is.SameAs(character));
            Assert.That(context.Party, Is.SameAs(party));
            Assert.That(context.Boss, Is.SameAs(boss));
            Assert.That(context.PlayerTurnNumber,
                Is.EqualTo(coordinator.Context.CurrentTurn));
            Assert.That(context.MatchQueueId,
                Is.EqualTo(coordinator.CurrentMatchQueueId));
            Assert.That(context.MatchEvents, Has.Count.EqualTo(1));
        }

        private static long[] ActionIds(CombatActionExecutionResult result)
        {
            var ids = new long[result.ActionResults.Count];
            for (int index = 0; index < ids.Length; index++)
            {
                ids[index] = result.ActionResults[index].Action.ActionId;
            }

            return ids;
        }

        private static BattleFlowCoordinator StartMatchResolution(
            int turnLimit = 2)
        {
            var coordinator = new BattleFlowCoordinator(turnLimit);
            coordinator.StartBattle();
            coordinator.TryBeginBoardResolution();
            coordinator.NotifyBoardActionResolved(SingleMatchCascade(), true);
            return coordinator;
        }

        private static BoardCascadeResult SingleMatchCascade()
        {
            return BattleFlowTestSupport.CreateCascade(new[]
            {
                BattleFlowTestSupport.Match(
                    ElementType.Fire,
                    new BoardPosition(0, 0),
                    new BoardPosition(1, 0),
                    new BoardPosition(2, 0))
            });
        }

        private static BoardCascadeResult EmptyCascade()
        {
            return BattleFlowTestSupport.CreateCascade();
        }

        private static CharacterBattleState Character(string id, int slot)
        {
            return new CharacterBattleState(
                id,
                slot,
                ElementType.Fire,
                1000,
                100d);
        }

        private static BossBattleState Boss(long hp)
        {
            return new BossBattleState(
                "boss",
                ElementType.Grass,
                hp,
                0d);
        }

        private sealed class DelegatePreMatchProvider
            : IPreMatchQueueActionProvider
        {
            private readonly Func<PreMatchQueueActionContext,
                IReadOnlyList<CombatAction>> createActions;

            public DelegatePreMatchProvider(
                Func<PreMatchQueueActionContext,
                    IReadOnlyList<CombatAction>> createActions)
            {
                this.createActions = createActions;
            }

            public IReadOnlyList<CombatAction> CreateRootActions(
                PreMatchQueueActionContext context)
            {
                return createActions(context);
            }
        }
    }
}
