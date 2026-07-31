using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board
{
    public sealed class BoardRefillerTests
    {
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Refill_FillsExpectedNumberAtTopOfOneColumn(int emptyCount)
        {
            BoardState board = CreateFullBoard();
            ClearTop(board, 2, emptyCount);
            var random = new RecordingRandomSource(
                CreateRepeatedSequence(emptyCount, 0));

            BoardRefillResult result =
                CreateRefiller(random, 100).Refill(board);

            Assert.That(result.Spawns, Has.Count.EqualTo(emptyCount));
            AssertBoardIsFull(result.Board);

            int firstEmptyY = BoardConstants.Height - emptyCount;
            for (int spawnIndex = 0;
                spawnIndex < emptyCount;
                spawnIndex++)
            {
                BoardBlockSpawn spawn = result.Spawns[spawnIndex];
                Assert.That(
                    spawn.Target,
                    Is.EqualTo(new BoardPosition(
                        2,
                        firstEmptyY + spawnIndex)));
                Assert.That(
                    spawn.SourceY,
                    Is.EqualTo(BoardConstants.Height + spawnIndex));
            }
        }

        [Test]
        public void Refill_FillsDifferentEmptyCountsAcrossColumns()
        {
            BoardState board = CreateFullBoard();
            ClearTop(board, 0, 1);
            ClearTop(board, 2, 3);
            ClearTop(board, 5, 2);
            var random = new RecordingRandomSource(
                CreateRepeatedSequence(6, 1));

            BoardRefillResult result =
                CreateRefiller(random, 100).Refill(board);

            Assert.That(result.Spawns, Has.Count.EqualTo(6));
            Assert.That(result.Spawns[0].Target, Is.EqualTo(new BoardPosition(0, 4)));
            Assert.That(result.Spawns[1].Target, Is.EqualTo(new BoardPosition(2, 2)));
            Assert.That(result.Spawns[2].Target, Is.EqualTo(new BoardPosition(2, 3)));
            Assert.That(result.Spawns[3].Target, Is.EqualTo(new BoardPosition(2, 4)));
            Assert.That(result.Spawns[4].Target, Is.EqualTo(new BoardPosition(5, 3)));
            Assert.That(result.Spawns[5].Target, Is.EqualTo(new BoardPosition(5, 4)));
            AssertBoardIsFull(result.Board);
        }

        [Test]
        public void Refill_FillsCompletelyEmptyColumn()
        {
            BoardState board = CreateFullBoard();
            ClearTop(board, 4, BoardConstants.Height);
            var random = new RecordingRandomSource(
                CreateRepeatedSequence(BoardConstants.Height, 2));

            BoardRefillResult result =
                CreateRefiller(random, 200).Refill(board);

            Assert.That(
                result.Spawns,
                Has.Count.EqualTo(BoardConstants.Height));
            for (int y = 0; y < BoardConstants.Height; y++)
            {
                Assert.That(
                    result.Spawns[y].Target,
                    Is.EqualTo(new BoardPosition(4, y)));
            }
        }

        [Test]
        public void Refill_FillsCompletelyEmptyBoard()
        {
            var board = new BoardState();
            var random = new RecordingRandomSource(
                CreateRepeatedSequence(BoardConstants.CellCount, 3));

            BoardRefillResult result =
                CreateRefiller(random, 1).Refill(board);

            Assert.That(
                result.Spawns,
                Has.Count.EqualTo(BoardConstants.CellCount));
            Assert.That(
                random.CallCount,
                Is.EqualTo(BoardConstants.CellCount));
            AssertBoardIsFull(result.Board);
        }

        [Test]
        public void Refill_FullBoardConsumesNoRandomValueOrRuntimeId()
        {
            BoardState board = CreateFullBoard();
            var random = new RecordingRandomSource(Array.Empty<int>());
            var idGenerator = new BoardBlockIdGenerator(100);
            var refiller = new BoardRefiller(random, idGenerator);

            BoardRefillResult result = refiller.Refill(board);

            Assert.That(result.Spawns, Is.Empty);
            Assert.That(random.CallCount, Is.Zero);
            Assert.That(idGenerator.Next(), Is.EqualTo(100));
            Assert.That(result.Board, Is.Not.SameAs(board));
            AssertBoardsHaveSameContents(board, result.Board);
        }

        [Test]
        public void Refill_ComputesSourceAndTargetCoordinatesFromSpawnIndex()
        {
            BoardState board = CreateFullBoard();
            ClearTop(board, 3, 3);
            var random = new RecordingRandomSource(
                CreateRepeatedSequence(3, 0));

            BoardRefillResult result =
                CreateRefiller(random, 100).Refill(board);

            for (int spawnIndex = 0; spawnIndex < 3; spawnIndex++)
            {
                BoardBlockSpawn spawn = result.Spawns[spawnIndex];
                Assert.That(spawn.SourceX, Is.EqualTo(spawn.Target.X));
                Assert.That(spawn.SourceY, Is.EqualTo(5 + spawnIndex));
                Assert.That(spawn.Target.Y, Is.EqualTo(2 + spawnIndex));
                Assert.That(spawn.SourceY, Is.GreaterThanOrEqualTo(5));
            }
        }

        [Test]
        public void Refill_SpawnsInOneColumnHaveEqualFallDistance()
        {
            BoardState board = CreateFullBoard();
            ClearTop(board, 1, 4);
            var random = new RecordingRandomSource(
                CreateRepeatedSequence(4, 0));

            BoardRefillResult result =
                CreateRefiller(random, 100).Refill(board);

            int expectedDistance =
                result.Spawns[0].SourceY - result.Spawns[0].Target.Y;
            foreach (BoardBlockSpawn spawn in result.Spawns)
            {
                Assert.That(
                    spawn.SourceY - spawn.Target.Y,
                    Is.EqualTo(expectedDistance));
            }
        }

        [Test]
        public void Refill_OrdersSpawnsByColumnThenAscendingTargetHeight()
        {
            BoardState board = CreateFullBoard();
            ClearTop(board, 4, 2);
            ClearTop(board, 1, 3);
            var random = new RecordingRandomSource(
                CreateRepeatedSequence(5, 0));

            BoardRefillResult result =
                CreateRefiller(random, 100).Refill(board);

            Assert.That(result.Spawns[0].Target, Is.EqualTo(new BoardPosition(1, 2)));
            Assert.That(result.Spawns[1].Target, Is.EqualTo(new BoardPosition(1, 3)));
            Assert.That(result.Spawns[2].Target, Is.EqualTo(new BoardPosition(1, 4)));
            Assert.That(result.Spawns[3].Target, Is.EqualTo(new BoardPosition(4, 3)));
            Assert.That(result.Spawns[4].Target, Is.EqualTo(new BoardPosition(4, 4)));
        }

        [Test]
        public void Refill_MapsFixedSelectionOrderToAllFiveElements()
        {
            BoardState board = CreateFullBoard();
            ClearTop(board, 0, BoardConstants.Height);
            var random = new RecordingRandomSource(new[] { 0, 1, 2, 3, 4 });
            ElementType[] expectedElements =
            {
                ElementType.Fire,
                ElementType.Water,
                ElementType.Grass,
                ElementType.Light,
                ElementType.Dark
            };

            BoardRefillResult result =
                CreateRefiller(random, 100).Refill(board);

            for (int index = 0; index < expectedElements.Length; index++)
            {
                BoardBlock block = result.Spawns[index].Block;
                Assert.That(block.BlockType, Is.EqualTo(BoardBlockType.Normal));
                Assert.That(block.Element.HasValue, Is.True);
                Assert.That(block.Element, Is.EqualTo(expectedElements[index]));
                Assert.That(random.MinimumArguments[index], Is.Zero);
                Assert.That(random.MaximumArguments[index], Is.EqualTo(5));
            }
        }

        [Test]
        public void Refill_AssignsPositiveUniqueIdsWithoutChangingExistingIds()
        {
            BoardState board = CreateFullBoard();
            ClearTop(board, 0, 2);
            ClearTop(board, 5, 1);
            BoardBlock[] existingBlocks = CaptureBoardContents(board);
            var random = new RecordingRandomSource(
                CreateRepeatedSequence(3, 0));

            BoardRefillResult result =
                CreateRefiller(random, 31).Refill(board);
            var ids = new HashSet<long>();

            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardBlock block = result.Board.Get(
                    BoardPosition.FromIndex(index));
                Assert.That(block.RuntimeId, Is.GreaterThan(0));
                Assert.That(ids.Add(block.RuntimeId), Is.True);
            }

            AssertExistingReferencesArePreserved(result.Board, existingBlocks);
        }

        [Test]
        public void Refill_IssuesSequentialIdsAndConsumesExactlyOnePerEmptyCell()
        {
            BoardState board = CreateFullBoard();
            ClearTop(board, 2, 3);
            var random = new RecordingRandomSource(new[] { 4, 3, 2 });
            var idGenerator = new BoardBlockIdGenerator(500);
            var refiller = new BoardRefiller(random, idGenerator);

            BoardRefillResult result = refiller.Refill(board);

            Assert.That(result.Spawns[0].RuntimeId, Is.EqualTo(500));
            Assert.That(result.Spawns[1].RuntimeId, Is.EqualTo(501));
            Assert.That(result.Spawns[2].RuntimeId, Is.EqualTo(502));
            Assert.That(random.CallCount, Is.EqualTo(3));
            Assert.That(idGenerator.Next(), Is.EqualTo(503));
        }

        [Test]
        public void Refill_SameRandomSequenceAndIdsProduceDeterministicResult()
        {
            BoardState board = CreateFullBoard();
            ClearTop(board, 0, 2);
            ClearTop(board, 3, 1);
            int[] sequence = { 4, 1, 3 };

            BoardRefillResult first = CreateRefiller(
                new RecordingRandomSource(sequence),
                100).Refill(board);
            BoardRefillResult second = CreateRefiller(
                new RecordingRandomSource(sequence),
                100).Refill(board);

            AssertBoardsHaveEquivalentData(first.Board, second.Board);
            for (int index = 0; index < first.Spawns.Count; index++)
            {
                Assert.That(
                    second.Spawns[index].RuntimeId,
                    Is.EqualTo(first.Spawns[index].RuntimeId));
                Assert.That(
                    second.Spawns[index].Target,
                    Is.EqualTo(first.Spawns[index].Target));
                Assert.That(
                    second.Spawns[index].SourceY,
                    Is.EqualTo(first.Spawns[index].SourceY));
            }
        }

        [Test]
        public void Refill_AllowsNewBlocksToCreateImmediateMatchWithoutReroll()
        {
            BoardState board = CreateFullBoardWithoutMatches();
            ClearTop(board, 0, 3);
            var random = new RecordingRandomSource(new[] { 0, 0, 0 });

            BoardRefillResult result =
                CreateRefiller(random, 100).Refill(board);
            IReadOnlyList<BoardMatch> matches =
                BoardMatchFinder.FindMatches(result.Board);

            Assert.That(random.CallCount, Is.EqualTo(3));
            Assert.That(result.Spawns, Has.Count.EqualTo(3));
            foreach (BoardBlockSpawn spawn in result.Spawns)
            {
                Assert.That(spawn.Block.Element, Is.EqualTo(ElementType.Fire));
            }

            Assert.That(
                ContainsMatchAt(
                    matches,
                    new BoardPosition(0, 2),
                    new BoardPosition(0, 3),
                    new BoardPosition(0, 4)),
                Is.True);
        }

        [Test]
        public void Refill_NullBoardThrows()
        {
            var refiller = CreateRefiller(
                new RecordingRandomSource(Array.Empty<int>()),
                1);

            Assert.Throws<ArgumentNullException>(() => refiller.Refill(null));
        }

        [Test]
        public void Constructor_NullRandomSourceThrows()
        {
            Assert.Throws<ArgumentNullException>(
                () => new BoardRefiller(
                    null,
                    new BoardBlockIdGenerator()));
        }

        [Test]
        public void Constructor_NullIdGeneratorThrows()
        {
            Assert.Throws<ArgumentNullException>(
                () => new BoardRefiller(
                    new RecordingRandomSource(Array.Empty<int>()),
                    null));
        }

        [Test]
        public void Refill_NonCompactedColumnThrowsBeforeConsumingDependencies()
        {
            BoardState board = CreateFullBoard();
            var gap = new BoardPosition(2, 2);
            board.Clear(gap);
            BoardBlock[] originalContents = CaptureBoardContents(board);
            var random = new RecordingRandomSource(Array.Empty<int>());
            var idGenerator = new BoardBlockIdGenerator(100);
            var refiller = new BoardRefiller(random, idGenerator);

            Assert.Throws<InvalidOperationException>(
                () => refiller.Refill(board));
            Assert.That(random.CallCount, Is.Zero);
            Assert.That(idGenerator.Next(), Is.EqualTo(100));
            AssertBoardContainsReferences(board, originalContents);
        }

        [Test]
        public void Refill_DoesNotChangeInputBoardAndReturnsIndependentBoard()
        {
            BoardState board = CreateFullBoard();
            ClearTop(board, 3, 2);
            BoardBlock[] originalContents = CaptureBoardContents(board);
            var random = new RecordingRandomSource(new[] { 0, 1 });

            BoardRefillResult result =
                CreateRefiller(random, 100).Refill(board);

            Assert.That(result.Board, Is.Not.SameAs(board));
            AssertBoardContainsReferences(board, originalContents);

            result.Board.Clear(new BoardPosition(0, 0));
            Assert.That(board.Get(new BoardPosition(0, 0)), Is.Not.Null);
        }

        [Test]
        public void Refill_SpawnCollectionCannotBeModifiedExternally()
        {
            BoardState board = CreateFullBoard();
            ClearTop(board, 0, 1);
            BoardRefillResult result = CreateRefiller(
                new RecordingRandomSource(new[] { 0 }),
                100).Refill(board);
            var spawns = result.Spawns as IList<BoardBlockSpawn>;

            Assert.That(spawns, Is.Not.Null);
            Assert.That(spawns.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => spawns.RemoveAt(0));
        }

        private static BoardRefiller CreateRefiller(
            RecordingRandomSource randomSource,
            int firstId)
        {
            return new BoardRefiller(
                randomSource,
                new BoardBlockIdGenerator(firstId));
        }

        private static BoardState CreateFullBoard()
        {
            var board = new BoardState();
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                board.Set(
                    position,
                    new BoardBlock(
                        index + 1,
                        BoardBlockType.Normal,
                        (ElementType)(index % 5)));
            }

            return board;
        }

        private static BoardState CreateFullBoardWithoutMatches()
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

            return board;
        }

        private static void ClearTop(
            BoardState board,
            int x,
            int emptyCount)
        {
            int firstEmptyY = BoardConstants.Height - emptyCount;
            for (int y = firstEmptyY; y < BoardConstants.Height; y++)
            {
                board.Clear(new BoardPosition(x, y));
            }
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

        private static void AssertExistingReferencesArePreserved(
            BoardState board,
            BoardBlock[] originalContents)
        {
            for (int index = 0; index < originalContents.Length; index++)
            {
                if (originalContents[index] == null)
                {
                    continue;
                }

                Assert.That(
                    board.Get(BoardPosition.FromIndex(index)),
                    Is.SameAs(originalContents[index]));
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

        private static bool ContainsMatchAt(
            IReadOnlyList<BoardMatch> matches,
            params BoardPosition[] expectedPositions)
        {
            for (int matchIndex = 0;
                matchIndex < matches.Count;
                matchIndex++)
            {
                BoardMatch match = matches[matchIndex];
                if (match.Element != ElementType.Fire
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
                MinimumArguments = new List<int>();
                MaximumArguments = new List<int>();
            }

            public int CallCount => MinimumArguments.Count;
            public List<int> MinimumArguments { get; }
            public List<int> MaximumArguments { get; }

            public int Next(int minInclusive, int maxExclusive)
            {
                MinimumArguments.Add(minInclusive);
                MaximumArguments.Add(maxExclusive);

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
