using NUnit.Framework;

namespace ValorChronicle.Tests.EditMode.Characters.Miyuki
{
    public sealed class ShimotsukiMiyukiCombatConfigTests
    {
        [Test]
        public void CanonicalValuesAreExposedAndValid()
        {
            var config = ShimotsukiMiyukiTestConfig.Create();

            Assert.That(config.TryValidate(out _), Is.True);
            Assert.That(config.Match3Coefficient, Is.EqualTo(0.55d));
            Assert.That(config.Match4Coefficient, Is.EqualTo(0.90d));
            Assert.That(config.Match5Coefficient, Is.EqualTo(1.45d));
            Assert.That(new[] { config.Match3WaterAmount,
                config.Match4WaterAmount, config.Match5WaterAmount },
                Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(config.PassiveWaterDamageIncreaseRate,
                Is.EqualTo(0.08d));
            Assert.That(config.EnhancedPassiveWaterDamageIncreaseRate,
                Is.EqualTo(0.10d));
            Assert.That(config.Match5WaterDamageIncreaseRate,
                Is.EqualTo(0.12d));
            Assert.That(config.Match5EffectDurationTurns, Is.EqualTo(2));
            Assert.That(config.EnhancedMatch3Coefficient, Is.EqualTo(0.60d));
            Assert.That(config.EnhancedMatch4Coefficient, Is.EqualTo(1.00d));
            Assert.That(config.ActiveCooldownTurns, Is.EqualTo(8));
            Assert.That(config.ActiveBlockConversionCount, Is.EqualTo(4));
            Assert.That(config.EnhancedActiveBlockConversionCount, Is.EqualTo(5));
            Assert.That(config.Awakening6ExtraWaterAmount, Is.EqualTo(1));
            Assert.That(config.WaterElementMaxAmount, Is.EqualTo(5));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(-0.01d)]
        public void InvalidRateIsRejected(double value)
        {
            var config = ShimotsukiMiyukiTestConfig.Create(
                match3Coefficient: value);

            Assert.That(config.TryValidate(out string error), Is.False);
            Assert.That(error, Does.Contain("Match3Coefficient"));
        }

        [TestCase(0, 2, 3, 5, "Match3WaterAmount")]
        [TestCase(1, 0, 3, 5, "Match4WaterAmount")]
        [TestCase(1, 2, 0, 5, "Match5WaterAmount")]
        [TestCase(1, 2, 3, 4, "WaterElementMaxAmount")]
        public void InvalidResourceSettingsAreRejected(
            int match3,
            int match4,
            int match5,
            int maximum,
            string field)
        {
            var config = ShimotsukiMiyukiTestConfig.Create(
                match3WaterAmount: match3,
                match4WaterAmount: match4,
                match5WaterAmount: match5,
                waterMaximum: maximum);

            Assert.That(config.TryValidate(out string error), Is.False);
            Assert.That(error, Does.Contain(field));
        }
    }
}
