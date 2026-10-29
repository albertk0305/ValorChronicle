using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.Shields;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Naira
{
    public sealed class NairaWindhornMatchActionProvider
        : IMatchEventActionProvider
    {
        private readonly NairaWindhornResolvedCombatRules combatRules;

        public NairaWindhornMatchActionProvider(
            NairaWindhornCombatConfig config)
            : this(config, awakening: 0)
        {
        }

        public NairaWindhornMatchActionProvider(
            NairaWindhornCombatConfig config,
            int awakening)
        {
            combatRules = NairaWindhornCombatRuleResolver.Resolve(
                ValidateConfig(config),
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
                context.PlayerTurnNumber,
                context.ActionIds);
        }

        public IReadOnlyList<CombatAction> CreateActions(
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss,
            BoardMatchTier matchTier,
            int finalComboCount,
            int playerTurnNumber,
            CombatActionIdSequence actionIds)
        {
            ValidateInputs(
                attacker,
                party,
                boss,
                matchTier,
                finalComboCount,
                playerTurnNumber,
                actionIds);

            double shieldRate = ResolveShieldRate(matchTier);
            string abilityId = ResolveAbilityId(matchTier);
            long shieldActionId = actionIds.Next();
            var shieldAction = new ShieldAction(
                shieldActionId,
                ActionOrigin.Match,
                new ShieldGenerationContextBuildRequest(
                    attacker,
                    party,
                    shieldRate,
                    combatRules.MatchShieldUsesComboMultiplier,
                    finalComboCount),
                new ShieldGrantRequest(
                    runtimeId: shieldActionId,
                    sourceId: abilityId,
                    createdTurn: playerTurnNumber,
                    remainingTurns: combatRules.ShieldDurationTurns,
                    creationOrder: shieldActionId));

            if (matchTier == BoardMatchTier.Three)
            {
                return new CombatAction[] { shieldAction };
            }

            double baseDamageCoefficient = matchTier
                == BoardMatchTier.Four
                    ? combatRules.Match4BaseDamageCoefficient
                    : combatRules.Match5BaseDamageCoefficient;
            double chargeDamageIncreaseRate = matchTier
                == BoardMatchTier.Four
                    ? combatRules.Match4ChargeDamageIncreaseRate
                    : combatRules.Match5ChargeDamageIncreaseRate;
            long damageActionId = actionIds.Next();
            var damageAction = new DamageAction(
                damageActionId,
                ActionOrigin.Match,
                new DamageContextBuildRequest(
                    attacker,
                    party,
                    boss,
                    ElementType.Grass,
                    AttackType.Match,
                    ResolveMatchTag(matchTier),
                    baseDamageCoefficient,
                    appliesCombo: true,
                    finalComboCount,
                    canCritical: true,
                    skillCoefficientResolver: request =>
                        ResolveChargedDamageCoefficient(
                            request,
                            chargeDamageIncreaseRate)));

            return new CombatAction[]
            {
                shieldAction,
                damageAction
            };
        }

        private double ResolveShieldRate(BoardMatchTier matchTier)
        {
            switch (matchTier)
            {
                case BoardMatchTier.Three:
                    return combatRules.Match3ShieldMaxHpRate;
                case BoardMatchTier.Four:
                    return combatRules.Match4ShieldMaxHpRate;
                case BoardMatchTier.FiveOrMore:
                    return combatRules.Match5ShieldMaxHpRate;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(matchTier),
                        matchTier,
                        "Match tier must be defined.");
            }
        }

        private static string ResolveAbilityId(BoardMatchTier matchTier)
        {
            switch (matchTier)
            {
                case BoardMatchTier.Three:
                    return NairaWindhornRules.Match3AbilityId;
                case BoardMatchTier.Four:
                    return NairaWindhornRules.Match4AbilityId;
                case BoardMatchTier.FiveOrMore:
                    return NairaWindhornRules.Match5AbilityId;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(matchTier),
                        matchTier,
                        "Match tier must be defined.");
            }
        }

        private static AttackTag ResolveMatchTag(
            BoardMatchTier matchTier)
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

        private static double ResolveChargedDamageCoefficient(
            DamageContextBuildRequest request,
            double chargeDamageIncreaseRate)
        {
            double charge = Math.Min(
                (double)request.Party.Shields.TotalShield
                    / request.Attacker.MaxHp,
                1d);
            return request.SkillCoefficient
                * (1d + (charge * chargeDamageIncreaseRate));
        }

        private static NairaWindhornCombatConfig ValidateConfig(
            NairaWindhornCombatConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(config));
            }

            return config;
        }

        private static void ValidateInputs(
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss,
            BoardMatchTier matchTier,
            int finalComboCount,
            int playerTurnNumber,
            CombatActionIdSequence actionIds)
        {
            if (attacker == null)
            {
                throw new ArgumentNullException(nameof(attacker));
            }

            if (!string.Equals(
                    attacker.CharacterId,
                    NairaWindhornRules.CharacterId,
                    StringComparison.Ordinal)
                || attacker.Element != ElementType.Grass)
            {
                throw new ArgumentException(
                    "Attacker must be Naira Windhorn with Grass element.",
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

            if (playerTurnNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playerTurnNumber));
            }

            if (actionIds == null)
            {
                throw new ArgumentNullException(nameof(actionIds));
            }
        }
    }
}

