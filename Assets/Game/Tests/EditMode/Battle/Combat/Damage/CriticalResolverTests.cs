using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Core.Random;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Damage
{
    public sealed class CriticalResolverTests
    {
        [Test]
        public void ZeroChanceNeverCriticalWithoutSampling()
        {
            var random = new SequenceRandomSource(0f);

            Assert.That(
                CriticalResolver.Resolve(true, 0d, random),
                Is.False);
            Assert.That(random.NextFloatCallCount, Is.Zero);
        }

        [Test]
        public void FullChanceAlwaysCriticalWithoutSampling()
        {
            var random = new SequenceRandomSource(0.999f);

            Assert.That(
                CriticalResolver.Resolve(true, 1d, random),
                Is.True);
            Assert.That(random.NextFloatCallCount, Is.Zero);
        }

        [Test]
        public void DisabledCriticalOverridesFullChance()
        {
            var random = new SequenceRandomSource(0f);

            Assert.That(
                CriticalResolver.Resolve(false, 1d, random),
                Is.False);
            Assert.That(random.NextFloatCallCount, Is.Zero);
        }

        [Test]
        public void SampleBelowChanceSucceedsAndBoundaryFails()
        {
            var random = new SequenceRandomSource(0.099f, 0.1f);

            Assert.That(
                CriticalResolver.Resolve(true, 0.1d, random),
                Is.True);
            Assert.That(
                CriticalResolver.Resolve(true, 0.1d, random),
                Is.False);
            Assert.That(random.NextFloatCallCount, Is.EqualTo(2));
        }

        private sealed class SequenceRandomSource : IRandomSource
        {
            private readonly Queue<float> values;

            public SequenceRandomSource(params float[] values)
            {
                this.values = new Queue<float>(values);
            }

            public int NextFloatCallCount { get; private set; }

            public int Next(int minInclusive, int maxExclusive)
            {
                throw new NotSupportedException();
            }

            public float NextFloat()
            {
                NextFloatCallCount++;
                return values.Dequeue();
            }
        }
    }
}
