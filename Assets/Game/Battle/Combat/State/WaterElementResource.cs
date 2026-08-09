using System;

namespace ValorChronicle.Battle.Combat.State
{
    public static class WaterElementResource
    {
        public const string Id = "water_element";
        public const int MaxAmount = 5;

        public static ResourceState Register(ResourceCollection resources)
        {
            if (resources == null)
            {
                throw new ArgumentNullException(nameof(resources));
            }

            return resources.Register(Id, MaxAmount);
        }
    }
}
