using System;
using UnityEngine;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Miyuki
{
    [CreateAssetMenu(
        fileName = "ShimotsukiMiyukiCombatConfig",
        menuName = "Valor Chronicle/Combat Configs/Shimotsuki Miyuki")]
    public sealed class ShimotsukiMiyukiCombatConfig
        : CombatConfigDefinition
    {
        [SerializeField] private double match3Coefficient;
        [SerializeField] private double match4Coefficient;
        [SerializeField] private double match5Coefficient;
        [SerializeField] private int match3WaterAmount;
        [SerializeField] private int match4WaterAmount;
        [SerializeField] private int match5WaterAmount;
        [SerializeField] private double passiveWaterDamageIncreaseRate;
        [SerializeField] private double enhancedPassiveWaterDamageIncreaseRate;
        [SerializeField] private double match5WaterDamageIncreaseRate;
        [SerializeField] private int match5EffectDurationTurns;
        [SerializeField] private double enhancedMatch3Coefficient;
        [SerializeField] private double enhancedMatch4Coefficient;
        [SerializeField] private int activeCooldownTurns;
        [SerializeField] private int activeBlockConversionCount;
        [SerializeField] private int enhancedActiveBlockConversionCount;
        [SerializeField] private int awakening6ExtraWaterAmount;
        [SerializeField] private int waterElementMaxAmount;

        public double Match3Coefficient => match3Coefficient;
        public double Match4Coefficient => match4Coefficient;
        public double Match5Coefficient => match5Coefficient;
        public int Match3WaterAmount => match3WaterAmount;
        public int Match4WaterAmount => match4WaterAmount;
        public int Match5WaterAmount => match5WaterAmount;
        public double PassiveWaterDamageIncreaseRate =>
            passiveWaterDamageIncreaseRate;
        public double EnhancedPassiveWaterDamageIncreaseRate =>
            enhancedPassiveWaterDamageIncreaseRate;
        public double Match5WaterDamageIncreaseRate =>
            match5WaterDamageIncreaseRate;
        public int Match5EffectDurationTurns => match5EffectDurationTurns;
        public double EnhancedMatch3Coefficient =>
            enhancedMatch3Coefficient;
        public double EnhancedMatch4Coefficient =>
            enhancedMatch4Coefficient;
        public int ActiveCooldownTurns => activeCooldownTurns;
        public int ActiveBlockConversionCount =>
            activeBlockConversionCount;
        public int EnhancedActiveBlockConversionCount =>
            enhancedActiveBlockConversionCount;
        public int Awakening6ExtraWaterAmount =>
            awakening6ExtraWaterAmount;
        public int WaterElementMaxAmount => waterElementMaxAmount;

        public override bool TryValidate(out string errorMessage)
        {
            if (!ValidateRate(match3Coefficient, "Match3Coefficient", out errorMessage)
                || !ValidateRate(match4Coefficient, "Match4Coefficient", out errorMessage)
                || !ValidateRate(match5Coefficient, "Match5Coefficient", out errorMessage)
                || !ValidatePositive(match3WaterAmount, "Match3WaterAmount", out errorMessage)
                || !ValidatePositive(match4WaterAmount, "Match4WaterAmount", out errorMessage)
                || !ValidatePositive(match5WaterAmount, "Match5WaterAmount", out errorMessage)
                || !ValidateRate(passiveWaterDamageIncreaseRate, "PassiveWaterDamageIncreaseRate", out errorMessage)
                || !ValidateRate(enhancedPassiveWaterDamageIncreaseRate, "EnhancedPassiveWaterDamageIncreaseRate", out errorMessage)
                || !ValidateRate(match5WaterDamageIncreaseRate, "Match5WaterDamageIncreaseRate", out errorMessage)
                || !ValidatePositive(match5EffectDurationTurns, "Match5EffectDurationTurns", out errorMessage)
                || !ValidateRate(enhancedMatch3Coefficient, "EnhancedMatch3Coefficient", out errorMessage)
                || !ValidateRate(enhancedMatch4Coefficient, "EnhancedMatch4Coefficient", out errorMessage)
                || !ValidateNonNegative(activeCooldownTurns, "ActiveCooldownTurns", out errorMessage)
                || !ValidatePositive(activeBlockConversionCount, "ActiveBlockConversionCount", out errorMessage)
                || !ValidatePositive(enhancedActiveBlockConversionCount, "EnhancedActiveBlockConversionCount", out errorMessage)
                || !ValidatePositive(awakening6ExtraWaterAmount, "Awakening6ExtraWaterAmount", out errorMessage))
            {
                return false;
            }

            if (waterElementMaxAmount != WaterElementResource.MaxAmount)
            {
                errorMessage = "WaterElementMaxAmount must match the shared water element maximum of "
                    + $"{WaterElementResource.MaxAmount}.";
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

        private static bool ValidateNonNegative(
            int value,
            string fieldName,
            out string errorMessage)
        {
            if (value < 0)
            {
                errorMessage = $"{fieldName} cannot be negative.";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}
