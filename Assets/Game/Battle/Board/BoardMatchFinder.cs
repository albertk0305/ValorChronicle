using System;
using System.Collections.Generic;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Board
{
    public static class BoardMatchFinder
    {
        public static IReadOnlyList<BoardMatch> FindMatches(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var matchedCells = new bool[BoardConstants.CellCount];
            MarkHorizontalRuns(board, matchedCells);
            MarkVerticalRuns(board, matchedCells);

            return BuildMatches(board, matchedCells);
        }

        private static void MarkHorizontalRuns(BoardState board, bool[] matchedCells)
        {
            for (int y = 0; y < BoardConstants.Height; y++)
            {
                int runStart = 0;

                while (runStart < BoardConstants.Width)
                {
                    ElementType? element = GetMatchElement(board, runStart, y);
                    if (!element.HasValue)
                    {
                        runStart++;
                        continue;
                    }

                    int runEnd = runStart + 1;
                    while (runEnd < BoardConstants.Width
                        && GetMatchElement(board, runEnd, y) == element)
                    {
                        runEnd++;
                    }

                    if (runEnd - runStart >= 3)
                    {
                        for (int x = runStart; x < runEnd; x++)
                        {
                            matchedCells[ToIndex(x, y)] = true;
                        }
                    }

                    runStart = runEnd;
                }
            }
        }

        private static void MarkVerticalRuns(BoardState board, bool[] matchedCells)
        {
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                int runStart = 0;

                while (runStart < BoardConstants.Height)
                {
                    ElementType? element = GetMatchElement(board, x, runStart);
                    if (!element.HasValue)
                    {
                        runStart++;
                        continue;
                    }

                    int runEnd = runStart + 1;
                    while (runEnd < BoardConstants.Height
                        && GetMatchElement(board, x, runEnd) == element)
                    {
                        runEnd++;
                    }

                    if (runEnd - runStart >= 3)
                    {
                        for (int y = runStart; y < runEnd; y++)
                        {
                            matchedCells[ToIndex(x, y)] = true;
                        }
                    }

                    runStart = runEnd;
                }
            }
        }

        private static IReadOnlyList<BoardMatch> BuildMatches(
            BoardState board,
            bool[] matchedCells)
        {
            var matches = new List<BoardMatch>();
            var visitedCells = new bool[BoardConstants.CellCount];
            var queue = new int[BoardConstants.CellCount];

            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    int originIndex = ToIndex(x, y);
                    if (!matchedCells[originIndex] || visitedCells[originIndex])
                    {
                        continue;
                    }

                    BoardPosition origin = new BoardPosition(x, y);
                    ElementType element = GetMatchElement(board, x, y).Value;
                    List<BoardPosition> positions = CollectConnectedGroup(
                        board,
                        matchedCells,
                        visitedCells,
                        queue,
                        originIndex,
                        element);

                    positions.Sort(CompareByDiscoveryOrder);
                    matches.Add(new BoardMatch(
                        element,
                        GetTier(positions.Count),
                        origin,
                        positions));
                }
            }

            return Array.AsReadOnly(matches.ToArray());
        }

        private static List<BoardPosition> CollectConnectedGroup(
            BoardState board,
            bool[] matchedCells,
            bool[] visitedCells,
            int[] queue,
            int originIndex,
            ElementType element)
        {
            var positions = new List<BoardPosition>();
            int readIndex = 0;
            int writeIndex = 0;

            queue[writeIndex++] = originIndex;
            visitedCells[originIndex] = true;

            while (readIndex < writeIndex)
            {
                int currentIndex = queue[readIndex++];
                BoardPosition current = BoardPosition.FromIndex(currentIndex);
                positions.Add(current);

                TryEnqueue(
                    board,
                    matchedCells,
                    visitedCells,
                    queue,
                    ref writeIndex,
                    current.X - 1,
                    current.Y,
                    element);
                TryEnqueue(
                    board,
                    matchedCells,
                    visitedCells,
                    queue,
                    ref writeIndex,
                    current.X + 1,
                    current.Y,
                    element);
                TryEnqueue(
                    board,
                    matchedCells,
                    visitedCells,
                    queue,
                    ref writeIndex,
                    current.X,
                    current.Y - 1,
                    element);
                TryEnqueue(
                    board,
                    matchedCells,
                    visitedCells,
                    queue,
                    ref writeIndex,
                    current.X,
                    current.Y + 1,
                    element);
            }

            return positions;
        }

        private static void TryEnqueue(
            BoardState board,
            bool[] matchedCells,
            bool[] visitedCells,
            int[] queue,
            ref int writeIndex,
            int x,
            int y,
            ElementType element)
        {
            if (!BoardPosition.IsValid(x, y))
            {
                return;
            }

            int index = ToIndex(x, y);
            if (!matchedCells[index]
                || visitedCells[index]
                || GetMatchElement(board, x, y) != element)
            {
                return;
            }

            visitedCells[index] = true;
            queue[writeIndex++] = index;
        }

        private static ElementType? GetMatchElement(BoardState board, int x, int y)
        {
            BoardBlock block = board.Get(new BoardPosition(x, y));
            if (block == null || block.BlockType != BoardBlockType.Normal)
            {
                return null;
            }

            return block.Element;
        }

        private static BoardMatchTier GetTier(int blockCount)
        {
            if (blockCount == 3)
            {
                return BoardMatchTier.Three;
            }

            if (blockCount == 4)
            {
                return BoardMatchTier.Four;
            }

            return BoardMatchTier.FiveOrMore;
        }

        private static int CompareByDiscoveryOrder(
            BoardPosition left,
            BoardPosition right)
        {
            int xComparison = left.X.CompareTo(right.X);
            return xComparison != 0
                ? xComparison
                : left.Y.CompareTo(right.Y);
        }

        private static int ToIndex(int x, int y)
        {
            return x + (y * BoardConstants.Width);
        }
    }
}
