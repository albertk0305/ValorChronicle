using System;
using UnityEngine;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Marea
{
    [CreateAssetMenu(
        fileName = "MareaBluefangCombatConfig",
        menuName = "Valor Chronicle/Combat Configs/Marea Bluefang")]
    public sealed class MareaBluefangCombatConfig
        : CombatConfigDefinition
    {
        [SerializeField]
        private double match3Coefficient;

        [SerializeField]
        private double match4BaseCoefficient;

        [SerializeField]
        private double match4WaterElementBonusCoefficient;

        [SerializeField]
        private double match5BaseCoefficient;

        [SerializeField]
        private double match5CoefficientPerWaterElement;

        [SerializeField]
        private double passiveDealtDamageIncreaseRate;

        [SerializeField]
        private double activeWaterDamageIncreaseRate;

        [SerializeField]
        private int activeDurationTurns;

        [SerializeField]
        private int activeCooldownTurns;

        [SerializeField]
        private int waterElementMaxAmount;

        public double Match3Coefficient => match3Coefficient;
        public double Match4BaseCoefficient => match4BaseCoefficient;
        public double Match4WaterElementBonusCoefficient =>
            match4WaterElementBonusCoefficient;
        public double Match5BaseCoefficient => match5BaseCoefficient;
        public double Match5CoefficientPerWaterElement =>
            match5CoefficientPerWaterElement;
        public double PassiveDealtDamageIncreaseRate =>
            passiveDealtDamageIncreaseRate;
        public double ActiveWaterDamageIncreaseRate =>
            activeWaterDamageIncreaseRate;
        public int ActiveDurationTurns => activeDurationTurns;
        public int ActiveCooldownTurns => activeCooldownTurns;
        public int WaterElementMaxAmount => waterElementMaxAmount;

        public override bool TryValidate(out string errorMessage)
        {
            if (!IsFiniteNonNegative(match3Coefficient))
            {
                errorMessage = "Match3Coefficient must be finite and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(match4BaseCoefficient))
            {
                errorMessage = "Match4BaseCoefficient must be finite and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(match4WaterElementBonusCoefficient))
            {
                errorMessage = "Match4WaterElementBonusCoefficient must be finite and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(match5BaseCoefficient))
            {
                errorMessage = "Match5BaseCoefficient must be finite and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(match5CoefficientPerWaterElement))
            {
                errorMessage = "Match5CoefficientPerWaterElement must be finite and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(passiveDealtDamageIncreaseRate))
            {
                errorMessage = "PassiveDealtDamageIncreaseRate must be finite and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(activeWaterDamageIncreaseRate))
            {
                errorMessage = "ActiveWaterDamageIncreaseRate must be finite and non-negative.";
                return false;
            }

            if (activeDurationTurns <= 0)
            {
                errorMessage = "ActiveDurationTurns must be greater than zero.";
                return false;
            }

            if (activeCooldownTurns < 0)
            {
                errorMessage = "ActiveCooldownTurns cannot be negative.";
                return false;
            }

            if (waterElementMaxAmount <= 0)
            {
                errorMessage = "WaterElementMaxAmount must be greater than zero.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool IsFiniteNonNegative(double value)
        {
            return !double.IsNaN(value)
                && !double.IsInfinity(value)
                && value >= 0d;
        }
    }
}
