using UnityEngine;

namespace ValorChronicle.Battle.Board.Presentation
{
    public static class BoardViewLayout
    {
        public const float BottomLeftCenterX = -450f;
        public const float BottomLeftCenterY = -870f;
        public const float CellSpacing = 180f;

        public static Vector2 GetAnchoredPosition(BoardPosition position)
        {
            return GetAnchoredPosition(position.X, position.Y);
        }

        public static Vector2 GetAnchoredPosition(int x, int visualY)
        {
            if (x < 0 || x >= BoardConstants.Width)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(x),
                    x,
                    $"X must be between 0 and {BoardConstants.Width - 1}.");
            }

            if (visualY < 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(visualY),
                    visualY,
                    "Visual Y cannot be negative.");
            }

            return new Vector2(
                BottomLeftCenterX + (x * CellSpacing),
                BottomLeftCenterY + (visualY * CellSpacing));
        }
    }
}
