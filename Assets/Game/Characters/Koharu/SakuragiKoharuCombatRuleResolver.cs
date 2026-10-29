using System;
using ValorChronicle.Characters.Build;

namespace ValorChronicle.Characters.Koharu
{
    public sealed class SakuragiKoharuResolvedCombatRules
    {
        internal SakuragiKoharuResolvedCombatRules(
            double match3Coefficient,
            double match4Coefficient,
            double match5Coefficient,
            int match3SpiritEnergyGain,
            int match4SpiritEnergyGain,
            int match5SpiritEnergyGain,
            double match5AdditionalAndChaseDamageIncreaseRate,
            int match5EffectDurationTurns,
            int spiritEnergyCostPerCooldownReduction,
            int activeCooldownTurns,
            int activeBlockConversionCount,
            bool hasAwakening6AdditionalAndChaseDamageIncrease,
            double awakening6AdditionalAndChaseDamageIncreaseRate,
            int awakening6EffectDurationTurns)
        {
            Match3Coefficient = match3Coefficient;
            Match4Coefficient = match4Coefficient;
            Match5Coefficient = match5Coefficient;
            Match3SpiritEnergyGain = match3SpiritEnergyGain;
            Match4SpiritEnergyGain = match4SpiritEnergyGain;
            Match5SpiritEnergyGain = match5SpiritEnergyGain;
            Match5AdditionalAndChaseDamageIncreaseRate =
                match5AdditionalAndChaseDamageIncreaseRate;
            Match5EffectDurationTurns = match5EffectDurationTurns;
            SpiritEnergyCostPerCooldownReduction =
                spiritEnergyCostPerCooldownReduction;
            ActiveCooldownTurns = activeCooldownTurns;
            ActiveBlockConversionCount = activeBlockConversionCount;
            HasAwakening6AdditionalAndChaseDamageIncrease =
                hasAwakening6AdditionalAndChaseDamageIncrease;
            Awakening6AdditionalAndChaseDamageIncreaseRate =
                awakening6AdditionalAndChaseDamageIncreaseRate;
            Awakening6EffectDurationTurns = awakening6EffectDurationTurns;
        }

        public double Match3Coefficient { get; }
        public double Match4Coefficient { get; }
        public double Match5Coefficient { get; }
        public int Match3SpiritEnergyGain { get; }
        public int Match4SpiritEnergyGain { get; }
        public int Match5SpiritEnergyGain { get; }
        public double Match5AdditionalAndChaseDamageIncreaseRate { get; }
        public int Match5EffectDurationTurns { get; }
        public int SpiritEnergyCostPerCooldownReduction { get; }
        public int ActiveCooldownTurns { get; }
        public int ActiveBlockConversionCount { get; }
        public bool HasAwakening6AdditionalAndChaseDamageIncrease { get; }
        public double Awakening6AdditionalAndChaseDamageIncreaseRate { get; }
        public int Awakening6EffectDurationTurns { get; }
    }

    public static class SakuragiKoharuCombatRuleResolver
    {
        private const int Match34EnhancementAwakening = 3;
        private const int Match5EnhancementAwakening = 4;
        private const int SpiritCostEnhancementAwakening = 5;
        private const int ActiveBuffAwakening = 6;

        public static SakuragiKoharuResolvedCombatRules Resolve(
            SakuragiKoharuCombatConfig config,
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

            return new SakuragiKoharuResolvedCombatRules(
                awakening >= Match34EnhancementAwakening
                    ? config.Awakening3Match3Coefficient
                    : config.Match3Coefficient,
                awakening >= Match34EnhancementAwakening
                    ? config.Awakening3Match4Coefficient
                    : config.Match4Coefficient,
                awakening >= Match5EnhancementAwakening
                    ? config.Awakening4Match5Coefficient
                    : config.Match5Coefficient,
                config.Match3SpiritEnergyGain,
                config.Match4SpiritEnergyGain,
                config.Match5SpiritEnergyGain,
                awakening >= Match5EnhancementAwakening
                    ? config.Awakening4Match5AdditionalAndChaseDamageIncreaseRate
                    : config.Match5AdditionalAndChaseDamageIncreaseRate,
                config.Match5EffectDurationTurns,
                awakening >= SpiritCostEnhancementAwakening
                    ? config.Awakening5SpiritEnergyCostPerCooldownReduction
                    : config.SpiritEnergyCostPerCooldownReduction,
                config.ActiveCooldownTurns,
                config.ActiveBlockConversionCount,
                awakening >= ActiveBuffAwakening,
                config.Awakening6AdditionalAndChaseDamageIncreaseRate,
                config.Awakening6EffectDurationTurns);
        }
    }
}
