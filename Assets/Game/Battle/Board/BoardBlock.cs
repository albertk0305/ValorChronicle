using System;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardBlock
    {
        public BoardBlock(
            long runtimeId,
            BoardBlockType blockType,
            ElementType? element)
        {
            if (blockType == BoardBlockType.Normal && !element.HasValue)
            {
                throw new ArgumentException(
                    "A normal block must have an element.",
                    nameof(element));
            }

            RuntimeId = runtimeId;
            BlockType = blockType;
            Element = element;
        }

        public long RuntimeId { get; }
        public BoardBlockType BlockType { get; }
        public ElementType? Element { get; }
    }
}
