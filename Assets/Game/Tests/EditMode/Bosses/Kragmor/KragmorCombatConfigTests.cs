using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Bosses.Kragmor
{
    public sealed class KragmorCombatConfigTests
    {
        [Test]
        public void CanonicalConfigContainsMigratedValues()
        {
            var config = KragmorTestConfig.Canonical;

            Assert.That(config, Is.Not.Null);
            Assert.That(config.ColossusIronFistCoefficient,
                Is.EqualTo(0.75d));
            Assert.That(config.RockshardEruptionCoefficient,
                Is.EqualTo(0.65d));
            Assert.That(config.RockshardRockCreationCount, Is.EqualTo(3));
            Assert.That(config.MaximumRockCount, Is.EqualTo(6));
            Assert.That(config.EarthCollapseCoefficient,
                Is.EqualTo(2.40d));
            Assert.That(config.VolcanicCarapaceReductionRate,
                Is.EqualTo(0.10d));
            Assert.That(config.CoreCompressionReductionRate,
                Is.EqualTo(0.20d));
            Assert.That(config.CoreExposureIncreaseRate,
                Is.EqualTo(0.30d));
            Assert.That(config.TryValidate(out _), Is.True);
        }

        [Test]
        public void BossDefinitionReferencesCanonicalConfig()
        {
            BossDefinition boss = AssetDatabase.LoadAssetAtPath<
                BossDefinition>(
                "Assets/Data/Bosses/boss_kragmor.asset");

            Assert.That(boss, Is.Not.Null);
            Assert.That(boss.CombatConfig,
                Is.SameAs(KragmorTestConfig.Canonical));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(-0.01d)]
        public void InvalidDoubleIsRejected(double value)
        {
            var config = KragmorTestConfig.Create(
                colossusIronFistCoefficient: value);

            Assert.That(config.TryValidate(out string error), Is.False);
            Assert.That(error, Does.Contain("ColossusIronFistCoefficient"));
        }

        [TestCase(-1, 6, "RockshardRockCreationCount")]
        [TestCase(3, -1, "MaximumRockCount")]
        public void NegativeRockCountIsRejected(
            int requested,
            int maximum,
            string expectedField)
        {
            var config = KragmorTestConfig.Create(
                rockshardRockCreationCount: requested,
                maximumRockCount: maximum);

            Assert.That(config.TryValidate(out string error), Is.False);
            Assert.That(error, Does.Contain(expectedField));
        }

        [Test]
        public void RequestedCountAboveMaximumRemainsValidForRuntimeClamp()
        {
            var config = KragmorTestConfig.Create(
                rockshardRockCreationCount: 7,
                maximumRockCount: 4);

            Assert.That(config.TryValidate(out _), Is.True);
        }

        [Test]
        public void ModifiersAboveOneRemainValid()
        {
            var config = KragmorTestConfig.Create(
                volcanicCarapaceReductionRate: 1.2d,
                coreCompressionReductionRate: 1.3d,
                coreExposureIncreaseRate: 1.4d);

            Assert.That(config.TryValidate(out _), Is.True);
        }
    }
}
