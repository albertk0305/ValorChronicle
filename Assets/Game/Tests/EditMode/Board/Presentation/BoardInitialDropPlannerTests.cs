using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board.Presentation
{
    public sealed class BoardInitialDropPlannerTests
    {
        private BoardInitialDropPlanner planner;

        [SetUp]
        public void SetUp()
        {
            planner = new BoardInitialDropPlanner();
        }

        [Test]
        public void Build_NullBoardThrows()
        {
            Assert.Throws<ArgumentNullException>(() => planner.Build(null));
        }

        [Test]
        public void Build_FullBoardCreatesThirtyUniqueEntries()
        {
            IReadOnlyList<BoardInitialDropEntry> entries =
                planner.Build(CreateFullBoard());
            var runtimeIds = new HashSet<long>();

            Assert.That(entries.Count, Is.EqualTo(BoardConstants.CellCount));
            for (int index = 0; index < entries.Count; index++)
            {
                Assert.That(runtimeIds.Add(entries[index].RuntimeId), Is.True);
            }
        }

        [Test]
        public void Build_OrdersXFirstThenYAscending()
        {
            IReadOnlyList<BoardInitialDropEntry> entries =
                planner.Build(CreateFullBoard());

            for (int index = 0; index < entries.Count; index++)
            {
                int expectedX = index / BoardConstants.Height;
                int expectedY = index % BoardConstants.Height;
                Assert.That(entries[index].Target, Is.EqualTo(
                    new BoardPosition(expectedX, expectedY)));
            }
        }

        [Test]
        public void Build_SourceCoordinatesUseTargetXAndFiveRowsAboveTarget()
        {
            IReadOnlyList<BoardInitialDropEntry> entries =
                planner.Build(CreateFullBoard());

            foreach (BoardInitialDropEntry entry in entries)
            {
                Assert.That(entry.SourceX, Is.EqualTo(entry.Target.X));
                Assert.That(entry.SourceY, Is.EqualTo(
                    BoardConstants.Height + entry.Target.Y));
                Assert.That(
                    entry.SourceY - entry.Target.Y,
                    Is.EqualTo(BoardConstants.Height));
            }
        }

        [Test]
        public void Build_DoesNotMutateInputBoard()
        {
            BoardState board = CreateFullBoard();
            var originalBlocks = new BoardBlock[BoardConstants.CellCount];
            for (int index = 0; index < originalBlocks.Length; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                originalBlocks[index] = board.Get(position);
            }

            planner.Build(board);

            for (int index = 0; index < originalBlocks.Length; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                Assert.That(board.Get(position), Is.SameAs(
                    originalBlocks[index]));
            }
        }

        [Test]
        public void Build_ResultCannotBeModifiedExternally()
        {
            IReadOnlyList<BoardInitialDropEntry> entries =
                planner.Build(CreateFullBoard());
            var list = (IList<BoardInitialDropEntry>)entries;

            Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
        }

        [Test]
        public void Build_EmptyCellIsRejected()
        {
            BoardState board = CreateFullBoard();
            board.Clear(new BoardPosition(2, 3));

            Assert.Throws<InvalidOperationException>(
                () => planner.Build(board));
        }

        [Test]
        public void Build_UnsupportedBlockIsRejected()
        {
            BoardState board = CreateFullBoard();
            board.Set(
                new BoardPosition(2, 3),
                new BoardBlock(1000, BoardBlockType.Rock, null));

            Assert.Throws<NotSupportedException>(() => planner.Build(board));
        }

        [Test]
        public void Build_DuplicateRuntimeIdIsRejected()
        {
            BoardState board = CreateFullBoard();
            long duplicateId = board.Get(new BoardPosition(0, 0)).RuntimeId;
            board.Set(
                new BoardPosition(5, 4),
                new BoardBlock(
                    duplicateId,
                    BoardBlockType.Normal,
                    ElementType.Fire));

            Assert.Throws<InvalidOperationException>(
                () => planner.Build(board));
        }

        [Test]
        public void Entry_SourceYBelowBoardIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BoardInitialDropEntry(
                    1,
                    new BoardPosition(0, 0),
                    BoardConstants.Height - 1));
        }

        private static BoardState CreateFullBoard()
        {
            ElementType[] elements =
            {
                ElementType.Fire,
                ElementType.Water,
                ElementType.Grass,
                ElementType.Light,
                ElementType.Dark
            };
            var board = new BoardState();
            long runtimeId = 1;

            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    board.Set(
                        new BoardPosition(x, y),
                        new BoardBlock(
                            runtimeId++,
                            BoardBlockType.Normal,
                            elements[(x + y) % elements.Length]));
                }
            }

            return board;
        }
    }
}
