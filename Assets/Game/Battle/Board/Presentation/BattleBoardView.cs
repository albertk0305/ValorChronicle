using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace ValorChronicle.Battle.Board.Presentation
{
    public sealed class BattleBoardView : MonoBehaviour
    {
        [SerializeField]
        private BoardElementSpriteSet spriteSet;

        [SerializeField]
        private BlockViewPool pool;

        private readonly Dictionary<long, BlockView> viewsByRuntimeId =
            new Dictionary<long, BlockView>();
        private readonly List<RenderItem> renderItems =
            new List<RenderItem>(BoardConstants.CellCount);
        private readonly List<long> obsoleteRuntimeIds =
            new List<long>(BoardConstants.CellCount);
        private IReadOnlyDictionary<long, BlockView> readOnlyViews;

        public int ActiveViewCount => viewsByRuntimeId.Count;

        public IReadOnlyDictionary<long, BlockView> ActiveViews
        {
            get
            {
                if (readOnlyViews == null)
                {
                    readOnlyViews =
                        new ReadOnlyDictionary<long, BlockView>(
                            viewsByRuntimeId);
                }

                return readOnlyViews;
            }
        }

        public void Configure(
            BoardElementSpriteSet elementSpriteSet,
            BlockViewPool viewPool)
        {
            spriteSet = elementSpriteSet
                ?? throw new ArgumentNullException(nameof(elementSpriteSet));
            pool = viewPool
                ?? throw new ArgumentNullException(nameof(viewPool));
        }

        public void Render(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            ValidateDependencies();
            CollectRenderItems(board);
            ReleaseObsoleteViews();

            for (int index = 0; index < renderItems.Count; index++)
            {
                RenderItem item = renderItems[index];
                if (!viewsByRuntimeId.TryGetValue(
                    item.Block.RuntimeId,
                    out BlockView view))
                {
                    view = pool.Acquire();
                    viewsByRuntimeId.Add(item.Block.RuntimeId, view);
                }

                view.Bind(item.Block, item.Position, item.Sprite);
            }
        }

        public bool TryGetView(long runtimeId, out BlockView view)
        {
            return viewsByRuntimeId.TryGetValue(runtimeId, out view);
        }

        private void ValidateDependencies()
        {
            if (spriteSet == null)
            {
                throw new InvalidOperationException(
                    "A BoardElementSpriteSet must be assigned.");
            }

            if (pool == null)
            {
                throw new InvalidOperationException(
                    "A BlockViewPool must be assigned.");
            }

            pool.Initialize();
        }

        private void CollectRenderItems(BoardState board)
        {
            renderItems.Clear();
            var runtimeIds = new HashSet<long>();

            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    BoardBlock block = board.Get(position);
                    if (block == null)
                    {
                        continue;
                    }

                    if (block.BlockType != BoardBlockType.Normal
                        || !block.Element.HasValue)
                    {
                        throw new NotSupportedException(
                            $"Board block type {block.BlockType} at " +
                            $"{position} is not supported by BattleBoardView.");
                    }

                    if (!runtimeIds.Add(block.RuntimeId))
                    {
                        throw new InvalidOperationException(
                            $"RuntimeId {block.RuntimeId} appears more than " +
                            "once on the board.");
                    }

                    Sprite sprite =
                        spriteSet.GetSprite(block.Element.Value);
                    renderItems.Add(
                        new RenderItem(block, position, sprite));
                }
            }
        }

        private void ReleaseObsoleteViews()
        {
            obsoleteRuntimeIds.Clear();

            foreach (long runtimeId in viewsByRuntimeId.Keys)
            {
                bool remains = false;
                for (int index = 0; index < renderItems.Count; index++)
                {
                    if (renderItems[index].Block.RuntimeId == runtimeId)
                    {
                        remains = true;
                        break;
                    }
                }

                if (!remains)
                {
                    obsoleteRuntimeIds.Add(runtimeId);
                }
            }

            for (int index = 0; index < obsoleteRuntimeIds.Count; index++)
            {
                long runtimeId = obsoleteRuntimeIds[index];
                BlockView view = viewsByRuntimeId[runtimeId];
                viewsByRuntimeId.Remove(runtimeId);
                pool.Release(view);
            }
        }

        private readonly struct RenderItem
        {
            public RenderItem(
                BoardBlock block,
                BoardPosition position,
                Sprite sprite)
            {
                Block = block;
                Position = position;
                Sprite = sprite;
            }

            public BoardBlock Block { get; }
            public BoardPosition Position { get; }
            public Sprite Sprite { get; }
        }
    }
}
