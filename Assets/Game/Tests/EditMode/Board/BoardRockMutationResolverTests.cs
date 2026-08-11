using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board
{
    public sealed class BoardRockMutationResolverTests
    {
        private const int MaximumRockCount = 6;

        private static readonly int[] PlayableElements =
        {
            0, 0, 1, 4, 0,
            4, 0, 4, 3, 4,
            1, 3, 0, 3, 1,
            0, 4, 1, 1, 4,
            0, 0, 2, 4, 4,
            1, 2, 4, 0, 0
        };

        private static readonly int[] OneMoveElements =
        {
            4, 3, 1, 4, 0,
            4, 1, 0, 4, 0,
            1, 3, 0, 3, 2,
            0, 2, 4, 2, 0,
            3, 3, 1, 4, 1,
            4, 2, 0, 0, 2
        };

        [TestCase(0, 3, 3)]
        [TestCase(1, 3, 3)]
        [TestCase(3, 3, 3)]
        [TestCase(4, 3, 2)]
        [TestCase(5, 3, 1)]
        [TestCase(6, 3, 0)]
        public void ResolveHonorsRequestedAndMaximumCounts(
            int existingRockCount,
            int requestedCount,
            int expectedCreatedCount)
        {
            BoardState board = CreateBoard(PlayableElements);
            AddExistingRocks(board, existingRockCount);
            BoardBlock[] original = Capture(board);

            BoardRockMutationResult result = CreateResolver(
                new SeededRandomSource(1701),
                firstId: 1000).Resolve(
                    board,
                    requestedCount,
                    MaximumRockCount);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.CreatedCount,
                Is.EqualTo(expectedCreatedCount));
            Assert.That(CountRocks(result.Board),
                Is.EqualTo(existingRockCount + expectedCreatedCount));
            Assert.That(new BoardMoveAnalyzer().HasAnyValidSwap(result.Board),
                Is.True);
            AssertOriginalUnchanged(board, original);
        }

        [Test]
        public void ResolveConvertsOnlyNormalBlocksWithNewIdentity()
        {
            BoardState board = CreateBoard(PlayableElements);
            var rockPosition = new BoardPosition(0, 0);
            var specialPosition = new BoardPosition(0, 1);
            var lockedPosition = new BoardPosition(0, 2);
            board.Set(rockPosition,
                new BoardBlock(
                    board.Get(rockPosition).RuntimeId,
                    BoardBlockType.Rock,
                    null));
            board.Set(specialPosition,
                new BoardBlock(
                    board.Get(specialPosition).RuntimeId,
                    BoardBlockType.Special,
                    ElementType.Water));
            board.Set(lockedPosition,
                new BoardBlock(
                    board.Get(lockedPosition).RuntimeId,
                    BoardBlockType.Locked,
                    ElementType.Grass));
            var originalIds = new HashSet<long>(Enumerable.Range(1, 30)
                .Select(value => (long)value));

            BoardRockMutationResult result = CreateResolver(
                new SeededRandomSource(99),
                firstId: 1000).Resolve(board, 3, MaximumRockCount);

            Assert.That(result.Succeeded, Is.True);
            BoardPosition[] placementPositions = result.Placements
                .Select(item => item.Position).ToArray();
            Assert.That(placementPositions.Contains(rockPosition), Is.False);
            Assert.That(placementPositions.Contains(specialPosition), Is.False);
            Assert.That(placementPositions.Contains(lockedPosition), Is.False);
            Assert.That(result.Placements.Select(item => item.Position)
                .Distinct().Count(), Is.EqualTo(result.CreatedCount));
            foreach (BoardRockPlacement placement in result.Placements)
            {
                Assert.That(placement.ReplacedBlock.BlockType,
                    Is.EqualTo(BoardBlockType.Normal));
                Assert.That(placement.RockBlock.BlockType,
                    Is.EqualTo(BoardBlockType.Rock));
                Assert.That(placement.RockBlock.Element, Is.Null);
                Assert.That(originalIds.Contains(
                    placement.RockBlock.RuntimeId), Is.False);
                Assert.That(result.Board.Get(placement.Position),
                    Is.SameAs(placement.RockBlock));
            }
        }

        [Test]
        public void SameSeedProducesSamePositionsAndRockIds()
        {
            BoardState board = CreateBoard(PlayableElements);

            BoardRockMutationResult first = CreateResolver(
                new SeededRandomSource(24680),
                firstId: 1000).Resolve(board, 3, MaximumRockCount);
            BoardRockMutationResult second = CreateResolver(
                new SeededRandomSource(24680),
                firstId: 1000).Resolve(board, 3, MaximumRockCount);

            Assert.That(second.Placements.Select(item => item.Position),
                Is.EqualTo(first.Placements.Select(item => item.Position)));
            Assert.That(second.Placements.Select(
                    item => item.RockBlock.RuntimeId),
                Is.EqualTo(first.Placements.Select(
                    item => item.RockBlock.RuntimeId)));
            AssertBoardsEquivalent(first.Board, second.Board);
        }

        [Test]
        public void DeadPlacementRetriesAnotherDeterministicSelection()
        {
            BoardState board = CreateBoard(OneMoveElements);
            var random = new RecordingRandomSource(new[]
            {
                0, 1, 4,
                0, 1, 2
            });
            BoardRockMutationResolver resolver = CreateResolver(
                random,
                firstId: 1000,
                maxPlacementAttempts: 2);

            BoardRockMutationResult result = resolver.Resolve(
                board,
                requestedCount: 3,
                maximumRockCount: MaximumRockCount);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.PlacementAttemptCount, Is.EqualTo(2));
            Assert.That(result.WasShuffled, Is.False);
            Assert.That(result.Placements.Select(item => item.Position),
                Is.EqualTo(new[]
                {
                    new BoardPosition(0, 0),
                    new BoardPosition(0, 1),
                    new BoardPosition(0, 2)
                }));
            Assert.That(random.CallCount, Is.EqualTo(6));
        }

        [Test]
        public void DeadPlacementFallsBackToNormalOnlyShuffle()
        {
            int[] sequence = Combine(
                new[] { 0, 1, 4 },
                IdentityPermutationSequence(normalCount: 27),
                new[]
                {
                    4, 1, 3, 3, 0, 2, 0, 0, 2,
                    1, 3, 0, 1, 1, 2, 1, 4, 4,
                    3, 3, 4, 2, 3, 2, 4, 2, 3
                });
            var random = new RecordingRandomSource(sequence);
            BoardRockMutationResolver resolver = CreateResolver(
                random,
                firstId: 1000,
                maxPlacementAttempts: 1,
                maxPermutationAttempts: 1,
                maxRegenerationAttempts: 1);

            BoardRockMutationResult result = resolver.Resolve(
                CreateBoard(OneMoveElements),
                requestedCount: 3,
                maximumRockCount: MaximumRockCount);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.WasShuffled, Is.True);
            Assert.That(result.ShuffleResult.Kind,
                Is.EqualTo(BoardShuffleKind.Regeneration));
            Assert.That(new BoardMoveAnalyzer().HasAnyValidSwap(result.Board),
                Is.True);
            foreach (BoardRockPlacement placement in result.Placements)
            {
                Assert.That(result.Board.Get(placement.Position),
                    Is.SameAs(placement.RockBlock));
            }
        }

        [Test]
        public void CompleteFailureReturnsOriginalDataWithoutPartialMutation()
        {
            int[] sequence = Combine(
                new[] { 0, 1, 4 },
                IdentityPermutationSequence(normalCount: 27),
                new[]
                {
                    1, 4, 4, 1, 0, 4, 0, 1, 3,
                    0, 3, 2, 0, 2, 4, 2, 0, 3,
                    3, 1, 4, 1, 4, 2, 0, 0, 2
                });
            BoardState board = CreateBoard(OneMoveElements);
            BoardBlock[] original = Capture(board);
            var random = new RecordingRandomSource(sequence);
            BoardRockMutationResolver resolver = CreateResolver(
                random,
                firstId: 1000,
                maxPlacementAttempts: 1,
                maxPermutationAttempts: 1,
                maxRegenerationAttempts: 1);

            BoardRockMutationResult result = resolver.Resolve(
                board,
                requestedCount: 3,
                maximumRockCount: MaximumRockCount);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.EqualTo(
                BoardRockMutationFailure.UnableToCreatePlayableBoard));
            Assert.That(result.CreatedCount, Is.Zero);
            AssertOriginalUnchanged(board, original);
            AssertBoardsContainSameReferences(result.Board, board);
        }

        private static BoardRockMutationResolver CreateResolver(
            IRandomSource random,
            int firstId,
            int maxPlacementAttempts =
                BoardRockMutationResolver.DefaultMaxPlacementAttempts,
            int maxPermutationAttempts =
                BoardShuffler.DefaultMaxPermutationAttempts,
            int maxRegenerationAttempts =
                BoardShuffler.DefaultMaxRegenerationAttempts)
        {
            var analyzer = new BoardMoveAnalyzer();
            var shuffler = new BoardShuffler(
                random,
                analyzer,
                maxPermutationAttempts,
                maxRegenerationAttempts);
            return new BoardRockMutationResolver(
                random,
                new BoardBlockIdGenerator(firstId),
                analyzer,
                shuffler,
                maxPlacementAttempts);
        }

        private static BoardState CreateBoard(int[] elements)
        {
            var board = new BoardState();
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    board.Set(position, new BoardBlock(
                        position.ToIndex() + 1,
                        BoardBlockType.Normal,
                        (ElementType)elements[
                            (x * BoardConstants.Height) + y]));
                }
            }

            Assert.That(BoardMatchFinder.FindMatches(board), Is.Empty);
            return board;
        }

        private static void AddExistingRocks(BoardState board, int count)
        {
            for (int index = 0; index < count; index++)
            {
                var position = new BoardPosition(
                    index / BoardConstants.Height,
                    index % BoardConstants.Height);
                board.Set(position, new BoardBlock(
                    board.Get(position).RuntimeId,
                    BoardBlockType.Rock,
                    null));
            }
        }

        private static int CountRocks(BoardState board)
        {
            int count = 0;
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                if (board.Get(BoardPosition.FromIndex(index)).BlockType
                    == BoardBlockType.Rock)
                {
                    count++;
                }
            }

            return count;
        }

        private static BoardBlock[] Capture(BoardState board)
        {
            var blocks = new BoardBlock[BoardConstants.CellCount];
            for (int index = 0; index < blocks.Length; index++)
            {
                blocks[index] = board.Get(BoardPosition.FromIndex(index));
            }

            return blocks;
        }

        private static void AssertOriginalUnchanged(
            BoardState board,
            IReadOnlyList<BoardBlock> original)
        {
            for (int index = 0; index < original.Count; index++)
            {
                Assert.That(board.Get(BoardPosition.FromIndex(index)),
                    Is.SameAs(original[index]));
            }
        }

        private static void AssertBoardsContainSameReferences(
            BoardState first,
            BoardState second)
        {
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                Assert.That(first.Get(position), Is.SameAs(second.Get(position)));
            }
        }

        private static void AssertBoardsEquivalent(
            BoardState first,
            BoardState second)
        {
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                BoardBlock left = first.Get(position);
                BoardBlock right = second.Get(position);
                Assert.That(right.RuntimeId, Is.EqualTo(left.RuntimeId));
                Assert.That(right.BlockType, Is.EqualTo(left.BlockType));
                Assert.That(right.Element, Is.EqualTo(left.Element));
            }
        }

        private static int[] IdentityPermutationSequence(int normalCount)
        {
            var sequence = new int[normalCount - 1];
            for (int index = 0; index < sequence.Length; index++)
            {
                sequence[index] = normalCount - 1 - index;
            }

            return sequence;
        }

        private static int[] Combine(params int[][] arrays)
        {
            return arrays.SelectMany(array => array).ToArray();
        }

        private sealed class RecordingRandomSource : IRandomSource
        {
            private readonly IReadOnlyList<int> values;
            private int index;

            public RecordingRandomSource(IReadOnlyList<int> values)
            {
                this.values = values;
            }

            public int CallCount => index;

            public int Next(int minInclusive, int maxExclusive)
            {
                if (index >= values.Count)
                {
                    throw new InvalidOperationException(
                        "No recorded random value remains.");
                }

                int value = values[index++];
                if (value < minInclusive || value >= maxExclusive)
                {
                    throw new InvalidOperationException(
                        $"Recorded value {value} is outside "
                            + $"[{minInclusive}, {maxExclusive}).");
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
