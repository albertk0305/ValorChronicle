using System;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Bosses.Kragmor
{
    public static class KragmorDefenseEffectFactory
    {
        public static EffectInstance InitializeBattle(
            BossBattleState boss,
            KragmorBattleRuntimeState runtimeState,
            CombatActionIdSequence actionIds)
        {
            if (boss == null)
            {
                throw new ArgumentNullException(nameof(boss));
            }

            if (runtimeState == null)
            {
                throw new ArgumentNullException(nameof(runtimeState));
            }

            if (actionIds == null)
            {
                throw new ArgumentNullException(nameof(actionIds));
            }

            if (runtimeState.CurrentDefenseState
                    != KragmorDefenseState.VolcanicCarapace
                || CountActiveDefenseEffects(boss) != 0)
            {
                throw new InvalidOperationException(
                    "Kragmor defense effects were already initialized.");
            }

            long runtimeId = actionIds.Next();
            EffectInstance effect = Create(
                KragmorDefenseState.VolcanicCarapace,
                runtimeId,
                runtimeState.Config);
            boss.Effects.ApplyEffect(effect);
            return effect;
        }

        public static EffectInstance Create(
            KragmorDefenseState defenseState,
            long runtimeId,
            KragmorCombatConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(config));
            }

            string effectId;
            EffectCategory category;
            EffectModifierType modifierType;
            double magnitude;
            switch (defenseState)
            {
                case KragmorDefenseState.VolcanicCarapace:
                    effectId = KragmorRules.VolcanicCarapaceEffectId;
                    category = EffectCategory.Buff;
                    modifierType =
                        EffectModifierType.TargetTakenDamageReduction;
                    magnitude =
                        config.VolcanicCarapaceReductionRate;
                    break;
                case KragmorDefenseState.CoreCompression:
                    effectId = KragmorRules.CoreCompressionEffectId;
                    category = EffectCategory.Buff;
                    modifierType =
                        EffectModifierType.TargetTakenDamageReduction;
                    magnitude = config.CoreCompressionReductionRate;
                    break;
                case KragmorDefenseState.CoreExposure:
                    effectId = KragmorRules.CoreExposureEffectId;
                    category = EffectCategory.Debuff;
                    modifierType =
                        EffectModifierType.TargetTakenDamageIncrease;
                    magnitude = config.CoreExposureIncreaseRate;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(defenseState));
            }

            return new EffectInstance(
                runtimeId,
                effectId,
                KragmorRules.BossId,
                category,
                modifierType,
                magnitude,
                remainingTurns: null,
                creationOrder: runtimeId,
                EffectStackPolicy.Unique);
        }

        public static EffectInstance GetRequiredActiveEffect(
            BossBattleState boss,
            KragmorDefenseState expectedState)
        {
            if (boss == null)
            {
                throw new ArgumentNullException(nameof(boss));
            }

            string expectedEffectId = GetEffectId(expectedState);
            EffectInstance match = null;
            int defenseEffectCount = 0;
            var effects = boss.Effects.GetActiveEffects();
            for (int index = 0; index < effects.Count; index++)
            {
                EffectInstance effect = effects[index];
                if (!IsDefenseEffectId(effect.EffectId))
                {
                    continue;
                }

                defenseEffectCount++;
                if (string.Equals(
                    effect.EffectId,
                    expectedEffectId,
                    StringComparison.Ordinal))
                {
                    match = effect;
                }
            }

            if (defenseEffectCount != 1 || match == null)
            {
                throw new InvalidOperationException(
                    "Kragmor must have exactly one defense effect matching "
                        + $"runtime state {expectedState}.");
            }

            return match;
        }

        public static int CountActiveDefenseEffects(BossBattleState boss)
        {
            if (boss == null)
            {
                throw new ArgumentNullException(nameof(boss));
            }

            int count = 0;
            var effects = boss.Effects.GetActiveEffects();
            for (int index = 0; index < effects.Count; index++)
            {
                if (IsDefenseEffectId(effects[index].EffectId))
                {
                    count++;
                }
            }

            return count;
        }

        public static string GetEffectId(KragmorDefenseState defenseState)
        {
            switch (defenseState)
            {
                case KragmorDefenseState.VolcanicCarapace:
                    return KragmorRules.VolcanicCarapaceEffectId;
                case KragmorDefenseState.CoreCompression:
                    return KragmorRules.CoreCompressionEffectId;
                case KragmorDefenseState.CoreExposure:
                    return KragmorRules.CoreExposureEffectId;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(defenseState));
            }
        }

        public static bool IsDefenseEffectId(string effectId)
        {
            return string.Equals(
                    effectId,
                    KragmorRules.VolcanicCarapaceEffectId,
                    StringComparison.Ordinal)
                || string.Equals(
                    effectId,
                    KragmorRules.CoreCompressionEffectId,
                    StringComparison.Ordinal)
                || string.Equals(
                    effectId,
                    KragmorRules.CoreExposureEffectId,
                    StringComparison.Ordinal);
        }
    }
}
