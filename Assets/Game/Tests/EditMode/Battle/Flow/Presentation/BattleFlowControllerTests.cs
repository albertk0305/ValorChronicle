using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Flow.Presentation
{
    public sealed class BattleFlowControllerTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();
        private GameObject root;
        private BattleBoardController boardController;
        private BattleFlowController flowController;

        [UnitySetUp]
        public IEnumerator EnterRuntimeMode()
        {
            yield return new EnterPlayMode();
        }

        [UnityTearDown]
        public IEnumerator ExitRuntimeMode()
        {
            yield return new ExitPlayMode();
        }

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("BattleFlowControllerTests");
            root.SetActive(false);
            createdObjects.Add(root);
            boardController = root.AddComponent<BattleBoardController>();
            boardController.enabled = false;
            flowController = root.AddComponent<BattleFlowController>();
            SetField(flowController, "boardController", boardController);
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void SetupCopiesCooldownsAndRejectsInvalidValues()
        {
            var cooldowns = new[] { 3, 8 };
            var setup = new BattleFlowSetup(25, cooldowns);
            cooldowns[0] = 99;

            Assert.That(setup.TurnLimit, Is.EqualTo(25));
            Assert.That(setup.ActiveAbilityCooldowns,
                Is.EqualTo(new[] { 3, 8 }));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BattleFlowSetup(0));
            Assert.Throws<ArgumentNullException>(
                () => new BattleFlowSetup(25, null));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BattleFlowSetup(25, new[] { -1 }));
        }

        [Test]
        public void InitializationWaitsForBoardAndCatchesMissedReadyOnce()
        {
            Assert.That(boardController.HasInitialBoardReady, Is.False);
            InitializeFlow(new BattleFlowSetup(25));
            ActivateFixture();

            Assert.That(GetField<bool>(flowController, "connectionEnabled"),
                Is.True);
            Assert.That(GetField<bool>(flowController, "boardEventsSubscribed"),
                Is.True);
            Assert.That(boardController.IsExternalInputEnabled, Is.False);
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.NotStarted));

            PublishInitialBoardReady();

            Assert.That(flowController.Context.CurrentTurn, Is.EqualTo(1));
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.ActiveInput));
            Assert.That(boardController.IsExternalInputEnabled, Is.False);

            PublishInitialBoardReady();
            Assert.That(flowController.Context.CurrentTurn, Is.EqualTo(1));
        }

        [Test]
        public void LateInitializationStartsFromReadyStateAndDoesNotRestart()
        {
            SetField(boardController, "initialBoardReadyPublished", true);
            InitializeFlow(new BattleFlowSetup(25));
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.NotStarted));
            ActivateFixture();

            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.ActiveInput));
            Assert.Throws<InvalidOperationException>(() =>
                flowController.Initialize(new BattleFlowSetup(25)));

            DeactivateFixture();
            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Aborted));
            InvokePrivate(flowController, "HandleInitialBoardReady");
            Assert.That(flowController.Context.CurrentTurn, Is.EqualTo(1));
        }

        [Test]
        public void ActiveInputDelegatesAndEnablesOnlyPuzzleInput()
        {
            InitializeStarted(new[] { 3 });

            Assert.That(flowController.Context.ActiveAbilities, Has.Count.EqualTo(1));
            Assert.That(flowController.TryUseActive(0), Is.True);
            Assert.That(flowController.Context.ActiveAbilities[0]
                .RemainingCooldown, Is.EqualTo(3));
            Assert.That(boardController.IsExternalInputEnabled, Is.False);

            Assert.That(flowController.CompleteActiveInput(), Is.True);
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.PuzzleInput));
            Assert.That(boardController.IsExternalInputEnabled, Is.True);
            Assert.That(boardController.CanAcceptBoardInput, Is.False);
            Assert.That(flowController.CompleteActiveInput(), Is.False);
        }

        [Test]
        public void EmptyActiveListAndNoBoardEventsRemainInPuzzleInput()
        {
            InitializeStarted(Array.Empty<int>());

            Assert.That(flowController.Context.ActiveAbilities, Is.Empty);
            Assert.That(flowController.CompleteActiveInput(), Is.True);
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.PuzzleInput));
            Assert.That(flowController.Context.CurrentTurn, Is.EqualTo(1));
            Assert.That(boardController.IsExternalInputEnabled, Is.True);
        }

        [Test]
        public void BoardActionValidatesPhaseAndActionId()
        {
            InitializeStarted(Array.Empty<int>());
            BoardSwapActionResult result = CreateNoMatchResult();
            BoardActionExecution invalidPhase = CreateExecution(1, result);
            LogAssert.Expect(
                LogType.Error,
                new Regex("Board action started in an invalid phase"));

            InvokePrivate(
                flowController,
                "HandleBoardActionStarted",
                invalidPhase);
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.ActiveInput));

            flowController.CompleteActiveInput();
            InvokePrivate(
                flowController,
                "HandleBoardActionStarted",
                CreateExecution(2, result));
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.BoardResolving));
            Assert.That(boardController.IsExternalInputEnabled, Is.False);

            LogAssert.Expect(
                LogType.Warning,
                new Regex("Ignored unexpected board completion"));
            InvokePrivate(
                flowController,
                "HandleBoardActionFinished",
                CreateCompletion(
                    1,
                    result,
                    BoardActionCompletionStatus.Completed));
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.BoardResolving));
        }

        [Test]
        public void NoMatchReturnsToPuzzleWithoutConsumingTurnOrActiveUse()
        {
            InitializeStarted(new[] { 4 });
            flowController.TryUseActive(0);
            flowController.CompleteActiveInput();
            ActiveAbilityRuntimeState active =
                flowController.Context.ActiveAbilities[0];
            BoardSwapActionResult result = CreateNoMatchResult();

            SendCompletedAction(1, result);

            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.PuzzleInput));
            Assert.That(flowController.Context.CurrentTurn, Is.EqualTo(1));
            Assert.That(active.RemainingCooldown, Is.EqualTo(4));
            Assert.That(active.UsedThisTurn, Is.True);
            Assert.That(flowController.Coordinator.PendingMatchEventCount,
                Is.Zero);
            Assert.That(boardController.IsExternalInputEnabled, Is.True);
        }

        [UnityTest]
        public IEnumerator ResolvedActionExecutesMatchesInOrderThenBossAndNextTurn()
        {
            InitializeStarted(Array.Empty<int>());
            flowController.CompleteActiveInput();
            BoardCascadeResult cascade = CreateTwoMatchCascade();
            BoardSwapActionResult result = CreateResolvedResult(cascade);
            var executed = new List<MatchEvent>();
            int bossActionCount = 0;
            flowController.Coordinator.MatchEventExecuting += executed.Add;
            flowController.Coordinator.BossActionStarted +=
                () => bossActionCount++;

            SendCompletedAction(1, result);

            Assert.That(boardController.IsExternalInputEnabled, Is.False);
            Assert.That(executed, Has.Count.EqualTo(1));
            Assert.That(flowController.Coordinator.HasMatchEventInFlight,
                Is.True);
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.MatchEventResolving));
            Assert.That(bossActionCount, Is.Zero);

            yield return WaitUntil(
                () => executed.Count == 2
                    && flowController.Context.Phase
                        == BattlePhase.BossActing,
                "The second MatchEvent and BossActing did not begin.");
            Assert.That(executed, Has.Count.EqualTo(2));
            Assert.That(executed[0].SequenceIndex, Is.Zero);
            Assert.That(executed[1].SequenceIndex, Is.EqualTo(1));
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));
            Assert.That(bossActionCount, Is.EqualTo(1));

            yield return WaitUntil(
                () => flowController.Context.CurrentTurn == 2
                    && flowController.Context.Phase
                        == BattlePhase.ActiveInput,
                "The placeholder Boss action did not complete the turn.");

            Assert.That(flowController.Context.CurrentTurn, Is.EqualTo(2));
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.ActiveInput));
            Assert.That(boardController.IsExternalInputEnabled, Is.False);
        }

        [UnityTest]
        public IEnumerator TwentyFiveTurnsFinishAfterFinalMatchAndBoss()
        {
            InitializeStarted(Array.Empty<int>());
            int matchCount = 0;
            int finalTurnMatchCount = 0;
            int bossCount = 0;
            flowController.Coordinator.MatchEventExecuting += matchEvent =>
            {
                matchCount++;
                if (flowController.Context.CurrentTurn == 25)
                {
                    finalTurnMatchCount++;
                }
            };
            flowController.Coordinator.BossActionStarted += () => bossCount++;

            long actionId = 1;
            for (int turn = 1; turn <= 25; turn++)
            {
                Assert.That(flowController.CompleteActiveInput(), Is.True);
                SendCompletedAction(
                    actionId++,
                    CreateResolvedResult(CreateSingleMatchCascade()));

                yield return WaitUntil(
                    () => flowController.Context.Result
                            != BattleResultKind.None
                        || flowController.Context.Phase
                            == BattlePhase.ActiveInput,
                    $"Turn {turn} did not finish within the frame limit.");

                if (turn < 25)
                {
                    Assert.That(flowController.Context.CurrentTurn,
                        Is.EqualTo(turn + 1));
                    Assert.That(flowController.Context.Result,
                        Is.EqualTo(BattleResultKind.None));
                }
            }

            Assert.That(matchCount, Is.EqualTo(25));
            Assert.That(finalTurnMatchCount, Is.EqualTo(1));
            Assert.That(bossCount, Is.EqualTo(25));
            Assert.That(flowController.Context.CurrentTurn, Is.EqualTo(25));
            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.TurnLimitReached));
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.Result));
            Assert.That(boardController.IsExternalInputEnabled, Is.False);
        }

        [UnityTest]
        public IEnumerator BossDefeatDuringFirstMatchStopsRemainingMatchesAndBoss()
        {
            InitializeStarted(Array.Empty<int>());
            flowController.CompleteActiveInput();
            int matchCount = 0;
            int bossCount = 0;
            int resultCount = 0;
            long interruptedExecutionId = 0;
            flowController.Coordinator.MatchEventExecuting += matchEvent =>
            {
                matchCount++;
                interruptedExecutionId = flowController.Coordinator
                    .CurrentMatchEventExecution.ExecutionId;
                flowController.NotifyBossDefeated();
            };
            flowController.Coordinator.BossActionStarted += () => bossCount++;
            flowController.Coordinator.ResultReached += result => resultCount++;

            SendCompletedAction(
                1,
                CreateResolvedResult(CreateTwoMatchCascade()));

            yield return WaitUntil(
                () => flowController.Context.Result
                    == BattleResultKind.Victory,
                "Boss defeat did not end the battle.");

            Assert.That(matchCount, Is.EqualTo(1));
            Assert.That(bossCount, Is.Zero);
            Assert.That(resultCount, Is.EqualTo(1));
            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Victory));
            Assert.That(flowController.Coordinator.PendingMatchEventCount,
                Is.Zero);
            Assert.That(flowController.Coordinator.HasMatchEventInFlight,
                Is.False);
            Assert.That(
                flowController.Coordinator.CompleteCurrentMatchEvent(
                    interruptedExecutionId),
                Is.False);
            Assert.That(boardController.IsExternalInputEnabled, Is.False);

            InvokePrivate(
                flowController,
                "HandleBoardActionFinished",
                CreateCompletion(
                    1,
                    CreateNoMatchResult(),
                    BoardActionCompletionStatus.Interrupted));
            Assert.That(resultCount, Is.EqualTo(1));
        }

        [Test]
        public void PartyDefeatAndAbortAreTerminalAndNotifyOnce()
        {
            InitializeStarted(Array.Empty<int>());
            int resultCount = 0;
            flowController.Coordinator.ResultReached += result => resultCount++;

            Assert.That(flowController.NotifyPartyDefeated(), Is.True);
            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Defeat));
            Assert.That(flowController.AbortBattle(), Is.False);
            Assert.That(resultCount, Is.EqualTo(1));
            Assert.That(boardController.IsExternalInputEnabled, Is.False);
        }

        [Test]
        public void FailedBoardCompletionAbortsAndPreservesFailureLog()
        {
            InitializeStarted(Array.Empty<int>());
            flowController.CompleteActiveInput();
            BoardSwapActionResult result = CreateNoMatchResult();
            InvokePrivate(
                flowController,
                "HandleBoardActionStarted",
                CreateExecution(1, result));
            var failure = new InvalidOperationException(
                "Injected connected presentation failure.");
            LogAssert.Expect(
                LogType.Exception,
                new Regex("Injected connected presentation failure\\."));
            LogAssert.Expect(
                LogType.Error,
                "[BattleFlow] Board presentation failed. ActionId=1.");

            InvokePrivate(
                flowController,
                "HandleBoardActionFinished",
                CreateCompletion(
                    1,
                    result,
                    BoardActionCompletionStatus.Failed,
                    failure));

            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Aborted));
            Assert.That(flowController.Coordinator.PendingMatchEventCount,
                Is.Zero);
            Assert.That(boardController.IsExternalInputEnabled, Is.False);
        }

        [Test]
        public void InterruptedBoardCompletionAbortsDuringBattle()
        {
            InitializeStarted(Array.Empty<int>());
            flowController.CompleteActiveInput();
            BoardSwapActionResult result = CreateNoMatchResult();
            InvokePrivate(
                flowController,
                "HandleBoardActionStarted",
                CreateExecution(1, result));

            LogAssert.Expect(
                LogType.Warning,
                new Regex("Board presentation was interrupted"));
            InvokePrivate(
                flowController,
                "HandleBoardActionFinished",
                CreateCompletion(
                    1,
                    result,
                    BoardActionCompletionStatus.Interrupted));

            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Aborted));
            Assert.That(boardController.IsExternalInputEnabled, Is.False);
        }

        [Test]
        public void InterruptedAndOnDisableAbortExactlyOnceAndUnsubscribe()
        {
            InitializeStarted(Array.Empty<int>());
            flowController.CompleteActiveInput();
            BoardSwapActionResult result = CreateNoMatchResult();
            InvokePrivate(
                flowController,
                "HandleBoardActionStarted",
                CreateExecution(1, result));
            int resultCount = 0;
            flowController.Coordinator.ResultReached += value => resultCount++;

            DeactivateFixture();
            InvokePrivate(
                flowController,
                "HandleBoardActionFinished",
                CreateCompletion(
                    1,
                    result,
                    BoardActionCompletionStatus.Interrupted));

            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Aborted));
            Assert.That(resultCount, Is.EqualTo(1));
            Assert.That(boardController.IsExternalInputEnabled, Is.False);
            Assert.That(GetField(flowController, "boardEventsSubscribed"),
                Is.False);
        }

        private void InitializeStarted(IReadOnlyList<int> cooldowns)
        {
            SetField(boardController, "initialBoardReadyPublished", true);
            Assert.That(boardController.HasInitialBoardReady, Is.True);
            InitializeFlow(new BattleFlowSetup(25, cooldowns));
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.NotStarted));
            ActivateFixture();
            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.ActiveInput));
        }

        private void InitializeFlow(BattleFlowSetup setup)
        {
            flowController.Initialize(setup);
            Assert.That(flowController.Context, Is.Not.Null);
        }

        private void ActivateFixture()
        {
            root.SetActive(true);
            Assert.That(flowController.isActiveAndEnabled, Is.True);
            if (!GetField<bool>(flowController, "connectionEnabled"))
            {
                InvokePrivate(flowController, "OnEnable");
            }

            Assert.That(GetField<bool>(flowController, "connectionEnabled"),
                Is.True);
            Assert.That(
                GetField<BattleBoardController>(
                    flowController,
                    "boardController"),
                Is.SameAs(boardController));
            Assert.That(flowController.Context, Is.Not.Null);
        }

        private void DeactivateFixture()
        {
            root.SetActive(false);
            Assert.That(flowController.isActiveAndEnabled, Is.False);
            if (GetField<bool>(flowController, "connectionEnabled"))
            {
                InvokePrivate(flowController, "OnDisable");
            }

            Assert.That(GetField<bool>(flowController, "connectionEnabled"),
                Is.False);
        }

        private void PublishInitialBoardReady()
        {
            SetField(boardController, "initialBoardReadyPublished", true);
            GetField<Action>(boardController, "InitialBoardReady")?.Invoke();
        }

        private static IEnumerator WaitUntil(
            Func<bool> condition,
            string failureMessage,
            int maximumFrames = 16)
        {
            for (int frame = 0; frame < maximumFrames; frame++)
            {
                if (condition())
                {
                    yield break;
                }

                yield return null;
            }

            Assert.That(condition(), Is.True, failureMessage);
        }

        private void SendCompletedAction(
            long actionId,
            BoardSwapActionResult result)
        {
            InvokePrivate(
                flowController,
                "HandleBoardActionStarted",
                CreateExecution(actionId, result));
            InvokePrivate(
                flowController,
                "HandleBoardActionFinished",
                CreateCompletion(
                    actionId,
                    result,
                    BoardActionCompletionStatus.Completed));
        }

        private static BoardCascadeResult CreateSingleMatchCascade()
        {
            return BattleFlowTestSupport.CreateCascade(
                new[]
                {
                    BattleFlowTestSupport.Match(
                        ElementType.Fire,
                        new BoardPosition(0, 0),
                        new BoardPosition(1, 0),
                        new BoardPosition(2, 0))
                });
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

        private static BoardSwapActionResult CreateNoMatchResult()
        {
            return CreateInternal<BoardSwapActionResult>(
                TestSwap(),
                BoardSwapActionStatus.NoMatch,
                new BoardState(),
                null,
                null,
                new BoardState());
        }

        private static BoardSwapActionResult CreateResolvedResult(
            BoardCascadeResult cascade)
        {
            BoardState finalBoard = cascade.Board;
            BoardShuffleResult shuffle = CreateInternal<BoardShuffleResult>(
                finalBoard,
                BoardShuffleKind.None,
                new List<BoardShuffleEntry>(),
                0);
            return CreateInternal<BoardSwapActionResult>(
                TestSwap(),
                BoardSwapActionStatus.Resolved,
                new BoardState(),
                cascade,
                shuffle,
                finalBoard);
        }

        private static BoardSwap TestSwap()
        {
            return new BoardSwap(
                new BoardPosition(0, 0),
                new BoardPosition(1, 0));
        }

        private static BoardActionExecution CreateExecution(
            long actionId,
            BoardSwapActionResult result)
        {
            return CreateInternal<BoardActionExecution>(actionId, result);
        }

        private static BoardActionCompletion CreateCompletion(
            long actionId,
            BoardSwapActionResult result,
            BoardActionCompletionStatus status,
            Exception failure = null)
        {
            return CreateInternal<BoardActionCompletion>(
                actionId,
                result,
                status,
                failure);
        }

        private static T CreateInternal<T>(params object[] arguments)
        {
            return (T)Activator.CreateInstance(
                typeof(T),
                BindingFlags.Instance
                    | BindingFlags.Public
                    | BindingFlags.NonPublic,
                null,
                arguments,
                null);
        }

        private static object InvokePrivate(
            object target,
            string methodName,
            params object[] arguments)
        {
            return target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(
                    target,
                    arguments);
        }

        private static void SetField(
            object target,
            string fieldName,
            object value)
        {
            target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(
                    target,
                    value);
        }

        private static object GetField(object target, string fieldName)
        {
            return target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(
                    target);
        }

        private static T GetField<T>(object target, string fieldName)
        {
            return (T)GetField(target, fieldName);
        }
    }
}
