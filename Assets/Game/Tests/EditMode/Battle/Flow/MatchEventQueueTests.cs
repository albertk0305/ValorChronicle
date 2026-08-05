using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Flow
{
    public sealed class MatchEventQueueTests
    {
        [Test]
        public void EnqueueRangeAndTryDequeue_PreserveFifoOrder()
        {
            IReadOnlyList<MatchEvent> events = CreateEvents();
            var queue = new MatchEventQueue();

            queue.EnqueueRange(events);

            Assert.That(queue.Count, Is.EqualTo(2));
            Assert.That(queue.TryDequeue(out MatchEvent first), Is.True);
            Assert.That(first, Is.SameAs(events[0]));
            Assert.That(queue.TryDequeue(out MatchEvent second), Is.True);
            Assert.That(second, Is.SameAs(events[1]));
            Assert.That(queue.TryDequeue(out MatchEvent missing), Is.False);
            Assert.That(missing, Is.Null);
        }

        [Test]
        public void Clear_RemovesAllPendingEvents()
        {
            var queue = new MatchEventQueue();
            queue.EnqueueRange(CreateEvents());

            queue.Clear();

            Assert.That(queue.Count, Is.Zero);
            Assert.That(queue.TryDequeue(out _), Is.False);
        }

        [Test]
        public void EnqueueRange_RejectsNullWithoutChangingQueue()
        {
            var queue = new MatchEventQueue();

            Assert.Throws<ArgumentNullException>(
                () => queue.EnqueueRange(null));
            Assert.Throws<ArgumentException>(
                () => queue.EnqueueRange(
                    new MatchEvent[] { null }));
            Assert.That(queue.Count, Is.Zero);
        }

        private static IReadOnlyList<MatchEvent> CreateEvents()
        {
            BoardCascadeResult cascade =
                BattleFlowTestSupport.CreateCascade(
                    new[]
                    {
                        BattleFlowTestSupport.Match(
                            ElementType.Light,
                            new BoardPosition(0, 0),
                            new BoardPosition(1, 0),
                            new BoardPosition(2, 0)),
                        BattleFlowTestSupport.Match(
                            ElementType.Dark,
                            new BoardPosition(3, 1),
                            new BoardPosition(4, 1),
                            new BoardPosition(5, 1))
                    });
            return MatchEventFactory.Create(cascade);
        }
    }
}
