using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board
{
    public sealed class BoardMoveAnalyzerTests
    {
        private BoardMoveAnalyzer analyzer;
        private long nextRuntimeId;

        [SetUp]
        public void SetUp()
        {
            analyzer = new BoardMoveAnalyzer();
            nextRuntimeId = 1;
        }

        [TestCase(2, 3)]
        [TestCase(3, 2)]
        [TestCase(2, 1)]
        [TestCase(1, 2)]
        public void IsAdjacent_OrthogonalNeighbor_ReturnsTrue(int x, int y)
        {
            Assert.That(
                analyzer.IsAdjacent(
                    new BoardPosition(2, 2),
                    new BoardPosition(x, y)),
                Is.True);
        }

        [Test]
        public void IsAdjacent_SamePosition_ReturnsFalse()
        {
            var position = new BoardPosition(2, 2);

            Assert.That(analyzer.IsAdjacent(position, position), Is.False);
        }

        [TestCase(1, 1)]
        [TestCase(3, 3)]
        [TestCase(1, 3)]
        [TestCase(3, 1)]
        public void IsAdjacent_DiagonalPosition_ReturnsFalse(int x, int y)
        {
            Assert.That(
                analyzer.IsAdjacent(
                    new BoardPosition(2, 2),
                    new BoardPosition(x, y)),
                Is.False);
        }

        [TestCase(0, 2)]
        [TestCase(4, 2)]
        [TestCase(2, 0)]
        [TestCase(2, 4)]
        public void IsAdjacent_PositionTwoCellsAway_ReturnsFalse(int x, int y)
        {
            Assert.That(
                analyzer.IsAdjacent(
                    new BoardPosition(2, 2),
                    new BoardPosition(x, y)),
                Is.False);
        }

        [Test]
        public void CanSwap_AdjacentDifferentNormalBlocks_ReturnsTrue()
        {
            var board = new BoardState();
            SetNormal(board, 2, 2, ElementType.Fire);
            SetNormal(board, 3, 2, ElementType.Water);

            Assert.That(
                analyzer.CanSwap(
                    board,
                    new BoardPosition(2, 2),
                    new BoardPosition(3, 2)),
                Is.True);
        }

        [Test]
        public void IsValidSwap_HorizontalSwapCreatesHorizontalMatch()
        {
            var board = new BoardState();
            SetHorizontal(board, 0, 1, 2, ElementType.Fire);
            SetNormal(board, 2, 2, ElementType.Water);
            SetNormal(board, 3, 2, ElementType.Fire);

            AssertValidSwap(board, 2, 2, 3, 2);
        }

        [Test]
        public void IsValidSwap_HorizontalSwapCreatesVerticalMatch()
        {
            var board = new BoardState();
            SetNormal(board, 2, 1, ElementType.Fire);
            SetNormal(board, 3, 1, ElementType.Water);
            SetNormal(board, 2, 0, ElementType.Water);
            SetNormal(board, 2, 2, ElementType.Water);

            AssertValidSwap(board, 2, 1, 3, 1);
        }

        [Test]
        public void IsValidSwap_VerticalSwapCreatesHorizontalMatch()
        {
            var board = new BoardState();
            SetNormal(board, 2, 1, ElementType.Water);
            SetNormal(board, 2, 2, ElementType.Fire);
            SetNormal(board, 0, 2, ElementType.Water);
            SetNormal(board, 1, 2, ElementType.Water);

            AssertValidSwap(board, 2, 1, 2, 2);
        }

        [Test]
        public void IsValidSwap_VerticalSwapCreatesVerticalMatch()
        {
            var board = new BoardState();
            SetVertical(board, 2, 0, 1, ElementType.Water);
            SetNormal(board, 2, 2, ElementType.Fire);
            SetNormal(board, 2, 3, ElementType.Water);

            AssertValidSwap(board, 2, 2, 2, 3);
        }

        [Test]
        public void IsValidSwap_CreatesFourMatch()
        {
            var board = new BoardState();
            SetHorizontal(board, 0, 2, 2, ElementType.Fire);
            SetNormal(board, 3, 2, ElementType.Water);
            SetNormal(board, 4, 2, ElementType.Fire);

            AssertValidSwap(board, 3, 2, 4, 2);
        }

        [Test]
        public void IsValidSwap_CreatesCompoundMatch()
        {
            var board = new BoardState();
            SetNormal(board, 0, 2, ElementType.Fire);
            SetNormal(board, 1, 2, ElementType.Fire);
            SetNormal(board, 2, 1, ElementType.Fire);
            SetNormal(board, 2, 2, ElementType.Water);
            SetNormal(board, 2, 3, ElementType.Fire);
            SetNormal(board, 3, 2, ElementType.Fire);

            AssertValidSwap(board, 2, 2, 3, 2);
        }

        [Test]
        public void IsValidSwap_WhenSwapCreatesNoMatch_ReturnsFalse()
        {
            var board = new BoardState();
            SetNormal(board, 2, 2, ElementType.Fire);
            SetNormal(board, 3, 2, ElementType.Water);

            AssertInvalidSwap(board, 2, 2, 3, 2);
        }

        [Test]
        public void IsValidSwap_WhenOneCellIsEmpty_ReturnsFalse()
        {
            var board = new BoardState();
            SetNormal(board, 2, 2, ElementType.Fire);

            AssertInvalidSwap(board, 2, 2, 3, 2);
        }

        [TestCase(BoardBlockType.Rock)]
        [TestCase(BoardBlockType.Special)]
        [TestCase(BoardBlockType.Locked)]
        public void IsValidSwap_WhenOneBlockIsNotNormal_ReturnsFalse(
            BoardBlockType blockType)
        {
            var board = new BoardState();
            SetNormal(board, 2, 2, ElementType.Fire);
            SetBlock(board, 3, 2, blockType, ElementType.Water);

            AssertInvalidSwap(board, 2, 2, 3, 2);
        }

        [Test]
        public void IsValidSwap_WhenElementsAreEqual_ReturnsFalse()
        {
            var board = new BoardState();
            SetNormal(board, 2, 2, ElementType.Fire);
            SetNormal(board, 3, 2, ElementType.Fire);

            AssertInvalidSwap(board, 2, 2, 3, 2);
        }

        [Test]
        public void IsValidSwap_WhenOnlyUnrelatedExistingMatchExists_ReturnsFalse()
        {
            var board = new BoardState();
            SetHorizontal(board, 0, 2, 4, ElementType.Fire);
            SetNormal(board, 2, 1, ElementType.Water);
            SetNormal(board, 3, 1, ElementType.Grass);

            AssertInvalidSwap(board, 2, 1, 3, 1);
        }

        [Test]
        public void IsValidSwap_WhenIdenticalExistingMatchRemains_ReturnsFalse()
        {
            var board = new BoardState();
            SetVertical(board, 0, 0, 2, ElementType.Dark);
            SetNormal(board, 4, 3, ElementType.Light);
            SetNormal(board, 4, 4, ElementType.Water);

            AssertInvalidSwap(board, 4, 3, 4, 4);
        }

        [Test]
        public void IsValidSwap_WhenExistingAndNewMatchesCoexist_ReturnsTrue()
        {
            var board = new BoardState();
            SetHorizontal(board, 0, 2, 4, ElementType.Dark);
            SetHorizontal(board, 0, 1, 1, ElementType.Fire);
            SetNormal(board, 2, 1, ElementType.Water);
            SetNormal(board, 3, 1, ElementType.Fire);

            AssertValidSwap(board, 2, 1, 3, 1);
        }

        [Test]
        public void IsValidSwap_WhenExistingGroupChangesPositionSet_ReturnsTrue()
        {
            var board = new BoardState();
            SetHorizontal(board, 0, 2, 2, ElementType.Fire);
            SetNormal(board, 3, 2, ElementType.Water);
            SetNormal(board, 4, 2, ElementType.Fire);

            AssertValidSwap(board, 3, 2, 4, 2);
        }

        [Test]
        public void FindValidSwaps_ReturnsOnlyValidSwapsWithoutReverseDuplicates()
        {
            BoardState board = CreateBoardWithMultipleValidSwaps();

            IReadOnlyList<BoardSwap> swaps = analyzer.FindValidSwaps(board);

            Assert.That(swaps, Is.Not.Empty);
            for (int index = 0; index < swaps.Count; index++)
            {
                BoardSwap swap = swaps[index];
                Assert.That(
                    analyzer.IsValidSwap(board, swap.First, swap.Second),
                    Is.True);
                Assert.That(
                    IndexOf(
                        swaps,
                        new BoardSwap(swap.Second, swap.First)),
                    Is.EqualTo(-1));
            }
        }

        [Test]
        public void FindValidSwaps_UsesXThenYAndRightThenUpOrder()
        {
            BoardState board = CreateBoardWithMultipleValidSwaps();

            IReadOnlyList<BoardSwap> swaps = analyzer.FindValidSwaps(board);

            for (int index = 1; index < swaps.Count; index++)
            {
                Assert.That(
                    CompareByTraversalOrder(swaps[index - 1], swaps[index]),
                    Is.LessThan(0));
            }

            int rightIndex = IndexOf(
                swaps,
                new BoardSwap(
                    new BoardPosition(2, 2),
                    new BoardPosition(3, 2)));
            int upIndex = IndexOf(
                swaps,
                new BoardSwap(
                    new BoardPosition(2, 2),
                    new BoardPosition(2, 3)));

            Assert.That(rightIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(upIndex, Is.EqualTo(rightIndex + 1));
        }

        [Test]
        public void FindValidSwaps_RightAndUpTraversalCoversAllAdjacentPairs()
        {
            BoardState board = CreateBoardWithMultipleValidSwaps();
            var expected = new List<BoardSwap>();

            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    AddExpectedIfValid(board, x, y, x + 1, y, expected);
                    AddExpectedIfValid(board, x, y, x, y + 1, expected);
                }
            }

            CollectionAssert.AreEqual(expected, analyzer.FindValidSwaps(board));
        }

        [Test]
        public void HasAnyValidSwap_AgreesWithFindValidSwaps()
        {
            BoardState withMoves = CreateBoardWithMultipleValidSwaps();
            var withoutMoves = new BoardState();
            SetNormal(withoutMoves, 0, 0, ElementType.Fire);
            SetNormal(withoutMoves, 1, 0, ElementType.Water);

            Assert.That(analyzer.HasAnyValidSwap(withMoves), Is.True);
            Assert.That(analyzer.FindValidSwaps(withMoves), Is.Not.Empty);
            Assert.That(analyzer.HasAnyValidSwap(withoutMoves), Is.False);
            Assert.That(analyzer.FindValidSwaps(withoutMoves), Is.Empty);
        }

        [Test]
        public void FindValidSwaps_ReturnsReadOnlyList()
        {
            IReadOnlyList<BoardSwap> swaps =
                analyzer.FindValidSwaps(CreateBoardWithMultipleValidSwaps());
            var list = swaps as IList<BoardSwap>;

            Assert.That(list, Is.Not.Null);
            Assert.That(list.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(
                () => list.Add(default(BoardSwap)));
        }

        [Test]
        public void IsValidSwap_DoesNotChangeOriginalBoardOrRuntimeIds()
        {
            BoardState board = CreateBoardWithMultipleValidSwaps();
            BoardBlock[] before = CaptureCells(board);

            analyzer.IsValidSwap(
                board,
                new BoardPosition(2, 2),
                new BoardPosition(3, 2));

            AssertBoardUnchanged(board, before);
        }

        [Test]
        public void FindValidSwaps_DoesNotChangeOriginalBoardOrRuntimeIds()
        {
            BoardState board = CreateBoardWithMultipleValidSwaps();
            BoardBlock[] before = CaptureCells(board);

            analyzer.FindValidSwaps(board);

            AssertBoardUnchanged(board, before);
        }

        [Test]
        public void BoardSwap_UsesOrderedValueEqualityAndReadableString()
        {
            var first = new BoardPosition(1, 2);
            var second = new BoardPosition(2, 2);
            var swap = new BoardSwap(first, second);
            var equal = new BoardSwap(first, second);
            var reverse = new BoardSwap(second, first);

            Assert.That(swap, Is.EqualTo(equal));
            Assert.That(swap == equal, Is.True);
            Assert.That(swap != reverse, Is.True);
            Assert.That(swap.GetHashCode(), Is.EqualTo(equal.GetHashCode()));
            Assert.That(swap.ToString(), Is.EqualTo("(1, 2) -> (2, 2)"));
        }

        [Test]
        public void PublicBoardMethods_NullBoard_Throw()
        {
            var first = new BoardPosition(0, 0);
            var second = new BoardPosition(1, 0);

            Assert.Throws<ArgumentNullException>(
                () => analyzer.CanSwap(null, first, second));
            Assert.Throws<ArgumentNullException>(
                () => analyzer.IsValidSwap(null, first, second));
            Assert.Throws<ArgumentNullException>(
                () => analyzer.HasAnyValidSwap(null));
            Assert.Throws<ArgumentNullException>(
                () => analyzer.FindValidSwaps(null));
        }

        private BoardState CreateBoardWithMultipleValidSwaps()
        {
            var board = new BoardState();
            SetNormal(board, 2, 0, ElementType.Water);
            SetNormal(board, 2, 1, ElementType.Water);
            SetNormal(board, 0, 2, ElementType.Grass);
            SetNormal(board, 1, 2, ElementType.Grass);
            SetNormal(board, 2, 2, ElementType.Fire);
            SetNormal(board, 3, 2, ElementType.Water);
            SetNormal(board, 2, 3, ElementType.Grass);
            return board;
        }

        private void AddExpectedIfValid(
            BoardState board,
            int firstX,
            int firstY,
            int secondX,
            int secondY,
            List<BoardSwap> expected)
        {
            if (!BoardPosition.IsValid(secondX, secondY))
            {
                return;
            }

            var first = new BoardPosition(firstX, firstY);
            var second = new BoardPosition(secondX, secondY);
            if (analyzer.IsValidSwap(board, first, second))
            {
                expected.Add(new BoardSwap(first, second));
            }
        }

        private static int CompareByTraversalOrder(BoardSwap left, BoardSwap right)
        {
            int xComparison = left.First.X.CompareTo(right.First.X);
            if (xComparison != 0)
            {
                return xComparison;
            }

            int yComparison = left.First.Y.CompareTo(right.First.Y);
            if (yComparison != 0)
            {
                return yComparison;
            }

            bool leftIsRight = left.Second.X == left.First.X + 1;
            bool rightIsRight = right.Second.X == right.First.X + 1;
            return leftIsRight == rightIsRight
                ? 0
                : leftIsRight ? -1 : 1;
        }

        private static int IndexOf(
            IReadOnlyList<BoardSwap> swaps,
            BoardSwap expected)
        {
            for (int index = 0; index < swaps.Count; index++)
            {
                if (swaps[index] == expected)
                {
                    return index;
                }
            }

            return -1;
        }

        private void AssertValidSwap(
            BoardState board,
            int firstX,
            int firstY,
            int secondX,
            int secondY)
        {
            Assert.That(
                analyzer.IsValidSwap(
                    board,
                    new BoardPosition(firstX, firstY),
                    new BoardPosition(secondX, secondY)),
                Is.True);
        }

        private void AssertInvalidSwap(
            BoardState board,
            int firstX,
            int firstY,
            int secondX,
            int secondY)
        {
            Assert.That(
                analyzer.IsValidSwap(
                    board,
                    new BoardPosition(firstX, firstY),
                    new BoardPosition(secondX, secondY)),
                Is.False);
        }

        private void SetHorizontal(
            BoardState board,
            int startX,
            int endX,
            int y,
            ElementType element)
        {
            for (int x = startX; x <= endX; x++)
            {
                SetNormal(board, x, y, element);
            }
        }

        private void SetVertical(
            BoardState board,
            int x,
            int startY,
            int endY,
            ElementType element)
        {
            for (int y = startY; y <= endY; y++)
            {
                SetNormal(board, x, y, element);
            }
        }

        private void SetNormal(
            BoardState board,
            int x,
            int y,
            ElementType element)
        {
            SetBlock(board, x, y, BoardBlockType.Normal, element);
        }

        private void SetBlock(
            BoardState board,
            int x,
            int y,
            BoardBlockType blockType,
            ElementType? element)
        {
            board.Set(
                new BoardPosition(x, y),
                new BoardBlock(nextRuntimeId++, blockType, element));
        }

        private static BoardBlock[] CaptureCells(BoardState board)
        {
            var cells = new BoardBlock[BoardConstants.CellCount];
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                cells[index] = board.Get(BoardPosition.FromIndex(index));
            }

            return cells;
        }

        private static void AssertBoardUnchanged(
            BoardState board,
            BoardBlock[] before)
        {
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardBlock actual = board.Get(BoardPosition.FromIndex(index));
                Assert.That(actual, Is.SameAs(before[index]));

                if (actual != null)
                {
                    Assert.That(
                        actual.RuntimeId,
                        Is.EqualTo(before[index].RuntimeId));
                }
            }
        }
    }
}
