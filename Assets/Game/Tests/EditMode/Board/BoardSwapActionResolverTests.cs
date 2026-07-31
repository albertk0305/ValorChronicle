using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board
{
    public sealed class BoardSwapActionResolverTests
    {
        private static readonly int[] DeadBoardElements =
        {
            3, 0, 2, 1, 0,
            0, 2, 3, 1, 2,
            4, 4, 0, 2, 4,
            1, 2, 0, 0, 4,
            0, 1, 2, 1, 2,
            0, 4, 4, 0, 3
        };

        private static readonly int[] PlayableBoardElements =
        {
            0, 0, 1, 4, 0,
            4, 0, 4, 3, 4,
            1, 3, 0, 3, 1,
            0, 4, 1, 1, 4,
            0, 0, 2, 4, 4,
            1, 2, 4, 0, 0
        };

        private static readonly int[] FourAttemptPermutationSequence =
        {
            20, 3, 0, 23, 8, 7, 7, 4, 3, 17, 2, 18, 13, 1, 0,
            1, 3, 3, 8, 9, 0, 8, 3, 5, 5, 4, 3, 0, 1,
            18, 8, 25, 0, 24, 5, 22, 13, 10, 8, 4, 6, 10, 3, 2,
            6, 1, 5, 5, 9, 4, 0, 7, 4, 0, 3, 0, 2, 1,
            26, 20, 19, 11, 18, 6, 22, 2, 1, 7, 9, 2, 7, 3, 12,
            4, 7, 10, 5, 2, 5, 5, 3, 5, 2, 0, 1, 2, 0,
            5, 14, 12, 8, 20, 22, 17, 7, 21, 10, 1, 7, 1, 10, 12,
            4, 1, 3, 9, 5, 3, 7, 6, 5, 3, 1, 2, 0, 0
        };

        [Test]
        public void Resolve_SamePositionReturnsNotSwappable()
        {
            BoardState board = CreateBoard(PlayableBoardElements);
            var position = new BoardPosition(2, 2);

            AssertNotSwappable(board, new BoardSwap(position, position));
        }

        [Test]
        public void Resolve_DiagonalSwapReturnsNotSwappable()
        {
            BoardState board = CreateBoard(PlayableBoardElements);

            AssertNotSwappable(
                board,
                new BoardSwap(
                    new BoardPosition(1, 1),
                    new BoardPosition(2, 2)));
        }

        [Test]
        public void Resolve_DistantSwapReturnsNotSwappable()
        {
            BoardState board = CreateBoard(PlayableBoardElements);

            AssertNotSwappable(
                board,
                new BoardSwap(
                    new BoardPosition(0, 0),
                    new BoardPosition(3, 0)));
        }

        [Test]
        public void Resolve_EmptyCellSwapReturnsNotSwappable()
        {
            BoardState board = CreateBoard(PlayableBoardElements);
            board.Clear(new BoardPosition(1, 0));

            AssertNotSwappable(
                board,
                new BoardSwap(
                    new BoardPosition(0, 0),
                    new BoardPosition(1, 0)));
        }

        [TestCase(BoardBlockType.Rock)]
        [TestCase(BoardBlockType.Special)]
        [TestCase(BoardBlockType.Locked)]
        public void Resolve_NonNormalBlockReturnsNotSwappable(
            BoardBlockType blockType)
        {
            BoardState board = CreateBoard(PlayableBoardElements);
            var fixedPosition = new BoardPosition(1, 0);
            long runtimeId = board.Get(fixedPosition).RuntimeId;
            board.Set(
                fixedPosition,
                new BoardBlock(runtimeId, blockType, ElementType.Dark));

            AssertNotSwappable(
                board,
                new BoardSwap(
                    new BoardPosition(0, 0),
                    fixedPosition));
        }

        [Test]
        public void Resolve_SameElementSwapReturnsNotSwappable()
        {
            BoardState board = CreateBoard(PlayableBoardElements);
            SetElement(board, 0, 0, ElementType.Fire);
            SetElement(board, 1, 0, ElementType.Fire);

            AssertNotSwappable(
                board,
                new BoardSwap(
                    new BoardPosition(0, 0),
                    new BoardPosition(1, 0)));
        }

        [Test]
        public void Resolve_NoMatchReturnsTemporarySwapAndOriginalFinalBoard()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            var swap = new BoardSwap(
                new BoardPosition(0, 0),
                new BoardPosition(1, 0));
            BoardBlock first = board.Get(swap.First);
            BoardBlock second = board.Get(swap.Second);
            var random = new RecordingRandomSource(Array.Empty<int>());
            var idGenerator = new BoardBlockIdGenerator(100);

            BoardSwapActionResult result = CreateResolver(
                random,
                idGenerator).Resolve(board, swap);

            Assert.That(result.Status, Is.EqualTo(BoardSwapActionStatus.NoMatch));
            Assert.That(result.Swap, Is.EqualTo(swap));
            Assert.That(result.SwappedBoard.Get(swap.First), Is.SameAs(second));
            Assert.That(result.SwappedBoard.Get(swap.Second), Is.SameAs(first));
            AssertBoardsHaveSameContents(board, result.Board);
            Assert.That(result.Cascade, Is.Null);
            Assert.That(result.Shuffle, Is.Null);
            Assert.That(result.ConsumesTurn, Is.False);
            Assert.That(result.RequiresSwapBack, Is.True);
            Assert.That(result.WasShuffled, Is.False);
            Assert.That(random.CallCount, Is.Zero);
            Assert.That(idGenerator.Next(), Is.EqualTo(100));
        }

        [Test]
        public void Resolve_NoMatchBoardsAreMutuallyIndependent()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            var swap = new BoardSwap(
                new BoardPosition(0, 0),
                new BoardPosition(1, 0));
            var random = new RecordingRandomSource(Array.Empty<int>());

            BoardSwapActionResult result =
                CreateResolver(random, 100).Resolve(board, swap);

            Assert.That(result.SwappedBoard, Is.Not.SameAs(board));
            Assert.That(result.Board, Is.Not.SameAs(board));
            Assert.That(result.Board, Is.Not.SameAs(result.SwappedBoard));

            result.SwappedBoard.Clear(new BoardPosition(5, 4));
            Assert.That(result.Board.Get(new BoardPosition(5, 4)), Is.Not.Null);
            Assert.That(board.Get(new BoardPosition(5, 4)), Is.Not.Null);
        }

        [Test]
        public void Resolve_HorizontalMatchRunsCascadeAndNeedsNoShuffle()
        {
            BoardState board = CreateSwapToTargetBoard(PlayableBoardElements);
            BoardSwap swap = CreateTopHorizontalMatchSwap();
            var random = new RecordingRandomSource(new[] { 0, 4, 1 });

            BoardSwapActionResult result =
                CreateResolver(random, 31).Resolve(board, swap);

            AssertResolved(result);
            Assert.That(result.Cascade.CascadeCount, Is.EqualTo(1));
            Assert.That(result.Cascade.ComboCount, Is.EqualTo(1));
            Assert.That(result.Shuffle.Kind, Is.EqualTo(BoardShuffleKind.None));
            Assert.That(result.WasShuffled, Is.False);
            Assert.That(random.CallCount, Is.EqualTo(3));
            AssertBoardsHaveElements(result.Board, PlayableBoardElements);
        }

        [Test]
        public void Resolve_VerticalMatchProducesResolvedResult()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            SetElement(board, 2, 0, ElementType.Fire);
            SetElement(board, 2, 1, ElementType.Fire);
            SetElement(board, 2, 2, ElementType.Water);
            SetElement(board, 2, 3, ElementType.Fire);
            var swap = new BoardSwap(
                new BoardPosition(2, 2),
                new BoardPosition(2, 3));
            Assert.That(
                new BoardMoveAnalyzer().IsValidSwap(
                    board,
                    swap.First,
                    swap.Second),
                Is.True);

            BoardSwapActionResult result = CreateResolver(
                new SeededRandomSource(101),
                31).Resolve(board, swap);

            AssertResolved(result);
            Assert.That(result.Cascade.ComboCount, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void Resolve_FourMatchProducesResolvedResult()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            SetElement(board, 0, 2, ElementType.Fire);
            SetElement(board, 1, 2, ElementType.Fire);
            SetElement(board, 2, 2, ElementType.Fire);
            SetElement(board, 3, 2, ElementType.Water);
            SetElement(board, 4, 2, ElementType.Fire);
            var swap = new BoardSwap(
                new BoardPosition(3, 2),
                new BoardPosition(4, 2));
            Assert.That(
                new BoardMoveAnalyzer().IsValidSwap(
                    board,
                    swap.First,
                    swap.Second),
                Is.True);

            BoardSwapActionResult result = CreateResolver(
                new SeededRandomSource(202),
                31).Resolve(board, swap);

            AssertResolved(result);
            Assert.That(
                result.Cascade.Steps[0].Matches[0].Tier,
                Is.EqualTo(BoardMatchTier.Four));
        }

        [Test]
        public void Resolve_CompoundMatchProducesOneMatchEvent()
        {
            BoardState board = CreateBoard(DeadBoardElements);
            SetElement(board, 0, 2, ElementType.Fire);
            SetElement(board, 1, 2, ElementType.Fire);
            SetElement(board, 2, 1, ElementType.Fire);
            SetElement(board, 2, 2, ElementType.Water);
            SetElement(board, 2, 3, ElementType.Fire);
            SetElement(board, 3, 2, ElementType.Fire);
            var swap = new BoardSwap(
                new BoardPosition(2, 2),
                new BoardPosition(3, 2));
            Assert.That(
                new BoardMoveAnalyzer().IsValidSwap(
                    board,
                    swap.First,
                    swap.Second),
                Is.True);

            BoardSwapActionResult result = CreateResolver(
                new SeededRandomSource(303),
                31).Resolve(board, swap);

            AssertResolved(result);
            Assert.That(
                result.Cascade.Steps[0].MatchEventCount,
                Is.EqualTo(1));
            Assert.That(
                result.Cascade.Steps[0].Matches[0].BlockCount,
                Is.EqualTo(5));
        }

        [Test]
        public void Resolve_NaturalChainProcessesMultipleCascadeSteps()
        {
            BoardState board = CreateTwoStepSwapBoard();
            var swap = new BoardSwap(
                new BoardPosition(2, 0),
                new BoardPosition(3, 0));
            int[] sequence = Combine(
                new[] { 0, 0, 0, 0, 1, 2 },
                CreateIdentityPermutationSequence(30),
                PlayableBoardElements);
            var random = new RecordingRandomSource(sequence);

            BoardSwapActionResult result = CreateResolver(
                random,
                31,
                maxPermutationAttempts: 1,
                maxRegenerationAttempts: 1).Resolve(board, swap);

            AssertResolved(result);
            Assert.That(result.Cascade.CascadeCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(result.Cascade.ComboCount, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void Resolve_ExistingMatchWithUnrelatedSwapReturnsNoMatch()
        {
            BoardState board = CreateBoard(PlayableBoardElements);
            SetElement(board, 0, 0, ElementType.Dark);
            SetElement(board, 1, 0, ElementType.Dark);
            SetElement(board, 2, 0, ElementType.Dark);
            var swap = new BoardSwap(
                new BoardPosition(0, 1),
                new BoardPosition(0, 2));
            Assert.That(BoardMatchFinder.FindMatches(board), Is.Not.Empty);
            Assert.That(
                new BoardMoveAnalyzer().IsValidSwap(
                    board,
                    swap.First,
                    swap.Second),
                Is.False);
            var random = new RecordingRandomSource(Array.Empty<int>());

            BoardSwapActionResult result =
                CreateResolver(random, 100).Resolve(board, swap);

            Assert.That(result.Status, Is.EqualTo(BoardSwapActionStatus.NoMatch));
            Assert.That(BoardMatchFinder.FindMatches(result.Board), Is.Not.Empty);
            Assert.That(result.Cascade, Is.Null);
            Assert.That(result.Shuffle, Is.Null);
            Assert.That(random.CallCount, Is.Zero);
        }

        [Test]
        public void Resolve_NewMatchAlongsideExistingMatchProcessesBothInFirstStep()
        {
            BoardState board = CreateBoard(PlayableBoardElements);
            SetElement(board, 0, 0, ElementType.Dark);
            SetElement(board, 1, 0, ElementType.Dark);
            SetElement(board, 2, 0, ElementType.Dark);
            SetElement(board, 3, 4, ElementType.Water);
            SetElement(board, 4, 4, ElementType.Fire);
            SetElement(board, 5, 4, ElementType.Water);
            SetElement(board, 4, 3, ElementType.Water);
            var swap = new BoardSwap(
                new BoardPosition(4, 3),
                new BoardPosition(4, 4));
            Assert.That(
                new BoardMoveAnalyzer().IsValidSwap(
                    board,
                    swap.First,
                    swap.Second),
                Is.True);

            BoardSwapActionResult result = CreateResolver(
                new SeededRandomSource(404),
                31).Resolve(board, swap);

            AssertResolved(result);
            Assert.That(
                result.Cascade.Steps[0].MatchEventCount,
                Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void Resolve_DeadStableBoardIsAutomaticallyShuffled()
        {
            BoardState board = CreateSwapToTargetBoard(DeadBoardElements);
            BoardSwap swap = CreateTopHorizontalMatchSwap();
            int[] sequence = Combine(
                new[] { 0, 2, 4 },
                FourAttemptPermutationSequence);
            var random = new RecordingRandomSource(sequence);

            BoardSwapActionResult result = CreateResolver(
                random,
                31,
                maxPermutationAttempts: 4).Resolve(board, swap);

            AssertResolved(result);
            Assert.That(
                result.Shuffle.Kind,
                Is.EqualTo(BoardShuffleKind.Permutation));
            Assert.That(result.WasShuffled, Is.True);
            Assert.That(result.Cascade.ComboCount, Is.EqualTo(1));
            Assert.That(BoardMatchFinder.FindMatches(result.Board), Is.Empty);
            Assert.That(
                new BoardMoveAnalyzer().HasAnyValidSwap(result.Board),
                Is.True);
        }

        [Test]
        public void Resolve_ShuffleDoesNotChangeCascadeComboCountOrIssueIds()
        {
            BoardState board = CreateSwapToTargetBoard(DeadBoardElements);
            BoardSwap swap = CreateTopHorizontalMatchSwap();
            int[] sequence = Combine(
                new[] { 0, 2, 4 },
                FourAttemptPermutationSequence);
            var random = new RecordingRandomSource(sequence);
            var idGenerator = new BoardBlockIdGenerator(31);

            BoardSwapActionResult result = CreateResolver(
                random,
                idGenerator,
                maxPermutationAttempts: 4).Resolve(board, swap);

            Assert.That(result.Cascade.ComboCount, Is.EqualTo(1));
            Assert.That(result.Shuffle.WasShuffled, Is.True);
            Assert.That(result.Cascade.TotalSpawnedBlockCount, Is.EqualTo(3));
            Assert.That(idGenerator.Next(), Is.EqualTo(34));
            for (int index = 0; index < result.Shuffle.Entries.Count; index++)
            {
                BoardShuffleEntry entry = result.Shuffle.Entries[index];
                Assert.That(
                    result.Board.Get(entry.Position).RuntimeId,
                    Is.EqualTo(entry.RuntimeId));
            }
        }

        [Test]
        public void Resolve_SwappedBoardMovesRuntimeIdsAndKeepsInputUnchanged()
        {
            BoardState board = CreateSwapToTargetBoard(PlayableBoardElements);
            BoardSwap swap = CreateTopHorizontalMatchSwap();
            BoardBlock first = board.Get(swap.First);
            BoardBlock second = board.Get(swap.Second);
            BoardBlock[] originalContents = CaptureBoardContents(board);
            var random = new RecordingRandomSource(new[] { 0, 4, 1 });

            BoardSwapActionResult result =
                CreateResolver(random, 31).Resolve(board, swap);

            Assert.That(result.SwappedBoard.Get(swap.First), Is.SameAs(second));
            Assert.That(result.SwappedBoard.Get(swap.Second), Is.SameAs(first));
            AssertBoardContainsReferences(board, originalContents);
            Assert.That(result.SwappedBoard, Is.Not.SameAs(board));
            Assert.That(result.Cascade.Board, Is.Not.SameAs(result.SwappedBoard));
            Assert.That(result.Board, Is.Not.SameAs(result.Cascade.Board));
        }

        [Test]
        public void Resolve_SameConditionsProduceDeterministicResult()
        {
            BoardState board = CreateSwapToTargetBoard(DeadBoardElements);
            BoardSwap swap = CreateTopHorizontalMatchSwap();
            int[] sequence = Combine(
                new[] { 0, 2, 4 },
                FourAttemptPermutationSequence);

            BoardSwapActionResult first = CreateResolver(
                new RecordingRandomSource(sequence),
                31,
                maxPermutationAttempts: 4).Resolve(board, swap);
            BoardSwapActionResult second = CreateResolver(
                new RecordingRandomSource(sequence),
                31,
                maxPermutationAttempts: 4).Resolve(board, swap);

            Assert.That(second.Status, Is.EqualTo(first.Status));
            Assert.That(
                second.Cascade.CascadeCount,
                Is.EqualTo(first.Cascade.CascadeCount));
            Assert.That(
                second.Cascade.ComboCount,
                Is.EqualTo(first.Cascade.ComboCount));
            Assert.That(second.Shuffle.Kind, Is.EqualTo(first.Shuffle.Kind));
            AssertBoardsHaveEquivalentData(first.Board, second.Board);
        }

        [Test]
        public void Resolve_NullBoardThrows()
        {
            var resolver = CreateResolver(
                new RecordingRandomSource(Array.Empty<int>()),
                1);

            Assert.Throws<ArgumentNullException>(
                () => resolver.Resolve(
                    null,
                    new BoardSwap(
                        new BoardPosition(0, 0),
                        new BoardPosition(1, 0))));
        }

        [Test]
        public void Constructor_NullMoveAnalyzerThrows()
        {
            ResolverDependencies dependencies = CreateDependencies(
                new RecordingRandomSource(Array.Empty<int>()),
                new BoardBlockIdGenerator());

            Assert.Throws<ArgumentNullException>(
                () => new BoardSwapActionResolver(
                    null,
                    dependencies.CascadeResolver,
                    dependencies.Shuffler));
        }

        [Test]
        public void Constructor_NullCascadeResolverThrows()
        {
            ResolverDependencies dependencies = CreateDependencies(
                new RecordingRandomSource(Array.Empty<int>()),
                new BoardBlockIdGenerator());

            Assert.Throws<ArgumentNullException>(
                () => new BoardSwapActionResolver(
                    dependencies.MoveAnalyzer,
                    null,
                    dependencies.Shuffler));
        }

        [Test]
        public void Constructor_NullShufflerThrows()
        {
            ResolverDependencies dependencies = CreateDependencies(
                new RecordingRandomSource(Array.Empty<int>()),
                new BoardBlockIdGenerator());

            Assert.Throws<ArgumentNullException>(
                () => new BoardSwapActionResolver(
                    dependencies.MoveAnalyzer,
                    dependencies.CascadeResolver,
                    null));
        }

        private static void AssertNotSwappable(
            BoardState board,
            BoardSwap swap)
        {
            BoardBlock[] originalContents = CaptureBoardContents(board);
            var random = new RecordingRandomSource(Array.Empty<int>());
            var idGenerator = new BoardBlockIdGenerator(100);

            BoardSwapActionResult result = CreateResolver(
                random,
                idGenerator).Resolve(board, swap);

            Assert.That(
                result.Status,
                Is.EqualTo(BoardSwapActionStatus.NotSwappable));
            Assert.That(result.SwappedBoard, Is.Null);
            Assert.That(result.Cascade, Is.Null);
            Assert.That(result.Shuffle, Is.Null);
            Assert.That(result.ConsumesTurn, Is.False);
            Assert.That(result.RequiresSwapBack, Is.False);
            Assert.That(result.WasShuffled, Is.False);
            Assert.That(result.Board, Is.Not.SameAs(board));
            AssertBoardContainsReferences(result.Board, originalContents);
            AssertBoardContainsReferences(board, originalContents);
            Assert.That(random.CallCount, Is.Zero);
            Assert.That(idGenerator.Next(), Is.EqualTo(100));
        }

        private static void AssertResolved(BoardSwapActionResult result)
        {
            Assert.That(
                result.Status,
                Is.EqualTo(BoardSwapActionStatus.Resolved));
            Assert.That(result.SwappedBoard, Is.Not.Null);
            Assert.That(result.Cascade, Is.Not.Null);
            Assert.That(result.Shuffle, Is.Not.Null);
            Assert.That(result.Board, Is.SameAs(result.Shuffle.Board));
            Assert.That(result.ConsumesTurn, Is.True);
            Assert.That(result.RequiresSwapBack, Is.False);
            Assert.That(result.Cascade.CascadeCount, Is.GreaterThan(0));
            Assert.That(result.Cascade.ComboCount, Is.GreaterThan(0));
            Assert.That(BoardMatchFinder.FindMatches(result.Board), Is.Empty);
            Assert.That(
                new BoardMoveAnalyzer().HasAnyValidSwap(result.Board),
                Is.True);
        }

        private static BoardSwapActionResolver CreateResolver(
            IRandomSource randomSource,
            int firstId,
            int maxPermutationAttempts =
                BoardShuffler.DefaultMaxPermutationAttempts,
            int maxRegenerationAttempts =
                BoardShuffler.DefaultMaxRegenerationAttempts)
        {
            return CreateResolver(
                randomSource,
                new BoardBlockIdGenerator(firstId),
                maxPermutationAttempts,
                maxRegenerationAttempts);
        }

        private static BoardSwapActionResolver CreateResolver(
            IRandomSource randomSource,
            BoardBlockIdGenerator idGenerator,
            int maxPermutationAttempts =
                BoardShuffler.DefaultMaxPermutationAttempts,
            int maxRegenerationAttempts =
                BoardShuffler.DefaultMaxRegenerationAttempts)
        {
            ResolverDependencies dependencies = CreateDependencies(
                randomSource,
                idGenerator,
                maxPermutationAttempts,
                maxRegenerationAttempts);
            return new BoardSwapActionResolver(
                dependencies.MoveAnalyzer,
                dependencies.CascadeResolver,
                dependencies.Shuffler);
        }

        private static ResolverDependencies CreateDependencies(
            IRandomSource randomSource,
            BoardBlockIdGenerator idGenerator,
            int maxPermutationAttempts =
                BoardShuffler.DefaultMaxPermutationAttempts,
            int maxRegenerationAttempts =
                BoardShuffler.DefaultMaxRegenerationAttempts)
        {
            var moveAnalyzer = new BoardMoveAnalyzer();
            var refiller = new BoardRefiller(randomSource, idGenerator);
            var stepResolver = new BoardCascadeStepResolver(refiller);
            var cascadeResolver = new BoardCascadeResolver(stepResolver);
            var shuffler = new BoardShuffler(
                randomSource,
                moveAnalyzer,
                maxPermutationAttempts,
                maxRegenerationAttempts);
            return new ResolverDependencies(
                moveAnalyzer,
                cascadeResolver,
                shuffler);
        }

        private static BoardState CreateSwapToTargetBoard(int[] targetElements)
        {
            BoardState board = CreateBoard(targetElements);
            SetElement(board, 0, 4, ElementType.Grass);
            SetElement(board, 1, 4, ElementType.Grass);
            SetElement(board, 2, 4, ElementType.Dark);
            SetElement(board, 3, 4, ElementType.Grass);

            Assert.That(BoardMatchFinder.FindMatches(board), Is.Empty);
            Assert.That(
                new BoardMoveAnalyzer().IsValidSwap(
                    board,
                    new BoardPosition(2, 4),
                    new BoardPosition(3, 4)),
                Is.True);
            return board;
        }

        private static BoardSwap CreateTopHorizontalMatchSwap()
        {
            return new BoardSwap(
                new BoardPosition(2, 4),
                new BoardPosition(3, 4));
        }

        private static BoardState CreateTwoStepSwapBoard()
        {
            var elements = new int[BoardConstants.CellCount];
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    elements[(x * BoardConstants.Height) + y] = (x + y) % 5;
                }
            }

            BoardState board = CreateBoard(elements);
            SetElement(board, 0, 0, ElementType.Dark);
            SetElement(board, 1, 0, ElementType.Dark);
            SetElement(board, 2, 0, ElementType.Fire);
            SetElement(board, 3, 0, ElementType.Dark);
            Assert.That(BoardMatchFinder.FindMatches(board), Is.Empty);
            Assert.That(
                new BoardMoveAnalyzer().IsValidSwap(
                    board,
                    new BoardPosition(2, 0),
                    new BoardPosition(3, 0)),
                Is.True);
            return board;
        }

        private static BoardState CreateBoard(int[] elements)
        {
            var board = new BoardState();
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    var position = new BoardPosition(x, y);
                    board.Set(
                        position,
                        new BoardBlock(
                            position.ToIndex() + 1,
                            BoardBlockType.Normal,
                            (ElementType)elements[
                                (x * BoardConstants.Height) + y]));
                }
            }

            return board;
        }

        private static void SetElement(
            BoardState board,
            int x,
            int y,
            ElementType element)
        {
            var position = new BoardPosition(x, y);
            long runtimeId = board.Get(position).RuntimeId;
            board.Set(
                position,
                new BoardBlock(runtimeId, BoardBlockType.Normal, element));
        }

        private static int[] CreateIdentityPermutationSequence(int count)
        {
            var sequence = new int[Math.Max(0, count - 1)];
            for (int index = count - 1; index >= 1; index--)
            {
                sequence[count - 1 - index] = index;
            }

            return sequence;
        }

        private static int[] Combine(params int[][] sequences)
        {
            int totalCount = 0;
            foreach (int[] sequence in sequences)
            {
                totalCount += sequence.Length;
            }

            var combined = new int[totalCount];
            int writeIndex = 0;
            foreach (int[] sequence in sequences)
            {
                Array.Copy(
                    sequence,
                    0,
                    combined,
                    writeIndex,
                    sequence.Length);
                writeIndex += sequence.Length;
            }

            return combined;
        }

        private static BoardBlock[] CaptureBoardContents(BoardState board)
        {
            var contents = new BoardBlock[BoardConstants.CellCount];
            for (int index = 0; index < contents.Length; index++)
            {
                contents[index] = board.Get(BoardPosition.FromIndex(index));
            }

            return contents;
        }

        private static void AssertBoardContainsReferences(
            BoardState board,
            BoardBlock[] expected)
        {
            for (int index = 0; index < expected.Length; index++)
            {
                Assert.That(
                    board.Get(BoardPosition.FromIndex(index)),
                    Is.SameAs(expected[index]));
            }
        }

        private static void AssertBoardsHaveSameContents(
            BoardState expected,
            BoardState actual)
        {
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                Assert.That(actual.Get(position), Is.SameAs(expected.Get(position)));
            }
        }

        private static void AssertBoardsHaveElements(
            BoardState board,
            int[] expectedElements)
        {
            for (int x = 0; x < BoardConstants.Width; x++)
            {
                for (int y = 0; y < BoardConstants.Height; y++)
                {
                    Assert.That(
                        board.Get(new BoardPosition(x, y)).Element,
                        Is.EqualTo(
                            (ElementType)expectedElements[
                                (x * BoardConstants.Height) + y]));
                }
            }
        }

        private static void AssertBoardsHaveEquivalentData(
            BoardState expected,
            BoardState actual)
        {
            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                BoardBlock expectedBlock = expected.Get(position);
                BoardBlock actualBlock = actual.Get(position);

                Assert.That(
                    actualBlock.RuntimeId,
                    Is.EqualTo(expectedBlock.RuntimeId));
                Assert.That(
                    actualBlock.BlockType,
                    Is.EqualTo(expectedBlock.BlockType));
                Assert.That(actualBlock.Element, Is.EqualTo(expectedBlock.Element));
            }
        }

        private sealed class ResolverDependencies
        {
            public ResolverDependencies(
                BoardMoveAnalyzer moveAnalyzer,
                BoardCascadeResolver cascadeResolver,
                BoardShuffler shuffler)
            {
                MoveAnalyzer = moveAnalyzer;
                CascadeResolver = cascadeResolver;
                Shuffler = shuffler;
            }

            public BoardMoveAnalyzer MoveAnalyzer { get; }
            public BoardCascadeResolver CascadeResolver { get; }
            public BoardShuffler Shuffler { get; }
        }

        private sealed class RecordingRandomSource : IRandomSource
        {
            private readonly Queue<int> values;

            public RecordingRandomSource(IEnumerable<int> values)
            {
                this.values = new Queue<int>(values);
            }

            public int CallCount { get; private set; }

            public int Next(int minInclusive, int maxExclusive)
            {
                CallCount++;

                if (values.Count == 0)
                {
                    throw new InvalidOperationException(
                        "No recorded random value remains.");
                }

                int value = values.Dequeue();
                if (value < minInclusive || value >= maxExclusive)
                {
                    throw new InvalidOperationException(
                        $"Recorded random value {value} is outside the requested range.");
                }

                return value;
            }

            public float NextFloat()
            {
                throw new NotSupportedException();
            }
        }
    }
}
