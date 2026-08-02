using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board.Presentation
{
    public sealed class BoardSwapPresentationPlannerTests
    {
        private BoardSwapPresentationPlanner planner;

        [SetUp]
        public void SetUp()
        {
            planner = new BoardSwapPresentationPlanner();
        }

        [Test]
        public void Build_NullInputsThrow()
        {
            BoardSwapActionResult result =
                BoardSwapPresentationTestSupport.CreateNoMatch(
                    out BoardState board);

            Assert.Throws<ArgumentNullException>(() =>
                planner.Build(null, result));
            Assert.Throws<ArgumentNullException>(() =>
                planner.Build(board, null));
        }

        [Test]
        public void Build_NotSwappableHasNoMotion()
        {
            BoardSwapActionResult result =
                BoardSwapPresentationTestSupport.CreateNotSwappable(
                    out BoardState board);

            BoardSwapPresentationPlan plan = planner.Build(board, result);

            Assert.That(plan.Status, Is.EqualTo(
                BoardSwapActionStatus.NotSwappable));
            Assert.That(plan.Motions, Is.Empty);
            Assert.That(plan.RequiresSwapBack, Is.False);
        }

        [TestCase(BoardSwapActionStatus.NoMatch, true)]
        [TestCase(BoardSwapActionStatus.Resolved, false)]
        public void Build_SwappableResultCreatesTwoExactMotions(
            BoardSwapActionStatus status,
            bool expectedSwapBack)
        {
            BoardState board;
            BoardSwapActionResult result = status == BoardSwapActionStatus.NoMatch
                ? BoardSwapPresentationTestSupport.CreateNoMatch(out board)
                : BoardSwapPresentationTestSupport.CreateResolved(out board);
            BoardBlock first = board.Get(result.Swap.First);
            BoardBlock second = board.Get(result.Swap.Second);

            BoardSwapPresentationPlan plan = planner.Build(board, result);

            Assert.That(plan.Status, Is.EqualTo(status));
            Assert.That(plan.Motions.Count, Is.EqualTo(2));
            Assert.That(plan.RequiresSwapBack, Is.EqualTo(expectedSwapBack));
            AssertMotion(
                plan.Motions[0],
                first.RuntimeId,
                result.Swap.First,
                result.Swap.Second);
            AssertMotion(
                plan.Motions[1],
                second.RuntimeId,
                result.Swap.Second,
                result.Swap.First);
        }

        [Test]
        public void Build_InvalidSwappedBoardIsRejected()
        {
            BoardSwapActionResult result =
                BoardSwapPresentationTestSupport.CreateNoMatch(
                    out BoardState board);
            result.SwappedBoard.Swap(
                result.Swap.First,
                result.Swap.Second);

            Assert.Throws<InvalidOperationException>(() =>
                planner.Build(board, result));
        }

        [Test]
        public void Build_NoMatchFinalBoardDifferentFromBeforeIsRejected()
        {
            BoardSwapActionResult result =
                BoardSwapPresentationTestSupport.CreateNoMatch(
                    out BoardState board);
            result.Board.Swap(
                result.Swap.First,
                result.Swap.Second);

            Assert.Throws<InvalidOperationException>(() =>
                planner.Build(board, result));
        }

        [Test]
        public void Build_DoesNotMutateAnyInputBoard()
        {
            BoardSwapActionResult result =
                BoardSwapPresentationTestSupport.CreateResolved(
                    out BoardState board);
            BoardBlock[] before = Capture(board);
            BoardBlock[] swapped = Capture(result.SwappedBoard);
            BoardBlock[] final = Capture(result.Board);

            planner.Build(board, result);

            AssertUnchanged(board, before);
            AssertUnchanged(result.SwappedBoard, swapped);
            AssertUnchanged(result.Board, final);
        }

        [Test]
        public void Build_ResultMotionsCannotBeModifiedExternally()
        {
            BoardSwapActionResult result =
                BoardSwapPresentationTestSupport.CreateNoMatch(
                    out BoardState board);
            BoardSwapPresentationPlan plan = planner.Build(board, result);
            var motions = (IList<BoardSwapViewMotion>)plan.Motions;

            Assert.Throws<NotSupportedException>(() => motions.RemoveAt(0));
        }

        private static void AssertMotion(
            BoardSwapViewMotion motion,
            long runtimeId,
            BoardPosition from,
            BoardPosition to)
        {
            Assert.That(motion.RuntimeId, Is.EqualTo(runtimeId));
            Assert.That(motion.From, Is.EqualTo(from));
            Assert.That(motion.To, Is.EqualTo(to));
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

    internal static class BoardSwapPresentationTestSupport
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

        public static BoardSwapActionResult CreateNotSwappable(
            out BoardState board)
        {
            board = CreateBoard(PlayableBoardElements);
            var position = new BoardPosition(2, 2);
            return CreateResolver(new SequenceRandomSource()).Resolve(
                board,
                new BoardSwap(position, position));
        }

        public static BoardSwapActionResult CreateNoMatch(
            out BoardState board)
        {
            board = CreateBoard(DeadBoardElements);
            return CreateResolver(new SequenceRandomSource()).Resolve(
                board,
                new BoardSwap(
                    new BoardPosition(0, 0),
                    new BoardPosition(1, 0)));
        }

        public static BoardSwapActionResult CreateResolved(
            out BoardState board)
        {
            board = CreateBoard(PlayableBoardElements);
            SetElement(board, 0, 4, ElementType.Grass);
            SetElement(board, 1, 4, ElementType.Grass);
            SetElement(board, 2, 4, ElementType.Dark);
            SetElement(board, 3, 4, ElementType.Grass);

            return CreateResolver(
                new SequenceRandomSource(0, 4, 1)).Resolve(
                    board,
                    new BoardSwap(
                        new BoardPosition(2, 4),
                        new BoardPosition(3, 4)));
        }

        private static BoardSwapActionResolver CreateResolver(
            IRandomSource randomSource)
        {
            var moveAnalyzer = new BoardMoveAnalyzer();
            var refiller = new BoardRefiller(
                randomSource,
                new BoardBlockIdGenerator(31));
            var cascadeResolver = new BoardCascadeResolver(
                new BoardCascadeStepResolver(refiller));
            var shuffler = new BoardShuffler(randomSource, moveAnalyzer);
            return new BoardSwapActionResolver(
                moveAnalyzer,
                cascadeResolver,
                shuffler);
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
                            (ElementType)elements[
                                (x * BoardConstants.Height) + y]));
                }
            }

            return board;
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
                new BoardBlock(
                    runtimeId,
                    BoardBlockType.Normal,
                    element));
        }

        private sealed class SequenceRandomSource : IRandomSource
        {
            private readonly Queue<int> values;

            public SequenceRandomSource(params int[] values)
            {
                this.values = new Queue<int>(values);
            }

            public int Next(int minInclusive, int maxExclusive)
            {
                if (values.Count == 0)
                {
                    throw new InvalidOperationException(
                        "The deterministic random sequence was exhausted.");
                }

                int value = values.Dequeue();
                if (value < minInclusive || value >= maxExclusive)
                {
                    throw new InvalidOperationException(
                        $"Random value {value} is outside " +
                        $"[{minInclusive}, {maxExclusive}).");
                }

                return value;
            }

            public float NextFloat()
            {
                throw new InvalidOperationException(
                    "No float random value was expected.");
            }
        }
    }
}
