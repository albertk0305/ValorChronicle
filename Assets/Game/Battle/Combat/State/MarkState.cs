using System;

namespace ValorChronicle.Battle.Combat.State
{
    public sealed class MarkState
    {
        public MarkState(string markId, int maxStacks)
        {
            if (string.IsNullOrWhiteSpace(markId))
            {
                throw new ArgumentException(
                    "Mark ID cannot be null or whitespace.",
                    nameof(markId));
            }

            if (maxStacks <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxStacks),
                    maxStacks,
                    "Maximum stacks must be positive.");
            }

            MarkId = markId;
            MaxStacks = maxStacks;
        }

        public string MarkId { get; }
        public int CurrentStacks { get; private set; }
        public int MaxStacks { get; }
        public bool HasAny => CurrentStacks > 0;

        public int Add(int amount)
        {
            ValidatePositive(amount, nameof(amount));
            int added = Math.Min(amount, MaxStacks - CurrentStacks);
            CurrentStacks += added;
            return added;
        }

        public int Consume(int amount)
        {
            ValidatePositive(amount, nameof(amount));
            int consumed = Math.Min(amount, CurrentStacks);
            CurrentStacks -= consumed;
            return consumed;
        }

        public int ConsumeAll()
        {
            int consumed = CurrentStacks;
            CurrentStacks = 0;
            return consumed;
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
