using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Characters.Marea
{
    public static class MareaBluefangLingeringSurgeState
    {
        public static bool HasCharge(CharacterBattleState character)
        {
            ValidateCharacter(character);
            return TryGetChargeEffects(
                character,
                out _,
                out _);
        }

        internal static bool TryGetChargeEffects(
            CharacterBattleState character,
            out EffectInstance match3Effect,
            out EffectInstance match4Effect)
        {
            ValidateCharacter(character);
            match3Effect = FindEffect(
                character,
                MareaBluefangRules.LingeringSurgeMatch3EffectId);
            match4Effect = FindEffect(
                character,
                MareaBluefangRules.LingeringSurgeMatch4EffectId);
            return match3Effect != null && match4Effect != null;
        }

        internal static IReadOnlyList<CombatAction> CreateGrantActions(
            CharacterBattleState character,
            double damageIncreaseRate,
            CombatActionIdSequence actionIds,
            long rootActionId,
            long sourceActionId)
        {
            ValidateCharacter(character);
            if (double.IsNaN(damageIncreaseRate)
                || double.IsInfinity(damageIncreaseRate)
                || damageIncreaseRate < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(damageIncreaseRate));
            }

            if (actionIds == null)
            {
                throw new ArgumentNullException(nameof(actionIds));
            }

            ApplyEffectAction match3 = CreateGrantAction(
                character,
                damageIncreaseRate,
                actionIds,
                MareaBluefangRules.LingeringSurgeMatch3EffectId,
                AttackTag.Match3,
                rootActionId,
                sourceActionId);
            ApplyEffectAction match4 = CreateGrantAction(
                character,
                damageIncreaseRate,
                actionIds,
                MareaBluefangRules.LingeringSurgeMatch4EffectId,
                AttackTag.Match4,
                rootActionId,
                sourceActionId);
            return new CombatAction[] { match3, match4 };
        }

        private static ApplyEffectAction CreateGrantAction(
            CharacterBattleState character,
            double damageIncreaseRate,
            CombatActionIdSequence actionIds,
            string effectId,
            AttackTag requiredAttackTag,
            long rootActionId,
            long sourceActionId)
        {
            long actionId = actionIds.Next();
            var effect = new EffectInstance(
                actionId,
                effectId,
                character.CharacterId,
                EffectCategory.Buff,
                EffectModifierType.AttackTypeDamageIncrease,
                damageIncreaseRate,
                remainingTurns: null,
                creationOrder: actionId,
                EffectStackPolicy.Unique,
                attackTypeMask: AttackTypeMask.Match,
                requiredAttackTags: requiredAttackTag);
            return new ApplyEffectAction(
                actionId,
                ActionOrigin.System,
                character,
                effect,
                rootActionId,
                sourceActionId);
        }

        private static EffectInstance FindEffect(
            CharacterBattleState character,
            string effectId)
        {
            IReadOnlyList<EffectInstance> effects =
                character.Effects.FindByEffectId(effectId);
            return effects.Count > 0 ? effects[0] : null;
        }

        private static void ValidateCharacter(
            CharacterBattleState character)
        {
            if (character == null)
            {
                throw new ArgumentNullException(nameof(character));
            }

            if (!string.Equals(
                character.CharacterId,
                MareaBluefangRules.CharacterId,
                StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Lingering Surge state belongs only to Marea Bluefang.",
                    nameof(character));
            }
        }
    }
}
