using System;
using System.Collections.Generic;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Board
{
    public sealed class BoardRefiller
    {
        private static readonly ElementType[] ElementSelectionOrder =
        {
            ElementType.Fire,
            ElementType.Water,
            ElementType.Grass,
            ElementType.Light,
            ElementType.Dark
        };

        private readonly IRandomSource randomSource;
        private readonly BoardBlockIdGenerator idGenerator;

        public BoardRefiller(
            IRandomSource randomSource,
            BoardBlockIdGenerator idGenerator)
        {
            this.randomSource = randomSource
                ?? throw new ArgumentNullException(nameof(randomSource));
            this.idGenerator = idGenerator
                ?? throw new ArgumentNullException(nameof(idGenerator));
        }

        public BoardRefillResult Refill(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            BoardState workingBoard = board.Clone();
            int[] firstEmptyRows = FindFirstEmptyRows(workingBoard);
            var spawns = new List<BoardBlockSpawn>();

            for (int x = 0; x < BoardConstants.Width; x++)
            {
                int firstEmptyY = firstEmptyRows[x];
                int emptyCount = BoardConstants.Height - firstEmptyY;

                for (int spawnIndex = 0;
                    spawnIndex < emptyCount;
                    spawnIndex++)
                {
                    int selectedElementIndex = randomSource.Next(
                        0,
                        ElementSelectionOrder.Length);
                    var block = new BoardBlock(
                        idGenerator.Next(),
                        BoardBlockType.Normal,
                        ElementSelectionOrder[selectedElementIndex]);
                    var target = new BoardPosition(
                        x,
                        firstEmptyY + spawnIndex);
                    int sourceY = BoardConstants.Height + spawnIndex;

                    workingBoard.Set(target, block);
                    spawns.Add(new BoardBlockSpawn(block, target, sourceY));
                }
            }

            return new BoardRefillResult(workingBoard, spawns);
        }

        private static int[] FindFirstEmptyRows(BoardState board)
        {
            var firstEmptyRows = new int[BoardConstants.Width];

            for (int x = 0; x < BoardConstants.Width; x++)
            {
                int firstEmptyY = BoardConstants.Height;

                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    bool isOccupied =
                        board.IsOccupied(new BoardPosition(x, y));

                    if (!isOccupied)
                    {
                        if (firstEmptyY == BoardConstants.Height)
                        {
                            firstEmptyY = y;
                        }

                        continue;
                    }

                    if (firstEmptyY != BoardConstants.Height)
                    {
                        throw new InvalidOperationException(
                            $"Column {x} has an occupied cell above an empty cell.");
                    }
                }

                firstEmptyRows[x] = firstEmptyY;
            }

            return firstEmptyRows;
        }
    }
}
