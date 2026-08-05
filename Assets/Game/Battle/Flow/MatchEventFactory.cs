using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;

namespace ValorChronicle.Battle.Flow
{
    public static class MatchEventFactory
    {
        public static IReadOnlyList<MatchEvent> Create(
            BoardCascadeResult cascade)
        {
            if (cascade == null)
            {
                throw new ArgumentNullException(nameof(cascade));
            }

            var events = new List<MatchEvent>(cascade.ComboCount);
            int sequenceIndex = 0;
            for (int stepIndex = 0;
                stepIndex < cascade.Steps.Count;
                stepIndex++)
            {
                BoardCascadeStep step = cascade.Steps[stepIndex];
                int matchedBlockCount = 0;
                for (int matchIndex = 0;
                    matchIndex < step.Matches.Count;
                    matchIndex++)
                {
                    BoardMatch match = step.Matches[matchIndex];
                    matchedBlockCount += match.BlockCount;
                    events.Add(new MatchEvent(
                        sequenceIndex++,
                        stepIndex,
                        matchIndex,
                        match.Element,
                        match.Tier,
                        match.Origin,
                        match.Positions,
                        match.BlockCount));
                }

                if (matchedBlockCount != step.RemovedBlockCount)
                {
                    throw new InvalidOperationException(
                        $"CascadeStep[{stepIndex}] match block count " +
                        $"{matchedBlockCount} does not match removed block " +
                        $"count {step.RemovedBlockCount}.");
                }
            }

            return Array.AsReadOnly(events.ToArray());
        }
    }
}
