using System;
using System.Collections.Generic;

namespace ValorChronicle.Battle.Board.Presentation
{
    public sealed class BoardShufflePresentationPlanner
    {
        public BoardShufflePresentationPlan Build(
            BoardState beforeBoard,
            BoardShuffleResult result)
        {
            if (beforeBoard == null)
            {
                throw new ArgumentNullException(nameof(beforeBoard));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            switch (result.Kind)
            {
                case BoardShuffleKind.None:
                    if (result.Entries.Count != 0)
                    {
                        throw new InvalidOperationException(
                            "A None shuffle cannot contain entries.");
                    }

                    ValidateSameLayout(beforeBoard, result.Board);
                    break;

                case BoardShuffleKind.Permutation:
                case BoardShuffleKind.Regeneration:
                    ValidateShuffle(beforeBoard, result);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(result),
                        result.Kind,
                        "Unknown shuffle kind.");
            }

            return new BoardShufflePresentationPlan(
                result.Kind,
                result.Entries,
                result.Board);
        }

        private static void ValidateShuffle(
            BoardState beforeBoard,
            BoardShuffleResult result)
        {
            var positions = new HashSet<BoardPosition>();
            var runtimeIds = new HashSet<long>();
            var entriesByPosition =
                new Dictionary<BoardPosition, BoardShuffleEntry>();

            for (int index = 0; index < result.Entries.Count; index++)
            {
                BoardShuffleEntry entry = result.Entries[index];
                if (!positions.Add(entry.Position)
                    || !runtimeIds.Add(entry.RuntimeId))
                {
                    throw new InvalidOperationException(
                        "Shuffle entries cannot duplicate Position or RuntimeId.");
                }

                BoardBlock beforeBlock = beforeBoard.Get(entry.Position);
                BoardBlock afterBlock = result.Board.Get(entry.Position);
                if (beforeBlock == null
                    || beforeBlock.BlockType != BoardBlockType.Normal
                    || !beforeBlock.Element.HasValue
                    || beforeBlock.RuntimeId != entry.RuntimeId
                    || beforeBlock.Element.Value != entry.PreviousElement)
                {
                    throw new InvalidOperationException(
                        "A shuffle entry does not match beforeBoard at " +
                        $"{entry.Position}.");
                }

                if (afterBlock == null
                    || afterBlock.BlockType != BoardBlockType.Normal
                    || !afterBlock.Element.HasValue
                    || afterBlock.RuntimeId != entry.RuntimeId
                    || afterBlock.Element.Value != entry.NewElement)
                {
                    throw new InvalidOperationException(
                        "A shuffle entry does not match result.Board at " +
                        $"{entry.Position}.");
                }

                entriesByPosition.Add(entry.Position, entry);
            }

            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                BoardBlock beforeBlock = beforeBoard.Get(position);
                BoardBlock afterBlock = result.Board.Get(position);
                if (beforeBlock == null || afterBlock == null)
                {
                    throw new InvalidOperationException(
                        "Shuffle boards must occupy all 30 cells.");
                }

                if (beforeBlock.RuntimeId != afterBlock.RuntimeId
                    || beforeBlock.BlockType != afterBlock.BlockType)
                {
                    throw new InvalidOperationException(
                        "A shuffle must retain RuntimeId and BlockType at " +
                        $"{position}.");
                }

                if (beforeBlock.BlockType == BoardBlockType.Normal)
                {
                    if (!entriesByPosition.ContainsKey(position))
                    {
                        throw new InvalidOperationException(
                            "Every Normal block must have one shuffle entry at " +
                            $"{position}.");
                    }
                }
                else
                {
                    if (entriesByPosition.ContainsKey(position)
                        || beforeBlock.Element != afterBlock.Element)
                    {
                        throw new InvalidOperationException(
                            "A non-Normal block must remain unchanged at " +
                            $"{position}.");
                    }
                }
            }
        }

        private static void ValidateSameLayout(
            BoardState beforeBoard,
            BoardState resultBoard)
        {
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                if (!BlocksMatch(
                    beforeBoard.Get(position),
                    resultBoard.Get(position)))
                {
                    throw new InvalidOperationException(
                        "A None shuffle Board must match beforeBoard at " +
                        $"{position}.");
                }
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
