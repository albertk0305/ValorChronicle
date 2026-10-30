using System;
using ValorChronicle.Battle.Combat.Effects;

namespace ValorChronicle.Characters.BaekHyeon
{
    public static class BaekHyeonExposureEffectFactory
    {
        public static EffectInstance Create(
            long runtimeId,
            BaekHyeonResolvedCombatRules combatRules)
        {
            if (combatRules == null)
            {
                throw new ArgumentNullException(nameof(combatRules));
            }

            if (!combatRules.ExposureEnabled)
            {
                throw new InvalidOperationException(
                    "Baek Hyeon exposure requires awakening six.");
            }

            return new EffectInstance(
                runtimeId,
                BaekHyeonRules.ExposureEffectId,
                BaekHyeonRules.CharacterId,
                EffectCategory.Debuff,
                EffectModifierType.TargetTakenDamageIncrease,
                combatRules.ExposureDamageIncreaseRate,
                combatRules.ExposureDurationTurns,
                runtimeId,
                combatRules.ExposureStackPolicy,
                maxStackCount: combatRules.ExposureMaxStackCount,
                attackTypeMask: combatRules.ExposureAttackTypeMask);
        }
    }
}
