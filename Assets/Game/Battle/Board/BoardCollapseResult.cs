using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardCollapseResult
    {
        private readonly ReadOnlyCollection<BoardBlockRemoval> removals;
        private readonly ReadOnlyCollection<BoardBlockMove> moves;

        internal BoardCollapseResult(
            BoardState board,
            IList<BoardBlockRemoval> removals,
            IList<BoardBlockMove> moves)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (removals == null)
            {
                throw new ArgumentNullException(nameof(removals));
            }

            if (moves == null)
            {
                throw new ArgumentNullException(nameof(moves));
            }

            Board = board;
            this.removals = CopyAsReadOnly(removals);
            this.moves = CopyAsReadOnly(moves);
        }

        public BoardState Board { get; }
        public IReadOnlyList<BoardBlockRemoval> Removals => removals;
        public IReadOnlyList<BoardBlockMove> Moves => moves;

        private static ReadOnlyCollection<T> CopyAsReadOnly<T>(IList<T> source)
        {
            var copy = new T[source.Count];
            source.CopyTo(copy, 0);
            return Array.AsReadOnly(copy);
        }
    }
}
