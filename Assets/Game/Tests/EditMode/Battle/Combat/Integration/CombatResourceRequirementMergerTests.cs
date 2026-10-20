using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Integration;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Integration
{
    public sealed class CombatResourceRequirementMergerTests
    {
        [Test]
        public void MergeDeduplicatesEqualRequirementsAndSortsById()
        {
            var water = new CombatResourceRequirement("water", 5);

            var merged = CombatResourceRequirementMerger.Merge(
                new[]
                {
                    water,
                    new CombatResourceRequirement("fire", 3),
                    new CombatResourceRequirement("water", 5)
                });

            Assert.That(merged, Has.Count.EqualTo(2));
            Assert.That(merged[0].ResourceId, Is.EqualTo("fire"));
            Assert.That(merged[0].MaxAmount, Is.EqualTo(3));
            Assert.That(merged[1].ResourceId, Is.EqualTo("water"));
            Assert.That(merged[1].MaxAmount, Is.EqualTo(5));
        }

        [Test]
        public void MergeIsIndependentOfRequirementOrder()
        {
            var first = CombatResourceRequirementMerger.Merge(
                new[]
                {
                    new CombatResourceRequirement("water", 5),
                    new CombatResourceRequirement("fire", 3)
                });
            var second = CombatResourceRequirementMerger.Merge(
                new[]
                {
                    new CombatResourceRequirement("fire", 3),
                    new CombatResourceRequirement("water", 5)
                });

            Assert.That(second[0].ResourceId,
                Is.EqualTo(first[0].ResourceId));
            Assert.That(second[0].MaxAmount,
                Is.EqualTo(first[0].MaxAmount));
            Assert.That(second[1].ResourceId,
                Is.EqualTo(first[1].ResourceId));
            Assert.That(second[1].MaxAmount,
                Is.EqualTo(first[1].MaxAmount));
        }

        [Test]
        public void MergeRejectsConflictingMaximums()
        {
            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(() =>
                    CombatResourceRequirementMerger.Merge(
                        new[]
                        {
                            new CombatResourceRequirement("water", 5),
                            new CombatResourceRequirement("water", 7)
                        }));

            Assert.That(exception.Message, Does.Contain("water"));
            Assert.That(exception.Message, Does.Contain("5"));
            Assert.That(exception.Message, Does.Contain("7"));
        }

        [Test]
        public void MergeAcceptsEmptyAndRejectsInvalidCollections()
        {
            Assert.That(
                CombatResourceRequirementMerger.Merge(
                    Array.Empty<CombatResourceRequirement>()),
                Is.Empty);
            Assert.Throws<ArgumentNullException>(() =>
                CombatResourceRequirementMerger.Merge(null));
            Assert.Throws<ArgumentException>(() =>
                CombatResourceRequirementMerger.Merge(
                    new CombatResourceRequirement[] { null }));
        }
    }
}
