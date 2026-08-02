using System;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Board.Presentation;

namespace ValorChronicle.Tests.EditMode.Board.Presentation
{
    public sealed class BoardDragSwapPlannerTests
    {
        private const float MinimumDistance = 54f;

        [TestCase(54f, 0f, 3, 2)]
        [TestCase(-54f, 0f, 1, 2)]
        [TestCase(0f, 54f, 2, 3)]
        [TestCase(0f, -54f, 2, 1)]
        [TestCase(80f, 60f, 3, 2)]
        [TestCase(-60f, -80f, 2, 1)]
        public void TryCreateSwap_DominantAxisCreatesOneCellSwap(
            float deltaX,
            float deltaY,
            int expectedX,
            int expectedY)
        {
            var start = new BoardPosition(2, 2);

            bool created = BoardDragSwapPlanner.TryCreateSwap(
                start,
                new Vector2(deltaX, deltaY),
                MinimumDistance,
                out BoardSwap swap);

            Assert.That(created, Is.True);
            Assert.That(swap.First, Is.EqualTo(start));
            Assert.That(swap.Second, Is.EqualTo(
                new BoardPosition(expectedX, expectedY)));
            int distance = Math.Abs(swap.Second.X - swap.First.X)
                + Math.Abs(swap.Second.Y - swap.First.Y);
            Assert.That(distance, Is.EqualTo(1));
        }

        [TestCase(53.999f, 0f)]
        [TestCase(30f, 30f)]
        [TestCase(-80f, 80f)]
        [TestCase(0f, 0f)]
        public void TryCreateSwap_InsufficientOrTiedDeltaCancels(
            float deltaX,
            float deltaY)
        {
            bool created = BoardDragSwapPlanner.TryCreateSwap(
                new BoardPosition(2, 2),
                new Vector2(deltaX, deltaY),
                MinimumDistance,
                out BoardSwap swap);

            Assert.That(created, Is.False);
            Assert.That(swap, Is.EqualTo(default(BoardSwap)));
        }

        [TestCase(0, 2, -54f, 0f)]
        [TestCase(5, 2, 54f, 0f)]
        [TestCase(2, 0, 0f, -54f)]
        [TestCase(2, 4, 0f, 54f)]
        public void TryCreateSwap_TargetOutsideBoardCancels(
            int startX,
            int startY,
            float deltaX,
            float deltaY)
        {
            bool created = BoardDragSwapPlanner.TryCreateSwap(
                new BoardPosition(startX, startY),
                new Vector2(deltaX, deltaY),
                MinimumDistance,
                out BoardSwap swap);

            Assert.That(created, Is.False);
            Assert.That(swap, Is.EqualTo(default(BoardSwap)));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void TryCreateSwap_InvalidMinimumDistanceThrows(float value)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                BoardDragSwapPlanner.TryCreateSwap(
                    new BoardPosition(2, 2),
                    Vector2.right,
                    value,
                    out _));

            Assert.That(exception.ParamName,
                Is.EqualTo("minimumDragDistance"));
        }

        [TestCase(float.NaN, 0f)]
        [TestCase(float.PositiveInfinity, 0f)]
        [TestCase(0f, float.NegativeInfinity)]
        public void TryCreateSwap_NonFiniteDeltaThrows(
            float deltaX,
            float deltaY)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                BoardDragSwapPlanner.TryCreateSwap(
                    new BoardPosition(2, 2),
                    new Vector2(deltaX, deltaY),
                    MinimumDistance,
                    out _));

            Assert.That(exception.ParamName, Is.EqualTo("dragDelta"));
        }
    }
}
