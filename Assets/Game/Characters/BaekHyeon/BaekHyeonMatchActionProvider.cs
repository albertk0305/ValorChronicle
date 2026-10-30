using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.BaekHyeon
{
    public sealed class BaekHyeonMatchActionProvider
        : IMatchEventActionProvider
    {
        private readonly BaekHyeonResolvedCombatRules combatRules;

        public BaekHyeonMatchActionProvider(BaekHyeonCombatConfig config)
            : this(config, awakening: 0)
        {
        }

        public BaekHyeonMatchActionProvider(
            BaekHyeonCombatConfig config,
            int awakening)
        {
            combatRules = BaekHyeonCombatRuleResolver.Resolve(
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

            int matchSize = ResolveMatchSize(matchTier);
            long damageActionId = actionIds.Next();
            var damageAction = new DamageAction(
                damageActionId,
                ActionOrigin.Match,
                new DamageContextBuildRequest(
                    attacker,
                    party,
                    boss,
                    ElementType.Grass,
                    combatRules.MatchAttackType,
                    ResolveMatchTag(matchTier),
                    combatRules.GetMatchBaseCoefficient(matchSize),
                    combatRules.MatchUsesCombo,
                    finalComboCount,
                    canCritical: true,
                    skillCoefficientResolver: request =>
                        combatRules.CalculateMatchCoefficient(
                            matchSize,
                            BaekHyeonMarkEffectFactory.GetMarkCount(
                                request.TargetBoss))));

            int markGainAmount = combatRules.GetMarkGainAmount(matchSize);
            var actions = new List<CombatAction>(1 + markGainAmount)
            {
                damageAction
            };
            for (int index = 0; index < markGainAmount; index++)
            {
                long markActionId = actionIds.Next();
                actions.Add(new ApplyEffectAction(
                    markActionId,
                    ActionOrigin.System,
                    boss,
                    BaekHyeonMarkEffectFactory.Create(
                        markActionId,
                        combatRules),
                    damageActionId,
                    damageActionId));
            }

            return actions.AsReadOnly();
        }

        private static int ResolveMatchSize(BoardMatchTier matchTier)
        {
            switch (matchTier)
            {
                case BoardMatchTier.Three:
                    return 3;
                case BoardMatchTier.Four:
                    return 4;
                case BoardMatchTier.FiveOrMore:
                    return 5;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(matchTier),
                        matchTier,
                        "Match tier must be defined.");
            }
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
                    BaekHyeonRules.CharacterId,
                    StringComparison.Ordinal)
                || attacker.Element != ElementType.Grass)
            {
                throw new ArgumentException(
                    "Attacker must be Baek Hyeon with Grass element.",
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
