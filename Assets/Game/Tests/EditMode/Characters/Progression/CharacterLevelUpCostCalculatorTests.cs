using System;
using NUnit.Framework;
using ValorChronicle.Characters.Progression;

namespace ValorChronicle.Tests.EditMode.Characters.Progression
{
    public sealed class CharacterLevelUpCostCalculatorTests
    {
        [TestCase(1, 120L)]
        [TestCase(99, 2080L)]
        public void GetNextLevelCostUsesCurrentLevel(
            int currentLevel,
            long expectedCost)
        {
            long result = CharacterLevelUpCostCalculator
                .GetNextLevelCost(currentLevel);

            Assert.That(result, Is.EqualTo(expectedCost));
        }

        [TestCase(1, 100, 108900L)]
        [TestCase(99, 100, 2080L)]
        [TestCase(10, 15, 1700L)]
        public void GetTotalCostSumsEveryLevelBeforeTarget(
            int currentLevel,
            int targetLevel,
            long expectedCost)
        {
            long result = CharacterLevelUpCostCalculator.GetTotalCost(
                currentLevel,
                targetLevel);

            Assert.That(result, Is.EqualTo(expectedCost));
        }

        [TestCase(99, false)]
        [TestCase(100, true)]
        public void IsMaximumLevelIdentifiesOnlyLevelOneHundred(
            int level,
            bool expected)
        {
            Assert.That(
                CharacterLevelUpCostCalculator.IsMaximumLevel(level),
                Is.EqualTo(expected));
        }

        [TestCase(20, 20)]
        [TestCase(20, 19)]
        public void GetTotalCostRejectsNonIncreasingTarget(
            int currentLevel,
            int targetLevel)
        {
            Assert.That(
                () => CharacterLevelUpCostCalculator.GetTotalCost(
                    currentLevel,
                    targetLevel),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName")
                    .EqualTo("targetLevel"));
        }

        [TestCase(0)]
        [TestCase(101)]
        public void GetNextLevelCostRejectsLevelOutsideRange(int level)
        {
            Assert.That(
                () => CharacterLevelUpCostCalculator
                    .GetNextLevelCost(level),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [TestCase(0)]
        [TestCase(101)]
        public void IsMaximumLevelRejectsLevelOutsideRange(int level)
        {
            Assert.That(
                () => CharacterLevelUpCostCalculator
                    .IsMaximumLevel(level),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [TestCase(0, 2, "currentLevel")]
        [TestCase(101, 100, "currentLevel")]
        [TestCase(1, 0, "targetLevel")]
        [TestCase(1, 101, "targetLevel")]
        public void GetTotalCostRejectsLevelOutsideRange(
            int currentLevel,
            int targetLevel,
            string expectedParameterName)
        {
            Assert.That(
                () => CharacterLevelUpCostCalculator.GetTotalCost(
                    currentLevel,
                    targetLevel),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName")
                    .EqualTo(expectedParameterName));
        }

        [Test]
        public void GetNextLevelCostRejectsMaximumLevel()
        {
            Assert.That(
                () => CharacterLevelUpCostCalculator
                    .GetNextLevelCost(100),
                Throws.TypeOf<InvalidOperationException>());
        }

        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void ExtremeLevelsAreRejectedBeforeCostArithmetic(int level)
        {
            Assert.That(
                () => CharacterLevelUpCostCalculator
                    .GetNextLevelCost(level),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
