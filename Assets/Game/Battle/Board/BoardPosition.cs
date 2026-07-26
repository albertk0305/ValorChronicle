using System;

namespace ValorChronicle.Battle.Board
{
    public readonly struct BoardPosition : IEquatable<BoardPosition>
    {
        public BoardPosition(int x, int y)
        {
            if (x < 0 || x >= BoardConstants.Width)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(x),
                    x,
                    $"X must be between 0 and {BoardConstants.Width - 1}.");
            }

            if (y < 0 || y >= BoardConstants.Height)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(y),
                    y,
                    $"Y must be between 0 and {BoardConstants.Height - 1}.");
            }

            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public static bool IsValid(int x, int y)
        {
            return x >= 0
                && x < BoardConstants.Width
                && y >= 0
                && y < BoardConstants.Height;
        }

        public int ToIndex()
        {
            return X + (Y * BoardConstants.Width);
        }

        public static BoardPosition FromIndex(int index)
        {
            if (index < 0 || index >= BoardConstants.CellCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    index,
                    $"Index must be between 0 and {BoardConstants.CellCount - 1}.");
            }

            int x = index % BoardConstants.Width;
            int y = index / BoardConstants.Width;
            return new BoardPosition(x, y);
        }

        public bool Equals(BoardPosition other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is BoardPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }

        public static bool operator ==(BoardPosition left, BoardPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BoardPosition left, BoardPosition right)
        {
            return !left.Equals(right);
        }
    }
}
