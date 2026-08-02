using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Board.Presentation
{
    public sealed class BoardCascadeStepPresentationPlan
    {
        private readonly ReadOnlyCollection<BoardBlockRemoval> removals;
        private readonly ReadOnlyCollection<BoardBlockMove> moves;
        private readonly ReadOnlyCollection<BoardBlockSpawn> spawns;

        internal BoardCascadeStepPresentationPlan(
            IReadOnlyList<BoardBlockRemoval> removals,
            IReadOnlyList<BoardBlockMove> moves,
            IReadOnlyList<BoardBlockSpawn> spawns,
            BoardState collapseBoard,
            BoardState board)
        {
            this.removals = CopyAsReadOnly(removals, nameof(removals));
            this.moves = CopyAsReadOnly(moves, nameof(moves));
            this.spawns = CopyAsReadOnly(spawns, nameof(spawns));
            CollapseBoard = collapseBoard
                ?? throw new ArgumentNullException(nameof(collapseBoard));
            Board = board ?? throw new ArgumentNullException(nameof(board));
        }

        public IReadOnlyList<BoardBlockRemoval> Removals => removals;
        public IReadOnlyList<BoardBlockMove> Moves => moves;
        public IReadOnlyList<BoardBlockSpawn> Spawns => spawns;
        public BoardState CollapseBoard { get; }
        public BoardState Board { get; }

        private static ReadOnlyCollection<T> CopyAsReadOnly<T>(
            IReadOnlyList<T> source,
            string parameterName)
            where T : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var copy = new T[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                copy[index] = source[index]
                    ?? throw new ArgumentException(
                        "Presentation plan entries cannot contain null.",
                        parameterName);
            }

            return Array.AsReadOnly(copy);
        }
    }
}
