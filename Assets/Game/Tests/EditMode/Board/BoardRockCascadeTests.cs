using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board
{
    public sealed class BoardRockCascadeTests
    {
        [Test]
        public void OrthogonalRocksAreCollateralButDiagonalRockSurvives()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 1, 3, 2, ElementType.Water);
            BoardBlock below = SetRock(board, 1, 1, runtimeId: 101);
            BoardBlock above = SetRock(board, 3, 3, runtimeId: 102);
            BoardBlock diagonal = SetRock(board, 0, 1, runtimeId: 103);

            BoardCascadeStep step = ResolveStep(board);
            BoardCascadeResult cascade = Cascade(step);
            IReadOnlyList<MatchEvent> events = MatchEventFactory.Create(cascade);

            Assert.That(step.Matches, Has.Count.EqualTo(1));
            Assert.That(step.Matches[0].BlockCount, Is.EqualTo(3));
            Assert.That(step.Collapse.Removals, Has.Count.EqualTo(5));
            Assert.That(RemovedRuntimeIds(step),
                Does.Contain(below.RuntimeId));
            Assert.That(RemovedRuntimeIds(step),
                Does.Contain(above.RuntimeId));
            Assert.That(RemovedRuntimeIds(step).Contains(diagonal.RuntimeId),
                Is.False);
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].RemovedBlockCount, Is.EqualTo(3));
            Assert.That(cascade.ComboCount, Is.EqualTo(1));
        }

        [Test]
        public void RockAdjacentToTwoCellsOfOneMatchIsRemovedOnce()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 1, 3, 1, ElementType.Fire);
            SetVertical(board, 1, 1, 3, ElementType.Fire);
            BoardBlock sharedRock = SetRock(
                board,
                2,
                2,
                runtimeId: 101);

            BoardCascadeStep step = ResolveStep(board);

            Assert.That(step.Matches, Has.Count.EqualTo(1));
            Assert.That(step.Matches[0].BlockCount, Is.EqualTo(5));
            Assert.That(step.Collapse.Removals, Has.Count.EqualTo(6));
            Assert.That(RemovedRuntimeIds(step).Count(
                runtimeId => runtimeId == sharedRock.RuntimeId),
                Is.EqualTo(1));
        }

        [Test]
        public void RockSharedByTwoMatchGroupsIsRemovedOnceWithoutExtraEvent()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 1, ElementType.Fire);
            SetNormal(board, 3, 1, ElementType.Light);
            SetHorizontal(board, 0, 2, 3, ElementType.Water);
            BoardBlock sharedRock = SetRock(
                board,
                1,
                2,
                runtimeId: 101);

            BoardCascadeStep step = ResolveStep(board);
            IReadOnlyList<MatchEvent> events = MatchEventFactory.Create(
                Cascade(step));

            Assert.That(step.Matches, Has.Count.EqualTo(2));
            Assert.That(step.Collapse.Removals, Has.Count.EqualTo(7));
            Assert.That(RemovedRuntimeIds(step).Count(
                runtimeId => runtimeId == sharedRock.RuntimeId),
                Is.EqualTo(1));
            Assert.That(events, Has.Count.EqualTo(2));
            Assert.That(events.Select(item => item.RemovedBlockCount),
                Is.EqualTo(new[] { 3, 3 }));
        }

        [Test]
        public void PresentationPlannerAcceptsValidatedCollateralRockRemoval()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 1, 3, 2, ElementType.Grass);
            SetRock(board, 2, 1, runtimeId: 101);
            BoardBlock survivingRock = SetRock(
                board,
                5,
                4,
                runtimeId: 102);
            BoardCascadeStep step = ResolveStep(board);

            BoardCascadeStepPresentationPlan plan =
                new BoardCascadeStepPresentationPlanner().Build(board, step);

            Assert.That(plan.Removals, Has.Count.EqualTo(4));
            Assert.That(plan.Removals.Count(removal =>
                    removal.Block.BlockType == BoardBlockType.Rock),
                Is.EqualTo(1));
            Assert.That(step.Board.Get(new BoardPosition(5, 4)),
                Is.SameAs(survivingRock));
        }

        [Test]
        public void MatchEventFactoryRejectsUnrelatedExtraNormalRemoval()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 1, 3, 2, ElementType.Dark);
            BoardCascadeStep step = ResolveStep(board);
            var removals = new List<BoardBlockRemoval>(
                step.Collapse.Removals)
            {
                CreateInternal<BoardBlockRemoval>(
                    new BoardBlock(
                        999,
                        BoardBlockType.Normal,
                        ElementType.Fire),
                    new BoardPosition(0, 1))
            };
            BoardCollapseResult collapse =
                CreateInternal<BoardCollapseResult>(
                    step.Collapse.Board,
                    removals,
                    new List<BoardBlockMove>(step.Collapse.Moves));
            BoardCascadeStep invalidStep =
                CreateInternal<BoardCascadeStep>(
                    step.Matches,
                    collapse,
                    step.Refill);

            Assert.Throws<InvalidOperationException>(() =>
                MatchEventFactory.Create(Cascade(invalidStep)));
        }

        [Test]
        public void LaterCascadeMatchCanDestroyRock()
        {
            BoardState board = CreateBoardWithoutMatches();
            SetHorizontal(board, 0, 2, 0, ElementType.Light);
            SetNormal(board, 3, 0, ElementType.Water);
            SetRock(board, 3, 4, runtimeId: 101);
            var stepResolver = new BoardCascadeStepResolver(
                new BoardRefiller(
                    new PrefixedRandomSource(
                        new[] { 0, 0, 0 },
                        fallbackSeed: 314159),
                    new BoardBlockIdGenerator(1000)));
            var resolver = new BoardCascadeResolver(stepResolver);
            BoardCascadeResult cascade = resolver.Resolve(board);

            Assert.That(cascade.CascadeCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(cascade.Steps.Skip(1).Any(step =>
                step.Collapse.Removals.Any(removal =>
                    removal.Block.BlockType == BoardBlockType.Rock)),
                Is.True);
            IReadOnlyList<MatchEvent> events =
                MatchEventFactory.Create(cascade);
            Assert.That(events.Count, Is.EqualTo(cascade.ComboCount));
            Assert.That(events.Sum(item => item.RemovedBlockCount),
                Is.LessThan(cascade.TotalRemovedBlockCount));
        }

        [Test]
        public void RockCannotSwapOrMatch()
        {
            var board = new BoardState();
            var first = new BoardPosition(0, 0);
            var second = new BoardPosition(1, 0);
            var third = new BoardPosition(2, 0);
            board.Set(first,
                new BoardBlock(1, BoardBlockType.Rock, null));
            board.Set(second,
                new BoardBlock(2, BoardBlockType.Rock, null));
            board.Set(third,
                new BoardBlock(3, BoardBlockType.Rock, null));
            var normalPosition = new BoardPosition(0, 1);
            board.Set(normalPosition,
                new BoardBlock(
                    4,
                    BoardBlockType.Normal,
                    ElementType.Fire));
            var analyzer = new BoardMoveAnalyzer();

            Assert.That(analyzer.CanSwap(board, first, second), Is.False);
            Assert.That(analyzer.CanSwap(board, first, normalPosition),
                Is.False);
            Assert.That(analyzer.CanSwap(board, normalPosition, first),
                Is.False);
            Assert.That(BoardMatchFinder.FindMatches(board), Is.Empty);
        }

        private static BoardCascadeStep ResolveStep(BoardState board)
        {
            var resolver = new BoardCascadeStepResolver(
                new BoardRefiller(
                    new SeededRandomSource(314159),
                    new BoardBlockIdGenerator(1000)));
            Assert.That(resolver.TryResolve(board, out BoardCascadeStep step),
                Is.True);
            return step;
        }

        private static BoardCascadeResult Cascade(BoardCascadeStep step)
        {
            return CreateInternal<BoardCascadeResult>(
                step.Board,
                new List<BoardCascadeStep> { step });
        }

        private static BoardState CreateBoardWithoutMatches()
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
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    board.Set(position, new BoardBlock(
                        position.ToIndex() + 1,
                        BoardBlockType.Normal,
                        elements[(x + (2 * y)) % elements.Length]));
                }
            }

            Assert.That(BoardMatchFinder.FindMatches(board), Is.Empty);
            return board;
        }

        private static void SetHorizontal(
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

        private static void SetVertical(
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

        private static void SetNormal(
            BoardState board,
            int x,
            int y,
            ElementType element)
        {
            var position = new BoardPosition(x, y);
            board.Set(position, new BoardBlock(
                board.Get(position).RuntimeId,
                BoardBlockType.Normal,
                element));
        }

        private static BoardBlock SetRock(
            BoardState board,
            int x,
            int y,
            long runtimeId)
        {
            var rock = new BoardBlock(
                runtimeId,
                BoardBlockType.Rock,
                null);
            board.Set(new BoardPosition(x, y), rock);
            return rock;
        }

        private static long[] RemovedRuntimeIds(BoardCascadeStep step)
        {
            return step.Collapse.Removals.Select(
                removal => removal.RuntimeId).ToArray();
        }

        private static T CreateInternal<T>(params object[] arguments)
        {
            return (T)Activator.CreateInstance(
                typeof(T),
                BindingFlags.Instance
                    | BindingFlags.Public
                    | BindingFlags.NonPublic,
                binder: null,
                args: arguments,
                culture: null);
        }

        private sealed class PrefixedRandomSource : IRandomSource
        {
            private readonly IReadOnlyList<int> prefix;
            private readonly SeededRandomSource fallback;
            private int index;

            public PrefixedRandomSource(
                IReadOnlyList<int> prefix,
                int fallbackSeed)
            {
                this.prefix = prefix;
                fallback = new SeededRandomSource(fallbackSeed);
            }

            public int Next(int minInclusive, int maxExclusive)
            {
                if (index >= prefix.Count)
                {
                    return fallback.Next(minInclusive, maxExclusive);
                }

                int value = prefix[index++];
                if (value < minInclusive || value >= maxExclusive)
                {
                    throw new InvalidOperationException(
                        $"Prefixed value {value} is outside "
                            + $"[{minInclusive}, {maxExclusive}).");
                }

                return value;
            }

            public float NextFloat()
            {
                return fallback.NextFloat();
            }
        }
    }
}
