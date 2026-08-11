using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Board
{
    public enum BoardRockMutationFailure
    {
        None = 0,
        UnableToCreatePlayableBoard = 1
    }

    public sealed class BoardRockMutationResult
    {
        private readonly ReadOnlyCollection<BoardRockPlacement> placements;

        internal BoardRockMutationResult(
            BoardState board,
            int requestedCount,
            IReadOnlyList<BoardRockPlacement> placements,
            int placementAttemptCount,
            BoardShuffleResult shuffleResult,
            BoardRockMutationFailure failure)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            if (requestedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedCount));
            }

            if (placements == null)
            {
                throw new ArgumentNullException(nameof(placements));
            }

            if (placementAttemptCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(placementAttemptCount));
            }

            if (!Enum.IsDefined(typeof(BoardRockMutationFailure), failure))
            {
                throw new ArgumentOutOfRangeException(nameof(failure));
            }

            var copy = new BoardRockPlacement[placements.Count];
            for (int index = 0; index < placements.Count; index++)
            {
                copy[index] = placements[index]
                    ?? throw new ArgumentException(
                        "Rock placements cannot contain null.",
                        nameof(placements));
            }

            RequestedCount = requestedCount;
            PlacementAttemptCount = placementAttemptCount;
            ShuffleResult = shuffleResult;
            Failure = failure;
            this.placements = Array.AsReadOnly(copy);
        }

        public BoardState Board { get; }
        public int RequestedCount { get; }
        public IReadOnlyList<BoardRockPlacement> Placements => placements;
        public int CreatedCount => placements.Count;
        public int PlacementAttemptCount { get; }
        public BoardShuffleResult ShuffleResult { get; }
        public bool WasShuffled => ShuffleResult?.WasShuffled == true;
        public BoardRockMutationFailure Failure { get; }
        public bool Succeeded => Failure == BoardRockMutationFailure.None;
    }
}
