using System;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Characters.Build;

namespace ValorChronicle.Characters.BaekHyeon
{
    public sealed class BaekHyeonResolvedCombatRules
    {
        internal BaekHyeonResolvedCombatRules(
            BaekHyeonCombatConfig config,
            int awakening)
        {
            InitialMarkCount = config.InitialMarkCount;
            MarkMaxStackCount = config.MarkMaxStackCount;
            MarkRemainingTurns = config.MarkRemainingTurns;
            MarkStackPolicy = config.MarkStackPolicy;
            Match3MarkGainAmount = config.Match3MarkGainAmount;
            Match4MarkGainAmount = config.Match4MarkGainAmount;
            Match5MarkGainAmount = config.Match5MarkGainAmount;
            MarkDamageIncreaseRatePerStack = awakening >= 3
                ? config.Awakening3MarkDamageIncreaseRate
                : config.MarkDamageIncreaseRate;
            MarkDamageAttackTypes = config.MarkDamageAttackTypes;
            MarkConsumptionAttackTypes =
                config.MarkConsumptionAttackTypes;
            MinimumActualDamageToConsumeMarks =
                config.MinimumActualDamageToConsumeMarks;
            ConsumeAllMarksOnQualifyingDamage =
                config.ConsumeAllMarksOnQualifyingDamage;
            ConsumeMarksAfterAttackCompletion =
                config.ConsumeMarksAfterAttackCompletion;
            ApplyMatchMarksAfterDamage = config.ApplyMatchMarksAfterDamage;

            Match3BaseCoefficient = awakening >= 4
                ? config.Awakening4Match3BaseCoefficient
                : config.Match3BaseCoefficient;
            Match4BaseCoefficient = awakening >= 4
                ? config.Awakening4Match4BaseCoefficient
                : config.Match4BaseCoefficient;
            Match5BaseCoefficient = config.Match5BaseCoefficient;
            Match3MarkCoefficient = config.Match3MarkCoefficient;
            Match4MarkCoefficient = config.Match4MarkCoefficient;
            Match5MarkCoefficient = config.Match5MarkCoefficient;
            MatchAttackType = config.MatchAttackType;
            MatchUsesCombo = config.MatchUsesCombo;

            ActiveBaseCoefficient = awakening >= 5
                ? config.Awakening5ActiveBaseCoefficient
                : config.ActiveBaseCoefficient;
            ActiveMarkCoefficient = awakening >= 5
                ? config.Awakening5ActiveMarkCoefficient
                : config.ActiveMarkCoefficient;
            ActiveCooldownTurns = config.ActiveCooldownTurns;
            ActiveAttackType = config.ActiveAttackType;
            ActiveAvailableAtBattleStart =
                config.ActiveAvailableAtBattleStart;
            ActiveUsesCombo = config.ActiveUsesCombo;

            ExposureEnabled = awakening >= 6;
            ExposureDamageIncreaseRate = ExposureEnabled
                ? config.ExposureDamageIncreaseRate
                : 0d;
            ExposureDurationTurns = ExposureEnabled
                ? config.ExposureDurationTurns
                : 0;
            ExposureMaxStackCount = ExposureEnabled
                ? config.ExposureMaxStackCount
                : 0;
            ExposureStackPolicy = config.ExposureStackPolicy;
            ExposureAttackTypeMask = config.ExposureAttackTypeMask;
            ExposureMinimumMatchSize = config.ExposureMinimumMatchSize;
            ApplyExposureAfterMarkGain =
                config.ApplyExposureAfterMarkGain;
        }

        public int InitialMarkCount { get; }
        public int MarkMaxStackCount { get; }
        public int? MarkRemainingTurns { get; }
        public EffectStackPolicy MarkStackPolicy { get; }
        public int Match3MarkGainAmount { get; }
        public int Match4MarkGainAmount { get; }
        public int Match5MarkGainAmount { get; }
        public double MarkDamageIncreaseRatePerStack { get; }
        public AttackTypeMask MarkDamageAttackTypes { get; }
        public AttackTypeMask MarkConsumptionAttackTypes { get; }
        public int MinimumActualDamageToConsumeMarks { get; }
        public bool ConsumeAllMarksOnQualifyingDamage { get; }
        public bool ConsumeMarksAfterAttackCompletion { get; }
        public bool ApplyMatchMarksAfterDamage { get; }
        public double Match3BaseCoefficient { get; }
        public double Match4BaseCoefficient { get; }
        public double Match5BaseCoefficient { get; }
        public double Match3MarkCoefficient { get; }
        public double Match4MarkCoefficient { get; }
        public double Match5MarkCoefficient { get; }
        public AttackType MatchAttackType { get; }
        public bool MatchUsesCombo { get; }
        public double ActiveBaseCoefficient { get; }
        public double ActiveMarkCoefficient { get; }
        public int ActiveCooldownTurns { get; }
        public AttackType ActiveAttackType { get; }
        public bool ActiveAvailableAtBattleStart { get; }
        public bool ActiveUsesCombo { get; }
        public bool ExposureEnabled { get; }
        public double ExposureDamageIncreaseRate { get; }
        public int ExposureDurationTurns { get; }
        public int ExposureMaxStackCount { get; }
        public EffectStackPolicy ExposureStackPolicy { get; }
        public AttackTypeMask ExposureAttackTypeMask { get; }
        public int ExposureMinimumMatchSize { get; }
        public bool ApplyExposureAfterMarkGain { get; }

        public double GetMatchBaseCoefficient(int matchSize)
        {
            ValidateMatchSize(matchSize);
            if (matchSize == 3)
            {
                return Match3BaseCoefficient;
            }

            return matchSize == 4
                ? Match4BaseCoefficient
                : Match5BaseCoefficient;
        }

        public double GetMatchMarkCoefficient(int matchSize)
        {
            ValidateMatchSize(matchSize);
            if (matchSize == 3)
            {
                return Match3MarkCoefficient;
            }

            return matchSize == 4
                ? Match4MarkCoefficient
                : Match5MarkCoefficient;
        }

        public int GetMarkGainAmount(int matchSize)
        {
            ValidateMatchSize(matchSize);
            if (matchSize == 3)
            {
                return Match3MarkGainAmount;
            }

            return matchSize == 4
                ? Match4MarkGainAmount
                : Match5MarkGainAmount;
        }

        public double CalculateMatchCoefficient(
            int matchSize,
            int markCount)
        {
            ValidateMarkCount(markCount);
            return GetMatchBaseCoefficient(matchSize)
                + (GetMatchMarkCoefficient(matchSize) * markCount);
        }

        public double CalculateActiveCoefficient(int markCount)
        {
            ValidateMarkCount(markCount);
            return ActiveBaseCoefficient
                + (ActiveMarkCoefficient * markCount);
        }

        public double CalculateMarkDamageIncreaseRate(int markCount)
        {
            ValidateMarkCount(markCount);
            return MarkDamageIncreaseRatePerStack * markCount;
        }

        public bool CanApplyExposure(int matchSize)
        {
            ValidateMatchSize(matchSize);
            return ExposureEnabled
                && matchSize >= ExposureMinimumMatchSize;
        }

        private void ValidateMarkCount(int markCount)
        {
            if (markCount < InitialMarkCount
                || markCount > MarkMaxStackCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(markCount),
                    markCount,
                    $"Mark count must be between {InitialMarkCount} and "
                        + $"{MarkMaxStackCount}.");
            }
        }

        private static void ValidateMatchSize(int matchSize)
        {
            if (matchSize < 3)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(matchSize),
                    matchSize,
                    "Match size must be at least three.");
            }
        }
    }

    public static class BaekHyeonCombatRuleResolver
    {
        public static BaekHyeonResolvedCombatRules Resolve(
            BaekHyeonCombatConfig config,
            int awakening)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(config));
            }

            if (awakening < CharacterBuildResolver.MinimumAwakening
                || awakening > CharacterBuildResolver.MaximumAwakening)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(awakening),
                    awakening,
                    $"Awakening must be between "
                        + $"{CharacterBuildResolver.MinimumAwakening} and "
                        + $"{CharacterBuildResolver.MaximumAwakening}.");
            }

            return new BaekHyeonResolvedCombatRules(config, awakening);
        }
    }
}
