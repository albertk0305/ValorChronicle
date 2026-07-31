using System;
using System.Collections.Generic;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardShuffler
    {
        public const int DefaultMaxPermutationAttempts = 256;
        public const int DefaultMaxRegenerationAttempts = 256;

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
        private readonly int maxPermutationAttempts;
        private readonly int maxRegenerationAttempts;

        public BoardShuffler(
            IRandomSource randomSource,
            BoardMoveAnalyzer moveAnalyzer,
            int maxPermutationAttempts = DefaultMaxPermutationAttempts,
            int maxRegenerationAttempts = DefaultMaxRegenerationAttempts)
            : this(
                randomSource,
                BoardMatchFinder.FindMatches,
                moveAnalyzer,
                maxPermutationAttempts,
                maxRegenerationAttempts)
        {
        }

        public BoardShuffler(
            IRandomSource randomSource,
            Func<BoardState, IReadOnlyList<BoardMatch>> matchFinder,
            BoardMoveAnalyzer moveAnalyzer,
            int maxPermutationAttempts = DefaultMaxPermutationAttempts,
            int maxRegenerationAttempts = DefaultMaxRegenerationAttempts)
        {
            this.randomSource = randomSource
                ?? throw new ArgumentNullException(nameof(randomSource));
            this.matchFinder = matchFinder
                ?? throw new ArgumentNullException(nameof(matchFinder));
            this.moveAnalyzer = moveAnalyzer
                ?? throw new ArgumentNullException(nameof(moveAnalyzer));

            if (maxPermutationAttempts <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxPermutationAttempts),
                    maxPermutationAttempts,
                    "Maximum permutation attempts must be positive.");
            }

            if (maxRegenerationAttempts <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxRegenerationAttempts),
                    maxRegenerationAttempts,
                    "Maximum regeneration attempts must be positive.");
            }

            this.maxPermutationAttempts = maxPermutationAttempts;
            this.maxRegenerationAttempts = maxRegenerationAttempts;
        }

        public BoardShuffleResult EnsurePlayable(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            ValidateFullBoard(board);
            IReadOnlyList<BoardMatch> existingMatches = FindMatches(board);
            if (existingMatches.Count > 0)
            {
                throw new InvalidOperationException(
                    "Cannot shuffle a board that contains completed matches.");
            }

            if (moveAnalyzer.HasAnyValidSwap(board))
            {
                return new BoardShuffleResult(
                    board.Clone(),
                    BoardShuffleKind.None,
                    Array.Empty<BoardShuffleEntry>(),
                    0);
            }

            CollectNormalBlocks(
                board,
                out List<BoardPosition> positions,
                out List<BoardBlock> originalBlocks,
                out ElementType[] originalElements);

            int attemptCount = 0;

            for (int attempt = 0;
                attempt < maxPermutationAttempts;
                attempt++)
            {
                attemptCount++;
                ElementType[] candidateElements =
                    CopyElements(originalElements);
                Shuffle(candidateElements);
                BoardState candidate = BuildCandidate(
                    board,
                    positions,
                    originalBlocks,
                    candidateElements);

                if (IsPlayable(candidate))
                {
                    return BuildResult(
                        candidate,
                        BoardShuffleKind.Permutation,
                        positions,
                        originalBlocks,
                        originalElements,
                        candidateElements,
                        attemptCount);
                }
            }

            for (int attempt = 0;
                attempt < maxRegenerationAttempts;
                attempt++)
            {
                attemptCount++;
                var candidateElements = new ElementType[positions.Count];
                for (int index = 0; index < candidateElements.Length; index++)
                {
                    int selectedIndex = randomSource.Next(
                        0,
                        ElementSelectionOrder.Length);
                    candidateElements[index] =
                        ElementSelectionOrder[selectedIndex];
                }

                BoardState candidate = BuildCandidate(
                    board,
                    positions,
                    originalBlocks,
                    candidateElements);

                if (IsPlayable(candidate))
                {
                    return BuildResult(
                        candidate,
                        BoardShuffleKind.Regeneration,
                        positions,
                        originalBlocks,
                        originalElements,
                        candidateElements,
                        attemptCount);
                }
            }

            throw new InvalidOperationException(
                "Unable to create a playable board within the shuffle attempt limits.");
        }

        private static void ValidateFullBoard(BoardState board)
        {
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                if (!board.IsOccupied(position))
                {
                    throw new InvalidOperationException(
                        $"Cannot shuffle a board with an empty position {position}.");
                }
            }
        }

        private IReadOnlyList<BoardMatch> FindMatches(BoardState board)
        {
            IReadOnlyList<BoardMatch> matches = matchFinder(board);
            if (matches == null)
            {
                throw new InvalidOperationException(
                    "The match finder returned null.");
            }

            return matches;
        }

        private static void CollectNormalBlocks(
            BoardState board,
            out List<BoardPosition> positions,
            out List<BoardBlock> originalBlocks,
            out ElementType[] originalElements)
        {
            positions = new List<BoardPosition>();
            originalBlocks = new List<BoardBlock>();
            var elements = new List<ElementType>();

            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    BoardBlock block = board.Get(position);
                    if (block.BlockType != BoardBlockType.Normal)
                    {
                        continue;
                    }

                    positions.Add(position);
                    originalBlocks.Add(block);
                    elements.Add(block.Element.Value);
                }
            }

            originalElements = elements.ToArray();
        }

        private void Shuffle(ElementType[] elements)
        {
            for (int index = elements.Length - 1; index >= 1; index--)
            {
                int swapIndex = randomSource.Next(0, index + 1);
                ElementType temporary = elements[index];
                elements[index] = elements[swapIndex];
                elements[swapIndex] = temporary;
            }
        }

        private static BoardState BuildCandidate(
            BoardState board,
            IReadOnlyList<BoardPosition> positions,
            IReadOnlyList<BoardBlock> originalBlocks,
            IReadOnlyList<ElementType> elements)
        {
            BoardState candidate = board.Clone();

            for (int index = 0; index < positions.Count; index++)
            {
                BoardBlock originalBlock = originalBlocks[index];
                candidate.Set(
                    positions[index],
                    new BoardBlock(
                        originalBlock.RuntimeId,
                        BoardBlockType.Normal,
                        elements[index]));
            }

            return candidate;
        }

        private bool IsPlayable(BoardState candidate)
        {
            return FindMatches(candidate).Count == 0
                && moveAnalyzer.HasAnyValidSwap(candidate);
        }

        private static BoardShuffleResult BuildResult(
            BoardState board,
            BoardShuffleKind kind,
            IReadOnlyList<BoardPosition> positions,
            IReadOnlyList<BoardBlock> originalBlocks,
            IReadOnlyList<ElementType> originalElements,
            IReadOnlyList<ElementType> newElements,
            int attemptCount)
        {
            var entries = new List<BoardShuffleEntry>(positions.Count);

            for (int index = 0; index < positions.Count; index++)
            {
                entries.Add(new BoardShuffleEntry(
                    positions[index],
                    originalBlocks[index].RuntimeId,
                    originalElements[index],
                    newElements[index]));
            }

            return new BoardShuffleResult(
                board,
                kind,
                entries,
                attemptCount);
        }

        private static ElementType[] CopyElements(ElementType[] source)
        {
            var copy = new ElementType[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }
    }
}
