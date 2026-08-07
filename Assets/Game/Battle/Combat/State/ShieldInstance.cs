using System;

namespace ValorChronicle.Battle.Combat.State
{
    public sealed class ShieldInstance
    {
        private bool isRegistered;

        public ShieldInstance(
            long runtimeId,
            string sourceId,
            long initialAmount,
            int createdTurn,
            int? remainingTurns,
            long creationOrder)
        {
            if (runtimeId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(runtimeId),
                    runtimeId,
                    "Runtime ID must be positive.");
            }

            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException(
                    "Source ID cannot be null or whitespace.",
                    nameof(sourceId));
            }

            if (initialAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialAmount),
                    initialAmount,
                    "Initial shield amount must be positive.");
            }

            if (createdTurn <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(createdTurn),
                    createdTurn,
                    "Created turn must be positive.");
            }

            if (remainingTurns.HasValue
                && remainingTurns.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(remainingTurns),
                    remainingTurns,
                    "Finite remaining turns must be positive.");
            }

            if (creationOrder <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(creationOrder),
                    creationOrder,
                    "Creation order must be positive.");
            }

            RuntimeId = runtimeId;
            SourceId = sourceId;
            InitialAmount = initialAmount;
            CurrentAmount = initialAmount;
            CreatedTurn = createdTurn;
            RemainingTurns = remainingTurns;
            CreationOrder = creationOrder;
        }

        public long RuntimeId { get; }
        public string SourceId { get; }
        public long InitialAmount { get; }
        public long CurrentAmount { get; private set; }
        public int CreatedTurn { get; }
        public int? RemainingTurns { get; private set; }
        public long CreationOrder { get; }
        public bool IsExpired =>
            RemainingTurns.HasValue && RemainingTurns.Value == 0;
        public bool IsDepleted => CurrentAmount == 0;

        internal long Absorb(long damage)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage));
            }

            long absorbedDamage = Math.Min(damage, CurrentAmount);
            CurrentAmount -= absorbedDamage;
            return absorbedDamage;
        }

        internal void RegisterWithCollection()
        {
            if (isRegistered)
            {
                throw new InvalidOperationException(
                    "A shield instance can belong to only one collection.");
            }

            isRegistered = true;
        }

        internal void ProcessTurnEnd()
        {
            if (!RemainingTurns.HasValue || IsExpired)
            {
                return;
            }

            RemainingTurns--;
        }
    }
}
