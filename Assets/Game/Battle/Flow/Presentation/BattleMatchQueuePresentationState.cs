using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Flow.Presentation
{
    public sealed class BattleMatchQueuePresentationEntry
    {
        internal BattleMatchQueuePresentationEntry(
            int sequenceIndex,
            int cascadeStepIndex,
            ElementType element,
            int removedBlockCount)
        {
            SequenceIndex = sequenceIndex;
            CascadeStepIndex = cascadeStepIndex;
            Element = element;
            RemovedBlockCount = removedBlockCount;
        }

        public int SequenceIndex { get; }
        public int CascadeStepIndex { get; }
        public ElementType Element { get; }
        public int RemovedBlockCount { get; }
    }

    public sealed class BattleMatchQueuePresentationState
    {
        private readonly List<BattleMatchQueuePresentationEntry> entries =
            new List<BattleMatchQueuePresentationEntry>();
        private long activeActionId;
        private int nextCascadeStepIndex;
        private int nextSequenceIndex;

        public long ActiveActionId => activeActionId;
        public int Count => entries.Count;

        public IReadOnlyList<BattleMatchQueuePresentationEntry> GetSnapshot()
        {
            return new ReadOnlyCollection<BattleMatchQueuePresentationEntry>(
                entries.ToArray());
        }

        internal bool BeginAction(long actionId)
        {
            if (actionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(actionId));
            }

            bool changed = entries.Count > 0;
            entries.Clear();
            activeActionId = actionId;
            nextCascadeStepIndex = 0;
            nextSequenceIndex = 0;
            return changed;
        }

        internal bool TryAppend(BoardCascadeStepPresentation presentation)
        {
            if (presentation == null)
            {
                throw new ArgumentNullException(nameof(presentation));
            }

            if (activeActionId == 0
                || presentation.ActionId != activeActionId
                || presentation.CascadeStepIndex != nextCascadeStepIndex)
            {
                return false;
            }

            IReadOnlyList<MatchEventProjection> projections =
                MatchEventFactory.ProjectStep(
                    presentation.Step,
                    presentation.CascadeStepIndex,
                    nextSequenceIndex);
            for (int index = 0; index < projections.Count; index++)
            {
                MatchEventProjection projection = projections[index];
                entries.Add(new BattleMatchQueuePresentationEntry(
                    projection.SequenceIndex,
                    projection.CascadeStepIndex,
                    projection.Element,
                    projection.RemovedBlockCount));
            }

            nextCascadeStepIndex++;
            nextSequenceIndex = checked(
                nextSequenceIndex + projections.Count);
            return projections.Count > 0;
        }

        internal bool TryPop(MatchEvent matchEvent)
        {
            if (matchEvent == null)
            {
                throw new ArgumentNullException(nameof(matchEvent));
            }

            if (entries.Count == 0
                || !Matches(entries[0], matchEvent))
            {
                return false;
            }

            entries.RemoveAt(0);
            return true;
        }

        internal bool Reconcile(IReadOnlyList<MatchEvent> authoritative)
        {
            if (authoritative == null)
            {
                throw new ArgumentNullException(nameof(authoritative));
            }

            bool matches = entries.Count == authoritative.Count;
            for (int index = 0; matches && index < entries.Count; index++)
            {
                matches = authoritative[index] != null
                    && Matches(entries[index], authoritative[index]);
            }

            activeActionId = 0;
            nextCascadeStepIndex = 0;
            nextSequenceIndex = 0;
            if (matches)
            {
                return false;
            }

            entries.Clear();
            for (int index = 0; index < authoritative.Count; index++)
            {
                MatchEvent matchEvent = authoritative[index]
                    ?? throw new ArgumentException(
                        "Authoritative events cannot contain null.",
                        nameof(authoritative));
                entries.Add(FromMatchEvent(matchEvent));
            }

            return true;
        }

        internal bool Clear()
        {
            bool changed = entries.Count > 0 || activeActionId != 0;
            entries.Clear();
            activeActionId = 0;
            nextCascadeStepIndex = 0;
            nextSequenceIndex = 0;
            return changed;
        }

        private static BattleMatchQueuePresentationEntry FromMatchEvent(
            MatchEvent matchEvent)
        {
            return new BattleMatchQueuePresentationEntry(
                matchEvent.SequenceIndex,
                matchEvent.CascadeStepIndex,
                matchEvent.Element,
                matchEvent.RemovedBlockCount);
        }

        private static bool Matches(
            BattleMatchQueuePresentationEntry entry,
            MatchEvent matchEvent)
        {
            return entry.SequenceIndex == matchEvent.SequenceIndex
                && entry.CascadeStepIndex == matchEvent.CascadeStepIndex
                && entry.Element == matchEvent.Element
                && entry.RemovedBlockCount == matchEvent.RemovedBlockCount;
        }
    }
}
