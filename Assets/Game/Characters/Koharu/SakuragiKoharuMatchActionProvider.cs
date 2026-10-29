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

namespace ValorChronicle.Characters.Koharu
{
    public sealed class SakuragiKoharuMatchActionProvider
        : IMatchEventActionProvider
    {
        private const AttackTypeMask EnhancedAttackTypes =
            AttackTypeMask.Additional | AttackTypeMask.Chase;

        private readonly SakuragiKoharuResolvedCombatRules combatRules;

        public SakuragiKoharuMatchActionProvider(
            SakuragiKoharuCombatConfig config,
            int awakening = 0)
        {
            combatRules = SakuragiKoharuCombatRuleResolver.Resolve(
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
                    ResolveCoefficient(matchTier),
                    appliesCombo: true,
                    finalComboCount,
                    canCritical: true));

            if (matchTier != BoardMatchTier.FiveOrMore)
            {
                return new CombatAction[] { damageAction };
            }

            long effectActionId = actionIds.Next();
            var effect = new EffectInstance(
                effectActionId,
                SakuragiKoharuRules.Match5EffectId,
                attacker.CharacterId,
                EffectCategory.Buff,
                EffectModifierType.AttackTypeDamageIncrease,
                combatRules.Match5AdditionalAndChaseDamageIncreaseRate,
                combatRules.Match5EffectDurationTurns,
                effectActionId,
                EffectStackPolicy.RefreshDuration,
                attackTypeMask: EnhancedAttackTypes);
            return new CombatAction[]
            {
                damageAction,
                new ApplyEffectAction(
                    effectActionId,
                    ActionOrigin.System,
                    party,
                    effect,
                    damageActionId,
                    damageActionId)
            };
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
                    throw new ArgumentOutOfRangeException(nameof(matchTier));
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
                    SakuragiKoharuRules.CharacterId,
                    StringComparison.Ordinal)
                || attacker.Element != ElementType.Grass)
            {
                throw new ArgumentException(
                    "Attacker must be Sakuragi Koharu with Grass element.",
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
