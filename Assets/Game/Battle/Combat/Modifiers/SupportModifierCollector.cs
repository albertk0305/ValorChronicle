using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Modifiers
{
    public static class SupportModifierCollector
    {
        public static SupportModifierSnapshot Collect(
            CharacterBattleState source,
            PartyBattleState party)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (party == null)
            {
                throw new ArgumentNullException(nameof(party));
            }

            var values = new SupportModifierValues();
            CollectSource(source.Effects.GetActiveEffects(), values);
            CollectSource(party.Effects.GetActiveEffects(), values);
            return new SupportModifierSnapshot(
                values.HealingIncrease,
                values.ShieldAmountIncrease);
        }

        private static void CollectSource(
            IReadOnlyList<EffectInstance> effects,
            SupportModifierValues values)
        {
            for (int index = 0; index < effects.Count; index++)
            {
                EffectInstance effect = effects[index];
                switch (effect.ModifierType)
                {
                    case EffectModifierType.HealingIncrease:
                        values.HealingIncrease = Add(
                            values.HealingIncrease,
                            effect.EffectiveMagnitude);
                        break;
                    case EffectModifierType.ShieldAmountIncrease:
                        values.ShieldAmountIncrease = Add(
                            values.ShieldAmountIncrease,
                            effect.EffectiveMagnitude);
                        break;
                }
            }
        }

        private static double Add(double current, double magnitude)
        {
            double result = current + magnitude;
            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                throw new OverflowException(
                    "Collected support modifier sum must remain finite.");
            }

            return result;
        }

        private sealed class SupportModifierValues
        {
            public double HealingIncrease;
            public double ShieldAmountIncrease;
        }
    }
}
