using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Serena
{
    public sealed class SerenaCoralspearMatchActionProvider
        : IMatchEventActionProvider
    {
        private readonly SerenaCoralspearCombatConfig config;
        private readonly SerenaCoralspearResolvedCombatRules combatRules;

        public SerenaCoralspearMatchActionProvider(
            SerenaCoralspearCombatConfig config)
            : this(config, awakening: 0)
        {
        }

        public SerenaCoralspearMatchActionProvider(
            SerenaCoralspearCombatConfig config,
            int awakening)
        {
            this.config = ValidateConfig(config);
            combatRules = SerenaCoralspearCombatRuleResolver.Resolve(
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
        /// Creates one match sequence and snapshots WaterElement at this
        /// invocation boundary. Call when Serena's attack begins.
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

            bool hasWaterAtAttackStart = water.CurrentAmount > 0;
            AttackTag matchTag = ResolveMatchTag(matchTier);
            long matchActionId = actionIds.Next();
            var matchDamage = CreateDamage(
                matchActionId,
                ActionOrigin.Match,
                AttackType.Match,
                attacker,
                party,
                boss,
                matchTag,
                ResolveMatchCoefficient(matchTier),
                finalComboCount);

            switch (matchTier)
            {
                case BoardMatchTier.Three:
                    return new CombatAction[]
                    {
                        matchDamage,
                        new AddResourceAction(
                            actionIds.Next(),
                            ActionOrigin.System,
                            boss,
                            WaterElementResource.Id,
                            combatRules.Match3WaterElementGainAmount,
                            matchActionId,
                            matchActionId)
                    };

                case BoardMatchTier.Four:
                    if (!hasWaterAtAttackStart)
                    {
                        return new CombatAction[] { matchDamage };
                    }

                    return new CombatAction[]
                    {
                        matchDamage,
                        CreateDamage(
                            actionIds.Next(),
                            ActionOrigin.Additional,
                            AttackType.Additional,
                            attacker,
                            party,
                            boss,
                            matchTag,
                            combatRules.Match4AdditionalCoefficient,
                            finalComboCount,
                            combatRules
                                .AdditionalAndChaseDamageIncreaseRate,
                            matchActionId,
                            matchActionId)
                    };

                case BoardMatchTier.FiveOrMore:
                    if (combatRules.Match5WaterElementGainAmount == 0)
                    {
                        return new CombatAction[] { matchDamage };
                    }

                    return new CombatAction[]
                    {
                        matchDamage,
                        new AddResourceAction(
                            actionIds.Next(),
                            ActionOrigin.System,
                            boss,
                            WaterElementResource.Id,
                            combatRules.Match5WaterElementGainAmount,
                            matchActionId,
                            matchActionId)
                    };

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(matchTier),
                        matchTier,
                        "Match tier must be defined.");
            }
        }

        private double ResolveMatchCoefficient(BoardMatchTier matchTier)
        {
            switch (matchTier)
            {
                case BoardMatchTier.Three:
                    return combatRules.Match3Coefficient;
                case BoardMatchTier.Four:
                    return combatRules.Match4BaseCoefficient;
                case BoardMatchTier.FiveOrMore:
                    return combatRules.Match5Coefficient;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(matchTier),
                        matchTier,
                        "Match tier must be defined.");
            }
        }

        private static DamageAction CreateDamage(
            long actionId,
            ActionOrigin origin,
            AttackType attackType,
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss,
            AttackTag matchTag,
            double skillCoefficient,
            int finalComboCount,
            double actionLocalAttackTypeDamageIncreaseRate = 0d,
            long? rootActionId = null,
            long? sourceActionId = null)
        {
            return new DamageAction(
                actionId,
                origin,
                new DamageContextBuildRequest(
                    attacker,
                    party,
                    boss,
                    ElementType.Water,
                    attackType,
                    matchTag,
                    skillCoefficient,
                    appliesCombo: true,
                    finalComboCount,
                    canCritical: true,
                    actionLocalAttackTypeDamageIncreaseRate:
                        actionLocalAttackTypeDamageIncreaseRate),
                rootActionId,
                sourceActionId);
        }

        private static SerenaCoralspearCombatConfig ValidateConfig(
            SerenaCoralspearCombatConfig value)
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
                    SerenaCoralspearRules.CharacterId,
                    StringComparison.Ordinal)
                || attacker.Element != ElementType.Water)
            {
                throw new ArgumentException(
                    "Attacker must be Serena Coralspear with Water element.",
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
