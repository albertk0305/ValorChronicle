using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
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
