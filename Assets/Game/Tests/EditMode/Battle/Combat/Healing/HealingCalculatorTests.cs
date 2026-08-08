using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Healing;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Healing
{
    public sealed class HealingCalculatorTests
    {
        [Test]
        public void CalculateWithComboMultiplier_ValidatesDocumentedFormula()
        {
            var context = new HealingContext(
                3000,
                0.20d,
                true,
                1,
                0.20d);

            HealingResult result =
                HealingCalculator.CalculateWithComboMultiplier(
                    context,
                    1.50d);

            Assert.That(result.SourceMaxHp, Is.EqualTo(3000L));
            Assert.That(result.HealingCoefficient, Is.EqualTo(0.20d));
            Assert.That(result.ComboMultiplier, Is.EqualTo(1.50d));
            Assert.That(result.HealingIncreaseMultiplier, Is.EqualTo(1.20d));
            Assert.That(result.RawHealing, Is.EqualTo(1080d));
            Assert.That(result.FinalHealing, Is.EqualTo(1080L));
        }

        [Test]
        public void Calculate_UsesActualComboTableAndFloorsOnlyFinalValue()
        {
            var context = new HealingContext(
                5800,
                0.08d,
                true,
                3,
                0d);

            HealingResult result = HealingCalculator.Calculate(context);

            Assert.That(result.ComboMultiplier, Is.EqualTo(1.12d));
            Assert.That(result.RawHealing,
                Is.EqualTo(519.68d).Within(0.000000001d));
            Assert.That(result.FinalHealing, Is.EqualTo(519L));
        }

        [Test]
        public void Calculate_WhenComboDoesNotApplyUsesOne()
        {
            HealingResult result = HealingCalculator.Calculate(
                new HealingContext(1000, 0.25d, false, 0, 0d));

            Assert.That(result.ComboMultiplier, Is.EqualTo(1d));
            Assert.That(result.FinalHealing, Is.EqualTo(250L));
        }

        [Test]
        public void Context_RejectsInvalidValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HealingContext(0, 0.1d, false, 0, 0d));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HealingContext(1, -0.1d, false, 0, 0d));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HealingContext(1, double.NaN, false, 0, 0d));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HealingContext(1, 0.1d, true, 0, 0d));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HealingContext(1, 0.1d, false, 0, double.PositiveInfinity));
        }

        [Test]
        public void Calculate_RejectsNullAndNonFiniteResult()
        {
            Assert.Throws<ArgumentNullException>(() =>
                HealingCalculator.Calculate(null));
            Assert.Throws<OverflowException>(() =>
                HealingCalculator.Calculate(
                    new HealingContext(
                        long.MaxValue,
                        double.MaxValue,
                        false,
                        0,
                        0d)));
        }
    }
}
