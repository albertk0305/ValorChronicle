using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board
{
    public sealed class BoardCascadeStepResolverTests
    {
        [Test]
        public void TryResolve_NoMatchesReturnsFalseWithoutConsumingDependencies()
        {
            BoardState board = CreateBoardWithoutMatches();
            BoardBlock[] originalContents = CaptureBoardContents(board);
            var random = new RecordingRandomSource(Array.Empty<int>());
            var idGenerator = new BoardBlockIdGenerator(100);
            var resolver = CreateResolver(random, idGenerator);

            bool resolved = resolver.TryResolve(board, out BoardCascadeStep step);

            Assert.That(resolved, Is.False);
            Assert.That(step, Is.Null);
            Assert.That(random.CallCount, Is.Zero);
            Assert.That(idGenerator.Next(), Is.EqualTo(100));
            AssertBoardContainsReferences(board, originalContents);
        }

        [Test]
        public void TryResolve_HorizontalThreeMatchReturnsExactIntegratedResults()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 2, ElementType.Water);
            BoardBlock[] originalContents = CaptureBoardContents(board);
            var random = new RecordingRandomSource(
                CreateRepeatedSequence(3, 0));

            bool resolved = CreateResolver(random, 31).TryResolve(
                board,
                out BoardCascadeStep step);

            Assert.That(resolved, Is.True);
            Assert.That(step.MatchEventCount, Is.EqualTo(1));
            Assert.That(step.Matches[0].BlockCount, Is.EqualTo(3));
            Assert.That(step.RemovedBlockCount, Is.EqualTo(3));
            Assert.That(step.Collapse.Moves, Has.Count.EqualTo(6));
            Assert.That(step.Refill.Spawns, Has.Count.EqualTo(3));
            Assert.That(step.Board, Is.SameAs(step.Refill.Board));
            AssertBoardIsFull(step.Board);
            AssertBoardContainsReferences(board, originalContents);
        }

        [Test]
        public void TryResolve_VerticalFourMatchReturnsExpectedMoveAndSpawnCounts()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetVertical(board, 2, 0, 3, ElementType.Grass);
            var random = new RecordingRandomSource(
                CreateRepeatedSequence(4, 1));

            bool resolved = CreateResolver(random, 31).TryResolve(
                board,
                out BoardCascadeStep step);

            Assert.That(resolved, Is.True);
            Assert.That(step.Matches, Has.Count.EqualTo(1));
            Assert.That(step.Matches[0].Tier, Is.EqualTo(BoardMatchTier.Four));
            Assert.That(step.Collapse.Moves, Has.Count.EqualTo(1));
            Assert.That(step.Refill.Spawns, Has.Count.EqualTo(4));
            Assert.That(step.RemovedBlockCount, Is.EqualTo(4));
            AssertBoardIsFull(step.Board);
        }

        [Test]
        public void TryResolve_FiveMatchUsesFiveOrMoreTier()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 4, 1, ElementType.Dark);
            var random = new RecordingRandomSource(
                CreateRepeatedSequence(5, 2));

            bool resolved = CreateResolver(random, 31).TryResolve(
                board,
                out BoardCascadeStep step);

            Assert.That(resolved, Is.True);
            Assert.That(step.Matches, Has.Count.EqualTo(1));
            Assert.That(
                step.Matches[0].Tier,
                Is.EqualTo(BoardMatchTier.FiveOrMore));
            Assert.That(step.RemovedBlockCount, Is.EqualTo(5));
            Assert.That(step.Refill.Spawns, Has.Count.EqualTo(5));
        }

        [Test]
        public void TryResolve_TShapeRemainsOneMatchEvent()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 1, 3, 3, ElementType.Fire);
            SetVertical(board, 2, 1, 3, ElementType.Fire);
            var random = new RecordingRandomSource(
                CreateRepeatedSequence(5, 3));

            bool resolved = CreateResolver(random, 31).TryResolve(
                board,
                out BoardCascadeStep step);

            Assert.That(resolved, Is.True);
            Assert.That(step.MatchEventCount, Is.EqualTo(1));
            Assert.That(step.Matches[0].BlockCount, Is.EqualTo(5));
            Assert.That(step.RemovedBlockCount, Is.EqualTo(5));
            Assert.That(step.Refill.Spawns, Has.Count.EqualTo(5));
        }

        [Test]
        public void TryResolve_LShapeRemainsOneMatchEvent()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 1, 3, 1, ElementType.Dark);
            SetVertical(board, 1, 1, 3, ElementType.Dark);
            var random = new RecordingRandomSource(
                CreateRepeatedSequence(5, 4));

            bool resolved = CreateResolver(random, 31).TryResolve(
                board,
                out BoardCascadeStep step);

            Assert.That(resolved, Is.True);
            Assert.That(step.MatchEventCount, Is.EqualTo(1));
            Assert.That(step.Matches[0].BlockCount, Is.EqualTo(5));
            Assert.That(step.RemovedBlockCount, Is.EqualTo(5));
        }

        [Test]
        public void TryResolve_CrossShapeRemainsOneMatchEvent()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 1, 3, 2, ElementType.Light);
            SetVertical(board, 2, 1, 3, ElementType.Light);
            var random = new RecordingRandomSource(
                CreateRepeatedSequence(5, 0));

            bool resolved = CreateResolver(random, 31).TryResolve(
                board,
                out BoardCascadeStep step);

            Assert.That(resolved, Is.True);
            Assert.That(step.MatchEventCount, Is.EqualTo(1));
            Assert.That(step.Matches[0].BlockCount, Is.EqualTo(5));
            Assert.That(step.RemovedBlockCount, Is.EqualTo(5));
        }

        [Test]
        public void TryResolve_SeparatedSameElementGroupsRemainSeparateAndOrdered()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 0, ElementType.Dark);
            SetHorizontal(board, 3, 5, 4, ElementType.Dark);
            IReadOnlyList<BoardMatch> expectedMatches =
                BoardMatchFinder.FindMatches(board);
            Assert.That(expectedMatches, Has.Count.EqualTo(2));
            var random = new RecordingRandomSource(
                CreateRepeatedSequence(6, 0));
            var resolver = new BoardCascadeStepResolver(
                candidate => expectedMatches,
                new BoardRefiller(
                    random,
                    new BoardBlockIdGenerator(31)));

            bool resolved = resolver.TryResolve(
                board,
                out BoardCascadeStep step);

            Assert.That(resolved, Is.True);
            Assert.That(step.MatchEventCount, Is.EqualTo(2));
            Assert.That(step.Matches[0], Is.SameAs(expectedMatches[0]));
            Assert.That(step.Matches[1], Is.SameAs(expectedMatches[1]));
            Assert.That(step.Matches[0].Origin, Is.EqualTo(new BoardPosition(0, 0)));
            Assert.That(step.Matches[1].Origin, Is.EqualTo(new BoardPosition(3, 4)));
            Assert.That(step.RemovedBlockCount, Is.EqualTo(6));
            Assert.That(step.Refill.Spawns, Has.Count.EqualTo(6));
        }

        [Test]
        public void TryResolve_DifferentElementGroupsAcrossColumnsResolveTogether()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 1, ElementType.Fire);
            SetVertical(board, 4, 2, 4, ElementType.Grass);
            IReadOnlyList<BoardMatch> expectedMatches =
                BoardMatchFinder.FindMatches(board);
            Assert.That(expectedMatches, Has.Count.EqualTo(2));
            var random = new RecordingRandomSource(
                CreateRepeatedSequence(6, 1));

            bool resolved = CreateResolver(random, 31).TryResolve(
                board,
                out BoardCascadeStep step);

            Assert.That(resolved, Is.True);
            Assert.That(step.MatchEventCount, Is.EqualTo(2));
            Assert.That(step.Matches[0].Element, Is.EqualTo(ElementType.Fire));
            Assert.That(step.Matches[1].Element, Is.EqualTo(ElementType.Grass));
            Assert.That(step.Collapse.Removals, Has.Count.EqualTo(6));
            Assert.That(step.Refill.Spawns, Has.Count.EqualTo(6));
            Assert.That(random.CallCount, Is.EqualTo(6));
            AssertBoardIsFull(step.Board);
        }

        [Test]
        public void TryResolve_RemovalsFollowMatchThenPositionOrder()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 0, ElementType.Dark);
            SetHorizontal(board, 3, 5, 4, ElementType.Dark);
            var random = new RecordingRandomSource(
                CreateRepeatedSequence(6, 2));

            CreateResolver(random, 31).TryResolve(
                board,
                out BoardCascadeStep step);

            int removalIndex = 0;
            foreach (BoardMatch match in step.Matches)
            {
                foreach (BoardPosition position in match.Positions)
                {
                    Assert.That(
                        step.Collapse.Removals[removalIndex].Position,
                        Is.EqualTo(position));
                    removalIndex++;
                }
            }

            Assert.That(removalIndex, Is.EqualTo(step.RemovedBlockCount));
        }

        [Test]
        public void TryResolve_EveryMatchedPositionIsRemovedExactlyOnce()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 1, ElementType.Fire);
            SetVertical(board, 4, 2, 4, ElementType.Grass);
            var random = new RecordingRandomSource(
                CreateRepeatedSequence(6, 3));

            CreateResolver(random, 31).TryResolve(
                board,
                out BoardCascadeStep step);
            var removedPositions = new HashSet<BoardPosition>();

            foreach (BoardBlockRemoval removal in step.Collapse.Removals)
            {
                Assert.That(removedPositions.Add(removal.Position), Is.True);
            }

            Assert.That(removedPositions, Has.Count.EqualTo(6));
        }

        [Test]
        public void TryResolve_UnmatchedBlockKeepsIdentityAndRuntimeData()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 2, ElementType.Water);
            var unaffectedPosition = new BoardPosition(5, 1);
            BoardBlock unaffected = board.Get(unaffectedPosition);
            var random = new RecordingRandomSource(
                CreateRepeatedSequence(3, 4));

            CreateResolver(random, 31).TryResolve(
                board,
                out BoardCascadeStep step);
            BoardBlock resultBlock = step.Board.Get(unaffectedPosition);

            Assert.That(resultBlock, Is.SameAs(unaffected));
            Assert.That(resultBlock.RuntimeId, Is.EqualTo(unaffected.RuntimeId));
            Assert.That(resultBlock.BlockType, Is.EqualTo(unaffected.BlockType));
            Assert.That(resultBlock.Element, Is.EqualTo(unaffected.Element));
        }

        [Test]
        public void TryResolve_MoveRecordsMatchSingleCollapseResult()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 2, ElementType.Water);
            BoardBlock expectedFirstMoved =
                board.Get(new BoardPosition(0, 3));
            var random = new RecordingRandomSource(
                CreateRepeatedSequence(3, 0));

            CreateResolver(random, 31).TryResolve(
                board,
                out BoardCascadeStep step);

            Assert.That(step.Collapse.Moves[0].Block, Is.SameAs(expectedFirstMoved));
            Assert.That(
                step.Collapse.Moves[0].From,
                Is.EqualTo(new BoardPosition(0, 3)));
            Assert.That(
                step.Collapse.Moves[0].To,
                Is.EqualTo(new BoardPosition(0, 2)));
            Assert.That(step.Collapse.Moves, Has.Count.EqualTo(6));
        }

        [Test]
        public void TryResolve_SpawnCountEqualsRemovedCountAndIdsAreSequential()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 1, ElementType.Fire);
            SetVertical(board, 4, 2, 4, ElementType.Grass);
            var random = new RecordingRandomSource(
                CreateRepeatedSequence(6, 0));

            CreateResolver(random, 100).TryResolve(
                board,
                out BoardCascadeStep step);

            Assert.That(
                step.Refill.Spawns.Count,
                Is.EqualTo(step.RemovedBlockCount));
            for (int index = 0; index < step.Refill.Spawns.Count; index++)
            {
                Assert.That(
                    step.Refill.Spawns[index].RuntimeId,
                    Is.EqualTo(100 + index));
            }
        }

        [Test]
        public void TryResolve_NewMatchFromRefillRemainsForNextCascadeStep()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 0, ElementType.Dark);
            var random = new RecordingRandomSource(new[] { 0, 0, 0 });

            CreateResolver(random, 31).TryResolve(
                board,
                out BoardCascadeStep step);
            IReadOnlyList<BoardMatch> matchesAfter =
                BoardMatchFinder.FindMatches(step.Board);

            Assert.That(step.MatchEventCount, Is.EqualTo(1));
            Assert.That(step.Matches[0].Element, Is.EqualTo(ElementType.Dark));
            Assert.That(random.CallCount, Is.EqualTo(3));
            Assert.That(
                ContainsMatchAt(
                    matchesAfter,
                    ElementType.Fire,
                    new BoardPosition(0, 4),
                    new BoardPosition(1, 4),
                    new BoardPosition(2, 4)),
                Is.True);
        }

        [Test]
        public void TryResolve_CallsMatchFinderExactlyOnce()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 2, ElementType.Water);
            int callCount = 0;
            var random = new RecordingRandomSource(
                CreateRepeatedSequence(3, 0));
            var resolver = new BoardCascadeStepResolver(
                candidate =>
                {
                    callCount++;
                    return BoardMatchFinder.FindMatches(candidate);
                },
                new BoardRefiller(
                    random,
                    new BoardBlockIdGenerator(31)));

            bool resolved = resolver.TryResolve(
                board,
                out BoardCascadeStep step);

            Assert.That(resolved, Is.True);
            Assert.That(step, Is.Not.Null);
            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void TryResolve_DuplicateMatchedPositionThrowsBeforeRefill()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 2, ElementType.Water);
            BoardMatch match = BoardMatchFinder.FindMatches(board)[0];
            var random = new RecordingRandomSource(Array.Empty<int>());
            var idGenerator = new BoardBlockIdGenerator(100);
            var resolver = new BoardCascadeStepResolver(
                candidate => new[] { match, match },
                new BoardRefiller(random, idGenerator));

            Assert.Throws<InvalidOperationException>(
                () => resolver.TryResolve(board, out BoardCascadeStep _));
            Assert.That(random.CallCount, Is.Zero);
            Assert.That(idGenerator.Next(), Is.EqualTo(100));
        }

        [Test]
        public void TryResolve_DoesNotChangeInputAndResultBoardIsIndependent()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 2, ElementType.Water);
            BoardBlock[] originalContents = CaptureBoardContents(board);
            var random = new RecordingRandomSource(
                CreateRepeatedSequence(3, 1));

            CreateResolver(random, 31).TryResolve(
                board,
                out BoardCascadeStep step);

            Assert.That(step.Board, Is.Not.SameAs(board));
            AssertBoardContainsReferences(board, originalContents);

            step.Board.Clear(new BoardPosition(5, 4));
            Assert.That(board.Get(new BoardPosition(5, 4)), Is.Not.Null);
        }

        [Test]
        public void TryResolve_MatchesCollectionCannotBeModifiedExternally()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 2, ElementType.Water);
            var random = new RecordingRandomSource(
                CreateRepeatedSequence(3, 0));

            CreateResolver(random, 31).TryResolve(
                board,
                out BoardCascadeStep step);
            var matches = step.Matches as IList<BoardMatch>;

            Assert.That(matches, Is.Not.Null);
            Assert.That(matches.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => matches.RemoveAt(0));
        }

        [Test]
        public void TryResolve_SameInitialConditionsProduceSameStep()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 1, ElementType.Fire);
            SetVertical(board, 4, 2, 4, ElementType.Grass);
            int[] sequence = { 0, 1, 2, 3, 4, 0 };

            CreateResolver(
                new RecordingRandomSource(sequence),
                100).TryResolve(board, out BoardCascadeStep first);
            CreateResolver(
                new RecordingRandomSource(sequence),
                100).TryResolve(board, out BoardCascadeStep second);

            Assert.That(second.MatchEventCount, Is.EqualTo(first.MatchEventCount));
            Assert.That(
                second.RemovedBlockCount,
                Is.EqualTo(first.RemovedBlockCount));
            AssertBoardsHaveEquivalentData(first.Board, second.Board);
            for (int index = 0; index < first.Refill.Spawns.Count; index++)
            {
                Assert.That(
                    second.Refill.Spawns[index].Target,
                    Is.EqualTo(first.Refill.Spawns[index].Target));
                Assert.That(
                    second.Refill.Spawns[index].SourceY,
                    Is.EqualTo(first.Refill.Spawns[index].SourceY));
            }
        }

        [Test]
        public void TryResolve_NullBoardThrows()
        {
            var resolver = CreateResolver(
                new RecordingRandomSource(Array.Empty<int>()),
                1);

            Assert.Throws<ArgumentNullException>(
                () => resolver.TryResolve(null, out BoardCascadeStep _));
        }

        [Test]
        public void Constructor_NullMatchFinderThrows()
        {
            var refiller = new BoardRefiller(
                new RecordingRandomSource(Array.Empty<int>()),
                new BoardBlockIdGenerator());

            Assert.Throws<ArgumentNullException>(
                () => new BoardCascadeStepResolver(null, refiller));
        }

        [Test]
        public void Constructor_NullRefillerThrows()
        {
            Assert.Throws<ArgumentNullException>(
                () => new BoardCascadeStepResolver(null));
        }

        [Test]
        public void TryResolve_NullMatchCollectionThrows()
        {
            var resolver = new BoardCascadeStepResolver(
                board => null,
                new BoardRefiller(
                    new RecordingRandomSource(Array.Empty<int>()),
                    new BoardBlockIdGenerator()));

            Assert.Throws<InvalidOperationException>(
                () => resolver.TryResolve(
                    CreateBoardWithoutMatches(),
                    out BoardCascadeStep _));
        }

        private static BoardCascadeStepResolver CreateResolver(
            RecordingRandomSource random,
            int firstId)
        {
            return CreateResolver(
                random,
                new BoardBlockIdGenerator(firstId));
        }

        private static BoardCascadeStepResolver CreateResolver(
            RecordingRandomSource random,
            BoardBlockIdGenerator idGenerator)
        {
            return new BoardCascadeStepResolver(
                new BoardRefiller(random, idGenerator));
        }

        private static BoardState CreateBoardWithoutMatches()
        {
            var board = new BoardState();
            long runtimeId = 1;

            for (int y = 0; y < BoardConstants.Height; y++)
            {
                for (int x = 0; x < BoardConstants.Width; x++)
                {
                    board.Set(
                        new BoardPosition(x, y),
                        new BoardBlock(
                            runtimeId++,
                            BoardBlockType.Normal,
                            (ElementType)((x + y) % 5)));
                }
            }

            Assert.That(BoardMatchFinder.FindMatches(board), Is.Empty);
            return board;
        }

        private static void SetHorizontal(
            BoardState board,
            int startX,
            int endX,
            int y,
            ElementType element)
        {
            for (int x = startX; x <= endX; x++)
            {
                SetElement(board, x, y, element);
            }
        }

        private static void SetVertical(
            BoardState board,
            int x,
            int startY,
            int endY,
            ElementType element)
        {
            for (int y = startY; y <= endY; y++)
            {
                SetElement(board, x, y, element);
            }
        }

        private static void SetElement(
            BoardState board,
            int x,
            int y,
            ElementType element)
        {
            var position = new BoardPosition(x, y);
            long runtimeId = board.Get(position).RuntimeId;
            board.Set(
                position,
                new BoardBlock(
                    runtimeId,
                    BoardBlockType.Normal,
                    element));
        }

        private static int[] CreateRepeatedSequence(int count, int value)
        {
            var sequence = new int[count];
            for (int index = 0; index < count; index++)
            {
                sequence[index] = value;
            }

            return sequence;
        }

        private static BoardBlock[] CaptureBoardContents(BoardState board)
        {
            var contents = new BoardBlock[BoardConstants.CellCount];
            for (int index = 0; index < contents.Length; index++)
            {
                contents[index] = board.Get(BoardPosition.FromIndex(index));
            }

            return contents;
        }

        private static void AssertBoardContainsReferences(
            BoardState board,
            BoardBlock[] expected)
        {
            for (int index = 0; index < expected.Length; index++)
            {
                Assert.That(
                    board.Get(BoardPosition.FromIndex(index)),
                    Is.SameAs(expected[index]));
            }
        }

        private static void AssertBoardIsFull(BoardState board)
        {
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                Assert.That(
                    board.Get(BoardPosition.FromIndex(index)),
                    Is.Not.Null);
            }
        }

        private static void AssertBoardsHaveEquivalentData(
            BoardState expected,
            BoardState actual)
        {
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                BoardBlock expectedBlock = expected.Get(position);
                BoardBlock actualBlock = actual.Get(position);

                Assert.That(
                    actualBlock.RuntimeId,
                    Is.EqualTo(expectedBlock.RuntimeId));
                Assert.That(
                    actualBlock.BlockType,
                    Is.EqualTo(expectedBlock.BlockType));
                Assert.That(actualBlock.Element, Is.EqualTo(expectedBlock.Element));
            }
        }

        private static bool ContainsMatchAt(
            IReadOnlyList<BoardMatch> matches,
            ElementType element,
            params BoardPosition[] expectedPositions)
        {
            for (int matchIndex = 0;
                matchIndex < matches.Count;
                matchIndex++)
            {
                BoardMatch match = matches[matchIndex];
                if (match.Element != element
                    || match.Positions.Count != expectedPositions.Length)
                {
                    continue;
                }

                bool allFound = true;
                for (int expectedIndex = 0;
                    expectedIndex < expectedPositions.Length;
                    expectedIndex++)
                {
                    bool found = false;
                    for (int actualIndex = 0;
                        actualIndex < match.Positions.Count;
                        actualIndex++)
                    {
                        if (match.Positions[actualIndex]
                            == expectedPositions[expectedIndex])
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        allFound = false;
                        break;
                    }
                }

                if (allFound)
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class RecordingRandomSource : IRandomSource
        {
            private readonly Queue<int> values;

            public RecordingRandomSource(IEnumerable<int> values)
            {
                this.values = new Queue<int>(values);
            }

            public int CallCount { get; private set; }

            public int Next(int minInclusive, int maxExclusive)
            {
                CallCount++;

                if (values.Count == 0)
                {
                    throw new InvalidOperationException(
                        "No recorded random value remains.");
                }

                int value = values.Dequeue();
                if (value < minInclusive || value >= maxExclusive)
                {
                    throw new InvalidOperationException(
                        $"Recorded random value {value} is outside the requested range.");
                }

                return value;
            }

            public float NextFloat()
            {
                throw new NotSupportedException();
            }
        }
    }
}
