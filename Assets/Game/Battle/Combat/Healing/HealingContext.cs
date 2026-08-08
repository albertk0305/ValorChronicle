using System;

namespace ValorChronicle.Battle.Combat.Healing
{
    public sealed class HealingContext
    {
        public HealingContext(
            long sourceMaxHp,
            double healingCoefficient,
            bool appliesCombo,
            int finalComboCount,
            double healingIncreaseRateSum)
        {
            if (sourceMaxHp < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceMaxHp));
            }

            ValidateNonNegative(
                healingCoefficient,
                nameof(healingCoefficient));
            ValidateNonNegative(
                healingIncreaseRateSum,
                nameof(healingIncreaseRateSum));
            if (appliesCombo && finalComboCount < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(finalComboCount),
                    finalComboCount,
                    "Final combo count must be positive when combo applies.");
            }

            SourceMaxHp = sourceMaxHp;
            HealingCoefficient = healingCoefficient;
            AppliesCombo = appliesCombo;
            FinalComboCount = finalComboCount;
            HealingIncreaseRateSum = healingIncreaseRateSum;
        }

        public long SourceMaxHp { get; }
        public double HealingCoefficient { get; }
        public bool AppliesCombo { get; }
        public int FinalComboCount { get; }
        public double HealingIncreaseRateSum { get; }

        private static void ValidateNonNegative(
            double value,
            string parameterName)
        {
            if (double.IsNaN(value)
                || double.IsInfinity(value)
                || value < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Value must be finite and non-negative.");
            }
        }
    }
}
