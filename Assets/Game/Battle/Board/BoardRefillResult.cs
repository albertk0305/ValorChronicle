using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardRefillResult
    {
        private readonly ReadOnlyCollection<BoardBlockSpawn> spawns;

        internal BoardRefillResult(
            BoardState board,
            IList<BoardBlockSpawn> spawns)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (spawns == null)
            {
                throw new ArgumentNullException(nameof(spawns));
            }

            Board = board;

            var copiedSpawns = new BoardBlockSpawn[spawns.Count];
            spawns.CopyTo(copiedSpawns, 0);
            this.spawns = Array.AsReadOnly(copiedSpawns);
        }

        public BoardState Board { get; }
        public IReadOnlyList<BoardBlockSpawn> Spawns => spawns;
    }
}
