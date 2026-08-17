using System;

namespace ValorChronicle.Battle.Board.Presentation
{
    public sealed class BoardActionExecution
    {
        internal BoardActionExecution(
            long actionId,
            BoardSwapActionResult result)
        {
            if (actionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(actionId));
            }

            Result = result ?? throw new ArgumentNullException(nameof(result));
            ActionId = actionId;
        }

        public long ActionId { get; }
        public BoardSwapActionResult Result { get; }
    }

    public sealed class BoardCascadeStepPresentation
    {
        internal BoardCascadeStepPresentation(
            long actionId,
            int cascadeStepIndex,
            BoardCascadeStep step)
        {
            if (actionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(actionId));
            }

            if (cascadeStepIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cascadeStepIndex));
            }

            Step = step ?? throw new ArgumentNullException(nameof(step));
            ActionId = actionId;
            CascadeStepIndex = cascadeStepIndex;
        }

        public long ActionId { get; }
        public int CascadeStepIndex { get; }
        public BoardCascadeStep Step { get; }
    }
}
