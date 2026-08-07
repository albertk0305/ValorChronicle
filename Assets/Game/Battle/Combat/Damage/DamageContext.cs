using System;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Combat.Damage
{
    public sealed class DamageContext
    {
        public DamageContext(
            double baseAttack,
            double attackIncreaseRateSum,
            double skillCoefficient,
            bool appliesCombo,
            int finalComboCount,
            bool canCritical,
            bool isCritical,
            double criticalDamageMultiplier,
            double elementDamageIncreaseRateSum,
            double attackTypeDamageIncreaseRateSum,
            double dealtDamageIncreaseRateSum,
            ElementType attackElement,
            ElementType targetElement,
            double targetTakenDamageIncreaseRateSum,
            double targetTakenDamageReductionRateSum)
        {
            ValidateNonNegative(baseAttack, nameof(baseAttack));
            ValidateFinite(
                attackIncreaseRateSum,
                nameof(attackIncreaseRateSum));
            ValidateNonNegative(
                skillCoefficient,
                nameof(skillCoefficient));
            if (appliesCombo && finalComboCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(finalComboCount),
                    finalComboCount,
                    "Final combo count must be positive when combo applies.");
            }

            ValidateAtLeastOne(
                criticalDamageMultiplier,
                nameof(criticalDamageMultiplier));
            ValidateFinite(
                elementDamageIncreaseRateSum,
                nameof(elementDamageIncreaseRateSum));
            ValidateFinite(
                attackTypeDamageIncreaseRateSum,
                nameof(attackTypeDamageIncreaseRateSum));
            ValidateFinite(
                dealtDamageIncreaseRateSum,
                nameof(dealtDamageIncreaseRateSum));
            ValidateElement(attackElement, nameof(attackElement));
            ValidateElement(targetElement, nameof(targetElement));
            ValidateFinite(
                targetTakenDamageIncreaseRateSum,
                nameof(targetTakenDamageIncreaseRateSum));
            ValidateFinite(
                targetTakenDamageReductionRateSum,
                nameof(targetTakenDamageReductionRateSum));

            ValidateMultiplier(
                1d + attackIncreaseRateSum,
                nameof(attackIncreaseRateSum));
            ValidateMultiplier(
                1d + elementDamageIncreaseRateSum,
                nameof(elementDamageIncreaseRateSum));
            ValidateMultiplier(
                1d + attackTypeDamageIncreaseRateSum,
                nameof(attackTypeDamageIncreaseRateSum));
            ValidateMultiplier(
                1d + dealtDamageIncreaseRateSum,
                nameof(dealtDamageIncreaseRateSum));
            ValidateMultiplier(
                1d
                    + (targetTakenDamageIncreaseRateSum
                        - targetTakenDamageReductionRateSum),
                nameof(targetTakenDamageReductionRateSum));

            BaseAttack = baseAttack;
            AttackIncreaseRateSum = attackIncreaseRateSum;
            SkillCoefficient = skillCoefficient;
            AppliesCombo = appliesCombo;
            FinalComboCount = finalComboCount;
            CanCritical = canCritical;
            IsCritical = isCritical;
            CriticalDamageMultiplier = criticalDamageMultiplier;
            ElementDamageIncreaseRateSum = elementDamageIncreaseRateSum;
            AttackTypeDamageIncreaseRateSum =
                attackTypeDamageIncreaseRateSum;
            DealtDamageIncreaseRateSum = dealtDamageIncreaseRateSum;
            AttackElement = attackElement;
            TargetElement = targetElement;
            TargetTakenDamageIncreaseRateSum =
                targetTakenDamageIncreaseRateSum;
            TargetTakenDamageReductionRateSum =
                targetTakenDamageReductionRateSum;
        }

        public double BaseAttack { get; }
        public double AttackIncreaseRateSum { get; }
        public double SkillCoefficient { get; }
        public bool AppliesCombo { get; }
        public int FinalComboCount { get; }
        public bool CanCritical { get; }
        public bool IsCritical { get; }
        public double CriticalDamageMultiplier { get; }
        public double ElementDamageIncreaseRateSum { get; }
        public double AttackTypeDamageIncreaseRateSum { get; }
        public double DealtDamageIncreaseRateSum { get; }
        public ElementType AttackElement { get; }
        public ElementType TargetElement { get; }
        public double TargetTakenDamageIncreaseRateSum { get; }
        public double TargetTakenDamageReductionRateSum { get; }

        private static void ValidateNonNegative(
            double value,
            string parameterName)
        {
            ValidateFinite(value, parameterName);
            if (value < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Value cannot be negative.");
            }
        }

        private static void ValidateAtLeastOne(
            double value,
            string parameterName)
        {
            ValidateFinite(value, parameterName);
            if (value < 1d)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Value must be at least one.");
            }
        }

        private static void ValidateMultiplier(
            double multiplier,
            string parameterName)
        {
            if (!IsFinite(multiplier) || multiplier < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    multiplier,
                    "The resulting multiplier must be finite and non-negative.");
            }
        }

        private static void ValidateFinite(
            double value,
            string parameterName)
        {
            if (!IsFinite(value))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Value must be finite.");
            }
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static void ValidateElement(
            ElementType element,
            string parameterName)
        {
            if (!Enum.IsDefined(typeof(ElementType), element))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    element,
                    "Element must be a defined value.");
            }
        }
    }
}
