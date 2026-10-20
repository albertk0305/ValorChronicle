using System;
using ValorChronicle.Characters.Build;

namespace ValorChronicle.Characters.Serena
{
    public sealed class SerenaCoralspearResolvedCombatRules
    {
        internal SerenaCoralspearResolvedCombatRules(
            double match3Coefficient,
            double match4BaseCoefficient,
            double match4AdditionalCoefficient,
            double match5Coefficient,
            double passiveChaseCoefficientPerWaterElement,
            double additionalAndChaseDamageIncreaseRate,
            double empoweredChaseDamageIncreaseRate,
            int empoweredChaseMaxCharges,
            double activeAdditionalAndChaseDamageIncreaseRate,
            int activeCooldownTurns,
            int activeDurationTurns,
            int match3WaterElementGainAmount,
            int match5WaterElementGainAmount,
            int waterElementMaxAmount)
        {
            Match3Coefficient = match3Coefficient;
            Match4BaseCoefficient = match4BaseCoefficient;
            Match4AdditionalCoefficient = match4AdditionalCoefficient;
            Match5Coefficient = match5Coefficient;
            PassiveChaseCoefficientPerWaterElement =
                passiveChaseCoefficientPerWaterElement;
            AdditionalAndChaseDamageIncreaseRate =
                additionalAndChaseDamageIncreaseRate;
            EmpoweredChaseDamageIncreaseRate =
                empoweredChaseDamageIncreaseRate;
            EmpoweredChaseMaxCharges = empoweredChaseMaxCharges;
            ActiveAdditionalAndChaseDamageIncreaseRate =
                activeAdditionalAndChaseDamageIncreaseRate;
            ActiveCooldownTurns = activeCooldownTurns;
            ActiveDurationTurns = activeDurationTurns;
            Match3WaterElementGainAmount =
                match3WaterElementGainAmount;
            Match5WaterElementGainAmount =
                match5WaterElementGainAmount;
            WaterElementMaxAmount = waterElementMaxAmount;
        }

        public double Match3Coefficient { get; }
        public double Match4BaseCoefficient { get; }
        public double Match4AdditionalCoefficient { get; }
        public double Match5Coefficient { get; }
        public double PassiveChaseCoefficientPerWaterElement { get; }
        public double AdditionalAndChaseDamageIncreaseRate { get; }
        public double EmpoweredChaseDamageIncreaseRate { get; }
        public int EmpoweredChaseMaxCharges { get; }
        public double ActiveAdditionalAndChaseDamageIncreaseRate { get; }
        public int ActiveCooldownTurns { get; }
        public int ActiveDurationTurns { get; }
        public int Match3WaterElementGainAmount { get; }
        public int Match5WaterElementGainAmount { get; }
        public int WaterElementMaxAmount { get; }
    }

    public static class SerenaCoralspearCombatRuleResolver
    {
        private const int AdditionalEnhancementAwakening = 2;
        private const int PassiveEnhancementAwakening = 3;
        private const int MatchEnhancementAwakening = 4;
        private const int ActiveEnhancementAwakening = 5;
        private const int Match5WaterGainAwakening = 6;

        public static SerenaCoralspearResolvedCombatRules Resolve(
            SerenaCoralspearCombatConfig config,
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

            return new SerenaCoralspearResolvedCombatRules(
                awakening >= MatchEnhancementAwakening
                    ? config.Awakening4Match3Coefficient
                    : config.Match3Coefficient,
                awakening >= MatchEnhancementAwakening
                    ? config.Awakening4Match4BaseCoefficient
                    : config.Match4BaseCoefficient,
                awakening >= MatchEnhancementAwakening
                    ? config.Awakening4Match4AdditionalCoefficient
                    : config.Match4AdditionalCoefficient,
                config.Match5Coefficient,
                awakening >= PassiveEnhancementAwakening
                    ? config.Awakening3PassiveChaseCoefficientPerWaterElement
                    : config.PassiveChaseCoefficientPerWaterElement,
                awakening >= AdditionalEnhancementAwakening
                    ? config.Awakening2AdditionalAndChaseDamageIncreaseRate
                    : 0d,
                config.EmpoweredChaseDamageIncreaseRate,
                config.EmpoweredChaseMaxCharges,
                awakening >= ActiveEnhancementAwakening
                    ? config.Awakening5ActiveAdditionalAndChaseDamageIncreaseRate
                    : config.ActiveAdditionalAndChaseDamageIncreaseRate,
                config.ActiveCooldownTurns,
                config.ActiveDurationTurns,
                config.Match3WaterElementGainAmount,
                awakening >= Match5WaterGainAwakening
                    ? config.Awakening6Match5WaterElementGainAmount
                    : 0,
                config.WaterElementMaxAmount);
        }
    }
}
