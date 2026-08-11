using System;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardRockPlacement
    {
        internal BoardRockPlacement(
            BoardPosition position,
            BoardBlock replacedBlock,
            BoardBlock rockBlock)
        {
            Position = position;
            ReplacedBlock = replacedBlock
                ?? throw new ArgumentNullException(nameof(replacedBlock));
            RockBlock = rockBlock
                ?? throw new ArgumentNullException(nameof(rockBlock));
            if (replacedBlock.BlockType != BoardBlockType.Normal
                || rockBlock.BlockType != BoardBlockType.Rock)
            {
                throw new ArgumentException(
                    "A Rock placement must replace a Normal block.");
            }
        }

        public BoardPosition Position { get; }
        public BoardBlock ReplacedBlock { get; }
        public BoardBlock RockBlock { get; }
    }
}
