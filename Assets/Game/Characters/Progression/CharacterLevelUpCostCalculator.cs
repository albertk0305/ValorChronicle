using System;

namespace ValorChronicle.Characters.Progression
{
    public static class CharacterLevelUpCostCalculator
    {
        public const int MinimumLevel = 1;
        public const int MaximumLevel = 100;

        private const long BaseCost = 100L;
        private const long CostPerCurrentLevel = 20L;

        public static long GetNextLevelCost(int currentLevel)
        {
            ValidateLevel(currentLevel, nameof(currentLevel));
            if (IsMaximumLevel(currentLevel))
            {
                throw new InvalidOperationException(
                    "A character at the maximum level cannot level up.");
            }

            return CalculateNextLevelCost(currentLevel);
        }

        public static long GetTotalCost(
            int currentLevel,
            int targetLevel)
        {
            ValidateLevel(currentLevel, nameof(currentLevel));
            ValidateLevel(targetLevel, nameof(targetLevel));
            if (targetLevel <= currentLevel)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(targetLevel),
                    targetLevel,
                    "Target level must be greater than current level.");
            }

            long totalCost = 0L;
            checked
            {
                for (int level = currentLevel;
                    level < targetLevel;
                    level++)
                {
                    totalCost += CalculateNextLevelCost(level);
                }
            }

            return totalCost;
        }

        public static bool IsMaximumLevel(int level)
        {
            ValidateLevel(level, nameof(level));
            return level == MaximumLevel;
        }

        private static long CalculateNextLevelCost(int currentLevel)
        {
            return checked(
                BaseCost
                + checked((long)currentLevel * CostPerCurrentLevel));
        }

        private static void ValidateLevel(int level, string parameterName)
        {
            if (level < MinimumLevel || level > MaximumLevel)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    level,
                    $"Level must be between {MinimumLevel} and "
                        + $"{MaximumLevel}.");
            }
        }
    }
}
