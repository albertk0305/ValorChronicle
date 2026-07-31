using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board
{
    public sealed class BoardCascadeResolverTests
    {
        [Test]
        public void Resolve_NoMatchesReturnsZeroCountsAndIndependentBoard()
        {
            BoardState board = CreateBoardWithoutMatches();
            BoardBlock[] originalContents = CaptureBoardContents(board);
            var random = new RecordingRandomSource(Array.Empty<int>());
            var idGenerator = new BoardBlockIdGenerator(100);

            BoardCascadeResult result =
                CreateResolver(random, idGenerator).Resolve(board);

            Assert.That(result.Steps, Is.Empty);
            Assert.That(result.CascadeCount, Is.Zero);
            Assert.That(result.ComboCount, Is.Zero);
            Assert.That(result.TotalRemovedBlockCount, Is.Zero);
            Assert.That(result.TotalSpawnedBlockCount, Is.Zero);
            Assert.That(result.TotalMoveCount, Is.Zero);
            Assert.That(random.CallCount, Is.Zero);
            Assert.That(idGenerator.Next(), Is.EqualTo(100));
            Assert.That(result.Board, Is.Not.SameAs(board));
            AssertBoardsHaveSameContents(board, result.Board);
            AssertBoardContainsReferences(board, originalContents);
        }

        [Test]
        public void Resolve_SingleMatchProducesOneCascadeAndOneCombo()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 2, ElementType.Water);
            var random = new RecordingRandomSource(new[] { 0, 1, 2 });

            BoardCascadeResult result =
                CreateResolver(random, 31).Resolve(board);

            Assert.That(result.CascadeCount, Is.EqualTo(1));
            Assert.That(result.ComboCount, Is.EqualTo(1));
            Assert.That(result.TotalRemovedBlockCount, Is.EqualTo(3));
            Assert.That(result.TotalSpawnedBlockCount, Is.EqualTo(3));
            Assert.That(BoardMatchFinder.FindMatches(result.Board), Is.Empty);
            AssertBoardIsFull(result.Board);
        }

        [Test]
        public void Resolve_TwoSimultaneousMatchesProduceOneCascadeAndTwoCombos()
        {
            BoardState board = CreateTwoMatchBoard();
            var random = new RecordingRandomSource(
                new[] { 0, 1, 2, 3, 4, 0 });

            BoardCascadeResult result =
                CreateResolver(random, 31).Resolve(board);

            Assert.That(result.CascadeCount, Is.EqualTo(1));
            Assert.That(result.ComboCount, Is.EqualTo(2));
            Assert.That(result.Steps[0].MatchEventCount, Is.EqualTo(2));
            Assert.That(result.TotalRemovedBlockCount, Is.EqualTo(6));
            Assert.That(result.TotalSpawnedBlockCount, Is.EqualTo(6));
            Assert.That(BoardMatchFinder.FindMatches(result.Board), Is.Empty);
        }

        [Test]
        public void Resolve_RefillMatchProducesSecondCascadeInOrder()
        {
            BoardState board = CreateTwoStepBoard();
            var random = new RecordingRandomSource(
                new[] { 0, 0, 0, 0, 1, 2 });

            BoardCascadeResult result =
                CreateResolver(random, 31).Resolve(board);

            Assert.That(result.CascadeCount, Is.EqualTo(2));
            Assert.That(result.ComboCount, Is.EqualTo(2));
            Assert.That(
                result.Steps[0].Matches[0].Element,
                Is.EqualTo(ElementType.Dark));
            Assert.That(
                result.Steps[1].Matches[0].Element,
                Is.EqualTo(ElementType.Fire));
            Assert.That(BoardMatchFinder.FindMatches(result.Board), Is.Empty);
        }

        [Test]
        public void Resolve_MultipleMatchesAcrossStepsSumAllCombos()
        {
            BoardState board = CreateTwoMatchBoard();
            var random = new RecordingRandomSource(
                new[]
                {
                    0, 0, 0, 1, 1, 1,
                    0, 1, 2, 3, 4, 0
                });

            BoardCascadeResult result =
                CreateResolver(random, 31).Resolve(board);

            Assert.That(result.CascadeCount, Is.EqualTo(2));
            Assert.That(result.Steps[0].MatchEventCount, Is.EqualTo(2));
            Assert.That(result.Steps[1].MatchEventCount, Is.EqualTo(2));
            Assert.That(result.ComboCount, Is.EqualTo(4));
            Assert.That(
                result.ComboCount,
                Is.Not.EqualTo(result.CascadeCount));
            Assert.That(BoardMatchFinder.FindMatches(result.Board), Is.Empty);
        }

        [Test]
        public void Resolve_TotalsAreCalculatedFromAllSteps()
        {
            BoardState board = CreateTwoMatchBoard();
            var random = new RecordingRandomSource(
                new[]
                {
                    0, 0, 0, 1, 1, 1,
                    0, 1, 2, 3, 4, 0
                });

            BoardCascadeResult result =
                CreateResolver(random, 31).Resolve(board);
            int expectedRemoved = 0;
            int expectedSpawned = 0;
            int expectedMoves = 0;

            foreach (BoardCascadeStep step in result.Steps)
            {
                expectedRemoved += step.Collapse.Removals.Count;
                expectedSpawned += step.Refill.Spawns.Count;
                expectedMoves += step.Collapse.Moves.Count;
            }

            Assert.That(
                result.TotalRemovedBlockCount,
                Is.EqualTo(expectedRemoved));
            Assert.That(
                result.TotalSpawnedBlockCount,
                Is.EqualTo(expectedSpawned));
            Assert.That(result.TotalMoveCount, Is.EqualTo(expectedMoves));
            Assert.That(
                result.TotalSpawnedBlockCount,
                Is.EqualTo(result.TotalRemovedBlockCount));
        }

        [Test]
        public void Resolve_NewRuntimeIdsRemainUniqueAndSequentialAcrossSteps()
        {
            BoardState board = CreateTwoStepBoard();
            var random = new RecordingRandomSource(
                new[] { 0, 0, 0, 0, 1, 2 });

            BoardCascadeResult result =
                CreateResolver(random, 100).Resolve(board);
            var runtimeIds = new HashSet<long>();
            int expectedId = 100;

            foreach (BoardCascadeStep step in result.Steps)
            {
                foreach (BoardBlockSpawn spawn in step.Refill.Spawns)
                {
                    Assert.That(spawn.RuntimeId, Is.EqualTo(expectedId++));
                    Assert.That(runtimeIds.Add(spawn.RuntimeId), Is.True);
                }
            }

            Assert.That(runtimeIds, Has.Count.EqualTo(6));
        }

        [Test]
        public void Resolve_UnmatchedExistingBlockKeepsIdentityAndRuntimeData()
        {
            BoardState board = CreateTwoStepBoard();
            var unaffectedPosition = new BoardPosition(5, 2);
            BoardBlock unaffected = board.Get(unaffectedPosition);
            var random = new RecordingRandomSource(
                new[] { 0, 0, 0, 0, 1, 2 });

            BoardCascadeResult result =
                CreateResolver(random, 31).Resolve(board);
            BoardBlock resultBlock = result.Board.Get(unaffectedPosition);

            Assert.That(resultBlock, Is.SameAs(unaffected));
            Assert.That(resultBlock.RuntimeId, Is.EqualTo(unaffected.RuntimeId));
            Assert.That(resultBlock.BlockType, Is.EqualTo(unaffected.BlockType));
            Assert.That(resultBlock.Element, Is.EqualTo(unaffected.Element));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_NonPositiveMaximumThrows(int maxCascadeSteps)
        {
            BoardCascadeStepResolver stepResolver = CreateStepResolver(
                new RecordingRandomSource(Array.Empty<int>()),
                new BoardBlockIdGenerator());

            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BoardCascadeResolver(
                    stepResolver,
                    maxCascadeSteps));
        }

        [Test]
        public void Resolve_StopsBeforeMaximumWhenMatchesEndEarlier()
        {
            BoardState board = CreateTwoStepBoard();
            var random = new RecordingRandomSource(
                new[] { 0, 0, 0, 0, 1, 2 });
            var idGenerator = new BoardBlockIdGenerator(31);
            var resolver = CreateResolver(
                random,
                idGenerator,
                maxCascadeSteps: 3);

            BoardCascadeResult result = resolver.Resolve(board);

            Assert.That(result.CascadeCount, Is.EqualTo(2));
            Assert.That(random.CallCount, Is.EqualTo(6));
            Assert.That(idGenerator.Next(), Is.EqualTo(37));
        }

        [Test]
        public void Resolve_ExactlyMaximumStepsThenNoMatchReturnsNormally()
        {
            BoardState board = CreateTwoStepBoard();
            var random = new RecordingRandomSource(
                new[] { 0, 0, 0, 0, 1, 2 });
            var idGenerator = new BoardBlockIdGenerator(31);
            var resolver = CreateResolver(
                random,
                idGenerator,
                maxCascadeSteps: 2);

            BoardCascadeResult result = resolver.Resolve(board);

            Assert.That(result.CascadeCount, Is.EqualTo(2));
            Assert.That(BoardMatchFinder.FindMatches(result.Board), Is.Empty);
            Assert.That(random.CallCount, Is.EqualTo(6));
            Assert.That(idGenerator.Next(), Is.EqualTo(37));
        }

        [Test]
        public void Resolve_MatchAfterMaximumThrowsWithoutExtraConsumption()
        {
            BoardState board = CreateTwoStepBoard();
            var random = new RecordingRandomSource(new[] { 0, 0, 0 });
            var idGenerator = new BoardBlockIdGenerator(31);
            var resolver = CreateResolver(
                random,
                idGenerator,
                maxCascadeSteps: 1);

            Assert.Throws<InvalidOperationException>(
                () => resolver.Resolve(board));
            Assert.That(random.CallCount, Is.EqualTo(3));
            Assert.That(idGenerator.Next(), Is.EqualTo(34));
        }

        [Test]
        public void Resolve_MaximumCheckUsesMatcherWithoutAnotherStepResolution()
        {
            BoardState board = CreateTwoStepBoard();
            var random = new RecordingRandomSource(new[] { 0, 0, 0 });
            var idGenerator = new BoardBlockIdGenerator(31);
            int maximumCheckCount = 0;
            var resolver = new BoardCascadeResolver(
                CreateStepResolver(random, idGenerator),
                candidate =>
                {
                    maximumCheckCount++;
                    return BoardMatchFinder.FindMatches(candidate);
                },
                1);

            Assert.Throws<InvalidOperationException>(
                () => resolver.Resolve(board));
            Assert.That(maximumCheckCount, Is.EqualTo(1));
            Assert.That(random.CallCount, Is.EqualTo(3));
            Assert.That(idGenerator.Next(), Is.EqualTo(34));
        }

        [Test]
        public void Resolve_DoesNotChangeInputAndResultBoardIsIndependent()
        {
            BoardState board = CreateTwoStepBoard();
            BoardBlock[] originalContents = CaptureBoardContents(board);
            var random = new RecordingRandomSource(
                new[] { 0, 0, 0, 0, 1, 2 });

            BoardCascadeResult result =
                CreateResolver(random, 31).Resolve(board);

            AssertBoardContainsReferences(board, originalContents);
            Assert.That(result.Board, Is.Not.SameAs(board));

            result.Board.Clear(new BoardPosition(5, 4));
            Assert.That(board.Get(new BoardPosition(5, 4)), Is.Not.Null);
        }

        [Test]
        public void Resolve_StepsCollectionCannotBeModifiedExternally()
        {
            BoardState board = CreateTwoStepBoard();
            var random = new RecordingRandomSource(
                new[] { 0, 0, 0, 0, 1, 2 });

            BoardCascadeResult result =
                CreateResolver(random, 31).Resolve(board);
            var steps = result.Steps as IList<BoardCascadeStep>;

            Assert.That(steps, Is.Not.Null);
            Assert.That(steps.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => steps.RemoveAt(0));
        }

        [Test]
        public void Resolve_SameInitialConditionsProduceSameResult()
        {
            BoardState board = CreateTwoMatchBoard();
            int[] sequence =
            {
                0, 0, 0, 1, 1, 1,
                0, 1, 2, 3, 4, 0
            };

            BoardCascadeResult first = CreateResolver(
                new RecordingRandomSource(sequence),
                100).Resolve(board);
            BoardCascadeResult second = CreateResolver(
                new RecordingRandomSource(sequence),
                100).Resolve(board);

            Assert.That(second.CascadeCount, Is.EqualTo(first.CascadeCount));
            Assert.That(second.ComboCount, Is.EqualTo(first.ComboCount));
            Assert.That(
                second.TotalRemovedBlockCount,
                Is.EqualTo(first.TotalRemovedBlockCount));
            Assert.That(
                second.TotalSpawnedBlockCount,
                Is.EqualTo(first.TotalSpawnedBlockCount));
            AssertBoardsHaveEquivalentData(first.Board, second.Board);
        }

        [Test]
        public void Resolve_NullBoardThrows()
        {
            var resolver = CreateResolver(
                new RecordingRandomSource(Array.Empty<int>()),
                1);

            Assert.Throws<ArgumentNullException>(() => resolver.Resolve(null));
        }

        [Test]
        public void Constructor_NullStepResolverThrows()
        {
            Assert.Throws<ArgumentNullException>(
                () => new BoardCascadeResolver(null));
        }

        [Test]
        public void Constructor_NullMatchFinderThrows()
        {
            BoardCascadeStepResolver stepResolver = CreateStepResolver(
                new RecordingRandomSource(Array.Empty<int>()),
                new BoardBlockIdGenerator());

            Assert.Throws<ArgumentNullException>(
                () => new BoardCascadeResolver(
                    stepResolver,
                    null));
        }

        [Test]
        public void Resolve_NullMaximumCheckMatchCollectionThrows()
        {
            BoardState board = CreateTwoStepBoard();
            var random = new RecordingRandomSource(new[] { 0, 0, 0 });
            var resolver = new BoardCascadeResolver(
                CreateStepResolver(
                    random,
                    new BoardBlockIdGenerator(31)),
                candidate => null,
                1);

            Assert.Throws<InvalidOperationException>(
                () => resolver.Resolve(board));
        }

        private static BoardCascadeResolver CreateResolver(
            RecordingRandomSource random,
            int firstId,
            int maxCascadeSteps =
                BoardCascadeResolver.DefaultMaxCascadeSteps)
        {
            return CreateResolver(
                random,
                new BoardBlockIdGenerator(firstId),
                maxCascadeSteps);
        }

        private static BoardCascadeResolver CreateResolver(
            RecordingRandomSource random,
            BoardBlockIdGenerator idGenerator,
            int maxCascadeSteps =
                BoardCascadeResolver.DefaultMaxCascadeSteps)
        {
            return new BoardCascadeResolver(
                CreateStepResolver(random, idGenerator),
                maxCascadeSteps);
        }

        private static BoardCascadeStepResolver CreateStepResolver(
            RecordingRandomSource random,
            BoardBlockIdGenerator idGenerator)
        {
            return new BoardCascadeStepResolver(
                new BoardRefiller(random, idGenerator));
        }

        private static BoardState CreateTwoStepBoard()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 0, ElementType.Dark);
            Assert.That(BoardMatchFinder.FindMatches(board), Has.Count.EqualTo(1));
            return board;
        }

        private static BoardState CreateTwoMatchBoard()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 0, ElementType.Dark);
            SetHorizontal(board, 3, 5, 4, ElementType.Dark);
            Assert.That(BoardMatchFinder.FindMatches(board), Has.Count.EqualTo(2));
            return board;
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
                var position = new BoardPosition(x, y);
                long runtimeId = board.Get(position).RuntimeId;
                board.Set(
                    position,
                    new BoardBlock(
                        runtimeId,
                        BoardBlockType.Normal,
                        element));
            }
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

        private static void AssertBoardsHaveSameContents(
            BoardState expected,
            BoardState actual)
        {
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                Assert.That(actual.Get(position), Is.SameAs(expected.Get(position)));
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

        private static void AssertBoardIsFull(BoardState board)
        {
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                Assert.That(
                    board.Get(BoardPosition.FromIndex(index)),
                    Is.Not.Null);
            }
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
