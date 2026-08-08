using System;

namespace ValorChronicle.Battle.Combat.Application
{
    public sealed class ShieldGrantRequest
    {
        public ShieldGrantRequest(
            long runtimeId,
            string sourceId,
            int createdTurn,
            int? remainingTurns,
            long creationOrder)
        {
            if (runtimeId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(runtimeId));
            }

            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException(
                    "Source ID cannot be null or whitespace.",
                    nameof(sourceId));
            }

            if (createdTurn <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(createdTurn));
            }

            if (remainingTurns.HasValue && remainingTurns.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(remainingTurns));
            }

            if (creationOrder <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(creationOrder));
            }

            RuntimeId = runtimeId;
            SourceId = sourceId;
            CreatedTurn = createdTurn;
            RemainingTurns = remainingTurns;
            CreationOrder = creationOrder;
        }

        public long RuntimeId { get; }
        public string SourceId { get; }
        public int CreatedTurn { get; }
        public int? RemainingTurns { get; }
        public long CreationOrder { get; }
    }
}
