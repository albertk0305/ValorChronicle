using System;

namespace ValorChronicle.Characters.Marea
{
    public sealed class MareaBluefangResolvedCombatRules
    {
        internal MareaBluefangResolvedCombatRules(
            double match3Coefficient,
            double match4BaseCoefficient,
            double match4WaterElementBonusCoefficient,
            double match5BaseCoefficient,
            double match5CoefficientPerWaterElement,
            double passiveDealtDamageIncreaseRate,
            bool hasLingeringSurge,
            double lingeringSurgeDamageIncreaseRate)
        {
            Match3Coefficient = match3Coefficient;
            Match4BaseCoefficient = match4BaseCoefficient;
            Match4WaterElementBonusCoefficient =
                match4WaterElementBonusCoefficient;
            Match5BaseCoefficient = match5BaseCoefficient;
            Match5CoefficientPerWaterElement =
                match5CoefficientPerWaterElement;
            PassiveDealtDamageIncreaseRate =
                passiveDealtDamageIncreaseRate;
            HasLingeringSurge = hasLingeringSurge;
            LingeringSurgeDamageIncreaseRate =
                lingeringSurgeDamageIncreaseRate;
        }

        public double Match3Coefficient { get; }
        public double Match4BaseCoefficient { get; }
        public double Match4WaterElementBonusCoefficient { get; }
        public double Match5BaseCoefficient { get; }
        public double Match5CoefficientPerWaterElement { get; }
        public double PassiveDealtDamageIncreaseRate { get; }
        public bool HasLingeringSurge { get; }
        public double LingeringSurgeDamageIncreaseRate { get; }
    }

    public static class MareaBluefangCombatRuleResolver
    {
        public const int MinimumAwakening = 0;
        public const int MaximumAwakening = 6;
        public const int LingeringSurgeRequiredWaterAmount = 5;

        private const int PassiveEnhancementAwakening = 3;
        private const int MatchEnhancementAwakening = 4;
        private const int DevourEnhancementAwakening = 5;
        private const int LingeringSurgeAwakening = 6;

        private const double EnhancedPassiveRate = 0.18d;
        private const double EnhancedMatch3Coefficient = 1.00d;
        private const double EnhancedMatch4BaseCoefficient = 1.65d;
        private const double EnhancedMatch4WaterBonusCoefficient = 0.45d;
        private const double EnhancedDevourPerWaterCoefficient = 1.60d;
        private const double LingeringSurgeDamageIncreaseRate = 0.30d;

        public static MareaBluefangResolvedCombatRules Resolve(
            MareaBluefangCombatConfig config,
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

            if (awakening < MinimumAwakening
                || awakening > MaximumAwakening)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(awakening),
                    awakening,
                    $"Awakening must be between {MinimumAwakening} and "
                        + $"{MaximumAwakening}.");
            }

            return new MareaBluefangResolvedCombatRules(
                awakening >= MatchEnhancementAwakening
                    ? EnhancedMatch3Coefficient
                    : config.Match3Coefficient,
                awakening >= MatchEnhancementAwakening
                    ? EnhancedMatch4BaseCoefficient
                    : config.Match4BaseCoefficient,
                awakening >= MatchEnhancementAwakening
                    ? EnhancedMatch4WaterBonusCoefficient
                    : config.Match4WaterElementBonusCoefficient,
                config.Match5BaseCoefficient,
                awakening >= DevourEnhancementAwakening
                    ? EnhancedDevourPerWaterCoefficient
                    : config.Match5CoefficientPerWaterElement,
                awakening >= PassiveEnhancementAwakening
                    ? EnhancedPassiveRate
                    : config.PassiveDealtDamageIncreaseRate,
                awakening >= LingeringSurgeAwakening,
                LingeringSurgeDamageIncreaseRate);
        }
    }
}
