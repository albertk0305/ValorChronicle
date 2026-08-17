using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Tests.EditMode.Battle.Flow;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Integration
{
    public sealed class BattleFlowCombatBridgeTests
    {
        [Test]
        public void MatchEventsRunRootsDerivedAndSlotsBeforeBoss()
        {
            PartyBattleState party = PartyWithFireSlots();
            BossBattleState boss = Boss(100000, 100d);
            BattleFlowCoordinator coordinator = StartMatchResolution(
                2,
                TwoFireMatchCascade());
            var actionIds = new CombatActionIdSequence();
            var providerCalls = new List<string>();
            var bossCalls = new List<int>();
            var matchProvider = new DelegateMatchEventActionProvider(context =>
            {
                providerCalls.Add(
                    $"E{context.MatchEvent.SequenceIndex}-"
                        + $"S{context.Character.PartySlotIndex}");
                return new CombatAction[]
                {
                    CombatIntegrationTestSupport.MatchDamage(context, 0d)
                };
            });
            var bossProvider = new DelegateBossCombatActionProvider(context =>
            {
                bossCalls.Add(context.CurrentTurn);
                return new CombatAction[]
                {
                    CombatIntegrationTestSupport.BossDamage(context, 0d)
                };
            });
            var trigger = new DelegateIntegrationTriggerRule(context =>
            {
                if (!(context.CompletedAction is DamageAction root)
                    || root.Origin != ActionOrigin.Match
                    || root.ContextRequest.Attacker.PartySlotIndex != 0)
                {
                    return Array.Empty<CombatAction>();
                }

                return new CombatAction[]
                {
                    AdditionalDamage(root, context, actionIds)
                };
            });
            BattleFlowCombatBridge bridge = Bridge(
                coordinator,
                party,
                boss,
                actionIds,
                matchProvider,
                bossProvider,
                trigger);

            Assert.That(
                coordinator.TryBeginNextMatchEvent(
                    out MatchEventExecution first),
                Is.True);
            Assert.That(bridge.ResolveMatchEvent(first), Is.True);
            Assert.That(
                ActionIds(bridge.LastMatchExecutionResult),
                Is.EqualTo(new long[] { 1, 2, 3, 4 }));
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.MatchEventResolving));

            coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution second);
            Assert.That(bridge.ResolveMatchEvent(second), Is.True);
            Assert.That(
                ActionIds(bridge.LastMatchExecutionResult),
                Is.EqualTo(new long[] { 5, 6, 7, 8 }));
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));
            Assert.That(bossCalls, Is.Empty);

            Assert.That(
                bridge.ResolveBossAction(coordinator.Context.CurrentTurn),
                Is.True);

            Assert.That(providerCalls, Is.EqualTo(new[]
            {
                "E0-S0", "E0-S2", "E0-S4",
                "E1-S0", "E1-S2", "E1-S4"
            }));
            Assert.That(bossCalls, Is.EqualTo(new[] { 1 }));
            Assert.That(
                ActionIds(bridge.LastBossExecutionResult),
                Is.EqualTo(new long[] { 9 }));
            Assert.That(actionIds.LastIssuedId, Is.EqualTo(9));
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(coordinator.Context.CurrentTurn, Is.EqualTo(2));
        }

        [Test]
        public void FourEventsUseComboFourThroughDamagePipeline()
        {
            CharacterBattleState character = Character("fire", 0, 100d);
            PartyBattleState party = new PartyBattleState(
                new[] { character });
            BossBattleState boss = Boss(10000, 0d);
            BattleFlowCoordinator coordinator = StartMatchResolution(
                1,
                FourFireMatchCascade());
            var actionIds = new CombatActionIdSequence();
            var observedCounts = new List<int>();
            var multipliers = new List<double>();
            var matchProvider = new DelegateMatchEventActionProvider(context =>
            {
                observedCounts.Add(context.FinalComboCount);
                return new CombatAction[]
                {
                    CombatIntegrationTestSupport.MatchDamage(context, 0.10d)
                };
            });
            BattleFlowCombatBridge bridge = Bridge(
                coordinator,
                party,
                boss,
                actionIds,
                matchProvider,
                EmptyBossProvider());

            for (int eventIndex = 0; eventIndex < 4; eventIndex++)
            {
                coordinator.TryBeginNextMatchEvent(
                    out MatchEventExecution execution);
                Assert.That(bridge.ResolveMatchEvent(execution), Is.True);
                var result = (DamageActionResult)
                    bridge.LastMatchExecutionResult.ActionResults[0];
                multipliers.Add(result.DamageResult.ComboMultiplier);
            }

            Assert.That(observedCounts, Is.EqualTo(new[] { 4, 4, 4, 4 }));
            Assert.That(multipliers, Has.All.EqualTo(1.21d).Within(0.000001d));
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));
        }

        [Test]
        public void CombatActionsAppliedFiresAfterMatchAndBossStateChanges()
        {
            CharacterBattleState character = Character("fire", 0, 100d);
            var party = new PartyBattleState(new[] { character });
            BossBattleState boss = Boss(10000, 100d);
            BattleFlowCoordinator coordinator = StartMatchResolution(
                2,
                SingleFireMatchCascade());
            var actionIds = new CombatActionIdSequence();
            BattleFlowCombatBridge bridge = Bridge(
                coordinator,
                party,
                boss,
                actionIds,
                new DelegateMatchEventActionProvider(context =>
                    new CombatAction[]
                    {
                        CombatIntegrationTestSupport.MatchDamage(context)
                    }),
                new DelegateBossCombatActionProvider(context =>
                    new CombatAction[]
                    {
                        CombatIntegrationTestSupport.BossDamage(context)
                    }));
            var observedBossHp = new List<long>();
            var observedPartyHp = new List<long>();
            bridge.CombatActionsApplied += () =>
            {
                observedBossHp.Add(boss.CurrentHp);
                observedPartyHp.Add(party.CurrentHp);
            };

            coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution);
            Assert.That(bridge.ResolveMatchEvent(execution), Is.True);
            Assert.That(observedBossHp, Has.Count.EqualTo(1));
            Assert.That(observedBossHp[0], Is.LessThan(boss.MaxHp));
            Assert.That(observedPartyHp[0], Is.EqualTo(party.MaxHp));

            Assert.That(bridge.ResolveBossAction(1), Is.True);
            Assert.That(observedBossHp, Has.Count.EqualTo(2));
            Assert.That(observedPartyHp[1], Is.LessThan(party.MaxHp));
        }

        [Test]
        public void CombatActionsAppliedSkipsRejectedAndEmptyRequests()
        {
            CharacterBattleState character = Character("fire", 0, 100d);
            var party = new PartyBattleState(new[] { character });
            BossBattleState boss = Boss(10000, 100d);
            BattleFlowCoordinator coordinator = StartMatchResolution(
                2,
                SingleFireMatchCascade());
            BattleFlowCombatBridge bridge = Bridge(
                coordinator,
                party,
                boss,
                new CombatActionIdSequence(),
                EmptyMatchProvider(),
                EmptyBossProvider());
            int notificationCount = 0;
            bridge.CombatActionsApplied += () => notificationCount++;

            Assert.That(bridge.TryUseActive(0), Is.False);
            Assert.That(bridge.ResolveBossAction(1), Is.False);
            Assert.That(notificationCount, Is.Zero);

            coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution);
            Assert.That(bridge.ResolveMatchEvent(execution), Is.True);
            Assert.That(notificationCount, Is.Zero);
            Assert.That(bridge.ResolveBossAction(1), Is.True);
            Assert.That(notificationCount, Is.Zero);
        }

        [Test]
        public void StagedMatchAppliesOneStepBeforeBatchNotification()
        {
            CharacterBattleState character = Character("fire", 0, 100d);
            var party = new PartyBattleState(new[] { character });
            BossBattleState boss = Boss(10000, 0d);
            boss.Resources.Register("test_resource", 5);
            BattleFlowCoordinator coordinator = StartMatchResolution(
                2,
                SingleFireMatchCascade());
            var actionIds = new CombatActionIdSequence();
            int providerCalls = 0;
            BattleFlowCombatBridge bridge = Bridge(
                coordinator,
                party,
                boss,
                actionIds,
                new DelegateMatchEventActionProvider(context =>
                {
                    providerCalls++;
                    return new CombatAction[]
                    {
                        CombatIntegrationTestSupport.MatchDamage(
                            context,
                            1d),
                        new AddResourceAction(
                            context.ActionIds.Next(),
                            ActionOrigin.Match,
                            context.Boss,
                            "test_resource",
                            1)
                    };
                }),
                EmptyBossProvider());
            int stepCount = 0;
            int batchCount = 0;
            var observedActionIds = new List<long>();
            bridge.CombatActionStepApplied += step =>
            {
                stepCount++;
                observedActionIds.Add(step.ActionStep.Action.ActionId);
            };
            bridge.CombatActionsApplied += () => batchCount++;
            coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution);

            Assert.That(bridge.TryBeginMatchEventCombat(
                execution,
                out bool hasMatchingCharacter), Is.True);
            Assert.That(hasMatchingCharacter, Is.True);
            Assert.That(providerCalls, Is.Zero);

            Assert.That(bridge.TryGetNextMatchCombatAction(
                out CharacterBattleState actingCharacter,
                out CombatAction firstAction), Is.True);
            Assert.That(actingCharacter, Is.SameAs(character));
            Assert.That(firstAction, Is.TypeOf<DamageAction>());
            Assert.That(providerCalls, Is.EqualTo(1));
            Assert.That(boss.CurrentHp, Is.EqualTo(boss.MaxHp));

            Assert.That(bridge.TryApplyNextMatchCombatAction(
                out var firstStep), Is.True);
            Assert.That(firstStep.PartySlotIndex, Is.Zero);
            Assert.That(boss.CurrentHp, Is.LessThan(boss.MaxHp));
            Assert.That(stepCount, Is.EqualTo(1));
            Assert.That(batchCount, Is.Zero);
            Assert.That(boss.Resources.GetAmount("test_resource"), Is.Zero);

            Assert.That(bridge.TryGetNextMatchCombatAction(
                out _,
                out CombatAction secondAction), Is.True);
            Assert.That(secondAction, Is.TypeOf<AddResourceAction>());
            Assert.That(bridge.TryApplyNextMatchCombatAction(out _), Is.True);
            Assert.That(stepCount, Is.EqualTo(2));
            Assert.That(batchCount, Is.Zero);
            Assert.That(boss.Resources.GetAmount("test_resource"),
                Is.EqualTo(1));
            Assert.That(bridge.TryGetNextMatchCombatAction(
                out _,
                out _), Is.False);

            Assert.That(bridge.TryCompleteMatchEventCombat(execution),
                Is.True);
            Assert.That(batchCount, Is.EqualTo(1));
            Assert.That(observedActionIds, Is.EqualTo(new long[] { 1, 2 }));
            Assert.That(bridge.LastMatchExecutionResult.CompletedActionCount,
                Is.EqualTo(2));
        }

        [Test]
        public void StagedMatchReportsNoMatchingCharacterWithoutMutation()
        {
            CharacterBattleState character = Character("fire", 0, 100d);
            var party = new PartyBattleState(new[] { character });
            BossBattleState boss = Boss(10000, 0d);
            BattleFlowCoordinator coordinator = StartMatchResolution(
                2,
                BattleFlowTestSupport.CreateCascade(new[]
                {
                    BattleFlowTestSupport.Match(
                        ElementType.Water,
                        new BoardPosition(0, 0),
                        new BoardPosition(1, 0),
                        new BoardPosition(2, 0))
                }));
            int providerCalls = 0;
            BattleFlowCombatBridge bridge = Bridge(
                coordinator,
                party,
                boss,
                new CombatActionIdSequence(),
                new DelegateMatchEventActionProvider(context =>
                {
                    providerCalls++;
                    return Array.Empty<CombatAction>();
                }),
                EmptyBossProvider());
            int stepCount = 0;
            int batchCount = 0;
            bridge.CombatActionStepApplied += step => stepCount++;
            bridge.CombatActionsApplied += () => batchCount++;
            coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution);

            Assert.That(bridge.TryBeginMatchEventCombat(
                execution,
                out bool hasMatchingCharacter), Is.True);

            Assert.That(hasMatchingCharacter, Is.False);
            Assert.That(bridge.TryGetNextMatchCombatAction(
                out _,
                out _), Is.False);
            Assert.That(bridge.TryApplyNextMatchCombatAction(out _), Is.False);
            Assert.That(bridge.TryCompleteMatchEventCombat(execution),
                Is.True);
            Assert.That(providerCalls, Is.Zero);
            Assert.That(stepCount, Is.Zero);
            Assert.That(batchCount, Is.Zero);
            Assert.That(boss.CurrentHp, Is.EqualTo(boss.MaxHp));
            Assert.That(party.CurrentHp, Is.EqualTo(party.MaxHp));
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));
        }

        [Test]
        public void StagedBossPeeksWithoutMutationAndNotifiesAfterBatch()
        {
            var party = new PartyBattleState(new[]
            {
                Character("fire", 0, 100d)
            });
            BossBattleState boss = Boss(10000, 100d);
            boss.Resources.Register("test_resource", 5);
            BattleFlowCoordinator coordinator = StartMatchResolution(
                2,
                BattleFlowTestSupport.CreateCascade());
            Assert.That(coordinator.TryBeginNextMatchEvent(out _), Is.False);
            var actionIds = new CombatActionIdSequence();
            var bossProvider = new CompletionTrackingBossProvider(context =>
                new CombatAction[]
                {
                    CombatIntegrationTestSupport.BossDamage(context),
                    new AddResourceAction(
                        context.ActionIds.Next(),
                        ActionOrigin.System,
                        context.Boss,
                        "test_resource",
                        1)
                });
            BattleFlowCombatBridge bridge = Bridge(
                coordinator,
                party,
                boss,
                actionIds,
                EmptyMatchProvider(),
                bossProvider);
            int batchCount = 0;
            int stepCount = 0;
            bridge.CombatActionsApplied += () => batchCount++;
            bridge.BossCombatActionStepApplied += step => stepCount++;
            long initialPartyHp = party.CurrentHp;

            Assert.That(bridge.TryBeginBossActionCombat(1), Is.True);
            Assert.That(bridge.TryBeginBossActionCombat(1), Is.False);
            Assert.That(bridge.TryGetNextBossCombatAction(
                out CombatAction firstAction), Is.True);
            Assert.That(firstAction, Is.TypeOf<BossDamageAction>());
            Assert.That(party.CurrentHp, Is.EqualTo(initialPartyHp));
            Assert.That(stepCount, Is.Zero);
            Assert.That(batchCount, Is.Zero);

            Assert.That(bridge.TryApplyNextBossCombatAction(
                out CombatActionExecutionStepResult firstStep), Is.True);
            Assert.That(firstStep.Action, Is.SameAs(firstAction));
            Assert.That(party.CurrentHp, Is.LessThan(initialPartyHp));
            Assert.That(stepCount, Is.EqualTo(1));
            Assert.That(batchCount, Is.Zero);
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));

            Assert.That(bridge.TryGetNextBossCombatAction(
                out CombatAction secondAction), Is.True);
            Assert.That(secondAction, Is.TypeOf<AddResourceAction>());
            Assert.That(boss.Resources.GetAmount("test_resource"), Is.Zero);
            Assert.That(bridge.TryApplyNextBossCombatAction(out _), Is.True);
            Assert.That(boss.Resources.GetAmount("test_resource"),
                Is.EqualTo(1));
            Assert.That(stepCount, Is.EqualTo(2));
            Assert.That(batchCount, Is.Zero);
            Assert.That(bossProvider.CommitCount, Is.Zero);
            Assert.That(bridge.TryGetNextBossCombatAction(out _), Is.False);

            Assert.That(bridge.TryCompleteBossActionCombat(), Is.True);
            Assert.That(batchCount, Is.EqualTo(1));
            Assert.That(bossProvider.CommitCount, Is.EqualTo(1));
            Assert.That(ActionIds(bridge.LastBossExecutionResult),
                Is.EqualTo(new long[] { 1, 2 }));
            Assert.That(actionIds.LastIssuedId, Is.EqualTo(2));
            Assert.That(coordinator.Context.CurrentTurn, Is.EqualTo(2));
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
        }

        [Test]
        public void StagedBossEnqueuesDerivedActionImmediatelyAfterSource()
        {
            var party = new PartyBattleState(new[]
            {
                Character("fire", 0, 100d)
            });
            BossBattleState boss = Boss(10000, 100d);
            boss.Resources.Register("test_resource", 5);
            BattleFlowCoordinator coordinator = StartMatchResolution(
                2,
                BattleFlowTestSupport.CreateCascade());
            Assert.That(coordinator.TryBeginNextMatchEvent(out _), Is.False);
            var actionIds = new CombatActionIdSequence();
            var trigger = new DelegateIntegrationTriggerRule(context =>
            {
                if (!(context.CompletedAction is BossDamageAction))
                {
                    return Array.Empty<CombatAction>();
                }

                return new CombatAction[]
                {
                    new AddResourceAction(
                        actionIds.Next(),
                        ActionOrigin.Additional,
                        boss,
                        "test_resource",
                        1,
                        context.RootActionId,
                        context.ActionId)
                };
            });
            BattleFlowCombatBridge bridge = Bridge(
                coordinator,
                party,
                boss,
                actionIds,
                EmptyMatchProvider(),
                new DelegateBossCombatActionProvider(context =>
                    new CombatAction[]
                    {
                        CombatIntegrationTestSupport.BossDamage(context)
                    }),
                trigger);

            Assert.That(bridge.TryBeginBossActionCombat(1), Is.True);
            Assert.That(bridge.TryGetNextBossCombatAction(
                out CombatAction rootAction), Is.True);
            Assert.That(rootAction.ActionId, Is.EqualTo(1));
            Assert.That(bridge.TryApplyNextBossCombatAction(out _), Is.True);

            Assert.That(bridge.TryGetNextBossCombatAction(
                out CombatAction derivedAction), Is.True);
            Assert.That(derivedAction, Is.TypeOf<AddResourceAction>());
            Assert.That(derivedAction.ActionId, Is.EqualTo(2));
            Assert.That(derivedAction.RootActionId, Is.EqualTo(1));
            Assert.That(derivedAction.SourceActionId, Is.EqualTo(1));
            Assert.That(bridge.TryApplyNextBossCombatAction(out _), Is.True);
            Assert.That(bridge.TryCompleteBossActionCombat(), Is.True);

            Assert.That(boss.Resources.GetAmount("test_resource"),
                Is.EqualTo(1));
            Assert.That(ActionIds(bridge.LastBossExecutionResult),
                Is.EqualTo(new long[] { 1, 2 }));
            Assert.That(actionIds.LastIssuedId, Is.EqualTo(2));
        }

        [Test]
        public void StagedBossTerminalStepClearsRemainingActions()
        {
            var party = new PartyBattleState(new[]
            {
                Character("fire", 0, 100d, hp: 1)
            });
            BossBattleState boss = Boss(10000, 100d);
            boss.Resources.Register("test_resource", 5);
            BattleFlowCoordinator coordinator = StartMatchResolution(
                2,
                BattleFlowTestSupport.CreateCascade());
            Assert.That(coordinator.TryBeginNextMatchEvent(out _), Is.False);
            var actionIds = new CombatActionIdSequence();
            var bossProvider = new CompletionTrackingBossProvider(context =>
                new CombatAction[]
                {
                    CombatIntegrationTestSupport.BossDamage(context),
                    new AddResourceAction(
                        context.ActionIds.Next(),
                        ActionOrigin.System,
                        context.Boss,
                        "test_resource",
                        1)
                });
            BattleFlowCombatBridge bridge = Bridge(
                coordinator,
                party,
                boss,
                actionIds,
                EmptyMatchProvider(),
                bossProvider);

            Assert.That(bridge.TryBeginBossActionCombat(1), Is.True);
            Assert.That(bridge.TryApplyNextBossCombatAction(
                out CombatActionExecutionStepResult terminalStep), Is.True);
            Assert.That(terminalStep.WasTerminalAfterStep, Is.True);
            Assert.That(terminalStep.IsCompleted, Is.True);
            Assert.That(bridge.TryGetNextBossCombatAction(out _), Is.False);
            Assert.That(bridge.TryApplyNextBossCombatAction(out _), Is.False);
            Assert.That(boss.Resources.GetAmount("test_resource"), Is.Zero);

            Assert.That(bridge.TryCompleteBossActionCombat(), Is.True);
            Assert.That(coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.Defeat));
            Assert.That(bridge.LastBossExecutionResult.ActionResults,
                Has.Count.EqualTo(1));
            Assert.That(bridge.LastBossExecutionResult.StoppedEarly,
                Is.True);
            Assert.That(bridge.LastBossExecutionResult
                .ClearedRemainingActions, Is.True);
            Assert.That(boss.Resources.GetAmount("test_resource"), Is.Zero);
            Assert.That(bossProvider.CommitCount, Is.Zero);
        }

        [Test]
        public void BossDefeatStopsCurrentAndRemainingEventsAndSkipsBoss()
        {
            PartyBattleState party = PartyWithFireSlots();
            BossBattleState boss = Boss(50, 100d);
            BattleFlowCoordinator coordinator = StartMatchResolution(
                25,
                TwoFireMatchCascade());
            int bossProviderCalls = 0;
            var actionIds = new CombatActionIdSequence();
            var matchProvider = new DelegateMatchEventActionProvider(context =>
                new CombatAction[]
                {
                    CombatIntegrationTestSupport.MatchDamage(context, 1d)
                });
            var bossProvider = new DelegateBossCombatActionProvider(context =>
            {
                bossProviderCalls++;
                return Array.Empty<CombatAction>();
            });
            BattleFlowCombatBridge bridge = Bridge(
                coordinator,
                party,
                boss,
                actionIds,
                matchProvider,
                bossProvider);
            coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution);

            Assert.That(bridge.ResolveMatchEvent(execution), Is.True);

            Assert.That(coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.Victory));
            Assert.That(coordinator.HasMatchEventInFlight, Is.False);
            Assert.That(coordinator.PendingMatchEventCount, Is.Zero);
            Assert.That(bossProviderCalls, Is.Zero);
            Assert.That(
                bridge.LastMatchExecutionResult.CompletedActionCount,
                Is.EqualTo(1));
            Assert.That(
                bridge.LastMatchExecutionResult.ClearedRemainingActions,
                Is.True);
            Assert.That(
                coordinator.CompleteCurrentMatchEvent(execution.ExecutionId),
                Is.False);
        }

        [Test]
        public void BossDamageIncapacitationEndsBattleWithoutCompletingBoss()
        {
            CharacterBattleState character = Character("fire", 0, 50d, 50);
            PartyBattleState party = new PartyBattleState(
                new[] { character });
            BossBattleState boss = Boss(10000, 100d);
            BattleFlowCoordinator coordinator = StartMatchResolution(
                25,
                BattleFlowTestSupport.CreateCascade());
            Assert.That(
                coordinator.TryBeginNextMatchEvent(out _),
                Is.False);
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));
            var actionIds = new CombatActionIdSequence();
            BattleFlowCombatBridge bridge = Bridge(
                coordinator,
                party,
                boss,
                actionIds,
                EmptyMatchProvider(),
                new DelegateBossCombatActionProvider(context =>
                    new CombatAction[]
                    {
                        CombatIntegrationTestSupport.BossDamage(context, 1d)
                    }));

            Assert.That(bridge.ResolveBossAction(1), Is.True);

            Assert.That(coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.Defeat));
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.Result));
            Assert.That(
                bridge.LastBossExecutionResult.CompletedActionCount,
                Is.EqualTo(1));
            Assert.That(coordinator.CompleteBossAction(), Is.False);
        }

        [Test]
        public void ZeroBossActionsCompleteAndTwentyFifthTurnEndsAfterBoss()
        {
            CharacterBattleState character = Character("fire", 0, 0d);
            PartyBattleState party = new PartyBattleState(
                new[] { character });
            BossBattleState boss = Boss(10000, 0d);
            var coordinator = new BattleFlowCoordinator(25);
            var actionIds = new CombatActionIdSequence();
            int playerCalls = 0;
            int bossCalls = 0;
            var matchProvider = new DelegateMatchEventActionProvider(context =>
            {
                playerCalls++;
                return new CombatAction[]
                {
                    CombatIntegrationTestSupport.MatchDamage(context, 0d)
                };
            });
            var bossProvider = new DelegateBossCombatActionProvider(context =>
            {
                bossCalls++;
                return Array.Empty<CombatAction>();
            });
            BattleFlowCombatBridge bridge = Bridge(
                coordinator,
                party,
                boss,
                actionIds,
                matchProvider,
                bossProvider);
            coordinator.StartBattle();

            for (int turn = 1; turn <= 25; turn++)
            {
                coordinator.TryBeginBoardResolution();
                coordinator.NotifyBoardActionResolved(
                    SingleFireMatchCascade(),
                    true);
                coordinator.TryBeginNextMatchEvent(
                    out MatchEventExecution execution);
                Assert.That(bridge.ResolveMatchEvent(execution), Is.True);
                Assert.That(coordinator.Context.Phase,
                    Is.EqualTo(BattlePhase.BossActing));
                Assert.That(bridge.ResolveBossAction(turn), Is.True);
            }

            Assert.That(playerCalls, Is.EqualTo(25));
            Assert.That(bossCalls, Is.EqualTo(25));
            Assert.That(coordinator.Context.CurrentTurn, Is.EqualTo(25));
            Assert.That(coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.TurnLimitReached));
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.Result));
            Assert.That(new CombatActionIdSequence().Next(), Is.EqualTo(1));
        }

        private static BattleFlowCombatBridge Bridge(
            BattleFlowCoordinator coordinator,
            PartyBattleState party,
            BossBattleState boss,
            CombatActionIdSequence actionIds,
            IMatchEventActionProvider matchProvider,
            IBossCombatActionProvider bossProvider,
            params ICombatTriggerRule[] rules)
        {
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)),
                new CombatTriggerResolver(rules));
            return new BattleFlowCombatBridge(
                coordinator,
                party,
                boss,
                executor,
                matchProvider,
                bossProvider,
                actionIds);
        }

        private static DamageAction AdditionalDamage(
            DamageAction root,
            CombatActionTriggerContext triggerContext,
            CombatActionIdSequence actionIds)
        {
            DamageContextBuildRequest request = root.ContextRequest;
            return new DamageAction(
                actionIds.Next(),
                ActionOrigin.Additional,
                new DamageContextBuildRequest(
                    request.Attacker,
                    request.Party,
                    request.TargetBoss,
                    request.AttackElement,
                    AttackType.Additional,
                    request.AttackTags,
                    0d,
                    false,
                    0,
                    false),
                triggerContext.RootActionId,
                triggerContext.ActionId);
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

        private static IMatchEventActionProvider EmptyMatchProvider()
        {
            return new DelegateMatchEventActionProvider(
                context => Array.Empty<CombatAction>());
        }

        private static IBossCombatActionProvider EmptyBossProvider()
        {
            return new DelegateBossCombatActionProvider(
                context => Array.Empty<CombatAction>());
        }

        private static BattleFlowCoordinator StartMatchResolution(
            int turnLimit,
            BoardCascadeResult cascade)
        {
            var coordinator = new BattleFlowCoordinator(turnLimit);
            coordinator.StartBattle();
            coordinator.TryBeginBoardResolution();
            coordinator.NotifyBoardActionResolved(cascade, true);
            return coordinator;
        }

        private static PartyBattleState PartyWithFireSlots()
        {
            return new PartyBattleState(new[]
            {
                Character("fire_4", 4, 100d),
                Character("fire_0", 0, 100d),
                Character("fire_2", 2, 100d)
            });
        }

        private static CharacterBattleState Character(
            string id,
            int slot,
            double attack,
            long hp = 1000)
        {
            return new CharacterBattleState(
                id,
                slot,
                ElementType.Fire,
                hp,
                attack);
        }

        private static BossBattleState Boss(long hp, double attack)
        {
            return new BossBattleState(
                "boss",
                ElementType.Fire,
                hp,
                attack);
        }

        private static BoardCascadeResult SingleFireMatchCascade()
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

        private static BoardCascadeResult TwoFireMatchCascade()
        {
            return BattleFlowTestSupport.CreateCascade(new[]
            {
                BattleFlowTestSupport.Match(
                    ElementType.Fire,
                    new BoardPosition(0, 0),
                    new BoardPosition(1, 0),
                    new BoardPosition(2, 0)),
                BattleFlowTestSupport.Match(
                    ElementType.Fire,
                    new BoardPosition(0, 1),
                    new BoardPosition(1, 1),
                    new BoardPosition(2, 1),
                    new BoardPosition(3, 1))
            });
        }

        private static BoardCascadeResult FourFireMatchCascade()
        {
            return BattleFlowTestSupport.CreateCascade(new[]
            {
                BattleFlowTestSupport.Match(
                    ElementType.Fire,
                    new BoardPosition(0, 0),
                    new BoardPosition(1, 0),
                    new BoardPosition(2, 0)),
                BattleFlowTestSupport.Match(
                    ElementType.Fire,
                    new BoardPosition(0, 1),
                    new BoardPosition(1, 1),
                    new BoardPosition(2, 1)),
                BattleFlowTestSupport.Match(
                    ElementType.Fire,
                    new BoardPosition(0, 2),
                    new BoardPosition(1, 2),
                    new BoardPosition(2, 2)),
                BattleFlowTestSupport.Match(
                    ElementType.Fire,
                    new BoardPosition(0, 3),
                    new BoardPosition(1, 3),
                    new BoardPosition(2, 3))
            });
        }

        private sealed class CompletionTrackingBossProvider
            : IBossCombatActionProvider,
                IBossCombatActionCompletionHandler
        {
            private readonly Func<BossCombatActionContext,
                IReadOnlyList<CombatAction>> createActions;

            public CompletionTrackingBossProvider(
                Func<BossCombatActionContext,
                    IReadOnlyList<CombatAction>> createActions)
            {
                this.createActions = createActions
                    ?? throw new ArgumentNullException(nameof(createActions));
            }

            public int CommitCount { get; private set; }

            public IReadOnlyList<CombatAction> CreateRootActions(
                BossCombatActionContext context)
            {
                return createActions(context);
            }

            public bool TryCommitCompletedAction()
            {
                CommitCount++;
                return true;
            }
        }
    }
}
