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
            spriteSet.Configure(sprite, sprite, sprite, sprite, sprite);
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
            SetField(controller, "initialDropDuration", 0f);
            SetField(controller, "initialDropColumnStagger", 0f);

            controller.Initialize();
            Assert.That(controller.CurrentBoard, Is.Not.Null);

            yield return null;

            Assert.That(controller.IsBoardReady, Is.True);
            Assert.That(boardView.IsAnimating, Is.False);
            foreach (BlockView view in boardView.ActiveViews.Values)
            {
                Assert.That(view.IsInputEnabled, Is.True);
            }
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
            object resolver = GetField(controller, "swapActionResolver");
            Assert.That(GetField(controller, "randomSource"),
                Is.SameAs(randomSource));
            Assert.That(GetField(generator, "randomSource"),
                Is.SameAs(randomSource));
            Assert.That(GetField(refiller, "randomSource"),
                Is.SameAs(randomSource));
            Assert.That(GetField(shuffler, "randomSource"),
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
        }

        [UnityTest]
        public IEnumerator TryExecuteSwap_NotSwappableConsumesNoDependencies()
        {
            yield return InitializeReadyBoard();
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
        }

        [UnityTest]
        public IEnumerator TryExecuteSwap_NoMatchReturnsToReadyWithoutConsumption()
        {
            yield return InitializeReadyBoard();
            BoardSwap swap = FindNoMatchSwap(controller.CurrentBoard);
            int randomCalls = randomSource.NextCallCount;
            long nextId = GetNextId();

            Assert.That(controller.TryExecuteSwap(swap), Is.True);
            yield return null;

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
        }

        [UnityTest]
        public IEnumerator TryExecuteSwap_ResolvedUsesContinuousRefillIds()
        {
            yield return InitializeReadyBoard();
            BoardSwap swap = FindValidSwap(controller.CurrentBoard);
            object resolver = GetField(controller, "swapActionResolver");
            object idGenerator = GetField(controller, "blockIdGenerator");

            Assert.That(controller.TryExecuteSwap(swap), Is.True);
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

        private static void InvokePrivate(object target, string name)
        {
            target.GetType().GetMethod(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(
                    target,
                    null);
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
    }
}
