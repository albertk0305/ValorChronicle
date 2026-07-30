using System;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardBlockIdGenerator
    {
        private long nextId;

        public BoardBlockIdGenerator(int firstId = 1)
        {
            if (firstId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(firstId),
                    firstId,
                    "First ID must be positive.");
            }

            nextId = firstId;
        }

        public int Next()
        {
            if (nextId > int.MaxValue)
            {
                throw new InvalidOperationException(
                    "No more board block IDs are available in the Int32 range.");
            }

            int id = (int)nextId;
            nextId++;
            return id;
        }
    }
}
