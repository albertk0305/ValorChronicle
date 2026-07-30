using System;

namespace ValorChronicle.Battle.Board
{
    public readonly struct BoardSwap : IEquatable<BoardSwap>
    {
        public BoardSwap(BoardPosition first, BoardPosition second)
        {
            First = first;
            Second = second;
        }

        public BoardPosition First { get; }
        public BoardPosition Second { get; }

        public bool Equals(BoardSwap other)
        {
            return First.Equals(other.First) && Second.Equals(other.Second);
        }

        public override bool Equals(object obj)
        {
            return obj is BoardSwap other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (First.GetHashCode() * 397) ^ Second.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"{First} -> {Second}";
        }

        public static bool operator ==(BoardSwap left, BoardSwap right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BoardSwap left, BoardSwap right)
        {
            return !left.Equals(right);
        }
    }
}
