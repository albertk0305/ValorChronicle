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
                null);
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
    }
}
