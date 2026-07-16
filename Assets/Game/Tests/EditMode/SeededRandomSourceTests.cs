using System;
using NUnit.Framework;
using ValorChronicle.Core.Random;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class SeededRandomSourceTests
    {
        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            var first = new SeededRandomSource(12345);
            var second = new SeededRandomSource(12345);

            for (int i = 0; i < 20; i++)
            {
                Assert.That(first.Next(-10, 20), Is.EqualTo(second.Next(-10, 20)));
                Assert.That(first.NextFloat(), Is.EqualTo(second.NextFloat()));
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentSequences()
        {
            var first = new SeededRandomSource(100);
            var second = new SeededRandomSource(200);
            var firstSequence = new int[10];
            var secondSequence = new int[10];

            for (int i = 0; i < firstSequence.Length; i++)
            {
                firstSequence[i] = first.Next(0, 100000);
                secondSequence[i] = second.Next(0, 100000);
            }

            Assert.That(firstSequence, Is.Not.EqualTo(secondSequence));
        }

        [Test]
        public void Values_StayWithinDocumentedRanges()
        {
            var random = new SeededRandomSource(9876);

            for (int i = 0; i < 1000; i++)
            {
                Assert.That(random.Next(-3, 7), Is.InRange(-3, 6));
                Assert.That(random.NextFloat(), Is.InRange(0f, 0.99999994f));
            }
        }

        [TestCase(1, 1)]
        [TestCase(2, 1)]
        public void Next_RejectsInvalidRange(int minInclusive, int maxExclusive)
        {
            var random = new SeededRandomSource(1);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => random.Next(minInclusive, maxExclusive));
        }
    }
}
