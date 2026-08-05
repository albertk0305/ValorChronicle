using System;
using System.Collections.Generic;
using System.Reflection;
using ValorChronicle.Battle.Board;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Flow
{
    internal static class BattleFlowTestSupport
    {
        public static MatchFixture Match(
            ElementType element,
            params BoardPosition[] positions)
        {
            return new MatchFixture(element, positions);
        }

        public static BoardCascadeResult CreateCascade(
            params MatchFixture[][] steps)
        {
            var cascadeSteps = new List<BoardCascadeStep>(steps.Length);
            long nextRuntimeId = 1;
            for (int stepIndex = 0;
                stepIndex < steps.Length;
                stepIndex++)
            {
                cascadeSteps.Add(CreateStep(
                    steps[stepIndex],
                    null,
                    ref nextRuntimeId));
            }

            return CreateInternal<BoardCascadeResult>(
                new BoardState(),
                cascadeSteps);
        }

        public static BoardCascadeResult CreateCascadeWithRemovalCount(
            int removalCount,
            params MatchFixture[] matches)
        {
            long nextRuntimeId = 1;
            BoardCascadeStep step = CreateStep(
                matches,
                removalCount,
                ref nextRuntimeId);
            return CreateInternal<BoardCascadeResult>(
                new BoardState(),
                new List<BoardCascadeStep> { step });
        }

        public static void CompleteTurnWithoutMatchEvents(
            ValorChronicle.Battle.Flow.BattleFlowCoordinator coordinator)
        {
            if (!coordinator.CompleteActiveInput())
            {
                throw new InvalidOperationException(
                    "Active input did not complete.");
            }

            if (!coordinator.NotifyBoardActionStarted())
            {
                throw new InvalidOperationException(
                    "Board action did not start.");
            }

            if (!coordinator.NotifyBoardActionResolved(null, true))
            {
                throw new InvalidOperationException(
                    "Board action did not resolve.");
            }

            coordinator.ExecuteRemainingMatchEvents();
            if (!coordinator.CompleteBossAction())
            {
                throw new InvalidOperationException(
                    "Boss action did not complete.");
            }
        }

        private static BoardCascadeStep CreateStep(
            IReadOnlyList<MatchFixture> fixtures,
            int? removalCountOverride,
            ref long nextRuntimeId)
        {
            var matches = new List<BoardMatch>(fixtures.Count);
            var removals = new List<BoardBlockRemoval>();
            for (int matchIndex = 0;
                matchIndex < fixtures.Count;
                matchIndex++)
            {
                MatchFixture fixture = fixtures[matchIndex];
                var positions = new List<BoardPosition>(fixture.Positions);
                matches.Add(CreateInternal<BoardMatch>(
                    fixture.Element,
                    GetTier(positions.Count),
                    positions[0],
                    positions));

                for (int positionIndex = 0;
                    positionIndex < positions.Count;
                    positionIndex++)
                {
                    var block = new BoardBlock(
                        nextRuntimeId++,
                        BoardBlockType.Normal,
                        fixture.Element);
                    removals.Add(CreateInternal<BoardBlockRemoval>(
                        block,
                        positions[positionIndex]));
                }
            }

            if (removalCountOverride.HasValue)
            {
                int targetCount = removalCountOverride.Value;
                if (targetCount < 0 || targetCount > removals.Count)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(removalCountOverride));
                }

                if (targetCount < removals.Count)
                {
                    removals.RemoveRange(
                        targetCount,
                        removals.Count - targetCount);
                }
            }

            BoardCollapseResult collapse =
                CreateInternal<BoardCollapseResult>(
                    new BoardState(),
                    removals,
                    new List<BoardBlockMove>());
            BoardRefillResult refill = CreateInternal<BoardRefillResult>(
                new BoardState(),
                new List<BoardBlockSpawn>());
            return CreateInternal<BoardCascadeStep>(
                matches,
                collapse,
                refill);
        }

        private static BoardMatchTier GetTier(int blockCount)
        {
            if (blockCount == 3)
            {
                return BoardMatchTier.Three;
            }

            if (blockCount == 4)
            {
                return BoardMatchTier.Four;
            }

            return BoardMatchTier.FiveOrMore;
        }

        private static T CreateInternal<T>(params object[] arguments)
        {
            return (T)Activator.CreateInstance(
                typeof(T),
                BindingFlags.Instance
                    | BindingFlags.Public
                    | BindingFlags.NonPublic,
                null,
                arguments,
                null);
        }

        internal sealed class MatchFixture
        {
            public MatchFixture(
                ElementType element,
                IReadOnlyList<BoardPosition> positions)
            {
                if (positions == null || positions.Count == 0)
                {
                    throw new ArgumentException(
                        "Match positions are required.",
                        nameof(positions));
                }

                Element = element;
                Positions = positions;
            }

            public ElementType Element { get; }
            public IReadOnlyList<BoardPosition> Positions { get; }
        }
    }
}
