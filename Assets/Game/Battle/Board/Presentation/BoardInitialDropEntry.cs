using System;

namespace ValorChronicle.Battle.Board.Presentation
{
    public sealed class BoardInitialDropEntry
    {
        public BoardInitialDropEntry(
            long runtimeId,
            BoardPosition target,
            int sourceY)
        {
            if (sourceY < BoardConstants.Height)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sourceY),
                    sourceY,
                    $"Source Y must be at least {BoardConstants.Height}.");
            }

            RuntimeId = runtimeId;
            Target = target;
            SourceY = sourceY;
        }

        public long RuntimeId { get; }
        public BoardPosition Target { get; }
        public int SourceX => Target.X;
        public int SourceY { get; }
    }
}
