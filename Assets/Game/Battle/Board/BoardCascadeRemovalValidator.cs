using System;
using System.Collections.Generic;

namespace ValorChronicle.Battle.Board
{
    internal static class BoardCascadeRemovalValidator
    {
        public static void Validate(
            IReadOnlyList<BoardMatch> matches,
            IReadOnlyList<BoardBlockRemoval> removals)
        {
            if (matches == null)
            {
                throw new ArgumentNullException(nameof(matches));
            }

            if (removals == null)
            {
                throw new ArgumentNullException(nameof(removals));
            }

            var removalsByPosition =
                new Dictionary<BoardPosition, BoardBlockRemoval>();
            for (int index = 0; index < removals.Count; index++)
            {
                BoardBlockRemoval removal = removals[index]
                    ?? throw new InvalidOperationException(
                        "Cascade removals cannot contain null.");
                if (removal.Block == null
                    || removalsByPosition.ContainsKey(removal.Position))
                {
                    throw new InvalidOperationException(
                        $"Cascade removal at {removal.Position} is invalid "
                            + "or duplicated.");
                }

                removalsByPosition.Add(removal.Position, removal);
            }

            var matchedPositions = new HashSet<BoardPosition>();
            for (int matchIndex = 0;
                matchIndex < matches.Count;
                matchIndex++)
            {
                BoardMatch match = matches[matchIndex]
                    ?? throw new InvalidOperationException(
                        $"Match at index {matchIndex} is null.");
                for (int positionIndex = 0;
                    positionIndex < match.Positions.Count;
                    positionIndex++)
                {
                    BoardPosition position = match.Positions[positionIndex];
                    if (!matchedPositions.Add(position)
                        || !removalsByPosition.TryGetValue(
                            position,
                            out BoardBlockRemoval removal)
                        || removal.Block.BlockType
                            != BoardBlockType.Normal
                        || removal.Block.Element != match.Element)
                    {
                        throw new InvalidOperationException(
                            $"Matched position {position} must have one "
                                + $"removed Normal {match.Element} block.");
                    }
                }
            }

            foreach (KeyValuePair<BoardPosition, BoardBlockRemoval> pair
                in removalsByPosition)
            {
                if (matchedPositions.Contains(pair.Key))
                {
                    continue;
                }

                BoardBlock block = pair.Value.Block;
                if (block.BlockType != BoardBlockType.Rock
                    || block.Element.HasValue
                    || !IsAdjacentToAny(pair.Key, matchedPositions))
                {
                    throw new InvalidOperationException(
                        $"Additional removal at {pair.Key} must be an "
                            + "elementless Rock adjacent to a matched cell.");
                }
            }
        }

        private static bool IsAdjacentToAny(
            BoardPosition position,
            IEnumerable<BoardPosition> matchedPositions)
        {
            foreach (BoardPosition matchedPosition in matchedPositions)
            {
                int distance = Math.Abs(position.X - matchedPosition.X)
                    + Math.Abs(position.Y - matchedPosition.Y);
                if (distance == 1)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
