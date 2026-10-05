using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Marea
{
    public sealed class MareaBluefangMatchActionProvider
        : IMatchEventActionProvider
    {
        private readonly MareaBluefangCombatConfig config;
        private readonly MareaBluefangResolvedCombatRules combatRules;

        public MareaBluefangMatchActionProvider(
            MareaBluefangCombatConfig config)
            : this(config, awakening: 0)
        {
        }

        public MareaBluefangMatchActionProvider(
            MareaBluefangCombatConfig config,
            int awakening)
        {
            this.config = ValidateConfig(config);
            combatRules = MareaBluefangCombatRuleResolver.Resolve(
                config,
                awakening);
        }

        public IReadOnlyList<CombatAction> CreateRootActions(
            MatchEventActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return CreateActions(
                context.Character,
                context.Party,
                context.Boss,
                context.MatchEvent.Tier,
                context.FinalComboCount,
                context.ActionIds);
        }

        /// <summary>
        /// Creates one attack sequence and snapshots WaterElement at this
        /// invocation boundary. Call when Marea's attack begins.
        /// </summary>
        public IReadOnlyList<CombatAction> CreateActions(
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss,
            BoardMatchTier matchTier,
            int finalComboCount,
            CombatActionIdSequence actionIds)
        {
            ValidateInputs(
                attacker,
                party,
                boss,
                matchTier,
                finalComboCount,
                actionIds);

            ResourceState water = boss.Resources.Get(
                WaterElementResource.Id);
            if (water.MaxAmount != config.WaterElementMaxAmount)
            {
                throw new InvalidOperationException(
                    "WaterElement must use the shared maximum amount.");
            }

            int waterAtAttackStart = water.CurrentAmount;
            bool hasWaterAtAttackStart = waterAtAttackStart > 0;
            double localDealtDamageIncrease = hasWaterAtAttackStart
                ? combatRules.PassiveDealtDamageIncreaseRate
                : 0d;
            AttackTag matchTag = ResolveMatchTag(matchTier);
            double skillCoefficient = ResolveSkillCoefficient(
                matchTier,
                waterAtAttackStart);

            long damageActionId = actionIds.Next();
            var damageAction = new DamageAction(
                damageActionId,
                ActionOrigin.Match,
                new DamageContextBuildRequest(
                    attacker,
                    party,
                    boss,
                    ElementType.Water,
                    AttackType.Match,
                    matchTag,
                    skillCoefficient,
                    true,
                    finalComboCount,
                    true,
                    actionLocalDealtDamageIncreaseRate:
                        localDealtDamageIncrease));

            if (matchTier == BoardMatchTier.FiveOrMore)
            {
                if (waterAtAttackStart == 0)
                {
                    return new CombatAction[] { damageAction };
                }

                long consumeActionId = actionIds.Next();
                var actions = new List<CombatAction>
                {
                    damageAction,
                    new ConsumeResourceAction(
                        consumeActionId,
                        ActionOrigin.System,
                        boss,
                        WaterElementResource.Id,
                        attacker.CharacterId,
                        ResourceConsumptionMode.Amount,
                        waterAtAttackStart,
                        damageActionId,
                        damageActionId)
                };

                if (combatRules.HasLingeringSurge
                    && waterAtAttackStart
                        == MareaBluefangCombatRuleResolver
                            .LingeringSurgeRequiredWaterAmount)
                {
                    actions.AddRange(
                        MareaBluefangLingeringSurgeState
                            .CreateGrantActions(
                                attacker,
                                combatRules
                                    .LingeringSurgeDamageIncreaseRate,
                                actionIds,
                                damageActionId,
                                consumeActionId));
                }

                return actions.AsReadOnly();
            }

            var nonDevourActions = new List<CombatAction>
            {
                damageAction
            };
            if (combatRules.HasLingeringSurge
                && MareaBluefangLingeringSurgeState.TryGetChargeEffects(
                    attacker,
                    out EffectInstance match3Effect,
                    out EffectInstance match4Effect))
            {
                nonDevourActions.Add(new RemoveEffectAction(
                    actionIds.Next(),
                    ActionOrigin.System,
                    attacker,
                    match3Effect.RuntimeId,
                    damageActionId,
                    damageActionId));
                nonDevourActions.Add(new RemoveEffectAction(
                    actionIds.Next(),
                    ActionOrigin.System,
                    attacker,
                    match4Effect.RuntimeId,
                    damageActionId,
                    damageActionId));
            }

            nonDevourActions.Add(new AddResourceAction(
                actionIds.Next(),
                ActionOrigin.System,
                boss,
                WaterElementResource.Id,
                1,
                damageActionId,
                damageActionId));
            return nonDevourActions.AsReadOnly();
        }

        private double ResolveSkillCoefficient(
            BoardMatchTier matchTier,
            int waterAtAttackStart)
        {
            switch (matchTier)
            {
                case BoardMatchTier.Three:
                    return combatRules.Match3Coefficient;
                case BoardMatchTier.Four:
                    return combatRules.Match4BaseCoefficient
                        + (waterAtAttackStart > 0
                            ? combatRules
                                .Match4WaterElementBonusCoefficient
                            : 0d);
                case BoardMatchTier.FiveOrMore:
                    return combatRules.Match5BaseCoefficient
                        + combatRules.Match5CoefficientPerWaterElement
                        * waterAtAttackStart;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(matchTier),
                        matchTier,
                        "Match tier must be defined.");
            }
        }

        private static MareaBluefangCombatConfig ValidateConfig(
            MareaBluefangCombatConfig value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!value.TryValidate(out string errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(value));
            }

            return value;
        }

        private static AttackTag ResolveMatchTag(BoardMatchTier matchTier)
        {
            switch (matchTier)
            {
                case BoardMatchTier.Three:
                    return AttackTag.Match3;
                case BoardMatchTier.Four:
                    return AttackTag.Match4;
                case BoardMatchTier.FiveOrMore:
                    return AttackTag.Match5Plus;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(matchTier),
                        matchTier,
                        "Match tier must be defined.");
            }
        }

        private static void ValidateInputs(
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss,
            BoardMatchTier matchTier,
            int finalComboCount,
            CombatActionIdSequence actionIds)
        {
            if (attacker == null)
            {
                throw new ArgumentNullException(nameof(attacker));
            }

            if (!string.Equals(
                    attacker.CharacterId,
                    MareaBluefangRules.CharacterId,
                    StringComparison.Ordinal)
                || attacker.Element != ElementType.Water)
            {
                throw new ArgumentException(
                    "Attacker must be Marea Bluefang with Water element.",
                    nameof(attacker));
            }

            if (party == null)
            {
                throw new ArgumentNullException(nameof(party));
            }

            if (boss == null)
            {
                throw new ArgumentNullException(nameof(boss));
            }

            if (!Enum.IsDefined(typeof(BoardMatchTier), matchTier))
            {
                throw new ArgumentOutOfRangeException(nameof(matchTier));
            }

            if (finalComboCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(finalComboCount));
            }

            if (actionIds == null)
            {
                throw new ArgumentNullException(nameof(actionIds));
            }
        }
    }
}
