namespace ValorChronicle.Battle.Board
{
    public sealed class BoardBlockRemoval
    {
        internal BoardBlockRemoval(BoardBlock block, BoardPosition position)
        {
            Block = block;
            Position = position;
        }

        public BoardBlock Block { get; }
        public BoardPosition Position { get; }
        public long RuntimeId => Block.RuntimeId;
    }
}
