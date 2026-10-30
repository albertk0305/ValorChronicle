using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Characters.BaekHyeon
{
    public static class BaekHyeonMarkEffectFactory
    {
        public static EffectInstance Create(
            long runtimeId,
            BaekHyeonResolvedCombatRules combatRules)
        {
            if (combatRules == null)
            {
                throw new ArgumentNullException(nameof(combatRules));
            }

            return new EffectInstance(
                runtimeId,
                BaekHyeonRules.MarkEffectId,
                BaekHyeonRules.CharacterId,
                EffectCategory.Debuff,
                EffectModifierType.TargetTakenDamageIncrease,
                combatRules.MarkDamageIncreaseRatePerStack,
                combatRules.MarkRemainingTurns,
                runtimeId,
                combatRules.MarkStackPolicy,
                stackCount: 1,
                maxStackCount: combatRules.MarkMaxStackCount,
                attackTypeMask: combatRules.MarkDamageAttackTypes);
        }

        public static int GetMarkCount(BossBattleState boss)
        {
            return TryGetMarkEffect(boss, out EffectInstance effect)
                ? effect.StackCount
                : 0;
        }

        public static bool TryGetMarkEffect(
            BossBattleState boss,
            out EffectInstance effect)
        {
            if (boss == null)
            {
                throw new ArgumentNullException(nameof(boss));
            }

            IReadOnlyList<EffectInstance> matches =
                boss.Effects.FindByEffectId(BaekHyeonRules.MarkEffectId);
            if (matches.Count > 1)
            {
                throw new InvalidOperationException(
                    "Baek Hyeon mark must use one StackCount effect.");
            }

            effect = matches.Count == 1 ? matches[0] : null;
            return effect != null;
        }
    }
}
