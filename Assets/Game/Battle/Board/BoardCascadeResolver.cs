using System;
using System.Collections.Generic;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardCascadeResolver
    {
        public const int DefaultMaxCascadeSteps = 100;

        private readonly BoardCascadeStepResolver stepResolver;
        private readonly Func<BoardState, IReadOnlyList<BoardMatch>> matchFinder;
        private readonly int maxCascadeSteps;

        public BoardCascadeResolver(
            BoardCascadeStepResolver stepResolver,
            int maxCascadeSteps = DefaultMaxCascadeSteps)
            : this(
                stepResolver,
                BoardMatchFinder.FindMatches,
                maxCascadeSteps)
        {
        }

        public BoardCascadeResolver(
            BoardCascadeStepResolver stepResolver,
            Func<BoardState, IReadOnlyList<BoardMatch>> matchFinder,
            int maxCascadeSteps = DefaultMaxCascadeSteps)
        {
            this.stepResolver = stepResolver
                ?? throw new ArgumentNullException(nameof(stepResolver));
            this.matchFinder = matchFinder
                ?? throw new ArgumentNullException(nameof(matchFinder));

            if (maxCascadeSteps <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxCascadeSteps),
                    maxCascadeSteps,
                    "Maximum cascade steps must be positive.");
            }

            this.maxCascadeSteps = maxCascadeSteps;
        }

        public BoardCascadeResult Resolve(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            BoardState currentBoard = board.Clone();
            var steps = new List<BoardCascadeStep>();

            for (int cascadeIndex = 0;
                cascadeIndex < maxCascadeSteps;
                cascadeIndex++)
            {
                if (!stepResolver.TryResolve(
                    currentBoard,
                    out BoardCascadeStep step))
                {
                    return new BoardCascadeResult(currentBoard, steps);
                }

                if (step == null)
                {
                    throw new InvalidOperationException(
                        "The cascade step resolver returned success without a step.");
                }

                steps.Add(step);
                currentBoard = step.Board;
            }

            IReadOnlyList<BoardMatch> remainingMatches =
                matchFinder(currentBoard);
            if (remainingMatches == null)
            {
                throw new InvalidOperationException(
                    "The match finder returned null.");
            }

            if (remainingMatches.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Cascade resolution exceeded the limit of {maxCascadeSteps} steps.");
            }

            return new BoardCascadeResult(currentBoard, steps);
        }
    }
}
