using System;

namespace ValorChronicle.Battle.Combat.Damage
{
    public static class ComboMultiplierResolver
    {
        private static readonly double[] Multipliers =
        {
            1.00d,
            1.05d,
            1.12d,
            1.21d,
            1.32d,
            1.44d,
            1.57d,
            1.70d,
            1.85d,
            2.00d
        };

        public static double Resolve(int finalComboCount)
        {
            if (finalComboCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(finalComboCount),
                    finalComboCount,
                    "Final combo count must be positive.");
            }

            int index = Math.Min(finalComboCount, Multipliers.Length) - 1;
            return Multipliers[index];
        }
    }
}
