using System;
using System.Collections.Generic;

namespace ValorChronicle.Battle.Combat.Integration
{
    public static class CombatResourceRequirementMerger
    {
        public static IReadOnlyList<CombatResourceRequirement> Merge(
            IReadOnlyList<CombatResourceRequirement> requirements)
        {
            if (requirements == null)
            {
                throw new ArgumentNullException(nameof(requirements));
            }

            var byResourceId = new Dictionary<string, int>(
                requirements.Count,
                StringComparer.Ordinal);
            for (int index = 0; index < requirements.Count; index++)
            {
                CombatResourceRequirement requirement = requirements[index]
                    ?? throw new ArgumentException(
                        $"Resource requirement entry {index} is null.",
                        nameof(requirements));

                if (byResourceId.TryGetValue(
                        requirement.ResourceId,
                        out int existingMaximum))
                {
                    if (existingMaximum != requirement.MaxAmount)
                    {
                        throw new InvalidOperationException(
                            $"Conflicting maximum amounts for combat "
                                + $"resource '{requirement.ResourceId}': "
                                + $"{existingMaximum} and "
                                + $"{requirement.MaxAmount}.");
                    }

                    continue;
                }

                byResourceId.Add(
                    requirement.ResourceId,
                    requirement.MaxAmount);
            }

            var resourceIds = new List<string>(byResourceId.Keys);
            resourceIds.Sort(StringComparer.Ordinal);
            var merged = new CombatResourceRequirement[resourceIds.Count];
            for (int index = 0; index < resourceIds.Count; index++)
            {
                string resourceId = resourceIds[index];
                merged[index] = new CombatResourceRequirement(
                    resourceId,
                    byResourceId[resourceId]);
            }

            return Array.AsReadOnly(merged);
        }
    }
}
