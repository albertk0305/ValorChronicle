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
}
