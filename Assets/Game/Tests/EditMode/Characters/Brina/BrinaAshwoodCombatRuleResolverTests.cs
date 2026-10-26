using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Characters.Brina;

namespace ValorChronicle.Tests.EditMode.Characters.Brina
{
    public sealed class BrinaAshwoodCombatRuleResolverTests
    {
        [TestCase(0, 1.30d, 2.40d, 5.00d, 8, 0)]
        [TestCase(1, 1.30d, 2.40d, 5.00d, 8, 0)]
        [TestCase(2, 1.30d, 2.40d, 5.00d, 8, 0)]
        [TestCase(3, 1.30d, 2.40d, 5.25d, 8, 0)]
        [TestCase(4, 1.35d, 2.50d, 5.25d, 8, 0)]
        [TestCase(5, 1.35d, 2.50d, 5.25d, 7, 0)]
        [TestCase(6, 1.35d, 2.50d, 5.25d, 7, 1)]
        public void ResolveAppliesEachAwakeningOnce(
            int awakening,
            double expectedMatch3,
            double expectedMatch4,
            double expectedAdditional,
            int expectedCooldown,
            int expectedFirstWoodcutBonus)
        {
            BrinaAshwoodCombatConfig config =
                BrinaAshwoodTestConfig.Create();
            try
            {
                BrinaAshwoodResolvedCombatRules result =
                    BrinaAshwoodCombatRuleResolver.Resolve(
                        config,
                        awakening);

                Assert.That(result.Match3Coefficient,
                    Is.EqualTo(expectedMatch3));
                Assert.That(result.Match4Coefficient,
                    Is.EqualTo(expectedMatch4));
                Assert.That(result.Match5Coefficient, Is.EqualTo(4.00d));
                Assert.That(result.Match3WoodcutGainAmount, Is.EqualTo(1));
                Assert.That(result.Match4WoodcutGainAmount, Is.EqualTo(2));
                Assert.That(result.Match5WoodcutGainAmount, Is.EqualTo(3));
                Assert.That(result.WoodcutTriggerThreshold,
                    Is.EqualTo(10));
                Assert.That(result.PassiveAdditionalCoefficient,
                    Is.EqualTo(expectedAdditional));
                Assert.That(result.ActiveWoodcutGainMultiplier,
                    Is.EqualTo(2.00d));
                Assert.That(result.ActiveCooldownTurns,
                    Is.EqualTo(expectedCooldown));
                Assert.That(result.ActiveDurationTurns, Is.EqualTo(1));
                Assert.That(result.FirstWoodcutBonusAmount,
                    Is.EqualTo(expectedFirstWoodcutBonus));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [TestCase(-1)]
        [TestCase(7)]
        public void ResolveRejectsAwakeningOutsideSupportedRange(
            int awakening)
        {
            BrinaAshwoodCombatConfig config =
                BrinaAshwoodTestConfig.Create();
            try
            {
                Assert.That(
                    () => BrinaAshwoodCombatRuleResolver.Resolve(
                        config,
                        awakening),
                    Throws.TypeOf<System.ArgumentOutOfRangeException>()
                        .With.Property("ParamName").EqualTo("awakening"));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ResolveRejectsInvalidConfig()
        {
            BrinaAshwoodCombatConfig config =
                BrinaAshwoodTestConfig.Create();
            try
            {
                BrinaAshwoodTestConfig.SetInt(
                    config,
                    "woodcutTriggerThreshold",
                    0);

                Assert.That(
                    () => BrinaAshwoodCombatRuleResolver.Resolve(config, 0),
                    Throws.ArgumentException
                        .With.Property("ParamName").EqualTo("config"));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ResolveRejectsMissingConfig()
        {
            Assert.That(
                () => BrinaAshwoodCombatRuleResolver.Resolve(null, 0),
                Throws.ArgumentNullException
                    .With.Property("ParamName").EqualTo("config"));
        }
    }
}
