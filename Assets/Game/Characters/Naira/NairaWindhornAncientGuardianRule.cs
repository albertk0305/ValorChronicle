using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Naira
{
    public sealed class NairaWindhornAncientGuardianRule
        : ICombatTriggerRule
    {
        private readonly CharacterBattleState owner;
        private readonly NairaWindhornResolvedCombatRules combatRules;
        private readonly CombatActionIdSequence actionIds;
        private readonly HashSet<long> processedShieldActionIds =
            new HashSet<long>();

        public NairaWindhornAncientGuardianRule(
            CharacterBattleState owner,
            NairaWindhornCombatConfig config,
            int awakening,
            CombatActionIdSequence actionIds)
        {
            this.owner = owner
                ?? throw new ArgumentNullException(nameof(owner));
            if (!string.Equals(
                    owner.CharacterId,
                    NairaWindhornRules.CharacterId,
                    StringComparison.Ordinal)
                || owner.Element != ElementType.Grass)
            {
                throw new ArgumentException(
                    "Owner must be Naira Windhorn with Grass element.",
                    nameof(owner));
            }

            combatRules = NairaWindhornCombatRuleResolver.Resolve(
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

            if (!combatRules.AncientGuardianEnabled
                || !(context.CompletedResult is ShieldActionResult)
                || !(context.CompletedAction is ShieldAction shieldAction)
                || shieldAction.Origin != ActionOrigin.Match
                || !string.Equals(
                    shieldAction.GrantRequest.SourceId,
                    NairaWindhornRules.Match5AbilityId,
                    StringComparison.Ordinal)
                || !ReferenceEquals(
                    shieldAction.ContextRequest.Source,
                    owner)
                || !ReferenceEquals(
                    shieldAction.ContextRequest.Party,
                    context.Party)
                || !processedShieldActionIds.Add(shieldAction.ActionId))
            {
                return Array.Empty<CombatAction>();
            }

            var result = (ShieldActionResult)context.CompletedResult;
            if (result.ApplicationResult.TotalShieldAfter < owner.MaxHp)
            {
                return Array.Empty<CombatAction>();
            }

            long effectActionId = actionIds.Next();
            var effect = new EffectInstance(
                effectActionId,
                NairaWindhornRules.AncientGuardianEffectId,
                owner.CharacterId,
                EffectCategory.Buff,
                EffectModifierType.NextBossHitDamageReduction,
                combatRules.AncientGuardianDamageReductionRate,
                remainingTurns: null,
                creationOrder: effectActionId,
                stackPolicy: EffectStackPolicy.Unique);
            return new CombatAction[]
            {
                new ApplyEffectAction(
                    effectActionId,
                    ActionOrigin.System,
                    context.Party,
                    effect,
                    context.RootActionId,
                    context.ActionId)
            };
        }
    }
}
