using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Board
{
    public sealed class BoardGeneratorTests
    {
        [Test]
        public void Generate_FillsEveryCellWithElementalNormalBlock()
        {
            BoardState board = CreateGenerator(12345).Generate();

            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardBlock block = board.Get(BoardPosition.FromIndex(index));

                Assert.That(block, Is.Not.Null);
                Assert.That(block.BlockType, Is.EqualTo(BoardBlockType.Normal));
                Assert.That(block.Element.HasValue, Is.True);
                Assert.That(IsInitialElement(block.Element.Value), Is.True);
            }
        }

        [Test]
        public void Generate_DefaultIdGenerator_AssignsPositiveUniqueIdsOneToThirty()
        {
            BoardState board = CreateGenerator(54321).Generate();
            var ids = new HashSet<long>();

            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                long runtimeId =
                    board.Get(BoardPosition.FromIndex(index)).RuntimeId;

                Assert.That(runtimeId, Is.EqualTo(index + 1));
                Assert.That(runtimeId, Is.GreaterThan(0));
                Assert.That(ids.Add(runtimeId), Is.True);
            }

            Assert.That(ids, Has.Count.EqualTo(BoardConstants.CellCount));
        }

        [Test]
        public void Generate_ResultHasNoMatchesAndAtLeastOneValidSwap()
        {
            BoardState board = CreateGenerator(9876).Generate();
            var analyzer = new BoardMoveAnalyzer();

            Assert.That(BoardMatchFinder.FindMatches(board), Is.Empty);
            Assert.That(analyzer.HasAnyValidSwap(board), Is.True);
            Assert.That(analyzer.FindValidSwaps(board), Is.Not.Empty);
        }

        [Test]
        public void Generate_SameSeedAndFreshIdGenerators_ProducesSameBoard()
        {
            BoardState first = CreateGenerator(202603).Generate();
            BoardState second = CreateGenerator(202603).Generate();

            for (int index = 0; index < BoardConstants.CellCount; index++)
            {
                BoardPosition position = BoardPosition.FromIndex(index);
                BoardBlock firstBlock = first.Get(position);
                BoardBlock secondBlock = second.Get(position);

                Assert.That(firstBlock.Element, Is.EqualTo(secondBlock.Element));
                Assert.That(
                    firstBlock.RuntimeId,
                    Is.EqualTo(secondBlock.RuntimeId));
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(42)]
        [TestCase(1000)]
        [TestCase(123456789)]
        [TestCase(int.MaxValue)]
        public void Generate_MultipleSeedsAlwaysSatisfyPuzzleConditions(int seed)
        {
            BoardState board = CreateGenerator(seed).Generate();
            var analyzer = new BoardMoveAnalyzer();

            Assert.That(BoardMatchFinder.FindMatches(board), Is.Empty);
            Assert.That(analyzer.HasAnyValidSwap(board), Is.True);
        }

        [Test]
        public void Generate_SeparateBoardsHaveIndependentCellStorage()
        {
            BoardState first = CreateGenerator(777).Generate();
            BoardState second = CreateGenerator(777).Generate();
            var position = new BoardPosition(0, 0);
            BoardBlock secondBlock = second.Get(position);

            Assert.That(first.Get(position), Is.Not.SameAs(secondBlock));

            first.Clear(position);

            Assert.That(first.Get(position), Is.Null);
            Assert.That(second.Get(position), Is.SameAs(secondBlock));
        }

        [Test]
        public void Generate_SharedIdGeneratorContinuesAcrossBoards()
        {
            var idGenerator = new BoardBlockIdGenerator();
            BoardState first = CreateGenerator(10, idGenerator).Generate();
            BoardState second = CreateGenerator(20, idGenerator).Generate();

            Assert.That(
                first.Get(new BoardPosition(0, 0)).RuntimeId,
                Is.EqualTo(1));
            Assert.That(
                first.Get(new BoardPosition(5, 4)).RuntimeId,
                Is.EqualTo(30));
            Assert.That(
                second.Get(new BoardPosition(0, 0)).RuntimeId,
                Is.EqualTo(31));
            Assert.That(
                second.Get(new BoardPosition(5, 4)).RuntimeId,
                Is.EqualTo(60));
        }

        [Test]
        public void Generate_RejectedCandidateDoesNotConsumeRuntimeIds()
        {
            int validationCount = 0;
            Func<BoardState, IReadOnlyList<BoardMatch>> matchFinder = board =>
            {
                validationCount++;
                return validationCount == 1
                    ? CreateKnownMatch()
                    : BoardMatchFinder.FindMatches(board);
            };
            var idGenerator = new BoardBlockIdGenerator();
            var generator = new BoardGenerator(
                new SeededRandomSource(31415),
                matchFinder,
                new BoardMoveAnalyzer(),
                idGenerator);

            BoardState board = generator.Generate();

            Assert.That(validationCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(
                board.Get(new BoardPosition(0, 0)).RuntimeId,
                Is.EqualTo(1));
            Assert.That(
                board.Get(new BoardPosition(5, 4)).RuntimeId,
                Is.EqualTo(30));
        }

        [Test]
        public void Generate_WhenAllAttemptsFail_ThrowsWithoutConsumingIds()
        {
            var idGenerator = new BoardBlockIdGenerator();
            var generator = new BoardGenerator(
                new SeededRandomSource(1),
                board => CreateKnownMatch(),
                new BoardMoveAnalyzer(),
                idGenerator,
                2);

            Assert.Throws<InvalidOperationException>(() => generator.Generate());
            Assert.That(idGenerator.Next(), Is.EqualTo(1));
        }

        [Test]
        public void Constructor_NullRandomSource_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => new BoardGenerator(
                    null,
                    BoardMatchFinder.FindMatches,
                    new BoardMoveAnalyzer(),
                    new BoardBlockIdGenerator()));
        }

        [Test]
        public void Constructor_NullMatchFinder_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => new BoardGenerator(
                    new SeededRandomSource(1),
                    null,
                    new BoardMoveAnalyzer(),
                    new BoardBlockIdGenerator()));
        }

        [Test]
        public void Constructor_NullMoveAnalyzer_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => new BoardGenerator(
                    new SeededRandomSource(1),
                    BoardMatchFinder.FindMatches,
                    null,
                    new BoardBlockIdGenerator()));
        }

        [Test]
        public void Constructor_NullIdGenerator_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => new BoardGenerator(
                    new SeededRandomSource(1),
                    BoardMatchFinder.FindMatches,
                    new BoardMoveAnalyzer(),
                    null));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_NonPositiveMaxAttempts_Throws(int maxAttempts)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BoardGenerator(
                    new SeededRandomSource(1),
                    BoardMatchFinder.FindMatches,
                    new BoardMoveAnalyzer(),
                    new BoardBlockIdGenerator(),
                    maxAttempts));
        }

        private static BoardGenerator CreateGenerator(
            int seed,
            BoardBlockIdGenerator idGenerator = null)
        {
            return new BoardGenerator(
                new SeededRandomSource(seed),
                BoardMatchFinder.FindMatches,
                new BoardMoveAnalyzer(),
                idGenerator ?? new BoardBlockIdGenerator());
        }

        private static bool IsInitialElement(ElementType element)
        {
            return element == ElementType.Fire
                || element == ElementType.Water
                || element == ElementType.Grass
                || element == ElementType.Light
                || element == ElementType.Dark;
        }

        private static IReadOnlyList<BoardMatch> CreateKnownMatch()
        {
            var board = new BoardState();
            board.Set(
                new BoardPosition(0, 0),
                new BoardBlock(1, BoardBlockType.Normal, ElementType.Fire));
            board.Set(
                new BoardPosition(1, 0),
                new BoardBlock(2, BoardBlockType.Normal, ElementType.Fire));
            board.Set(
                new BoardPosition(2, 0),
                new BoardBlock(3, BoardBlockType.Normal, ElementType.Fire));
            return BoardMatchFinder.FindMatches(board);
        }
    }
}
