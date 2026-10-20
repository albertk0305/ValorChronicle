using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;

namespace ValorChronicle.Characters.Serena
{
    public sealed class SerenaCoralspearMatch5CompletionRule
        : ICombatTriggerRule
    {
        private readonly SerenaCoralspearBattleState battleState;
        private readonly HashSet<long> processedActionIds =
            new HashSet<long>();

        public SerenaCoralspearMatch5CompletionRule(
            SerenaCoralspearBattleState battleState)
        {
            this.battleState = battleState
                ?? throw new ArgumentNullException(nameof(battleState));
        }

        public IReadOnlyList<CombatAction> CreateDerivedActions(
            CombatActionTriggerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!(context.CompletedResult is DamageActionResult result)
                || !(result.Action is DamageAction damage)
                || damage.Origin != ActionOrigin.Match
                || damage.RootActionId != damage.ActionId
                || damage.SourceActionId.HasValue
                || damage.ContextRequest.AttackType != AttackType.Match
                || (damage.ContextRequest.AttackTags
                    & AttackTag.Match5Plus) == 0
                || !ReferenceEquals(
                    damage.ContextRequest.Attacker,
                    battleState.Owner)
                || !ReferenceEquals(
                    damage.ContextRequest.Party,
                    context.Party)
                || !ReferenceEquals(
                    damage.ContextRequest.TargetBoss,
                    context.Boss)
                || !processedActionIds.Add(damage.ActionId))
            {
                return Array.Empty<CombatAction>();
            }

            battleState.TryGrantEmpoweredChase();
            return Array.Empty<CombatAction>();
        }
    }
}
