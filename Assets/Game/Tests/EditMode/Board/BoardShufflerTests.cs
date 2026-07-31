using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board
{
    public sealed class BoardShufflerTests
    {
        private static readonly int[] DeadBoardElements =
        {
            3, 0, 2, 1, 0,
            0, 2, 3, 1, 2,
            4, 4, 0, 2, 4,
            1, 2, 0, 0, 4,
            0, 1, 2, 1, 2,
            0, 4, 4, 0, 3
        };

        private static readonly int[] PlayableBoardElements =
        {
            0, 0, 1, 4, 0,
            4, 0, 4, 3, 4,
            1, 3, 0, 3, 1,
            0, 4, 1, 1, 4,
            0, 0, 2, 4, 4,
            1, 2, 4, 0, 0
        };

        private static readonly int[] SuccessfulPermutationElements =
        {
            0, 4, 3, 2, 2,
            4, 2, 2, 0, 1,
            3, 2, 1, 1, 0,
            4, 0, 4, 2, 0,
            4, 1, 3, 0, 2,
            0, 1, 0, 4, 0
        };

        private static readonly int[] FourAttemptPermutationSequence =
        {
            20, 3, 0, 23, 8, 7, 7, 4, 3, 17, 2, 18, 13, 1, 0,
            1, 3, 3, 8, 9, 0, 8, 3, 5, 5, 4, 3, 0, 1,
            18, 8, 25, 0, 24, 5, 22, 13, 10, 8, 4, 6, 10, 3, 2,
            6, 1, 5, 5, 9, 4, 0, 7, 4, 0, 3, 0, 2, 1,
            26, 20, 19, 11, 18, 6, 22, 2, 1, 7, 9, 2, 7, 3, 12,
            4, 7, 10, 5, 2, 5, 5, 3, 5, 2, 0, 1, 2, 0,
            5, 14, 12, 8, 20, 22, 17, 7, 21, 10, 1, 7, 1, 10, 12,
            4, 1, 3, 9, 5, 3, 7, 6, 5, 3, 1, 2, 0, 0
        };

        [Test]
        public void EnsurePlayable_PlayableBoardReturnsIndependentUnchangedBoard()
        {
            BoardState board = CreateBoard(PlayableBoardElements);
            BoardBlock[] originalContents = CaptureBoardContents(board);
            var random = new RecordingRandomSource(Array.Empty<int>());

            BoardShuffleResult result =
                CreateShuffler(random).EnsurePlayable(board);

            Assert.That(result.Kind, Is.EqualTo(BoardShuffleKind.None));
            Assert.That(result.WasShuffled, Is.False);
            Assert.That(result.AttemptCount, Is.Zero);
            Assert.That(result.Entries, Is.Empty);
            Assert.That(random.CallCount, Is.Zero);
            Assert.That(result.Board, Is.Not.SameAs(board));
            AssertBoardContainsReferences(result.Board, originalContents);
            AssertBoardContainsReferences(board, originalContents);
        }

        [Test]
        public void EnsurePlayable_DeadBoardReturnsPlayablePermutation()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            var random = new RecordingRandomSource(
                FourAttemptPermutationSequence);

            BoardShuffleResult result = CreateShuffler(
                random,
                maxPermutationAttempts: 4).EnsurePlayable(board);

            Assert.That(result.Kind, Is.EqualTo(BoardShuffleKind.Permutation));
            Assert.That(result.WasShuffled, Is.True);
            Assert.That(result.AttemptCount, Is.EqualTo(4));
            Assert.That(BoardMatchFinder.FindMatches(result.Board), Is.Empty);
            Assert.That(
                new BoardMoveAnalyzer().HasAnyValidSwap(result.Board),
                Is.True);
        }

        [Test]
        public void EnsurePlayable_PermutationPreservesElementCounts()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            var random = new RecordingRandomSource(
                FourAttemptPermutationSequence);

            BoardShuffleResult result = CreateShuffler(
                random,
                maxPermutationAttempts: 4).EnsurePlayable(board);

            AssertElementCountsEqual(board, result.Board);
        }

        [Test]
        public void EnsurePlayable_FisherYatesUsesExactRangesAndExpectedPermutation()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            var random = new RecordingRandomSource(
                FourAttemptPermutationSequence);

            BoardShuffleResult result = CreateShuffler(
                random,
                maxPermutationAttempts: 4).EnsurePlayable(board);

            Assert.That(random.CallCount, Is.EqualTo(4 * 29));
            for (int attempt = 0; attempt < 4; attempt++)
            {
                for (int offset = 0; offset < 29; offset++)
                {
                    int callIndex = (attempt * 29) + offset;
                    Assert.That(
                        random.MinimumArguments[callIndex],
                        Is.Zero);
                    Assert.That(
                        random.MaximumArguments[callIndex],
                        Is.EqualTo(30 - offset));
                }
            }

            AssertBoardElements(
                result.Board,
                SuccessfulPermutationElements);
        }

        [Test]
        public void EnsurePlayable_EachPermutationAttemptStartsFromOriginalElements()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            var random = new RecordingRandomSource(
                FourAttemptPermutationSequence);

            BoardShuffleResult result = CreateShuffler(
                random,
                maxPermutationAttempts: 4).EnsurePlayable(board);

            AssertBoardElements(
                result.Board,
                SuccessfulPermutationElements);
            for (int index = 0; index < result.Entries.Count; index++)
            {
                Assert.That(
                    result.Entries[index].PreviousElement,
                    Is.EqualTo((ElementType)DeadBoardElements[index]));
            }
        }

        [Test]
        public void EnsurePlayable_SameSeedAndInputProduceSameResult()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            var firstShuffler = new BoardShuffler(
                new SeededRandomSource(20260731),
                new BoardMoveAnalyzer());
            var secondShuffler = new BoardShuffler(
                new SeededRandomSource(20260731),
                new BoardMoveAnalyzer());

            BoardShuffleResult first = firstShuffler.EnsurePlayable(board);
            BoardShuffleResult second = secondShuffler.EnsurePlayable(board);

            Assert.That(second.Kind, Is.EqualTo(first.Kind));
            Assert.That(second.AttemptCount, Is.EqualTo(first.AttemptCount));
            AssertBoardsHaveEquivalentData(first.Board, second.Board);
        }

        [Test]
        public void EnsurePlayable_PreservesNormalPositionsAndRuntimeIds()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            var random = new RecordingRandomSource(
                FourAttemptPermutationSequence);

            BoardShuffleResult result = CreateShuffler(
                random,
                maxPermutationAttempts: 4).EnsurePlayable(board);

            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    BoardBlock before = board.Get(position);
                    BoardBlock after = result.Board.Get(position);
                    Assert.That(after.RuntimeId, Is.EqualTo(before.RuntimeId));
                    Assert.That(after.BlockType, Is.EqualTo(BoardBlockType.Normal));
                }
            }
        }

        [Test]
        public void EnsurePlayable_PreservesNonNormalBlocksAndExcludesTheirEntries()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            BoardBlock rock = ReplaceWithFixedBlock(
                board,
                0,
                0,
                BoardBlockType.Rock,
                null);
            BoardBlock special = ReplaceWithFixedBlock(
                board,
                1,
                0,
                BoardBlockType.Special,
                ElementType.Light);
            BoardBlock locked = ReplaceWithFixedBlock(
                board,
                2,
                0,
                BoardBlockType.Locked,
                ElementType.Dark);
            int[] regenerationSequence =
                CollectNormalTargetElements(board, PlayableBoardElements);
            var random = new RecordingRandomSource(
                Combine(
                    CreateIdentityPermutationSequence(27),
                    regenerationSequence));

            BoardShuffleResult result = CreateShuffler(
                random,
                maxPermutationAttempts: 1,
                maxRegenerationAttempts: 1).EnsurePlayable(board);

            Assert.That(result.Kind, Is.EqualTo(BoardShuffleKind.Regeneration));
            Assert.That(result.Board.Get(new BoardPosition(0, 0)), Is.SameAs(rock));
            Assert.That(result.Board.Get(new BoardPosition(1, 0)), Is.SameAs(special));
            Assert.That(result.Board.Get(new BoardPosition(2, 0)), Is.SameAs(locked));
            Assert.That(result.Entries, Has.Count.EqualTo(27));
            Assert.That(
                ContainsEntryAt(result.Entries, new BoardPosition(0, 0)),
                Is.False);
            Assert.That(
                ContainsEntryAt(result.Entries, new BoardPosition(1, 0)),
                Is.False);
            Assert.That(
                ContainsEntryAt(result.Entries, new BoardPosition(2, 0)),
                Is.False);
        }

        [Test]
        public void EnsurePlayable_EntriesRecordEveryNormalInCollectionOrder()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            var random = new RecordingRandomSource(
                FourAttemptPermutationSequence);

            BoardShuffleResult result = CreateShuffler(
                random,
                maxPermutationAttempts: 4).EnsurePlayable(board);

            Assert.That(result.Entries, Has.Count.EqualTo(30));
            int entryIndex = 0;
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    BoardShuffleEntry entry = result.Entries[entryIndex];
                    BoardPosition position = new BoardPosition(x, y);
                    BoardBlock resultBlock = result.Board.Get(position);

                    Assert.That(entry.Position, Is.EqualTo(position));
                    Assert.That(
                        entry.RuntimeId,
                        Is.EqualTo(board.Get(position).RuntimeId));
                    Assert.That(
                        entry.PreviousElement,
                        Is.EqualTo((ElementType)DeadBoardElements[entryIndex]));
                    Assert.That(
                        entry.NewElement,
                        Is.EqualTo(resultBlock.Element));
                    Assert.That(
                        entry.Changed,
                        Is.EqualTo(
                            entry.PreviousElement != entry.NewElement));
                    entryIndex++;
                }
            }
        }

        [Test]
        public void EnsurePlayable_EntriesCannotBeModifiedExternally()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            var random = new RecordingRandomSource(
                FourAttemptPermutationSequence);

            BoardShuffleResult result = CreateShuffler(
                random,
                maxPermutationAttempts: 4).EnsurePlayable(board);
            var entries = result.Entries as IList<BoardShuffleEntry>;

            Assert.That(entries, Is.Not.Null);
            Assert.That(entries.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => entries.RemoveAt(0));
        }

        [Test]
        public void EnsurePlayable_FallsBackToRegenerationAfterPermutationFailure()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            var random = new RecordingRandomSource(
                Combine(
                    CreateIdentityPermutationSequence(30),
                    PlayableBoardElements));

            BoardShuffleResult result = CreateShuffler(
                random,
                maxPermutationAttempts: 1,
                maxRegenerationAttempts: 1).EnsurePlayable(board);

            Assert.That(result.Kind, Is.EqualTo(BoardShuffleKind.Regeneration));
            Assert.That(result.AttemptCount, Is.EqualTo(2));
            Assert.That(random.CallCount, Is.EqualTo(29 + 30));
            AssertBoardElements(result.Board, PlayableBoardElements);
            Assert.That(BoardMatchFinder.FindMatches(result.Board), Is.Empty);
            Assert.That(
                new BoardMoveAnalyzer().HasAnyValidSwap(result.Board),
                Is.True);
        }

        [Test]
        public void EnsurePlayable_RegenerationUsesFixedFiveElementRange()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            var random = new RecordingRandomSource(
                Combine(
                    CreateIdentityPermutationSequence(30),
                    PlayableBoardElements));

            CreateShuffler(
                random,
                maxPermutationAttempts: 1,
                maxRegenerationAttempts: 1).EnsurePlayable(board);

            for (int index = 29; index < 59; index++)
            {
                Assert.That(random.MinimumArguments[index], Is.Zero);
                Assert.That(random.MaximumArguments[index], Is.EqualTo(5));
            }
        }

        [Test]
        public void EnsurePlayable_RegenerationMayChangeElementCounts()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            var random = new RecordingRandomSource(
                Combine(
                    CreateIdentityPermutationSequence(30),
                    PlayableBoardElements));

            BoardShuffleResult result = CreateShuffler(
                random,
                maxPermutationAttempts: 1,
                maxRegenerationAttempts: 1).EnsurePlayable(board);

            Assert.That(
                GetElementCounts(result.Board),
                Is.Not.EqualTo(GetElementCounts(board)));
        }

        [Test]
        public void EnsurePlayable_StructurallyImpossibleBoardThrows()
        {
            BoardState board = CreateAllFixedBoard(BoardBlockType.Rock);
            var random = new RecordingRandomSource(Array.Empty<int>());
            var shuffler = CreateShuffler(
                random,
                maxPermutationAttempts: 2,
                maxRegenerationAttempts: 2);

            Assert.Throws<InvalidOperationException>(
                () => shuffler.EnsurePlayable(board));
            Assert.That(random.CallCount, Is.Zero);
        }

        [Test]
        public void EnsurePlayable_AllCandidateAttemptsFailThrowsAndKeepsInput()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            BoardBlock[] originalContents = CaptureBoardContents(board);
            var random = new RecordingRandomSource(
                Combine(
                    CreateIdentityPermutationSequence(30),
                    DeadBoardElements));
            var shuffler = CreateShuffler(
                random,
                maxPermutationAttempts: 1,
                maxRegenerationAttempts: 1);

            Assert.Throws<InvalidOperationException>(
                () => shuffler.EnsurePlayable(board));
            AssertBoardContainsReferences(board, originalContents);
        }

        [Test]
        public void EnsurePlayable_EmptyCellThrowsBeforeRandomConsumption()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            board.Clear(new BoardPosition(3, 2));
            var random = new RecordingRandomSource(Array.Empty<int>());

            Assert.Throws<InvalidOperationException>(
                () => CreateShuffler(random).EnsurePlayable(board));
            Assert.That(random.CallCount, Is.Zero);
        }

        [Test]
        public void EnsurePlayable_ExistingMatchThrowsBeforeRandomConsumption()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            SetElement(board, 0, 0, ElementType.Fire);
            SetElement(board, 1, 0, ElementType.Fire);
            SetElement(board, 2, 0, ElementType.Fire);
            var random = new RecordingRandomSource(Array.Empty<int>());

            Assert.Throws<InvalidOperationException>(
                () => CreateShuffler(random).EnsurePlayable(board));
            Assert.That(random.CallCount, Is.Zero);
        }

        [Test]
        public void EnsurePlayable_NullBoardThrows()
        {
            var shuffler = CreateShuffler(
                new RecordingRandomSource(Array.Empty<int>()));

            Assert.Throws<ArgumentNullException>(
                () => shuffler.EnsurePlayable(null));
        }

        [Test]
        public void Constructor_NullRandomSourceThrows()
        {
            Assert.Throws<ArgumentNullException>(
                () => new BoardShuffler(
                    null,
                    new BoardMoveAnalyzer()));
        }

        [Test]
        public void Constructor_NullMatcherThrows()
        {
            Assert.Throws<ArgumentNullException>(
                () => new BoardShuffler(
                    new RecordingRandomSource(Array.Empty<int>()),
                    null,
                    new BoardMoveAnalyzer()));
        }

        [Test]
        public void Constructor_NullMoveAnalyzerThrows()
        {
            Assert.Throws<ArgumentNullException>(
                () => new BoardShuffler(
                    new RecordingRandomSource(Array.Empty<int>()),
                    null));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_NonPositivePermutationAttemptsThrows(
            int maxPermutationAttempts)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BoardShuffler(
                    new RecordingRandomSource(Array.Empty<int>()),
                    new BoardMoveAnalyzer(),
                    maxPermutationAttempts,
                    1));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_NonPositiveRegenerationAttemptsThrows(
            int maxRegenerationAttempts)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BoardShuffler(
                    new RecordingRandomSource(Array.Empty<int>()),
                    new BoardMoveAnalyzer(),
                    1,
                    maxRegenerationAttempts));
        }

        [Test]
        public void EnsurePlayable_NullMatcherResultThrows()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            var shuffler = new BoardShuffler(
                new RecordingRandomSource(Array.Empty<int>()),
                candidate => null,
                new BoardMoveAnalyzer());

            Assert.Throws<InvalidOperationException>(
                () => shuffler.EnsurePlayable(board));
        }

        [Test]
        public void EnsurePlayable_DoesNotChangeOriginalBoard()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            BoardBlock[] originalContents = CaptureBoardContents(board);
            var random = new RecordingRandomSource(
                FourAttemptPermutationSequence);

            CreateShuffler(
                random,
                maxPermutationAttempts: 4).EnsurePlayable(board);

            AssertBoardContainsReferences(board, originalContents);
        }

        private static BoardShuffler CreateShuffler(
            RecordingRandomSource random,
            int maxPermutationAttempts =
                BoardShuffler.DefaultMaxPermutationAttempts,
            int maxRegenerationAttempts =
                BoardShuffler.DefaultMaxRegenerationAttempts)
        {
            return new BoardShuffler(
                random,
                new BoardMoveAnalyzer(),
                maxPermutationAttempts,
                maxRegenerationAttempts);
        }

        private static BoardState CreateBoard(int[] elements)
        {
            var board = new BoardState();

            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    board.Set(
                        position,
                        new BoardBlock(
                            position.ToIndex() + 1,
                            BoardBlockType.Normal,
                            (ElementType)elements[(x * BoardConstants.Height) + y]));
                }
            }

            Assert.That(BoardMatchFinder.FindMatches(board), Is.Empty);
            return board;
        }

        private static BoardState CreateAllFixedBoard(BoardBlockType blockType)
        {
            var board = new BoardState();
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                board.Set(
                    BoardPosition.FromIndex(index),
                    new BoardBlock(index + 1, blockType, null));
            }

            return board;
        }

        private static BoardBlock ReplaceWithFixedBlock(
            BoardState board,
            int x,
            int y,
            BoardBlockType blockType,
            ElementType? element)
        {
            var position = new BoardPosition(x, y);
            long runtimeId = board.Get(position).RuntimeId;
            var block = new BoardBlock(runtimeId, blockType, element);
            board.Set(position, block);
            return block;
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
                new BoardBlock(runtimeId, BoardBlockType.Normal, element));
        }

        private static int[] CreateIdentityPermutationSequence(int count)
        {
            var sequence = new int[Math.Max(0, count - 1)];
            for (int index = count - 1; index >= 1; index--)
            {
                sequence[count - 1 - index] = index;
            }

            return sequence;
        }

        private static int[] CollectNormalTargetElements(
            BoardState board,
            int[] targetElements)
        {
            var values = new List<int>();
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    if (board.Get(new BoardPosition(x, y)).BlockType
                        == BoardBlockType.Normal)
                    {
                        values.Add(
                            targetElements[(x * BoardConstants.Height) + y]);
                    }
                }
            }

            return values.ToArray();
        }

        private static int[] Combine(params int[][] sequences)
        {
            int totalCount = 0;
            foreach (int[] sequence in sequences)
            {
                totalCount += sequence.Length;
            }

            var combined = new int[totalCount];
            int writeIndex = 0;
            foreach (int[] sequence in sequences)
            {
                Array.Copy(
                    sequence,
                    0,
                    combined,
                    writeIndex,
                    sequence.Length);
                writeIndex += sequence.Length;
            }

            return combined;
        }

        private static bool ContainsEntryAt(
            IReadOnlyList<BoardShuffleEntry> entries,
            BoardPosition position)
        {
            foreach (BoardShuffleEntry entry in entries)
            {
                if (entry.Position == position)
                {
                    return true;
                }
            }

            return false;
        }

        private static int[] GetElementCounts(BoardState board)
        {
            var counts = new int[5];
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardBlock block = board.Get(BoardPosition.FromIndex(index));
                if (block.BlockType == BoardBlockType.Normal)
                {
                    counts[(int)block.Element.Value]++;
                }
            }

            return counts;
        }

        private static void AssertElementCountsEqual(
            BoardState expected,
            BoardState actual)
        {
            CollectionAssert.AreEqual(
                GetElementCounts(expected),
                GetElementCounts(actual));
        }

        private static void AssertBoardElements(
            BoardState board,
            int[] expectedElements)
        {
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    Assert.That(
                        board.Get(new BoardPosition(x, y)).Element,
                        Is.EqualTo(
                            (ElementType)expectedElements[
                                (x * BoardConstants.Height) + y]));
                }
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
