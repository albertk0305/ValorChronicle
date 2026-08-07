using System;
using ValorChronicle.Core.Random;

namespace ValorChronicle.Battle.Combat.Damage
{
    public static class CriticalResolver
    {
        public static bool Resolve(
            bool canCritical,
            double finalCriticalChance,
            IRandomSource randomSource)
        {
            if (randomSource == null)
            {
                throw new ArgumentNullException(nameof(randomSource));
            }

            if (double.IsNaN(finalCriticalChance)
                || double.IsInfinity(finalCriticalChance)
                || finalCriticalChance < 0d
                || finalCriticalChance > 1d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(finalCriticalChance),
                    finalCriticalChance,
                    "Final critical chance must be between zero and one.");
            }

            if (!canCritical || finalCriticalChance <= 0d)
            {
                return false;
            }

            if (finalCriticalChance >= 1d)
            {
                return true;
            }

            float sample = randomSource.NextFloat();
            if (float.IsNaN(sample)
                || float.IsInfinity(sample)
                || sample < 0f
                || sample >= 1f)
            {
                throw new InvalidOperationException(
                    "Random source returned a value outside [0, 1).");
            }

            return sample < finalCriticalChance;
        }
    }
}
