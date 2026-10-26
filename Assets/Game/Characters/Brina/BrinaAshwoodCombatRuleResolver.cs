using System;
using ValorChronicle.Characters.Build;

namespace ValorChronicle.Characters.Brina
{
    public sealed class BrinaAshwoodResolvedCombatRules
    {
        internal BrinaAshwoodResolvedCombatRules(
            double match3Coefficient,
            double match4Coefficient,
            double match5Coefficient,
            int match3WoodcutGainAmount,
            int match4WoodcutGainAmount,
            int match5WoodcutGainAmount,
            int woodcutTriggerThreshold,
            double passiveAdditionalCoefficient,
            double activeWoodcutGainMultiplier,
            int activeCooldownTurns,
            int activeDurationTurns,
            int firstWoodcutBonusAmount)
        {
            Match3Coefficient = match3Coefficient;
            Match4Coefficient = match4Coefficient;
            Match5Coefficient = match5Coefficient;
            Match3WoodcutGainAmount = match3WoodcutGainAmount;
            Match4WoodcutGainAmount = match4WoodcutGainAmount;
            Match5WoodcutGainAmount = match5WoodcutGainAmount;
            WoodcutTriggerThreshold = woodcutTriggerThreshold;
            PassiveAdditionalCoefficient = passiveAdditionalCoefficient;
            ActiveWoodcutGainMultiplier = activeWoodcutGainMultiplier;
            ActiveCooldownTurns = activeCooldownTurns;
            ActiveDurationTurns = activeDurationTurns;
            FirstWoodcutBonusAmount = firstWoodcutBonusAmount;
        }

        public double Match3Coefficient { get; }
        public double Match4Coefficient { get; }
        public double Match5Coefficient { get; }
        public int Match3WoodcutGainAmount { get; }
        public int Match4WoodcutGainAmount { get; }
        public int Match5WoodcutGainAmount { get; }
        public int WoodcutTriggerThreshold { get; }
        public double PassiveAdditionalCoefficient { get; }
        public double ActiveWoodcutGainMultiplier { get; }
        public int ActiveCooldownTurns { get; }
        public int ActiveDurationTurns { get; }
        public int FirstWoodcutBonusAmount { get; }
    }

    public static class BrinaAshwoodCombatRuleResolver
    {
        private const int PassiveEnhancementAwakening = 3;
        private const int MatchEnhancementAwakening = 4;
        private const int CooldownEnhancementAwakening = 5;
        private const int FirstWoodcutBonusAwakening = 6;

        public static BrinaAshwoodResolvedCombatRules Resolve(
            BrinaAshwoodCombatConfig config,
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

            return new BrinaAshwoodResolvedCombatRules(
                awakening >= MatchEnhancementAwakening
                    ? config.Awakening4Match3Coefficient
                    : config.Match3Coefficient,
                awakening >= MatchEnhancementAwakening
                    ? config.Awakening4Match4Coefficient
                    : config.Match4Coefficient,
                config.Match5Coefficient,
                config.Match3WoodcutGainAmount,
                config.Match4WoodcutGainAmount,
                config.Match5WoodcutGainAmount,
                config.WoodcutTriggerThreshold,
                awakening >= PassiveEnhancementAwakening
                    ? config.Awakening3PassiveAdditionalCoefficient
                    : config.PassiveAdditionalCoefficient,
                config.ActiveWoodcutGainMultiplier,
                awakening >= CooldownEnhancementAwakening
                    ? config.Awakening5ActiveCooldownTurns
                    : config.ActiveCooldownTurns,
                config.ActiveDurationTurns,
                awakening >= FirstWoodcutBonusAwakening
                    ? config.Awakening6FirstWoodcutBonusAmount
                    : 0);
        }
    }
}
