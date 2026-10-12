using System;
using System.Collections.Generic;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Gacha
{
    public static class GachaRewardSelector
    {
        public static GachaRewardEntry Select(
            GachaDefinition definition,
            IRandomSource randomSource)
        {
            ValidateOrThrow(definition);
            if (randomSource == null)
            {
                throw new ArgumentNullException(nameof(randomSource));
            }

            int roll = randomSource.Next(
                0,
                GachaDefinition.RequiredTotalWeight);
            int cumulativeWeight = 0;
            IReadOnlyList<GachaRewardEntry> entries =
                definition.RewardEntries;
            for (int index = 0; index < entries.Count; index++)
            {
                cumulativeWeight += entries[index].Weight;
                if (roll < cumulativeWeight)
                {
                    return entries[index];
                }
            }

            throw new InvalidOperationException(
                "A validated Gacha reward table did not resolve a reward.");
        }

        public static void ValidateOrThrow(GachaDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            IReadOnlyList<GachaRewardEntry> entries =
                definition.RewardEntries;
            if (entries.Count == 0)
            {
                throw new ArgumentException(
                    "Gacha reward entries cannot be empty.",
                    nameof(definition));
            }

            long totalWeight = 0L;
            for (int index = 0; index < entries.Count; index++)
            {
                GachaRewardEntry entry = entries[index];
                if (entry == null)
                {
                    throw new ArgumentException(
                        $"Gacha reward entry {index} cannot be null.",
                        nameof(definition));
                }

                if (!Enum.IsDefined(
                        typeof(GachaRewardKind),
                        entry.RewardKind))
                {
                    throw new ArgumentException(
                        $"Gacha reward entry {index} has an invalid kind.",
                        nameof(definition));
                }

                if (entry.Quantity <= 0L)
                {
                    throw new ArgumentException(
                        $"Gacha reward entry {index} must have a positive "
                            + "quantity.",
                        nameof(definition));
                }

                if (entry.Weight <= 0)
                {
                    throw new ArgumentException(
                        $"Gacha reward entry {index} must have a positive "
                            + "weight.",
                        nameof(definition));
                }

                totalWeight += entry.Weight;
            }

            if (totalWeight != GachaDefinition.RequiredTotalWeight)
            {
                throw new ArgumentException(
                    $"Gacha reward weight total must be "
                        + $"{GachaDefinition.RequiredTotalWeight}, but was "
                        + $"{totalWeight}.",
                    nameof(definition));
            }
        }
    }
}
