using System;

namespace ValorChronicle.Battle.Board
{
    public sealed class BattleBoardMutationCompletion
    {
        public BattleBoardMutationCompletion(
            BattleBoardMutationCommand command,
            BoardRockMutationResult result,
            BattleBoardMutationCompletionStatus status,
            Exception failure = null)
        {
            Command = command
                ?? throw new ArgumentNullException(nameof(command));
            if (!Enum.IsDefined(
                typeof(BattleBoardMutationCompletionStatus),
                status))
            {
                throw new ArgumentOutOfRangeException(nameof(status));
            }

            if (status == BattleBoardMutationCompletionStatus.Completed
                && (result == null || !result.Succeeded || failure != null))
            {
                throw new ArgumentException(
                    "A completed board mutation requires a successful "
                        + "result and no failure.",
                    nameof(result));
            }

            Result = result;
            Status = status;
            Failure = failure;
        }

        public BattleBoardMutationCommand Command { get; }
        public BoardRockMutationResult Result { get; }
        public BattleBoardMutationCompletionStatus Status { get; }
        public Exception Failure { get; }
        public bool Succeeded =>
            Status == BattleBoardMutationCompletionStatus.Completed;
    }
}
