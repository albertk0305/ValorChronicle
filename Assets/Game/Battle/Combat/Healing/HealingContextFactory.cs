using System;
using ValorChronicle.Battle.Combat.Modifiers;

namespace ValorChronicle.Battle.Combat.Healing
{
    public static class HealingContextFactory
    {
        public static HealingContext Build(
            HealingContextBuildRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            SupportModifierSnapshot modifiers =
                SupportModifierCollector.Collect(
                    request.Source,
                    request.Party);
            double healingIncreaseRateSum = Add(
                modifiers.HealingIncreaseRateSum,
                request.ActionLocalHealingIncreaseRate);
            return new HealingContext(
                request.Source.MaxHp,
                request.HealingCoefficient,
                request.AppliesCombo,
                request.FinalComboCount,
                healingIncreaseRateSum);
        }

        private static double Add(double left, double right)
        {
            double result = left + right;
            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                throw new OverflowException(
                    "Combined healing increase must remain finite.");
            }

            return result;
        }
    }
}
