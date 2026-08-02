using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Board.Presentation
{
    public sealed class BoardInitialDropPlanner
    {
        public IReadOnlyList<BoardInitialDropEntry> Build(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var entries = new List<BoardInitialDropEntry>(
                BoardConstants.CellCount);
            var runtimeIds = new HashSet<long>();

            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var target = new BoardPosition(x, y);
                    BoardBlock block = board.Get(target);
                    if (block == null)
                    {
                        throw new InvalidOperationException(
                            $"Initial board cell {target} must be occupied.");
                    }

                    if (block.BlockType != BoardBlockType.Normal
                        || !block.Element.HasValue)
                    {
                        throw new NotSupportedException(
                            $"Board block type {block.BlockType} at " +
                            $"{target} is not supported by the initial drop.");
                    }

                    if (!runtimeIds.Add(block.RuntimeId))
                    {
                        throw new InvalidOperationException(
                            $"RuntimeId {block.RuntimeId} appears more than " +
                            "once on the initial board.");
                    }

                    entries.Add(new BoardInitialDropEntry(
                        block.RuntimeId,
                        target,
                        BoardConstants.Height + target.Y));
                }
            }

            return new ReadOnlyCollection<BoardInitialDropEntry>(entries);
        }
    }
}
