using System;

namespace ValorChronicle.Battle.Board.Presentation
{
    public sealed class BoardSwapViewMotion
    {
        public BoardSwapViewMotion(
            long runtimeId,
            BoardPosition from,
            BoardPosition to)
        {
            if (from == to)
            {
                throw new ArgumentException(
                    "Swap motion positions must be different.",
                    nameof(to));
            }

            RuntimeId = runtimeId;
            From = from;
            To = to;
        }

        public long RuntimeId { get; }
        public BoardPosition From { get; }
        public BoardPosition To { get; }
    }
}
