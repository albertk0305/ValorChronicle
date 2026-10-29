using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Characters.Naira;

namespace ValorChronicle.Tests.EditMode.Characters.Naira
{
    public sealed class NairaWindhornCombatConfigTests
    {
        [Test]
        public void CanonicalConfigContainsConfirmedValues()
        {
            NairaWindhornCombatConfig config =
                NairaWindhornTestConfig.Canonical;

            Assert.That(config, Is.Not.Null);
            Assert.That(config.Match3ShieldMaxHpRate, Is.EqualTo(0.07d));
            Assert.That(config.Match4ShieldMaxHpRate, Is.EqualTo(0.12d));
            Assert.That(config.Match5ShieldMaxHpRate, Is.EqualTo(0.20d));
            Assert.That(config.ActiveShieldMaxHpRate, Is.EqualTo(0.35d));
            Assert.That(config.ShieldDurationTurns, Is.EqualTo(3));
            Assert.That(config.MatchShieldUsesComboMultiplier, Is.True);
            Assert.That(config.ActiveShieldUsesComboMultiplier, Is.False);
            Assert.That(config.Match4BaseDamageCoefficient,
                Is.EqualTo(1.20d));
            Assert.That(config.Match4ChargeDamageIncreaseRate,
                Is.EqualTo(0.50d));
            Assert.That(config.Match5BaseDamageCoefficient,
                Is.EqualTo(2.20d));
            Assert.That(config.Match5ChargeDamageIncreaseRate,
                Is.EqualTo(1.00d));
            Assert.That(config.PassiveTargetTakenDamageIncreaseRate,
                Is.EqualTo(0.05d));
            Assert.That(
                config.Awakening3PassiveTargetTakenDamageIncreaseRate,
                Is.EqualTo(0.06d));
            Assert.That(config.ActiveCooldownTurns, Is.EqualTo(12));
            Assert.That(config.WhiteStagGraceDamageReductionRate,
                Is.EqualTo(0.35d));
            Assert.That(
                config.Awakening5WhiteStagGraceDamageReductionRate,
                Is.EqualTo(0.40d));
            Assert.That(config.Awakening4Match3ShieldMaxHpRate,
                Is.EqualTo(0.08d));
            Assert.That(config.Awakening4Match4ShieldMaxHpRate,
                Is.EqualTo(0.13d));
            Assert.That(config.AncientGuardianDamageReductionRate,
                Is.EqualTo(0.15d));
            Assert.That(config.TryValidate(out _), Is.True);
        }

        [TestCase("match3ShieldMaxHpRate", -0.01d,
            "Match3ShieldMaxHpRate")]
        [TestCase("match4ShieldMaxHpRate", double.NaN,
            "Match4ShieldMaxHpRate")]
        [TestCase("activeShieldMaxHpRate", 1.01d,
            "ActiveShieldMaxHpRate")]
        [TestCase("match4BaseDamageCoefficient", 0d,
            "Match4BaseDamageCoefficient")]
        [TestCase("match4ChargeDamageIncreaseRate",
            double.PositiveInfinity,
            "Match4ChargeDamageIncreaseRate")]
        [TestCase("match5BaseDamageCoefficient", -0.01d,
            "Match5BaseDamageCoefficient")]
        [TestCase("passiveTargetTakenDamageIncreaseRate", 1.01d,
            "PassiveTargetTakenDamageIncreaseRate")]
        [TestCase("whiteStagGraceDamageReductionRate", -0.01d,
            "WhiteStagGraceDamageReductionRate")]
        [TestCase("ancientGuardianDamageReductionRate", double.NaN,
            "AncientGuardianDamageReductionRate")]
        public void InvalidDoubleIsRejected(
            string propertyName,
            double value,
            string expectedField)
        {
            NairaWindhornCombatConfig config =
                NairaWindhornTestConfig.Create();
            try
            {
                NairaWindhornTestConfig.SetDouble(
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

        [TestCase("shieldDurationTurns", 2, "ShieldDurationTurns")]
        [TestCase("shieldDurationTurns", 4, "ShieldDurationTurns")]
        [TestCase("activeCooldownTurns", 11, "ActiveCooldownTurns")]
        [TestCase("activeCooldownTurns", 13, "ActiveCooldownTurns")]
        public void ContractIntegerWithWrongValueIsRejected(
            string propertyName,
            int value,
            string expectedField)
        {
            NairaWindhornCombatConfig config =
                NairaWindhornTestConfig.Create();
            try
            {
                NairaWindhornTestConfig.SetInt(config, propertyName, value);

                Assert.That(config.TryValidate(out string error), Is.False);
                Assert.That(error, Does.Contain(expectedField));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase("matchShieldUsesComboMultiplier", false,
            "MatchShieldUsesComboMultiplier")]
        [TestCase("activeShieldUsesComboMultiplier", true,
            "ActiveShieldUsesComboMultiplier")]
        public void WrongComboContractIsRejected(
            string propertyName,
            bool value,
            string expectedField)
        {
            NairaWindhornCombatConfig config =
                NairaWindhornTestConfig.Create();
            try
            {
                NairaWindhornTestConfig.SetBool(config, propertyName, value);

                Assert.That(config.TryValidate(out string error), Is.False);
                Assert.That(error, Does.Contain(expectedField));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void EnhancementBelowBaseIsRejected()
        {
            NairaWindhornCombatConfig config =
                NairaWindhornTestConfig.Create();
            try
            {
                NairaWindhornTestConfig.SetDouble(
                    config,
                    "awakening4Match3ShieldMaxHpRate",
                    0.06d);

                Assert.That(config.TryValidate(out string error), Is.False);
                Assert.That(error,
                    Does.Contain("Awakening4Match3ShieldMaxHpRate"));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}

