using System;

namespace ValorChronicle.Battle.Combat.State
{
    public sealed class ResourceState
    {
        public ResourceState(string resourceId, int maxAmount)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                throw new ArgumentException(
                    "Resource ID cannot be null or whitespace.",
                    nameof(resourceId));
            }

            if (maxAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxAmount),
                    maxAmount,
                    "Maximum amount must be positive.");
            }

            ResourceId = resourceId;
            MaxAmount = maxAmount;
        }

        public string ResourceId { get; }
        public int CurrentAmount { get; private set; }
        public int MaxAmount { get; }

        public ResourceAddResult Add(int amount)
        {
            ValidatePositive(amount, nameof(amount));
            int before = CurrentAmount;
            int capacity = MaxAmount - before;
            int added = Math.Min(amount, capacity);
            CurrentAmount += added;
            return new ResourceAddResult(
                ResourceId,
                amount,
                added,
                amount - added,
                before,
                CurrentAmount);
        }

        public ResourceConsumeResult Consume(int amount)
        {
            ValidatePositive(amount, nameof(amount));
            int before = CurrentAmount;
            int consumed = Math.Min(amount, before);
            CurrentAmount -= consumed;
            return new ResourceConsumeResult(
                ResourceId,
                amount,
                consumed,
                before,
                CurrentAmount);
        }

        public ResourceConsumeResult ConsumeAll()
        {
            int before = CurrentAmount;
            CurrentAmount = 0;
            return new ResourceConsumeResult(
                ResourceId,
                before,
                before,
                before,
                0);
        }

        private static void ValidatePositive(
            int amount,
            string parameterName)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    amount,
                    "Amount must be positive.");
            }
        }
    }
}
