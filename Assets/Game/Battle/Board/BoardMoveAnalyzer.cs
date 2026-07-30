using System;
using System.Collections.Generic;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardMoveAnalyzer
    {
        public bool IsAdjacent(BoardPosition first, BoardPosition second)
        {
            int horizontalDistance = Math.Abs(first.X - second.X);
            int verticalDistance = Math.Abs(first.Y - second.Y);
            return horizontalDistance + verticalDistance == 1;
        }

        public bool CanSwap(
            BoardState board,
            BoardPosition first,
            BoardPosition second)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (!IsAdjacent(first, second))
            {
                return false;
            }

            BoardBlock firstBlock = board.Get(first);
            BoardBlock secondBlock = board.Get(second);

            return IsSwappableNormalBlock(firstBlock)
                && IsSwappableNormalBlock(secondBlock)
                && firstBlock.Element != secondBlock.Element;
        }

        public bool IsValidSwap(
            BoardState board,
            BoardPosition first,
            BoardPosition second)
        {
            if (!CanSwap(board, first, second))
            {
                return false;
            }

            IReadOnlyList<BoardMatch> matchesBefore =
                BoardMatchFinder.FindMatches(board);
            BoardState candidate = board.Clone();
            candidate.Swap(first, second);
            IReadOnlyList<BoardMatch> matchesAfter =
                BoardMatchFinder.FindMatches(candidate);

            for (int index = 0; index < matchesAfter.Count; index++)
            {
                BoardMatch matchAfter = matchesAfter[index];
                if (!ContainsPosition(matchAfter, first)
                    && !ContainsPosition(matchAfter, second))
                {
                    continue;
                }

                if (!ContainsEquivalentMatch(matchesBefore, matchAfter))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasAnyValidSwap(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    BoardPosition first = new BoardPosition(x, y);

                    if (x + 1 < BoardConstants.Width
                        && IsValidSwap(
                            board,
                            first,
                            new BoardPosition(x + 1, y)))
                    {
                        return true;
                    }

                    if (y + 1 < BoardConstants.Height
                        && IsValidSwap(
                            board,
                            first,
                            new BoardPosition(x, y + 1)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public IReadOnlyList<BoardSwap> FindValidSwaps(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var swaps = new List<BoardSwap>();

            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    BoardPosition first = new BoardPosition(x, y);
                    TryAddValidSwap(
                        board,
                        first,
                        x + 1,
                        y,
                        swaps);
                    TryAddValidSwap(
                        board,
                        first,
                        x,
                        y + 1,
                        swaps);
                }
            }

            return Array.AsReadOnly(swaps.ToArray());
        }

        private static bool IsSwappableNormalBlock(BoardBlock block)
        {
            return block != null
                && block.BlockType == BoardBlockType.Normal
                && block.Element.HasValue;
        }

        private static bool ContainsPosition(
            BoardMatch match,
            BoardPosition position)
        {
            for (int index = 0; index < match.Positions.Count; index++)
            {
                if (match.Positions[index] == position)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsEquivalentMatch(
            IReadOnlyList<BoardMatch> matches,
            BoardMatch candidate)
        {
            for (int index = 0; index < matches.Count; index++)
            {
                if (AreEquivalent(matches[index], candidate))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AreEquivalent(BoardMatch first, BoardMatch second)
        {
            if (first.Element != second.Element
                || first.Positions.Count != second.Positions.Count)
            {
                return false;
            }

            for (int firstIndex = 0;
                firstIndex < first.Positions.Count;
                firstIndex++)
            {
                bool found = false;
                for (int secondIndex = 0;
                    secondIndex < second.Positions.Count;
                    secondIndex++)
                {
                    if (first.Positions[firstIndex]
                        == second.Positions[secondIndex])
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private void TryAddValidSwap(
            BoardState board,
            BoardPosition first,
            int secondX,
            int secondY,
            List<BoardSwap> swaps)
        {
            if (!BoardPosition.IsValid(secondX, secondY))
            {
                return;
            }

            var second = new BoardPosition(secondX, secondY);
            if (IsValidSwap(board, first, second))
            {
                swaps.Add(new BoardSwap(first, second));
            }
        }
    }
}
