using System;

namespace ValorChronicle.Characters.Build
{
    public sealed class CharacterBuildStatMultipliers
    {
        public CharacterBuildStatMultipliers(
            double maxHpMultiplier,
            double attackMultiplier)
        {
            ValidateMultiplier(
                maxHpMultiplier,
                nameof(maxHpMultiplier));
            ValidateMultiplier(
                attackMultiplier,
                nameof(attackMultiplier));

            MaxHpMultiplier = maxHpMultiplier;
            AttackMultiplier = attackMultiplier;
        }

        public static CharacterBuildStatMultipliers Identity { get; } =
            new CharacterBuildStatMultipliers(1d, 1d);

        public double MaxHpMultiplier { get; }
        public double AttackMultiplier { get; }

        private static void ValidateMultiplier(
            double value,
            string parameterName)
        {
            if (double.IsNaN(value)
                || double.IsInfinity(value)
                || value <= 0d)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "A character build stat multiplier must be finite "
                        + "and greater than zero.");
            }
        }
    }
}
