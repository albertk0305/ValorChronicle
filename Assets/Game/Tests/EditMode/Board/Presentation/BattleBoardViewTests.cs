using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board.Presentation
{
    public sealed class BattleBoardViewTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();
        private GameObject root;
        private GameObject prefabObject;
        private BattleBoardView boardView;
        private BlockViewPool pool;
        private BoardElementSpriteSet spriteSet;
        private Sprite fire;
        private Sprite water;
        private Sprite grass;
        private Sprite light;
        private Sprite dark;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject(
                "BoardViewTestRoot",
                typeof(RectTransform));
            createdObjects.Add(root);

            fire = CreateSprite(Color.red);
            water = CreateSprite(Color.blue);
            grass = CreateSprite(Color.green);
            light = CreateSprite(Color.white);
            dark = CreateSprite(Color.black);

            spriteSet = ScriptableObject.CreateInstance<
                BoardElementSpriteSet>();
            spriteSet.Configure(fire, water, grass, light, dark);
            createdObjects.Add(spriteSet);

            prefabObject = new GameObject(
                "BlockViewTemplate",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(BlockView));
            createdObjects.Add(prefabObject);
            var prefabRect =
                prefabObject.GetComponent<RectTransform>();
            prefabRect.sizeDelta = new Vector2(180f, 180f);
            var prefabImage = prefabObject.GetComponent<Image>();
            prefabObject.GetComponent<BlockView>().Configure(prefabImage);
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
                    UnityEngine.Object.DestroyImmediate(
                        createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void SpriteSet_MapsAllFiveElements()
        {
            Assert.That(spriteSet.GetSprite(ElementType.Fire), Is.SameAs(fire));
            Assert.That(spriteSet.GetSprite(ElementType.Water), Is.SameAs(water));
            Assert.That(spriteSet.GetSprite(ElementType.Grass), Is.SameAs(grass));
            Assert.That(spriteSet.GetSprite(ElementType.Light), Is.SameAs(light));
            Assert.That(spriteSet.GetSprite(ElementType.Dark), Is.SameAs(dark));
        }

        [Test]
        public void SpriteSet_MissingSpriteThrowsClearException()
        {
            spriteSet.Configure(fire, water, null, light, dark);

            InvalidOperationException exception = Assert.Throws<
                InvalidOperationException>(
                () => spriteSet.GetSprite(ElementType.Grass));

            Assert.That(exception.Message, Does.Contain("Grass"));
        }

        [Test]
        public void Render_FullBoardCreatesThirtyMappedViews()
        {
            BoardState board = CreateFullBoard();

            boardView.Render(board);

            Assert.That(boardView.ActiveViewCount, Is.EqualTo(
                BoardConstants.CellCount));
            Assert.That(pool.ActiveCount, Is.EqualTo(
                BoardConstants.CellCount));

            var uniqueViews = new HashSet<BlockView>();
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    BoardBlock block = board.Get(position);
                    Assert.That(
                        boardView.TryGetView(block.RuntimeId, out BlockView view),
                        Is.True);
                    Assert.That(view.RuntimeId, Is.EqualTo(block.RuntimeId));
                    Assert.That(view.Position, Is.EqualTo(position));
                    Assert.That(view.RectTransform.anchoredPosition, Is.EqualTo(
                        BoardViewLayout.GetAnchoredPosition(position)));
                    uniqueViews.Add(view);
                }
            }

            Assert.That(uniqueViews.Count, Is.EqualTo(
                BoardConstants.CellCount));
        }

        [Test]
        public void Render_SameBoardReusesEveryView()
        {
            BoardState board = CreateFullBoard();
            boardView.Render(board);
            var firstViews = new Dictionary<long, BlockView>();
            foreach (KeyValuePair<long, BlockView> pair
                in boardView.ActiveViews)
            {
                firstViews.Add(pair.Key, pair.Value);
            }

            boardView.Render(board);

            Assert.That(pool.TotalCreatedCount, Is.EqualTo(
                BoardConstants.CellCount));
            foreach (KeyValuePair<long, BlockView> pair in firstViews)
            {
                Assert.That(
                    boardView.ActiveViews[pair.Key],
                    Is.SameAs(pair.Value));
            }
        }

        [Test]
        public void Render_SameRuntimeIdWithNewElementUpdatesSprite()
        {
            BoardState board = CreateFullBoard();
            var position = new BoardPosition(2, 3);
            long runtimeId = board.Get(position).RuntimeId;
            boardView.Render(board);
            BlockView originalView = boardView.ActiveViews[runtimeId];

            BoardState changed = board.Clone();
            changed.Set(
                position,
                new BoardBlock(
                    runtimeId,
                    BoardBlockType.Normal,
                    ElementType.Dark));
            boardView.Render(changed);

            Assert.That(
                boardView.ActiveViews[runtimeId],
                Is.SameAs(originalView));
            Assert.That(originalView.Image.sprite, Is.SameAs(dark));
        }

        [Test]
        public void Render_DoesNotMutateInputBoard()
        {
            BoardState board = CreateFullBoard();
            var originalBlocks = new BoardBlock[BoardConstants.CellCount];
            for (int index = 0; index < originalBlocks.Length; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                originalBlocks[index] = board.Get(position);
            }

            boardView.Render(board);

            for (int index = 0; index < originalBlocks.Length; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                Assert.That(
                    board.Get(position),
                    Is.SameAs(originalBlocks[index]));
            }
        }

        [Test]
        public void Render_RemovedViewReturnsToPoolAndIsReusedWithoutDuplication()
        {
            BoardState fullBoard = CreateFullBoard();
            boardView.Render(fullBoard);
            var removedPosition = new BoardPosition(5, 4);
            long removedId = fullBoard.Get(removedPosition).RuntimeId;
            BlockView removedView = boardView.ActiveViews[removedId];

            BoardState missingOne = fullBoard.Clone();
            missingOne.Clear(removedPosition);
            boardView.Render(missingOne);

            Assert.That(boardView.ActiveViewCount, Is.EqualTo(29));
            Assert.That(removedView.gameObject.activeSelf, Is.False);

            missingOne.Set(
                removedPosition,
                new BoardBlock(
                    1000,
                    BoardBlockType.Normal,
                    ElementType.Fire));
            boardView.Render(missingOne);

            Assert.That(boardView.ActiveViewCount, Is.EqualTo(30));
            Assert.That(boardView.ActiveViews[1000], Is.SameAs(removedView));
            Assert.That(new HashSet<BlockView>(
                boardView.ActiveViews.Values).Count, Is.EqualTo(30));
        }

        [Test]
        public void ActiveViews_CannotBeModifiedExternally()
        {
            boardView.Render(CreateFullBoard());
            var dictionary =
                (IDictionary<long, BlockView>)boardView.ActiveViews;

            Assert.Throws<NotSupportedException>(
                () => dictionary.Add(2000, dictionary[1]));
        }

        [Test]
        public void Render_DuplicateRuntimeIdIsRejected()
        {
            BoardState board = CreateFullBoard();
            long duplicateId =
                board.Get(new BoardPosition(0, 0)).RuntimeId;
            board.Set(
                new BoardPosition(1, 0),
                new BoardBlock(
                    duplicateId,
                    BoardBlockType.Normal,
                    ElementType.Water));

            Assert.Throws<InvalidOperationException>(
                () => boardView.Render(board));
            Assert.That(boardView.ActiveViewCount, Is.Zero);
        }

        [Test]
        public void Render_UnsupportedBlockTypeIsRejected()
        {
            BoardState board = CreateFullBoard();
            board.Set(
                new BoardPosition(0, 0),
                new BoardBlock(1, BoardBlockType.Rock, null));

            Assert.Throws<NotSupportedException>(
                () => boardView.Render(board));
            Assert.That(boardView.ActiveViewCount, Is.Zero);
        }

        [Test]
        public void RenderInitialDrop_PreparesThirtyViewsAtSourcePositions()
        {
            BoardState board = CreateFullBoard();

            IEnumerator animation = boardView.RenderInitialDrop(
                board,
                1f,
                0.1f);

            Assert.That(boardView.IsAnimating, Is.True);
            Assert.That(boardView.ActiveViewCount, Is.EqualTo(
                BoardConstants.CellCount));
            Assert.That(pool.ActiveCount, Is.EqualTo(
                BoardConstants.CellCount));

            var uniqueViews = new HashSet<BlockView>();
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var target = new BoardPosition(x, y);
                    BoardBlock block = board.Get(target);
                    Assert.That(boardView.TryGetView(
                        block.RuntimeId,
                        out BlockView view), Is.True);
                    Assert.That(view.RuntimeId, Is.EqualTo(block.RuntimeId));
                    Assert.That(view.Position, Is.EqualTo(target));
                    Assert.That(view.Image.sprite, Is.SameAs(
                        spriteSet.GetSprite(block.Element.Value)));
                    Assert.That(view.RectTransform.anchoredPosition, Is.EqualTo(
                        BoardViewLayout.GetAnchoredPosition(
                            x,
                            BoardConstants.Height + y)));
                    Assert.That(view.IsInputEnabled, Is.False);
                    uniqueViews.Add(view);
                }
            }

            Assert.That(uniqueViews.Count, Is.EqualTo(
                BoardConstants.CellCount));
            Assert.That(animation.MoveNext(), Is.True);
            Dispose(animation);
        }

        [Test]
        public void RenderInitialDrop_ZeroDurationSnapsToStaticRenderResult()
        {
            BoardState board = CreateFullBoard();
            IEnumerator animation = boardView.RenderInitialDrop(board, 0f, 1f);

            Assert.That(animation.MoveNext(), Is.False);

            Assert.That(boardView.IsAnimating, Is.False);
            foreach (KeyValuePair<long, BlockView> pair
                in boardView.ActiveViews)
            {
                BlockView view = pair.Value;
                Assert.That(view.RectTransform.anchoredPosition, Is.EqualTo(
                    BoardViewLayout.GetAnchoredPosition(view.Position)));
                Assert.That(view.IsInputEnabled, Is.True);
            }
        }

        [UnityTest]
        public IEnumerator RenderInitialDrop_NonZeroCompletesNaturally()
        {
            BoardState board = CreateFullBoard();
            IEnumerator animation = boardView.RenderInitialDrop(
                board,
                0.01f,
                0.002f);
            int frameCount = 0;

            while (animation.MoveNext())
            {
                Assert.That(++frameCount, Is.LessThan(300),
                    "EditMode Time.deltaTime did not advance the drop.");
                yield return animation.Current;
            }

            Assert.That(boardView.IsAnimating, Is.False);
            Assert.That(boardView.TryValidateCurrentViewLayout(
                board,
                "InitialDropNaturalCompletionTest",
                out string reason),
                Is.True,
                reason);
            AssertAllViewsInputEnabled();
        }

        [Test]
        public void RenderInitialDrop_ReusesRuntimeIdViewsWithoutDuplication()
        {
            BoardState board = CreateFullBoard();
            boardView.Render(board);
            var originalViews = new Dictionary<long, BlockView>();
            foreach (KeyValuePair<long, BlockView> pair
                in boardView.ActiveViews)
            {
                originalViews.Add(pair.Key, pair.Value);
            }

            IEnumerator animation = boardView.RenderInitialDrop(board, 0f, 0f);
            Assert.That(animation.MoveNext(), Is.False);

            Assert.That(pool.TotalCreatedCount, Is.EqualTo(
                BoardConstants.CellCount));
            Assert.That(new HashSet<BlockView>(
                boardView.ActiveViews.Values).Count, Is.EqualTo(
                    BoardConstants.CellCount));
            foreach (KeyValuePair<long, BlockView> pair in originalViews)
            {
                Assert.That(boardView.ActiveViews[pair.Key], Is.SameAs(
                    pair.Value));
            }
        }

        [Test]
        public void RenderInitialDrop_DuplicateAnimationIsRejected()
        {
            BoardState board = CreateFullBoard();
            IEnumerator animation = boardView.RenderInitialDrop(board, 1f, 0f);

            Assert.Throws<InvalidOperationException>(() =>
                boardView.RenderInitialDrop(board, 1f, 0f));

            Assert.That(animation.MoveNext(), Is.True);
            Dispose(animation);
            Assert.That(boardView.IsAnimating, Is.False);
        }

        [TestCase(-0.1f, 0f)]
        [TestCase(0f, -0.1f)]
        public void RenderInitialDrop_NegativeTimingIsRejected(
            float duration,
            float columnStagger)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                boardView.RenderInitialDrop(
                    CreateFullBoard(),
                    duration,
                    columnStagger));
        }

        [Test]
        public void PlaySwapTransition_NotSwappableDoesNothing()
        {
            BoardSwapActionResult result =
                BoardSwapPresentationTestSupport.CreateNotSwappable(
                    out BoardState board);
            boardView.Render(board);
            var positions = CaptureViewPositions();
            int totalCreatedCount = pool.TotalCreatedCount;

            IEnumerator animation = boardView.PlaySwapTransition(
                board,
                result,
                1f);

            Assert.That(boardView.IsAnimating, Is.False);
            Assert.That(animation.MoveNext(), Is.False);
            AssertViewPositionsUnchanged(positions);
            Assert.That(pool.TotalCreatedCount, Is.EqualTo(totalCreatedCount));
            Assert.That(pool.ActiveCount, Is.EqualTo(
                BoardConstants.CellCount));
        }

        [Test]
        public void PlaySwapTransition_NoMatchZeroDurationReturnsToBeforeBoard()
        {
            BoardSwapActionResult result =
                BoardSwapPresentationTestSupport.CreateNoMatch(
                    out BoardState board);
            boardView.Render(board);
            var views = CaptureViews();
            var sprites = CaptureViewSprites();
            int totalCreatedCount = pool.TotalCreatedCount;

            IEnumerator animation = boardView.PlaySwapTransition(
                board,
                result,
                0f);
            Assert.That(animation.MoveNext(), Is.False);

            AssertViewsMatchBoard(board);
            AssertViewIdentityAndSpritesUnchanged(views, sprites);
            Assert.That(pool.TotalCreatedCount, Is.EqualTo(totalCreatedCount));
            Assert.That(boardView.IsAnimating, Is.False);
            AssertAllViewsInputEnabled();
        }

        [Test]
        public void PlaySwapTransition_ResolvedZeroDurationStopsAtSwappedBoard()
        {
            BoardSwapActionResult result =
                BoardSwapPresentationTestSupport.CreateResolved(
                    out BoardState beforeBoard);
            AssertRuntimeIdSetsEqual(beforeBoard, result.SwappedBoard);
            boardView.Render(beforeBoard);
            var views = CaptureViews();
            var sprites = CaptureViewSprites();
            var positions = CaptureViewPositions();
            int totalCreatedCount = pool.TotalCreatedCount;

            IEnumerator animation = boardView.PlaySwapTransition(
                beforeBoard,
                result,
                0f);
            Assert.That(animation.MoveNext(), Is.False);

            AssertViewsMatchBoard(result.SwappedBoard);
            AssertViewIdentityAndSpritesUnchanged(views, sprites);
            AssertOnlySwapViewsMoved(positions, result);
            Assert.That(pool.TotalCreatedCount, Is.EqualTo(totalCreatedCount));
            Assert.That(boardView.IsAnimating, Is.False);
            AssertAllViewsInputEnabled();
        }

        [Test]
        public void PlaySwapTransition_LocksAndRestoresPreviousInputState()
        {
            BoardSwapActionResult result =
                BoardSwapPresentationTestSupport.CreateResolved(
                    out BoardState board);
            boardView.Render(board);
            BlockView previouslyDisabled = boardView.ActiveViews[
                board.Get(new BoardPosition(5, 0)).RuntimeId];
            previouslyDisabled.SetInputEnabled(false);

            IEnumerator animation = boardView.PlaySwapTransition(
                board,
                result,
                1f);

            Assert.That(boardView.IsAnimating, Is.True);
            foreach (BlockView view in boardView.ActiveViews.Values)
            {
                Assert.That(view.IsInputEnabled, Is.False);
            }
            Assert.Throws<InvalidOperationException>(() =>
                boardView.PlaySwapTransition(board, result, 1f));

            Assert.That(animation.MoveNext(), Is.True);
            Dispose(animation);

            Assert.That(boardView.IsAnimating, Is.False);
            Assert.That(previouslyDisabled.IsInputEnabled, Is.False);
            foreach (BlockView view in boardView.ActiveViews.Values)
            {
                if (view != previouslyDisabled)
                {
                    Assert.That(view.IsInputEnabled, Is.True);
                }
            }
        }

        [Test]
        public void PlaySwapTransition_OnDisableCleansUpAtStableResultState()
        {
            BoardSwapActionResult result =
                BoardSwapPresentationTestSupport.CreateNoMatch(
                    out BoardState board);
            boardView.Render(board);
            boardView.PlaySwapTransition(board, result, 1f);

            typeof(BattleBoardView).GetMethod(
                "OnDisable",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(
                    boardView,
                    null);

            Assert.That(boardView.IsAnimating, Is.False);
            AssertViewsMatchBoard(board);
            AssertAllViewsInputEnabled();
        }

        [Test]
        public void PlaySwapTransition_ViewLayoutMismatchIsRejected()
        {
            BoardSwapActionResult result =
                BoardSwapPresentationTestSupport.CreateNoMatch(
                    out BoardState board);
            boardView.Render(board);
            BoardBlock block = board.Get(result.Swap.First);
            boardView.ActiveViews[block.RuntimeId].SetAnchoredPosition(
                Vector2.zero);

            InvalidOperationException exception = Assert.Throws<
                InvalidOperationException>(() =>
                    boardView.PlaySwapTransition(board, result, 0f));

            Assert.That(exception.Message, Does.Contain("beforeBoard"));
            Assert.That(boardView.IsAnimating, Is.False);
        }

        [Test]
        public void PlaySwapTransition_NegativeDurationIsRejected()
        {
            BoardSwapActionResult result =
                BoardSwapPresentationTestSupport.CreateNoMatch(
                    out BoardState board);
            boardView.Render(board);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                boardView.PlaySwapTransition(board, result, -0.1f));
        }

        [Test]
        public void PlayCascadeStep_ZeroDurationsEndAtExactFinalBoard()
        {
            BoardCascadeStep step =
                BoardCascadeStepPresentationPlannerTests
                    .CreateSingleMatchStep(out BoardState board);
            boardView.Render(board);
            var originalViews = CaptureViews();
            var originalSprites = CaptureViewSprites();
            var removedIds = new HashSet<long>();
            foreach (BoardBlockRemoval removal in step.Collapse.Removals)
            {
                removedIds.Add(removal.RuntimeId);
            }

            IEnumerator animation = boardView.PlayCascadeStep(
                board,
                step,
                0f,
                0f,
                0f);
            Assert.That(animation.MoveNext(), Is.False);

            AssertViewsMatchBoard(step.Board);
            Assert.That(boardView.ActiveViewCount, Is.EqualTo(30));
            Assert.That(pool.ActiveCount, Is.EqualTo(30));
            Assert.That(pool.TotalCreatedCount, Is.EqualTo(30));
            Assert.That(boardView.IsAnimating, Is.False);
            foreach (KeyValuePair<long, BlockView> pair
                in boardView.ActiveViews)
            {
                BoardBlock block = FindBlock(step.Board, pair.Key);
                Assert.That(pair.Value.Image.sprite, Is.SameAs(
                    spriteSet.GetSprite(block.Element.Value)));
                Assert.That(pair.Value.RectTransform.localScale,
                    Is.EqualTo(Vector3.one));
                Assert.That(pair.Value.IsInputEnabled, Is.True);

                if (!removedIds.Contains(pair.Key)
                    && originalViews.ContainsKey(pair.Key))
                {
                    Assert.That(pair.Value, Is.SameAs(
                        originalViews[pair.Key]));
                    Assert.That(pair.Value.Image.sprite, Is.SameAs(
                        originalSprites[pair.Key]));
                }
            }
        }

        [Test]
        public void PlayCascadeStep_RemovalScaleAffectsOnlyRemovedViews()
        {
            BoardCascadeStep step =
                BoardCascadeStepPresentationPlannerTests
                    .CreateSingleMatchStep(out BoardState board);
            boardView.Render(board);
            var removedIds = new HashSet<long>();
            foreach (BoardBlockRemoval removal in step.Collapse.Removals)
            {
                removedIds.Add(removal.RuntimeId);
            }

            IEnumerator animation = boardView.PlayCascadeStep(
                board,
                step,
                1f,
                1f,
                1f);
            typeof(BattleBoardView).GetMethod(
                "UpdateRemovalScales",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(
                    boardView,
                    new object[] { 0.5f });

            foreach (KeyValuePair<long, BlockView> pair
                in boardView.ActiveViews)
            {
                Assert.That(
                    pair.Value.RectTransform.localScale,
                    removedIds.Contains(pair.Key)
                        ? Is.Not.EqualTo(Vector3.one)
                        : Is.EqualTo(Vector3.one));
            }

            Assert.That(animation.MoveNext(), Is.True);
            Dispose(animation);
            AssertViewsMatchBoard(step.Board);
        }

        [Test]
        public void PlayCascadeStep_CollapseMovesOnlyRecordedViews()
        {
            BoardCascadeStep step =
                BoardCascadeStepPresentationPlannerTests
                    .CreateSingleMatchStep(out BoardState board);
            boardView.Render(board);
            var originalPositions = CaptureViewPositions();
            var moveIds = new HashSet<long>();
            foreach (BoardBlockMove move in step.Collapse.Moves)
            {
                moveIds.Add(move.RuntimeId);
            }

            IEnumerator animation = boardView.PlayCascadeStep(
                board,
                step,
                0f,
                1f,
                1f);
            Assert.That(animation.MoveNext(), Is.True);
            typeof(BattleBoardView).GetMethod(
                "UpdateMovePositions",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(
                    boardView,
                    new object[] { 0.5f });

            foreach (KeyValuePair<long, BlockView> pair
                in boardView.ActiveViews)
            {
                bool changed = pair.Value.RectTransform.anchoredPosition
                    != originalPositions[pair.Key];
                Assert.That(changed, Is.EqualTo(moveIds.Contains(pair.Key)));
            }

            Dispose(animation);
            AssertViewsMatchBoard(step.Board);
        }

        [Test]
        public void PlayCascadeStep_RefillStartsSpawnsAtRecordedSources()
        {
            BoardCascadeStep step =
                BoardCascadeStepPresentationPlannerTests
                    .CreateSingleMatchStep(out BoardState board);
            boardView.Render(board);

            IEnumerator animation = boardView.PlayCascadeStep(
                board,
                step,
                0f,
                0f,
                1f);
            Assert.That(animation.MoveNext(), Is.True);

            foreach (BoardBlockSpawn spawn in step.Refill.Spawns)
            {
                Assert.That(boardView.TryGetView(
                    spawn.RuntimeId,
                    out BlockView view), Is.True);
                Assert.That(view.RuntimeId, Is.EqualTo(spawn.RuntimeId));
                Assert.That(view.Image.sprite, Is.SameAs(
                    spriteSet.GetSprite(spawn.Block.Element.Value)));
                Assert.That(view.RectTransform.anchoredPosition, Is.EqualTo(
                    BoardViewLayout.GetAnchoredPosition(
                        spawn.SourceX,
                        spawn.SourceY)));
                Assert.That(view.IsInputEnabled, Is.False);
            }

            Dispose(animation);
            AssertViewsMatchBoard(step.Board);
        }

        [TestCase(1f, 1f, 1f, 30)]
        [TestCase(0f, 1f, 1f, 27)]
        [TestCase(0f, 0f, 1f, 30)]
        public void PlayCascadeStep_InterruptionStabilizesEveryPhase(
            float removalDuration,
            float collapseDuration,
            float refillDuration,
            int activeCountBeforeInterruption)
        {
            BoardCascadeStep step =
                BoardCascadeStepPresentationPlannerTests
                    .CreateSingleMatchStep(out BoardState board);
            boardView.Render(board);
            IEnumerator animation = boardView.PlayCascadeStep(
                board,
                step,
                removalDuration,
                collapseDuration,
                refillDuration);
            Assert.That(animation.MoveNext(), Is.True);
            Assert.That(boardView.ActiveViewCount,
                Is.EqualTo(activeCountBeforeInterruption));

            typeof(BattleBoardView).GetMethod(
                "OnDisable",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(
                    boardView,
                    null);
            Dispose(animation);

            Assert.That(boardView.IsAnimating, Is.False);
            AssertViewsMatchBoard(step.Board);
            Assert.That(pool.ActiveCount, Is.EqualTo(30));
            Assert.That(pool.TotalCreatedCount, Is.EqualTo(30));
            AssertAllViewsInputEnabled();
        }

        [Test]
        public void PlayCascadeStep_RestoresExternalBoardWideInputLock()
        {
            BoardCascadeStep step =
                BoardCascadeStepPresentationPlannerTests
                    .CreateSingleMatchStep(out BoardState board);
            boardView.Render(board);
            foreach (BlockView view in boardView.ActiveViews.Values)
            {
                view.SetInputEnabled(false);
            }

            IEnumerator animation = boardView.PlayCascadeStep(
                board,
                step,
                0f,
                0f,
                0f);
            Assert.That(animation.MoveNext(), Is.False);

            foreach (BlockView view in boardView.ActiveViews.Values)
            {
                Assert.That(view.IsInputEnabled, Is.False);
            }
        }

        [Test]
        public void PlayCascadeStep_ViewLayoutMismatchIsRejected()
        {
            BoardCascadeStep step =
                BoardCascadeStepPresentationPlannerTests
                    .CreateSingleMatchStep(out BoardState board);
            boardView.Render(board);
            BoardBlock block = board.Get(new BoardPosition(5, 0));
            boardView.ActiveViews[block.RuntimeId].SetAnchoredPosition(
                Vector2.zero);

            InvalidOperationException exception = Assert.Throws<
                InvalidOperationException>(() => boardView.PlayCascadeStep(
                    board,
                    step,
                    0f,
                    0f,
                    0f));

            Assert.That(exception.Message, Does.Contain("beforeBoard"));
            Assert.That(boardView.IsAnimating, Is.False);
        }

        [TestCase("Position")]
        [TestCase("AnchoredPosition")]
        [TestCase("Sprite")]
        [TestCase("Scale")]
        public void DetailedValidation_IdentifiesFirstMismatchedViewField(
            string field)
        {
            BoardState board = CreateFullBoard();
            boardView.Render(board);
            var position = new BoardPosition(2, 2);
            BoardBlock block = board.Get(position);
            BlockView view = boardView.ActiveViews[block.RuntimeId];

            switch (field)
            {
                case "Position":
                    view.SetLogicalPosition(new BoardPosition(0, 0));
                    break;
                case "AnchoredPosition":
                    view.SetAnchoredPosition(Vector2.zero);
                    break;
                case "Sprite":
                    view.SetSprite(view.Image.sprite == fire ? water : fire);
                    break;
                case "Scale":
                    view.SetLocalScale(Vector3.one * 0.5f);
                    break;
            }

            bool valid = boardView.TryValidateCurrentViewLayout(
                board,
                "DiagnosticTest",
                out string reason);

            Assert.That(valid, Is.False);
            Assert.That(reason, Does.Contain("Context=DiagnosticTest"));
            Assert.That(reason, Does.Contain($"Field={field}"));
            Assert.That(reason, Does.Contain(
                $"ExpectedRuntimeId={block.RuntimeId}"));
            Assert.That(reason, Does.Contain("ExpectedPosition=(2, 2)"));
            Assert.That(reason, Does.Contain("ActualPosition="));
            Assert.That(reason, Does.Contain("ActualAnchoredPosition="));
            Assert.That(reason, Does.Contain("ActualSprite="));
            Assert.That(reason, Does.Contain("ActualScale="));
            Assert.That(reason, Does.Contain("UniqueViewInstances=30"));
        }

        [Test]
        public void DetailedValidation_AllowsAnchoredPositionWithinTolerance()
        {
            BoardState board = CreateFullBoard();
            boardView.Render(board);
            BlockView view = boardView.ActiveViews[
                board.Get(new BoardPosition(2, 2)).RuntimeId];
            view.SetAnchoredPosition(
                view.RectTransform.anchoredPosition
                + new Vector2(0.00001f, 0f));

            bool valid = boardView.TryValidateCurrentViewLayout(
                board,
                "AnchorToleranceTest",
                out string reason);

            Assert.That(valid, Is.True, reason);
        }

        [Test]
        public void DetailedValidation_RejectsAnchoredPositionBeyondTolerance()
        {
            BoardState board = CreateFullBoard();
            boardView.Render(board);
            BlockView view = boardView.ActiveViews[
                board.Get(new BoardPosition(2, 2)).RuntimeId];
            view.SetAnchoredPosition(
                view.RectTransform.anchoredPosition
                + new Vector2(0.02f, 0f));

            bool valid = boardView.TryValidateCurrentViewLayout(
                board,
                "AnchorToleranceTest",
                out string reason);

            Assert.That(valid, Is.False);
            Assert.That(reason, Does.Contain("Field=AnchoredPosition"));
            Assert.That(reason, Does.Contain("ExpectedAnchoredPosition="));
            Assert.That(reason, Does.Contain("ActualAnchoredPosition="));
            Assert.That(reason, Does.Contain("AnchorDelta="));
            Assert.That(reason, Does.Contain("AnchorDeltaMagnitude="));
        }

        [Test]
        public void DetailedValidation_AllowsScaleWithinTolerance()
        {
            BoardState board = CreateFullBoard();
            boardView.Render(board);
            BlockView view = boardView.ActiveViews[
                board.Get(new BoardPosition(2, 2)).RuntimeId];
            view.SetLocalScale(Vector3.one + new Vector3(0.00001f, 0f, 0f));

            bool valid = boardView.TryValidateCurrentViewLayout(
                board,
                "ScaleToleranceTest",
                out string reason);

            Assert.That(valid, Is.True, reason);
        }

        [Test]
        public void DetailedValidation_RejectsOneCellPositionError()
        {
            BoardState board = CreateFullBoard();
            boardView.Render(board);
            var position = new BoardPosition(2, 2);
            BlockView view = boardView.ActiveViews[
                board.Get(position).RuntimeId];
            view.SetAnchoredPosition(
                BoardViewLayout.GetAnchoredPosition(
                    new BoardPosition(position.X, position.Y + 1)));

            bool valid = boardView.TryValidateCurrentViewLayout(
                board,
                "OneCellErrorTest",
                out string reason);

            Assert.That(valid, Is.False);
            Assert.That(reason, Does.Contain("Field=AnchoredPosition"));
        }

        [Test]
        public void DetailedValidation_IdentifiesDictionaryKeyRuntimeIdMismatch()
        {
            BoardState board = CreateFullBoard();
            boardView.Render(board);
            var position = new BoardPosition(2, 2);
            BoardBlock block = board.Get(position);
            BlockView view = boardView.ActiveViews[block.RuntimeId];
            view.Bind(
                new BoardBlock(
                    block.RuntimeId + 1000,
                    block.BlockType,
                    block.Element),
                position,
                view.Image.sprite);

            bool valid = boardView.TryValidateCurrentViewLayout(
                board,
                "KeyMismatchTest",
                out string reason);

            Assert.That(valid, Is.False);
            Assert.That(reason,
                Does.Contain("Field=DictionaryKeyRuntimeId"));
            Assert.That(reason,
                Does.Contain($"DictionaryKey={block.RuntimeId}"));
            Assert.That(reason,
                Does.Contain($"ViewRuntimeId={block.RuntimeId + 1000}"));
        }

        [Test]
        public void DetailedValidation_IdentifiesViewAliasAndAllKeys()
        {
            BoardState board = CreateFullBoard();
            boardView.Render(board);
            Dictionary<long, BlockView> views = GetMutableViews();
            BlockView aliasedView = views[1];
            views.Add(9999, aliasedView);

            bool valid = boardView.TryValidateCurrentViewLayout(
                board,
                "AliasTest",
                out string reason);

            Assert.That(valid, Is.False);
            Assert.That(reason, Does.Contain("Field=ViewAlias"));
            Assert.That(reason,
                Does.Contain($"ViewInstanceId={aliasedView.GetInstanceID()}"));
            Assert.That(reason, Does.Contain("DictionaryKeys=[1,9999]"));
            Assert.That(reason, Does.Contain("DictionaryCount=31"));
            Assert.That(reason, Does.Contain("UniqueViewInstances=30"));
        }

        [Test]
        public void DetailedValidation_IdentifiesViewCountMismatch()
        {
            BoardState board = CreateFullBoard();
            boardView.Render(board);
            BlockView extraView = pool.Acquire();
            var extraBlock = new BoardBlock(
                9999,
                BoardBlockType.Normal,
                ElementType.Fire);
            extraView.Bind(
                extraBlock,
                new BoardPosition(0, 0),
                fire);
            GetMutableViews().Add(extraBlock.RuntimeId, extraView);

            bool valid = boardView.TryValidateCurrentViewLayout(
                board,
                "CountTest",
                out string reason);

            Assert.That(valid, Is.False);
            Assert.That(reason, Does.Contain("Field=ViewCount"));
            Assert.That(reason, Does.Contain("Expected=30; Actual=31"));
            Assert.That(reason, Does.Contain("PoolActive=31"));
        }

        [Test]
        public void DetailedValidation_IdentifiesMissingTargetView()
        {
            BoardState board = CreateFullBoard();
            boardView.Render(board);
            var position = new BoardPosition(2, 2);
            long runtimeId = board.Get(position).RuntimeId;
            GetMutableViews().Remove(runtimeId);

            bool valid = boardView.TryValidateCurrentViewLayout(
                board,
                "MissingViewTest",
                out string reason);

            Assert.That(valid, Is.False);
            Assert.That(reason, Does.Contain("Field=ViewMissing"));
            Assert.That(reason,
                Does.Contain($"ExpectedRuntimeId={runtimeId}"));
            Assert.That(reason, Does.Contain("ExpectedPosition=(2, 2)"));
            Assert.That(reason, Does.Contain("ActualView=<missing>"));
            Assert.That(reason, Does.Contain("DictionaryCount=29"));
            Assert.That(reason, Does.Contain("PoolActive=30"));
        }

        [TestCase(-0.1f, 0f, 0f)]
        [TestCase(0f, -0.1f, 0f)]
        [TestCase(0f, 0f, -0.1f)]
        public void PlayCascadeStep_NegativeDurationIsRejected(
            float removalDuration,
            float collapseDuration,
            float refillDuration)
        {
            BoardCascadeStep step =
                BoardCascadeStepPresentationPlannerTests
                    .CreateSingleMatchStep(out BoardState board);
            boardView.Render(board);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                boardView.PlayCascadeStep(
                    board,
                    step,
                    removalDuration,
                    collapseDuration,
                    refillDuration));
        }

        private Dictionary<long, BlockView> CaptureViews()
        {
            return new Dictionary<long, BlockView>(boardView.ActiveViews);
        }

        private Dictionary<long, BlockView> GetMutableViews()
        {
            FieldInfo field = typeof(BattleBoardView).GetField(
                "viewsByRuntimeId",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (Dictionary<long, BlockView>)field.GetValue(boardView);
        }

        private Dictionary<long, Sprite> CaptureViewSprites()
        {
            var sprites = new Dictionary<long, Sprite>();
            foreach (KeyValuePair<long, BlockView> pair
                in boardView.ActiveViews)
            {
                sprites.Add(pair.Key, pair.Value.Image.sprite);
            }

            return sprites;
        }

        private Dictionary<long, Vector2> CaptureViewPositions()
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

        private static BoardBlock FindBlock(BoardState board, long runtimeId)
        {
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    BoardBlock block = board.Get(new BoardPosition(x, y));
                    if (block != null && block.RuntimeId == runtimeId)
                    {
                        return block;
                    }
                }
            }

            throw new InvalidOperationException(
                $"RuntimeId {runtimeId} was not found.");
        }

        private static void AssertRuntimeIdSetsEqual(
            BoardState expected,
            BoardState actual)
        {
            var expectedIds = new HashSet<long>();
            var actualIds = new HashSet<long>();
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                expectedIds.Add(expected.Get(position).RuntimeId);
                actualIds.Add(actual.Get(position).RuntimeId);
            }

            Assert.That(actualIds, Is.EquivalentTo(expectedIds));
        }

        private void AssertViewsMatchBoard(BoardState expectedBoard)
        {
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    BoardBlock block = expectedBoard.Get(position);
                    Assert.That(boardView.TryGetView(
                        block.RuntimeId,
                        out BlockView view), Is.True);
                    Assert.That(view.Position, Is.EqualTo(position));
                    Assert.That(view.RectTransform.anchoredPosition, Is.EqualTo(
                        BoardViewLayout.GetAnchoredPosition(position)));
                }
            }
        }

        private void AssertViewIdentityAndSpritesUnchanged(
            Dictionary<long, BlockView> expectedViews,
            Dictionary<long, Sprite> expectedSprites)
        {
            foreach (KeyValuePair<long, BlockView> pair in expectedViews)
            {
                Assert.That(boardView.ActiveViews[pair.Key], Is.SameAs(
                    pair.Value));
                Assert.That(pair.Value.RuntimeId, Is.EqualTo(pair.Key));
                Assert.That(pair.Value.Image.sprite, Is.SameAs(
                    expectedSprites[pair.Key]));
            }
        }

        private void AssertOnlySwapViewsMoved(
            Dictionary<long, Vector2> beforePositions,
            BoardSwapActionResult result)
        {
            long firstId = result.SwappedBoard.Get(
                result.Swap.Second).RuntimeId;
            long secondId = result.SwappedBoard.Get(
                result.Swap.First).RuntimeId;
            int movedCount = 0;

            foreach (KeyValuePair<long, BlockView> pair
                in boardView.ActiveViews)
            {
                bool moved = pair.Value.RectTransform.anchoredPosition
                    != beforePositions[pair.Key];
                if (moved)
                {
                    movedCount++;
                    Assert.That(
                        pair.Key == firstId || pair.Key == secondId,
                        Is.True);
                }
            }

            Assert.That(movedCount, Is.EqualTo(2));
        }

        private void AssertViewPositionsUnchanged(
            Dictionary<long, Vector2> expected)
        {
            foreach (KeyValuePair<long, Vector2> pair in expected)
            {
                Assert.That(
                    boardView.ActiveViews[pair.Key]
                        .RectTransform.anchoredPosition,
                    Is.EqualTo(pair.Value));
            }
        }

        private void AssertAllViewsInputEnabled()
        {
            foreach (BlockView view in boardView.ActiveViews.Values)
            {
                Assert.That(view.IsInputEnabled, Is.True);
            }
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

        private static BoardState CreateFullBoard()
        {
            ElementType[] elements =
            {
                ElementType.Fire,
                ElementType.Water,
                ElementType.Grass,
                ElementType.Light,
                ElementType.Dark
            };
            var board = new BoardState();
            long runtimeId = 1;

            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    board.Set(
                        new BoardPosition(x, y),
                        new BoardBlock(
                            runtimeId++,
                            BoardBlockType.Normal,
                            elements[(x + y) % elements.Length]));
                }
            }

            return board;
        }
    }
}
