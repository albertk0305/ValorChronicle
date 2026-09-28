using NUnit.Framework;
using ValorChronicle.Party.Presentation;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class VirtualizedCharacterGridTests
    {
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(5, 1)]
        [TestCase(6, 2)]
        [TestCase(31, 7)]
        [TestCase(32, 7)]
        [TestCase(100, 20)]
        public void RowCount_UsesFiveColumns(int itemCount, int expected)
        {
            Assert.That(
                VirtualizedGridLayout.GetRowCount(itemCount, 5),
                Is.EqualTo(expected));
        }

        [Test]
        public void BoundRange_CoversTopMiddleAndBottom()
        {
            VirtualizedGridRange top = BoundRange(100, 0f);
            VirtualizedGridRange middle = BoundRange(100, 1500f);
            VirtualizedGridRange bottom = BoundRange(100, 3047f);

            Assert.That(top.FirstRow, Is.Zero);
            Assert.That(top.RowCount, Is.EqualTo(7));
            Assert.That(middle.FirstRow, Is.GreaterThan(0));
            Assert.That(middle.LastRowExclusive, Is.LessThan(20));
            Assert.That(bottom.LastRowExclusive, Is.EqualTo(20));
        }

        [Test]
        public void PoolRows_AreConstantForViewportSize()
        {
            int rows = VirtualizedGridLayout.GetRequiredPoolRowCount(
                viewportHeight: 1175f,
                cellHeight: 100f,
                rowStride: 210f,
                bufferRows: 1);

            Assert.That(rows, Is.EqualTo(9));
            Assert.That(rows * 5, Is.EqualTo(45));
        }

        [TestCase(0, 1175f)]
        [TestCase(1, 1175f)]
        [TestCase(5, 1175f)]
        [TestCase(6, 1175f)]
        [TestCase(32, 1382f)]
        public void ContentHeight_IsSafeForSmallAndPartialRosters(
            int itemCount,
            float expectedHeight)
        {
            float height = VirtualizedGridLayout.GetContentHeight(
                itemCount,
                columnCount: 5,
                viewportHeight: 1175f,
                cellHeight: 100f,
                rowSpacing: 110f,
                topPadding: 11f,
                bottomPadding: 11f);

            Assert.That(height, Is.EqualTo(expectedHeight));
        }

        private static VirtualizedGridRange BoundRange(
            int itemCount,
            float offset)
        {
            return VirtualizedGridLayout.GetBoundRowRange(
                itemCount,
                columnCount: 5,
                scrollOffset: offset,
                viewportHeight: 1175f,
                cellHeight: 100f,
                rowSpacing: 110f,
                topPadding: 11f,
                bufferRows: 1);
        }
    }
}
