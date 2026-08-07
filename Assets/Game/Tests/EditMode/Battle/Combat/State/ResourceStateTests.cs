using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.State
{
    public sealed class ResourceStateTests
    {
        [Test]
        public void WaterExample_AddOverflowAndConsumeAllAreExact()
        {
            var resource = new ResourceState(
                "resource_water_element",
                5);

            ResourceAddResult first = resource.Add(1);
            resource.Add(3);
            ResourceAddResult overflow = resource.Add(3);

            Assert.That(first.AmountBefore, Is.Zero);
            Assert.That(first.AddedAmount, Is.EqualTo(1));
            Assert.That(overflow.RequestedAmount, Is.EqualTo(3));
            Assert.That(overflow.AddedAmount, Is.EqualTo(1));
            Assert.That(overflow.OverflowAmount, Is.EqualTo(2));
            Assert.That(overflow.AmountBefore, Is.EqualTo(4));
            Assert.That(overflow.AmountAfter, Is.EqualTo(5));

            ResourceConsumeResult consumed = resource.ConsumeAll();
            Assert.That(consumed.ConsumedAmount, Is.EqualTo(5));
            Assert.That(resource.CurrentAmount, Is.Zero);
        }

        [Test]
        public void ConsumeNeverDropsBelowZero()
        {
            var resource = new ResourceState("resource", 5);
            resource.Add(2);

            ResourceConsumeResult result = resource.Consume(5);

            Assert.That(result.RequestedAmount, Is.EqualTo(5));
            Assert.That(result.ConsumedAmount, Is.EqualTo(2));
            Assert.That(result.AmountBefore, Is.EqualTo(2));
            Assert.That(result.AmountAfter, Is.Zero);
        }

        [Test]
        public void CollectionRegistersAndRoutesOperationsById()
        {
            var resources = new ResourceCollection();
            ResourceState registered = resources.Register("b", 3);
            resources.Register("a", 2);

            resources.Add("b", 2);

            Assert.That(resources.Get("b"), Is.SameAs(registered));
            Assert.That(resources.GetAmount("b"), Is.EqualTo(2));
            Assert.That(resources.TryGet("missing", out _), Is.False);
            Assert.Throws<ArgumentException>(() =>
                resources.Register("b", 9));
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(
                () => resources.Get("missing"));
            Assert.That(resources.GetAll()[0].ResourceId, Is.EqualTo("a"));
        }

        [Test]
        public void BossOwnsResourceAndMarkCollections()
        {
            var boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                100,
                10d);

            Assert.That(boss.Resources, Is.Not.Null);
            Assert.That(boss.Marks, Is.Not.Null);
            Assert.That(boss.Resources.Count, Is.Zero);
            Assert.That(boss.Marks.Count, Is.Zero);
        }

        [Test]
        public void AttackStartAmountReadIsAValueSnapshot()
        {
            var resources = new ResourceCollection();
            resources.Register("resource_water_element", 5);
            resources.Add("resource_water_element", 3);
            int plannedConsumption = resources.GetAmount(
                "resource_water_element");

            resources.Add("resource_water_element", 2);

            Assert.That(plannedConsumption, Is.EqualTo(3));
            Assert.That(
                resources.GetAmount("resource_water_element"),
                Is.EqualTo(5));
        }

        [Test]
        public void ConsumptionRecordRequiresOnePositiveActualConsumption()
        {
            var resource = new ResourceState(
                "resource_water_element",
                5);
            resource.Add(5);
            var record = new ResourceConsumptionRecord(
                resource.ConsumeAll(),
                "character_marea",
                "boss");

            Assert.That(record.ConsumedAmount, Is.EqualTo(5));
            Assert.Throws<ArgumentException>(() =>
                new ResourceConsumptionRecord(
                    resource.ConsumeAll(),
                    "consumer",
                    "target"));
        }
    }
}
