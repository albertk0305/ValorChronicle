using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Damage;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Damage
{
    public sealed class ComboMultiplierResolverTests
    {
        [TestCase(1, 1.00d)]
        [TestCase(2, 1.05d)]
        [TestCase(3, 1.12d)]
        [TestCase(4, 1.21d)]
        [TestCase(5, 1.32d)]
        [TestCase(6, 1.44d)]
        [TestCase(7, 1.57d)]
        [TestCase(8, 1.70d)]
        [TestCase(9, 1.85d)]
        [TestCase(10, 2.00d)]
        public void Resolve_ReturnsConfirmedMultiplier(
            int comboCount,
            double expected)
        {
            Assert.That(
                ComboMultiplierResolver.Resolve(comboCount),
                Is.EqualTo(expected).Within(0.000000001d));
        }

        [TestCase(11)]
        [TestCase(100)]
        [TestCase(int.MaxValue)]
        public void Resolve_AboveTenUsesMaximumMultiplier(int comboCount)
        {
            Assert.That(
                ComboMultiplierResolver.Resolve(comboCount),
                Is.EqualTo(2d));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Resolve_RejectsNonPositiveCount(int comboCount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => ComboMultiplierResolver.Resolve(comboCount));
        }
    }
}
