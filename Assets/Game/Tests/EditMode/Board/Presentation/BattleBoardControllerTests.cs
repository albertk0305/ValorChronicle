using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board.Presentation
{
    public sealed class BattleBoardControllerTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();
        private GameBootstrapper bootstrapper;
        private CountingRandomSource randomSource;
        private BattleBoardView boardView;
        private BlockViewPool pool;
        private BattleBoardController controller;
        private GameObject shuffleText;
        private GameObject shuffleIcon;

        [SetUp]
        public void SetUp()
        {
            var bootstrapObject = new GameObject("TestBootstrapper");
            bootstrapObject.SetActive(false);
            createdObjects.Add(bootstrapObject);
            bootstrapper = bootstrapObject.AddComponent<GameBootstrapper>();
            randomSource = new CountingRandomSource(48271);
            SetProperty(bootstrapper, "RandomSource", randomSource);
            SetStaticProperty(
                typeof(GameBootstrapper),
                "Instance",
                bootstrapper);

            var root = new GameObject(
                "ControllerTestRoot",
                typeof(RectTransform));
            createdObjects.Add(root);

            var texture = new Texture2D(2, 2);
            createdObjects.Add(texture);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f));
            createdObjects.Add(sprite);

            var spriteSet = ScriptableObject.CreateInstance<
                BoardElementSpriteSet>();
            spriteSet.Configure(
                sprite,
                sprite,
                sprite,
                sprite,
                sprite,
                sprite);
            createdObjects.Add(spriteSet);

            var prefabObject = new GameObject(
                "BlockViewTemplate",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(BlockView));
            createdObjects.Add(prefabObject);
            var prefabView = prefabObject.GetComponent<BlockView>();
            prefabView.Configure(prefabObject.GetComponent<Image>());
            prefabObject.SetActive(false);

            pool = root.AddComponent<BlockViewPool>();
            pool.Configure(
                prefabView,
                root.GetComponent<RectTransform>(),
                BoardConstants.CellCount);
            boardView = root.AddComponent<BattleBoardView>();
            boardView.Configure(spriteSet, pool);
            shuffleText = new GameObject("ShuffleText", typeof(RectTransform));
            shuffleText.transform.SetParent(root.transform, false);
            shuffleText.SetActive(false);
            createdObjects.Add(shuffleText);
            shuffleIcon = new GameObject("ShuffleIcon", typeof(RectTransform));
            shuffleIcon.transform.SetParent(root.transform, false);
            shuffleIcon.SetActive(false);
            createdObjects.Add(shuffleIcon);
            SetField(boardView, "shuffleText", shuffleText);
            SetField(boardView, "shuffleIcon", shuffleIcon);

            controller = root.AddComponent<BattleBoardController>();
            SetField(controller, "boardView", boardView);
        }

        [TearDown]
        public void TearDown()
        {
            SetStaticProperty(typeof(GameBootstrapper), "Instance", null);

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
        public void Initialize_PublishesFinalBoardBeforeDropWithoutExtraWork()
        {
            var expectedRandom = new CountingRandomSource(48271);
            BoardState expectedBoard = CreateBoard(expectedRandom);
            SetField(controller, "initialDropDuration", 1f);
            SetField(controller, "initialDropColumnStagger", 0f);

            controller.Initialize();

            Assert.That(controller.CurrentBoard, Is.Not.Null);
            Assert.That(controller.IsInitialized, Is.True);
            Assert.That(controller.IsBoardReady, Is.False);
            Assert.That(boardView.IsAnimating, Is.True);
            Assert.That(randomSource.NextCallCount, Is.EqualTo(
                expectedRandom.NextCallCount));
            AssertBoardsEquivalent(expectedBoard, controller.CurrentBoard);
            Assert.That(pool.TotalCreatedCount, Is.EqualTo(
                BoardConstants.CellCount));

            int randomCalls = randomSource.NextCallCount;
            controller.Initialize();

            Assert.That(randomSource.NextCallCount, Is.EqualTo(randomCalls));
            Assert.That(pool.TotalCreatedCount, Is.EqualTo(
                BoardConstants.CellCount));
        }

        [UnityTest]
        public IEnumerator Initialize_ZeroDurationBecomesReadyAfterDropCompletes()
        {
            int readyCount = 0;
            controller.InitialBoardReady += () => readyCount++;
            SetField(controller, "initialDropDuration", 0f);
            SetField(controller, "initialDropColumnStagger", 0f);

            controller.Initialize();
            Assert.That(controller.CurrentBoard, Is.Not.Null);

            yield return null;

            Assert.That(controller.IsBoardReady, Is.True);
            Assert.That(readyCount, Is.EqualTo(1));
            Assert.That(boardView.IsAnimating, Is.False);
            foreach (BlockView view in boardView.ActiveViews.Values)
            {
                Assert.That(view.IsInputEnabled, Is.True);
            }

            InvokePrivate(controller, "UpdateReadyStateAfterInitialDrop");
            Assert.That(readyCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Initialize_DoesNotPublishReadyBeforeDropOrFromStaleInitialization()
        {
            int readyCount = 0;
            controller.InitialBoardReady += () => readyCount++;
            SetField(controller, "initialDropDuration", 0f);
            SetField(controller, "initialDropColumnStagger", 0f);

            controller.Initialize();
            Assert.That(readyCount, Is.Zero);

            InvokePrivate(controller, "OnDisable");
            yield return null;

            Assert.That(readyCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator InitialDropMismatch_ReportsDetailedReasonAndStaysNotReady()
        {
            yield return InitializeReadyBoard();
            BlockView changedView = FirstView();
            changedView.SetAnchoredPosition(Vector2.zero);
            LogAssert.Expect(
                LogType.Error,
                new Regex(
                    "^\\[BattleBoard\\] Initial board View mismatch: " +
                    ".*Context=InitialDropComplete; " +
                    "Field=AnchoredPosition"));

            InvokePrivate(controller, "UpdateReadyStateAfterInitialDrop");

            Assert.That(controller.IsBoardReady, Is.False);
            Assert.That(controller.CanAcceptBoardInput, Is.False);
            Assert.That(controller.CurrentBoard, Is.Not.Null);
        }

        [Test]
        public void Initialize_BuildsOneSharedResolverGraphOnlyOnce()
        {
            SetAllDurationsToZero();

            controller.Initialize();

            object idGenerator = GetField(controller, "blockIdGenerator");
            object generator = GetField(controller, "boardGenerator");
            object refiller = GetField(controller, "boardRefiller");
            object shuffler = GetField(controller, "boardShuffler");
            object conversionResolver = GetField(
                controller,
                "elementConversionResolver");
            object resolver = GetField(controller, "swapActionResolver");
            Assert.That(GetField(controller, "randomSource"),
                Is.SameAs(randomSource));
            Assert.That(GetField(generator, "randomSource"),
                Is.SameAs(randomSource));
            Assert.That(GetField(refiller, "randomSource"),
                Is.SameAs(randomSource));
            Assert.That(GetField(shuffler, "randomSource"),
                Is.SameAs(randomSource));
            Assert.That(GetField(conversionResolver, "randomSource"),
                Is.SameAs(randomSource));
            Assert.That(GetField(generator, "idGenerator"),
                Is.SameAs(idGenerator));
            Assert.That(GetField(refiller, "idGenerator"),
                Is.SameAs(idGenerator));
            Assert.That(GetField(idGenerator, "nextId"), Is.EqualTo(31L));

            controller.Initialize();

            Assert.That(GetField(controller, "blockIdGenerator"),
                Is.SameAs(idGenerator));
            Assert.That(GetField(controller, "boardGenerator"),
                Is.SameAs(generator));
            Assert.That(GetField(controller, "swapActionResolver"),
                Is.SameAs(resolver));
            Assert.That(GetField(controller, "elementConversionResolver"),
                Is.SameAs(conversionResolver));
        }

        [UnityTest]
        public IEnumerator TryExecuteSwap_NotSwappableConsumesNoDependencies()
        {
            yield return InitializeReadyBoard();
            int startedCount = 0;
            int finishedCount = 0;
            int cascadeSignalCount = 0;
            controller.BoardActionStarted += _ => startedCount++;
            controller.BoardActionFinished += _ => finishedCount++;
            controller.CascadeStepPresented += _ => cascadeSignalCount++;
            BoardState beforeBoard = controller.CurrentBoard;
            int randomCalls = randomSource.NextCallCount;
            long nextId = GetNextId();
            var position = new BoardPosition(0, 0);

            bool accepted = controller.TryExecuteSwap(
                new BoardSwap(position, position));

            Assert.That(accepted, Is.True);
            Assert.That(controller.LastSwapActionResult.Status,
                Is.EqualTo(BoardSwapActionStatus.NotSwappable));
            Assert.That(controller.CurrentBoard, Is.SameAs(beforeBoard));
            Assert.That(controller.IsBoardReady, Is.True);
            Assert.That(controller.CanAcceptBoardInput, Is.True);
            Assert.That(boardView.IsAnimating, Is.False);
            Assert.That(randomSource.NextCallCount, Is.EqualTo(randomCalls));
            Assert.That(GetNextId(), Is.EqualTo(nextId));
            Assert.That(startedCount, Is.Zero);
            Assert.That(finishedCount, Is.Zero);
            Assert.That(cascadeSignalCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ExternalInputGate_PreservesInternalInputConditions()
        {
            yield return InitializeReadyBoard();

            Assert.That(controller.IsExternalInputEnabled, Is.True);
            Assert.That(controller.CanAcceptBoardInput, Is.True);

            controller.IsExternalInputEnabled = false;
            Assert.That(controller.CanAcceptBoardInput, Is.False);

            controller.IsExternalInputEnabled = true;
            Assert.That(controller.CanAcceptBoardInput, Is.True);

            SetField(
                controller,
                "actionTimings",
                new BoardActionPresentationTimings(1f, 0f, 0f, 0f, 0f));
            Assert.That(controller.TryExecuteSwap(
                FindValidSwap(controller.CurrentBoard)), Is.True);
            controller.IsExternalInputEnabled = false;
            controller.IsExternalInputEnabled = true;

            Assert.That(controller.CanAcceptBoardInput, Is.False);
            InvokePrivate(controller, "OnDisable");
        }

        [UnityTest]
        public IEnumerator TryExecuteSwap_NoMatchReturnsToReadyWithoutConsumption()
        {
            yield return InitializeReadyBoard();
            var eventOrder = new List<string>();
            BoardActionExecution execution = null;
            BoardActionCompletion completion = null;
            int cascadeSignalCount = 0;
            bool inputLockedAtStart = false;
            controller.BoardActionStarted += value =>
            {
                execution = value;
                inputLockedAtStart = !controller.CanAcceptBoardInput;
                eventOrder.Add("Started");
            };
            controller.BoardActionFinished += value =>
            {
                completion = value;
                eventOrder.Add("Finished");
            };
            controller.CascadeStepPresented += _ => cascadeSignalCount++;
            BoardSwap swap = FindNoMatchSwap(controller.CurrentBoard);
            int randomCalls = randomSource.NextCallCount;
            long nextId = GetNextId();

            Assert.That(controller.TryExecuteSwap(swap), Is.True);
            Assert.That(execution, Is.Not.Null);
            Assert.That(inputLockedAtStart, Is.True);
            Assert.That(completion, Is.Not.Null);
            Assert.That(controller.LastSwapActionResult.Status,
                Is.EqualTo(BoardSwapActionStatus.NoMatch));
            Assert.That(controller.CurrentBoard,
                Is.SameAs(controller.LastSwapActionResult.Board));
            Assert.That(controller.IsBoardReady, Is.True);
            Assert.That(controller.CanAcceptBoardInput, Is.True);
            Assert.That(boardView.MatchesBoard(controller.CurrentBoard),
                Is.True);
            Assert.That(randomSource.NextCallCount, Is.EqualTo(randomCalls));
            Assert.That(GetNextId(), Is.EqualTo(nextId));
            Assert.That(eventOrder, Is.EqualTo(new[]
            {
                "Started",
                "Finished"
            }));
            Assert.That(cascadeSignalCount, Is.Zero);
            Assert.That(completion.ActionId, Is.EqualTo(execution.ActionId));
            Assert.That(completion.Result, Is.SameAs(
                controller.LastSwapActionResult));
            Assert.That(completion.CompletionStatus, Is.EqualTo(
                BoardActionCompletionStatus.Completed));
            Assert.That(completion.Result.ConsumesTurn, Is.False);
            Assert.That(completion.Failure, Is.Null);
        }

        [UnityTest]
        public IEnumerator TryExecuteSwap_ResolvedUsesContinuousRefillIds()
        {
            yield return InitializeReadyBoard();
            BoardActionExecution execution = null;
            BoardActionCompletion completion = null;
            int startedCount = 0;
            int finishedCount = 0;
            var presentations =
                new List<BoardCascadeStepPresentation>();
            var eventOrder = new List<string>();
            controller.BoardActionStarted += value =>
            {
                execution = value;
                startedCount++;
                eventOrder.Add("Started");
            };
            controller.CascadeStepPresented += value =>
            {
                presentations.Add(value);
                eventOrder.Add($"Cascade[{value.CascadeStepIndex}]");
            };
            controller.BoardActionFinished += value =>
            {
                completion = value;
                finishedCount++;
                eventOrder.Add("Finished");
            };
            BoardSwap swap = FindValidSwap(controller.CurrentBoard);
            object resolver = GetField(controller, "swapActionResolver");
            object idGenerator = GetField(controller, "blockIdGenerator");

            Assert.That(controller.TryExecuteSwap(swap), Is.True);
            Assert.That(execution, Is.Not.Null);
            yield return null;

            BoardSwapActionResult result = controller.LastSwapActionResult;
            Assert.That(result.Status, Is.EqualTo(
                BoardSwapActionStatus.Resolved));
            Assert.That(controller.CurrentBoard, Is.SameAs(result.Board));
            Assert.That(controller.IsBoardReady, Is.True);
            Assert.That(boardView.MatchesBoard(controller.CurrentBoard),
                Is.True);
            Assert.That(pool.TotalCreatedCount, Is.EqualTo(30));
            Assert.That(GetField(controller, "swapActionResolver"),
                Is.SameAs(resolver));
            Assert.That(GetField(controller, "blockIdGenerator"),
                Is.SameAs(idGenerator));

            long expectedId = 31;
            var runtimeIds = new HashSet<long>();
            foreach (BoardCascadeStep step in result.Cascade.Steps)
            {
                foreach (BoardBlockSpawn spawn in step.Refill.Spawns)
                {
                    Assert.That(spawn.RuntimeId, Is.EqualTo(expectedId++));
                }
            }

            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                Assert.That(runtimeIds.Add(controller.CurrentBoard.Get(
                    BoardPosition.FromIndex(index)).RuntimeId), Is.True);
            }

            Assert.That(GetNextId(), Is.EqualTo(expectedId));
            Assert.That(completion, Is.Not.Null);
            Assert.That(startedCount, Is.EqualTo(1));
            Assert.That(finishedCount, Is.EqualTo(1));
            Assert.That(completion.ActionId, Is.EqualTo(execution.ActionId));
            Assert.That(completion.CompletionStatus, Is.EqualTo(
                BoardActionCompletionStatus.Completed));
            Assert.That(completion.Result.ConsumesTurn, Is.True);
            Assert.That(completion.Result.Cascade, Is.SameAs(result.Cascade));
            Assert.That(completion.Result.Shuffle, Is.SameAs(result.Shuffle));
            Assert.That(presentations.Count,
                Is.EqualTo(result.Cascade.Steps.Count));
            for (int index = 0; index < presentations.Count; index++)
            {
                Assert.That(presentations[index].ActionId,
                    Is.EqualTo(execution.ActionId));
                Assert.That(presentations[index].CascadeStepIndex,
                    Is.EqualTo(index));
                Assert.That(presentations[index].Step,
                    Is.SameAs(result.Cascade.Steps[index]));
            }

            Assert.That(eventOrder[0], Is.EqualTo("Started"));
            Assert.That(eventOrder[eventOrder.Count - 1],
                Is.EqualTo("Finished"));
            Assert.That(eventOrder.Count,
                Is.EqualTo(result.Cascade.Steps.Count + 2));
        }

        [UnityTest]
        public IEnumerator CascadeSignalObserverFailureDoesNotBreakAction()
        {
            yield return InitializeReadyBoard();
            bool hasThrown = false;
            int successfulObservationCount = 0;
            BoardActionCompletion completion = null;
            controller.CascadeStepPresented += _ =>
            {
                if (!hasThrown)
                {
                    hasThrown = true;
                    throw new InvalidOperationException(
                        "Injected cascade presentation observer failure.");
                }
            };
            controller.CascadeStepPresented += _ =>
                successfulObservationCount++;
            controller.BoardActionFinished += value => completion = value;
            LogAssert.Expect(
                LogType.Exception,
                new Regex(
                    "Injected cascade presentation observer failure\\."));
            LogAssert.Expect(
                LogType.Error,
                "[BattleBoard] Cascade presentation observer failed.");

            Assert.That(controller.TryExecuteSwap(
                FindValidSwap(controller.CurrentBoard)), Is.True);
            yield return null;

            Assert.That(hasThrown, Is.True);
            Assert.That(successfulObservationCount,
                Is.EqualTo(controller.LastSwapActionResult
                    .Cascade.Steps.Count));
            Assert.That(completion, Is.Not.Null);
            Assert.That(completion.CompletionStatus,
                Is.EqualTo(BoardActionCompletionStatus.Completed));
            Assert.That(controller.IsBoardReady, Is.True);
            Assert.That(boardView.MatchesBoard(controller.CurrentBoard),
                Is.True);
        }

        [UnityTest]
        public IEnumerator BoardActionsUseIncreasingIdsAndIgnoreStaleCompletion()
        {
            yield return InitializeReadyBoard();
            var started = new List<BoardActionExecution>();
            var finished = new List<BoardActionCompletion>();
            controller.BoardActionStarted += started.Add;
            controller.BoardActionFinished += finished.Add;

            Assert.That(controller.TryExecuteSwap(
                FindNoMatchSwap(controller.CurrentBoard)), Is.True);
            yield return null;
            long firstActionId = started[0].ActionId;

            SetField(
                controller,
                "actionTimings",
                new BoardActionPresentationTimings(1f, 0f, 0f, 0f, 0f));
            Assert.That(controller.TryExecuteSwap(
                FindValidSwap(controller.CurrentBoard)), Is.True);
            Assert.That(started[1].ActionId, Is.GreaterThan(firstActionId));

            bool staleAccepted = (bool)InvokePrivate(
                controller,
                "TryCompleteBoardAction",
                firstActionId,
                BoardActionCompletionStatus.Completed,
                null);

            Assert.That(staleAccepted, Is.False);
            Assert.That(finished.Count, Is.EqualTo(1));
            InvokePrivate(controller, "OnDisable");
            Assert.That(finished.Count, Is.EqualTo(2));
            Assert.That(finished[1].ActionId, Is.EqualTo(
                started[1].ActionId));
            Assert.That(finished[1].CompletionStatus, Is.EqualTo(
                BoardActionCompletionStatus.Interrupted));
        }

        [UnityTest]
        public IEnumerator PresentationFailurePublishesFailedExactlyOnce()
        {
            yield return InitializeReadyBoard();
            SetField(
                controller,
                "actionTimings",
                new BoardActionPresentationTimings(
                    1f,
                    0f,
                    0f,
                    0f,
                    0f));
            var finished = new List<BoardActionCompletion>();
            controller.BoardActionFinished += finished.Add;
            Assert.That(controller.TryExecuteSwap(
                FindValidSwap(controller.CurrentBoard)), Is.True);
            Assert.That(finished, Is.Empty);
            var failure = new InvalidOperationException(
                "Injected presentation failure.");
            var original = (IEnumerator)GetField(
                controller,
                "activePresentationSequence");
            if (original is IDisposable disposable)
            {
                disposable.Dispose();
            }

            SetField(
                controller,
                "activePresentationSequence",
                new ThrowingEnumerator(failure));
            LogAssert.Expect(
                LogType.Exception,
                new Regex("Injected presentation failure\\."));
            LogAssert.Expect(
                LogType.Error,
                "[BattleBoard] Board presentation failed.");

            yield return null;

            Assert.That(finished.Count, Is.EqualTo(1));
            Assert.That(finished[0].CompletionStatus, Is.EqualTo(
                BoardActionCompletionStatus.Failed));
            Assert.That(finished[0].Failure, Is.SameAs(failure));
            Assert.That(controller.IsBoardReady, Is.True);
            Assert.That(controller.CanAcceptBoardInput, Is.True);
            Assert.That(boardView.MatchesBoard(controller.CurrentBoard),
                Is.True);
            InvokePrivate(controller, "OnDisable");
            Assert.That(finished.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator TryExecuteSwap_RejectsSecondRequestWhileBusy()
        {
            yield return InitializeReadyBoard();
            SetField(controller, "swapDuration", 1f);
            SetField(
                controller,
                "actionTimings",
                new BoardActionPresentationTimings(1f, 0f, 0f, 0f, 0f));
            BoardSwap swap = FindValidSwap(controller.CurrentBoard);

            Assert.That(controller.TryExecuteSwap(swap), Is.True);
            Assert.That(controller.IsBoardReady, Is.False);
            Assert.That(controller.CanAcceptBoardInput, Is.False);
            Assert.That(controller.TryExecuteSwap(swap), Is.False);

            InvokePrivate(controller, "OnDisable");
            yield return null;

            Assert.That(controller.IsBoardReady, Is.True);
            Assert.That(boardView.MatchesBoard(controller.CurrentBoard),
                Is.True);
        }

        [UnityTest]
        public IEnumerator TryExecuteSwap_AllowsNextActionAfterCompletion()
        {
            yield return InitializeReadyBoard();
            object resolver = GetField(controller, "swapActionResolver");
            object idGenerator = GetField(controller, "blockIdGenerator");

            Assert.That(controller.TryExecuteSwap(
                FindValidSwap(controller.CurrentBoard)), Is.True);
            yield return null;
            int firstVersion = (int)GetField(controller, "actionVersion");
            long nextIdAfterFirst = GetNextId();

            Assert.That(controller.TryExecuteSwap(
                FindValidSwap(controller.CurrentBoard)), Is.True);
            yield return null;

            Assert.That(controller.IsBoardReady, Is.True);
            Assert.That(boardView.MatchesBoard(controller.CurrentBoard),
                Is.True);
            Assert.That((int)GetField(controller, "actionVersion"),
                Is.GreaterThan(firstVersion));
            Assert.That(GetNextId(), Is.GreaterThan(nextIdAfterFirst));
            Assert.That(GetField(controller, "swapActionResolver"),
                Is.SameAs(resolver));
            Assert.That(GetField(controller, "blockIdGenerator"),
                Is.SameAs(idGenerator));
        }

        [UnityTest]
        public IEnumerator InterruptedActionKeepsFinalBoardAndStabilizesView()
        {
            yield return InitializeReadyBoard();
            var finished = new List<BoardActionCompletion>();
            var presentations =
                new List<BoardCascadeStepPresentation>();
            controller.BoardActionFinished += finished.Add;
            controller.CascadeStepPresented += presentations.Add;
            SetField(
                controller,
                "actionTimings",
                new BoardActionPresentationTimings(1f, 1f, 1f, 1f, 1f));
            Assert.That(controller.TryExecuteSwap(
                FindValidSwap(controller.CurrentBoard)), Is.True);
            BoardState finalBoard = controller.CurrentBoard;

            InvokePrivate(controller, "OnDisable");
            yield return null;

            Assert.That(controller.CurrentBoard, Is.SameAs(finalBoard));
            Assert.That(controller.IsBoardReady, Is.True);
            Assert.That(GetField(controller, "isActionInProgress"),
                Is.False);
            Assert.That(boardView.IsAnimating, Is.False);
            Assert.That(boardView.MatchesBoard(finalBoard), Is.True);
            Assert.That(finished.Count, Is.EqualTo(1));
            Assert.That(finished[0].CompletionStatus, Is.EqualTo(
                BoardActionCompletionStatus.Interrupted));
            Assert.That(presentations, Is.Empty);

            InvokePrivate(controller, "OnDisable");
            yield return null;
            Assert.That(finished.Count, Is.EqualTo(1));
            Assert.That(presentations, Is.Empty);
        }

        [UnityTest]
        public IEnumerator InterruptedAction_ViewMismatchKeepsBoardNotReady()
        {
            yield return InitializeReadyBoard();
            SetField(
                controller,
                "actionTimings",
                new BoardActionPresentationTimings(1f, 1f, 1f, 1f, 1f));
            Assert.That(controller.TryExecuteSwap(
                FindValidSwap(controller.CurrentBoard)), Is.True);

            InvokePrivate(boardView, "OnDisable");
            BlockView changedView = FirstView();
            changedView.SetAnchoredPosition(Vector2.zero);
            LogAssert.Expect(
                LogType.Error,
                "[BattleBoard] The interrupted board presentation did not stabilize.");
            InvokePrivate(controller, "OnDisable");
            yield return null;

            Assert.That(controller.IsBoardReady, Is.False);
            Assert.That(controller.CanAcceptBoardInput, Is.False);
            Assert.That(boardView.MatchesBoard(controller.CurrentBoard),
                Is.False);
        }

        [Test]
        public void TryExecuteSwap_BeforeInitializationIsRejected()
        {
            Assert.That(controller.TryExecuteSwap(
                new BoardSwap(
                    new BoardPosition(0, 0),
                    new BoardPosition(1, 0))), Is.False);
            Assert.That(controller.LastSwapActionResult, Is.Null);
        }

        [UnityTest]
        public IEnumerator RockMutationUpdatesBoardViewsAndCapSynchronously()
        {
            SetAllDurationsToZero();
            controller.Initialize();
            yield return null;
            BoardState inputBoard = controller.CurrentBoard;
            BoardBlock[] original = Capture(inputBoard);
            var command = new BattleBoardMutationCommand(
                BattleBoardMutationKind.CreateRock,
                requestedCount: 3,
                maximumCount: 6);
            BattleBoardMutationCompletion completion = null;

            Assert.That(controller.TryExecuteMutation(
                command,
                result => completion = result), Is.True);

            Assert.That(completion, Is.Not.Null);
            Assert.That(completion.Succeeded, Is.True);
            Assert.That(completion.Result.CreatedCount, Is.EqualTo(3));
            Assert.That(controller.CurrentBoard,
                Is.SameAs(completion.Result.Board));
            Assert.That(controller.CurrentBoard, Is.Not.SameAs(inputBoard));
            Assert.That(boardView.MatchesBoard(controller.CurrentBoard),
                Is.True);
            Assert.That(controller.IsBoardReady, Is.True);
            AssertOriginalUnchanged(inputBoard, original);
            foreach (BoardRockPlacement placement
                in completion.Result.Placements)
            {
                Assert.That(placement.RockBlock.RuntimeId,
                    Is.Not.EqualTo(placement.ReplacedBlock.RuntimeId));
                Assert.That(boardView.TryGetView(
                    placement.RockBlock.RuntimeId,
                    out BlockView view), Is.True);
                Assert.That(view.Position, Is.EqualTo(placement.Position));
                Assert.That(boardView.TryGetView(
                    placement.ReplacedBlock.RuntimeId,
                    out _), Is.False);
            }

            controller.TryExecuteMutation(command, result =>
                completion = result);
            Assert.That(CountRocks(controller.CurrentBoard), Is.EqualTo(6));
            Assert.That(completion.Result.CreatedCount, Is.EqualTo(3));
            controller.TryExecuteMutation(command, result =>
                completion = result);
            Assert.That(CountRocks(controller.CurrentBoard), Is.EqualTo(6));
            Assert.That(completion.Result.CreatedCount, Is.Zero);
            Assert.That(shuffleText.activeSelf, Is.False);
            Assert.That(shuffleIcon.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator RockMutationShuffleWaitsForPresentationAndCompletesOnce()
        {
            SetAllDurationsToZero();
            SetField(controller, "shuffleDuration", 0.05f);
            controller.Initialize();
            yield return null;
            // Initialize installs runtime time sources. Override them after
            // initialization so the presentation frame count is deterministic.
            boardView.ConfigureBlockAnimationTimeSources(
                () => false,
                () => 0.01f);
            BoardState sourceBoard = CreateOneMoveBoard();
            SetProperty(controller, "CurrentBoard", sourceBoard);
            SetProperty(controller, "IsBoardReady", true);
            boardView.Render(sourceBoard);
            SetField(controller, "rockMutationResolver",
                CreateForcedShuffleRockResolver());
            var command = new BattleBoardMutationCommand(
                BattleBoardMutationKind.CreateRock,
                requestedCount: 3,
                maximumCount: 6);
            BattleBoardMutationCompletion completion = null;
            int completionCount = 0;

            Assert.That(controller.TryExecuteMutation(command, result =>
            {
                completion = result;
                completionCount++;
            }), Is.True);

            Assert.That(completion, Is.Null);
            Assert.That(controller.LastRockMutationResult, Is.Not.Null);
            Assert.That(controller.LastRockMutationResult.WasShuffled,
                Is.True);
            Assert.That(controller.LastRockMutationResult.ShuffleResult,
                Is.Not.Null);
            Assert.That(
                controller.LastRockMutationResult.ShuffleResult.WasShuffled,
                Is.True);
            Assert.That(shuffleText.activeSelf, Is.True);
            Assert.That(shuffleIcon.activeSelf, Is.True);
            Assert.That(controller.IsBoardReady, Is.False);
            Assert.That(controller.TryExecuteMutation(command, _ => { }),
                Is.False);
            Assert.That(controller.TryConvertElements(
                ElementType.Water,
                4,
                out _), Is.False);
            Assert.That(controller.TryExecuteSwap(
                new BoardPosition(0, 0),
                new BoardPosition(0, 1)), Is.False);

            int presentationVersion =
                (int)GetField(controller, "actionVersion");
            // EditMode advances StartCoroutine to its first yield, but does
            // not reliably resume that MonoBehaviour coroutine. Continue the
            // same controller presentation iterator deterministically.
            controller.StopAllCoroutines();
            var presentation = (IEnumerator)InvokePrivate(
                controller,
                "RunRockMutationPresentation",
                presentationVersion);
            int remainingPresentationFrames = 0;
            while (presentation.MoveNext())
            {
                remainingPresentationFrames++;
                Assert.That(completion, Is.Null);
                Assert.That(shuffleText.activeSelf, Is.True);
                Assert.That(shuffleIcon.activeSelf, Is.True);
            }

            Assert.That(remainingPresentationFrames, Is.EqualTo(5));
            Assert.That(completion, Is.Not.Null);
            Assert.That(completion.Succeeded, Is.True);
            Assert.That(completionCount, Is.EqualTo(1));
            Assert.That(completion.Result.WasShuffled, Is.True);
            Assert.That(controller.IsBoardReady, Is.True);
            Assert.That(controller.CanAcceptBoardInput, Is.True);
            Assert.That(boardView.MatchesBoard(controller.CurrentBoard),
                Is.True);
            Assert.That(shuffleText.activeSelf, Is.False);
            Assert.That(shuffleIcon.activeSelf, Is.False);
            Assert.That(CountRocks(controller.CurrentBoard), Is.EqualTo(3));
            foreach (BoardRockPlacement placement
                in completion.Result.Placements)
            {
                Assert.That(controller.CurrentBoard.Get(placement.Position),
                    Is.SameAs(placement.RockBlock));
                Assert.That(boardView.TryGetView(
                    placement.RockBlock.RuntimeId,
                    out BlockView view), Is.True);
                Assert.That(view.Position, Is.EqualTo(placement.Position));
            }

            yield return null;
            Assert.That(completionCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator InterruptedRockMutationShuffleFailsOnceAndCleansUi()
        {
            SetAllDurationsToZero();
            SetField(controller, "shuffleDuration", 1f);
            controller.Initialize();
            yield return null;
            BoardState sourceBoard = CreateOneMoveBoard();
            SetProperty(controller, "CurrentBoard", sourceBoard);
            SetProperty(controller, "IsBoardReady", true);
            boardView.Render(sourceBoard);
            SetField(controller, "rockMutationResolver",
                CreateForcedShuffleRockResolver());
            var command = new BattleBoardMutationCommand(
                BattleBoardMutationKind.CreateRock,
                requestedCount: 3,
                maximumCount: 6);
            BattleBoardMutationCompletion completion = null;
            int completionCount = 0;
            controller.TryExecuteMutation(command, result =>
            {
                completion = result;
                completionCount++;
            });
            Assert.That(shuffleText.activeSelf, Is.True);

            InvokePrivate(controller, "OnDisable");
            yield return null;

            Assert.That(completion, Is.Not.Null);
            Assert.That(completion.Succeeded, Is.False);
            Assert.That(completion.Failure, Is.Not.Null);
            Assert.That(completionCount, Is.EqualTo(1));
            Assert.That(shuffleText.activeSelf, Is.False);
            Assert.That(shuffleIcon.activeSelf, Is.False);
            Assert.That(boardView.IsAnimating, Is.False);
            Assert.That(boardView.MatchesBoard(controller.CurrentBoard),
                Is.True);
        }

        [UnityTest]
        public IEnumerator ElementConversion_FourUpdatesBoardViewAndSprite()
        {
            yield return InitializeReadyBoard();
            Sprite waterSprite = ConfigureDistinctElementSprites();
            BoardState sourceBoard = CreateElementConversionBoard();
            ConfigureElementConversion(sourceBoard, new SequenceRandomSource(
                new[] { 0, 1, 2, 4 }));
            int startedCount = 0;
            int finishedCount = 0;
            controller.BoardActionStarted += _ => startedCount++;
            controller.BoardActionFinished += _ => finishedCount++;

            Assert.That(controller.TryConvertElements(
                ElementType.Water,
                4,
                out BoardElementConversionResult result), Is.True);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ConvertedCount, Is.EqualTo(4));
            Assert.That(controller.CurrentBoard, Is.SameAs(result.Board));
            Assert.That(controller.LastElementConversionResult,
                Is.SameAs(result));
            Assert.That(controller.IsBoardReady, Is.True);
            Assert.That(controller.CanAcceptBoardInput, Is.True);
            Assert.That(boardView.MatchesBoard(controller.CurrentBoard),
                Is.True);
            Assert.That(startedCount, Is.Zero);
            Assert.That(finishedCount, Is.Zero);
            Assert.That(shuffleText.activeSelf, Is.False);
            Assert.That(shuffleIcon.activeSelf, Is.False);

            foreach (BoardElementConversionEntry entry in result.Entries)
            {
                BoardBlock before = sourceBoard.Get(entry.Position);
                BoardBlock after = controller.CurrentBoard.Get(entry.Position);
                Assert.That(after.RuntimeId, Is.EqualTo(before.RuntimeId));
                Assert.That(after.BlockType, Is.EqualTo(before.BlockType));
                Assert.That(boardView.TryGetView(
                    after.RuntimeId,
                    out BlockView view), Is.True);
                Assert.That(view.Position, Is.EqualTo(entry.Position));
                Assert.That(view.Image.sprite, Is.SameAs(waterSprite));
            }

            Assert.That(BoardMatchFinder.FindMatches(controller.CurrentBoard),
                Is.Not.Empty);
        }

        [UnityTest]
        public IEnumerator ElementConversion_FiveConvertsExactlyFive()
        {
            yield return InitializeReadyBoard();
            ConfigureElementConversion(
                CreateElementConversionBoard(),
                new SequenceRandomSource(new[] { 0, 1, 2, 3, 4 }));

            Assert.That(controller.TryConvertElements(
                ElementType.Water,
                5,
                out BoardElementConversionResult result), Is.True);

            Assert.That(result.ConvertedCount, Is.EqualTo(5));
            Assert.That(boardView.MatchesBoard(controller.CurrentBoard),
                Is.True);
        }

        [UnityTest]
        public IEnumerator ElementConversion_ResolverFailurePreservesBoardAndView()
        {
            yield return InitializeReadyBoard();
            BoardState sourceBoard = CreateUniformBoard(ElementType.Water);
            ConfigureElementConversion(
                sourceBoard,
                new SequenceRandomSource(Array.Empty<int>()));
            BoardBlock[] original = Capture(sourceBoard);
            var originalViews = new Dictionary<long, BlockView>(
                boardView.ActiveViews);

            Assert.That(controller.TryConvertElements(
                ElementType.Water,
                4,
                out BoardElementConversionResult result), Is.False);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Failure,
                Is.EqualTo(BoardElementConversionFailure.NoCandidates));
            Assert.That(controller.CurrentBoard, Is.SameAs(sourceBoard));
            Assert.That(controller.IsBoardReady, Is.True);
            Assert.That(boardView.MatchesBoard(sourceBoard), Is.True);
            AssertOriginalUnchanged(sourceBoard, original);
            foreach (KeyValuePair<long, BlockView> pair in originalViews)
            {
                Assert.That(boardView.TryGetView(
                    pair.Key,
                    out BlockView current), Is.True);
                Assert.That(current, Is.SameAs(pair.Value));
            }
        }

        [UnityTest]
        public IEnumerator ElementConversion_FewerCandidatesRemainUnchangedWhenUnplayable()
        {
            yield return InitializeReadyBoard();
            BoardState sourceBoard = CreateUniformBoard(ElementType.Water);
            SetElement(sourceBoard, 0, 0, ElementType.Fire);
            SetElement(sourceBoard, 1, 0, ElementType.Grass);
            ConfigureElementConversion(
                sourceBoard,
                new SequenceRandomSource(new[] { 0, 1 }),
                maxRandomSelectionAttempts: 1);

            Assert.That(controller.TryConvertElements(
                ElementType.Water,
                4,
                out BoardElementConversionResult result), Is.False);

            Assert.That(result.Failure,
                Is.EqualTo(
                    BoardElementConversionFailure.NoPlayableCombination));
            Assert.That(result.DeterministicCombinationCount, Is.EqualTo(1));
            Assert.That(controller.CurrentBoard, Is.SameAs(sourceBoard));
            Assert.That(boardView.MatchesBoard(sourceBoard), Is.True);
        }

        [UnityTest]
        public IEnumerator ElementConversion_ValidSwapProcessesExistingAndNewMatch()
        {
            yield return InitializeReadyBoard();
            ConfigureElementConversion(
                CreateElementConversionBoard(),
                new SequenceRandomSource(new[] { 0, 1, 2, 4 }));
            Assert.That(controller.TryConvertElements(
                ElementType.Water,
                4,
                out _), Is.True);
            IReadOnlyList<BoardMatch> existingMatches =
                BoardMatchFinder.FindMatches(controller.CurrentBoard);
            Assert.That(existingMatches, Is.Not.Empty);
            BoardSwap swap = FindValidSwap(controller.CurrentBoard);

            Assert.That(controller.TryExecuteSwap(swap), Is.True);

            BoardSwapActionResult action = controller.LastSwapActionResult;
            Assert.That(action.Status,
                Is.EqualTo(BoardSwapActionStatus.Resolved));
            Assert.That(action.Cascade.Steps, Is.Not.Empty);
            BoardMatch firstResolvedMatch =
                action.Cascade.Steps[0].Matches[0];
            foreach (BoardPosition position in existingMatches[0].Positions)
            {
                Assert.That(
                    ContainsPosition(firstResolvedMatch.Positions, position),
                    Is.True);
            }

            Assert.That(
                ContainsPosition(firstResolvedMatch.Positions, swap.First)
                    || ContainsPosition(
                        firstResolvedMatch.Positions,
                        swap.Second),
                Is.True);
        }

        [UnityTest]
        public IEnumerator ElementConversion_InvalidSwapKeepsConvertedElements()
        {
            yield return InitializeReadyBoard();
            ConfigureElementConversion(
                CreateElementConversionBoard(),
                new SequenceRandomSource(new[] { 0, 1, 2, 4 }));
            Assert.That(controller.TryConvertElements(
                ElementType.Water,
                4,
                out BoardElementConversionResult conversion), Is.True);
            BoardSwap noMatchSwap = FindNoMatchSwap(controller.CurrentBoard);

            Assert.That(controller.TryExecuteSwap(noMatchSwap), Is.True);

            Assert.That(controller.LastSwapActionResult.Status,
                Is.EqualTo(BoardSwapActionStatus.NoMatch));
            foreach (BoardElementConversionEntry entry in conversion.Entries)
            {
                BoardBlock block = controller.CurrentBoard.Get(entry.Position);
                Assert.That(block.RuntimeId, Is.EqualTo(entry.RuntimeId));
                Assert.That(block.Element, Is.EqualTo(ElementType.Water));
            }
        }

        [UnityTest]
        public IEnumerator ElementConversion_RejectsUnavailableInputStates()
        {
            Assert.That(controller.TryConvertElements(
                ElementType.Water,
                4,
                out _), Is.False);

            SetAllDurationsToZero();
            controller.Initialize();
            Assert.That(controller.TryConvertElements(
                ElementType.Water,
                4,
                out _), Is.False);
            yield return null;

            controller.IsExternalInputEnabled = false;
            Assert.That(controller.TryConvertElements(
                ElementType.Water,
                4,
                out _), Is.False);
            controller.IsExternalInputEnabled = true;

            SetField(controller, "isActionInProgress", true);
            Assert.That(controller.TryConvertElements(
                ElementType.Water,
                4,
                out _), Is.False);
            SetField(controller, "isActionInProgress", false);
        }

        [UnityTest]
        public IEnumerator ElementConversion_ReentrantRequestIsRejected()
        {
            yield return InitializeReadyBoard();
            BoardState sourceBoard = CreateElementConversionBoard();
            bool nestedAccepted = true;
            BoardElementConversionResult nestedResult = null;
            var random = new ReentrantRandomSource(() =>
            {
                nestedAccepted = controller.TryConvertElements(
                    ElementType.Water,
                    4,
                    out nestedResult);
            });
            ConfigureElementConversion(
                sourceBoard,
                random,
                maxRandomSelectionAttempts: 1);

            Assert.That(controller.TryConvertElements(
                ElementType.Water,
                4,
                out BoardElementConversionResult result), Is.True);

            Assert.That(nestedAccepted, Is.False);
            Assert.That(nestedResult, Is.Null);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(boardView.MatchesBoard(controller.CurrentBoard),
                Is.True);
        }

        private void ConfigureElementConversion(
            BoardState board,
            IRandomSource conversionRandom,
            int maxRandomSelectionAttempts =
                BoardElementConversionResolver
                    .DefaultMaxRandomSelectionAttempts)
        {
            SetProperty(controller, "CurrentBoard", board);
            SetProperty(controller, "IsBoardReady", true);
            boardView.Render(board);
            SetField(
                controller,
                "elementConversionResolver",
                new BoardElementConversionResolver(
                    conversionRandom,
                    new BoardMoveAnalyzer(),
                    maxRandomSelectionAttempts));
        }

        private Sprite ConfigureDistinctElementSprites()
        {
            var texture = new Texture2D(2, 2);
            createdObjects.Add(texture);
            Sprite fire = CreateSprite(texture);
            Sprite water = CreateSprite(texture);
            Sprite grass = CreateSprite(texture);
            Sprite light = CreateSprite(texture);
            Sprite dark = CreateSprite(texture);
            var spriteSet = (BoardElementSpriteSet)GetField(
                boardView,
                "spriteSet");
            spriteSet.Configure(fire, water, grass, light, dark, fire);
            return water;
        }

        private Sprite CreateSprite(Texture2D texture)
        {
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f));
            createdObjects.Add(sprite);
            return sprite;
        }

        private static BoardState CreateElementConversionBoard()
        {
            int[] elements =
            {
                3, 0, 2, 1, 0,
                0, 2, 3, 1, 2,
                4, 4, 0, 2, 4,
                1, 2, 0, 0, 4,
                0, 1, 2, 1, 2,
                0, 4, 4, 0, 3
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

        private static BoardState CreateUniformBoard(ElementType element)
        {
            var board = new BoardState();
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                board.Set(position, new BoardBlock(
                    index + 1,
                    BoardBlockType.Normal,
                    element));
            }

            return board;
        }

        private static void SetElement(
            BoardState board,
            int x,
            int y,
            ElementType element)
        {
            var position = new BoardPosition(x, y);
            BoardBlock previous = board.Get(position);
            board.Set(position, new BoardBlock(
                previous.RuntimeId,
                previous.BlockType,
                element));
        }

        private static bool ContainsPosition(
            IReadOnlyList<BoardPosition> positions,
            BoardPosition target)
        {
            for (int index = 0; index < positions.Count; index++)
            {
                if (positions[index] == target)
                {
                    return true;
                }
            }

            return false;
        }

        private static BoardState CreateBoard(IRandomSource source)
        {
            return new BoardGenerator(
                source,
                BoardMatchFinder.FindMatches,
                new BoardMoveAnalyzer(),
                new BoardBlockIdGenerator()).Generate();
        }

        private IEnumerator InitializeReadyBoard()
        {
            SetAllDurationsToZero();
            controller.Initialize();
            yield return null;
            Assert.That(controller.IsBoardReady, Is.True);
            Assert.That(controller.CanAcceptBoardInput, Is.True);
        }

        private void SetAllDurationsToZero()
        {
            SetField(controller, "initialDropDuration", 0f);
            SetField(controller, "initialDropColumnStagger", 0f);
            SetField(controller, "swapDuration", 0f);
            SetField(controller, "removalDuration", 0f);
            SetField(controller, "collapseDuration", 0f);
            SetField(controller, "refillDuration", 0f);
            SetField(controller, "shuffleDuration", 0f);
        }

        private static BoardSwap FindValidSwap(BoardState board)
        {
            IReadOnlyList<BoardSwap> swaps =
                new BoardMoveAnalyzer().FindValidSwaps(board);
            Assert.That(swaps, Is.Not.Empty);
            return swaps[0];
        }

        private static BoardSwap FindNoMatchSwap(BoardState board)
        {
            var analyzer = new BoardMoveAnalyzer();
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var first = new BoardPosition(x, y);
                    var candidates = new[]
                    {
                        new BoardPosition(Mathf.Min(x + 1,
                            BoardConstants.Width - 1), y),
                        new BoardPosition(x, Mathf.Min(y + 1,
                            BoardConstants.Height - 1))
                    };
                    foreach (BoardPosition second in candidates)
                    {
                        if (first != second
                            && analyzer.CanSwap(board, first, second)
                            && !analyzer.IsValidSwap(board, first, second))
                        {
                            return new BoardSwap(first, second);
                        }
                    }
                }
            }

            throw new AssertionException("No no-match swap was found.");
        }

        private long GetNextId()
        {
            return (long)GetField(
                GetField(controller, "blockIdGenerator"),
                "nextId");
        }

        private BlockView FirstView()
        {
            foreach (BlockView view in boardView.ActiveViews.Values)
            {
                return view;
            }

            throw new AssertionException("No active BlockView exists.");
        }

        private static void AssertBoardsEquivalent(
            BoardState expected,
            BoardState actual)
        {
            var runtimeIds = new HashSet<long>();
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    BoardBlock expectedBlock = expected.Get(position);
                    BoardBlock actualBlock = actual.Get(position);
                    Assert.That(actualBlock.RuntimeId, Is.EqualTo(
                        expectedBlock.RuntimeId));
                    Assert.That(actualBlock.BlockType, Is.EqualTo(
                        expectedBlock.BlockType));
                    Assert.That(actualBlock.Element, Is.EqualTo(
                        expectedBlock.Element));
                    Assert.That(runtimeIds.Add(actualBlock.RuntimeId), Is.True);
                }
            }

            Assert.That(runtimeIds.Count, Is.EqualTo(
                BoardConstants.CellCount));
        }

        private static BoardBlock[] Capture(BoardState board)
        {
            var blocks = new BoardBlock[BoardConstants.CellCount];
            for (int index = 0; index < blocks.Length; index++)
            {
                blocks[index] = board.Get(BoardPosition.FromIndex(index));
            }

            return blocks;
        }

        private static void AssertOriginalUnchanged(
            BoardState board,
            IReadOnlyList<BoardBlock> original)
        {
            for (int index = 0; index < original.Count; index++)
            {
                Assert.That(board.Get(BoardPosition.FromIndex(index)),
                    Is.SameAs(original[index]));
            }
        }

        private static int CountRocks(BoardState board)
        {
            int count = 0;
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                if (board.Get(BoardPosition.FromIndex(index)).BlockType
                    == BoardBlockType.Rock)
                {
                    count++;
                }
            }

            return count;
        }

        private static BoardState CreateOneMoveBoard()
        {
            int[] elements =
            {
                4, 3, 1, 4, 0,
                4, 1, 0, 4, 0,
                1, 3, 0, 3, 2,
                0, 2, 4, 2, 0,
                3, 3, 1, 4, 1,
                4, 2, 0, 0, 2
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

            Assert.That(BoardMatchFinder.FindMatches(board), Is.Empty);
            Assert.That(new BoardMoveAnalyzer().FindValidSwaps(board),
                Has.Count.EqualTo(1));
            return board;
        }

        private static BoardRockMutationResolver
            CreateForcedShuffleRockResolver()
        {
            var values = new List<int> { 0, 1, 4 };
            for (int index = 26; index >= 1; index--)
            {
                values.Add(index);
            }

            values.AddRange(new[]
            {
                4, 1, 3, 3, 0, 2, 0, 0, 2,
                1, 3, 0, 1, 1, 2, 1, 4, 4,
                3, 3, 4, 2, 3, 2, 4, 2, 3
            });
            var random = new SequenceRandomSource(values);
            var analyzer = new BoardMoveAnalyzer();
            var shuffler = new BoardShuffler(
                random,
                analyzer,
                maxPermutationAttempts: 1,
                maxRegenerationAttempts: 1);
            return new BoardRockMutationResolver(
                random,
                new BoardBlockIdGenerator(1000),
                analyzer,
                shuffler,
                maxPlacementAttempts: 1);
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(
                    target,
                    value);
        }

        private static object GetField(object target, string name)
        {
            return target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(
                    target);
        }

        private static object InvokePrivate(
            object target,
            string name,
            params object[] arguments)
        {
            return target.GetType().GetMethod(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(
                    target,
                    arguments);
        }

        private sealed class ThrowingEnumerator : IEnumerator
        {
            private readonly Exception exception;

            public ThrowingEnumerator(Exception exception)
            {
                this.exception = exception;
            }

            public object Current => null;

            public bool MoveNext()
            {
                throw exception;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }
        }

        private static void SetProperty(
            object target,
            string name,
            object value)
        {
            target.GetType().GetProperty(
                name,
                BindingFlags.Instance
                    | BindingFlags.Public
                    | BindingFlags.NonPublic).SetValue(target, value);
        }

        private static void SetStaticProperty(
            System.Type type,
            string name,
            object value)
        {
            type.GetProperty(
                name,
                BindingFlags.Static
                    | BindingFlags.Public
                    | BindingFlags.NonPublic).SetValue(null, value);
        }

        private sealed class CountingRandomSource : IRandomSource
        {
            private readonly SeededRandomSource source;

            public CountingRandomSource(int seed)
            {
                source = new SeededRandomSource(seed);
            }

            public int NextCallCount { get; private set; }

            public int Next(int minInclusive, int maxExclusive)
            {
                NextCallCount++;
                return source.Next(minInclusive, maxExclusive);
            }

            public float NextFloat()
            {
                return source.NextFloat();
            }
        }

        private sealed class SequenceRandomSource : IRandomSource
        {
            private readonly IReadOnlyList<int> values;
            private int index;

            public SequenceRandomSource(IReadOnlyList<int> values)
            {
                this.values = values;
            }

            public int Next(int minInclusive, int maxExclusive)
            {
                if (index >= values.Count)
                {
                    throw new InvalidOperationException(
                        "The deterministic random sequence was exhausted.");
                }

                int value = values[index++];
                if (value < minInclusive || value >= maxExclusive)
                {
                    throw new InvalidOperationException(
                        $"Random value {value} is outside "
                            + $"[{minInclusive}, {maxExclusive}).");
                }

                return value;
            }

            public float NextFloat()
            {
                return 0f;
            }
        }

        private sealed class ReentrantRandomSource : IRandomSource
        {
            private readonly Action onFirstNext;
            private bool invoked;

            public ReentrantRandomSource(Action onFirstNext)
            {
                this.onFirstNext = onFirstNext;
            }

            public int Next(int minInclusive, int maxExclusive)
            {
                if (!invoked)
                {
                    invoked = true;
                    onFirstNext();
                }

                return minInclusive;
            }

            public float NextFloat()
            {
                return 0f;
            }
        }
    }
}
