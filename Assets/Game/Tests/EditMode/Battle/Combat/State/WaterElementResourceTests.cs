using NUnit.Framework;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.State
{
    public sealed class WaterElementResourceTests
    {
        [Test]
        public void RegisterUsesSharedIdAndMaximumAmount()
        {
            var resources = new ResourceCollection();

            ResourceState water = WaterElementResource.Register(
                resources,
                5);

            Assert.That(water.ResourceId,
                Is.EqualTo(WaterElementResource.Id));
            Assert.That(water.MaxAmount, Is.EqualTo(5));
            Assert.That(water.CurrentAmount, Is.Zero);
        }

        [Test]
        public void AddClampsAtFiveAndReportsExistingOverflowContract()
        {
            var resources = new ResourceCollection();
            ResourceState water = WaterElementResource.Register(
                resources,
                5);

            ResourceAddResult first = water.Add(1);
            ResourceAddResult second = water.Add(3);
            ResourceAddResult third = water.Add(1);
            ResourceAddResult overflow = water.Add(1);

            Assert.That(first.AmountAfter, Is.EqualTo(1));
            Assert.That(second.AmountAfter, Is.EqualTo(4));
            Assert.That(third.AmountAfter, Is.EqualTo(5));
            Assert.That(overflow.RequestedAmount, Is.EqualTo(1));
            Assert.That(overflow.AddedAmount, Is.Zero);
            Assert.That(overflow.OverflowAmount, Is.EqualTo(1));
            Assert.That(overflow.AmountBefore, Is.EqualTo(5));
            Assert.That(overflow.AmountAfter, Is.EqualTo(5));
        }

        [Test]
        public void ConsumeAllClearsWaterWithoutChangingOtherResources()
        {
            var resources = new ResourceCollection();
            ResourceState water = WaterElementResource.Register(
                resources,
                5);
            ResourceState other = resources.Register("fire_charge", 9);
            water.Add(5);
            other.Add(4);

            ResourceConsumeResult result = water.ConsumeAll();

            Assert.That(result.ConsumedAmount, Is.EqualTo(5));
            Assert.That(result.AmountBefore, Is.EqualTo(5));
            Assert.That(result.AmountAfter, Is.Zero);
            Assert.That(water.CurrentAmount, Is.Zero);
            Assert.That(other.CurrentAmount, Is.EqualTo(4));
        }

        [Test]
        public void RegisterUsesInjectedMaximum()
        {
            var resources = new ResourceCollection();

            ResourceState water = WaterElementResource.Register(
                resources,
                7);

            Assert.That(water.MaxAmount, Is.EqualTo(7));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void RegisterRejectsNonPositiveMaximum(int maximum)
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                WaterElementResource.Register(
                    new ResourceCollection(),
                    maximum));
        }
    }
}
