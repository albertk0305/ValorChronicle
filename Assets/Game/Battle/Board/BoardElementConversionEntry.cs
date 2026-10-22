using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardElementConversionEntry
    {
        internal BoardElementConversionEntry(
            BoardPosition position,
            long runtimeId,
            BoardBlockType blockType,
            ElementType previousElement,
            ElementType newElement)
        {
            Position = position;
            RuntimeId = runtimeId;
            BlockType = blockType;
            PreviousElement = previousElement;
            NewElement = newElement;
        }

        public BoardPosition Position { get; }
        public long RuntimeId { get; }
        public BoardBlockType BlockType { get; }
        public ElementType PreviousElement { get; }
        public ElementType NewElement { get; }
    }
}
