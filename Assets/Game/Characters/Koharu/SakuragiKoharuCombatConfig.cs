using System;
using UnityEngine;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Koharu
{
    [CreateAssetMenu(
        fileName = "SakuragiKoharuCombatConfig",
        menuName = "Valor Chronicle/Combat Configs/Sakuragi Koharu")]
    public sealed class SakuragiKoharuCombatConfig
        : CombatConfigDefinition
    {
        [SerializeField] private double match3Coefficient;
        [SerializeField] private double match4Coefficient;
        [SerializeField] private double match5Coefficient;
        [SerializeField] private int match3SpiritEnergyGain;
        [SerializeField] private int match4SpiritEnergyGain;
        [SerializeField] private int match5SpiritEnergyGain;
        [SerializeField] private double match5AdditionalAndChaseDamageIncreaseRate;
        [SerializeField] private int match5EffectDurationTurns;
        [SerializeField] private double awakening3Match3Coefficient;
        [SerializeField] private double awakening3Match4Coefficient;
        [SerializeField] private double awakening4Match5Coefficient;
        [SerializeField] private double awakening4Match5AdditionalAndChaseDamageIncreaseRate;
        [SerializeField] private int spiritEnergyCostPerCooldownReduction;
        [SerializeField] private int awakening5SpiritEnergyCostPerCooldownReduction;
        [SerializeField] private double awakening6AdditionalAndChaseDamageIncreaseRate;
        [SerializeField] private int awakening6EffectDurationTurns;
        [SerializeField] private int activeCooldownTurns;
        [SerializeField] private int activeBlockConversionCount;

        public double Match3Coefficient => match3Coefficient;
        public double Match4Coefficient => match4Coefficient;
        public double Match5Coefficient => match5Coefficient;
        public int Match3SpiritEnergyGain => match3SpiritEnergyGain;
        public int Match4SpiritEnergyGain => match4SpiritEnergyGain;
        public int Match5SpiritEnergyGain => match5SpiritEnergyGain;
        public double Match5AdditionalAndChaseDamageIncreaseRate =>
            match5AdditionalAndChaseDamageIncreaseRate;
        public int Match5EffectDurationTurns => match5EffectDurationTurns;
        public double Awakening3Match3Coefficient =>
            awakening3Match3Coefficient;
        public double Awakening3Match4Coefficient =>
            awakening3Match4Coefficient;
        public double Awakening4Match5Coefficient =>
            awakening4Match5Coefficient;
        public double Awakening4Match5AdditionalAndChaseDamageIncreaseRate =>
            awakening4Match5AdditionalAndChaseDamageIncreaseRate;
        public int SpiritEnergyCostPerCooldownReduction =>
            spiritEnergyCostPerCooldownReduction;
        public int Awakening5SpiritEnergyCostPerCooldownReduction =>
            awakening5SpiritEnergyCostPerCooldownReduction;
        public double Awakening6AdditionalAndChaseDamageIncreaseRate =>
            awakening6AdditionalAndChaseDamageIncreaseRate;
        public int Awakening6EffectDurationTurns =>
            awakening6EffectDurationTurns;
        public int ActiveCooldownTurns => activeCooldownTurns;
        public int ActiveBlockConversionCount => activeBlockConversionCount;

        public override bool TryValidate(out string errorMessage)
        {
            if (!ValidateRate(match3Coefficient, "Match3Coefficient", out errorMessage)
                || !ValidateRate(match4Coefficient, "Match4Coefficient", out errorMessage)
                || !ValidateRate(match5Coefficient, "Match5Coefficient", out errorMessage)
                || !ValidatePositive(match3SpiritEnergyGain, "Match3SpiritEnergyGain", out errorMessage)
                || !ValidatePositive(match4SpiritEnergyGain, "Match4SpiritEnergyGain", out errorMessage)
                || !ValidatePositive(match5SpiritEnergyGain, "Match5SpiritEnergyGain", out errorMessage)
                || !ValidateRate(match5AdditionalAndChaseDamageIncreaseRate, "Match5AdditionalAndChaseDamageIncreaseRate", out errorMessage)
                || !ValidatePositive(match5EffectDurationTurns, "Match5EffectDurationTurns", out errorMessage)
                || !ValidateRate(awakening3Match3Coefficient, "Awakening3Match3Coefficient", out errorMessage)
                || !ValidateRate(awakening3Match4Coefficient, "Awakening3Match4Coefficient", out errorMessage)
                || !ValidateRate(awakening4Match5Coefficient, "Awakening4Match5Coefficient", out errorMessage)
                || !ValidateRate(awakening4Match5AdditionalAndChaseDamageIncreaseRate, "Awakening4Match5AdditionalAndChaseDamageIncreaseRate", out errorMessage)
                || !ValidatePositive(spiritEnergyCostPerCooldownReduction, "SpiritEnergyCostPerCooldownReduction", out errorMessage)
                || !ValidatePositive(awakening5SpiritEnergyCostPerCooldownReduction, "Awakening5SpiritEnergyCostPerCooldownReduction", out errorMessage)
                || !ValidateRate(awakening6AdditionalAndChaseDamageIncreaseRate, "Awakening6AdditionalAndChaseDamageIncreaseRate", out errorMessage)
                || !ValidatePositive(awakening6EffectDurationTurns, "Awakening6EffectDurationTurns", out errorMessage)
                || !ValidatePositive(activeCooldownTurns, "ActiveCooldownTurns", out errorMessage)
                || !ValidatePositive(activeBlockConversionCount, "ActiveBlockConversionCount", out errorMessage))
            {
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool ValidateRate(
            double value,
            string fieldName,
            out string errorMessage)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                errorMessage = $"{fieldName} must be finite and non-negative.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool ValidatePositive(
            int value,
            string fieldName,
            out string errorMessage)
        {
            if (value <= 0)
            {
                errorMessage = $"{fieldName} must be greater than zero.";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}
