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
    }
}
