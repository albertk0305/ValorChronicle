using System;
using ValorChronicle.Battle.Combat.Damage;

namespace ValorChronicle.Battle.Combat.Shields
{
    public static class ShieldGenerationCalculator
    {
        public static ShieldGenerationResult Calculate(
            ShieldGenerationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            double comboMultiplier = context.AppliesCombo
                ? ComboMultiplierResolver.Resolve(context.FinalComboCount)
                : 1d;
            return CalculateWithComboMultiplier(context, comboMultiplier);
        }

        public static ShieldGenerationResult CalculateWithComboMultiplier(
            ShieldGenerationContext context,
            double comboMultiplier)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (double.IsNaN(comboMultiplier)
                || double.IsInfinity(comboMultiplier)
                || comboMultiplier < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(comboMultiplier),
                    comboMultiplier,
                    "Combo multiplier must be finite and non-negative.");
            }

            double shieldAmountIncreaseMultiplier =
                1d + context.ShieldAmountIncreaseRateSum;
            double rawShieldAmount = context.SourceMaxHp
                * context.ShieldCoefficient
                * comboMultiplier
                * shieldAmountIncreaseMultiplier;
            long finalShieldAmount = FloorToLong(rawShieldAmount);

            return new ShieldGenerationResult(
                context.SourceMaxHp,
                context.ShieldCoefficient,
                comboMultiplier,
                shieldAmountIncreaseMultiplier,
                rawShieldAmount,
                finalShieldAmount);
        }

        private static long FloorToLong(double rawShieldAmount)
        {
            if (double.IsNaN(rawShieldAmount)
                || double.IsInfinity(rawShieldAmount)
                || rawShieldAmount < 0d)
            {
                throw new OverflowException(
                    "Calculated shield amount must be finite and non-negative.");
            }

            return checked((long)Math.Floor(rawShieldAmount));
        }
    }
}
