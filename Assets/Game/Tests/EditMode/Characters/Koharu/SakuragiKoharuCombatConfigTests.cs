using NUnit.Framework;
using UnityEngine;

namespace ValorChronicle.Tests.EditMode.Characters.Koharu
{
    public sealed class SakuragiKoharuCombatConfigTests
    {
        [Test]
        public void CanonicalValuesAreExposedAndValid()
        {
            var config = SakuragiKoharuTestConfig.Create();
            try
            {
                Assert.That(config.TryValidate(out _), Is.True);
                Assert.That(config.Match3Coefficient, Is.EqualTo(0.50d));
                Assert.That(config.Match4Coefficient, Is.EqualTo(0.80d));
                Assert.That(config.Match5Coefficient, Is.EqualTo(1.30d));
                Assert.That(new[]
                {
                    config.Match3SpiritEnergyGain,
                    config.Match4SpiritEnergyGain,
                    config.Match5SpiritEnergyGain
                }, Is.EqualTo(new[] { 1, 2, 3 }));
                Assert.That(
                    config.Match5AdditionalAndChaseDamageIncreaseRate,
                    Is.EqualTo(0.12d));
                Assert.That(config.Match5EffectDurationTurns, Is.EqualTo(2));
                Assert.That(config.Awakening3Match3Coefficient,
                    Is.EqualTo(0.55d));
                Assert.That(config.Awakening3Match4Coefficient,
                    Is.EqualTo(0.90d));
                Assert.That(config.Awakening4Match5Coefficient,
                    Is.EqualTo(1.45d));
                Assert.That(
                    config.Awakening4Match5AdditionalAndChaseDamageIncreaseRate,
                    Is.EqualTo(0.15d));
                Assert.That(config.SpiritEnergyCostPerCooldownReduction,
                    Is.EqualTo(6));
                Assert.That(
                    config.Awakening5SpiritEnergyCostPerCooldownReduction,
                    Is.EqualTo(5));
                Assert.That(
                    config.Awakening6AdditionalAndChaseDamageIncreaseRate,
                    Is.EqualTo(0.10d));
                Assert.That(config.Awakening6EffectDurationTurns,
                    Is.EqualTo(1));
                Assert.That(config.ActiveCooldownTurns, Is.EqualTo(4));
                Assert.That(config.ActiveBlockConversionCount,
                    Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(-0.01d)]
        public void InvalidRateIsRejected(double value)
        {
            var config = SakuragiKoharuTestConfig.Create(
                match3Coefficient: value);
            try
            {
                Assert.That(config.TryValidate(out string error), Is.False);
                Assert.That(error, Does.Contain("Match3Coefficient"));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase(0, 2, 3, 6, "Match3SpiritEnergyGain")]
        [TestCase(1, 0, 3, 6, "Match4SpiritEnergyGain")]
        [TestCase(1, 2, 0, 6, "Match5SpiritEnergyGain")]
        [TestCase(1, 2, 3, 0, "SpiritEnergyCostPerCooldownReduction")]
        public void InvalidPositiveSettingsAreRejected(
            int match3Gain,
            int match4Gain,
            int match5Gain,
            int spiritCost,
            string expectedField)
        {
            var config = SakuragiKoharuTestConfig.Create(
                match3SpiritGain: match3Gain,
                match4SpiritGain: match4Gain,
                match5SpiritGain: match5Gain,
                spiritCost: spiritCost);
            try
            {
                Assert.That(config.TryValidate(out string error), Is.False);
                Assert.That(error, Does.Contain(expectedField));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}
