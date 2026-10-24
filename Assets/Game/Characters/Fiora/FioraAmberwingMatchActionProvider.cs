using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Healing;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Fiora
{
    public sealed class FioraAmberwingMatchActionProvider
        : IMatchEventActionProvider
    {
        private readonly FioraAmberwingResolvedCombatRules rules;
        private readonly FioraAmberwingBattleState battleState;

        public FioraAmberwingMatchActionProvider(
            FioraAmberwingCombatConfig config,
            int awakening,
            FioraAmberwingBattleState battleState)
        {
            rules = FioraAmberwingCombatRuleResolver.Resolve(config, awakening);
            this.battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
        }

        public IReadOnlyList<CombatAction> CreateRootActions(MatchEventActionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return CreateActions(
                context.Character,
                context.Party,
                context.MatchEvent.Tier,
                context.FinalComboCount,
                context.ActionIds);
        }

        public IReadOnlyList<CombatAction> CreateActions(
            CharacterBattleState source,
            PartyBattleState party,
            BoardMatchTier matchTier,
            int finalComboCount,
            CombatActionIdSequence actionIds)
        {
            ValidateInputs(source, party, matchTier, finalComboCount, actionIds);
            if (!ReferenceEquals(source, battleState.Owner))
            {
                throw new ArgumentException(
                    "Source must be the Fiora owner of this battle state.",
                    nameof(source));
            }

            long healActionId = actionIds.Next();
            var heal = new HealAction(
                healActionId,
                ActionOrigin.Match,
                new HealingContextBuildRequest(
                    source,
                    party,
                    ResolveHealingCoefficient(matchTier),
                    appliesCombo: true,
                    finalComboCount,
                    rules.ActionLocalHealingIncreaseRate));
            battleState.RegisterPassiveEligibleHeal(
                healActionId,
                FioraAmberwingHealingSource.Match);

            if (matchTier == BoardMatchTier.Three)
                return new CombatAction[] { heal };

            string effectId = matchTier == BoardMatchTier.Four
                ? FioraAmberwingRules.Match4AttackEffectId
                : FioraAmberwingRules.Match5AttackEffectId;
            double magnitude = matchTier == BoardMatchTier.Four
                ? rules.Match4AttackIncreaseRate
                : rules.Match5AttackIncreaseRate;
            int duration = matchTier == BoardMatchTier.Four
                ? rules.Match4BuffDurationTurns
                : rules.Match5BuffDurationTurns;
            long effectActionId = actionIds.Next();
            var effect = new EffectInstance(
                effectActionId,
                effectId,
                source.CharacterId,
                EffectCategory.Buff,
                EffectModifierType.AttackIncrease,
                magnitude,
                duration,
                effectActionId,
                EffectStackPolicy.RefreshDuration);
            return new CombatAction[]
            {
                heal,
                new ApplyEffectAction(
                    effectActionId,
                    ActionOrigin.Match,
                    party,
                    effect)
            };
        }

        private double ResolveHealingCoefficient(BoardMatchTier tier)
        {
            switch (tier)
            {
                case BoardMatchTier.Three: return rules.Match3HealingCoefficient;
                case BoardMatchTier.Four: return rules.Match4HealingCoefficient;
                case BoardMatchTier.FiveOrMore: return rules.Match5HealingCoefficient;
                default: throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        private static void ValidateInputs(
            CharacterBattleState source,
            PartyBattleState party,
            BoardMatchTier tier,
            int combo,
            CombatActionIdSequence actionIds)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!string.Equals(source.CharacterId, FioraAmberwingRules.CharacterId, StringComparison.Ordinal)
                || source.Element != ElementType.Fire)
                throw new ArgumentException("Source must be Fire Fiora Amberwing.", nameof(source));
            if (party == null) throw new ArgumentNullException(nameof(party));
            if (!Enum.IsDefined(typeof(BoardMatchTier), tier)) throw new ArgumentOutOfRangeException(nameof(tier));
            if (combo <= 0) throw new ArgumentOutOfRangeException(nameof(combo));
            if (actionIds == null) throw new ArgumentNullException(nameof(actionIds));
        }
    }
}
