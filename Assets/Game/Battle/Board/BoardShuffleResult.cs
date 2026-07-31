using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardShuffleResult
    {
        private readonly ReadOnlyCollection<BoardShuffleEntry> entries;

        internal BoardShuffleResult(
            BoardState board,
            BoardShuffleKind kind,
            IReadOnlyList<BoardShuffleEntry> entries,
            int attemptCount)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            if (attemptCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(attemptCount),
                    attemptCount,
                    "Attempt count cannot be negative.");
            }

            Board = board;
            Kind = kind;
            AttemptCount = attemptCount;

            var copiedEntries = new BoardShuffleEntry[entries.Count];
            for (int index = 0; index < entries.Count; index++)
            {
                BoardShuffleEntry entry = entries[index];
                if (entry == null)
                {
                    throw new ArgumentException(
                        "Entries cannot contain null.",
                        nameof(entries));
                }

                copiedEntries[index] = entry;
            }

            this.entries = Array.AsReadOnly(copiedEntries);
        }

        public BoardState Board { get; }
        public BoardShuffleKind Kind { get; }
        public IReadOnlyList<BoardShuffleEntry> Entries => entries;
        public int AttemptCount { get; }
        public bool WasShuffled => Kind != BoardShuffleKind.None;
    }
}
