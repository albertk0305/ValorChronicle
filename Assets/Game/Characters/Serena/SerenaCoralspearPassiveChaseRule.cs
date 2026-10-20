using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Serena
{
    public sealed class SerenaCoralspearPassiveChaseRule
        : ICombatTriggerRule
    {
        private readonly SerenaCoralspearBattleState battleState;
        private readonly SerenaCoralspearResolvedCombatRules combatRules;
        private readonly CombatActionIdSequence actionIds;
        private readonly HashSet<long> processedConsumptionActionIds =
            new HashSet<long>();
        private readonly HashSet<long> empoweredChaseActionIds =
            new HashSet<long>();

        public SerenaCoralspearPassiveChaseRule(
            SerenaCoralspearBattleState battleState,
            SerenaCoralspearCombatConfig config,
            int awakening,
            CombatActionIdSequence actionIds)
        {
            this.battleState = battleState
                ?? throw new ArgumentNullException(nameof(battleState));
            combatRules = SerenaCoralspearCombatRuleResolver.Resolve(
                config,
                awakening);
            if (battleState.EmpoweredChaseMaxCharges
                != combatRules.EmpoweredChaseMaxCharges)
            {
                throw new ArgumentException(
                    "Battle state and combat rules must use the same "
                        + "empowered chase maximum.",
                    nameof(battleState));
            }

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

            ConsumeCompletedEmpoweredChase(context);

            if (!TryGetEligibleConsumption(
                    context,
                    out ResourceConsumptionRecord consumption))
            {
                return Array.Empty<CombatAction>();
            }

            bool isEmpowered = battleState.HasEmpoweredChase;
            long chaseActionId = actionIds.Next();
            double attackTypeDamageIncreaseRate =
                combatRules.AdditionalAndChaseDamageIncreaseRate
                + (isEmpowered
                    ? combatRules.EmpoweredChaseDamageIncreaseRate
                    : 0d);
            var chase = new DamageAction(
                chaseActionId,
                ActionOrigin.Chase,
                new DamageContextBuildRequest(
                    battleState.Owner,
                    context.Party,
                    context.Boss,
                    ElementType.Water,
                    AttackType.Chase,
                    AttackTag.None,
                    combatRules.PassiveChaseCoefficientPerWaterElement
                        * consumption.ConsumedAmount,
                    appliesCombo: false,
                    finalComboCount: 0,
                    canCritical: true,
                    actionLocalAttackTypeDamageIncreaseRate:
                        attackTypeDamageIncreaseRate),
                context.RootActionId,
                context.ActionId);

            if (isEmpowered)
            {
                empoweredChaseActionIds.Add(chaseActionId);
            }

            return new CombatAction[] { chase };
        }

        private bool TryGetEligibleConsumption(
            CombatActionTriggerContext context,
            out ResourceConsumptionRecord consumption)
        {
            consumption = null;
            if (!(context.CompletedResult is
                    ConsumeResourceActionResult consumeResult)
                || consumeResult.ConsumptionRecord == null
                || !(consumeResult.Action is ConsumeResourceAction action)
                || !string.Equals(
                    consumeResult.ConsumptionRecord.ResourceId,
                    WaterElementResource.Id,
                    StringComparison.Ordinal)
                || !string.Equals(
                    action.ResourceId,
                    WaterElementResource.Id,
                    StringComparison.Ordinal)
                || consumeResult.ConsumptionRecord.ConsumedAmount <= 0
                || !ReferenceEquals(action.TargetBoss, context.Boss)
                || !string.Equals(
                    consumeResult.ConsumptionRecord.TargetId,
                    context.Boss.BossId,
                    StringComparison.Ordinal)
                || context.Boss.IsDefeated
                || context.Party.IsIncapacitated
                || !ContainsCharacter(context.Party, battleState.Owner)
                || !context.CanTriggerAttackFollowUp
                || !context.TryGetSourceActionResult(
                    out CombatActionResult sourceResult)
                || !(sourceResult is DamageActionResult sourceDamageResult)
                || !(sourceDamageResult.Action is DamageAction damage)
                || !ContainsCharacter(
                    context.Party,
                    damage.ContextRequest.Attacker)
                || !ReferenceEquals(
                    damage.ContextRequest.Party,
                    context.Party)
                || !ReferenceEquals(
                    damage.ContextRequest.TargetBoss,
                    context.Boss)
                || ReferenceEquals(
                    damage.ContextRequest.Attacker,
                    battleState.Owner)
                || string.Equals(
                    consumeResult.ConsumptionRecord.ConsumerId,
                    battleState.Owner.CharacterId,
                    StringComparison.Ordinal)
                || !string.Equals(
                    consumeResult.ConsumptionRecord.ConsumerId,
                    damage.ContextRequest.Attacker.CharacterId,
                    StringComparison.Ordinal)
                || !string.Equals(
                    action.ConsumerId,
                    damage.ContextRequest.Attacker.CharacterId,
                    StringComparison.Ordinal)
                || !processedConsumptionActionIds.Add(context.ActionId))
            {
                return false;
            }

            consumption = consumeResult.ConsumptionRecord;
            return true;
        }

        private void ConsumeCompletedEmpoweredChase(
            CombatActionTriggerContext context)
        {
            if (!(context.CompletedResult is DamageActionResult result)
                || !(result.Action is DamageAction damage)
                || damage.Origin != ActionOrigin.Chase
                || damage.ContextRequest.AttackType != AttackType.Chase
                || !ReferenceEquals(
                    damage.ContextRequest.Attacker,
                    battleState.Owner)
                || !empoweredChaseActionIds.Remove(damage.ActionId))
            {
                return;
            }

            if (!battleState.TryConsumeEmpoweredChase())
            {
                throw new InvalidOperationException(
                    "An empowered chase completed without a reserved charge.");
            }
        }

        private static bool ContainsCharacter(
            PartyBattleState party,
            CharacterBattleState character)
        {
            for (int index = 0; index < party.Characters.Count; index++)
            {
                if (ReferenceEquals(party.Characters[index], character))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
