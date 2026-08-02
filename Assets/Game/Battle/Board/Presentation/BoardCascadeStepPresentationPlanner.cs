using System;
using System.Collections.Generic;

namespace ValorChronicle.Battle.Board.Presentation
{
    public sealed class BoardCascadeStepPresentationPlanner
    {
        public BoardCascadeStepPresentationPlan Build(
            BoardState beforeBoard,
            BoardCascadeStep step)
        {
            if (beforeBoard == null)
            {
                throw new ArgumentNullException(nameof(beforeBoard));
            }

            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            Dictionary<long, BoardPosition> beforePositions =
                CollectRuntimeIds(beforeBoard, false, "beforeBoard");
            ValidateRemovals(beforeBoard, step, out HashSet<long> removedIds);
            ValidateMoves(
                beforeBoard,
                step.Collapse.Board,
                step.Collapse.Moves,
                removedIds);
            ValidateCollapseBoard(beforeBoard, step, removedIds);
            ValidateSpawns(beforePositions, step);
            ValidateFinalBoard(step);

            return new BoardCascadeStepPresentationPlan(
                step.Collapse.Removals,
                step.Collapse.Moves,
                step.Refill.Spawns,
                step.Collapse.Board,
                step.Board);
        }

        private static void ValidateRemovals(
            BoardState beforeBoard,
            BoardCascadeStep step,
            out HashSet<long> removedIds)
        {
            var matchPositions = new HashSet<BoardPosition>();
            for (int matchIndex = 0;
                matchIndex < step.Matches.Count;
                matchIndex++)
            {
                BoardMatch match = step.Matches[matchIndex];
                for (int positionIndex = 0;
                    positionIndex < match.Positions.Count;
                    positionIndex++)
                {
                    if (!matchPositions.Add(match.Positions[positionIndex]))
                    {
                        throw new InvalidOperationException(
                            "Match positions cannot be duplicated in a " +
                            "cascade presentation step.");
                    }
                }
            }

            var removalPositions = new HashSet<BoardPosition>();
            removedIds = new HashSet<long>();
            for (int index = 0;
                index < step.Collapse.Removals.Count;
                index++)
            {
                BoardBlockRemoval removal = step.Collapse.Removals[index]
                    ?? throw new InvalidOperationException(
                        "A removal cannot be null.");
                if (!removalPositions.Add(removal.Position))
                {
                    throw new InvalidOperationException(
                        $"Removal position {removal.Position} is duplicated.");
                }

                if (!removedIds.Add(removal.RuntimeId))
                {
                    throw new InvalidOperationException(
                        $"Removal RuntimeId {removal.RuntimeId} is duplicated.");
                }

                BoardBlock beforeBlock = beforeBoard.Get(removal.Position);
                if (!BlocksMatch(beforeBlock, removal.Block))
                {
                    throw new InvalidOperationException(
                        "A removal must match beforeBoard at " +
                        $"{removal.Position}.");
                }
            }

            if (!removalPositions.SetEquals(matchPositions))
            {
                throw new InvalidOperationException(
                    "The removal positions must equal the union of all " +
                    "match positions.");
            }
        }

        private static void ValidateMoves(
            BoardState beforeBoard,
            BoardState collapseBoard,
            IReadOnlyList<BoardBlockMove> moves,
            HashSet<long> removedIds)
        {
            var runtimeIds = new HashSet<long>();
            var fromPositions = new HashSet<BoardPosition>();
            var toPositions = new HashSet<BoardPosition>();

            for (int index = 0; index < moves.Count; index++)
            {
                BoardBlockMove move = moves[index]
                    ?? throw new InvalidOperationException(
                        "A collapse move cannot be null.");
                if (removedIds.Contains(move.RuntimeId))
                {
                    throw new InvalidOperationException(
                        "A removed RuntimeId cannot also move.");
                }

                if (!runtimeIds.Add(move.RuntimeId)
                    || !fromPositions.Add(move.From)
                    || !toPositions.Add(move.To))
                {
                    throw new InvalidOperationException(
                        "Collapse moves cannot duplicate RuntimeIds, From " +
                        "positions, or To positions.");
                }

                if (move.From == move.To
                    || move.From.X != move.To.X
                    || move.To.Y >= move.From.Y)
                {
                    throw new InvalidOperationException(
                        "A collapse move must travel downward in one column.");
                }

                if (!BlocksMatch(beforeBoard.Get(move.From), move.Block))
                {
                    throw new InvalidOperationException(
                        $"A collapse move must match beforeBoard at {move.From}.");
                }

                if (!BlocksMatch(collapseBoard.Get(move.To), move.Block))
                {
                    throw new InvalidOperationException(
                        "A collapse move must match Collapse.Board at " +
                        $"{move.To}.");
                }
            }
        }

        private static void ValidateCollapseBoard(
            BoardState beforeBoard,
            BoardCascadeStep step,
            HashSet<long> removedIds)
        {
            var moveTargets = new Dictionary<long, BoardPosition>();
            for (int index = 0; index < step.Collapse.Moves.Count; index++)
            {
                BoardBlockMove move = step.Collapse.Moves[index];
                moveTargets.Add(move.RuntimeId, move.To);
            }

            var expected = new Dictionary<BoardPosition, BoardBlock>();
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    BoardBlock block = beforeBoard.Get(position);
                    if (block == null || removedIds.Contains(block.RuntimeId))
                    {
                        continue;
                    }

                    BoardPosition target = moveTargets.TryGetValue(
                        block.RuntimeId,
                        out BoardPosition movedTarget)
                        ? movedTarget
                        : position;
                    if (expected.ContainsKey(target))
                    {
                        throw new InvalidOperationException(
                            $"More than one block collapses to {target}.");
                    }

                    expected.Add(target, block);
                }
            }

            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    expected.TryGetValue(position, out BoardBlock block);
                    if (!BlocksMatch(block, step.Collapse.Board.Get(position)))
                    {
                        throw new InvalidOperationException(
                            "Collapse.Board does not match removals and moves " +
                            $"at {position}.");
                    }
                }
            }
        }

        private static void ValidateSpawns(
            Dictionary<long, BoardPosition> beforePositions,
            BoardCascadeStep step)
        {
            var runtimeIds = new HashSet<long>();
            var targets = new HashSet<BoardPosition>();
            for (int index = 0; index < step.Refill.Spawns.Count; index++)
            {
                BoardBlockSpawn spawn = step.Refill.Spawns[index]
                    ?? throw new InvalidOperationException(
                        "A refill spawn cannot be null.");
                if (beforePositions.ContainsKey(spawn.RuntimeId))
                {
                    throw new InvalidOperationException(
                        "A refill spawn must have a new RuntimeId.");
                }

                if (!runtimeIds.Add(spawn.RuntimeId)
                    || !targets.Add(spawn.Target))
                {
                    throw new InvalidOperationException(
                        "Refill spawns cannot duplicate RuntimeIds or targets.");
                }

                if (spawn.SourceY < BoardConstants.Height)
                {
                    throw new InvalidOperationException(
                        "A refill source must be above the board.");
                }

                if (step.Collapse.Board.IsOccupied(spawn.Target))
                {
                    throw new InvalidOperationException(
                        $"A refill target {spawn.Target} must be empty.");
                }

                ValidateSupportedBlock(spawn.Block, "refill spawn");
                if (!BlocksMatch(step.Board.Get(spawn.Target), spawn.Block))
                {
                    throw new InvalidOperationException(
                        $"A refill spawn must match Board at {spawn.Target}.");
                }
            }
        }

        private static void ValidateFinalBoard(BoardCascadeStep step)
        {
            Dictionary<long, BoardPosition> finalPositions =
                CollectRuntimeIds(step.Board, true, "step.Board");
            if (finalPositions.Count != BoardConstants.CellCount)
            {
                throw new InvalidOperationException(
                    "A cascade step final Board must occupy all 30 cells.");
            }

            var spawnIds = new HashSet<long>();
            for (int index = 0; index < step.Refill.Spawns.Count; index++)
            {
                spawnIds.Add(step.Refill.Spawns[index].RuntimeId);
            }

            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    BoardBlock collapseBlock = step.Collapse.Board.Get(position);
                    BoardBlock finalBlock = step.Board.Get(position);
                    if (collapseBlock != null
                        && !BlocksMatch(collapseBlock, finalBlock))
                    {
                        throw new InvalidOperationException(
                            "Refill Board changed a surviving block at " +
                            $"{position}.");
                    }

                    if (collapseBlock == null
                        && !spawnIds.Contains(finalBlock.RuntimeId))
                    {
                        throw new InvalidOperationException(
                            "Every empty collapse position must be supplied " +
                            $"by a refill spawn at {position}.");
                    }
                }
            }
        }

        private static Dictionary<long, BoardPosition> CollectRuntimeIds(
            BoardState board,
            bool requireSupported,
            string boardName)
        {
            var positions = new Dictionary<long, BoardPosition>();
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    BoardBlock block = board.Get(position);
                    if (block == null)
                    {
                        continue;
                    }

                    if (requireSupported)
                    {
                        ValidateSupportedBlock(block, boardName);
                    }

                    if (positions.ContainsKey(block.RuntimeId))
                    {
                        throw new InvalidOperationException(
                            $"RuntimeId {block.RuntimeId} is duplicated in " +
                            $"{boardName}.");
                    }

                    positions.Add(block.RuntimeId, position);
                }
            }

            return positions;
        }

        private static void ValidateSupportedBlock(
            BoardBlock block,
            string context)
        {
            if (block == null
                || block.BlockType != BoardBlockType.Normal
                || !block.Element.HasValue)
            {
                throw new NotSupportedException(
                    $"Only Normal blocks with an Element are supported in {context}.");
            }
        }

        private static bool BlocksMatch(BoardBlock expected, BoardBlock actual)
        {
            if (expected == null || actual == null)
            {
                return expected == null && actual == null;
            }

            return expected.RuntimeId == actual.RuntimeId
                && expected.BlockType == actual.BlockType
                && expected.Element == actual.Element;
        }
    }
}
