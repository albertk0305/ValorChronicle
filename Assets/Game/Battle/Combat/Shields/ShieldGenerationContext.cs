using System;

namespace ValorChronicle.Battle.Combat.Shields
{
    public sealed class ShieldGenerationContext
    {
        public ShieldGenerationContext(
            long sourceMaxHp,
            double shieldCoefficient,
            bool appliesCombo,
            int finalComboCount,
            double shieldAmountIncreaseRateSum)
        {
            if (sourceMaxHp < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceMaxHp));
            }

            ValidateNonNegative(
                shieldCoefficient,
                nameof(shieldCoefficient));
            ValidateNonNegative(
                shieldAmountIncreaseRateSum,
                nameof(shieldAmountIncreaseRateSum));
            if (appliesCombo && finalComboCount < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(finalComboCount),
                    finalComboCount,
                    "Final combo count must be positive when combo applies.");
            }

            SourceMaxHp = sourceMaxHp;
            ShieldCoefficient = shieldCoefficient;
            AppliesCombo = appliesCombo;
            FinalComboCount = finalComboCount;
            ShieldAmountIncreaseRateSum = shieldAmountIncreaseRateSum;
        }

        public long SourceMaxHp { get; }
        public double ShieldCoefficient { get; }
        public bool AppliesCombo { get; }
        public int FinalComboCount { get; }
        public double ShieldAmountIncreaseRateSum { get; }

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
