using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardCascadeStep
    {
        private readonly ReadOnlyCollection<BoardMatch> matches;

        internal BoardCascadeStep(
            IReadOnlyList<BoardMatch> matches,
            BoardCollapseResult collapse,
            BoardRefillResult refill)
        {
            if (matches == null)
            {
                throw new ArgumentNullException(nameof(matches));
            }

            Collapse = collapse
                ?? throw new ArgumentNullException(nameof(collapse));
            Refill = refill
                ?? throw new ArgumentNullException(nameof(refill));

            var copiedMatches = new BoardMatch[matches.Count];
            for (int index = 0; index < matches.Count; index++)
            {
                BoardMatch match = matches[index];
                if (match == null)
                {
                    throw new ArgumentException(
                        "Matches cannot contain null.",
                        nameof(matches));
                }

                copiedMatches[index] = match;
            }

            this.matches = Array.AsReadOnly(copiedMatches);
        }

        public IReadOnlyList<BoardMatch> Matches => matches;
        public BoardCollapseResult Collapse { get; }
        public BoardRefillResult Refill { get; }
        public BoardState Board => Refill.Board;
        public int MatchEventCount => matches.Count;
        public int RemovedBlockCount => Collapse.Removals.Count;
    }
}
