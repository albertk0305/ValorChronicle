using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Board.Presentation
{
    public sealed class BoardShufflePresentationPlan
    {
        private readonly ReadOnlyCollection<BoardShuffleEntry> entries;

        internal BoardShufflePresentationPlan(
            BoardShuffleKind kind,
            IReadOnlyList<BoardShuffleEntry> entries,
            BoardState board)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            var copy = new BoardShuffleEntry[entries.Count];
            for (int index = 0; index < entries.Count; index++)
            {
                copy[index] = entries[index]
                    ?? throw new ArgumentException(
                        "Shuffle entries cannot contain null.",
                        nameof(entries));
            }

            Kind = kind;
            this.entries = Array.AsReadOnly(copy);
            Board = board ?? throw new ArgumentNullException(nameof(board));
        }

        public BoardShuffleKind Kind { get; }
        public IReadOnlyList<BoardShuffleEntry> Entries => entries;
        public BoardState Board { get; }
        public bool HasAnimation => Kind != BoardShuffleKind.None;
    }
}
