using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board.Presentation
{
    public sealed class BoardCascadeStepPresentationPlannerTests
    {
        private readonly BoardCascadeStepPresentationPlanner planner =
            new BoardCascadeStepPresentationPlanner();

        [Test]
        public void Build_SingleMatchCopiesExactOrderedStepData()
        {
            BoardCascadeStep step = CreateSingleMatchStep(out BoardState board);
            BoardBlock[] original = Capture(board);

            BoardCascadeStepPresentationPlan plan = planner.Build(board, step);

            Assert.That(plan.Removals.Count, Is.EqualTo(3));
            Assert.That(plan.Moves.Count, Is.EqualTo(6));
            Assert.That(plan.Spawns.Count, Is.EqualTo(3));
            Assert.That(plan.CollapseBoard, Is.SameAs(step.Collapse.Board));
            Assert.That(plan.Board, Is.SameAs(step.Board));
            for (int index = 0; index < plan.Removals.Count; index++)
            {
                Assert.That(plan.Removals[index], Is.SameAs(
                    step.Collapse.Removals[index]));
            }

            for (int index = 0; index < plan.Moves.Count; index++)
            {
                Assert.That(plan.Moves[index], Is.SameAs(
                    step.Collapse.Moves[index]));
            }

            for (int index = 0; index < plan.Spawns.Count; index++)
            {
                Assert.That(plan.Spawns[index], Is.SameAs(
                    step.Refill.Spawns[index]));
            }

            AssertBoardReferences(board, original);
        }

        [Test]
        public void Build_SimultaneousMatchesRetainMatchRemovalOrder()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 0, ElementType.Dark);
            SetHorizontal(board, 3, 5, 4, ElementType.Light);
            BoardCascadeStep step = Resolve(board);

            BoardCascadeStepPresentationPlan plan = planner.Build(board, step);

            int removalIndex = 0;
            foreach (BoardMatch match in step.Matches)
            {
                foreach (BoardPosition position in match.Positions)
                {
                    Assert.That(
                        plan.Removals[removalIndex].Position,
                        Is.EqualTo(position));
                    removalIndex++;
                }
            }

            Assert.That(removalIndex, Is.EqualTo(6));
        }

        [Test]
        public void Build_ReturnsExternallyReadOnlyCollections()
        {
            BoardCascadeStep step = CreateSingleMatchStep(out BoardState board);
            BoardCascadeStepPresentationPlan plan = planner.Build(board, step);

            Assert.Throws<NotSupportedException>(() =>
                ((IList<BoardBlockRemoval>)plan.Removals).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<BoardBlockMove>)plan.Moves).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<BoardBlockSpawn>)plan.Spawns).Clear());
        }

        [Test]
        public void Build_NullInputsAreRejected()
        {
            BoardCascadeStep step = CreateSingleMatchStep(out BoardState board);

            Assert.Throws<ArgumentNullException>(() =>
                planner.Build(null, step));
            Assert.Throws<ArgumentNullException>(() =>
                planner.Build(board, null));
        }

        [Test]
        public void Build_RemovalThatNoLongerMatchesBeforeBoardIsRejected()
        {
            BoardCascadeStep step = CreateSingleMatchStep(out BoardState board);
            BoardBlockRemoval removal = step.Collapse.Removals[0];
            board.Set(
                removal.Position,
                new BoardBlock(
                    999,
                    BoardBlockType.Normal,
                    ElementType.Fire));

            Assert.Throws<InvalidOperationException>(() =>
                planner.Build(board, step));
        }

        [Test]
        public void Build_InvalidCollapseBoardIsRejected()
        {
            BoardCascadeStep step = CreateSingleMatchStep(out BoardState board);
            BoardBlockMove move = step.Collapse.Moves[0];
            step.Collapse.Board.Clear(move.To);

            Assert.Throws<InvalidOperationException>(() =>
                planner.Build(board, step));
        }

        [Test]
        public void Build_DuplicateMoveIsRejected()
        {
            BoardCascadeStep step = CreateSingleMatchStep(out BoardState board);
            var moves = new List<BoardBlockMove>(step.Collapse.Moves)
            {
                step.Collapse.Moves[0]
            };
            BoardCollapseResult collapse = CreateInternal<BoardCollapseResult>(
                step.Collapse.Board,
                new List<BoardBlockRemoval>(step.Collapse.Removals),
                moves);
            BoardCascadeStep invalidStep = CreateInternal<BoardCascadeStep>(
                step.Matches,
                collapse,
                step.Refill);

            Assert.Throws<InvalidOperationException>(() =>
                planner.Build(board, invalidStep));
        }

        [Test]
        public void Build_OccupiedSpawnTargetIsRejected()
        {
            BoardCascadeStep step = CreateSingleMatchStep(out BoardState board);
            BoardBlockSpawn spawn = step.Refill.Spawns[0];
            step.Collapse.Board.Set(
                spawn.Target,
                new BoardBlock(
                    1000,
                    BoardBlockType.Normal,
                    ElementType.Water));

            Assert.Throws<InvalidOperationException>(() =>
                planner.Build(board, step));
        }

        [Test]
        public void Build_DuplicateSpawnIsRejected()
        {
            BoardCascadeStep step = CreateSingleMatchStep(out BoardState board);
            var spawns = new List<BoardBlockSpawn>(step.Refill.Spawns)
            {
                step.Refill.Spawns[0]
            };
            BoardRefillResult refill = CreateInternal<BoardRefillResult>(
                step.Board,
                spawns);
            BoardCascadeStep invalidStep = CreateInternal<BoardCascadeStep>(
                step.Matches,
                step.Collapse,
                refill);

            Assert.Throws<InvalidOperationException>(() =>
                planner.Build(board, invalidStep));
        }

        [Test]
        public void Build_IncompleteFinalBoardIsRejected()
        {
            BoardCascadeStep step = CreateSingleMatchStep(out BoardState board);
            step.Board.Clear(new BoardPosition(5, 4));

            Assert.Throws<InvalidOperationException>(() =>
                planner.Build(board, step));
        }

        [Test]
        public void Build_DuplicateFinalRuntimeIdIsRejected()
        {
            BoardCascadeStep step = CreateSingleMatchStep(out BoardState board);
            BoardBlock duplicate = step.Board.Get(new BoardPosition(0, 0));
            step.Board.Set(new BoardPosition(5, 4), duplicate);

            Assert.Throws<InvalidOperationException>(() =>
                planner.Build(board, step));
        }

        [Test]
        public void Build_UnsupportedFinalBlockIsRejected()
        {
            BoardCascadeStep step = CreateSingleMatchStep(out BoardState board);
            step.Board.Set(
                new BoardPosition(5, 4),
                new BoardBlock(999, BoardBlockType.Rock, null));

            Assert.Throws<NotSupportedException>(() =>
                planner.Build(board, step));
        }

        internal static BoardCascadeStep CreateSingleMatchStep(
            out BoardState board)
        {
            board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 2, ElementType.Water);
            return Resolve(board);
        }

        internal static BoardCascadeStep Resolve(BoardState board)
        {
            var resolver = new BoardCascadeStepResolver(
                new BoardRefiller(
                    new SeededRandomSource(2718),
                    new BoardBlockIdGenerator(31)));
            Assert.That(resolver.TryResolve(board, out BoardCascadeStep step),
                Is.True);
            return step;
        }

        internal static BoardState CreateBoardWithoutMatches()
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
                            elements[(x + (2 * y)) % elements.Length]));
                }
            }

            return board;
        }

        internal static void SetHorizontal(
            BoardState board,
            int minX,
            int maxX,
            int y,
            ElementType element)
        {
            for (int x = minX; x <= maxX; x++)
            {
                BoardBlock previous = board.Get(new BoardPosition(x, y));
                board.Set(
                    new BoardPosition(x, y),
                    new BoardBlock(
                        previous.RuntimeId,
                        BoardBlockType.Normal,
                        element));
            }
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

        private static void AssertBoardReferences(
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
