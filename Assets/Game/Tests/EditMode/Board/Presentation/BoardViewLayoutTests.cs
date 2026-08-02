using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Board.Presentation;

namespace ValorChronicle.Tests.EditMode.Board.Presentation
{
    public sealed class BoardViewLayoutTests
    {
        [TestCase(0, 0, -450f, -870f)]
        [TestCase(0, 4, -450f, -150f)]
        [TestCase(5, 0, 450f, -870f)]
        [TestCase(5, 4, 450f, -150f)]
        public void GetAnchoredPosition_MapsRepresentativeCoordinates(
            int x,
            int y,
            float expectedX,
            float expectedY)
        {
            Vector2 result = BoardViewLayout.GetAnchoredPosition(
                new BoardPosition(x, y));

            Assert.That(result, Is.EqualTo(
                new Vector2(expectedX, expectedY)));
        }

        [Test]
        public void GetAnchoredPosition_AllCellsHaveUniquePositions()
        {
            var positions = new HashSet<Vector2>();

            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    positions.Add(BoardViewLayout.GetAnchoredPosition(
                        new BoardPosition(x, y)));
                }
            }

            Assert.That(positions.Count, Is.EqualTo(
                BoardConstants.CellCount));
        }

        [Test]
        public void GetAnchoredPosition_IncreasingXMovesByCellSpacing()
        {
            Vector2 first = BoardViewLayout.GetAnchoredPosition(
                new BoardPosition(2, 3));
            Vector2 second = BoardViewLayout.GetAnchoredPosition(
                new BoardPosition(3, 3));

            Assert.That(second - first, Is.EqualTo(
                new Vector2(BoardViewLayout.CellSpacing, 0f)));
        }

        [Test]
        public void GetAnchoredPosition_IncreasingYMovesByCellSpacing()
        {
            Vector2 first = BoardViewLayout.GetAnchoredPosition(
                new BoardPosition(2, 2));
            Vector2 second = BoardViewLayout.GetAnchoredPosition(
                new BoardPosition(2, 3));

            Assert.That(second - first, Is.EqualTo(
                new Vector2(0f, BoardViewLayout.CellSpacing)));
        }

        [TestCase(0, 5, -450f, 30f)]
        [TestCase(5, 5, 450f, 30f)]
        [TestCase(0, 9, -450f, 750f)]
        [TestCase(5, 9, 450f, 750f)]
        public void GetAnchoredPosition_MapsVirtualRows(
            int x,
            int visualY,
            float expectedX,
            float expectedY)
        {
            Vector2 result = BoardViewLayout.GetAnchoredPosition(x, visualY);

            Assert.That(result, Is.EqualTo(
                new Vector2(expectedX, expectedY)));
        }

        [Test]
        public void GetAnchoredPosition_SourceIsNineHundredPixelsAboveTarget()
        {
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    Vector2 target = BoardViewLayout.GetAnchoredPosition(
                        new BoardPosition(x, y));
                    Vector2 source = BoardViewLayout.GetAnchoredPosition(
                        x,
                        BoardConstants.Height + y);

                    Assert.That(source - target, Is.EqualTo(
                        new Vector2(0f, 900f)));
                }
            }
        }

        [TestCase(-1, 0)]
        [TestCase(6, 0)]
        [TestCase(0, -1)]
        public void GetAnchoredPosition_InvalidVisualCoordinateThrows(
            int x,
            int visualY)
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => BoardViewLayout.GetAnchoredPosition(x, visualY));
        }
    }
}
