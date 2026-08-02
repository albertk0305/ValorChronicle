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
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board.Presentation
{
    public sealed class BoardActionSequencePresentationTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();
        private GameObject root;
        private BattleBoardView boardView;
        private BlockViewPool pool;
        private BoardElementSpriteSet spriteSet;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("ActionSequenceTest", typeof(RectTransform));
            createdObjects.Add(root);

            Sprite fire = CreateSprite(Color.red);
            Sprite water = CreateSprite(Color.blue);
            Sprite grass = CreateSprite(Color.green);
            Sprite light = CreateSprite(Color.white);
            Sprite dark = CreateSprite(Color.black);
            spriteSet = ScriptableObject.CreateInstance<
                BoardElementSpriteSet>();
            spriteSet.Configure(fire, water, grass, light, dark);
            createdObjects.Add(spriteSet);

            var prefabObject = new GameObject(
                "BlockViewTemplate",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(BlockView));
            createdObjects.Add(prefabObject);
            Image image = prefabObject.GetComponent<Image>();
            prefabObject.GetComponent<BlockView>().Configure(image);
            prefabObject.SetActive(false);

            pool = root.AddComponent<BlockViewPool>();
            pool.Configure(
                prefabObject.GetComponent<BlockView>(),
                root.GetComponent<RectTransform>(),
                BoardConstants.CellCount);
            boardView = root.AddComponent<BattleBoardView>();
            boardView.Configure(spriteSet, pool);
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void ShuffleTransition_NoneDoesNothing()
        {
            BoardShuffleResult result =
                BoardShufflePresentationTestSupport.CreateNone(
                    out BoardState board);
            boardView.Render(board);
            var views = CaptureViews();
            int createdCount = pool.TotalCreatedCount;

            IEnumerator animation = boardView.PlayShuffleTransition(
                board,
                result,
                1f);

            Assert.That(animation.MoveNext(), Is.False);
            Assert.That(boardView.IsAnimating, Is.False);
            Assert.That(pool.TotalCreatedCount, Is.EqualTo(createdCount));
            AssertSameViews(views);
        }

        [TestCase(BoardShuffleKind.Permutation)]
        [TestCase(BoardShuffleKind.Regeneration)]
        public void ShuffleTransition_ZeroDurationChangesOnlySprites(
            BoardShuffleKind kind)
        {
            BoardShuffleResult result =
                BoardShufflePresentationTestSupport.CreateChanged(
                    kind,
                    out BoardState board);
            boardView.Render(board);
            var views = CaptureViews();
            var positions = CapturePositions();
            int createdCount = pool.TotalCreatedCount;

            IEnumerator animation = boardView.PlayShuffleTransition(
                board,
                result,
                0f);
            Assert.That(animation.MoveNext(), Is.False);

            AssertViewsMatchBoard(result.Board);
            AssertSameViews(views);
            foreach (KeyValuePair<long, BlockView> pair
                in boardView.ActiveViews)
            {
                Assert.That(pair.Value.RectTransform.anchoredPosition,
                    Is.EqualTo(positions[pair.Key]));
                Assert.That(pair.Value.RectTransform.localScale,
                    Is.EqualTo(Vector3.one));
                Assert.That(pair.Value.IsInputEnabled, Is.True);
            }

            Assert.That(pool.TotalCreatedCount, Is.EqualTo(createdCount));
            Assert.That(pool.ActiveCount, Is.EqualTo(30));
        }

        [Test]
        public void ShuffleTransition_RestoresPreviousInputState()
        {
            BoardShuffleResult result =
                BoardShufflePresentationTestSupport.CreateChanged(
                    BoardShuffleKind.Permutation,
                    out BoardState board);
            boardView.Render(board);
            BlockView disabled = boardView.ActiveViews[1];
            disabled.SetInputEnabled(false);

            IEnumerator animation = boardView.PlayShuffleTransition(
                board,
                result,
                0f);
            Assert.That(animation.MoveNext(), Is.False);

            Assert.That(disabled.IsInputEnabled, Is.False);
            foreach (BlockView view in boardView.ActiveViews.Values)
            {
                if (view != disabled)
                {
                    Assert.That(view.IsInputEnabled, Is.True);
                }
            }
        }

        [Test]
        public void ActionSequence_NotSwappableEndsImmediatelyWithoutLocking()
        {
            BoardSwapActionResult result =
                BoardSwapPresentationTestSupport.CreateNotSwappable(
                    out BoardState board);
            boardView.Render(board);
            int createdCount = pool.TotalCreatedCount;

            IEnumerator animation = boardView.PlaySwapActionSequence(
                board,
                result,
                ZeroTimings());

            Assert.That(animation.MoveNext(), Is.False);
            Assert.That(boardView.IsAnimating, Is.False);
            Assert.That(pool.TotalCreatedCount, Is.EqualTo(createdCount));
            AssertAllInput(true);
            AssertViewsMatchBoard(board);
        }

        [Test]
        public void ActionSequence_NoMatchPlaysForwardAndBackToResultBoard()
        {
            BoardSwapActionResult result =
                BoardSwapPresentationTestSupport.CreateNoMatch(
                    out BoardState board);
            boardView.Render(board);
            var views = CaptureViews();

            IEnumerator animation = boardView.PlaySwapActionSequence(
                board,
                result,
                ZeroTimings());
            Assert.That(animation.MoveNext(), Is.False);

            AssertViewsMatchBoard(result.Board);
            AssertSameViews(views);
            Assert.That(boardView.IsAnimating, Is.False);
            AssertAllInput(true);
        }

        [Test]
        public void ActionSequence_ResolvedSingleStepEndsAtFinalBoard()
        {
            BoardSwapActionResult result =
                BoardSwapPresentationTestSupport.CreateResolved(
                    out BoardState board);
            Assert.That(result.Cascade.CascadeCount, Is.EqualTo(1));
            boardView.Render(board);

            IEnumerator animation = boardView.PlaySwapActionSequence(
                board,
                result,
                ZeroTimings());
            Assert.That(animation.MoveNext(), Is.False);

            AssertViewsMatchBoard(result.Board);
            Assert.That(boardView.IsAnimating, Is.False);
            Assert.That(pool.TotalCreatedCount, Is.EqualTo(30));
            AssertAllInput(true);
        }

        [Test]
        public void ActionSequence_ResolvedNaturalChainPlaysEveryStepInOrder()
        {
            BoardSwapActionResult result =
                ActionSequenceTestSupport.CreateMultiStep(
                    out BoardState board);
            Assert.That(result.Cascade.CascadeCount,
                Is.GreaterThanOrEqualTo(2));
            boardView.Render(board);

            IEnumerator animation = boardView.PlaySwapActionSequence(
                board,
                result,
                ZeroTimings());
            Assert.That(animation.MoveNext(), Is.False);

            AssertViewsMatchBoard(result.Cascade.Steps[
                result.Cascade.Steps.Count - 1].Board);
            AssertViewsMatchBoard(result.Board);
            Assert.That(pool.TotalCreatedCount, Is.EqualTo(30));
        }

        [UnityTest]
        public IEnumerator ActionSequence_NoMatchNonZeroCompletesNaturally()
        {
            BoardSwapActionResult result =
                BoardSwapPresentationTestSupport.CreateNoMatch(
                    out BoardState board);
            boardView.Render(board);
            IEnumerator animation = boardView.PlaySwapActionSequence(
                board,
                result,
                new BoardActionPresentationTimings(
                    0.01f, 0f, 0f, 0f, 0f));

            yield return RunNaturally(animation, 300);

            Assert.That(boardView.IsAnimating, Is.False);
            AssertViewsMatchBoard(result.Board);
            AssertAllInput(true);
        }

        [UnityTest]
        public IEnumerator ActionSequence_SingleCascadeNonZeroCompletesNaturally()
        {
            BoardSwapActionResult result =
                BoardSwapPresentationTestSupport.CreateResolved(
                    out BoardState board);
            Assert.That(result.Cascade.CascadeCount, Is.EqualTo(1));
            boardView.Render(board);
            IEnumerator animation = boardView.PlaySwapActionSequence(
                board,
                result,
                new BoardActionPresentationTimings(
                    0.005f,
                    0.005f,
                    0.005f,
                    0.005f,
                    0f));

            yield return RunNaturally(animation, 600);

            Assert.That(boardView.IsAnimating, Is.False);
            AssertViewsMatchBoard(result.Board);
            AssertAllInput(true);
        }

        [UnityTest]
        public IEnumerator ActionSequence_MultiCascadeNonZeroCompletesNaturally()
        {
            BoardSwapActionResult result =
                ActionSequenceTestSupport.CreateMultiStep(
                    out BoardState board);
            Assert.That(result.Cascade.CascadeCount,
                Is.GreaterThanOrEqualTo(2));
            AssertSpawnedRuntimeIdIsProcessedByNextStep(result);
            boardView.Render(board);
            IEnumerator animation = boardView.PlaySwapActionSequence(
                board,
                result,
                new BoardActionPresentationTimings(
                    0.005f,
                    0.005f,
                    0.005f,
                    0.005f,
                    0.005f));

            yield return RunNaturally(animation, 900);

            Assert.That(boardView.IsAnimating, Is.False);
            AssertViewsMatchBoard(result.Board);
            Assert.That(new HashSet<BlockView>(
                boardView.ActiveViews.Values).Count, Is.EqualTo(30));
            Assert.That(pool.ActiveCount, Is.EqualTo(30));
            AssertAllInput(true);
        }

        [UnityTest]
        public IEnumerator ActionSequence_TwoNonZeroActionsCompleteConsecutively()
        {
            BoardSwapActionResult result =
                BoardSwapPresentationTestSupport.CreateNoMatch(
                    out BoardState board);
            boardView.Render(board);
            var timings = new BoardActionPresentationTimings(
                0.005f, 0f, 0f, 0f, 0f);

            yield return RunNaturally(
                boardView.PlaySwapActionSequence(board, result, timings),
                300);
            AssertViewsMatchBoard(result.Board);

            yield return RunNaturally(
                boardView.PlaySwapActionSequence(
                    result.Board,
                    result,
                    timings),
                300);

            Assert.That(boardView.IsAnimating, Is.False);
            AssertViewsMatchBoard(result.Board);
            AssertAllInput(true);
        }

        [Test]
        public void ActionSequence_KeepsOneInputLockIntoShuffle()
        {
            BoardSwapActionResult result =
                ActionSequenceTestSupport.CreateResolvedWithShuffle(
                    out BoardState board);
            boardView.Render(board);
            var timings = new BoardActionPresentationTimings(
                0f,
                0f,
                0f,
                0f,
                1f);

            IEnumerator animation = boardView.PlaySwapActionSequence(
                board,
                result,
                timings);
            Assert.That(animation.MoveNext(), Is.True);

            Assert.That(boardView.IsAnimating, Is.True);
            AssertAllInput(false);
            Dispose(animation);
            AssertViewsMatchBoard(result.Board);
            AssertAllInput(true);
        }

        [TestCase("swap")]
        [TestCase("removal")]
        [TestCase("collapse")]
        [TestCase("refill")]
        [TestCase("shuffle")]
        [TestCase("no_match")]
        public void ActionSequence_InterruptionStabilizesFinalBoard(
            string phase)
        {
            BoardState board;
            BoardSwapActionResult result;
            if (phase == "shuffle")
            {
                result = ActionSequenceTestSupport.CreateResolvedWithShuffle(
                    out board);
            }
            else if (phase == "no_match")
            {
                result = BoardSwapPresentationTestSupport.CreateNoMatch(
                    out board);
            }
            else
            {
                result = BoardSwapPresentationTestSupport.CreateResolved(
                    out board);
            }

            boardView.Render(board);
            BoardActionPresentationTimings timings;
            if (phase == "swap" || phase == "no_match")
            {
                timings = new BoardActionPresentationTimings(
                    1f, 0f, 0f, 0f, 0f);
            }
            else if (phase == "removal")
            {
                timings = new BoardActionPresentationTimings(
                    0f, 1f, 0f, 0f, 0f);
            }
            else if (phase == "collapse")
            {
                timings = new BoardActionPresentationTimings(
                    0f, 0f, 1f, 0f, 0f);
            }
            else if (phase == "refill")
            {
                timings = new BoardActionPresentationTimings(
                    0f, 0f, 0f, 1f, 0f);
            }
            else
            {
                timings = new BoardActionPresentationTimings(
                    0f, 0f, 0f, 0f, 1f);
            }

            IEnumerator animation = boardView.PlaySwapActionSequence(
                board,
                result,
                timings);
            Assert.That(animation.MoveNext(), Is.True);
            InvokeOnDisable();
            Dispose(animation);

            Assert.That(boardView.IsAnimating, Is.False);
            AssertViewsMatchBoard(result.Board);
            Assert.That(pool.ActiveCount, Is.EqualTo(30));
            Assert.That(pool.TotalCreatedCount, Is.EqualTo(30));
            AssertAllInput(true);
        }

        [Test]
        public void ActionSequence_SecondaryStabilizationFailurePreservesFirstException()
        {
            BoardSwapActionResult result =
                BoardSwapPresentationTestSupport.CreateNoMatch(
                    out BoardState board);
            boardView.Render(board);
            IEnumerator animation = boardView.PlaySwapActionSequence(
                board,
                result,
                ZeroTimings());
            Dictionary<long, BlockView> views = GetMutableViews();
            BlockView aliasedView = boardView.ActiveViews[
                board.Get(result.Swap.First).RuntimeId];
            views.Add(9999, aliasedView);

            LogAssert.Expect(
                LogType.Exception,
                new Regex(
                    "Context=StabilizeActionSequence\\.Start; " +
                    "Field=ViewAlias"));
            LogAssert.Expect(
                LogType.Error,
                "[BattleBoard] Secondary action stabilization failure. " +
                "The original action exception, if any, remains authoritative.");

            InvalidOperationException exception = Assert.Throws<
                InvalidOperationException>(() => animation.MoveNext());

            Assert.That(exception.Message,
                Does.Contain("Context=SwapForwardComplete"));
            Assert.That(exception.Message, Does.Contain("Field=ViewAlias"));
            Assert.That(exception.Message,
                Does.Not.Contain("Context=StabilizeActionSequence.Start"));
            Assert.That(boardView.IsAnimating, Is.False);
            Assert.That(GetPrivateField("activeAnimation").ToString(),
                Is.EqualTo("None"));
            Assert.That(GetPrivateField("activeActionFinalBoard"), Is.Null);
            AssertAllInput(true);
        }

        [TestCase(-1f, 0f, 0f, 0f, 0f)]
        [TestCase(0f, -1f, 0f, 0f, 0f)]
        [TestCase(0f, 0f, -1f, 0f, 0f)]
        [TestCase(0f, 0f, 0f, -1f, 0f)]
        [TestCase(0f, 0f, 0f, 0f, -1f)]
        public void Timings_NegativeValueIsRejected(
            float swap,
            float removal,
            float collapse,
            float refill,
            float shuffle)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BoardActionPresentationTimings(
                    swap,
                    removal,
                    collapse,
                    refill,
                    shuffle));
        }

        [Test]
        public void ShuffleTransition_NegativeDurationIsRejected()
        {
            BoardShuffleResult result =
                BoardShufflePresentationTestSupport.CreateNone(
                    out BoardState board);
            boardView.Render(board);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                boardView.PlayShuffleTransition(board, result, -0.1f));
        }

        private BoardActionPresentationTimings ZeroTimings()
        {
            return new BoardActionPresentationTimings(0f, 0f, 0f, 0f, 0f);
        }

        private static IEnumerator RunNaturally(
            IEnumerator animation,
            int maximumFrames)
        {
            int frameCount = 0;
            while (animation.MoveNext())
            {
                Assert.That(++frameCount, Is.LessThan(maximumFrames),
                    "EditMode Time.deltaTime did not advance the animation.");
                yield return animation.Current;
            }
        }

        private static void AssertSpawnedRuntimeIdIsProcessedByNextStep(
            BoardSwapActionResult result)
        {
            for (int stepIndex = 0;
                stepIndex < result.Cascade.Steps.Count - 1;
                stepIndex++)
            {
                var spawnedIds = new HashSet<long>();
                foreach (BoardBlockSpawn spawn
                    in result.Cascade.Steps[stepIndex].Refill.Spawns)
                {
                    spawnedIds.Add(spawn.RuntimeId);
                }

                BoardCascadeStep nextStep =
                    result.Cascade.Steps[stepIndex + 1];
                foreach (BoardBlockRemoval removal
                    in nextStep.Collapse.Removals)
                {
                    if (spawnedIds.Contains(removal.RuntimeId))
                    {
                        return;
                    }
                }

                foreach (BoardBlockMove move in nextStep.Collapse.Moves)
                {
                    if (spawnedIds.Contains(move.RuntimeId))
                    {
                        return;
                    }
                }
            }

            Assert.Fail(
                "The multi-step fixture must process a RuntimeId spawned " +
                "by the preceding cascade step.");
        }

        private Dictionary<long, BlockView> CaptureViews()
        {
            return new Dictionary<long, BlockView>(boardView.ActiveViews);
        }

        private Dictionary<long, BlockView> GetMutableViews()
        {
            return (Dictionary<long, BlockView>)GetPrivateField(
                "viewsByRuntimeId");
        }

        private object GetPrivateField(string name)
        {
            FieldInfo field = typeof(BattleBoardView).GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            return field.GetValue(boardView);
        }

        private Dictionary<long, Vector2> CapturePositions()
        {
            var positions = new Dictionary<long, Vector2>();
            foreach (KeyValuePair<long, BlockView> pair
                in boardView.ActiveViews)
            {
                positions.Add(
                    pair.Key,
                    pair.Value.RectTransform.anchoredPosition);
            }

            return positions;
        }

        private void AssertSameViews(Dictionary<long, BlockView> expected)
        {
            foreach (KeyValuePair<long, BlockView> pair in expected)
            {
                Assert.That(boardView.ActiveViews[pair.Key],
                    Is.SameAs(pair.Value));
            }
        }

        private void AssertViewsMatchBoard(BoardState board)
        {
            Assert.That(boardView.ActiveViewCount, Is.EqualTo(30));
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                BoardBlock block = board.Get(position);
                Assert.That(boardView.TryGetView(
                    block.RuntimeId,
                    out BlockView view), Is.True);
                Assert.That(view.RuntimeId, Is.EqualTo(block.RuntimeId));
                Assert.That(view.Position, Is.EqualTo(position));
                Assert.That(view.RectTransform.anchoredPosition, Is.EqualTo(
                    BoardViewLayout.GetAnchoredPosition(position)));
                Assert.That(view.RectTransform.localScale,
                    Is.EqualTo(Vector3.one));
                Assert.That(view.Image.sprite, Is.SameAs(
                    spriteSet.GetSprite(block.Element.Value)));
            }
        }

        private void AssertAllInput(bool expected)
        {
            foreach (BlockView view in boardView.ActiveViews.Values)
            {
                Assert.That(view.IsInputEnabled, Is.EqualTo(expected));
            }
        }

        private void InvokeOnDisable()
        {
            typeof(BattleBoardView).GetMethod(
                "OnDisable",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(
                    boardView,
                    null);
        }

        private static void Dispose(IEnumerator animation)
        {
            if (animation is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private Sprite CreateSprite(Color color)
        {
            var texture = new Texture2D(2, 2);
            texture.SetPixels(new[] { color, color, color, color });
            texture.Apply();
            createdObjects.Add(texture);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f));
            createdObjects.Add(sprite);
            return sprite;
        }
    }

    internal static class ActionSequenceTestSupport
    {
        private static readonly int[] PlayableBoardElements =
        {
            0, 0, 1, 4, 0,
            4, 0, 4, 3, 4,
            1, 3, 0, 3, 1,
            0, 4, 1, 1, 4,
            0, 0, 2, 4, 4,
            1, 2, 4, 0, 0
        };

        public static BoardSwapActionResult CreateResolvedWithShuffle(
            out BoardState board)
        {
            BoardSwapActionResult original =
                BoardSwapPresentationTestSupport.CreateResolved(out board);
            BoardState shuffledBoard = original.Cascade.Board.Clone();
            var entries = new List<BoardShuffleEntry>();
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                BoardBlock block = original.Cascade.Board.Get(position);
                ElementType previous = block.Element.Value;
                ElementType next = (ElementType)(((int)previous + 1) % 5);
                shuffledBoard.Set(
                    position,
                    new BoardBlock(
                        block.RuntimeId,
                        BoardBlockType.Normal,
                        next));
                entries.Add(BoardShufflePresentationTestSupport.CreateEntry(
                    position,
                    block.RuntimeId,
                    previous,
                    next));
            }

            BoardShuffleResult shuffle =
                BoardShufflePresentationTestSupport.CreateResult(
                    shuffledBoard,
                    BoardShuffleKind.Regeneration,
                    entries);
            return CreateInternal<BoardSwapActionResult>(
                original.Swap,
                BoardSwapActionStatus.Resolved,
                original.SwappedBoard,
                original.Cascade,
                shuffle,
                shuffledBoard);
        }

        public static BoardSwapActionResult CreateMultiStep(
            out BoardState board)
        {
            board = CreateTwoStepSwapBoard();
            var sequence = new List<int>
            {
                0, 0, 0, 0, 1, 2
            };
            sequence.AddRange(CreateIdentityPermutationSequence(30));
            sequence.AddRange(PlayableBoardElements);
            var random = new QueueRandomSource(sequence);
            var moveAnalyzer = new BoardMoveAnalyzer();
            var refiller = new BoardRefiller(
                random,
                new BoardBlockIdGenerator(31));
            var cascadeResolver = new BoardCascadeResolver(
                new BoardCascadeStepResolver(refiller));
            var shuffler = new BoardShuffler(
                random,
                moveAnalyzer,
                1,
                1);
            var resolver = new BoardSwapActionResolver(
                moveAnalyzer,
                cascadeResolver,
                shuffler);
            return resolver.Resolve(
                board,
                new BoardSwap(
                    new BoardPosition(2, 0),
                    new BoardPosition(3, 0)));
        }

        private static BoardState CreateTwoStepSwapBoard()
        {
            var elements = new int[BoardConstants.CellCount];
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    elements[(x * BoardConstants.Height) + y] = (x + y) % 5;
                }
            }

            BoardState board = CreateBoard(elements);
            SetElement(board, 0, 0, ElementType.Dark);
            SetElement(board, 1, 0, ElementType.Dark);
            SetElement(board, 2, 0, ElementType.Fire);
            SetElement(board, 3, 0, ElementType.Dark);
            return board;
        }

        private static BoardState CreateBoard(int[] elements)
        {
            var board = new BoardState();
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    board.Set(
                        position,
                        new BoardBlock(
                            position.ToIndex() + 1,
                            BoardBlockType.Normal,
                            (ElementType)elements[
                                (x * BoardConstants.Height) + y]));
                }
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
            long runtimeId = board.Get(position).RuntimeId;
            board.Set(
                position,
                new BoardBlock(
                    runtimeId,
                    BoardBlockType.Normal,
                    element));
        }

        private static int[] CreateIdentityPermutationSequence(int count)
        {
            var values = new int[Math.Max(0, count - 1)];
            for (int index = count - 1; index >= 1; index--)
            {
                values[count - 1 - index] = index;
            }

            return values;
        }

        private static T CreateInternal<T>(params object[] arguments)
        {
            return (T)Activator.CreateInstance(
                typeof(T),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                arguments,
                null);
        }

        private sealed class QueueRandomSource : IRandomSource
        {
            private readonly Queue<int> values;

            public QueueRandomSource(IEnumerable<int> values)
            {
                this.values = new Queue<int>(values);
            }

            public int Next(int minInclusive, int maxExclusive)
            {
                if (values.Count == 0)
                {
                    throw new InvalidOperationException(
                        "No queued random value remains.");
                }

                int value = values.Dequeue();
                if (value < minInclusive || value >= maxExclusive)
                {
                    throw new InvalidOperationException(
                        $"Queued value {value} is outside the requested range.");
                }

                return value;
            }

            public float NextFloat()
            {
                throw new NotSupportedException();
            }
        }
    }
}
