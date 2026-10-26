using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Brina
{
    public sealed class BrinaAshwoodMatchCompletionRule
        : ICombatTriggerRule
    {
        private readonly BrinaAshwoodBattleState battleState;
        private readonly BrinaAshwoodResolvedCombatRules combatRules;
        private readonly CombatActionIdSequence actionIds;
        private readonly HashSet<long> processedActionIds =
            new HashSet<long>();

        public BrinaAshwoodMatchCompletionRule(
            BrinaAshwoodBattleState battleState,
            BrinaAshwoodCombatConfig config,
            int awakening,
            CombatActionIdSequence actionIds)
        {
            this.battleState = battleState
                ?? throw new ArgumentNullException(nameof(battleState));
            combatRules = BrinaAshwoodCombatRuleResolver.Resolve(
                config,
                awakening);
            if (battleState.WoodcutTriggerThreshold
                != combatRules.WoodcutTriggerThreshold)
            {
                throw new ArgumentException(
                    "Battle state and combat rules must use the same "
                        + "woodcut trigger threshold.",
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

            if (!TryGetCompletedBrinaMatch(
                context,
                out DamageAction matchDamage))
            {
                return Array.Empty<CombatAction>();
            }

            int gainAmount = ResolveWoodcutGain(
                matchDamage.ContextRequest.AttackTags,
                context.PlayerTurnNumber);
            int triggerCount = battleState.GainWoodcut(gainAmount);
            if (triggerCount == 0)
            {
                return Array.Empty<CombatAction>();
            }

            var actions = new CombatAction[triggerCount];
            for (int index = 0; index < actions.Length; index++)
            {
                actions[index] = CreatePassiveAdditional(
                    context,
                    matchDamage);
            }

            return actions;
        }

        private bool TryGetCompletedBrinaMatch(
            CombatActionTriggerContext context,
            out DamageAction matchDamage)
        {
            matchDamage = null;
            if (!(context.CompletedResult is DamageActionResult result)
                || !(result.Action is DamageAction damage)
                || damage.Origin != ActionOrigin.Match
                || damage.RootActionId != damage.ActionId
                || damage.SourceActionId.HasValue
                || damage.ContextRequest.AttackType != AttackType.Match
                || damage.ContextRequest.AttackElement != ElementType.Grass
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
                return false;
            }

            AttackTag matchTags = damage.ContextRequest.AttackTags
                & (AttackTag.Match3
                    | AttackTag.Match4
                    | AttackTag.Match5Plus);
            if (matchTags != AttackTag.Match3
                && matchTags != AttackTag.Match4
                && matchTags != AttackTag.Match5Plus)
            {
                return false;
            }

            matchDamage = damage;
            return true;
        }

        private int ResolveBaseWoodcutGain(AttackTag attackTags)
        {
            if ((attackTags & AttackTag.Match3) != 0)
            {
                return combatRules.Match3WoodcutGainAmount;
            }

            if ((attackTags & AttackTag.Match4) != 0)
            {
                return combatRules.Match4WoodcutGainAmount;
            }

            if ((attackTags & AttackTag.Match5Plus) != 0)
            {
                return combatRules.Match5WoodcutGainAmount;
            }

            throw new ArgumentOutOfRangeException(nameof(attackTags));
        }

        private int ResolveWoodcutGain(
            AttackTag attackTags,
            int playerTurnNumber)
        {
            int gainAmount = ResolveBaseWoodcutGain(attackTags);
            if (HasActiveWoodcutEffect())
            {
                double multiplied = gainAmount
                    * combatRules.ActiveWoodcutGainMultiplier;
                if (double.IsNaN(multiplied)
                    || double.IsInfinity(multiplied)
                    || multiplied > int.MaxValue)
                {
                    throw new OverflowException(
                        "Active woodcut gain must fit in an integer.");
                }

                gainAmount = checked((int)Math.Round(
                    multiplied,
                    MidpointRounding.AwayFromZero));
            }

            bool isFirstGainThisTurn =
                battleState.TryMarkFirstWoodcutGainForTurn(
                    playerTurnNumber);
            if (isFirstGainThisTurn
                && combatRules.FirstWoodcutBonusAmount > 0)
            {
                gainAmount = checked(
                    gainAmount
                    + combatRules.FirstWoodcutBonusAmount);
            }

            return gainAmount;
        }

        private bool HasActiveWoodcutEffect()
        {
            IReadOnlyList<EffectInstance> effects =
                battleState.Owner.Effects.GetActiveEffects();
            for (int index = 0; index < effects.Count; index++)
            {
                if (string.Equals(
                    effects[index].EffectId,
                    BrinaAshwoodRules.ActiveEffectId,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private DamageAction CreatePassiveAdditional(
            CombatActionTriggerContext context,
            DamageAction matchDamage)
        {
            return new DamageAction(
                actionIds.Next(),
                ActionOrigin.Additional,
                new DamageContextBuildRequest(
                    battleState.Owner,
                    context.Party,
                    context.Boss,
                    ElementType.Grass,
                    AttackType.Additional,
                    AttackTag.None,
                    combatRules.PassiveAdditionalCoefficient,
                    appliesCombo: false,
                    finalComboCount: 0,
                    canCritical: true),
                matchDamage.ActionId,
                matchDamage.ActionId);
        }
    }
}
