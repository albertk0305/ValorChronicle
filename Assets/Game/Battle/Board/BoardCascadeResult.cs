using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardCascadeResult
    {
        private readonly ReadOnlyCollection<BoardCascadeStep> steps;

        internal BoardCascadeResult(
            BoardState board,
            IReadOnlyList<BoardCascadeStep> steps)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (steps == null)
            {
                throw new ArgumentNullException(nameof(steps));
            }

            Board = board;

            var copiedSteps = new BoardCascadeStep[steps.Count];
            int comboCount = 0;
            int totalRemovedBlockCount = 0;
            int totalSpawnedBlockCount = 0;
            int totalMoveCount = 0;

            for (int index = 0; index < steps.Count; index++)
            {
                BoardCascadeStep step = steps[index];
                if (step == null)
                {
                    throw new ArgumentException(
                        "Steps cannot contain null.",
                        nameof(steps));
                }

                copiedSteps[index] = step;
                comboCount += step.MatchEventCount;
                totalRemovedBlockCount += step.RemovedBlockCount;
                totalSpawnedBlockCount += step.Refill.Spawns.Count;
                totalMoveCount += step.Collapse.Moves.Count;
            }

            this.steps = Array.AsReadOnly(copiedSteps);
            ComboCount = comboCount;
            TotalRemovedBlockCount = totalRemovedBlockCount;
            TotalSpawnedBlockCount = totalSpawnedBlockCount;
            TotalMoveCount = totalMoveCount;
        }

        public BoardState Board { get; }
        public IReadOnlyList<BoardCascadeStep> Steps => steps;
        public int CascadeCount => steps.Count;
        public int ComboCount { get; }
        public int TotalRemovedBlockCount { get; }
        public int TotalSpawnedBlockCount { get; }
        public int TotalMoveCount { get; }
    }
}
