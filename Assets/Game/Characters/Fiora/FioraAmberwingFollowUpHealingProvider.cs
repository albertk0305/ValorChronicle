using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Healing;
using ValorChronicle.Battle.Combat.Integration;

namespace ValorChronicle.Characters.Fiora
{
    public sealed class FioraAmberwingFollowUpHealingProvider
        : IPreMatchQueueActionProvider
    {
        private readonly FioraAmberwingResolvedCombatRules rules;
        private readonly FioraAmberwingBattleState battleState;

        public FioraAmberwingFollowUpHealingProvider(
            FioraAmberwingCombatConfig config,
            int awakening,
            FioraAmberwingBattleState battleState)
        {
            rules = FioraAmberwingCombatRuleResolver.Resolve(
                config,
                awakening);
            this.battleState = battleState
                ?? throw new ArgumentNullException(nameof(battleState));
        }

        public IReadOnlyList<CombatAction> CreateRootActions(
            PreMatchQueueActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!ReferenceEquals(context.Character, battleState.Owner))
            {
                throw new ArgumentException(
                    "Pre-match character must be this battle state's "
                        + "Fiora owner.",
                    nameof(context));
            }

            if (context.MatchEvents.Count == 0
                || !battleState.CanExecuteFollowUpHeal(
                    context.PlayerTurnNumber))
            {
                return Array.Empty<CombatAction>();
            }

            long actionId = context.ActionIds.Next();
            var action = new HealAction(
                actionId,
                ActionOrigin.System,
                new HealingContextBuildRequest(
                    context.Character,
                    context.Party,
                    rules.ActiveFollowUpHealingCoefficient,
                    appliesCombo: false,
                    finalComboCount: 0,
                    rules.ActionLocalHealingIncreaseRate));
            battleState.RegisterPassiveEligibleHeal(
                actionId,
                FioraAmberwingHealingSource.FollowUp);
            return new CombatAction[] { action };
        }
    }
}
