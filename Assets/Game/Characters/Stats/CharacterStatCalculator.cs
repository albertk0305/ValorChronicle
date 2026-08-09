using System;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Stats
{
    public static class CharacterStatCalculator
    {
        public const int MinimumLevel = 1;
        public const int MaximumLevel = 100;

        public static CharacterStatValues Calculate(
            CharacterDefinition definition,
            int level)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            ValidateLevel(level);
            return new CharacterStatValues(
                CalculateStat(
                    definition.Level1Hp,
                    definition.Level100Hp,
                    level),
                CalculateStat(
                    definition.Level1Attack,
                    definition.Level100Attack,
                    level));
        }

        public static long RoundFinalStat(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Final stat value must be finite.");
            }

            return checked((long)Math.Round(
                value,
                MidpointRounding.AwayFromZero));
        }

        private static long CalculateStat(
            int level1Value,
            int level100Value,
            int level)
        {
            double interpolatedValue = level1Value
                + ((double)level100Value - level1Value)
                * (level - MinimumLevel)
                / (MaximumLevel - MinimumLevel);
            return RoundFinalStat(interpolatedValue);
        }

        private static void ValidateLevel(int level)
        {
            if (level < MinimumLevel || level > MaximumLevel)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(level),
                    level,
                    $"Level must be between {MinimumLevel} and "
                        + $"{MaximumLevel}.");
            }
        }
    }
}
