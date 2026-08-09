using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Marea
{
    public sealed class MareaBluefangActiveActionProvider
        : IActiveAbilityActionProvider
    {
        public IReadOnlyList<CombatAction> CreateRootActions(
            ActiveAbilityActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return CreateActions(context.Character, context.ActionIds);
        }

        public IReadOnlyList<CombatAction> CreateActions(
            CharacterBattleState actor,
            CombatActionIdSequence actionIds)
        {
            ValidateInputs(actor, actionIds);

            long actionId = actionIds.Next();
            var effect = new EffectInstance(
                actionId,
                MareaBluefangRules.ActiveEffectId,
                actor.CharacterId,
                EffectCategory.Buff,
                EffectModifierType.ElementDamageIncrease,
                MareaBluefangRules.ActiveWaterDamageIncreaseRate,
                MareaBluefangRules.ActiveDurationTurns,
                actionId,
                EffectStackPolicy.RefreshDuration,
                elementFilter: ElementType.Water);
            return new CombatAction[]
            {
                new ApplyEffectAction(
                    actionId,
                    ActionOrigin.Active,
                    actor,
                    effect)
            };
        }

        private static void ValidateInputs(
            CharacterBattleState actor,
            CombatActionIdSequence actionIds)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            if (!string.Equals(
                    actor.CharacterId,
                    MareaBluefangRules.CharacterId,
                    StringComparison.Ordinal)
                || actor.Element != ElementType.Water)
            {
                throw new ArgumentException(
                    "Actor must be Marea Bluefang with Water element.",
                    nameof(actor));
            }

            if (actionIds == null)
            {
                throw new ArgumentNullException(nameof(actionIds));
            }
        }
    }
}
