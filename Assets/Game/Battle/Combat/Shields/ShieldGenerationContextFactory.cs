using System;
using ValorChronicle.Battle.Combat.Modifiers;

namespace ValorChronicle.Battle.Combat.Shields
{
    public static class ShieldGenerationContextFactory
    {
        public static ShieldGenerationContext Build(
            ShieldGenerationContextBuildRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            SupportModifierSnapshot modifiers =
                SupportModifierCollector.Collect(
                    request.Source,
                    request.Party);
            return new ShieldGenerationContext(
                request.Source.MaxHp,
                request.ShieldCoefficient,
                request.AppliesCombo,
                request.FinalComboCount,
                modifiers.ShieldAmountIncreaseRateSum);
        }
    }
}
