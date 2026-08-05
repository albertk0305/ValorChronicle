using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ValorChronicle.Battle.Board;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Flow
{
    public sealed class MatchEvent
    {
        private readonly ReadOnlyCollection<BoardPosition> positions;

        internal MatchEvent(
            int sequenceIndex,
            int cascadeStepIndex,
            int matchIndex,
            ElementType element,
            BoardMatchTier tier,
            BoardPosition origin,
            IReadOnlyList<BoardPosition> positions,
            int removedBlockCount)
        {
            if (sequenceIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sequenceIndex));
            }

            if (cascadeStepIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cascadeStepIndex));
            }

            if (matchIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(matchIndex));
            }

            if (positions == null)
            {
                throw new ArgumentNullException(nameof(positions));
            }

            if (removedBlockCount <= 0
                || removedBlockCount != positions.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(removedBlockCount),
                    removedBlockCount,
                    "Removed block count must match the non-empty " +
                    "position collection.");
            }

            var copiedPositions = new BoardPosition[positions.Count];
            for (int index = 0; index < positions.Count; index++)
            {
                copiedPositions[index] = positions[index];
            }

            SequenceIndex = sequenceIndex;
            CascadeStepIndex = cascadeStepIndex;
            MatchIndex = matchIndex;
            Element = element;
            Tier = tier;
            Origin = origin;
            this.positions = Array.AsReadOnly(copiedPositions);
            RemovedBlockCount = removedBlockCount;
        }

        public int SequenceIndex { get; }
        public int CascadeStepIndex { get; }
        public int MatchIndex { get; }
        public ElementType Element { get; }
        public BoardMatchTier Tier { get; }
        public BoardPosition Origin { get; }
        public IReadOnlyList<BoardPosition> Positions => positions;
        public int RemovedBlockCount { get; }
    }
}
