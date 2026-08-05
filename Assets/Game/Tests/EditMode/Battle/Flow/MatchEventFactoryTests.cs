using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Flow
{
    public sealed class MatchEventFactoryTests
    {
        [Test]
        public void Create_PreservesStepMatchAndPositionOrderWithoutMerging()
        {
            BoardPosition[] firstPositions =
            {
                new BoardPosition(3, 0),
                new BoardPosition(3, 1),
                new BoardPosition(3, 2)
            };
            BoardPosition[] secondPositions =
            {
                new BoardPosition(0, 4),
                new BoardPosition(1, 4),
                new BoardPosition(2, 4),
                new BoardPosition(3, 4)
            };
            BoardPosition[] thirdPositions =
            {
                new BoardPosition(5, 0),
                new BoardPosition(5, 1),
                new BoardPosition(5, 2),
                new BoardPosition(5, 3),
                new BoardPosition(5, 4)
            };
            BoardCascadeResult cascade =
                BattleFlowTestSupport.CreateCascade(
                    new[]
                    {
                        BattleFlowTestSupport.Match(
                            ElementType.Fire,
                            firstPositions),
                        BattleFlowTestSupport.Match(
                            ElementType.Fire,
                            secondPositions)
                    },
                    new[]
                    {
                        BattleFlowTestSupport.Match(
                            ElementType.Grass,
                            thirdPositions)
                    });

            IReadOnlyList<MatchEvent> events =
                MatchEventFactory.Create(cascade);

            Assert.That(events, Has.Count.EqualTo(3));
            AssertEvent(
                events[0],
                0,
                0,
                0,
                ElementType.Fire,
                BoardMatchTier.Three,
                firstPositions);
            AssertEvent(
                events[1],
                1,
                0,
                1,
                ElementType.Fire,
                BoardMatchTier.Four,
                secondPositions);
            AssertEvent(
                events[2],
                2,
                1,
                0,
                ElementType.Grass,
                BoardMatchTier.FiveOrMore,
                thirdPositions);
        }

        [Test]
        public void Create_ReturnsReadOnlyEventAndPositionCollections()
        {
            BoardCascadeResult cascade =
                BattleFlowTestSupport.CreateCascade(
                    new[]
                    {
                        BattleFlowTestSupport.Match(
                            ElementType.Water,
                            new BoardPosition(0, 0),
                            new BoardPosition(0, 1),
                            new BoardPosition(0, 2))
                    });

            IReadOnlyList<MatchEvent> events =
                MatchEventFactory.Create(cascade);
            var mutableEvents = events as IList<MatchEvent>;
            var mutablePositions = events[0].Positions
                as IList<BoardPosition>;

            Assert.That(mutableEvents, Is.Not.Null);
            Assert.That(mutablePositions, Is.Not.Null);
            Assert.Throws<NotSupportedException>(
                () => mutableEvents.Add(events[0]));
            Assert.Throws<NotSupportedException>(
                () => mutablePositions.Add(new BoardPosition(1, 1)));
        }

        [Test]
        public void Create_RejectsStepRemovalCountMismatch()
        {
            BoardCascadeResult cascade =
                BattleFlowTestSupport.CreateCascadeWithRemovalCount(
                    2,
                    BattleFlowTestSupport.Match(
                        ElementType.Dark,
                        new BoardPosition(0, 0),
                        new BoardPosition(1, 0),
                        new BoardPosition(2, 0)));

            InvalidOperationException exception = Assert.Throws<
                InvalidOperationException>(
                () => MatchEventFactory.Create(cascade));

            Assert.That(exception.Message,
                Does.Contain("CascadeStep[0]"));
            Assert.That(exception.Message,
                Does.Contain("3").And.Contain("2"));
        }

        [Test]
        public void Create_NullCascadeThrows()
        {
            Assert.Throws<ArgumentNullException>(
                () => MatchEventFactory.Create(null));
        }

        private static void AssertEvent(
            MatchEvent matchEvent,
            int sequenceIndex,
            int stepIndex,
            int matchIndex,
            ElementType element,
            BoardMatchTier tier,
            IReadOnlyList<BoardPosition> positions)
        {
            Assert.That(matchEvent.SequenceIndex, Is.EqualTo(sequenceIndex));
            Assert.That(matchEvent.CascadeStepIndex, Is.EqualTo(stepIndex));
            Assert.That(matchEvent.MatchIndex, Is.EqualTo(matchIndex));
            Assert.That(matchEvent.Element, Is.EqualTo(element));
            Assert.That(matchEvent.Tier, Is.EqualTo(tier));
            Assert.That(matchEvent.Origin, Is.EqualTo(positions[0]));
            Assert.That(matchEvent.Positions, Is.EqualTo(positions));
            Assert.That(
                matchEvent.RemovedBlockCount,
                Is.EqualTo(positions.Count));
        }
    }
}
