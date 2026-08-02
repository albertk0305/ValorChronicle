using System;
using UnityEngine;

namespace ValorChronicle.Battle.Board.Presentation
{
    public static class BoardDragSwapPlanner
    {
        public static bool TryCreateSwap(
            BoardPosition start,
            Vector2 dragDelta,
            float minimumDragDistance,
            out BoardSwap swap)
        {
            ValidateMinimumDragDistance(minimumDragDistance);
            ValidateDragDelta(dragDelta);

            swap = default(BoardSwap);
            float absoluteX = Mathf.Abs(dragDelta.x);
            float absoluteY = Mathf.Abs(dragDelta.y);
            if (Mathf.Max(absoluteX, absoluteY) < minimumDragDistance
                || absoluteX == absoluteY)
            {
                return false;
            }

            int targetX = start.X;
            int targetY = start.Y;
            if (absoluteX > absoluteY)
            {
                targetX += dragDelta.x > 0f ? 1 : -1;
            }
            else
            {
                targetY += dragDelta.y > 0f ? 1 : -1;
            }

            if (!BoardPosition.IsValid(targetX, targetY))
            {
                return false;
            }

            swap = new BoardSwap(
                start,
                new BoardPosition(targetX, targetY));
            return true;
        }

        private static void ValidateMinimumDragDistance(
            float minimumDragDistance)
        {
            if (minimumDragDistance <= 0f
                || float.IsNaN(minimumDragDistance)
                || float.IsInfinity(minimumDragDistance))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumDragDistance),
                    minimumDragDistance,
                    "Minimum drag distance must be a positive finite value.");
            }
        }

        private static void ValidateDragDelta(Vector2 dragDelta)
        {
            if (float.IsNaN(dragDelta.x)
                || float.IsInfinity(dragDelta.x)
                || float.IsNaN(dragDelta.y)
                || float.IsInfinity(dragDelta.y))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dragDelta),
                    dragDelta,
                    "Drag delta components must be finite values.");
            }
        }
    }
}
