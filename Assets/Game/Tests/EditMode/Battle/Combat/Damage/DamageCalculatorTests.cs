using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Damage
{
    public sealed class DamageCalculatorTests
    {
        [Test]
        public void Calculate_UsesSummedAttackIncreaseSnapshot()
        {
            DamageResult result = DamageCalculator.Calculate(
                CreateContext(baseAttack: 100d, attackIncreaseRateSum: 0.20d + 0.15d));

            Assert.That(result.FinalAttack, Is.EqualTo(135d).Within(0.000000001d));
            Assert.That(result.FinalDamage, Is.EqualTo(135L));
        }

        [Test]
        public void Calculate_UsesSummedElementDamageSnapshot()
        {
            DamageResult result = DamageCalculator.Calculate(
                CreateContext(
                    baseAttack: 100d,
                    elementDamageIncreaseRateSum: 0.25d + 0.10d));

            Assert.That(result.ElementDamageMultiplier,
                Is.EqualTo(1.35d).Within(0.000000001d));
            Assert.That(result.FinalDamage, Is.EqualTo(135L));
        }

        [Test]
        public void Calculate_MultipliesDifferentModifierCategories()
        {
            DamageResult result = DamageCalculator.Calculate(
                CreateContext(
                    baseAttack: 100d,
                    attackIncreaseRateSum: 0.20d,
                    elementDamageIncreaseRateSum: 0.30d));

            Assert.That(result.RawDamage, Is.EqualTo(156d).Within(0.000000001d));
            Assert.That(result.FinalDamage, Is.EqualTo(156L));
        }

        [Test]
        public void Calculate_AppliesComboOnlyWhenEnabled()
        {
            DamageResult applied = DamageCalculator.Calculate(
                CreateContext(appliesCombo: true, finalComboCount: 3));
            DamageResult ignored = DamageCalculator.Calculate(
                CreateContext(appliesCombo: false, finalComboCount: 10));

            Assert.That(applied.ComboMultiplier, Is.EqualTo(1.12d));
            Assert.That(ignored.ComboMultiplier, Is.EqualTo(1d));
        }

        [Test]
        public void Calculate_AppliesCriticalOnlyWhenAllowedAndTriggered()
        {
            DamageResult critical = DamageCalculator.Calculate(
                CreateContext(
                    canCritical: true,
                    isCritical: true,
                    criticalDamageMultiplier: 1.5d));
            DamageResult notTriggered = DamageCalculator.Calculate(
                CreateContext(
                    canCritical: true,
                    isCritical: false,
                    criticalDamageMultiplier: 1.5d));
            DamageResult notAllowed = DamageCalculator.Calculate(
                CreateContext(
                    canCritical: false,
                    isCritical: true,
                    criticalDamageMultiplier: 1.5d));

            Assert.That(critical.CriticalMultiplier, Is.EqualTo(1.5d));
            Assert.That(notTriggered.CriticalMultiplier, Is.EqualTo(1d));
            Assert.That(notAllowed.CriticalMultiplier, Is.EqualTo(1d));
        }

        [Test]
        public void Calculate_AppliesElementAffinity()
        {
            DamageResult result = DamageCalculator.Calculate(
                CreateContext(
                    baseAttack: 100d,
                    attackElement: ElementType.Fire,
                    targetElement: ElementType.Grass));

            Assert.That(result.ElementAffinityMultiplier, Is.EqualTo(1.30d));
            Assert.That(result.FinalDamage, Is.EqualTo(130L));
        }

        [Test]
        public void Calculate_CombinesTargetTakenIncreaseAndReduction()
        {
            DamageResult result = DamageCalculator.Calculate(
                CreateContext(
                    baseAttack: 100d,
                    targetTakenDamageIncreaseRateSum: 0.20d,
                    targetTakenDamageReductionRateSum: 0.10d));

            Assert.That(result.TargetTakenDamageMultiplier,
                Is.EqualTo(1.10d).Within(0.000000001d));
            Assert.That(result.FinalDamage, Is.EqualTo(110L));
        }

        [Test]
        public void Calculate_AllCategoriesMatchRepresentativeDamage()
        {
            DamageResult result = DamageCalculator.Calculate(
                CreateContext(
                    baseAttack: 1000d,
                    attackIncreaseRateSum: 0.35d,
                    skillCoefficient: 2d,
                    appliesCombo: true,
                    finalComboCount: 3,
                    canCritical: true,
                    isCritical: true,
                    criticalDamageMultiplier: 1.5d,
                    elementDamageIncreaseRateSum: 0.25d,
                    attackTypeDamageIncreaseRateSum: 0.10d,
                    dealtDamageIncreaseRateSum: 0.15d,
                    attackElement: ElementType.Fire,
                    targetElement: ElementType.Grass,
                    targetTakenDamageIncreaseRateSum: 0.20d,
                    targetTakenDamageReductionRateSum: 0.10d));

            Assert.That(result.FinalAttack, Is.EqualTo(1350d));
            Assert.That(result.RawDamage,
                Is.EqualTo(10256.7465d).Within(0.000000001d));
            Assert.That(result.FinalDamage, Is.EqualTo(10256L));
        }

        [Test]
        public void Calculate_FloorsOnlyAfterAllMultiplication()
        {
            DamageResult result = DamageCalculator.Calculate(
                CreateContext(
                    baseAttack: 1d,
                    attackIncreaseRateSum: 0.01d,
                    skillCoefficient: 1.99d));

            Assert.That(result.FinalAttack, Is.EqualTo(1.01d));
            Assert.That(result.RawDamage, Is.EqualTo(2.0099d).Within(0.000000001d));
            Assert.That(result.FinalDamage, Is.EqualTo(2L));
        }

        [Test]
        public void Context_RejectsInvalidInputsAndNegativeMultipliers()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateContext(baseAttack: -1d));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateContext(skillCoefficient: -1d));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateContext(criticalDamageMultiplier: 0.99d));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateContext(appliesCombo: true, finalComboCount: 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateContext(attackIncreaseRateSum: -1.01d));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateContext(
                    targetTakenDamageReductionRateSum: 1.01d));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateContext(baseAttack: double.NaN));
        }

        [Test]
        public void Calculate_RejectsNullContext()
        {
            Assert.Throws<ArgumentNullException>(
                () => DamageCalculator.Calculate(null));
        }

        private static DamageContext CreateContext(
            double baseAttack = 1d,
            double attackIncreaseRateSum = 0d,
            double skillCoefficient = 1d,
            bool appliesCombo = false,
            int finalComboCount = 1,
            bool canCritical = false,
            bool isCritical = false,
            double criticalDamageMultiplier = 1d,
            double elementDamageIncreaseRateSum = 0d,
            double attackTypeDamageIncreaseRateSum = 0d,
            double dealtDamageIncreaseRateSum = 0d,
            ElementType attackElement = ElementType.Fire,
            ElementType targetElement = ElementType.Fire,
            double targetTakenDamageIncreaseRateSum = 0d,
            double targetTakenDamageReductionRateSum = 0d)
        {
            return new DamageContext(
                baseAttack,
                attackIncreaseRateSum,
                skillCoefficient,
                appliesCombo,
                finalComboCount,
                canCritical,
                isCritical,
                criticalDamageMultiplier,
                elementDamageIncreaseRateSum,
                attackTypeDamageIncreaseRateSum,
                dealtDamageIncreaseRateSum,
                attackElement,
                targetElement,
                targetTakenDamageIncreaseRateSum,
                targetTakenDamageReductionRateSum);
        }
    }
}
