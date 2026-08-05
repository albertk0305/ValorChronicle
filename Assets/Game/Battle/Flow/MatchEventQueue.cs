using System;
using System.Collections.Generic;

namespace ValorChronicle.Battle.Flow
{
    public sealed class MatchEventQueue
    {
        private readonly Queue<MatchEvent> events =
            new Queue<MatchEvent>();

        public int Count => events.Count;

        public void EnqueueRange(IReadOnlyList<MatchEvent> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            for (int index = 0; index < source.Count; index++)
            {
                if (source[index] == null)
                {
                    throw new ArgumentException(
                        "Match events cannot contain null.",
                        nameof(source));
                }
            }

            for (int index = 0; index < source.Count; index++)
            {
                events.Enqueue(source[index]);
            }
        }

        public bool TryDequeue(out MatchEvent matchEvent)
        {
            if (events.Count == 0)
            {
                matchEvent = null;
                return false;
            }

            matchEvent = events.Dequeue();
            return true;
        }

        public void Clear()
        {
            events.Clear();
        }
    }
}
