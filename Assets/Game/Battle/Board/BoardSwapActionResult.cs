using System;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardSwapActionResult
    {
        internal BoardSwapActionResult(
            BoardSwap swap,
            BoardSwapActionStatus status,
            BoardState swappedBoard,
            BoardCascadeResult cascade,
            BoardShuffleResult shuffle,
            BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            ValidateState(status, swappedBoard, cascade, shuffle, board);

            Swap = swap;
            Status = status;
            SwappedBoard = swappedBoard;
            Cascade = cascade;
            Shuffle = shuffle;
            Board = board;
        }

        public BoardSwap Swap { get; }
        public BoardSwapActionStatus Status { get; }
        public BoardState SwappedBoard { get; }
        public BoardCascadeResult Cascade { get; }
        public BoardShuffleResult Shuffle { get; }
        public BoardState Board { get; }
        public bool ConsumesTurn => Status == BoardSwapActionStatus.Resolved;
        public bool RequiresSwapBack => Status == BoardSwapActionStatus.NoMatch;
        public bool WasShuffled => Shuffle != null && Shuffle.WasShuffled;

        private static void ValidateState(
            BoardSwapActionStatus status,
            BoardState swappedBoard,
            BoardCascadeResult cascade,
            BoardShuffleResult shuffle,
            BoardState board)
        {
            switch (status)
            {
                case BoardSwapActionStatus.NotSwappable:
                    if (swappedBoard != null
                        || cascade != null
                        || shuffle != null)
                    {
                        throw new ArgumentException(
                            "A not-swappable result cannot contain processing results.");
                    }

                    break;

                case BoardSwapActionStatus.NoMatch:
                    if (swappedBoard == null)
                    {
                        throw new ArgumentNullException(nameof(swappedBoard));
                    }

                    if (cascade != null || shuffle != null)
                    {
                        throw new ArgumentException(
                            "A no-match result cannot contain cascade or shuffle results.");
                    }

                    break;

                case BoardSwapActionStatus.Resolved:
                    if (swappedBoard == null)
                    {
                        throw new ArgumentNullException(nameof(swappedBoard));
                    }

                    if (cascade == null)
                    {
                        throw new ArgumentNullException(nameof(cascade));
                    }

                    if (shuffle == null)
                    {
                        throw new ArgumentNullException(nameof(shuffle));
                    }

                    if (cascade.CascadeCount == 0 || cascade.ComboCount == 0)
                    {
                        throw new ArgumentException(
                            "A resolved result must contain a non-empty cascade.",
                            nameof(cascade));
                    }

                    if (!ReferenceEquals(board, shuffle.Board))
                    {
                        throw new ArgumentException(
                            "The final board must be the shuffle result board.",
                            nameof(board));
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(status),
                        status,
                        "Unknown swap action status.");
            }
        }
    }
}
