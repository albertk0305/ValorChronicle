using System;
using System.Collections.Generic;

namespace ValorChronicle.Battle.Board
{
    public static class BoardCollapseResolver
    {
        public static BoardCollapseResult Resolve(
            BoardState board,
            IReadOnlyList<BoardPosition> positionsToRemove)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (positionsToRemove == null)
            {
                throw new ArgumentNullException(nameof(positionsToRemove));
            }

            BoardState workingBoard = board.Clone();
            ValidateRemovalPositions(workingBoard, positionsToRemove);

            var removals =
                new List<BoardBlockRemoval>(positionsToRemove.Count);
            RemoveBlocks(workingBoard, positionsToRemove, removals);

            var moves = new List<BoardBlockMove>();
            CollapseColumns(workingBoard, moves);

            return new BoardCollapseResult(workingBoard, removals, moves);
        }

        private static void ValidateRemovalPositions(
            BoardState board,
            IReadOnlyList<BoardPosition> positionsToRemove)
        {
            var uniquePositions = new HashSet<BoardPosition>();

            for (int index = 0; index < positionsToRemove.Count; index++)
            {
                BoardPosition position = positionsToRemove[index];
                if (!uniquePositions.Add(position))
                {
                    throw new ArgumentException(
                        $"Removal position {position} is duplicated.",
                        nameof(positionsToRemove));
                }

                if (!board.IsOccupied(position))
                {
                    throw new InvalidOperationException(
                        $"Cannot remove an empty board position {position}.");
                }
            }
        }

        private static void RemoveBlocks(
            BoardState board,
            IReadOnlyList<BoardPosition> positionsToRemove,
            List<BoardBlockRemoval> removals)
        {
            for (int index = 0; index < positionsToRemove.Count; index++)
            {
                BoardPosition position = positionsToRemove[index];
                BoardBlock block = board.Get(position);
                removals.Add(new BoardBlockRemoval(block, position));
                board.Clear(position);
            }
        }

        private static void CollapseColumns(
            BoardState board,
            List<BoardBlockMove> moves)
        {
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                int writeY = 0;

                for (int readY = 0; readY < BoardConstants.Height; readY++)
                {
                    var from = new BoardPosition(x, readY);
                    BoardBlock block = board.Get(from);
                    if (block == null)
                    {
                        continue;
                    }

                    if (readY != writeY)
                    {
                        var to = new BoardPosition(x, writeY);
                        board.Set(to, block);
                        board.Clear(from);
                        moves.Add(new BoardBlockMove(block, from, to));
                    }

                    writeY++;
                }

                for (int y = writeY; y < BoardConstants.Height; y++)
                {
                    board.Clear(new BoardPosition(x, y));
                }
            }
        }
    }
}
