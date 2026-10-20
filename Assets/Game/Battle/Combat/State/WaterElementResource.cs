using System;

namespace ValorChronicle.Battle.Combat.State
{
    public static class WaterElementResource
    {
        public const string Id = "water_element";
        public const int MaxAmount = 5;

        public static ResourceState Register(ResourceCollection resources)
        {
            return Register(resources, MaxAmount);
        }

        public static ResourceState Register(
            ResourceCollection resources,
            int maxAmount)
        {
            if (resources == null)
            {
                throw new ArgumentNullException(nameof(resources));
            }

            if (maxAmount != MaxAmount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxAmount),
                    maxAmount,
                    $"Water element maximum must be {MaxAmount}.");
            }

            return resources.Register(Id, maxAmount);
        }
    }
}
