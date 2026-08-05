using System;

namespace ValorChronicle.Battle.Board.Presentation
{
    public sealed class BoardActionCompletion
    {
        internal BoardActionCompletion(
            long actionId,
            BoardSwapActionResult result,
            BoardActionCompletionStatus status,
            Exception failure)
        {
            if (actionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(actionId));
            }

            if (!Enum.IsDefined(typeof(BoardActionCompletionStatus), status))
            {
                throw new ArgumentOutOfRangeException(nameof(status));
            }

            if (status == BoardActionCompletionStatus.Failed
                && failure == null)
            {
                throw new ArgumentNullException(nameof(failure));
            }

            Result = result ?? throw new ArgumentNullException(nameof(result));
            ActionId = actionId;
            CompletionStatus = status;
            Failure = failure;
        }

        public long ActionId { get; }
        public BoardSwapActionResult Result { get; }
        public BoardActionCompletionStatus CompletionStatus { get; }
        public Exception Failure { get; }
    }
}
