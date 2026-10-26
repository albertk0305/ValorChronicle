using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Brina
{
    public sealed class BrinaAshwoodActiveActionProvider
        : IActiveAbilityActionProvider
    {
        private readonly BrinaAshwoodResolvedCombatRules combatRules;

        public BrinaAshwoodActiveActionProvider(
            BrinaAshwoodCombatConfig config)
            : this(config, awakening: 0)
        {
        }

        public BrinaAshwoodActiveActionProvider(
            BrinaAshwoodCombatConfig config,
            int awakening)
        {
            combatRules = BrinaAshwoodCombatRuleResolver.Resolve(
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
                BrinaAshwoodRules.ActiveEffectId,
                actor.CharacterId,
                EffectCategory.Buff,
                EffectModifierType.AttackIncrease,
                0d,
                combatRules.ActiveDurationTurns,
                actionId,
                EffectStackPolicy.RefreshDuration);
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
                    BrinaAshwoodRules.CharacterId,
                    StringComparison.Ordinal)
                || actor.Element != ElementType.Grass)
            {
                throw new ArgumentException(
                    "Actor must be Brina Ashwood with Grass element.",
                    nameof(actor));
            }

            if (actionIds == null)
            {
                throw new ArgumentNullException(nameof(actionIds));
            }
        }
    }
}
