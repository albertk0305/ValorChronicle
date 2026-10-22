using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board
{
    public sealed class BoardElementConversionResolverTests
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

        private static readonly int[] ValidFourSelection = { 0, 1, 2, 4 };
        private static readonly int[] ValidFiveSelection = { 0, 1, 2, 3, 4 };

        [Test]
        public void Resolve_SelectsOnlyNonTargetNormalBlocks()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            BoardBlock special = ReplaceBlock(
                board,
                5,
                3,
                BoardBlockType.Special,
                ElementType.Fire);
            BoardBlock locked = ReplaceBlock(
                board,
                5,
                4,
                BoardBlockType.Locked,
                ElementType.Dark);

            BoardElementConversionResult result = CreateResolver(
                new SequenceRandomSource(ValidFourSelection)).Resolve(
                    board,
                    ElementType.Water,
                    4);

            Assert.That(result.Succeeded, Is.True);
            foreach (BoardElementConversionEntry entry in result.Entries)
            {
                Assert.That(entry.BlockType, Is.EqualTo(BoardBlockType.Normal));
                Assert.That(entry.PreviousElement, Is.Not.EqualTo(ElementType.Water));
                Assert.That(entry.NewElement, Is.EqualTo(ElementType.Water));
            }

            Assert.That(result.Board.Get(new BoardPosition(5, 3)), Is.SameAs(special));
            Assert.That(result.Board.Get(new BoardPosition(5, 4)), Is.SameAs(locked));
        }

        [Test]
        public void Resolve_ConvertedPositionsContainNoDuplicates()
        {
            BoardElementConversionResult result = ResolveValidFour();
            var positions = new HashSet<BoardPosition>();

            foreach (BoardElementConversionEntry entry in result.Entries)
            {
                Assert.That(positions.Add(entry.Position), Is.True);
            }

            Assert.That(positions, Has.Count.EqualTo(4));
        }

        [Test]
        public void Resolve_RequestFourConvertsExactlyFourBlocks()
        {
            BoardElementConversionResult result = ResolveValidFour();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RequestedCount, Is.EqualTo(4));
            Assert.That(result.ConvertedCount, Is.EqualTo(4));
        }

        [Test]
        public void Resolve_RequestFiveConvertsExactlyFiveBlocks()
        {
            BoardState board = CreateBoard(DeadBoardElements);

            BoardElementConversionResult result = CreateResolver(
                new SequenceRandomSource(ValidFiveSelection)).Resolve(
                    board,
                    ElementType.Water,
                    5);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RequestedCount, Is.EqualTo(5));
            Assert.That(result.ConvertedCount, Is.EqualTo(5));
        }

        [Test]
        public void Resolve_OnlyTwoCandidatesEvaluatesBothAndFailsAsUnplayable()
        {
            BoardState board = CreateUniformBoard(ElementType.Water);
            SetElement(board, 0, 0, ElementType.Fire);
            SetElement(board, 1, 0, ElementType.Grass);

            BoardElementConversionResult result = CreateResolver(
                new SequenceRandomSource(0, 1),
                maxRandomSelectionAttempts: 1).Resolve(
                    board,
                    ElementType.Water,
                    4);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(BoardElementConversionFailure.NoPlayableCombination));
            Assert.That(result.DeterministicCombinationCount, Is.EqualTo(1));
            Assert.That(result.ConvertedCount, Is.Zero);
        }

        [Test]
        public void Resolve_NoCandidatesFailsWithoutRandomConsumption()
        {
            BoardState board = CreateUniformBoard(ElementType.Water);
            var random = new SequenceRandomSource();

            BoardElementConversionResult result = CreateResolver(random).Resolve(
                board,
                ElementType.Water,
                4);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(BoardElementConversionFailure.NoCandidates));
            Assert.That(result.ConvertedCount, Is.Zero);
            Assert.That(result.RandomAttemptCount, Is.Zero);
            Assert.That(random.CallCount, Is.Zero);
        }

        [Test]
        public void Resolve_ExcludesTargetElementAndRockBlocks()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            BoardBlock rock = ReplaceBlock(
                board,
                0,
                0,
                BoardBlockType.Rock,
                element: null);
            BoardBlock water = board.Get(new BoardPosition(0, 3));

            BoardElementConversionResult result = CreateResolver(
                new SequenceRandomSource(ValidFourSelection)).Resolve(
                    board,
                    ElementType.Water,
                    4);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Board.Get(new BoardPosition(0, 0)), Is.SameAs(rock));
            Assert.That(result.Board.Get(new BoardPosition(0, 3)), Is.SameAs(water));
            Assert.That(ContainsPosition(result.Entries, new BoardPosition(0, 0)), Is.False);
            Assert.That(ContainsPosition(result.Entries, new BoardPosition(0, 3)), Is.False);
        }

        [Test]
        public void Resolve_PreservesRuntimeIdPositionAndBlockType()
        {
            BoardState board = CreateBoard(DeadBoardElements);

            BoardElementConversionResult result = CreateResolver(
                new SequenceRandomSource(ValidFourSelection)).Resolve(
                    board,
                    ElementType.Water,
                    4);

            foreach (BoardElementConversionEntry entry in result.Entries)
            {
                BoardBlock before = board.Get(entry.Position);
                BoardBlock after = result.Board.Get(entry.Position);
                Assert.That(entry.RuntimeId, Is.EqualTo(before.RuntimeId));
                Assert.That(after.RuntimeId, Is.EqualTo(before.RuntimeId));
                Assert.That(after.BlockType, Is.EqualTo(before.BlockType));
                Assert.That(after.Element, Is.EqualTo(ElementType.Water));
            }
        }

        [Test]
        public void Resolve_CompletedMatchesRemainOnResultBoard()
        {
            BoardElementConversionResult result = ResolveValidFour();

            Assert.That(BoardMatchFinder.FindMatches(result.Board), Is.Not.Empty);
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                Assert.That(
                    result.Board.IsOccupied(BoardPosition.FromIndex(index)),
                    Is.True);
            }
        }

        [Test]
        public void Resolve_FirstSelectionRemovesValidSwapThenRetries()
        {
            BoardState board = CreateBoard(PlayableBoardElements);
            Assert.That(new BoardMoveAnalyzer().HasAnyValidSwap(board), Is.True);
            var random = new SequenceRandomSource(
                1, 2, 14, 19,
                0, 1, 2, 3);

            BoardElementConversionResult result = CreateResolver(
                random,
                maxRandomSelectionAttempts: 2).Resolve(
                    board,
                    ElementType.Water,
                    4);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RandomAttemptCount, Is.EqualTo(2));
            Assert.That(result.UsedDeterministicSearch, Is.False);
            Assert.That(random.CallCount, Is.EqualTo(8));
            Assert.That(new BoardMoveAnalyzer().HasAnyValidSwap(result.Board), Is.True);
        }

        [Test]
        public void Resolve_RandomAttemptsFailThenDeterministicSearchFindsCombination()
        {
            BoardState board = CreateBoard(DeadBoardElements);

            BoardElementConversionResult result = CreateResolver(
                new SequenceRandomSource(0, 1, 2, 3),
                maxRandomSelectionAttempts: 1).Resolve(
                    board,
                    ElementType.Water,
                    4);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RandomAttemptCount, Is.EqualTo(1));
            Assert.That(result.UsedDeterministicSearch, Is.True);
            Assert.That(result.DeterministicCombinationCount, Is.EqualTo(2));
            Assert.That(new BoardMoveAnalyzer().HasAnyValidSwap(result.Board), Is.True);
        }

        [Test]
        public void Resolve_NoPlayableCombinationReturnsExplicitFailure()
        {
            BoardState board = CreateUniformBoard(ElementType.Water);
            SetElement(board, 0, 0, ElementType.Fire);

            BoardElementConversionResult result = CreateResolver(
                new SequenceRandomSource(0),
                maxRandomSelectionAttempts: 1).Resolve(
                    board,
                    ElementType.Water,
                    4);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(BoardElementConversionFailure.NoPlayableCombination));
            Assert.That(result.Entries, Is.Empty);
            Assert.That(result.UsedDeterministicSearch, Is.True);
        }

        [Test]
        public void Resolve_FailureLeavesOriginalBoardUnchanged()
        {
            BoardState board = CreateUniformBoard(ElementType.Water);
            SetElement(board, 0, 0, ElementType.Fire);
            BoardBlock[] original = CaptureBoardContents(board);

            BoardElementConversionResult result = CreateResolver(
                new SequenceRandomSource(0),
                maxRandomSelectionAttempts: 1).Resolve(
                    board,
                    ElementType.Water,
                    4);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Board, Is.Not.SameAs(board));
            AssertBoardContainsReferences(board, original);
            AssertBoardContainsReferences(result.Board, original);
        }

        [Test]
        public void Resolve_SameSeedAndInputProduceSameResult()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            var firstResolver = new BoardElementConversionResolver(
                new SeededRandomSource(20261021),
                new BoardMoveAnalyzer());
            var secondResolver = new BoardElementConversionResolver(
                new SeededRandomSource(20261021),
                new BoardMoveAnalyzer());

            BoardElementConversionResult first = firstResolver.Resolve(
                board,
                ElementType.Water,
                4);
            BoardElementConversionResult second = secondResolver.Resolve(
                board,
                ElementType.Water,
                4);

            Assert.That(second.Succeeded, Is.EqualTo(first.Succeeded));
            Assert.That(second.Failure, Is.EqualTo(first.Failure));
            Assert.That(second.RandomAttemptCount, Is.EqualTo(first.RandomAttemptCount));
            Assert.That(second.Entries, Has.Count.EqualTo(first.Entries.Count));
            for (int index = 0; index < first.Entries.Count; index++)
            {
                Assert.That(
                    second.Entries[index].Position,
                    Is.EqualTo(first.Entries[index].Position));
            }
        }

        [Test]
        public void Resolve_ExistingMatchDoesNotMakeUnrelatedSwapValid()
        {
            BoardElementConversionResult result = ResolveValidFour();
            var analyzer = new BoardMoveAnalyzer();

            Assert.That(BoardMatchFinder.FindMatches(result.Board), Is.Not.Empty);
            Assert.That(
                analyzer.IsValidSwap(
                    result.Board,
                    new BoardPosition(5, 3),
                    new BoardPosition(5, 4)),
                Is.False);
        }

        private static BoardElementConversionResult ResolveValidFour()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            return CreateResolver(
                new SequenceRandomSource(ValidFourSelection)).Resolve(
                    board,
                    ElementType.Water,
                    4);
        }

        private static BoardElementConversionResolver CreateResolver(
            IRandomSource randomSource,
            int maxRandomSelectionAttempts =
                BoardElementConversionResolver.DefaultMaxRandomSelectionAttempts)
        {
            return new BoardElementConversionResolver(
                randomSource,
                new BoardMoveAnalyzer(),
                maxRandomSelectionAttempts);
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

            return board;
        }

        private static BoardState CreateUniformBoard(ElementType element)
        {
            var elements = new int[BoardConstants.CellCount];
            for (int index = 0; index < elements.Length; index++)
            {
                elements[index] = (int)element;
            }

            return CreateBoard(elements);
        }

        private static BoardBlock ReplaceBlock(
            BoardState board,
            int x,
            int y,
            BoardBlockType blockType,
            ElementType? element)
        {
            var position = new BoardPosition(x, y);
            var replacement = new BoardBlock(
                board.Get(position).RuntimeId,
                blockType,
                element);
            board.Set(position, replacement);
            return replacement;
        }

        private static void SetElement(
            BoardState board,
            int x,
            int y,
            ElementType element)
        {
            ReplaceBlock(board, x, y, BoardBlockType.Normal, element);
        }

        private static bool ContainsPosition(
            IReadOnlyList<BoardElementConversionEntry> entries,
            BoardPosition position)
        {
            foreach (BoardElementConversionEntry entry in entries)
            {
                if (entry.Position == position)
                {
                    return true;
                }
            }

            return false;
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

        private sealed class SequenceRandomSource : IRandomSource
        {
            private readonly Queue<int> values;

            public SequenceRandomSource(params int[] values)
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
