using System;

namespace ValorChronicle.Party.Presentation
{
    public readonly struct VirtualizedGridRange
    {
        public VirtualizedGridRange(int firstRow, int rowCount)
        {
            FirstRow = firstRow;
            RowCount = rowCount;
        }

        public int FirstRow { get; }
        public int RowCount { get; }
        public int LastRowExclusive => FirstRow + RowCount;
    }

    public static class VirtualizedGridLayout
    {
        public static int GetRowCount(int itemCount, int columnCount)
        {
            if (itemCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(itemCount));
            }

            ValidatePositive(columnCount, nameof(columnCount));
            return itemCount == 0
                ? 0
                : ((itemCount - 1) / columnCount) + 1;
        }

        public static int GetRequiredPoolRowCount(
            float viewportHeight,
            float cellHeight,
            float rowStride,
            int bufferRows)
        {
            ValidatePositive(viewportHeight, nameof(viewportHeight));
            ValidatePositive(cellHeight, nameof(cellHeight));
            ValidatePositive(rowStride, nameof(rowStride));
            if (bufferRows < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferRows));
            }

            int maximumPartiallyVisibleRows = (int)Math.Ceiling(
                (viewportHeight + cellHeight) / rowStride);
            return maximumPartiallyVisibleRows + (bufferRows * 2);
        }

        public static float GetContentHeight(
            int itemCount,
            int columnCount,
            float viewportHeight,
            float cellHeight,
            float rowSpacing,
            float topPadding,
            float bottomPadding)
        {
            ValidatePositive(viewportHeight, nameof(viewportHeight));
            ValidatePositive(cellHeight, nameof(cellHeight));
            ValidateNonNegative(rowSpacing, nameof(rowSpacing));
            ValidateNonNegative(topPadding, nameof(topPadding));
            ValidateNonNegative(bottomPadding, nameof(bottomPadding));

            int rowCount = GetRowCount(itemCount, columnCount);
            float rowsHeight = rowCount == 0
                ? 0f
                : (rowCount * cellHeight)
                    + ((rowCount - 1) * rowSpacing);
            return Math.Max(
                viewportHeight,
                topPadding + rowsHeight + bottomPadding);
        }

        public static VirtualizedGridRange GetBoundRowRange(
            int itemCount,
            int columnCount,
            float scrollOffset,
            float viewportHeight,
            float cellHeight,
            float rowSpacing,
            float topPadding,
            int bufferRows)
        {
            int totalRows = GetRowCount(itemCount, columnCount);
            if (totalRows == 0)
            {
                return new VirtualizedGridRange(0, 0);
            }

            ValidatePositive(viewportHeight, nameof(viewportHeight));
            ValidatePositive(cellHeight, nameof(cellHeight));
            ValidateNonNegative(rowSpacing, nameof(rowSpacing));
            ValidateNonNegative(topPadding, nameof(topPadding));
            if (bufferRows < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferRows));
            }

            float rowStride = cellHeight + rowSpacing;
            float clampedOffset = Math.Max(0f, scrollOffset);
            int firstVisibleRow = (int)Math.Floor(
                Math.Max(0f, clampedOffset - topPadding) / rowStride);
            int lastVisibleRow = (int)Math.Floor(
                Math.Max(
                    0f,
                    clampedOffset + viewportHeight - topPadding)
                / rowStride);
            firstVisibleRow = Math.Min(firstVisibleRow, totalRows - 1);
            lastVisibleRow = Math.Min(lastVisibleRow, totalRows - 1);

            int firstBoundRow = Math.Max(
                0,
                firstVisibleRow - bufferRows);
            int lastBoundRow = Math.Min(
                totalRows - 1,
                lastVisibleRow + bufferRows);
            return new VirtualizedGridRange(
                firstBoundRow,
                lastBoundRow - firstBoundRow + 1);
        }

        private static void ValidatePositive(float value, string name)
        {
            if (value <= 0f)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }

        private static void ValidatePositive(int value, string name)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }

        private static void ValidateNonNegative(float value, string name)
        {
            if (value < 0f)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }
    }
}
