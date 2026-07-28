using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board
{
    public sealed class BoardMatchFinderTests
    {
        private long nextRuntimeId;

        [SetUp]
        public void SetUp()
        {
            nextRuntimeId = 1;
        }

        [TestCase(3, BoardMatchTier.Three)]
        [TestCase(4, BoardMatchTier.Four)]
        [TestCase(5, BoardMatchTier.FiveOrMore)]
        [TestCase(6, BoardMatchTier.FiveOrMore)]
        public void FindMatches_ClassifiesHorizontalRuns(
            int blockCount,
            BoardMatchTier expectedTier)
        {
            var board = new BoardState();
            SetHorizontal(board, 0, blockCount - 1, 2, ElementType.Fire);

            IReadOnlyList<BoardMatch> matches = BoardMatchFinder.FindMatches(board);

            Assert.That(matches, Has.Count.EqualTo(1));
            AssertMatch(
                matches[0],
                ElementType.Fire,
                expectedTier,
                CreateHorizontalPositions(0, blockCount - 1, 2));
        }

        [TestCase(3, BoardMatchTier.Three)]
        [TestCase(4, BoardMatchTier.Four)]
        [TestCase(5, BoardMatchTier.FiveOrMore)]
        public void FindMatches_ClassifiesVerticalRuns(
            int blockCount,
            BoardMatchTier expectedTier)
        {
            var board = new BoardState();
            SetVertical(board, 3, 0, blockCount - 1, ElementType.Water);

            IReadOnlyList<BoardMatch> matches = BoardMatchFinder.FindMatches(board);

            Assert.That(matches, Has.Count.EqualTo(1));
            AssertMatch(
                matches[0],
                ElementType.Water,
                expectedTier,
                CreateVerticalPositions(3, 0, blockCount - 1));
        }

        [Test]
        public void FindMatches_EmptyBoard_ReturnsNoMatches()
        {
            var board = new BoardState();

            IReadOnlyList<BoardMatch> matches = BoardMatchFinder.FindMatches(board);

            Assert.That(matches, Is.Empty);
        }

        [Test]
        public void FindMatches_TwoAdjacentBlocks_ReturnsNoMatches()
        {
            var board = new BoardState();
            SetNormal(board, 1, 1, ElementType.Grass);
            SetNormal(board, 2, 1, ElementType.Grass);

            IReadOnlyList<BoardMatch> matches = BoardMatchFinder.FindMatches(board);

            Assert.That(matches, Is.Empty);
        }

        [Test]
        public void FindMatches_ThreeDiagonalBlocks_ReturnsNoMatches()
        {
            var board = new BoardState();
            SetNormal(board, 0, 0, ElementType.Light);
            SetNormal(board, 1, 1, ElementType.Light);
            SetNormal(board, 2, 2, ElementType.Light);

            IReadOnlyList<BoardMatch> matches = BoardMatchFinder.FindMatches(board);

            Assert.That(matches, Is.Empty);
        }

        [Test]
        public void FindMatches_EmptyCellBreaksRun()
        {
            var board = new BoardState();
            SetNormal(board, 0, 2, ElementType.Dark);
            SetNormal(board, 1, 2, ElementType.Dark);
            SetNormal(board, 3, 2, ElementType.Dark);
            SetNormal(board, 4, 2, ElementType.Dark);

            IReadOnlyList<BoardMatch> matches = BoardMatchFinder.FindMatches(board);

            Assert.That(matches, Is.Empty);
        }

        [Test]
        public void FindMatches_CombinesTShape()
        {
            var board = new BoardState();
            SetHorizontal(board, 1, 3, 3, ElementType.Fire);
            SetVertical(board, 2, 1, 3, ElementType.Fire);

            IReadOnlyList<BoardMatch> matches = BoardMatchFinder.FindMatches(board);

            Assert.That(matches, Has.Count.EqualTo(1));
            AssertMatch(
                matches[0],
                ElementType.Fire,
                BoardMatchTier.FiveOrMore,
                new BoardPosition(1, 3),
                new BoardPosition(2, 1),
                new BoardPosition(2, 2),
                new BoardPosition(2, 3),
                new BoardPosition(3, 3));
        }

        [Test]
        public void FindMatches_CombinesLShape()
        {
            var board = new BoardState();
            SetHorizontal(board, 1, 3, 1, ElementType.Water);
            SetVertical(board, 1, 1, 3, ElementType.Water);

            IReadOnlyList<BoardMatch> matches = BoardMatchFinder.FindMatches(board);

            Assert.That(matches, Has.Count.EqualTo(1));
            AssertMatch(
                matches[0],
                ElementType.Water,
                BoardMatchTier.FiveOrMore,
                new BoardPosition(1, 1),
                new BoardPosition(1, 2),
                new BoardPosition(1, 3),
                new BoardPosition(2, 1),
                new BoardPosition(3, 1));
        }

        [Test]
        public void FindMatches_CombinesCrossShape()
        {
            var board = new BoardState();
            SetHorizontal(board, 1, 3, 2, ElementType.Grass);
            SetVertical(board, 2, 1, 3, ElementType.Grass);

            IReadOnlyList<BoardMatch> matches = BoardMatchFinder.FindMatches(board);

            Assert.That(matches, Has.Count.EqualTo(1));
            AssertMatch(
                matches[0],
                ElementType.Grass,
                BoardMatchTier.FiveOrMore,
                new BoardPosition(1, 2),
                new BoardPosition(2, 1),
                new BoardPosition(2, 2),
                new BoardPosition(2, 3),
                new BoardPosition(3, 2));
        }

        [Test]
        public void FindMatches_ComplexGroupOfSix_UsesFiveOrMoreTier()
        {
            var board = new BoardState();
            SetHorizontal(board, 0, 3, 2, ElementType.Light);
            SetVertical(board, 1, 1, 3, ElementType.Light);

            IReadOnlyList<BoardMatch> matches = BoardMatchFinder.FindMatches(board);

            Assert.That(matches, Has.Count.EqualTo(1));
            AssertMatch(
                matches[0],
                ElementType.Light,
                BoardMatchTier.FiveOrMore,
                new BoardPosition(0, 2),
                new BoardPosition(1, 1),
                new BoardPosition(1, 2),
                new BoardPosition(1, 3),
                new BoardPosition(2, 2),
                new BoardPosition(3, 2));
        }

        [Test]
        public void FindMatches_SeparatedGroupsOfSameElement_ReturnsSeparateEvents()
        {
            var board = new BoardState();
            SetHorizontal(board, 0, 2, 0, ElementType.Dark);
            SetVertical(board, 5, 2, 4, ElementType.Dark);

            IReadOnlyList<BoardMatch> matches = BoardMatchFinder.FindMatches(board);

            Assert.That(matches, Has.Count.EqualTo(2));
            Assert.That(matches[0].Element, Is.EqualTo(ElementType.Dark));
            Assert.That(matches[0].Origin, Is.EqualTo(new BoardPosition(0, 0)));
            Assert.That(matches[1].Element, Is.EqualTo(ElementType.Dark));
            Assert.That(matches[1].Origin, Is.EqualTo(new BoardPosition(5, 2)));
        }

        [Test]
        public void FindMatches_GroupsOfDifferentElements_ReturnsSeparateEvents()
        {
            var board = new BoardState();
            SetHorizontal(board, 0, 2, 1, ElementType.Fire);
            SetHorizontal(board, 3, 5, 3, ElementType.Water);

            IReadOnlyList<BoardMatch> matches = BoardMatchFinder.FindMatches(board);

            Assert.That(matches, Has.Count.EqualTo(2));
            Assert.That(matches[0].Element, Is.EqualTo(ElementType.Fire));
            Assert.That(matches[1].Element, Is.EqualTo(ElementType.Water));
        }

        [Test]
        public void FindMatches_OrdersByLeftmostColumnThenLowestCell()
        {
            var board = new BoardState();
            SetHorizontal(board, 0, 2, 1, ElementType.Grass);
            SetHorizontal(board, 0, 2, 4, ElementType.Fire);
            SetHorizontal(board, 1, 3, 0, ElementType.Water);

            IReadOnlyList<BoardMatch> matches = BoardMatchFinder.FindMatches(board);

            Assert.That(matches, Has.Count.EqualTo(3));
            Assert.That(matches[0].Origin, Is.EqualTo(new BoardPosition(0, 1)));
            Assert.That(matches[1].Origin, Is.EqualTo(new BoardPosition(0, 4)));
            Assert.That(matches[2].Origin, Is.EqualTo(new BoardPosition(1, 0)));
        }

        [TestCase(BoardBlockType.Rock)]
        [TestCase(BoardBlockType.Special)]
        [TestCase(BoardBlockType.Locked)]
        public void FindMatches_NonNormalBlocksAreExcluded(BoardBlockType blockType)
        {
            var board = new BoardState();
            SetBlock(board, 0, 2, blockType, ElementType.Fire);
            SetBlock(board, 1, 2, blockType, ElementType.Fire);
            SetBlock(board, 2, 2, blockType, ElementType.Fire);

            IReadOnlyList<BoardMatch> matches = BoardMatchFinder.FindMatches(board);

            Assert.That(matches, Is.Empty);
        }

        [Test]
        public void FindMatches_DoesNotChangeBoardState()
        {
            var board = new BoardState();
            SetHorizontal(board, 0, 3, 2, ElementType.Fire);
            SetBlock(board, 5, 4, BoardBlockType.Rock, null);
            var before = new BoardBlock[BoardConstants.CellCount];

            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                before[index] = board.Get(BoardPosition.FromIndex(index));
            }

            BoardMatchFinder.FindMatches(board);

            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                Assert.That(
                    board.Get(BoardPosition.FromIndex(index)),
                    Is.SameAs(before[index]));
            }
        }

        [Test]
        public void FindMatches_ReturnsPositionsThatCannotBeModifiedExternally()
        {
            var board = new BoardState();
            SetHorizontal(board, 0, 2, 0, ElementType.Fire);

            BoardMatch match = BoardMatchFinder.FindMatches(board)[0];
            var positions = match.Positions as IList<BoardPosition>;

            Assert.That(positions, Is.Not.Null);
            Assert.That(positions.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(
                () => positions.Add(new BoardPosition(5, 4)));
            Assert.Throws<NotSupportedException>(
                () => positions[0] = new BoardPosition(5, 4));
            Assert.That(match.BlockCount, Is.EqualTo(3));
            Assert.That(match.Origin, Is.EqualTo(new BoardPosition(0, 0)));
        }

        [Test]
        public void FindMatches_NullBoard_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => BoardMatchFinder.FindMatches(null));
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

        private static BoardPosition[] CreateHorizontalPositions(
            int startX,
            int endX,
            int y)
        {
            var positions = new BoardPosition[endX - startX + 1];
            for (int x = startX; x <= endX; x++)
            {
                positions[x - startX] = new BoardPosition(x, y);
            }

            return positions;
        }

        private static BoardPosition[] CreateVerticalPositions(
            int x,
            int startY,
            int endY)
        {
            var positions = new BoardPosition[endY - startY + 1];
            for (int y = startY; y <= endY; y++)
            {
                positions[y - startY] = new BoardPosition(x, y);
            }

            return positions;
        }

        private static void AssertMatch(
            BoardMatch match,
            ElementType expectedElement,
            BoardMatchTier expectedTier,
            params BoardPosition[] expectedPositions)
        {
            Assert.That(match.Element, Is.EqualTo(expectedElement));
            Assert.That(match.Tier, Is.EqualTo(expectedTier));
            Assert.That(match.BlockCount, Is.EqualTo(expectedPositions.Length));
            Assert.That(match.Origin, Is.EqualTo(expectedPositions[0]));
            CollectionAssert.AreEqual(expectedPositions, match.Positions);
        }
    }
}
