using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;

namespace ValorChronicle.Characters.Fiora
{
    public sealed class FioraAmberwingOverhealBlessingRule
        : ICombatTriggerRule
    {
        private readonly FioraAmberwingBattleState battleState;
        private readonly FioraAmberwingResolvedCombatRules rules;
        private readonly CombatActionIdSequence actionIds;

        public FioraAmberwingOverhealBlessingRule(
            FioraAmberwingBattleState battleState,
            FioraAmberwingCombatConfig config,
            int awakening,
            CombatActionIdSequence actionIds)
        {
            this.battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            rules = FioraAmberwingCombatRuleResolver.Resolve(config, awakening);
            this.actionIds = actionIds ?? throw new ArgumentNullException(nameof(actionIds));
        }

        public IReadOnlyList<CombatAction> CreateDerivedActions(
            CombatActionTriggerContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!(context.CompletedResult is HealActionResult healResult)
                || !battleState.TryTakePassiveEligibleHeal(
                    context.ActionId,
                    out FioraAmberwingHealingSource healingSource))
                return Array.Empty<CombatAction>();

            if (healingSource == FioraAmberwingHealingSource.Active)
            {
                battleState.GrantFollowUpHeals(
                    rules.ActiveFollowUpHealGrantCount,
                    context.PlayerTurnNumber);
            }
            else if (healingSource == FioraAmberwingHealingSource.FollowUp
                && !battleState.TryConsumeFollowUpHeal(
                    context.PlayerTurnNumber))
            {
                throw new InvalidOperationException(
                    "A completed Fiora follow-up heal could not consume "
                        + "its available charge.");
            }

            if (healResult.OverhealAmount <= 0 || context.Party.MaxHp <= 0)
                return Array.Empty<CombatAction>();

            double conversionRate = rules.PassiveConversionRate;
            bool isFullOverheal = healResult.FinalHealing > 0
                && healResult.AppliedHealing == 0
                && healResult.OverhealAmount > 0;
            if (rules.HasAwakening6FullOverheal
                && isFullOverheal
                && battleState.TryUseAwakening6Conversion(
                    context.PlayerTurnNumber))
            {
                conversionRate = rules.Awakening6FullOverhealConversionRate;
            }

            double overhealRatio = (double)healResult.OverhealAmount
                / context.Party.MaxHp;
            double magnitude = Math.Min(
                overhealRatio * conversionRate,
                rules.PassivePerInstanceCap);
            if (magnitude <= 0d)
                return Array.Empty<CombatAction>();

            long effectActionId = actionIds.Next();
            var effect = new EffectInstance(
                effectActionId,
                FioraAmberwingRules.PassiveBlessingEffectId,
                battleState.Owner.CharacterId,
                EffectCategory.Buff,
                EffectModifierType.AttackIncrease,
                magnitude,
                rules.PassiveDurationTurns,
                effectActionId,
                EffectStackPolicy.SeparateInstance,
                modifierGroupId:
                    FioraAmberwingRules.PassiveBlessingModifierGroupId,
                modifierGroupCap: rules.PassiveGroupCap);
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
