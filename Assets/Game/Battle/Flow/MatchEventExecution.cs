using System;
using ValorChronicle.Battle.Board;

namespace ValorChronicle.Battle.Flow
{
    public sealed class MatchEventExecution
    {
        internal MatchEventExecution(
            long executionId,
            MatchEvent matchEvent)
        {
            if (executionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(executionId));
            }

            MatchEvent = matchEvent
                ?? throw new ArgumentNullException(nameof(matchEvent));
            ExecutionId = executionId;
        }

        public long ExecutionId { get; }
        public MatchEvent MatchEvent { get; }
    }
}
