using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Combat.Actions
{
    public sealed class CombatTriggerResolver
    {
        private readonly ReadOnlyCollection<ICombatTriggerRule> rules;

        public CombatTriggerResolver(IReadOnlyList<ICombatTriggerRule> rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            var copiedRules = new ICombatTriggerRule[rules.Count];
            for (int index = 0; index < rules.Count; index++)
            {
                copiedRules[index] = rules[index]
                    ?? throw new ArgumentException(
                        "Trigger rule collection cannot contain null.",
                        nameof(rules));
            }

            this.rules = Array.AsReadOnly(copiedRules);
        }

        public IReadOnlyList<CombatAction> Resolve(
            CombatActionTriggerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var derivedActions = new List<CombatAction>();
            var derivedActionIds = new HashSet<long>();
            for (int ruleIndex = 0; ruleIndex < rules.Count; ruleIndex++)
            {
                IReadOnlyList<CombatAction> ruleActions =
                    rules[ruleIndex].CreateDerivedActions(context);
                if (ruleActions == null)
                {
                    throw new InvalidOperationException(
                        "Trigger rules must return an action collection.");
                }

                for (int actionIndex = 0;
                    actionIndex < ruleActions.Count;
                    actionIndex++)
                {
                    CombatAction action = ruleActions[actionIndex];
                    ValidateDerivedAction(
                        action,
                        context,
                        derivedActionIds);
                    derivedActions.Add(action);
                }
            }

            return derivedActions.AsReadOnly();
        }

        private static void ValidateDerivedAction(
            CombatAction action,
            CombatActionTriggerContext context,
            HashSet<long> derivedActionIds)
        {
            if (action == null)
            {
                throw new InvalidOperationException(
                    "Trigger rules cannot create a null action.");
            }

            if (action.RootActionId != context.RootActionId
                || action.SourceActionId != context.ActionId)
            {
                throw new InvalidOperationException(
                    "Derived actions must preserve root lineage and identify "
                        + "the completed action as their direct source.");
            }

            if (!derivedActionIds.Add(action.ActionId)
                || context.History.TryGetResult(
                    action.ActionId,
                    out _))
            {
                throw new InvalidOperationException(
                    $"Duplicate derived action ID: {action.ActionId}.");
            }

            if (action is DamageAction damageAction
                && (damageAction.Origin == ActionOrigin.Additional
                    || damageAction.Origin == ActionOrigin.Chase)
                && !context.CanTriggerAttackFollowUp)
            {
                throw new InvalidOperationException(
                    "The source lineage cannot trigger another attack.");
            }
        }
    }
}
