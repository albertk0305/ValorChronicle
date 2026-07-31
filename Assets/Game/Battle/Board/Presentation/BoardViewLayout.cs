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
            return new Vector2(
                BottomLeftCenterX + (position.X * CellSpacing),
                BottomLeftCenterY + (position.Y * CellSpacing));
        }
    }
}
