using System;
using NUnit.Framework;
using ValorChronicle.Battle.Results;

namespace ValorChronicle.Tests.EditMode.Battle.Results
{
    public sealed class BattleDamageScoreAccumulatorTests
    {
        [Test]
        public void Add_AccumulatesAppliedDamage()
        {
            var accumulator = new BattleDamageScoreAccumulator();

            accumulator.Add(120L);
            accumulator.Add(0L);
            accumulator.Add(380L);

            Assert.That(accumulator.DamageScore, Is.EqualTo(500L));
        }

        [Test]
        public void Add_RejectsNegativeAppliedDamage()
        {
            var accumulator = new BattleDamageScoreAccumulator();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => accumulator.Add(-1L));
            Assert.That(accumulator.DamageScore, Is.Zero);
        }

        [Test]
        public void Add_ThrowsWhenDamageScoreOverflows()
        {
            var accumulator = new BattleDamageScoreAccumulator();
            accumulator.Add(long.MaxValue);

            Assert.Throws<OverflowException>(() => accumulator.Add(1L));
            Assert.That(accumulator.DamageScore, Is.EqualTo(long.MaxValue));
        }
    }
}
