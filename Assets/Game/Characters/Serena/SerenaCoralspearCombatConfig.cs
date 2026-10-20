using UnityEngine;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Serena
{
    [CreateAssetMenu(
        fileName = "SerenaCoralspearCombatConfig",
        menuName = "Valor Chronicle/Combat Configs/Serena Coralspear")]
    public sealed class SerenaCoralspearCombatConfig
        : CombatConfigDefinition
    {
        [SerializeField]
        private double match3Coefficient = 0.75d;

        [SerializeField]
        private double match4BaseCoefficient = 1.35d;

        [SerializeField]
        private double match4AdditionalCoefficient = 0.90d;

        [SerializeField]
        private double match5Coefficient = 2.20d;

        [SerializeField]
        private double passiveChaseCoefficientPerWaterElement = 0.75d;

        [SerializeField]
        private double empoweredChaseDamageIncreaseRate = 0.80d;

        [SerializeField]
        private int empoweredChaseMaxCharges = 3;

        [SerializeField]
        private double activeAdditionalAndChaseDamageIncreaseRate = 0.35d;

        [SerializeField]
        private int activeCooldownTurns = 8;

        [SerializeField]
        private int activeDurationTurns = 3;

        [SerializeField]
        private double awakening2AdditionalAndChaseDamageIncreaseRate =
            0.10d;

        [SerializeField]
        private double awakening3PassiveChaseCoefficientPerWaterElement =
            0.85d;

        [SerializeField]
        private double awakening4Match3Coefficient = 0.85d;

        [SerializeField]
        private double awakening4Match4BaseCoefficient = 1.50d;

        [SerializeField]
        private double awakening4Match4AdditionalCoefficient = 1.00d;

        [SerializeField]
        private double awakening5ActiveAdditionalAndChaseDamageIncreaseRate =
            0.45d;

        [SerializeField]
        private int match3WaterElementGainAmount = 1;

        [SerializeField]
        private int awakening6Match5WaterElementGainAmount = 1;

        [SerializeField]
        private int waterElementMaxAmount = WaterElementResource.MaxAmount;

        public double Match3Coefficient => match3Coefficient;
        public double Match4BaseCoefficient => match4BaseCoefficient;
        public double Match4AdditionalCoefficient =>
            match4AdditionalCoefficient;
        public double Match5Coefficient => match5Coefficient;
        public double PassiveChaseCoefficientPerWaterElement =>
            passiveChaseCoefficientPerWaterElement;
        public double EmpoweredChaseDamageIncreaseRate =>
            empoweredChaseDamageIncreaseRate;
        public int EmpoweredChaseMaxCharges =>
            empoweredChaseMaxCharges;
        public double ActiveAdditionalAndChaseDamageIncreaseRate =>
            activeAdditionalAndChaseDamageIncreaseRate;
        public int ActiveCooldownTurns => activeCooldownTurns;
        public int ActiveDurationTurns => activeDurationTurns;
        public double Awakening2AdditionalAndChaseDamageIncreaseRate =>
            awakening2AdditionalAndChaseDamageIncreaseRate;
        public double Awakening3PassiveChaseCoefficientPerWaterElement =>
            awakening3PassiveChaseCoefficientPerWaterElement;
        public double Awakening4Match3Coefficient =>
            awakening4Match3Coefficient;
        public double Awakening4Match4BaseCoefficient =>
            awakening4Match4BaseCoefficient;
        public double Awakening4Match4AdditionalCoefficient =>
            awakening4Match4AdditionalCoefficient;
        public double Awakening5ActiveAdditionalAndChaseDamageIncreaseRate =>
            awakening5ActiveAdditionalAndChaseDamageIncreaseRate;
        public int Match3WaterElementGainAmount =>
            match3WaterElementGainAmount;
        public int Awakening6Match5WaterElementGainAmount =>
            awakening6Match5WaterElementGainAmount;
        public int WaterElementMaxAmount => waterElementMaxAmount;

        public override bool TryValidate(out string errorMessage)
        {
            if (!IsFiniteNonNegative(match3Coefficient))
            {
                errorMessage =
                    "Match3Coefficient must be finite and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(match4BaseCoefficient))
            {
                errorMessage = "Match4BaseCoefficient must be finite and "
                    + "non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(match4AdditionalCoefficient))
            {
                errorMessage = "Match4AdditionalCoefficient must be finite "
                    + "and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(match5Coefficient))
            {
                errorMessage =
                    "Match5Coefficient must be finite and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(
                    passiveChaseCoefficientPerWaterElement))
            {
                errorMessage = "PassiveChaseCoefficientPerWaterElement must "
                    + "be finite and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(
                    empoweredChaseDamageIncreaseRate))
            {
                errorMessage = "EmpoweredChaseDamageIncreaseRate must be "
                    + "finite and non-negative.";
                return false;
            }

            if (empoweredChaseMaxCharges <= 0)
            {
                errorMessage =
                    "EmpoweredChaseMaxCharges must be greater than zero.";
                return false;
            }

            if (!IsFiniteNonNegative(
                    activeAdditionalAndChaseDamageIncreaseRate))
            {
                errorMessage = "ActiveAdditionalAndChaseDamageIncreaseRate "
                    + "must be finite and non-negative.";
                return false;
            }

            if (activeCooldownTurns < 0)
            {
                errorMessage =
                    "ActiveCooldownTurns cannot be negative.";
                return false;
            }

            if (activeDurationTurns <= 0)
            {
                errorMessage =
                    "ActiveDurationTurns must be greater than zero.";
                return false;
            }

            if (!IsFiniteNonNegative(
                    awakening2AdditionalAndChaseDamageIncreaseRate))
            {
                errorMessage = "Awakening2AdditionalAndChaseDamageIncreaseRate "
                    + "must be finite and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(
                    awakening3PassiveChaseCoefficientPerWaterElement))
            {
                errorMessage = "Awakening3PassiveChaseCoefficientPerWaterElement "
                    + "must be finite and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(awakening4Match3Coefficient))
            {
                errorMessage = "Awakening4Match3Coefficient must be finite "
                    + "and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(awakening4Match4BaseCoefficient))
            {
                errorMessage = "Awakening4Match4BaseCoefficient must be "
                    + "finite and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(
                    awakening4Match4AdditionalCoefficient))
            {
                errorMessage = "Awakening4Match4AdditionalCoefficient must "
                    + "be finite and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(
                    awakening5ActiveAdditionalAndChaseDamageIncreaseRate))
            {
                errorMessage = "Awakening5ActiveAdditionalAndChaseDamageIncreaseRate "
                    + "must be finite and non-negative.";
                return false;
            }

            if (match3WaterElementGainAmount <= 0
                || match3WaterElementGainAmount
                    > WaterElementResource.MaxAmount)
            {
                errorMessage = "Match3WaterElementGainAmount must be between "
                    + $"one and {WaterElementResource.MaxAmount}.";
                return false;
            }

            if (awakening6Match5WaterElementGainAmount <= 0
                || awakening6Match5WaterElementGainAmount
                    > WaterElementResource.MaxAmount)
            {
                errorMessage = "Awakening6Match5WaterElementGainAmount must "
                    + $"be between one and {WaterElementResource.MaxAmount}.";
                return false;
            }

            if (waterElementMaxAmount != WaterElementResource.MaxAmount)
            {
                errorMessage = "WaterElementMaxAmount must match the shared "
                    + "water element maximum of "
                    + $"{WaterElementResource.MaxAmount}.";
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
