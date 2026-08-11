using System;
using System.Collections.Generic;
using ValorChronicle.Core.Random;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardRockMutationResolver
    {
        public const int DefaultMaxPlacementAttempts = 64;

        private readonly IRandomSource randomSource;
        private readonly BoardBlockIdGenerator idGenerator;
        private readonly BoardMoveAnalyzer moveAnalyzer;
        private readonly BoardShuffler shuffler;
        private readonly int maxPlacementAttempts;

        public BoardRockMutationResolver(
            IRandomSource randomSource,
            BoardBlockIdGenerator idGenerator,
            BoardMoveAnalyzer moveAnalyzer,
            BoardShuffler shuffler,
            int maxPlacementAttempts = DefaultMaxPlacementAttempts)
        {
            this.randomSource = randomSource
                ?? throw new ArgumentNullException(nameof(randomSource));
            this.idGenerator = idGenerator
                ?? throw new ArgumentNullException(nameof(idGenerator));
            this.moveAnalyzer = moveAnalyzer
                ?? throw new ArgumentNullException(nameof(moveAnalyzer));
            this.shuffler = shuffler
                ?? throw new ArgumentNullException(nameof(shuffler));
            if (maxPlacementAttempts <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxPlacementAttempts));
            }

            this.maxPlacementAttempts = maxPlacementAttempts;
        }

        public BoardRockMutationResult Resolve(
            BoardState board,
            int requestedCount,
            int maximumRockCount)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (requestedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedCount));
            }

            if (maximumRockCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumRockCount));
            }

            ValidateStableFullBoard(board);
            if (BoardMatchFinder.FindMatches(board).Count != 0)
            {
                throw new InvalidOperationException(
                    "Rock mutation requires a board without completed matches.");
            }

            CollectBoardData(
                board,
                out List<BoardPosition> candidates,
                out HashSet<long> existingRuntimeIds,
                out int currentRockCount);
            if (currentRockCount > maximumRockCount)
            {
                throw new InvalidOperationException(
                    "The board already exceeds the maximum Rock count.");
            }

            int availableCapacity = maximumRockCount - currentRockCount;
            int targetCount = Math.Min(
                Math.Min(requestedCount, availableCapacity),
                candidates.Count);
            BoardState lastCandidate = board.Clone();
            IReadOnlyList<BoardRockPlacement> lastPlacements =
                Array.Empty<BoardRockPlacement>();
            int attemptCount = 0;

            if (targetCount == 0
                && moveAnalyzer.HasAnyValidSwap(lastCandidate))
            {
                return Success(
                    lastCandidate,
                    requestedCount,
                    lastPlacements,
                    attemptCount,
                    shuffleResult: null);
            }

            for (int attempt = 0;
                attempt < maxPlacementAttempts && targetCount > 0;
                attempt++)
            {
                attemptCount++;
                SelectPositions(candidates, targetCount, out var selected);
                BuildCandidate(
                    board,
                    selected,
                    existingRuntimeIds,
                    out lastCandidate,
                    out lastPlacements);
                if (moveAnalyzer.HasAnyValidSwap(lastCandidate))
                {
                    return Success(
                        lastCandidate,
                        requestedCount,
                        lastPlacements,
                        attemptCount,
                        shuffleResult: null);
                }
            }

            try
            {
                BoardShuffleResult shuffleResult =
                    shuffler.EnsurePlayable(lastCandidate);
                return Success(
                    shuffleResult.Board,
                    requestedCount,
                    lastPlacements,
                    attemptCount,
                    shuffleResult);
            }
            catch (InvalidOperationException)
            {
                return new BoardRockMutationResult(
                    board.Clone(),
                    requestedCount,
                    Array.Empty<BoardRockPlacement>(),
                    attemptCount,
                    shuffleResult: null,
                    BoardRockMutationFailure.UnableToCreatePlayableBoard);
            }
        }

        private void SelectPositions(
            IReadOnlyList<BoardPosition> candidates,
            int count,
            out List<BoardPosition> selected)
        {
            var available = new List<BoardPosition>(candidates);
            selected = new List<BoardPosition>(count);
            for (int index = 0; index < count; index++)
            {
                int selectedIndex = randomSource.Next(
                    index,
                    available.Count);
                BoardPosition temporary = available[index];
                available[index] = available[selectedIndex];
                available[selectedIndex] = temporary;
                selected.Add(available[index]);
            }
        }

        private void BuildCandidate(
            BoardState board,
            IReadOnlyList<BoardPosition> selected,
            ISet<long> existingRuntimeIds,
            out BoardState candidate,
            out IReadOnlyList<BoardRockPlacement> placements)
        {
            candidate = board.Clone();
            var generatedRuntimeIds = new HashSet<long>();
            var created = new List<BoardRockPlacement>(selected.Count);
            for (int index = 0; index < selected.Count; index++)
            {
                BoardPosition position = selected[index];
                BoardBlock replacedBlock = board.Get(position);
                long runtimeId = idGenerator.Next();
                if (existingRuntimeIds.Contains(runtimeId)
                    || !generatedRuntimeIds.Add(runtimeId))
                {
                    throw new InvalidOperationException(
                        "BoardBlockIdGenerator returned an existing runtime ID.");
                }

                var rock = new BoardBlock(
                    runtimeId,
                    BoardBlockType.Rock,
                    element: null);
                candidate.Set(position, rock);
                created.Add(new BoardRockPlacement(
                    position,
                    replacedBlock,
                    rock));
            }

            placements = created.AsReadOnly();
        }

        private static void CollectBoardData(
            BoardState board,
            out List<BoardPosition> candidates,
            out HashSet<long> runtimeIds,
            out int rockCount)
        {
            candidates = new List<BoardPosition>();
            runtimeIds = new HashSet<long>();
            rockCount = 0;
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    BoardBlock block = board.Get(position);
                    if (!runtimeIds.Add(block.RuntimeId))
                    {
                        throw new InvalidOperationException(
                            $"Duplicate board runtime ID {block.RuntimeId}.");
                    }

                    if (block.BlockType == BoardBlockType.Normal)
                    {
                        candidates.Add(position);
                    }
                    else if (block.BlockType == BoardBlockType.Rock)
                    {
                        rockCount++;
                    }
                }
            }
        }

        private static void ValidateStableFullBoard(BoardState board)
        {
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                if (!board.IsOccupied(position))
                {
                    throw new InvalidOperationException(
                        $"Rock mutation requires a full board: {position}.");
                }
            }
        }

        private static BoardRockMutationResult Success(
            BoardState board,
            int requestedCount,
            IReadOnlyList<BoardRockPlacement> placements,
            int attemptCount,
            BoardShuffleResult shuffleResult)
        {
            return new BoardRockMutationResult(
                board,
                requestedCount,
                placements,
                attemptCount,
                shuffleResult,
                BoardRockMutationFailure.None);
        }
    }
}
