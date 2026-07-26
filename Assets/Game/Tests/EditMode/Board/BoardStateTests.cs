using System;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board
{
    public sealed class BoardStateTests
    {
        [TestCase(0, 0, 0)]
        [TestCase(5, 0, 5)]
        [TestCase(0, 1, 6)]
        [TestCase(0, 4, 24)]
        [TestCase(5, 4, 29)]
        public void ToIndex_ReturnsExpectedIndex(int x, int y, int expected)
        {
            var position = new BoardPosition(x, y);

            Assert.That(position.ToIndex(), Is.EqualTo(expected));
        }

        [TestCase(0, 0, 0)]
        [TestCase(5, 5, 0)]
        [TestCase(6, 0, 1)]
        [TestCase(24, 0, 4)]
        [TestCase(29, 5, 4)]
        public void FromIndex_ReturnsExpectedPosition(int index, int expectedX, int expectedY)
        {
            BoardPosition position = BoardPosition.FromIndex(index);

            Assert.That(position.X, Is.EqualTo(expectedX));
            Assert.That(position.Y, Is.EqualTo(expectedY));
        }

        [Test]
        public void EveryIndex_RoundTripsThroughPosition()
        {
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);

                Assert.That(position.ToIndex(), Is.EqualTo(index));
            }
        }

        [Test]
        public void Position_UsesValueEqualityAndReadableString()
        {
            var first = new BoardPosition(3, 2);
            var second = new BoardPosition(3, 2);
            var different = new BoardPosition(4, 2);

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first == second, Is.True);
            Assert.That(first != different, Is.True);
            Assert.That(first.ToString(), Is.EqualTo("(3, 2)"));
        }

        [TestCase(-1, 0)]
        [TestCase(6, 0)]
        [TestCase(0, -1)]
        [TestCase(0, 5)]
        public void Constructor_RejectsOutOfRangeCoordinates(int x, int y)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BoardPosition(x, y));
        }

        [TestCase(-1)]
        [TestCase(30)]
        public void FromIndex_RejectsOutOfRangeIndex(int index)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => BoardPosition.FromIndex(index));
        }

        [Test]
        public void NewBoard_StartsEmpty()
        {
            var board = new BoardState();

            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                Assert.That(board.Get(position), Is.Null);
                Assert.That(board.IsOccupied(position), Is.False);
            }
        }

        [Test]
        public void SetAndGet_PreserveBlockData()
        {
            var board = new BoardState();
            var position = new BoardPosition(2, 3);
            var block = CreateNormalBlock(42, ElementType.Water);

            board.Set(position, block);

            BoardBlock stored = board.Get(position);
            Assert.That(stored, Is.SameAs(block));
            Assert.That(stored.RuntimeId, Is.EqualTo(42));
            Assert.That(stored.BlockType, Is.EqualTo(BoardBlockType.Normal));
            Assert.That(stored.Element, Is.EqualTo(ElementType.Water));
            Assert.That(board.IsOccupied(position), Is.True);
        }

        [Test]
        public void Clear_EmptiesOccupiedCell()
        {
            var board = new BoardState();
            var position = new BoardPosition(1, 1);
            board.Set(position, CreateNormalBlock(1, ElementType.Fire));

            board.Clear(position);

            Assert.That(board.Get(position), Is.Null);
            Assert.That(board.IsOccupied(position), Is.False);
        }

        [Test]
        public void Swap_ExchangesTwoBlocks()
        {
            var board = new BoardState();
            var firstPosition = new BoardPosition(0, 0);
            var secondPosition = new BoardPosition(1, 0);
            var firstBlock = CreateNormalBlock(1, ElementType.Fire);
            var secondBlock = CreateNormalBlock(2, ElementType.Grass);
            board.Set(firstPosition, firstBlock);
            board.Set(secondPosition, secondBlock);

            board.Swap(firstPosition, secondPosition);

            Assert.That(board.Get(firstPosition), Is.SameAs(secondBlock));
            Assert.That(board.Get(secondPosition), Is.SameAs(firstBlock));
        }

        [Test]
        public void Swap_ExchangesBlockAndEmptyCell()
        {
            var board = new BoardState();
            var occupiedPosition = new BoardPosition(3, 2);
            var emptyPosition = new BoardPosition(3, 3);
            var block = CreateNormalBlock(9, ElementType.Dark);
            board.Set(occupiedPosition, block);

            board.Swap(occupiedPosition, emptyPosition);

            Assert.That(board.Get(occupiedPosition), Is.Null);
            Assert.That(board.Get(emptyPosition), Is.SameAs(block));
        }

        [Test]
        public void Swap_WithSamePosition_LeavesCellUnchanged()
        {
            var board = new BoardState();
            var position = new BoardPosition(4, 4);
            var block = CreateNormalBlock(17, ElementType.Light);
            board.Set(position, block);

            board.Swap(position, position);

            Assert.That(board.Get(position), Is.SameAs(block));
        }

        [Test]
        public void Clone_CanBeModifiedWithoutChangingOriginal()
        {
            var original = new BoardState();
            var firstPosition = new BoardPosition(0, 0);
            var secondPosition = new BoardPosition(5, 4);
            var firstBlock = CreateNormalBlock(100, ElementType.Fire);
            var secondBlock = CreateNormalBlock(200, ElementType.Water);
            original.Set(firstPosition, firstBlock);

            BoardState clone = original.Clone();
            clone.Clear(firstPosition);
            clone.Set(secondPosition, secondBlock);

            Assert.That(original.Get(firstPosition), Is.SameAs(firstBlock));
            Assert.That(original.Get(secondPosition), Is.Null);
            Assert.That(clone.Get(firstPosition), Is.Null);
            Assert.That(clone.Get(secondPosition), Is.SameAs(secondBlock));
        }

        [Test]
        public void NormalBlock_RejectsMissingElement()
        {
            Assert.Throws<ArgumentException>(
                () => new BoardBlock(1, BoardBlockType.Normal, null));
        }

        [Test]
        public void NonNormalBlock_AllowsMissingElement()
        {
            var block = new BoardBlock(300, BoardBlockType.Rock, null);

            Assert.That(block.RuntimeId, Is.EqualTo(300));
            Assert.That(block.BlockType, Is.EqualTo(BoardBlockType.Rock));
            Assert.That(block.Element, Is.Null);
        }

        private static BoardBlock CreateNormalBlock(long runtimeId, ElementType element)
        {
            return new BoardBlock(runtimeId, BoardBlockType.Normal, element);
        }
    }
}
