using System;
using ValorChronicle.Battle.Board;

namespace ValorChronicle.Battle.Flow
{
    public sealed class MatchEventExecution
    {
        internal MatchEventExecution(
            long executionId,
            MatchEvent matchEvent,
            int finalComboCount)
        {
            if (executionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(executionId));
            }

            MatchEvent = matchEvent
                ?? throw new ArgumentNullException(nameof(matchEvent));
            if (finalComboCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(finalComboCount),
                    finalComboCount,
                    "Final combo count must be positive.");
            }

            ExecutionId = executionId;
            FinalComboCount = finalComboCount;
        }

        public long ExecutionId { get; }
        public MatchEvent MatchEvent { get; }
        public int FinalComboCount { get; }
    }
}
