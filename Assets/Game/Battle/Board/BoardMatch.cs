using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardMatch
    {
        private readonly ReadOnlyCollection<BoardPosition> positions;

        internal BoardMatch(
            ElementType element,
            BoardMatchTier tier,
            BoardPosition origin,
            IList<BoardPosition> positions)
        {
            if (positions == null)
            {
                throw new ArgumentNullException(nameof(positions));
            }

            Element = element;
            Tier = tier;
            Origin = origin;

            var copiedPositions = new BoardPosition[positions.Count];
            positions.CopyTo(copiedPositions, 0);
            this.positions = Array.AsReadOnly(copiedPositions);
        }

        public ElementType Element { get; }
        public BoardMatchTier Tier { get; }
        public BoardPosition Origin { get; }
        public IReadOnlyList<BoardPosition> Positions => positions;
        public int BlockCount => positions.Count;
    }
}
