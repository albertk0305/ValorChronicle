using System.Linq;
using NUnit.Framework;
using ValorChronicle.Battle.Results;

namespace ValorChronicle.Tests.EditMode.Battle.Results
{
    public sealed class BattleGradeRewardCalculatorTests
    {
        [TestCase(19999L, BattleGrade.BelowC)]
        [TestCase(20000L, BattleGrade.C)]
        [TestCase(39999L, BattleGrade.C)]
        [TestCase(40000L, BattleGrade.B)]
        [TestCase(60000L, BattleGrade.A)]
        [TestCase(80000L, BattleGrade.S)]
        [TestCase(99999L, BattleGrade.S)]
        [TestCase(100000L, BattleGrade.SS)]
        [TestCase(114999L, BattleGrade.SS)]
        [TestCase(115000L, BattleGrade.SSS)]
        [TestCase(130000L, BattleGrade.SSS)]
        public void Calculate_UsesExactGradeBoundaries(
            long finalScore,
            BattleGrade expectedGrade)
        {
            BattleGradeRewardResult result = Calculate(
                BattleDifficultyIds.Normal,
                bossMaxHp: 100000L,
                finalScore: finalScore);

            Assert.That(result.Grade, Is.EqualTo(expectedGrade));
        }

        [Test]
        public void Calculate_BelowCHasNoRewardGradeIdOrProgression()
        {
            BattleGradeRewardResult result = Calculate(
                BattleDifficultyIds.Challenge,
                bossMaxHp: 100000L,
                finalScore: 19999L);

            Assert.That(result.Grade, Is.EqualTo(BattleGrade.BelowC));
            Assert.That(result.GradeId, Is.Empty);
            Assert.That(result.RepeatRewardAmount, Is.EqualTo(20L));
            Assert.That(result.FirstGradeProgression, Is.Empty);
        }

        [TestCase(
            BattleDifficultyIds.Challenge,
            115000L,
            1000L)]
        [TestCase(BattleDifficultyIds.Intro, 20000L, 50L)]
        [TestCase(BattleDifficultyIds.Hard, 100000L, 550L)]
        public void Calculate_ResolvesDifficultyRepeatReward(
            string difficultyId,
            long finalScore,
            long expectedReward)
        {
            BattleGradeRewardResult result = Calculate(
                difficultyId,
                bossMaxHp: 100000L,
                finalScore: finalScore);

            Assert.That(
                result.RepeatRewardAmount,
                Is.EqualTo(expectedReward));
        }

        [Test]
        public void GetFirstGradeProgression_SSReturnsCThroughSS()
        {
            var progression = BattleGradeRewardCalculator
                .GetFirstGradeProgression(
                    Balance(),
                    BattleGrade.SS);

            Assert.That(
                progression.Select(entry => entry.Grade),
                Is.EqualTo(new[]
                {
                    BattleGrade.C,
                    BattleGrade.B,
                    BattleGrade.A,
                    BattleGrade.S,
                    BattleGrade.SS
                }));
            Assert.That(
                progression.Select(entry => entry.GradeId),
                Is.EqualTo(new[]
                {
                    BattleGradeIds.C,
                    BattleGradeIds.B,
                    BattleGradeIds.A,
                    BattleGradeIds.S,
                    BattleGradeIds.SS
                }));
        }

        [Test]
        public void GetFirstGradeProgression_SSSReturnsCThroughSSS()
        {
            var progression = BattleGradeRewardCalculator
                .GetFirstGradeProgression(
                    Balance(),
                    BattleGrade.SSS);

            Assert.That(
                progression.Select(entry => entry.Grade),
                Is.EqualTo(BattleGradeRules.ProgressionGrades));
        }

        [TestCase(100000L, 400L)]
        [TestCase(115000L, 700L)]
        public void Calculate_SumsFirstGradeProgressionRewards(
            long finalScore,
            long expectedTotal)
        {
            BattleGradeRewardResult result = Calculate(
                BattleDifficultyIds.Normal,
                bossMaxHp: 100000L,
                finalScore: finalScore);

            Assert.That(
                result.FirstGradeProgressionTotal,
                Is.EqualTo(expectedTotal));
        }

        private static BattleGradeRewardResult Calculate(
            string difficultyId,
            long bossMaxHp,
            long finalScore)
        {
            return BattleGradeRewardCalculator.Calculate(
                Balance(),
                difficultyId,
                bossMaxHp,
                finalScore);
        }

        private static BattleResultBalance Balance()
        {
            return BattleResultBalanceDefaults.Create();
        }
    }
}
