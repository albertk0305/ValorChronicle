using System;

namespace ValorChronicle.Characters.Miyuki
{
    public sealed class ShimotsukiMiyukiResolvedCombatRules
    {
        internal ShimotsukiMiyukiResolvedCombatRules(
            double match3Coefficient,
            double match4Coefficient,
            double match5Coefficient,
            int match3WaterAmount,
            int match4WaterAmount,
            int match5WaterAmount,
            double passiveWaterDamageIncreaseRate,
            double match5WaterDamageIncreaseRate,
            int match5EffectDurationTurns,
            int activeCooldownTurns,
            int activeBlockConversionCount,
            bool hasFrostEcho,
            int frostEchoExtraWaterAmount)
        {
            Match3Coefficient = match3Coefficient;
            Match4Coefficient = match4Coefficient;
            Match5Coefficient = match5Coefficient;
            Match3WaterAmount = match3WaterAmount;
            Match4WaterAmount = match4WaterAmount;
            Match5WaterAmount = match5WaterAmount;
            PassiveWaterDamageIncreaseRate = passiveWaterDamageIncreaseRate;
            Match5WaterDamageIncreaseRate = match5WaterDamageIncreaseRate;
            Match5EffectDurationTurns = match5EffectDurationTurns;
            ActiveCooldownTurns = activeCooldownTurns;
            ActiveBlockConversionCount = activeBlockConversionCount;
            HasFrostEcho = hasFrostEcho;
            FrostEchoExtraWaterAmount = frostEchoExtraWaterAmount;
        }

        public double Match3Coefficient { get; }
        public double Match4Coefficient { get; }
        public double Match5Coefficient { get; }
        public int Match3WaterAmount { get; }
        public int Match4WaterAmount { get; }
        public int Match5WaterAmount { get; }
        public double PassiveWaterDamageIncreaseRate { get; }
        public double Match5WaterDamageIncreaseRate { get; }
        public int Match5EffectDurationTurns { get; }
        public int ActiveCooldownTurns { get; }
        public int ActiveBlockConversionCount { get; }
        public bool HasFrostEcho { get; }
        public int FrostEchoExtraWaterAmount { get; }
    }

    public static class ShimotsukiMiyukiCombatRuleResolver
    {
        private const int PassiveEnhancementAwakening = 3;
        private const int MatchEnhancementAwakening = 4;
        private const int ActiveEnhancementAwakening = 5;
        private const int FrostEchoAwakening = 6;

        public static ShimotsukiMiyukiResolvedCombatRules Resolve(
            ShimotsukiMiyukiCombatConfig config,
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

            if (awakening < 0 || awakening > 6)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(awakening), awakening,
                    "Awakening must be between 0 and 6.");
            }

            return new ShimotsukiMiyukiResolvedCombatRules(
                awakening >= MatchEnhancementAwakening
                    ? config.EnhancedMatch3Coefficient
                    : config.Match3Coefficient,
                awakening >= MatchEnhancementAwakening
                    ? config.EnhancedMatch4Coefficient
                    : config.Match4Coefficient,
                config.Match5Coefficient,
                config.Match3WaterAmount,
                config.Match4WaterAmount,
                config.Match5WaterAmount,
                awakening >= PassiveEnhancementAwakening
                    ? config.EnhancedPassiveWaterDamageIncreaseRate
                    : config.PassiveWaterDamageIncreaseRate,
                config.Match5WaterDamageIncreaseRate,
                config.Match5EffectDurationTurns,
                config.ActiveCooldownTurns,
                awakening >= ActiveEnhancementAwakening
                    ? config.EnhancedActiveBlockConversionCount
                    : config.ActiveBlockConversionCount,
                awakening >= FrostEchoAwakening,
                config.Awakening6ExtraWaterAmount);
        }
    }
}
