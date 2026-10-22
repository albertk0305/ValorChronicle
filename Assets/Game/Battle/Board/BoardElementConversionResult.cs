using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Board
{
    public enum BoardElementConversionFailure
    {
        None = 0,
        NoCandidates = 1,
        NoPlayableCombination = 2
    }

    public sealed class BoardElementConversionResult
    {
        private readonly ReadOnlyCollection<BoardElementConversionEntry> entries;

        internal BoardElementConversionResult(
            BoardState board,
            int requestedCount,
            IReadOnlyList<BoardElementConversionEntry> entries,
            int randomAttemptCount,
            int deterministicCombinationCount,
            bool usedDeterministicSearch,
            BoardElementConversionFailure failure)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            if (requestedCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedCount));
            }

            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            if (randomAttemptCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(randomAttemptCount));
            }

            if (deterministicCombinationCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deterministicCombinationCount));
            }

            if (!Enum.IsDefined(typeof(BoardElementConversionFailure), failure))
            {
                throw new ArgumentOutOfRangeException(nameof(failure));
            }

            var copy = new BoardElementConversionEntry[entries.Count];
            for (int index = 0; index < entries.Count; index++)
            {
                copy[index] = entries[index]
                    ?? throw new ArgumentException(
                        "Conversion entries cannot contain null.",
                        nameof(entries));
            }

            RequestedCount = requestedCount;
            RandomAttemptCount = randomAttemptCount;
            DeterministicCombinationCount = deterministicCombinationCount;
            UsedDeterministicSearch = usedDeterministicSearch;
            Failure = failure;
            this.entries = Array.AsReadOnly(copy);
        }

        public BoardState Board { get; }
        public int RequestedCount { get; }
        public IReadOnlyList<BoardElementConversionEntry> Entries => entries;
        public int ConvertedCount => entries.Count;
        public int RandomAttemptCount { get; }
        public int DeterministicCombinationCount { get; }
        public bool UsedDeterministicSearch { get; }
        public BoardElementConversionFailure Failure { get; }
        public bool Succeeded => Failure == BoardElementConversionFailure.None;
    }
}
