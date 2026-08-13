using System;

namespace ValorChronicle.Battle.Combat.State
{
    public static class WaterElementResource
    {
        public const string Id = "water_element";
        public static ResourceState Register(
            ResourceCollection resources,
            int maxAmount)
        {
            if (resources == null)
            {
                throw new ArgumentNullException(nameof(resources));
            }

            if (maxAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAmount));
            }

            return resources.Register(Id, maxAmount);
        }
    }
}
