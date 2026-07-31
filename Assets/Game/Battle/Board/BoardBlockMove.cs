using System;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardBlockMove
    {
        internal BoardBlockMove(
            BoardBlock block,
            BoardPosition from,
            BoardPosition to)
        {
            if (from == to)
            {
                throw new ArgumentException(
                    "A block move must change the block position.",
                    nameof(to));
            }

            Block = block;
            From = from;
            To = to;
        }

        public BoardBlock Block { get; }
        public BoardPosition From { get; }
        public BoardPosition To { get; }
        public long RuntimeId => Block.RuntimeId;
    }
}
