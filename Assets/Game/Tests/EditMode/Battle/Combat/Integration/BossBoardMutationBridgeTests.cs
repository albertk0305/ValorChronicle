using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Integration
{
    public sealed class BossBoardMutationBridgeTests
    {
        [Test]
        public void CompletionIsRequiredBeforeTurnEndCommitAndNextTurn()
        {
            Harness battle = CreateBattle(partyHp: 1000);

            Assert.That(battle.Bridge.ResolveBossAction(1), Is.True);

            Assert.That(battle.Sink.RequestCount, Is.EqualTo(1));
            Assert.That(battle.Party.CurrentHp, Is.LessThan(1000));
            Assert.That(battle.Bridge.HasPendingBossBoardMutation, Is.True);
            Assert.That(battle.Provider.CommitCount, Is.Zero);
            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.Zero);
            Assert.That(battle.Coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));
            Assert.That(battle.Bridge.ResolveBossAction(1), Is.False);

            BattleBoardMutationCompletion completion = Success(
                battle.Sink.LastCommand,
                existingRockCount: 0);
            battle.Sink.Complete(completion);

            Assert.That(battle.Bridge.HasPendingBossBoardMutation, Is.False);
            Assert.That(battle.Provider.CommitCount, Is.EqualTo(1));
            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.EqualTo(1));
            Assert.That(battle.Coordinator.Context.CurrentTurn, Is.EqualTo(2));
            Assert.That(battle.Coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.ActiveInput));
        }

        [Test]
        public void LethalDamageSkipsBoardMutationAndCommit()
        {
            Harness battle = CreateBattle(partyHp: 1);

            Assert.That(battle.Bridge.ResolveBossAction(1), Is.True);

            Assert.That(battle.Party.IsIncapacitated, Is.True);
            Assert.That(battle.Sink.RequestCount, Is.Zero);
            Assert.That(battle.Provider.CommitCount, Is.Zero);
            Assert.That(battle.Coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.Defeat));
        }

        [Test]
        public void MutationFailureAbortsWithoutTurnEndOrCommit()
        {
            Harness battle = CreateBattle(partyHp: 1000);
            battle.Bridge.ResolveBossAction(1);
            LogAssert.Expect(
                LogType.Error,
                "[BattleFlowCombatBridge] Boss Board mutation failed; "
                    + "the battle will be aborted without committing "
                    + "the boss pattern.");

            battle.Sink.Complete(new BattleBoardMutationCompletion(
                battle.Sink.LastCommand,
                result: null,
                status: BattleBoardMutationCompletionStatus.Failed));

            Assert.That(battle.Provider.CommitCount, Is.Zero);
            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.Zero);
            Assert.That(battle.Coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.Aborted));
            Assert.That(battle.Coordinator.Context.CurrentTurn, Is.EqualTo(1));
        }

        [Test]
        public void MaximumReachedCreatedZeroStillCompletesNormally()
        {
            Harness battle = CreateBattle(partyHp: 1000);
            battle.Bridge.ResolveBossAction(1);
            BattleBoardMutationCompletion completion = Success(
                battle.Sink.LastCommand,
                existingRockCount: 6);

            Assert.That(completion.Result.CreatedCount, Is.Zero);
            battle.Sink.Complete(completion);

            Assert.That(battle.Provider.CommitCount, Is.EqualTo(1));
            Assert.That(battle.Coordinator.Context.CurrentTurn, Is.EqualTo(2));
            Assert.That(battle.Coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.None));
        }

        [Test]
        public void DuplicateCompletionIsIgnoredWithoutDoubleCommit()
        {
            Harness battle = CreateBattle(partyHp: 1000);
            battle.Bridge.ResolveBossAction(1);
            BattleBoardMutationCompletion completion = Success(
                battle.Sink.LastCommand,
                existingRockCount: 0);
            battle.Sink.Complete(completion);
            LogAssert.Expect(
                LogType.Warning,
                "[BattleFlowCombatBridge] Ignored duplicate or stale "
                    + "boss Board mutation completion.");

            battle.Sink.RepeatCompletion(completion);

            Assert.That(battle.Provider.CommitCount, Is.EqualTo(1));
            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.EqualTo(1));
            Assert.That(battle.Coordinator.Context.CurrentTurn,
                Is.EqualTo(2));
        }

        [Test]
        public void RejectedMutationAbortsWithoutCommit()
        {
            Harness battle = CreateBattle(partyHp: 1000);
            battle.Sink.AcceptRequests = false;
            LogAssert.Expect(
                LogType.Error,
                "[BattleFlowCombatBridge] Boss Board mutation request "
                    + "was rejected.");

            Assert.That(battle.Bridge.ResolveBossAction(1), Is.False);

            Assert.That(battle.Provider.CommitCount, Is.Zero);
            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.Zero);
            Assert.That(battle.Coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.Aborted));
        }

        [Test]
        public void ThrowingMutationSinkAbortsWithoutCommit()
        {
            Harness battle = CreateBattle(partyHp: 1000);
            battle.Sink.RequestException =
                new InvalidOperationException("Injected sink failure.");
            LogAssert.Expect(
                LogType.Exception,
                new Regex("Injected sink failure\\."));
            LogAssert.Expect(
                LogType.Error,
                "[BattleFlowCombatBridge] Boss Board mutation request "
                    + "threw an exception.");

            Assert.That(battle.Bridge.ResolveBossAction(1), Is.False);

            Assert.That(battle.Provider.CommitCount, Is.Zero);
            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.Zero);
            Assert.That(battle.Coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.Aborted));
        }

        [Test]
        public void TerminalBeforeCallbackIgnoresCompletionWithoutCommit()
        {
            Harness battle = CreateBattle(partyHp: 1000);
            Assert.That(battle.Bridge.ResolveBossAction(1), Is.True);
            Assert.That(battle.Coordinator.AbortBattle(), Is.True);
            LogAssert.Expect(
                LogType.Warning,
                "[BattleFlowCombatBridge] Ignored boss Board mutation "
                    + "completion outside its originating boss action.");

            battle.Sink.Complete(Success(
                battle.Sink.LastCommand,
                existingRockCount: 0));

            Assert.That(battle.Provider.CommitCount, Is.Zero);
            Assert.That(battle.Bridge.ProcessedTurnEndCount, Is.Zero);
            Assert.That(battle.Coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.Aborted));
        }

        private static Harness CreateBattle(long partyHp)
        {
            var party = new PartyBattleState(new[]
            {
                new CharacterBattleState(
                    "test",
                    0,
                    ElementType.Water,
                    partyHp,
                    1d)
            });
            var boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                10000,
                100d);
            var coordinator = new BattleFlowCoordinator(10);
            var provider = new PlanProvider();
            var sink = new RecordingBoardMutationSink();
            var actionIds = new CombatActionIdSequence();
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)));
            var bridge = new BattleFlowCombatBridge(
                coordinator,
                party,
                boss,
                executor,
                new DelegateMatchEventActionProvider(
                    context => Array.Empty<CombatAction>()),
                provider,
                actionIds,
                sink);
            coordinator.StartBattle();
            coordinator.CompleteActiveInput();
            coordinator.NotifyBoardActionStarted();
            coordinator.NotifyBoardActionResolved(null, true);
            coordinator.TryBeginNextMatchEvent(out _);
            return new Harness(
                coordinator,
                party,
                provider,
                sink,
                bridge);
        }

        private static BattleBoardMutationCompletion Success(
            BattleBoardMutationCommand command,
            int existingRockCount)
        {
            BoardState board = CreatePlayableBoard();
            for (int index = 0; index < existingRockCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                board.Set(position, new BoardBlock(
                    board.Get(position).RuntimeId,
                    BoardBlockType.Rock,
                    null));
            }

            var analyzer = new BoardMoveAnalyzer();
            var random = new SeededRandomSource(1701);
            var resolver = new BoardRockMutationResolver(
                random,
                new BoardBlockIdGenerator(1000),
                analyzer,
                new BoardShuffler(random, analyzer));
            BoardRockMutationResult result = resolver.Resolve(
                board,
                command.RequestedCount,
                command.MaximumCount);
            return new BattleBoardMutationCompletion(
                command,
                result,
                BattleBoardMutationCompletionStatus.Completed);
        }

        private static BoardState CreatePlayableBoard()
        {
            int[] elements =
            {
                0, 0, 1, 4, 0,
                4, 0, 4, 3, 4,
                1, 3, 0, 3, 1,
                0, 4, 1, 1, 4,
                0, 0, 2, 4, 4,
                1, 2, 4, 0, 0
            };
            var board = new BoardState();
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    board.Set(position, new BoardBlock(
                        position.ToIndex() + 1,
                        BoardBlockType.Normal,
                        (ElementType)elements[
                            (x * BoardConstants.Height) + y]));
                }
            }

            return board;
        }

        private sealed class PlanProvider
            : IBossCombatActionPlanProvider,
                IBossCombatActionCompletionHandler
        {
            public int CommitCount { get; private set; }

            public IReadOnlyList<CombatAction> CreateRootActions(
                BossCombatActionContext context)
            {
                return CreatePlan(context).CombatActions;
            }

            public BossActionPlan CreatePlan(
                BossCombatActionContext context)
            {
                var action = new BossDamageAction(
                    context.ActionIds.Next(),
                    new BossDamageContextBuildRequest(
                        context.Boss,
                        context.Party,
                        0.65d,
                        AttackTag.None));
                return new BossActionPlan(
                    new CombatAction[] { action },
                    new BattleBoardMutationCommand(
                        BattleBoardMutationKind.CreateRock,
                        3,
                        6));
            }

            public bool TryCommitCompletedAction()
            {
                CommitCount++;
                return true;
            }
        }

        private sealed class Harness
        {
            public Harness(
                BattleFlowCoordinator coordinator,
                PartyBattleState party,
                PlanProvider provider,
                RecordingBoardMutationSink sink,
                BattleFlowCombatBridge bridge)
            {
                Coordinator = coordinator;
                Party = party;
                Provider = provider;
                Sink = sink;
                Bridge = bridge;
            }

            public BattleFlowCoordinator Coordinator { get; }
            public PartyBattleState Party { get; }
            public PlanProvider Provider { get; }
            public RecordingBoardMutationSink Sink { get; }
            public BattleFlowCombatBridge Bridge { get; }
        }
    }
}
