using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board
{
    public sealed class BoardCollapseResolverTests
    {
        [TestCase(0, 4)]
        [TestCase(2, 2)]
        [TestCase(4, 0)]
        public void Resolve_RemovesSingleBlockAtExpectedHeight(
            int removedY,
            int expectedMoveCount)
        {
            BoardState board = CreateFullBoard();
            var position = new BoardPosition(2, removedY);
            BoardBlock removedBlock = board.Get(position);

            BoardCollapseResult result = BoardCollapseResolver.Resolve(
                board,
                new[] { position });

            Assert.That(result.Removals, Has.Count.EqualTo(1));
            Assert.That(result.Removals[0].Block, Is.SameAs(removedBlock));
            Assert.That(result.Removals[0].Position, Is.EqualTo(position));
            Assert.That(result.Removals[0].RuntimeId, Is.EqualTo(removedBlock.RuntimeId));
            Assert.That(result.Moves, Has.Count.EqualTo(expectedMoveCount));
            Assert.That(
                result.Board.Get(new BoardPosition(2, BoardConstants.Height - 1)),
                Is.Null);
        }

        [Test]
        public void Resolve_RemovesMultipleBlocksFromOneColumnAndCompactsGaps()
        {
            BoardState board = CreateFullBoard();
            var lowerRemoval = new BoardPosition(1, 1);
            var upperRemoval = new BoardPosition(1, 3);
            BoardBlock bottom = board.Get(new BoardPosition(1, 0));
            BoardBlock middle = board.Get(new BoardPosition(1, 2));
            BoardBlock top = board.Get(new BoardPosition(1, 4));

            BoardCollapseResult result = BoardCollapseResolver.Resolve(
                board,
                new[] { lowerRemoval, upperRemoval });

            Assert.That(result.Board.Get(new BoardPosition(1, 0)), Is.SameAs(bottom));
            Assert.That(result.Board.Get(new BoardPosition(1, 1)), Is.SameAs(middle));
            Assert.That(result.Board.Get(new BoardPosition(1, 2)), Is.SameAs(top));
            Assert.That(result.Board.Get(new BoardPosition(1, 3)), Is.Null);
            Assert.That(result.Board.Get(new BoardPosition(1, 4)), Is.Null);
            Assert.That(result.Moves, Has.Count.EqualTo(2));
            AssertMove(result.Moves[0], middle, 1, 2, 1, 1);
            AssertMove(result.Moves[1], top, 1, 4, 1, 2);
        }

        [Test]
        public void Resolve_RemovesBlocksFromMultipleColumnsIndependently()
        {
            BoardState board = CreateFullBoard();
            BoardBlock columnZeroTop = board.Get(new BoardPosition(0, 4));
            BoardBlock columnFiveBottom = board.Get(new BoardPosition(5, 0));
            BoardBlock unaffected = board.Get(new BoardPosition(3, 2));

            BoardCollapseResult result = BoardCollapseResolver.Resolve(
                board,
                new[]
                {
                    new BoardPosition(0, 0),
                    new BoardPosition(5, 4)
                });

            Assert.That(
                result.Board.Get(new BoardPosition(0, 3)),
                Is.SameAs(columnZeroTop));
            Assert.That(
                result.Board.Get(new BoardPosition(5, 0)),
                Is.SameAs(columnFiveBottom));
            Assert.That(
                result.Board.Get(new BoardPosition(3, 2)),
                Is.SameAs(unaffected));
            Assert.That(result.Moves, Has.Count.EqualTo(4));
        }

        [Test]
        public void Resolve_RemovesEntireColumn()
        {
            BoardState board = CreateFullBoard();
            var positions = new BoardPosition[BoardConstants.Height];

            for (int y = 0; y < BoardConstants.Height; y++)
            {
                positions[y] = new BoardPosition(3, y);
            }

            BoardCollapseResult result =
                BoardCollapseResolver.Resolve(board, positions);

            Assert.That(result.Removals, Has.Count.EqualTo(BoardConstants.Height));
            Assert.That(result.Moves, Is.Empty);
            for (int y = 0; y < BoardConstants.Height; y++)
            {
                Assert.That(result.Board.Get(new BoardPosition(3, y)), Is.Null);
            }
        }

        [Test]
        public void Resolve_RemovesAllThirtyBlocks()
        {
            BoardState board = CreateFullBoard();
            var positions = new BoardPosition[BoardConstants.CellCount];

            for (int index = 0; index < positions.Length; index++)
            {
                positions[index] = BoardPosition.FromIndex(index);
            }

            BoardCollapseResult result =
                BoardCollapseResolver.Resolve(board, positions);

            Assert.That(result.Removals, Has.Count.EqualTo(BoardConstants.CellCount));
            Assert.That(result.Moves, Is.Empty);
            AssertBoardIsEmpty(result.Board);
        }

        [Test]
        public void Resolve_WithNoRemovalsReturnsIndependentEquivalentBoard()
        {
            BoardState board = CreateFullBoard();

            BoardCollapseResult result = BoardCollapseResolver.Resolve(
                board,
                Array.Empty<BoardPosition>());

            Assert.That(result.Board, Is.Not.SameAs(board));
            Assert.That(result.Removals, Is.Empty);
            Assert.That(result.Moves, Is.Empty);
            AssertBoardsHaveSameContents(board, result.Board);

            result.Board.Clear(new BoardPosition(0, 0));
            Assert.That(board.Get(new BoardPosition(0, 0)), Is.Not.Null);
        }

        [Test]
        public void Resolve_WhenTopBlockIsRemovedRecordsNoMove()
        {
            BoardState board = CreateFullBoard();

            BoardCollapseResult result = BoardCollapseResolver.Resolve(
                board,
                new[] { new BoardPosition(4, 4) });

            Assert.That(result.Moves, Is.Empty);
        }

        [Test]
        public void Resolve_WhenBottomBlockIsRemovedMovesEveryBlockAboveDown()
        {
            BoardState board = CreateFullBoard();
            var expectedBlocks = new BoardBlock[BoardConstants.Height - 1];
            for (int y = 1; y < BoardConstants.Height; y++)
            {
                expectedBlocks[y - 1] = board.Get(new BoardPosition(2, y));
            }

            BoardCollapseResult result = BoardCollapseResolver.Resolve(
                board,
                new[] { new BoardPosition(2, 0) });

            Assert.That(result.Moves, Has.Count.EqualTo(expectedBlocks.Length));
            for (int y = 0; y < expectedBlocks.Length; y++)
            {
                Assert.That(
                    result.Board.Get(new BoardPosition(2, y)),
                    Is.SameAs(expectedBlocks[y]));
                AssertMove(
                    result.Moves[y],
                    expectedBlocks[y],
                    2,
                    y + 1,
                    2,
                    y);
            }
        }

        [Test]
        public void Resolve_MovesAreOrderedByColumnThenOriginalHeight()
        {
            BoardState board = CreateFullBoard();

            BoardCollapseResult result = BoardCollapseResolver.Resolve(
                board,
                new[]
                {
                    new BoardPosition(4, 0),
                    new BoardPosition(1, 0)
                });

            Assert.That(result.Moves, Has.Count.EqualTo(8));
            for (int index = 0; index < result.Moves.Count; index++)
            {
                int expectedX = index < 4 ? 1 : 4;
                int expectedFromY = (index % 4) + 1;
                Assert.That(result.Moves[index].From.X, Is.EqualTo(expectedX));
                Assert.That(result.Moves[index].From.Y, Is.EqualTo(expectedFromY));
            }
        }

        [Test]
        public void Resolve_EveryMoveKeepsColumnAndMovesDown()
        {
            BoardState board = CreateFullBoard();

            BoardCollapseResult result = BoardCollapseResolver.Resolve(
                board,
                new[]
                {
                    new BoardPosition(0, 0),
                    new BoardPosition(0, 2),
                    new BoardPosition(3, 1),
                    new BoardPosition(5, 3)
                });

            foreach (BoardBlockMove move in result.Moves)
            {
                Assert.That(move.To.X, Is.EqualTo(move.From.X));
                Assert.That(move.To.Y, Is.LessThan(move.From.Y));
            }
        }

        [Test]
        public void Resolve_LeavesEmptyCellsOnlyAtTopOfEachColumn()
        {
            BoardState board = CreateFullBoard();

            BoardCollapseResult result = BoardCollapseResolver.Resolve(
                board,
                new[]
                {
                    new BoardPosition(0, 0),
                    new BoardPosition(1, 2),
                    new BoardPosition(1, 4),
                    new BoardPosition(3, 1),
                    new BoardPosition(3, 2),
                    new BoardPosition(5, 4)
                });

            AssertEmptyCellsAreOnlyAtColumnTops(result.Board);
        }

        [Test]
        public void Resolve_PreservesRelativeOrderOfRemainingBlocks()
        {
            BoardState board = CreateFullBoard();
            BoardBlock first = board.Get(new BoardPosition(2, 0));
            BoardBlock second = board.Get(new BoardPosition(2, 2));
            BoardBlock third = board.Get(new BoardPosition(2, 4));

            BoardCollapseResult result = BoardCollapseResolver.Resolve(
                board,
                new[]
                {
                    new BoardPosition(2, 1),
                    new BoardPosition(2, 3)
                });

            Assert.That(result.Board.Get(new BoardPosition(2, 0)), Is.SameAs(first));
            Assert.That(result.Board.Get(new BoardPosition(2, 1)), Is.SameAs(second));
            Assert.That(result.Board.Get(new BoardPosition(2, 2)), Is.SameAs(third));
        }

        [Test]
        public void Resolve_RemovalsPreserveInputOrderAndOriginalPositions()
        {
            BoardState board = CreateFullBoard();
            var positions = new[]
            {
                new BoardPosition(5, 4),
                new BoardPosition(0, 0),
                new BoardPosition(3, 2)
            };
            var expectedBlocks = new[]
            {
                board.Get(positions[0]),
                board.Get(positions[1]),
                board.Get(positions[2])
            };

            BoardCollapseResult result =
                BoardCollapseResolver.Resolve(board, positions);

            for (int index = 0; index < positions.Length; index++)
            {
                Assert.That(result.Removals[index].Position, Is.EqualTo(positions[index]));
                Assert.That(result.Removals[index].Block, Is.SameAs(expectedBlocks[index]));
                Assert.That(
                    result.Removals[index].RuntimeId,
                    Is.EqualTo(expectedBlocks[index].RuntimeId));
            }
        }

        [Test]
        public void Resolve_ExcludesStationaryAndRemovedBlocksFromMoves()
        {
            BoardState board = CreateFullBoard();
            BoardBlock stationary = board.Get(new BoardPosition(4, 0));
            BoardBlock removed = board.Get(new BoardPosition(4, 2));

            BoardCollapseResult result = BoardCollapseResolver.Resolve(
                board,
                new[] { new BoardPosition(4, 2) });

            foreach (BoardBlockMove move in result.Moves)
            {
                Assert.That(move.Block, Is.Not.SameAs(stationary));
                Assert.That(move.Block, Is.Not.SameAs(removed));
            }
        }

        [Test]
        public void Resolve_ResultCollectionsCannotBeModifiedExternally()
        {
            BoardState board = CreateFullBoard();
            BoardCollapseResult result = BoardCollapseResolver.Resolve(
                board,
                new[] { new BoardPosition(0, 0) });
            var removals = result.Removals as IList<BoardBlockRemoval>;
            var moves = result.Moves as IList<BoardBlockMove>;

            Assert.That(removals, Is.Not.Null);
            Assert.That(removals.IsReadOnly, Is.True);
            Assert.That(moves, Is.Not.Null);
            Assert.That(moves.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => removals.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => moves.RemoveAt(0));
        }

        [Test]
        public void Resolve_DoesNotChangeOriginalBoard()
        {
            BoardState board = CreateFullBoard();
            BoardBlock[] originalContents = CaptureBoardContents(board);

            BoardCollapseResolver.Resolve(
                board,
                new[]
                {
                    new BoardPosition(0, 0),
                    new BoardPosition(2, 2),
                    new BoardPosition(5, 4)
                });

            AssertBoardContainsReferences(board, originalContents);
        }

        [Test]
        public void Resolve_PreservesRemainingBlockIdentityTypeAndElement()
        {
            BoardState board = CreateFullBoard();
            var rockPosition = new BoardPosition(3, 4);
            var rock = new BoardBlock(1000, BoardBlockType.Rock, ElementType.Dark);
            board.Set(rockPosition, rock);

            BoardCollapseResult result = BoardCollapseResolver.Resolve(
                board,
                new[] { new BoardPosition(3, 1) });
            BoardBlock movedRock = result.Board.Get(new BoardPosition(3, 3));

            Assert.That(movedRock, Is.SameAs(rock));
            Assert.That(movedRock.RuntimeId, Is.EqualTo(1000));
            Assert.That(movedRock.BlockType, Is.EqualTo(BoardBlockType.Rock));
            Assert.That(movedRock.Element, Is.EqualTo(ElementType.Dark));
        }

        [Test]
        public void Resolve_WithSameInputProducesDeterministicResults()
        {
            BoardState board = CreateFullBoard();
            var positions = new[]
            {
                new BoardPosition(4, 1),
                new BoardPosition(0, 3),
                new BoardPosition(4, 3)
            };

            BoardCollapseResult first =
                BoardCollapseResolver.Resolve(board, positions);
            BoardCollapseResult second =
                BoardCollapseResolver.Resolve(board, positions);

            AssertBoardsHaveSameContents(first.Board, second.Board);
            Assert.That(second.Removals, Has.Count.EqualTo(first.Removals.Count));
            Assert.That(second.Moves, Has.Count.EqualTo(first.Moves.Count));
            for (int index = 0; index < first.Moves.Count; index++)
            {
                Assert.That(
                    second.Moves[index].RuntimeId,
                    Is.EqualTo(first.Moves[index].RuntimeId));
                Assert.That(second.Moves[index].From, Is.EqualTo(first.Moves[index].From));
                Assert.That(second.Moves[index].To, Is.EqualTo(first.Moves[index].To));
            }
        }

        [Test]
        public void Resolve_NullBoardThrows()
        {
            Assert.Throws<ArgumentNullException>(
                () => BoardCollapseResolver.Resolve(
                    null,
                    Array.Empty<BoardPosition>()));
        }

        [Test]
        public void Resolve_NullRemovalListThrows()
        {
            Assert.Throws<ArgumentNullException>(
                () => BoardCollapseResolver.Resolve(CreateFullBoard(), null));
        }

        [Test]
        public void Resolve_DuplicateRemovalPositionThrows()
        {
            BoardState board = CreateFullBoard();
            var duplicate = new BoardPosition(2, 3);

            Assert.Throws<ArgumentException>(
                () => BoardCollapseResolver.Resolve(
                    board,
                    new[] { duplicate, duplicate }));
        }

        [Test]
        public void Resolve_EmptyRemovalPositionThrows()
        {
            BoardState board = CreateFullBoard();
            var emptyPosition = new BoardPosition(1, 2);
            board.Clear(emptyPosition);

            Assert.Throws<InvalidOperationException>(
                () => BoardCollapseResolver.Resolve(
                    board,
                    new[] { emptyPosition }));
        }

        private static BoardState CreateFullBoard()
        {
            var board = new BoardState();

            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                var element = (ElementType)(index % 5);
                board.Set(
                    position,
                    new BoardBlock(index + 1, BoardBlockType.Normal, element));
            }

            return board;
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

        private static void AssertBoardIsEmpty(BoardState board)
        {
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                Assert.That(board.Get(BoardPosition.FromIndex(index)), Is.Null);
            }
        }

        private static void AssertEmptyCellsAreOnlyAtColumnTops(BoardState board)
        {
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                bool foundEmptyCell = false;
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    BoardBlock block = board.Get(new BoardPosition(x, y));
                    if (block == null)
                    {
                        foundEmptyCell = true;
                    }
                    else
                    {
                        Assert.That(
                            foundEmptyCell,
                            Is.False,
                            $"Column {x} has an occupied cell above an empty cell.");
                    }
                }
            }
        }

        private static void AssertMove(
            BoardBlockMove move,
            BoardBlock expectedBlock,
            int fromX,
            int fromY,
            int toX,
            int toY)
        {
            Assert.That(move.Block, Is.SameAs(expectedBlock));
            Assert.That(move.RuntimeId, Is.EqualTo(expectedBlock.RuntimeId));
            Assert.That(move.From, Is.EqualTo(new BoardPosition(fromX, fromY)));
            Assert.That(move.To, Is.EqualTo(new BoardPosition(toX, toY)));
        }
    }
}
