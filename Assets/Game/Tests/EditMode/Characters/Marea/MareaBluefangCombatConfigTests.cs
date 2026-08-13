using NUnit.Framework;

namespace ValorChronicle.Tests.EditMode.Characters.Marea
{
    public sealed class MareaBluefangCombatConfigTests
    {
        [Test]
        public void CanonicalConfigContainsMigratedValues()
        {
            var config = MareaBluefangTestConfig.Canonical;

            Assert.That(config, Is.Not.Null);
            Assert.That(config.Match3Coefficient, Is.EqualTo(0.90d));
            Assert.That(config.Match4BaseCoefficient, Is.EqualTo(1.50d));
            Assert.That(config.Match4WaterElementBonusCoefficient,
                Is.EqualTo(0.40d));
            Assert.That(config.Match5BaseCoefficient, Is.EqualTo(2.40d));
            Assert.That(config.Match5CoefficientPerWaterElement,
                Is.EqualTo(1.40d));
            Assert.That(config.PassiveDealtDamageIncreaseRate,
                Is.EqualTo(0.15d));
            Assert.That(config.ActiveWaterDamageIncreaseRate,
                Is.EqualTo(0.25d));
            Assert.That(config.ActiveDurationTurns, Is.EqualTo(3));
            Assert.That(config.ActiveCooldownTurns, Is.EqualTo(8));
            Assert.That(config.WaterElementMaxAmount, Is.EqualTo(5));
            Assert.That(config.TryValidate(out _), Is.True);
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(-0.01d)]
        public void InvalidDoubleIsRejected(double value)
        {
            var config = MareaBluefangTestConfig.Create(
                match3Coefficient: value);

            Assert.That(config.TryValidate(out string error), Is.False);
            Assert.That(error, Does.Contain("Match3Coefficient"));
        }

        [TestCase(0, 8, 5, "ActiveDurationTurns")]
        [TestCase(3, -1, 5, "ActiveCooldownTurns")]
        [TestCase(3, 8, 0, "WaterElementMaxAmount")]
        public void InvalidIntegerIsRejected(
            int duration,
            int cooldown,
            int waterMaximum,
            string expectedField)
        {
            var config = MareaBluefangTestConfig.Create(
                activeDurationTurns: duration,
                activeCooldownTurns: cooldown,
                waterElementMaxAmount: waterMaximum);

            Assert.That(config.TryValidate(out string error), Is.False);
            Assert.That(error, Does.Contain(expectedField));
        }

        [Test]
        public void RatesAboveOneRemainValid()
        {
            var config = MareaBluefangTestConfig.Create(
                passiveDealtDamageIncreaseRate: 1.5d,
                activeWaterDamageIncreaseRate: 2d);

            Assert.That(config.TryValidate(out _), Is.True);
        }
    }
}
