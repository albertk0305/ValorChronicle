using NUnit.Framework;
using ValorChronicle.Battle.Flow.Presentation;

namespace ValorChronicle.Tests.EditMode.Battle.Flow.Presentation
{
    public sealed class BattleTimeScalePolicyTests
    {
        [TestCase(1, 0.5f)]
        [TestCase(2, 1f)]
        [TestCase(3, 1.5f)]
        [TestCase(0, 0.5f)]
        [TestCase(4, 0.5f)]
        public void Resolve_MapsSupportedSpeedsAndFallsBackToOneTimes(
            int speed,
            float expected)
        {
            Assert.That(BattleTimeScalePolicy.Resolve(speed),
                Is.EqualTo(expected));
        }
    }
}
