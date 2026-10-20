using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Serena
{
    public sealed class SerenaCoralspearActiveActionProvider
        : IActiveAbilityActionProvider
    {
        private const AttackTypeMask EnhancedAttackTypes =
            AttackTypeMask.Additional | AttackTypeMask.Chase;

        private readonly SerenaCoralspearResolvedCombatRules combatRules;

        public SerenaCoralspearActiveActionProvider(
            SerenaCoralspearCombatConfig config)
            : this(config, awakening: 0)
        {
        }

        public SerenaCoralspearActiveActionProvider(
            SerenaCoralspearCombatConfig config,
            int awakening)
        {
            combatRules = SerenaCoralspearCombatRuleResolver.Resolve(
                config,
                awakening);
        }

        public int CooldownTurns => combatRules.ActiveCooldownTurns;

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
                SerenaCoralspearRules.ActiveEffectId,
                actor.CharacterId,
                EffectCategory.Buff,
                EffectModifierType.AttackTypeDamageIncrease,
                combatRules.ActiveAdditionalAndChaseDamageIncreaseRate,
                combatRules.ActiveDurationTurns,
                actionId,
                EffectStackPolicy.RefreshDuration,
                attackTypeMask: EnhancedAttackTypes);
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
                    SerenaCoralspearRules.CharacterId,
                    StringComparison.Ordinal)
                || actor.Element != ElementType.Water)
            {
                throw new ArgumentException(
                    "Actor must be Serena Coralspear with Water element.",
                    nameof(actor));
            }

            if (actionIds == null)
            {
                throw new ArgumentNullException(nameof(actionIds));
            }
        }
    }
}
