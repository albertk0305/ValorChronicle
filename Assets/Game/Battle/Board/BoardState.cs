using System;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardState
    {
        private readonly BoardBlock[] cells;

        public BoardState()
        {
            cells = new BoardBlock[BoardConstants.CellCount];
        }

        private BoardState(BoardBlock[] cells)
        {
            this.cells = cells;
        }

        public BoardBlock Get(BoardPosition position)
        {
            return cells[GetValidatedIndex(position)];
        }

        public void Set(BoardPosition position, BoardBlock block)
        {
            if (block == null)
            {
                throw new ArgumentNullException(nameof(block));
            }

            cells[GetValidatedIndex(position)] = block;
        }

        public void Clear(BoardPosition position)
        {
            cells[GetValidatedIndex(position)] = null;
        }

        public bool IsOccupied(BoardPosition position)
        {
            return cells[GetValidatedIndex(position)] != null;
        }

        public void Swap(BoardPosition first, BoardPosition second)
        {
            int firstIndex = GetValidatedIndex(first);
            int secondIndex = GetValidatedIndex(second);

            if (firstIndex == secondIndex)
            {
                return;
            }

            BoardBlock temporary = cells[firstIndex];
            cells[firstIndex] = cells[secondIndex];
            cells[secondIndex] = temporary;
        }

        public BoardState Clone()
        {
            var clonedCells = new BoardBlock[BoardConstants.CellCount];
            Array.Copy(cells, clonedCells, cells.Length);
            return new BoardState(clonedCells);
        }

        private static int GetValidatedIndex(BoardPosition position)
        {
            if (!BoardPosition.IsValid(position.X, position.Y))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(position),
                    position,
                    "Position must be inside the board.");
            }

            return position.ToIndex();
        }
    }
}
