using System;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardBlockSpawn
    {
        internal BoardBlockSpawn(
            BoardBlock block,
            BoardPosition target,
            int sourceY)
        {
            if (block == null)
            {
                throw new ArgumentNullException(nameof(block));
            }

            if (sourceY < BoardConstants.Height)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sourceY),
                    sourceY,
                    $"Source Y must be at least {BoardConstants.Height}.");
            }

            Block = block;
            Target = target;
            SourceY = sourceY;
        }

        public BoardBlock Block { get; }
        public BoardPosition Target { get; }
        public int SourceX => Target.X;
        public int SourceY { get; }
        public long RuntimeId => Block.RuntimeId;
    }
}
