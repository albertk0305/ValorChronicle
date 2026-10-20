using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Characters.Serena;

namespace ValorChronicle.Tests.EditMode.Characters.Serena
{
    public sealed class SerenaCoralspearCombatConfigTests
    {
        [Test]
        public void DefaultConfigContainsDesignedValues()
        {
            SerenaCoralspearCombatConfig config =
                SerenaCoralspearTestConfig.Create();
            try
            {
                Assert.That(config.Match3Coefficient, Is.EqualTo(0.75d));
                Assert.That(config.Match4BaseCoefficient, Is.EqualTo(1.35d));
                Assert.That(config.Match4AdditionalCoefficient,
                    Is.EqualTo(0.90d));
                Assert.That(config.Match5Coefficient, Is.EqualTo(2.20d));
                Assert.That(config.PassiveChaseCoefficientPerWaterElement,
                    Is.EqualTo(0.75d));
                Assert.That(config.EmpoweredChaseDamageIncreaseRate,
                    Is.EqualTo(0.80d));
                Assert.That(config.EmpoweredChaseMaxCharges, Is.EqualTo(3));
                Assert.That(
                    config.ActiveAdditionalAndChaseDamageIncreaseRate,
                    Is.EqualTo(0.35d));
                Assert.That(config.ActiveCooldownTurns, Is.EqualTo(8));
                Assert.That(config.ActiveDurationTurns, Is.EqualTo(3));
                Assert.That(
                    config.Awakening2AdditionalAndChaseDamageIncreaseRate,
                    Is.EqualTo(0.10d));
                Assert.That(
                    config.Awakening3PassiveChaseCoefficientPerWaterElement,
                    Is.EqualTo(0.85d));
                Assert.That(config.Awakening4Match3Coefficient,
                    Is.EqualTo(0.85d));
                Assert.That(config.Awakening4Match4BaseCoefficient,
                    Is.EqualTo(1.50d));
                Assert.That(config.Awakening4Match4AdditionalCoefficient,
                    Is.EqualTo(1.00d));
                Assert.That(
                    config.Awakening5ActiveAdditionalAndChaseDamageIncreaseRate,
                    Is.EqualTo(0.45d));
                Assert.That(config.Match3WaterElementGainAmount,
                    Is.EqualTo(1));
                Assert.That(config.Awakening6Match5WaterElementGainAmount,
                    Is.EqualTo(1));
                Assert.That(config.WaterElementMaxAmount, Is.EqualTo(5));
                Assert.That(config.TryValidate(out _), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase("match3Coefficient", double.NaN, "Match3Coefficient")]
        [TestCase("match4AdditionalCoefficient", double.PositiveInfinity,
            "Match4AdditionalCoefficient")]
        [TestCase("empoweredChaseDamageIncreaseRate", -0.01d,
            "EmpoweredChaseDamageIncreaseRate")]
        [TestCase("awakening3PassiveChaseCoefficientPerWaterElement",
            -0.01d,
            "Awakening3PassiveChaseCoefficientPerWaterElement")]
        public void InvalidDoubleIsRejected(
            string propertyName,
            double value,
            string expectedField)
        {
            SerenaCoralspearCombatConfig config =
                SerenaCoralspearTestConfig.Create();
            try
            {
                SerenaCoralspearTestConfig.SetDouble(
                    config,
                    propertyName,
                    value);

                Assert.That(config.TryValidate(out string error), Is.False);
                Assert.That(error, Does.Contain(expectedField));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase("empoweredChaseMaxCharges", 0,
            "EmpoweredChaseMaxCharges")]
        [TestCase("activeCooldownTurns", -1, "ActiveCooldownTurns")]
        [TestCase("activeDurationTurns", 0, "ActiveDurationTurns")]
        [TestCase("match3WaterElementGainAmount", 0,
            "Match3WaterElementGainAmount")]
        [TestCase("awakening6Match5WaterElementGainAmount", 6,
            "Awakening6Match5WaterElementGainAmount")]
        [TestCase("waterElementMaxAmount", 4, "WaterElementMaxAmount")]
        public void InvalidIntegerIsRejected(
            string propertyName,
            int value,
            string expectedField)
        {
            SerenaCoralspearCombatConfig config =
                SerenaCoralspearTestConfig.Create();
            try
            {
                SerenaCoralspearTestConfig.SetInt(
                    config,
                    propertyName,
                    value);

                Assert.That(config.TryValidate(out string error), Is.False);
                Assert.That(error, Does.Contain(expectedField));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ResolveSeparatesAwakeningThresholds()
        {
            SerenaCoralspearCombatConfig config =
                SerenaCoralspearTestConfig.Create();
            try
            {
                SerenaCoralspearResolvedCombatRules awakening0 =
                    SerenaCoralspearCombatRuleResolver.Resolve(config, 0);
                SerenaCoralspearResolvedCombatRules awakening2 =
                    SerenaCoralspearCombatRuleResolver.Resolve(config, 2);
                SerenaCoralspearResolvedCombatRules awakening3 =
                    SerenaCoralspearCombatRuleResolver.Resolve(config, 3);
                SerenaCoralspearResolvedCombatRules awakening4 =
                    SerenaCoralspearCombatRuleResolver.Resolve(config, 4);
                SerenaCoralspearResolvedCombatRules awakening5 =
                    SerenaCoralspearCombatRuleResolver.Resolve(config, 5);
                SerenaCoralspearResolvedCombatRules awakening6 =
                    SerenaCoralspearCombatRuleResolver.Resolve(config, 6);

                Assert.That(
                    awakening0.AdditionalAndChaseDamageIncreaseRate,
                    Is.Zero);
                Assert.That(
                    awakening2.AdditionalAndChaseDamageIncreaseRate,
                    Is.EqualTo(0.10d));
                Assert.That(
                    awakening2.PassiveChaseCoefficientPerWaterElement,
                    Is.EqualTo(0.75d));
                Assert.That(
                    awakening3.PassiveChaseCoefficientPerWaterElement,
                    Is.EqualTo(0.85d));
                Assert.That(awakening3.Match3Coefficient,
                    Is.EqualTo(0.75d));
                Assert.That(awakening4.Match3Coefficient,
                    Is.EqualTo(0.85d));
                Assert.That(awakening4.Match4BaseCoefficient,
                    Is.EqualTo(1.50d));
                Assert.That(awakening4.Match4AdditionalCoefficient,
                    Is.EqualTo(1.00d));
                Assert.That(
                    awakening4.ActiveAdditionalAndChaseDamageIncreaseRate,
                    Is.EqualTo(0.35d));
                Assert.That(
                    awakening5.ActiveAdditionalAndChaseDamageIncreaseRate,
                    Is.EqualTo(0.45d));
                Assert.That(awakening5.Match5WaterElementGainAmount,
                    Is.Zero);
                Assert.That(awakening6.Match5WaterElementGainAmount,
                    Is.EqualTo(1));
                Assert.That(awakening6.Match5Coefficient,
                    Is.EqualTo(2.20d));
                Assert.That(awakening6.EmpoweredChaseDamageIncreaseRate,
                    Is.EqualTo(0.80d));
                Assert.That(awakening6.EmpoweredChaseMaxCharges,
                    Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}
