using System;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardSwapActionResolver
    {
        private readonly BoardMoveAnalyzer moveAnalyzer;
        private readonly BoardCascadeResolver cascadeResolver;
        private readonly BoardShuffler shuffler;

        public BoardSwapActionResolver(
            BoardMoveAnalyzer moveAnalyzer,
            BoardCascadeResolver cascadeResolver,
            BoardShuffler shuffler)
        {
            this.moveAnalyzer = moveAnalyzer
                ?? throw new ArgumentNullException(nameof(moveAnalyzer));
            this.cascadeResolver = cascadeResolver
                ?? throw new ArgumentNullException(nameof(cascadeResolver));
            this.shuffler = shuffler
                ?? throw new ArgumentNullException(nameof(shuffler));
        }

        public BoardSwapActionResult Resolve(
            BoardState board,
            BoardSwap swap)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (!moveAnalyzer.CanSwap(board, swap.First, swap.Second))
            {
                return new BoardSwapActionResult(
                    swap,
                    BoardSwapActionStatus.NotSwappable,
                    null,
                    null,
                    null,
                    board.Clone());
            }

            BoardState swappedBoard = board.Clone();
            swappedBoard.Swap(swap.First, swap.Second);

            if (!moveAnalyzer.IsValidSwap(
                board,
                swap.First,
                swap.Second))
            {
                return new BoardSwapActionResult(
                    swap,
                    BoardSwapActionStatus.NoMatch,
                    swappedBoard,
                    null,
                    null,
                    board.Clone());
            }

            BoardCascadeResult cascade =
                cascadeResolver.Resolve(swappedBoard);
            if (cascade.CascadeCount == 0 || cascade.ComboCount == 0)
            {
                throw new InvalidOperationException(
                    "A valid swap produced no cascade result.");
            }

            BoardShuffleResult shuffle =
                shuffler.EnsurePlayable(cascade.Board);

            return new BoardSwapActionResult(
                swap,
                BoardSwapActionStatus.Resolved,
                swappedBoard,
                cascade,
                shuffle,
                shuffle.Board);
        }
    }
}
