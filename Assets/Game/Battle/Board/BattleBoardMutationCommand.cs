using System;

namespace ValorChronicle.Battle.Board
{
    public sealed class BattleBoardMutationCommand
    {
        public BattleBoardMutationCommand(
            BattleBoardMutationKind kind,
            int requestedCount,
            int maximumCount)
        {
            if (!Enum.IsDefined(typeof(BattleBoardMutationKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (requestedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedCount));
            }

            if (maximumCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumCount));
            }

            Kind = kind;
            RequestedCount = requestedCount;
            MaximumCount = maximumCount;
        }

        public BattleBoardMutationKind Kind { get; }
        public int RequestedCount { get; }
        public int MaximumCount { get; }
    }
}
