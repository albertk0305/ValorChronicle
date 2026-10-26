using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Characters.Brina;

namespace ValorChronicle.Tests.EditMode.Characters.Brina
{
    public sealed class BrinaAshwoodCombatConfigTests
    {
        [Test]
        public void DefaultConfigContainsDesignedValues()
        {
            BrinaAshwoodCombatConfig config =
                BrinaAshwoodTestConfig.Create();
            try
            {
                Assert.That(config.Match3Coefficient, Is.EqualTo(1.30d));
                Assert.That(config.Match4Coefficient, Is.EqualTo(2.40d));
                Assert.That(config.Match5Coefficient, Is.EqualTo(4.00d));
                Assert.That(config.Match3WoodcutGainAmount, Is.EqualTo(1));
                Assert.That(config.Match4WoodcutGainAmount, Is.EqualTo(2));
                Assert.That(config.Match5WoodcutGainAmount, Is.EqualTo(3));
                Assert.That(config.WoodcutTriggerThreshold, Is.EqualTo(10));
                Assert.That(config.PassiveAdditionalCoefficient,
                    Is.EqualTo(5.00d));
                Assert.That(config.ActiveWoodcutGainMultiplier,
                    Is.EqualTo(2.00d));
                Assert.That(config.ActiveCooldownTurns, Is.EqualTo(8));
                Assert.That(config.ActiveDurationTurns, Is.EqualTo(1));
                Assert.That(
                    config.Awakening3PassiveAdditionalCoefficient,
                    Is.EqualTo(5.25d));
                Assert.That(config.Awakening4Match3Coefficient,
                    Is.EqualTo(1.35d));
                Assert.That(config.Awakening4Match4Coefficient,
                    Is.EqualTo(2.50d));
                Assert.That(config.Awakening5ActiveCooldownTurns,
                    Is.EqualTo(7));
                Assert.That(config.Awakening6FirstWoodcutBonusAmount,
                    Is.EqualTo(1));
                Assert.That(config.TryValidate(out _), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase("match3Coefficient", double.NaN,
            "Match3Coefficient")]
        [TestCase("match4Coefficient", double.PositiveInfinity,
            "Match4Coefficient")]
        [TestCase("match5Coefficient", -0.01d,
            "Match5Coefficient")]
        [TestCase("passiveAdditionalCoefficient", -0.01d,
            "PassiveAdditionalCoefficient")]
        [TestCase("activeWoodcutGainMultiplier", 0d,
            "ActiveWoodcutGainMultiplier")]
        [TestCase("awakening3PassiveAdditionalCoefficient", -0.01d,
            "Awakening3PassiveAdditionalCoefficient")]
        [TestCase("awakening4Match3Coefficient", double.NaN,
            "Awakening4Match3Coefficient")]
        [TestCase("awakening4Match4Coefficient",
            double.NegativeInfinity,
            "Awakening4Match4Coefficient")]
        public void InvalidDoubleIsRejected(
            string propertyName,
            double value,
            string expectedField)
        {
            BrinaAshwoodCombatConfig config =
                BrinaAshwoodTestConfig.Create();
            try
            {
                BrinaAshwoodTestConfig.SetDouble(
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

        [TestCase("match3WoodcutGainAmount", 0,
            "Match3WoodcutGainAmount")]
        [TestCase("match4WoodcutGainAmount", -1,
            "Match4WoodcutGainAmount")]
        [TestCase("match5WoodcutGainAmount", 0,
            "Match5WoodcutGainAmount")]
        [TestCase("woodcutTriggerThreshold", 0,
            "WoodcutTriggerThreshold")]
        [TestCase("activeCooldownTurns", -1,
            "ActiveCooldownTurns")]
        [TestCase("activeDurationTurns", 0,
            "ActiveDurationTurns")]
        [TestCase("awakening5ActiveCooldownTurns", -1,
            "Awakening5ActiveCooldownTurns")]
        [TestCase("awakening6FirstWoodcutBonusAmount", 0,
            "Awakening6FirstWoodcutBonusAmount")]
        public void InvalidIntegerIsRejected(
            string propertyName,
            int value,
            string expectedField)
        {
            BrinaAshwoodCombatConfig config =
                BrinaAshwoodTestConfig.Create();
            try
            {
                BrinaAshwoodTestConfig.SetInt(
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

        [TestCase("awakening3PassiveAdditionalCoefficient", 4.99d,
            "Awakening3PassiveAdditionalCoefficient")]
        [TestCase("awakening4Match3Coefficient", 1.29d,
            "Awakening4Match3Coefficient")]
        [TestCase("awakening4Match4Coefficient", 2.39d,
            "Awakening4Match4Coefficient")]
        public void AwakeningEnhancementBelowBaseIsRejected(
            string propertyName,
            double value,
            string expectedField)
        {
            BrinaAshwoodCombatConfig config =
                BrinaAshwoodTestConfig.Create();
            try
            {
                BrinaAshwoodTestConfig.SetDouble(
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
        public void AwakeningCooldownAboveBaseIsRejected()
        {
            BrinaAshwoodCombatConfig config =
                BrinaAshwoodTestConfig.Create();
            try
            {
                BrinaAshwoodTestConfig.SetInt(
                    config,
                    "awakening5ActiveCooldownTurns",
                    9);

                Assert.That(config.TryValidate(out string error), Is.False);
                Assert.That(error,
                    Does.Contain("Awakening5ActiveCooldownTurns"));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}
