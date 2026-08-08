using System;
using ValorChronicle.Battle.Combat.Damage;

namespace ValorChronicle.Battle.Combat.Healing
{
    public static class HealingCalculator
    {
        public static HealingResult Calculate(HealingContext context)
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

        public static HealingResult CalculateWithComboMultiplier(
            HealingContext context,
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

            double healingIncreaseMultiplier =
                1d + context.HealingIncreaseRateSum;
            double rawHealing = context.SourceMaxHp
                * context.HealingCoefficient
                * comboMultiplier
                * healingIncreaseMultiplier;
            long finalHealing = FloorToLong(rawHealing);

            return new HealingResult(
                context.SourceMaxHp,
                context.HealingCoefficient,
                comboMultiplier,
                healingIncreaseMultiplier,
                rawHealing,
                finalHealing);
        }

        private static long FloorToLong(double rawHealing)
        {
            if (double.IsNaN(rawHealing)
                || double.IsInfinity(rawHealing)
                || rawHealing < 0d)
            {
                throw new OverflowException(
                    "Calculated healing must be finite and non-negative.");
            }

            return checked((long)Math.Floor(rawHealing));
        }
    }
}
