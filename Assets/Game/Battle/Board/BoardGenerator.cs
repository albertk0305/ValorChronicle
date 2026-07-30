using System;
using System.Collections.Generic;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardGenerator
    {
        public const int DefaultMaxAttempts = 256;

        private static readonly ElementType[] ElementSelectionOrder =
        {
            ElementType.Fire,
            ElementType.Water,
            ElementType.Grass,
            ElementType.Light,
            ElementType.Dark
        };

        private readonly IRandomSource randomSource;
        private readonly Func<BoardState, IReadOnlyList<BoardMatch>> matchFinder;
        private readonly BoardMoveAnalyzer moveAnalyzer;
        private readonly BoardBlockIdGenerator idGenerator;
        private readonly int maxAttempts;
        private readonly ElementType[] availableElements;

        public BoardGenerator(
            IRandomSource randomSource,
            Func<BoardState, IReadOnlyList<BoardMatch>> matchFinder,
            BoardMoveAnalyzer moveAnalyzer,
            BoardBlockIdGenerator idGenerator,
            int maxAttempts = DefaultMaxAttempts)
        {
            this.randomSource = randomSource
                ?? throw new ArgumentNullException(nameof(randomSource));
            this.matchFinder = matchFinder
                ?? throw new ArgumentNullException(nameof(matchFinder));
            this.moveAnalyzer = moveAnalyzer
                ?? throw new ArgumentNullException(nameof(moveAnalyzer));
            this.idGenerator = idGenerator
                ?? throw new ArgumentNullException(nameof(idGenerator));

            if (maxAttempts <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxAttempts),
                    maxAttempts,
                    "Maximum attempts must be positive.");
            }

            this.maxAttempts = maxAttempts;
            availableElements = new ElementType[ElementSelectionOrder.Length];
        }

        public BoardState Generate()
        {
            var elementLayout = new ElementType[BoardConstants.CellCount];

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                FillCandidateLayout(elementLayout);
                BoardState validationBoard = BuildValidationBoard(elementLayout);

                if (matchFinder(validationBoard).Count == 0
                    && moveAnalyzer.HasAnyValidSwap(validationBoard))
                {
                    return BuildFinalBoard(elementLayout);
                }
            }

            throw new InvalidOperationException(
                $"Unable to generate a valid board within {maxAttempts} attempts.");
        }

        private void FillCandidateLayout(ElementType[] elementLayout)
        {
            for (int y = 0; y < BoardConstants.Height; y++)
            {
                for (int x = 0; x < BoardConstants.Width; x++)
                {
                    int availableCount = CollectAvailableElements(
                        elementLayout,
                        x,
                        y);
                    int selectedIndex = randomSource.Next(0, availableCount);
                    elementLayout[ToIndex(x, y)] =
                        availableElements[selectedIndex];
                }
            }
        }

        private int CollectAvailableElements(
            ElementType[] elementLayout,
            int x,
            int y)
        {
            ElementType? horizontalExclusion = null;
            if (x >= 2)
            {
                ElementType left = elementLayout[ToIndex(x - 1, y)];
                if (left == elementLayout[ToIndex(x - 2, y)])
                {
                    horizontalExclusion = left;
                }
            }

            ElementType? verticalExclusion = null;
            if (y >= 2)
            {
                ElementType below = elementLayout[ToIndex(x, y - 1)];
                if (below == elementLayout[ToIndex(x, y - 2)])
                {
                    verticalExclusion = below;
                }
            }

            int availableCount = 0;
            for (int index = 0; index < ElementSelectionOrder.Length; index++)
            {
                ElementType element = ElementSelectionOrder[index];
                if (element == horizontalExclusion
                    || element == verticalExclusion)
                {
                    continue;
                }

                availableElements[availableCount++] = element;
            }

            return availableCount;
        }

        private static BoardState BuildValidationBoard(
            ElementType[] elementLayout)
        {
            var board = new BoardState();

            for (int y = 0; y < BoardConstants.Height; y++)
            {
                for (int x = 0; x < BoardConstants.Width; x++)
                {
                    int index = ToIndex(x, y);
                    board.Set(
                        new BoardPosition(x, y),
                        new BoardBlock(
                            index + 1,
                            BoardBlockType.Normal,
                            elementLayout[index]));
                }
            }

            return board;
        }

        private BoardState BuildFinalBoard(ElementType[] elementLayout)
        {
            var board = new BoardState();

            for (int y = 0; y < BoardConstants.Height; y++)
            {
                for (int x = 0; x < BoardConstants.Width; x++)
                {
                    int index = ToIndex(x, y);
                    board.Set(
                        new BoardPosition(x, y),
                        new BoardBlock(
                            idGenerator.Next(),
                            BoardBlockType.Normal,
                            elementLayout[index]));
                }
            }

            return board;
        }

        private static int ToIndex(int x, int y)
        {
            return x + (y * BoardConstants.Width);
        }
    }
}
