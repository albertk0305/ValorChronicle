using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Flow
{
    public sealed class BattleFlowCoordinatorTests
    {
        [Test]
        public void StartBattle_ProcessesTurnStartAndEntersActiveInput()
        {
            var phases = new List<BattlePhase>();
            var coordinator = new BattleFlowCoordinator(25);
            coordinator.PhaseChanged += phases.Add;

            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.NotStarted));
            Assert.That(coordinator.StartBattle(), Is.True);

            Assert.That(coordinator.Context.CurrentTurn, Is.EqualTo(1));
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.ActiveInput));
            Assert.That(phases, Is.EqualTo(new[]
            {
                BattlePhase.TurnStart,
                BattlePhase.ActiveInput
            }));
        }

        [Test]
        public void NormalTurn_TransitionsInRequiredOrder()
        {
            var phases = new List<BattlePhase>();
            var coordinator = new BattleFlowCoordinator(2);
            coordinator.PhaseChanged += phases.Add;
            coordinator.StartBattle();

            Assert.That(coordinator.CompleteActiveInput(), Is.True);
            Assert.That(coordinator.NotifyBoardActionStarted(), Is.True);
            Assert.That(
                coordinator.NotifyBoardActionResolved(null, true),
                Is.True);
            Assert.That(coordinator.ExecuteRemainingMatchEvents(), Is.Zero);
            Assert.That(coordinator.CompleteBossAction(), Is.True);

            Assert.That(phases, Is.EqualTo(new[]
            {
                BattlePhase.TurnStart,
                BattlePhase.ActiveInput,
                BattlePhase.PuzzleInput,
                BattlePhase.BoardResolving,
                BattlePhase.MatchEventResolving,
                BattlePhase.BossActing,
                BattlePhase.TurnEnd,
                BattlePhase.ResultCheck,
                BattlePhase.TurnStart,
                BattlePhase.ActiveInput
            }));
        }

        [Test]
        public void CommandsOutsideAllowedPhaseAreRejected()
        {
            var coordinator = new BattleFlowCoordinator(25, new[] { 1 });

            Assert.That(coordinator.CompleteActiveInput(), Is.False);
            Assert.That(coordinator.NotifyBoardActionStarted(), Is.False);
            Assert.That(
                coordinator.NotifyBoardActionResolved(null, true),
                Is.False);
            Assert.That(
                coordinator.TryBeginNextMatchEvent(out _),
                Is.False);
            Assert.That(coordinator.CompleteBossAction(), Is.False);
            Assert.That(coordinator.TryUseActiveAbility(0), Is.False);
            Assert.That(coordinator.StartBattle(), Is.True);
            Assert.That(coordinator.StartBattle(), Is.False);
        }

        [Test]
        public void BeginAndComplete_AdvanceOneEventAtATime()
        {
            BoardCascadeResult cascade = CreateTwoMatchCascade();
            var executed = new List<MatchEvent>();
            var coordinator = StartBoardResolution(25);
            coordinator.MatchEventExecuting += executed.Add;
            coordinator.NotifyBoardActionResolved(cascade, true);

            Assert.That(
                coordinator.TryBeginNextMatchEvent(
                    out MatchEventExecution first),
                Is.True);
            Assert.That(first.MatchEvent.SequenceIndex, Is.Zero);
            Assert.That(executed, Has.Count.EqualTo(1));
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.MatchEventResolving));
            Assert.That(coordinator.HasMatchEventInFlight, Is.True);
            Assert.That(coordinator.CurrentMatchEventExecution,
                Is.SameAs(first));

            Assert.That(
                coordinator.TryBeginNextMatchEvent(out _),
                Is.False);
            Assert.That(executed, Has.Count.EqualTo(1));

            Assert.That(
                coordinator.CompleteCurrentMatchEvent(first.ExecutionId),
                Is.True);
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.MatchEventResolving));
            Assert.That(coordinator.HasMatchEventInFlight, Is.False);

            Assert.That(
                coordinator.TryBeginNextMatchEvent(
                    out MatchEventExecution second),
                Is.True);
            Assert.That(second.MatchEvent.SequenceIndex, Is.EqualTo(1));
            Assert.That(executed, Has.Count.EqualTo(2));
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.MatchEventResolving));

            Assert.That(
                coordinator.CompleteCurrentMatchEvent(second.ExecutionId),
                Is.True);
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));
        }

        [Test]
        public void LastMatchBeginsWithoutStartingBossUntilCompletion()
        {
            int matchStartCount = 0;
            int bossStartCount = 0;
            var coordinator = StartBoardResolution(25);
            coordinator.MatchEventExecuting += _ => matchStartCount++;
            coordinator.BossActionStarted += () => bossStartCount++;
            coordinator.NotifyBoardActionResolved(
                BattleFlowTestSupport.CreateCascade(
                    new[]
                    {
                        BattleFlowTestSupport.Match(
                            ElementType.Fire,
                            new BoardPosition(0, 0),
                            new BoardPosition(1, 0),
                            new BoardPosition(2, 0))
                    }),
                true);

            Assert.That(
                coordinator.TryBeginNextMatchEvent(
                    out MatchEventExecution execution),
                Is.True);

            Assert.That(matchStartCount, Is.EqualTo(1));
            Assert.That(bossStartCount, Is.Zero);
            Assert.That(coordinator.PendingMatchEventCount, Is.Zero);
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.MatchEventResolving));

            Assert.That(
                coordinator.CompleteCurrentMatchEvent(execution.ExecutionId),
                Is.True);
            Assert.That(bossStartCount, Is.EqualTo(1));
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));
        }

        [Test]
        public void CompletionRejectsWrongStaleDuplicateAndMissingTokens()
        {
            var coordinator = StartBoardResolution(25);
            coordinator.NotifyBoardActionResolved(
                CreateTwoMatchCascade(),
                true);

            Assert.That(
                coordinator.CompleteCurrentMatchEvent(1),
                Is.False);
            coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution first);

            Assert.That(
                coordinator.CompleteCurrentMatchEvent(
                    first.ExecutionId + 100),
                Is.False);
            Assert.That(coordinator.HasMatchEventInFlight, Is.True);
            Assert.That(
                coordinator.CompleteCurrentMatchEvent(first.ExecutionId),
                Is.True);
            Assert.That(
                coordinator.CompleteCurrentMatchEvent(first.ExecutionId),
                Is.False);

            coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution second);
            Assert.That(
                coordinator.CompleteCurrentMatchEvent(first.ExecutionId),
                Is.False);
            Assert.That(coordinator.HasMatchEventInFlight, Is.True);
            Assert.That(
                coordinator.CompleteCurrentMatchEvent(second.ExecutionId),
                Is.True);
            Assert.That(
                coordinator.CompleteCurrentMatchEvent(second.ExecutionId),
                Is.False);
        }

        [TestCase(BattleResultKind.Victory)]
        [TestCase(BattleResultKind.Defeat)]
        public void TerminalResultCancelsInFlightAndRejectsLateCompletion(
            BattleResultKind result)
        {
            int bossActionCount = 0;
            var coordinator = StartBoardResolution(25);
            coordinator.BossActionStarted += () => bossActionCount++;
            coordinator.NotifyBoardActionResolved(
                CreateTwoMatchCascade(),
                true);
            coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution);

            bool ended = result == BattleResultKind.Victory
                ? coordinator.NotifyBossDefeated()
                : coordinator.NotifyPartyIncapacitated();

            Assert.That(ended, Is.True);
            Assert.That(coordinator.Context.Result, Is.EqualTo(result));
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.Result));
            Assert.That(coordinator.PendingMatchEventCount, Is.Zero);
            Assert.That(coordinator.HasMatchEventInFlight, Is.False);
            Assert.That(bossActionCount, Is.Zero);
            Assert.That(
                coordinator.CompleteCurrentMatchEvent(execution.ExecutionId),
                Is.False);
        }

        [Test]
        public void TwentyFiveTurnsCompleteOnlyAfterFinalBossAction()
        {
            int bossActionCount = 0;
            int turnTwentyFiveMatchCount = 0;
            var coordinator = new BattleFlowCoordinator(25);
            coordinator.BossActionStarted += () => bossActionCount++;
            coordinator.MatchEventExecuting += matchEvent =>
            {
                if (coordinator.Context.CurrentTurn == 25)
                {
                    turnTwentyFiveMatchCount++;
                }
            };
            coordinator.StartBattle();

            for (int turn = 1; turn <= 24; turn++)
            {
                BattleFlowTestSupport.CompleteTurnWithoutMatchEvents(
                    coordinator);
            }

            Assert.That(coordinator.Context.CurrentTurn, Is.EqualTo(25));
            Assert.That(coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.None));
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.ActiveInput));

            coordinator.CompleteActiveInput();
            coordinator.NotifyBoardActionStarted();
            coordinator.NotifyBoardActionResolved(
                BattleFlowTestSupport.CreateCascade(
                    new[]
                    {
                        BattleFlowTestSupport.Match(
                            ElementType.Fire,
                            new BoardPosition(0, 0),
                            new BoardPosition(1, 0),
                            new BoardPosition(2, 0))
                    }),
                true);
            Assert.That(coordinator.ExecuteRemainingMatchEvents(),
                Is.EqualTo(1));
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));
            Assert.That(coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.None));

            coordinator.CompleteBossAction();

            Assert.That(turnTwentyFiveMatchCount, Is.EqualTo(1));
            Assert.That(bossActionCount, Is.EqualTo(25));
            Assert.That(coordinator.Context.CurrentTurn, Is.EqualTo(25));
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.Result));
            Assert.That(coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.TurnLimitReached));
        }

        [Test]
        public void BossDefeatDuringFirstMatchClearsQueueAndSkipsBossAction()
        {
            int executedCount = 0;
            int bossActionCount = 0;
            int resultCount = 0;
            var coordinator = StartBoardResolution(25);
            coordinator.MatchEventExecuting += matchEvent =>
            {
                executedCount++;
                coordinator.NotifyBossDefeated();
            };
            coordinator.BossActionStarted += () => bossActionCount++;
            coordinator.ResultReached += result => resultCount++;
            coordinator.NotifyBoardActionResolved(
                CreateTwoMatchCascade(),
                true);

            Assert.That(coordinator.ExecuteRemainingMatchEvents(),
                Is.EqualTo(1));

            Assert.That(executedCount, Is.EqualTo(1));
            Assert.That(bossActionCount, Is.Zero);
            Assert.That(coordinator.PendingMatchEventCount, Is.Zero);
            Assert.That(coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.Victory));
            Assert.That(resultCount, Is.EqualTo(1));
            Assert.That(coordinator.NotifyBossDefeated(), Is.False);
            Assert.That(coordinator.NotifyPartyIncapacitated(), Is.False);
            Assert.That(resultCount, Is.EqualTo(1));
        }

        [Test]
        public void PartyIncapacitatedEndsBattleAndCommandsAreRejected()
        {
            int bossActionCount = 0;
            int resultCount = 0;
            var coordinator = StartBoardResolution(25);
            coordinator.BossActionStarted += () => bossActionCount++;
            coordinator.ResultReached += result => resultCount++;
            coordinator.NotifyBoardActionResolved(
                CreateTwoMatchCascade(),
                true);
            Assert.That(coordinator.PendingMatchEventCount, Is.EqualTo(2));

            Assert.That(coordinator.NotifyPartyIncapacitated(), Is.True);

            Assert.That(coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.Defeat));
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.Result));
            Assert.That(coordinator.PendingMatchEventCount, Is.Zero);
            Assert.That(bossActionCount, Is.Zero);
            Assert.That(resultCount, Is.EqualTo(1));
            Assert.That(coordinator.CompleteActiveInput(), Is.False);
            Assert.That(coordinator.NotifyBoardActionStarted(), Is.False);
            Assert.That(coordinator.CompleteBossAction(), Is.False);
            Assert.That(coordinator.AbortBattle(), Is.False);
        }

        [Test]
        public void AbortClearsPendingQueueAndNotifiesOnce()
        {
            int resultCount = 0;
            var coordinator = StartBoardResolution(25);
            coordinator.ResultReached += result => resultCount++;
            coordinator.NotifyBoardActionResolved(
                CreateTwoMatchCascade(),
                true);
            Assert.That(coordinator.PendingMatchEventCount, Is.EqualTo(2));

            Assert.That(coordinator.AbortBattle(), Is.True);
            Assert.That(coordinator.AbortBattle(), Is.False);

            Assert.That(coordinator.PendingMatchEventCount, Is.Zero);
            Assert.That(coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.Aborted));
            Assert.That(resultCount, Is.EqualTo(1));
        }

        private static BattleFlowCoordinator StartBoardResolution(
            int turnLimit)
        {
            var coordinator = new BattleFlowCoordinator(turnLimit);
            coordinator.StartBattle();
            coordinator.CompleteActiveInput();
            coordinator.NotifyBoardActionStarted();
            return coordinator;
        }

        private static BoardCascadeResult CreateTwoMatchCascade()
        {
            return BattleFlowTestSupport.CreateCascade(
                new[]
                {
                    BattleFlowTestSupport.Match(
                        ElementType.Fire,
                        new BoardPosition(0, 0),
                        new BoardPosition(1, 0),
                        new BoardPosition(2, 0)),
                    BattleFlowTestSupport.Match(
                        ElementType.Water,
                        new BoardPosition(3, 1),
                        new BoardPosition(4, 1),
                        new BoardPosition(5, 1))
                });
        }
    }
}
