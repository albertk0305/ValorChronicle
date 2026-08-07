using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Damage
{
    public sealed class ElementAffinityResolverTests
    {
        [TestCase(ElementType.Fire, ElementType.Grass)]
        [TestCase(ElementType.Water, ElementType.Fire)]
        [TestCase(ElementType.Grass, ElementType.Water)]
        [TestCase(ElementType.Light, ElementType.Dark)]
        [TestCase(ElementType.Dark, ElementType.Light)]
        public void Resolve_ReturnsAdvantageForConfirmedRelationships(
            ElementType attackElement,
            ElementType targetElement)
        {
            Assert.That(
                ElementAffinityResolver.Resolve(
                    attackElement,
                    targetElement),
                Is.EqualTo(1.30d));
        }

        [TestCase(ElementType.Fire, ElementType.Water)]
        [TestCase(ElementType.Water, ElementType.Grass)]
        [TestCase(ElementType.Grass, ElementType.Fire)]
        public void Resolve_ReturnsDisadvantageForConfirmedRelationships(
            ElementType attackElement,
            ElementType targetElement)
        {
            Assert.That(
                ElementAffinityResolver.Resolve(
                    attackElement,
                    targetElement),
                Is.EqualTo(0.70d));
        }

        [TestCase(ElementType.Fire, ElementType.Fire)]
        [TestCase(ElementType.Water, ElementType.Water)]
        [TestCase(ElementType.Grass, ElementType.Grass)]
        [TestCase(ElementType.Light, ElementType.Light)]
        [TestCase(ElementType.Dark, ElementType.Dark)]
        [TestCase(ElementType.Fire, ElementType.Light)]
        [TestCase(ElementType.Dark, ElementType.Grass)]
        public void Resolve_ReturnsNeutralForAllOtherRelationships(
            ElementType attackElement,
            ElementType targetElement)
        {
            Assert.That(
                ElementAffinityResolver.Resolve(
                    attackElement,
                    targetElement),
                Is.EqualTo(1d));
        }

        [Test]
        public void Resolve_RejectsUndefinedElement()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ElementAffinityResolver.Resolve(
                    (ElementType)999,
                    ElementType.Fire));
        }
    }
}
