using System;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class CombatResourceRequirement
    {
        public CombatResourceRequirement(string resourceId, int maxAmount)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                throw new ArgumentException(
                    "Resource ID cannot be null or whitespace.",
                    nameof(resourceId));
            }

            if (maxAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAmount));
            }

            ResourceId = resourceId;
            MaxAmount = maxAmount;
        }

        public string ResourceId { get; }
        public int MaxAmount { get; }
    }
}
