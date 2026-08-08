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
            return new HealingContext(
                request.Source.MaxHp,
                request.HealingCoefficient,
                request.AppliesCombo,
                request.FinalComboCount,
                modifiers.HealingIncreaseRateSum);
        }
    }
}
