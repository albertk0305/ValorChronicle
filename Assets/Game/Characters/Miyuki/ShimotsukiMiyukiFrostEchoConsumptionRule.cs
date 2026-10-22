using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Characters.Miyuki
{
    public sealed class ShimotsukiMiyukiFrostEchoConsumptionRule
        : ICombatTriggerRule
    {
        private readonly ShimotsukiMiyukiBattleState battleState;
        private readonly ShimotsukiMiyukiResolvedCombatRules combatRules;
        private readonly HashSet<long> processedActionIds =
            new HashSet<long>();

        public ShimotsukiMiyukiFrostEchoConsumptionRule(
            ShimotsukiMiyukiBattleState battleState,
            ShimotsukiMiyukiCombatConfig config,
            int awakening)
        {
            this.battleState = battleState
                ?? throw new ArgumentNullException(nameof(battleState));
            combatRules = ShimotsukiMiyukiCombatRuleResolver.Resolve(
                config,
                awakening);
        }

        public IReadOnlyList<CombatAction> CreateDerivedActions(
            CombatActionTriggerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!combatRules.HasFrostEcho
                || !battleState.HasFrostEcho
                || !(context.CompletedResult is AddResourceActionResult result)
                || !(result.Action is AddResourceAction add)
                || add.ResourceId != WaterElementResource.Id
                || !ReferenceEquals(add.TargetBoss, context.Boss)
                || !context.TryGetSourceActionResult(
                    out CombatActionResult sourceResult)
                || !(sourceResult is DamageActionResult damageResult)
                || !(damageResult.Action is DamageAction damage)
                || damage.Origin != ActionOrigin.Match
                || !ReferenceEquals(
                    damage.ContextRequest.Attacker,
                    battleState.Owner)
                || damage.ContextRequest.AttackType != AttackType.Match
                || add.Amount != ResolveEnhancedAmount(
                    damage.ContextRequest.AttackTags)
                || !processedActionIds.Add(add.ActionId))
            {
                return Array.Empty<CombatAction>();
            }

            battleState.TryConsumeFrostEcho();
            return Array.Empty<CombatAction>();
        }

        private int ResolveEnhancedAmount(AttackTag tags)
        {
            int baseAmount;
            if ((tags & AttackTag.Match5Plus) != 0)
            {
                baseAmount = combatRules.Match5WaterAmount;
            }
            else if ((tags & AttackTag.Match4) != 0)
            {
                baseAmount = combatRules.Match4WaterAmount;
            }
            else if ((tags & AttackTag.Match3) != 0)
            {
                baseAmount = combatRules.Match3WaterAmount;
            }
            else
            {
                return -1;
            }

            return checked(
                baseAmount + combatRules.FrostEchoExtraWaterAmount);
        }
    }
}
