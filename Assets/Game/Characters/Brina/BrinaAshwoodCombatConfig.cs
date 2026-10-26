using UnityEngine;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Brina
{
    [CreateAssetMenu(
        fileName = "BrinaAshwoodCombatConfig",
        menuName = "Valor Chronicle/Combat Configs/Brina Ashwood")]
    public sealed class BrinaAshwoodCombatConfig
        : CombatConfigDefinition
    {
        [SerializeField]
        private double match3Coefficient = 1.30d;

        [SerializeField]
        private double match4Coefficient = 2.40d;

        [SerializeField]
        private double match5Coefficient = 4.00d;

        [SerializeField]
        private int match3WoodcutGainAmount = 1;

        [SerializeField]
        private int match4WoodcutGainAmount = 2;

        [SerializeField]
        private int match5WoodcutGainAmount = 3;

        [SerializeField]
        private int woodcutTriggerThreshold = 10;

        [SerializeField]
        private double passiveAdditionalCoefficient = 5.00d;

        [SerializeField]
        private double activeWoodcutGainMultiplier = 2.00d;

        [SerializeField]
        private int activeCooldownTurns = 8;

        [SerializeField]
        private int activeDurationTurns = 1;

        [SerializeField]
        private double awakening3PassiveAdditionalCoefficient = 5.25d;

        [SerializeField]
        private double awakening4Match3Coefficient = 1.35d;

        [SerializeField]
        private double awakening4Match4Coefficient = 2.50d;

        [SerializeField]
        private int awakening5ActiveCooldownTurns = 7;

        [SerializeField]
        private int awakening6FirstWoodcutBonusAmount = 1;

        public double Match3Coefficient => match3Coefficient;
        public double Match4Coefficient => match4Coefficient;
        public double Match5Coefficient => match5Coefficient;
        public int Match3WoodcutGainAmount => match3WoodcutGainAmount;
        public int Match4WoodcutGainAmount => match4WoodcutGainAmount;
        public int Match5WoodcutGainAmount => match5WoodcutGainAmount;
        public int WoodcutTriggerThreshold => woodcutTriggerThreshold;
        public double PassiveAdditionalCoefficient =>
            passiveAdditionalCoefficient;
        public double ActiveWoodcutGainMultiplier =>
            activeWoodcutGainMultiplier;
        public int ActiveCooldownTurns => activeCooldownTurns;
        public int ActiveDurationTurns => activeDurationTurns;
        public double Awakening3PassiveAdditionalCoefficient =>
            awakening3PassiveAdditionalCoefficient;
        public double Awakening4Match3Coefficient =>
            awakening4Match3Coefficient;
        public double Awakening4Match4Coefficient =>
            awakening4Match4Coefficient;
        public int Awakening5ActiveCooldownTurns =>
            awakening5ActiveCooldownTurns;
        public int Awakening6FirstWoodcutBonusAmount =>
            awakening6FirstWoodcutBonusAmount;

        public override bool TryValidate(out string errorMessage)
        {
            if (!IsFiniteNonNegative(match3Coefficient))
            {
                errorMessage =
                    "Match3Coefficient must be finite and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(match4Coefficient))
            {
                errorMessage =
                    "Match4Coefficient must be finite and non-negative.";
                return false;
            }

            if (!IsFiniteNonNegative(match5Coefficient))
            {
                errorMessage =
                    "Match5Coefficient must be finite and non-negative.";
                return false;
            }

            if (match3WoodcutGainAmount <= 0)
            {
                errorMessage =
                    "Match3WoodcutGainAmount must be greater than zero.";
                return false;
            }

            if (match4WoodcutGainAmount <= 0)
            {
                errorMessage =
                    "Match4WoodcutGainAmount must be greater than zero.";
                return false;
            }

            if (match5WoodcutGainAmount <= 0)
            {
                errorMessage =
                    "Match5WoodcutGainAmount must be greater than zero.";
                return false;
            }

            if (woodcutTriggerThreshold <= 0)
            {
                errorMessage =
                    "WoodcutTriggerThreshold must be greater than zero.";
                return false;
            }

            if (!IsFiniteNonNegative(passiveAdditionalCoefficient))
            {
                errorMessage = "PassiveAdditionalCoefficient must be "
                    + "finite and non-negative.";
                return false;
            }

            if (!IsFinitePositive(activeWoodcutGainMultiplier))
            {
                errorMessage = "ActiveWoodcutGainMultiplier must be "
                    + "finite and greater than zero.";
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
                    awakening3PassiveAdditionalCoefficient))
            {
                errorMessage = "Awakening3PassiveAdditionalCoefficient "
                    + "must be finite and non-negative.";
                return false;
            }

            if (awakening3PassiveAdditionalCoefficient
                < passiveAdditionalCoefficient)
            {
                errorMessage = "Awakening3PassiveAdditionalCoefficient "
                    + "cannot be less than PassiveAdditionalCoefficient.";
                return false;
            }

            if (!IsFiniteNonNegative(awakening4Match3Coefficient))
            {
                errorMessage = "Awakening4Match3Coefficient must be "
                    + "finite and non-negative.";
                return false;
            }

            if (awakening4Match3Coefficient < match3Coefficient)
            {
                errorMessage = "Awakening4Match3Coefficient cannot be "
                    + "less than Match3Coefficient.";
                return false;
            }

            if (!IsFiniteNonNegative(awakening4Match4Coefficient))
            {
                errorMessage = "Awakening4Match4Coefficient must be "
                    + "finite and non-negative.";
                return false;
            }

            if (awakening4Match4Coefficient < match4Coefficient)
            {
                errorMessage = "Awakening4Match4Coefficient cannot be "
                    + "less than Match4Coefficient.";
                return false;
            }

            if (awakening5ActiveCooldownTurns < 0
                || awakening5ActiveCooldownTurns > activeCooldownTurns)
            {
                errorMessage = "Awakening5ActiveCooldownTurns must be "
                    + "between zero and ActiveCooldownTurns.";
                return false;
            }

            if (awakening6FirstWoodcutBonusAmount <= 0)
            {
                errorMessage = "Awakening6FirstWoodcutBonusAmount must be "
                    + "greater than zero.";
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

        private static bool IsFinitePositive(double value)
        {
            return !double.IsNaN(value)
                && !double.IsInfinity(value)
                && value > 0d;
        }
    }
}
