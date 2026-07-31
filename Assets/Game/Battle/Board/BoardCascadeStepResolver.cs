using System;
using System.Collections.Generic;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardCascadeStepResolver
    {
        private readonly Func<BoardState, IReadOnlyList<BoardMatch>> matchFinder;
        private readonly BoardRefiller refiller;

        public BoardCascadeStepResolver(BoardRefiller refiller)
            : this(BoardMatchFinder.FindMatches, refiller)
        {
        }

        public BoardCascadeStepResolver(
            Func<BoardState, IReadOnlyList<BoardMatch>> matchFinder,
            BoardRefiller refiller)
        {
            this.matchFinder = matchFinder
                ?? throw new ArgumentNullException(nameof(matchFinder));
            this.refiller = refiller
                ?? throw new ArgumentNullException(nameof(refiller));
        }

        public bool TryResolve(
            BoardState board,
            out BoardCascadeStep step)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            IReadOnlyList<BoardMatch> matches = matchFinder(board);
            if (matches == null)
            {
                throw new InvalidOperationException(
                    "The match finder returned null.");
            }

            if (matches.Count == 0)
            {
                step = null;
                return false;
            }

            List<BoardPosition> positionsToRemove =
                CollectRemovalPositions(matches);
            BoardCollapseResult collapse = BoardCollapseResolver.Resolve(
                board,
                positionsToRemove);
            BoardRefillResult refill = refiller.Refill(collapse.Board);

            step = new BoardCascadeStep(matches, collapse, refill);
            return true;
        }

        private static List<BoardPosition> CollectRemovalPositions(
            IReadOnlyList<BoardMatch> matches)
        {
            var positions = new List<BoardPosition>();
            var uniquePositions = new HashSet<BoardPosition>();

            for (int matchIndex = 0;
                matchIndex < matches.Count;
                matchIndex++)
            {
                BoardMatch match = matches[matchIndex];
                if (match == null)
                {
                    throw new InvalidOperationException(
                        $"Match at index {matchIndex} is null.");
                }

                for (int positionIndex = 0;
                    positionIndex < match.Positions.Count;
                    positionIndex++)
                {
                    BoardPosition position = match.Positions[positionIndex];
                    if (!uniquePositions.Add(position))
                    {
                        throw new InvalidOperationException(
                            $"Matched position {position} appears more than once.");
                    }

                    positions.Add(position);
                }
            }

            return positions;
        }
    }
}
