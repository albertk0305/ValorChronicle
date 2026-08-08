using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Shields;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Shields
{
    public sealed class ShieldGenerationCalculatorTests
    {
        [Test]
        public void CalculateWithComboMultiplier_ValidatesDocumentedFormula()
        {
            var context = new ShieldGenerationContext(
                4000,
                0.25d,
                true,
                1,
                0.10d);

            ShieldGenerationResult result =
                ShieldGenerationCalculator.CalculateWithComboMultiplier(
                    context,
                    1.40d);

            Assert.That(result.SourceMaxHp, Is.EqualTo(4000L));
            Assert.That(result.ShieldCoefficient, Is.EqualTo(0.25d));
            Assert.That(result.ComboMultiplier, Is.EqualTo(1.40d));
            Assert.That(result.ShieldAmountIncreaseMultiplier,
                Is.EqualTo(1.10d));
            Assert.That(result.RawShieldAmount,
                Is.EqualTo(1540d).Within(0.000000001d));
            Assert.That(result.FinalShieldAmount, Is.EqualTo(1540L));
        }

        [Test]
        public void Calculate_UsesActualComboTableAndFloorsOnlyFinalValue()
        {
            var context = new ShieldGenerationContext(
                6200,
                0.07d,
                true,
                3,
                0d);

            ShieldGenerationResult result =
                ShieldGenerationCalculator.Calculate(context);

            Assert.That(result.ComboMultiplier, Is.EqualTo(1.12d));
            Assert.That(result.RawShieldAmount,
                Is.EqualTo(486.08d).Within(0.000000001d));
            Assert.That(result.FinalShieldAmount, Is.EqualTo(486L));
        }

        [Test]
        public void Calculate_WhenComboDoesNotApplyUsesOne()
        {
            ShieldGenerationResult result =
                ShieldGenerationCalculator.Calculate(
                    new ShieldGenerationContext(
                        1000,
                        0.25d,
                        false,
                        0,
                        0d));

            Assert.That(result.ComboMultiplier, Is.EqualTo(1d));
            Assert.That(result.FinalShieldAmount, Is.EqualTo(250L));
        }

        [Test]
        public void Context_RejectsInvalidValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ShieldGenerationContext(0, 0.1d, false, 0, 0d));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ShieldGenerationContext(1, -0.1d, false, 0, 0d));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ShieldGenerationContext(1, double.NaN, false, 0, 0d));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ShieldGenerationContext(1, 0.1d, true, 0, 0d));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ShieldGenerationContext(
                    1,
                    0.1d,
                    false,
                    0,
                    double.PositiveInfinity));
        }

        [Test]
        public void Calculate_RejectsNullAndNonFiniteResult()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ShieldGenerationCalculator.Calculate(null));
            Assert.Throws<OverflowException>(() =>
                ShieldGenerationCalculator.Calculate(
                    new ShieldGenerationContext(
                        long.MaxValue,
                        double.MaxValue,
                        false,
                        0,
                        0d)));
        }
    }
}
