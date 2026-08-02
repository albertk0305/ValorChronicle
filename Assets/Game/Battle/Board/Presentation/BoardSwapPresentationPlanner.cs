using System;
using System.Collections.Generic;

namespace ValorChronicle.Battle.Board.Presentation
{
    public sealed class BoardSwapPresentationPlanner
    {
        private static readonly IReadOnlyList<BoardSwapViewMotion>
            NoMotions = Array.AsReadOnly(Array.Empty<BoardSwapViewMotion>());

        public BoardSwapPresentationPlan Build(
            BoardState beforeBoard,
            BoardSwapActionResult result)
        {
            if (beforeBoard == null)
            {
                throw new ArgumentNullException(nameof(beforeBoard));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            BoardPosition first = result.Swap.First;
            BoardPosition second = result.Swap.Second;
            BoardBlock firstBlock = beforeBoard.Get(first);
            BoardBlock secondBlock = beforeBoard.Get(second);
            if (firstBlock == null || secondBlock == null)
            {
                throw new InvalidOperationException(
                    "Both swap positions must be occupied before presentation.");
            }

            switch (result.Status)
            {
                case BoardSwapActionStatus.NotSwappable:
                    ValidateSameLayout(
                        beforeBoard,
                        result.Board,
                        "A not-swappable result board must match beforeBoard.");
                    return new BoardSwapPresentationPlan(
                        result.Status,
                        NoMotions,
                        false);

                case BoardSwapActionStatus.NoMatch:
                    ValidateSwappedLayout(
                        beforeBoard,
                        result.SwappedBoard,
                        first,
                        second);
                    ValidateSameLayout(
                        beforeBoard,
                        result.Board,
                        "A no-match result board must match beforeBoard.");
                    return BuildMotionPlan(
                        result,
                        firstBlock,
                        secondBlock,
                        true);

                case BoardSwapActionStatus.Resolved:
                    ValidateSwappedLayout(
                        beforeBoard,
                        result.SwappedBoard,
                        first,
                        second);
                    return BuildMotionPlan(
                        result,
                        firstBlock,
                        secondBlock,
                        false);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(result),
                        result.Status,
                        "Unknown swap action status.");
            }
        }

        private static BoardSwapPresentationPlan BuildMotionPlan(
            BoardSwapActionResult result,
            BoardBlock firstBlock,
            BoardBlock secondBlock,
            bool requiresSwapBack)
        {
            BoardSwap swap = result.Swap;
            var motions = new[]
            {
                new BoardSwapViewMotion(
                    firstBlock.RuntimeId,
                    swap.First,
                    swap.Second),
                new BoardSwapViewMotion(
                    secondBlock.RuntimeId,
                    swap.Second,
                    swap.First)
            };

            return new BoardSwapPresentationPlan(
                result.Status,
                motions,
                requiresSwapBack);
        }

        private static void ValidateSwappedLayout(
            BoardState beforeBoard,
            BoardState swappedBoard,
            BoardPosition first,
            BoardPosition second)
        {
            if (swappedBoard == null)
            {
                throw new InvalidOperationException(
                    "A swappable result must contain SwappedBoard.");
            }

            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    BoardBlock expected;
                    if (position == first)
                    {
                        expected = beforeBoard.Get(second);
                    }
                    else if (position == second)
                    {
                        expected = beforeBoard.Get(first);
                    }
                    else
                    {
                        expected = beforeBoard.Get(position);
                    }

                    if (!BlocksMatch(expected, swappedBoard.Get(position)))
                    {
                        throw new InvalidOperationException(
                            "SwappedBoard must contain exactly the two " +
                            $"exchanged blocks. Mismatch at {position}.");
                    }
                }
            }
        }

        private static void ValidateSameLayout(
            BoardState expected,
            BoardState actual,
            string message)
        {
            if (actual == null)
            {
                throw new InvalidOperationException(message);
            }

            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    if (!BlocksMatch(
                        expected.Get(position),
                        actual.Get(position)))
                    {
                        throw new InvalidOperationException(
                            $"{message} Mismatch at {position}.");
                    }
                }
            }
        }

        private static bool BlocksMatch(BoardBlock expected, BoardBlock actual)
        {
            if (expected == null || actual == null)
            {
                return expected == null && actual == null;
            }

            return expected.RuntimeId == actual.RuntimeId
                && expected.BlockType == actual.BlockType
                && expected.Element == actual.Element;
        }
    }
}
