using System;
using System.Collections.Generic;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardElementConversionResolver
    {
        public const int DefaultMaxRandomSelectionAttempts = 100;

        private readonly IRandomSource randomSource;
        private readonly BoardMoveAnalyzer moveAnalyzer;
        private readonly int maxRandomSelectionAttempts;

        public BoardElementConversionResolver(
            IRandomSource randomSource,
            BoardMoveAnalyzer moveAnalyzer,
            int maxRandomSelectionAttempts =
                DefaultMaxRandomSelectionAttempts)
        {
            this.randomSource = randomSource
                ?? throw new ArgumentNullException(nameof(randomSource));
            this.moveAnalyzer = moveAnalyzer
                ?? throw new ArgumentNullException(nameof(moveAnalyzer));
            if (maxRandomSelectionAttempts <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxRandomSelectionAttempts));
            }

            this.maxRandomSelectionAttempts = maxRandomSelectionAttempts;
        }

        public BoardElementConversionResult Resolve(
            BoardState board,
            ElementType targetElement,
            int requestedCount)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (!Enum.IsDefined(typeof(ElementType), targetElement))
            {
                throw new ArgumentOutOfRangeException(nameof(targetElement));
            }

            if (requestedCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedCount));
            }

            List<BoardPosition> candidates = CollectCandidates(
                board,
                targetElement);
            if (candidates.Count == 0)
            {
                return Failure(
                    board,
                    requestedCount,
                    randomAttemptCount: 0,
                    deterministicCombinationCount: 0,
                    usedDeterministicSearch: false,
                    BoardElementConversionFailure.NoCandidates);
            }

            int conversionCount = Math.Min(requestedCount, candidates.Count);
            for (int attempt = 1;
                attempt <= maxRandomSelectionAttempts;
                attempt++)
            {
                IReadOnlyList<BoardPosition> selected = SelectPositions(
                    candidates,
                    conversionCount);
                BoardElementConversionResult candidate = BuildResult(
                    board,
                    targetElement,
                    requestedCount,
                    selected,
                    attempt,
                    deterministicCombinationCount: 0,
                    usedDeterministicSearch: false);
                if (moveAnalyzer.HasAnyValidSwap(candidate.Board))
                {
                    return candidate;
                }
            }

            var combination = CreateInitialCombination(conversionCount);
            int deterministicCombinationCount = 0;
            do
            {
                deterministicCombinationCount++;
                IReadOnlyList<BoardPosition> selected = SelectCombination(
                    candidates,
                    combination);
                BoardElementConversionResult candidate = BuildResult(
                    board,
                    targetElement,
                    requestedCount,
                    selected,
                    maxRandomSelectionAttempts,
                    deterministicCombinationCount,
                    usedDeterministicSearch: true);
                if (moveAnalyzer.HasAnyValidSwap(candidate.Board))
                {
                    return candidate;
                }
            }
            while (MoveToNextCombination(combination, candidates.Count));

            return Failure(
                board,
                requestedCount,
                maxRandomSelectionAttempts,
                deterministicCombinationCount,
                usedDeterministicSearch: true,
                BoardElementConversionFailure.NoPlayableCombination);
        }

        private static List<BoardPosition> CollectCandidates(
            BoardState board,
            ElementType targetElement)
        {
            var candidates = new List<BoardPosition>();
            var runtimeIds = new HashSet<long>();
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    BoardBlock block = board.Get(position);
                    if (block == null)
                    {
                        throw new InvalidOperationException(
                            $"Element conversion requires a full board: {position}.");
                    }

                    if (!runtimeIds.Add(block.RuntimeId))
                    {
                        throw new InvalidOperationException(
                            $"Duplicate board runtime ID {block.RuntimeId}.");
                    }

                    if (block.BlockType == BoardBlockType.Normal
                        && block.Element.HasValue
                        && block.Element.Value != targetElement)
                    {
                        candidates.Add(position);
                    }
                }
            }

            return candidates;
        }

        private IReadOnlyList<BoardPosition> SelectPositions(
            IReadOnlyList<BoardPosition> candidates,
            int count)
        {
            var available = new List<BoardPosition>(candidates);
            var selected = new List<BoardPosition>(count);
            for (int index = 0; index < count; index++)
            {
                int selectedIndex = randomSource.Next(index, available.Count);
                BoardPosition temporary = available[index];
                available[index] = available[selectedIndex];
                available[selectedIndex] = temporary;
                selected.Add(available[index]);
            }

            return selected;
        }

        private static int[] CreateInitialCombination(int count)
        {
            var combination = new int[count];
            for (int index = 0; index < count; index++)
            {
                combination[index] = index;
            }

            return combination;
        }

        private static IReadOnlyList<BoardPosition> SelectCombination(
            IReadOnlyList<BoardPosition> candidates,
            IReadOnlyList<int> combination)
        {
            var selected = new BoardPosition[combination.Count];
            for (int index = 0; index < combination.Count; index++)
            {
                selected[index] = candidates[combination[index]];
            }

            return selected;
        }

        private static bool MoveToNextCombination(
            int[] combination,
            int candidateCount)
        {
            for (int index = combination.Length - 1; index >= 0; index--)
            {
                int maximumAtIndex = candidateCount
                    - combination.Length
                    + index;
                if (combination[index] >= maximumAtIndex)
                {
                    continue;
                }

                combination[index]++;
                for (int resetIndex = index + 1;
                    resetIndex < combination.Length;
                    resetIndex++)
                {
                    combination[resetIndex] = combination[resetIndex - 1] + 1;
                }

                return true;
            }

            return false;
        }

        private static BoardElementConversionResult BuildResult(
            BoardState board,
            ElementType targetElement,
            int requestedCount,
            IReadOnlyList<BoardPosition> selected,
            int randomAttemptCount,
            int deterministicCombinationCount,
            bool usedDeterministicSearch)
        {
            BoardState candidate = board.Clone();
            var entries = new List<BoardElementConversionEntry>(selected.Count);
            for (int index = 0; index < selected.Count; index++)
            {
                BoardPosition position = selected[index];
                BoardBlock previous = board.Get(position);
                var converted = new BoardBlock(
                    previous.RuntimeId,
                    previous.BlockType,
                    targetElement);
                candidate.Set(position, converted);
                entries.Add(new BoardElementConversionEntry(
                    position,
                    previous.RuntimeId,
                    previous.BlockType,
                    previous.Element.Value,
                    targetElement));
            }

            return new BoardElementConversionResult(
                candidate,
                requestedCount,
                entries,
                randomAttemptCount,
                deterministicCombinationCount,
                usedDeterministicSearch,
                BoardElementConversionFailure.None);
        }

        private static BoardElementConversionResult Failure(
            BoardState board,
            int requestedCount,
            int randomAttemptCount,
            int deterministicCombinationCount,
            bool usedDeterministicSearch,
            BoardElementConversionFailure failure)
        {
            return new BoardElementConversionResult(
                board.Clone(),
                requestedCount,
                Array.Empty<BoardElementConversionEntry>(),
                randomAttemptCount,
                deterministicCombinationCount,
                usedDeterministicSearch,
                failure);
        }
    }
}
