using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Healing;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Fiora
{
    public sealed class FioraAmberwingActiveActionProvider
        : IActiveAbilityActionProvider
    {
        private readonly FioraAmberwingResolvedCombatRules rules;
        private readonly FioraAmberwingBattleState battleState;

        public FioraAmberwingActiveActionProvider(
            FioraAmberwingCombatConfig config,
            int awakening,
            FioraAmberwingBattleState battleState)
        {
            rules = FioraAmberwingCombatRuleResolver.Resolve(config, awakening);
            this.battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
        }

        public int CooldownTurns => rules.ActiveCooldownTurns;

        public IReadOnlyList<CombatAction> CreateRootActions(ActiveAbilityActionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return CreateActions(context.Character, context.Party, context.ActionIds);
        }

        public IReadOnlyList<CombatAction> CreateActions(
            CharacterBattleState source,
            PartyBattleState party,
            CombatActionIdSequence actionIds)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!string.Equals(source.CharacterId, FioraAmberwingRules.CharacterId, StringComparison.Ordinal)
                || source.Element != ElementType.Fire)
                throw new ArgumentException("Source must be Fire Fiora Amberwing.", nameof(source));
            if (party == null) throw new ArgumentNullException(nameof(party));
            if (actionIds == null) throw new ArgumentNullException(nameof(actionIds));
            if (!ReferenceEquals(source, battleState.Owner))
                throw new ArgumentException(
                    "Source must be the Fiora owner of this battle state.",
                    nameof(source));

            long actionId = actionIds.Next();
            var action = new HealAction(
                actionId,
                ActionOrigin.Active,
                new HealingContextBuildRequest(
                    source,
                    party,
                    rules.ActiveInstantHealingCoefficient,
                    appliesCombo: false,
                    finalComboCount: 0,
                    rules.ActionLocalHealingIncreaseRate));
            battleState.RegisterPassiveEligibleHeal(
                actionId,
                FioraAmberwingHealingSource.Active);
            return new CombatAction[] { action };
        }
    }
}
