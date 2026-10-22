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

namespace ValorChronicle.Characters.Miyuki
{
    public sealed class ShimotsukiMiyukiMatchActionProvider
        : IMatchEventActionProvider
    {
        private readonly ShimotsukiMiyukiCombatConfig config;
        private readonly ShimotsukiMiyukiResolvedCombatRules combatRules;
        private readonly ShimotsukiMiyukiBattleState battleState;

        public ShimotsukiMiyukiMatchActionProvider(
            ShimotsukiMiyukiCombatConfig config,
            int awakening = 0)
            : this(config, awakening, battleState: null)
        {
        }

        public ShimotsukiMiyukiMatchActionProvider(
            ShimotsukiMiyukiCombatConfig config,
            int awakening,
            ShimotsukiMiyukiBattleState battleState)
        {
            this.config = ValidateConfig(config);
            combatRules = ShimotsukiMiyukiCombatRuleResolver.Resolve(
                config,
                awakening);
            this.battleState = battleState;
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
                    ResolveMatchTag(matchTier),
                    ResolveCoefficient(matchTier),
                    appliesCombo: true,
                    finalComboCount,
                    canCritical: true));

            var actions = new List<CombatAction>
            {
                damageAction,
                new AddResourceAction(
                    actionIds.Next(),
                    ActionOrigin.System,
                    boss,
                    WaterElementResource.Id,
                    ResolveWaterAmount(matchTier)
                        + ResolveFrostEchoBonus(attacker),
                    damageActionId,
                    damageActionId)
            };

            if (matchTier == BoardMatchTier.FiveOrMore)
            {
                long effectActionId = actionIds.Next();
                actions.Add(new ApplyEffectAction(
                    effectActionId,
                    ActionOrigin.System,
                    party,
                    new EffectInstance(
                        effectActionId,
                        ShimotsukiMiyukiRules.Match5EffectId,
                        attacker.CharacterId,
                        EffectCategory.Buff,
                        EffectModifierType.ElementDamageIncrease,
                        combatRules.Match5WaterDamageIncreaseRate,
                        combatRules.Match5EffectDurationTurns,
                        effectActionId,
                        EffectStackPolicy.RefreshDuration,
                        elementFilter: ElementType.Water),
                    damageActionId,
                    damageActionId));
            }

            return actions.AsReadOnly();
        }

        private double ResolveCoefficient(BoardMatchTier matchTier)
        {
            switch (matchTier)
            {
                case BoardMatchTier.Three:
                    return combatRules.Match3Coefficient;
                case BoardMatchTier.Four:
                    return combatRules.Match4Coefficient;
                case BoardMatchTier.FiveOrMore:
                    return combatRules.Match5Coefficient;
                default:
                    throw new ArgumentOutOfRangeException(nameof(matchTier));
            }
        }

        private int ResolveWaterAmount(BoardMatchTier matchTier)
        {
            switch (matchTier)
            {
                case BoardMatchTier.Three:
                    return combatRules.Match3WaterAmount;
                case BoardMatchTier.Four:
                    return combatRules.Match4WaterAmount;
                case BoardMatchTier.FiveOrMore:
                    return combatRules.Match5WaterAmount;
                default:
                    throw new ArgumentOutOfRangeException(nameof(matchTier));
            }
        }

        private int ResolveFrostEchoBonus(CharacterBattleState attacker)
        {
            if (!combatRules.HasFrostEcho
                || battleState == null
                || !ReferenceEquals(battleState.Owner, attacker)
                || !battleState.HasFrostEcho)
            {
                return 0;
            }

            return combatRules.FrostEchoExtraWaterAmount;
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
                    throw new ArgumentOutOfRangeException(nameof(matchTier));
            }
        }

        private static ShimotsukiMiyukiCombatConfig ValidateConfig(
            ShimotsukiMiyukiCombatConfig value)
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
                    ShimotsukiMiyukiRules.CharacterId,
                    StringComparison.Ordinal)
                || attacker.Element != ElementType.Water)
            {
                throw new ArgumentException(
                    "Attacker must be Shimotsuki Miyuki with Water element.",
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
                throw new ArgumentOutOfRangeException(nameof(finalComboCount));
            }

            if (actionIds == null)
            {
                throw new ArgumentNullException(nameof(actionIds));
            }
        }
    }
}
