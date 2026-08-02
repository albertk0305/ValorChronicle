using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board.Presentation
{
    public sealed class BoardShufflePresentationPlannerTests
    {
        private readonly BoardShufflePresentationPlanner planner =
            new BoardShufflePresentationPlanner();

        [Test]
        public void Build_NoneHasNoEntriesAndSameBoard()
        {
            BoardShuffleResult result =
                BoardShufflePresentationTestSupport.CreateNone(
                    out BoardState beforeBoard);

            BoardShufflePresentationPlan plan = planner.Build(
                beforeBoard,
                result);

            Assert.That(plan.Kind, Is.EqualTo(BoardShuffleKind.None));
            Assert.That(plan.HasAnimation, Is.False);
            Assert.That(plan.Entries, Is.Empty);
            Assert.That(plan.Board, Is.SameAs(result.Board));
        }

        [TestCase(BoardShuffleKind.Permutation)]
        [TestCase(BoardShuffleKind.Regeneration)]
        public void Build_ActualShuffleCopiesExactEntries(
            BoardShuffleKind kind)
        {
            BoardShuffleResult result =
                BoardShufflePresentationTestSupport.CreateChanged(
                    kind,
                    out BoardState beforeBoard);

            BoardShufflePresentationPlan plan = planner.Build(
                beforeBoard,
                result);

            Assert.That(plan.Kind, Is.EqualTo(kind));
            Assert.That(plan.HasAnimation, Is.True);
            Assert.That(plan.Entries.Count, Is.EqualTo(30));
            for (int index = 0; index < plan.Entries.Count; index++)
            {
                Assert.That(plan.Entries[index], Is.SameAs(
                    result.Entries[index]));
            }
        }

        [Test]
        public void Build_NonNormalBlockIsRetainedWithoutEntry()
        {
            BoardShuffleResult result =
                BoardShufflePresentationTestSupport.CreateChanged(
                    BoardShuffleKind.Regeneration,
                    out BoardState beforeBoard,
                    true);
            var fixedPosition = new BoardPosition(5, 4);

            BoardShufflePresentationPlan plan = planner.Build(
                beforeBoard,
                result);

            Assert.That(plan.Entries.Count, Is.EqualTo(29));
            Assert.That(result.Board.Get(fixedPosition).BlockType,
                Is.EqualTo(BoardBlockType.Rock));
            foreach (BoardShuffleEntry entry in plan.Entries)
            {
                Assert.That(entry.Position, Is.Not.EqualTo(fixedPosition));
            }
        }

        [Test]
        public void Build_WrongEntryValuesAreRejected()
        {
            BoardShuffleResult result =
                BoardShufflePresentationTestSupport.CreateChanged(
                    BoardShuffleKind.Permutation,
                    out BoardState beforeBoard);
            var entries = new List<BoardShuffleEntry>(result.Entries);
            BoardShuffleEntry first = entries[0];
            entries[0] = BoardShufflePresentationTestSupport.CreateEntry(
                first.Position,
                first.RuntimeId,
                first.NewElement,
                first.NewElement);
            BoardShuffleResult invalid =
                BoardShufflePresentationTestSupport.CreateResult(
                    result.Board,
                    result.Kind,
                    entries);

            Assert.Throws<InvalidOperationException>(() =>
                planner.Build(beforeBoard, invalid));
        }

        [Test]
        public void Build_DuplicatePositionAndRuntimeIdAreRejected()
        {
            BoardShuffleResult result =
                BoardShufflePresentationTestSupport.CreateChanged(
                    BoardShuffleKind.Permutation,
                    out BoardState beforeBoard);
            var entries = new List<BoardShuffleEntry>(result.Entries)
            {
                result.Entries[0]
            };
            BoardShuffleResult invalid =
                BoardShufflePresentationTestSupport.CreateResult(
                    result.Board,
                    result.Kind,
                    entries);

            Assert.Throws<InvalidOperationException>(() =>
                planner.Build(beforeBoard, invalid));
        }

        [Test]
        public void Build_RuntimeIdOrPositionChangesAreRejected()
        {
            BoardShuffleResult result =
                BoardShufflePresentationTestSupport.CreateChanged(
                    BoardShuffleKind.Regeneration,
                    out BoardState beforeBoard);
            BoardPosition position = result.Entries[0].Position;
            BoardBlock block = result.Board.Get(position);
            result.Board.Set(
                position,
                new BoardBlock(
                    999,
                    block.BlockType,
                    block.Element));

            Assert.Throws<InvalidOperationException>(() =>
                planner.Build(beforeBoard, result));
        }

        [Test]
        public void Build_DoesNotMutateInputsAndReturnsReadOnlyEntries()
        {
            BoardShuffleResult result =
                BoardShufflePresentationTestSupport.CreateChanged(
                    BoardShuffleKind.Permutation,
                    out BoardState beforeBoard);
            BoardBlock[] before = Capture(beforeBoard);
            BoardBlock[] after = Capture(result.Board);

            BoardShufflePresentationPlan plan = planner.Build(
                beforeBoard,
                result);

            AssertUnchanged(beforeBoard, before);
            AssertUnchanged(result.Board, after);
            Assert.Throws<NotSupportedException>(() =>
                ((IList<BoardShuffleEntry>)plan.Entries).Clear());
        }

        [Test]
        public void Build_NullInputsAreRejected()
        {
            BoardShuffleResult result =
                BoardShufflePresentationTestSupport.CreateNone(
                    out BoardState beforeBoard);

            Assert.Throws<ArgumentNullException>(() =>
                planner.Build(null, result));
            Assert.Throws<ArgumentNullException>(() =>
                planner.Build(beforeBoard, null));
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

        private static void AssertUnchanged(
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
    }

    internal static class BoardShufflePresentationTestSupport
    {
        public static BoardShuffleResult CreateNone(out BoardState beforeBoard)
        {
            beforeBoard = CreateBoard();
            return CreateResult(
                beforeBoard.Clone(),
                BoardShuffleKind.None,
                Array.Empty<BoardShuffleEntry>());
        }

        public static BoardShuffleResult CreateChanged(
            BoardShuffleKind kind,
            out BoardState beforeBoard,
            bool includeFixedBlock = false)
        {
            beforeBoard = CreateBoard();
            if (includeFixedBlock)
            {
                var fixedPosition = new BoardPosition(5, 4);
                beforeBoard.Set(
                    fixedPosition,
                    new BoardBlock(30, BoardBlockType.Rock, null));
            }

            BoardState resultBoard = beforeBoard.Clone();
            var entries = new List<BoardShuffleEntry>();
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                BoardBlock block = beforeBoard.Get(position);
                if (block.BlockType != BoardBlockType.Normal)
                {
                    continue;
                }

                ElementType previous = block.Element.Value;
                ElementType next = (ElementType)(((int)previous + 1) % 5);
                resultBoard.Set(
                    position,
                    new BoardBlock(
                        block.RuntimeId,
                        BoardBlockType.Normal,
                        next));
                entries.Add(CreateEntry(
                    position,
                    block.RuntimeId,
                    previous,
                    next));
            }

            return CreateResult(resultBoard, kind, entries);
        }

        public static BoardShuffleEntry CreateEntry(
            BoardPosition position,
            long runtimeId,
            ElementType previous,
            ElementType next)
        {
            return CreateInternal<BoardShuffleEntry>(
                position,
                runtimeId,
                previous,
                next);
        }

        public static BoardShuffleResult CreateResult(
            BoardState board,
            BoardShuffleKind kind,
            IReadOnlyList<BoardShuffleEntry> entries)
        {
            return CreateInternal<BoardShuffleResult>(
                board,
                kind,
                entries,
                kind == BoardShuffleKind.None ? 0 : 1);
        }

        private static BoardState CreateBoard()
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
                        (ElementType)((position.X + position.Y) % 5)));
            }

            return board;
        }

        private static T CreateInternal<T>(params object[] arguments)
        {
            return (T)Activator.CreateInstance(
                typeof(T),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                arguments,
                null);
        }
    }
}
