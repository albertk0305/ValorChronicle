using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Integration;

namespace ValorChronicle.Characters.BaekHyeon
{
    public sealed class BaekHyeonExposureApplicationRule
        : ICombatTriggerRule
    {
        private const long MinimumAppliedDamage = 1L;

        private readonly BaekHyeonResolvedCombatRules combatRules;
        private readonly CombatActionIdSequence actionIds;
        private readonly HashSet<long> processedRootActionIds =
            new HashSet<long>();

        public BaekHyeonExposureApplicationRule(
            BaekHyeonCombatConfig config,
            int awakening,
            CombatActionIdSequence actionIds)
        {
            combatRules = BaekHyeonCombatRuleResolver.Resolve(
                config,
                awakening);
            this.actionIds = actionIds
                ?? throw new ArgumentNullException(nameof(actionIds));
        }

        public IReadOnlyList<CombatAction> CreateDerivedActions(
            CombatActionTriggerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!combatRules.ExposureEnabled
                || !combatRules.ApplyExposureAfterMarkGain
                || !TryGetCompletedMarkApplication(
                    context,
                    out ApplyEffectAction markAction)
                || !TryGetEligibleMatchDamage(
                    context,
                    markAction,
                    out DamageActionResult damageResult)
                || damageResult.AppliedDamage < MinimumAppliedDamage
                || CountCompletedMarkApplications(
                    context.History,
                    damageResult.Action.ActionId)
                    < combatRules.GetMarkGainAmount(
                        combatRules.ExposureMinimumMatchSize)
                || !processedRootActionIds.Add(
                    damageResult.Action.ActionId))
            {
                return Array.Empty<CombatAction>();
            }

            long actionId = actionIds.Next();
            return new CombatAction[]
            {
                new ApplyEffectAction(
                    actionId,
                    ActionOrigin.System,
                    context.Boss,
                    BaekHyeonExposureEffectFactory.Create(
                        actionId,
                        combatRules),
                    context.RootActionId,
                    context.ActionId)
            };
        }

        private static bool TryGetCompletedMarkApplication(
            CombatActionTriggerContext context,
            out ApplyEffectAction markAction)
        {
            markAction = null;
            if (!(context.CompletedResult is ApplyEffectActionResult result)
                || !(result.Action is ApplyEffectAction action)
                || action.TargetType != CombatEffectTargetType.Boss
                || !ReferenceEquals(action.TargetBoss, context.Boss)
                || !string.Equals(
                    action.Effect.EffectId,
                    BaekHyeonRules.MarkEffectId,
                    StringComparison.Ordinal)
                || !string.Equals(
                    action.Effect.SourceId,
                    BaekHyeonRules.CharacterId,
                    StringComparison.Ordinal))
            {
                return false;
            }

            markAction = action;
            return true;
        }

        private bool TryGetEligibleMatchDamage(
            CombatActionTriggerContext context,
            ApplyEffectAction markAction,
            out DamageActionResult damageResult)
        {
            damageResult = null;
            if (!markAction.SourceActionId.HasValue
                || markAction.RootActionId
                    != markAction.SourceActionId.Value
                || !context.History.TryGetResult(
                    markAction.RootActionId,
                    out CombatActionResult rootResult)
                || !(rootResult is DamageActionResult rootDamageResult)
                || !(rootDamageResult.Action is DamageAction damage)
                || damage.Origin != ActionOrigin.Match
                || damage.RootActionId != damage.ActionId
                || damage.SourceActionId.HasValue
                || damage.ContextRequest.AttackType
                    != combatRules.MatchAttackType
                || (damage.ContextRequest.AttackTags
                    & AttackTag.Match5Plus) == 0
                || !ReferenceEquals(
                    damage.ContextRequest.Party,
                    context.Party)
                || !ReferenceEquals(
                    damage.ContextRequest.TargetBoss,
                    context.Boss)
                || !string.Equals(
                    damage.ContextRequest.Attacker.CharacterId,
                    BaekHyeonRules.CharacterId,
                    StringComparison.Ordinal)
                || !combatRules.CanApplyExposure(
                    combatRules.ExposureMinimumMatchSize))
            {
                return false;
            }

            damageResult = rootDamageResult;
            return true;
        }

        private static int CountCompletedMarkApplications(
            CombatActionExecutionHistory history,
            long rootActionId)
        {
            int count = 0;
            for (int index = 0; index < history.Results.Count; index++)
            {
                if (!(history.Results[index]
                        is ApplyEffectActionResult result)
                    || !(result.Action is ApplyEffectAction action)
                    || action.RootActionId != rootActionId
                    || action.SourceActionId != rootActionId
                    || !string.Equals(
                        action.Effect.EffectId,
                        BaekHyeonRules.MarkEffectId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                count++;
            }

            return count;
        }
    }
}
