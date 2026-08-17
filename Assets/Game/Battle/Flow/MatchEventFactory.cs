using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Data.Definitions;

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
                IReadOnlyList<MatchEventProjection> projections =
                    ProjectStep(
                        cascade.Steps[stepIndex],
                        stepIndex,
                        sequenceIndex);
                for (int index = 0; index < projections.Count; index++)
                {
                    MatchEventProjection projection = projections[index];
                    events.Add(new MatchEvent(
                        projection.SequenceIndex,
                        projection.CascadeStepIndex,
                        projection.MatchIndex,
                        projection.Element,
                        projection.Tier,
                        projection.Origin,
                        projection.Positions,
                        projection.RemovedBlockCount));
                }

                sequenceIndex += projections.Count;
            }

            return Array.AsReadOnly(events.ToArray());
        }

        internal static IReadOnlyList<MatchEventProjection> ProjectStep(
            BoardCascadeStep step,
            int cascadeStepIndex,
            int startingSequenceIndex)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            if (cascadeStepIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cascadeStepIndex));
            }

            if (startingSequenceIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startingSequenceIndex));
            }

            BoardCascadeRemovalValidator.Validate(
                step.Matches,
                step.Collapse.Removals);
            var projections = new MatchEventProjection[step.Matches.Count];
            for (int matchIndex = 0;
                matchIndex < step.Matches.Count;
                matchIndex++)
            {
                BoardMatch match = step.Matches[matchIndex];
                projections[matchIndex] = new MatchEventProjection(
                    checked(startingSequenceIndex + matchIndex),
                    cascadeStepIndex,
                    matchIndex,
                    match.Element,
                    match.Tier,
                    match.Origin,
                    match.Positions,
                    match.BlockCount);
            }

            return Array.AsReadOnly(projections);
        }
    }

    internal sealed class MatchEventProjection
    {
        public MatchEventProjection(
            int sequenceIndex,
            int cascadeStepIndex,
            int matchIndex,
            ElementType element,
            BoardMatchTier tier,
            BoardPosition origin,
            IReadOnlyList<BoardPosition> positions,
            int removedBlockCount)
        {
            SequenceIndex = sequenceIndex;
            CascadeStepIndex = cascadeStepIndex;
            MatchIndex = matchIndex;
            Element = element;
            Tier = tier;
            Origin = origin;
            Positions = positions;
            RemovedBlockCount = removedBlockCount;
        }

        public int SequenceIndex { get; }
        public int CascadeStepIndex { get; }
        public int MatchIndex { get; }
        public ElementType Element { get; }
        public BoardMatchTier Tier { get; }
        public BoardPosition Origin { get; }
        public IReadOnlyList<BoardPosition> Positions { get; }
        public int RemovedBlockCount { get; }
    }
}
