using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardShuffleEntry
    {
        internal BoardShuffleEntry(
            BoardPosition position,
            long runtimeId,
            ElementType previousElement,
            ElementType newElement)
        {
            Position = position;
            RuntimeId = runtimeId;
            PreviousElement = previousElement;
            NewElement = newElement;
        }

        public BoardPosition Position { get; }
        public long RuntimeId { get; }
        public ElementType PreviousElement { get; }
        public ElementType NewElement { get; }
        public bool Changed => PreviousElement != NewElement;
    }
}
