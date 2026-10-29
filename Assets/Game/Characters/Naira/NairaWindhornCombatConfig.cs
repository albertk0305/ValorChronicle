using UnityEngine;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Naira
{
    [CreateAssetMenu(
        fileName = "NairaWindhornCombatConfig",
        menuName = "Valor Chronicle/Combat Configs/Naira Windhorn")]
    public sealed class NairaWindhornCombatConfig
        : CombatConfigDefinition
    {
        public const int RequiredShieldDurationTurns = 3;
        public const int RequiredActiveCooldownTurns = 12;

        [SerializeField]
        private double match3ShieldMaxHpRate = 0.07d;

        [SerializeField]
        private double match4ShieldMaxHpRate = 0.12d;

        [SerializeField]
        private double match5ShieldMaxHpRate = 0.20d;

        [SerializeField]
        private double activeShieldMaxHpRate = 0.35d;

        [SerializeField]
        private int shieldDurationTurns = RequiredShieldDurationTurns;

        [SerializeField]
        private bool matchShieldUsesComboMultiplier = true;

        [SerializeField]
        private bool activeShieldUsesComboMultiplier = false;

        [SerializeField]
        private double match4BaseDamageCoefficient = 1.20d;

        [SerializeField]
        private double match4ChargeDamageIncreaseRate = 0.50d;

        [SerializeField]
        private double match5BaseDamageCoefficient = 2.20d;

        [SerializeField]
        private double match5ChargeDamageIncreaseRate = 1.00d;

        [SerializeField]
        private double passiveTargetTakenDamageIncreaseRate = 0.05d;

        [SerializeField]
        private double awakening3PassiveTargetTakenDamageIncreaseRate =
            0.06d;

        [SerializeField]
        private int activeCooldownTurns = RequiredActiveCooldownTurns;

        [SerializeField]
        private double whiteStagGraceDamageReductionRate = 0.35d;

        [SerializeField]
        private double awakening5WhiteStagGraceDamageReductionRate =
            0.40d;

        [SerializeField]
        private double awakening4Match3ShieldMaxHpRate = 0.08d;

        [SerializeField]
        private double awakening4Match4ShieldMaxHpRate = 0.13d;

        [SerializeField]
        private double ancientGuardianDamageReductionRate = 0.15d;

        public double Match3ShieldMaxHpRate => match3ShieldMaxHpRate;
        public double Match4ShieldMaxHpRate => match4ShieldMaxHpRate;
        public double Match5ShieldMaxHpRate => match5ShieldMaxHpRate;
        public double ActiveShieldMaxHpRate => activeShieldMaxHpRate;
        public int ShieldDurationTurns => shieldDurationTurns;
        public bool MatchShieldUsesComboMultiplier =>
            matchShieldUsesComboMultiplier;
        public bool ActiveShieldUsesComboMultiplier =>
            activeShieldUsesComboMultiplier;
        public double Match4BaseDamageCoefficient =>
            match4BaseDamageCoefficient;
        public double Match4ChargeDamageIncreaseRate =>
            match4ChargeDamageIncreaseRate;
        public double Match5BaseDamageCoefficient =>
            match5BaseDamageCoefficient;
        public double Match5ChargeDamageIncreaseRate =>
            match5ChargeDamageIncreaseRate;
        public double PassiveTargetTakenDamageIncreaseRate =>
            passiveTargetTakenDamageIncreaseRate;
        public double Awakening3PassiveTargetTakenDamageIncreaseRate =>
            awakening3PassiveTargetTakenDamageIncreaseRate;
        public int ActiveCooldownTurns => activeCooldownTurns;
        public double WhiteStagGraceDamageReductionRate =>
            whiteStagGraceDamageReductionRate;
        public double Awakening5WhiteStagGraceDamageReductionRate =>
            awakening5WhiteStagGraceDamageReductionRate;
        public double Awakening4Match3ShieldMaxHpRate =>
            awakening4Match3ShieldMaxHpRate;
        public double Awakening4Match4ShieldMaxHpRate =>
            awakening4Match4ShieldMaxHpRate;
        public double AncientGuardianDamageReductionRate =>
            ancientGuardianDamageReductionRate;

        public override bool TryValidate(out string errorMessage)
        {
            if (!IsFiniteUnitRate(match3ShieldMaxHpRate))
            {
                errorMessage = "Match3ShieldMaxHpRate must be finite and "
                    + "between zero and one.";
                return false;
            }

            if (!IsFiniteUnitRate(match4ShieldMaxHpRate))
            {
                errorMessage = "Match4ShieldMaxHpRate must be finite and "
                    + "between zero and one.";
                return false;
            }

            if (!IsFiniteUnitRate(match5ShieldMaxHpRate))
            {
                errorMessage = "Match5ShieldMaxHpRate must be finite and "
                    + "between zero and one.";
                return false;
            }

            if (!IsFiniteUnitRate(activeShieldMaxHpRate))
            {
                errorMessage = "ActiveShieldMaxHpRate must be finite and "
                    + "between zero and one.";
                return false;
            }

            if (shieldDurationTurns != RequiredShieldDurationTurns)
            {
                errorMessage = $"ShieldDurationTurns must be "
                    + $"{RequiredShieldDurationTurns}.";
                return false;
            }

            if (!matchShieldUsesComboMultiplier)
            {
                errorMessage = "MatchShieldUsesComboMultiplier must be true.";
                return false;
            }

            if (activeShieldUsesComboMultiplier)
            {
                errorMessage = "ActiveShieldUsesComboMultiplier must be false.";
                return false;
            }

            if (!IsFinitePositive(match4BaseDamageCoefficient))
            {
                errorMessage = "Match4BaseDamageCoefficient must be finite "
                    + "and greater than zero.";
                return false;
            }

            if (!IsFiniteNonNegative(match4ChargeDamageIncreaseRate))
            {
                errorMessage = "Match4ChargeDamageIncreaseRate must be "
                    + "finite and non-negative.";
                return false;
            }

            if (!IsFinitePositive(match5BaseDamageCoefficient))
            {
                errorMessage = "Match5BaseDamageCoefficient must be finite "
                    + "and greater than zero.";
                return false;
            }

            if (!IsFiniteNonNegative(match5ChargeDamageIncreaseRate))
            {
                errorMessage = "Match5ChargeDamageIncreaseRate must be "
                    + "finite and non-negative.";
                return false;
            }

            if (!IsFiniteUnitRate(passiveTargetTakenDamageIncreaseRate))
            {
                errorMessage = "PassiveTargetTakenDamageIncreaseRate must "
                    + "be finite and between zero and one.";
                return false;
            }

            if (!IsFiniteUnitRate(
                    awakening3PassiveTargetTakenDamageIncreaseRate))
            {
                errorMessage = "Awakening3PassiveTargetTakenDamageIncreaseRate "
                    + "must be finite and between zero and one.";
                return false;
            }

            if (awakening3PassiveTargetTakenDamageIncreaseRate
                < passiveTargetTakenDamageIncreaseRate)
            {
                errorMessage = "Awakening3PassiveTargetTakenDamageIncreaseRate "
                    + "cannot be less than "
                    + "PassiveTargetTakenDamageIncreaseRate.";
                return false;
            }

            if (activeCooldownTurns != RequiredActiveCooldownTurns)
            {
                errorMessage = $"ActiveCooldownTurns must be "
                    + $"{RequiredActiveCooldownTurns}.";
                return false;
            }

            if (!IsFiniteUnitRate(whiteStagGraceDamageReductionRate))
            {
                errorMessage = "WhiteStagGraceDamageReductionRate must be "
                    + "finite and between zero and one.";
                return false;
            }

            if (!IsFiniteUnitRate(
                    awakening5WhiteStagGraceDamageReductionRate))
            {
                errorMessage = "Awakening5WhiteStagGraceDamageReductionRate "
                    + "must be finite and between zero and one.";
                return false;
            }

            if (awakening5WhiteStagGraceDamageReductionRate
                < whiteStagGraceDamageReductionRate)
            {
                errorMessage = "Awakening5WhiteStagGraceDamageReductionRate "
                    + "cannot be less than "
                    + "WhiteStagGraceDamageReductionRate.";
                return false;
            }

            if (!IsFiniteUnitRate(awakening4Match3ShieldMaxHpRate))
            {
                errorMessage = "Awakening4Match3ShieldMaxHpRate must be "
                    + "finite and between zero and one.";
                return false;
            }

            if (awakening4Match3ShieldMaxHpRate < match3ShieldMaxHpRate)
            {
                errorMessage = "Awakening4Match3ShieldMaxHpRate cannot be "
                    + "less than Match3ShieldMaxHpRate.";
                return false;
            }

            if (!IsFiniteUnitRate(awakening4Match4ShieldMaxHpRate))
            {
                errorMessage = "Awakening4Match4ShieldMaxHpRate must be "
                    + "finite and between zero and one.";
                return false;
            }

            if (awakening4Match4ShieldMaxHpRate < match4ShieldMaxHpRate)
            {
                errorMessage = "Awakening4Match4ShieldMaxHpRate cannot be "
                    + "less than Match4ShieldMaxHpRate.";
                return false;
            }

            if (!IsFiniteUnitRate(ancientGuardianDamageReductionRate))
            {
                errorMessage = "AncientGuardianDamageReductionRate must be "
                    + "finite and between zero and one.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool IsFiniteUnitRate(double value)
        {
            return IsFiniteNonNegative(value) && value <= 1d;
        }

        private static bool IsFiniteNonNegative(double value)
        {
            return !double.IsNaN(value)
                && !double.IsInfinity(value)
                && value >= 0d;
        }

        private static bool IsFinitePositive(double value)
        {
            return IsFiniteNonNegative(value) && value > 0d;
        }
    }
}

