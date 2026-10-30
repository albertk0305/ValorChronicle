using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Integration;

namespace ValorChronicle.Characters.BaekHyeon
{
    public sealed class BaekHyeonMarkConsumptionRule
        : ICombatTriggerRule
    {
        private readonly BaekHyeonResolvedCombatRules combatRules;
        private readonly CombatActionIdSequence actionIds;
        private readonly HashSet<long> processedActionIds =
            new HashSet<long>();

        public BaekHyeonMarkConsumptionRule(
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

            if (!combatRules.ConsumeAllMarksOnQualifyingDamage
                || !combatRules.ConsumeMarksAfterAttackCompletion
                || !(context.CompletedResult is DamageActionResult result)
                || !(result.Action is DamageAction damage)
                || !ReferenceEquals(
                    damage.ContextRequest.Party,
                    context.Party)
                || !ReferenceEquals(
                    damage.ContextRequest.TargetBoss,
                    context.Boss)
                || !IsConsumptionAttackType(
                    damage.ContextRequest.AttackType)
                || result.AppliedDamage
                    < combatRules.MinimumActualDamageToConsumeMarks
                || !BaekHyeonMarkEffectFactory.TryGetMarkEffect(
                    context.Boss,
                    out var mark)
                || !processedActionIds.Add(damage.ActionId))
            {
                return Array.Empty<CombatAction>();
            }

            return new CombatAction[]
            {
                new RemoveEffectAction(
                    actionIds.Next(),
                    ActionOrigin.System,
                    context.Boss,
                    mark.RuntimeId,
                    context.RootActionId,
                    context.ActionId)
            };
        }

        private bool IsConsumptionAttackType(AttackType attackType)
        {
            AttackTypeMask attackTypeMask;
            switch (attackType)
            {
                case AttackType.Match:
                    attackTypeMask = AttackTypeMask.Match;
                    break;
                case AttackType.Active:
                    attackTypeMask = AttackTypeMask.Active;
                    break;
                case AttackType.Additional:
                    attackTypeMask = AttackTypeMask.Additional;
                    break;
                case AttackType.Chase:
                    attackTypeMask = AttackTypeMask.Chase;
                    break;
                case AttackType.DamageOverTime:
                    attackTypeMask = AttackTypeMask.DamageOverTime;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(attackType),
                        attackType,
                        "Attack type must be defined.");
            }

            return (combatRules.MarkConsumptionAttackTypes
                & attackTypeMask) != 0;
        }
    }
}
