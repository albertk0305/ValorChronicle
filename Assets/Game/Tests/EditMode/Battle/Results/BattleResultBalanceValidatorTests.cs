using System;
using System.Linq;
using NUnit.Framework;
using ValorChronicle.Battle.Results;

namespace ValorChronicle.Tests.EditMode.Battle.Results
{
    public sealed class BattleResultBalanceValidatorTests
    {
        [Test]
        public void DefaultBalance_IsValidAndMatchesScoreBonusRate()
        {
            BattleResultBalance balance = DefaultBalance();

            Assert.That(
                BattleResultBalanceValidator.TryValidate(
                    balance,
                    out string error),
                Is.True,
                error);
            Assert.That(
                balance.RemainingTurnBonusRate,
                Is.EqualTo(BattleScoreCalculator
                    .DefaultRemainingTurnBonusPercent));
        }

        [Test]
        public void TryValidate_RejectsDuplicateGradeId()
        {
            BattleGradeThresholdEntry[] thresholds = DefaultThresholds();
            thresholds[1] = new BattleGradeThresholdEntry(
                BattleGrade.B,
                BattleGradeIds.C,
                40);

            AssertInvalid(Create(thresholds: thresholds));
        }

        [Test]
        public void TryValidate_RejectsEmptyGradeId()
        {
            BattleGradeThresholdEntry[] thresholds = DefaultThresholds();
            thresholds[0] = new BattleGradeThresholdEntry(
                BattleGrade.C,
                string.Empty,
                20);

            AssertInvalid(Create(thresholds: thresholds));
        }

        [TestCase(20)]
        [TestCase(10)]
        public void TryValidate_RejectsDuplicateOrReversedThreshold(
            int bThreshold)
        {
            BattleGradeThresholdEntry[] thresholds = DefaultThresholds();
            thresholds[1] = new BattleGradeThresholdEntry(
                BattleGrade.B,
                BattleGradeIds.B,
                bThreshold);

            AssertInvalid(Create(thresholds: thresholds));
        }

        [Test]
        public void TryValidate_RejectsMissingGradeThreshold()
        {
            BattleGradeThresholdEntry[] thresholds = DefaultThresholds()
                .Take(5)
                .ToArray();

            AssertInvalid(Create(thresholds: thresholds));
        }

        [Test]
        public void TryValidate_RejectsNegativeFirstGradeReward()
        {
            BattleGradeRewardEntry[] rewards = DefaultFirstRewards();
            rewards[0] = new BattleGradeRewardEntry(BattleGrade.C, -1L);

            AssertInvalid(Create(firstRewards: rewards));
        }

        [Test]
        public void TryValidate_RejectsNegativeRepeatReward()
        {
            BattleDifficultyRepeatRewardTable[] tables = DefaultTables();
            BattleGradeRewardEntry[] rewards = tables[0].Rewards.ToArray();
            rewards[0] = new BattleGradeRewardEntry(
                BattleGrade.BelowC,
                -1L);
            tables[0] = new BattleDifficultyRepeatRewardTable(
                BattleDifficultyIds.Intro,
                rewards);

            AssertInvalid(Create(tables: tables));
        }

        [Test]
        public void TryValidate_RejectsDuplicateDifficulty()
        {
            BattleDifficultyRepeatRewardTable[] defaults = DefaultTables();
            var tables = new BattleDifficultyRepeatRewardTable[
                defaults.Length + 1];
            Array.Copy(defaults, tables, defaults.Length);
            tables[tables.Length - 1] = defaults[0];

            AssertInvalid(Create(tables: tables));
        }

        [Test]
        public void TryValidate_RejectsMissingDifficulty()
        {
            BattleDifficultyRepeatRewardTable[] tables = DefaultTables()
                .Where(table => table.DifficultyId
                    != BattleDifficultyIds.Challenge)
                .ToArray();

            AssertInvalid(Create(tables: tables));
        }

        [Test]
        public void TryValidate_RejectsMissingRepeatRewardEntry()
        {
            BattleDifficultyRepeatRewardTable[] tables = DefaultTables();
            BattleGradeRewardEntry[] rewards = tables[4].Rewards
                .Take(6)
                .ToArray();
            tables[4] = new BattleDifficultyRepeatRewardTable(
                BattleDifficultyIds.Challenge,
                rewards);

            AssertInvalid(Create(tables: tables));
        }

        [TestCase("0.29")]
        [TestCase("0.31")]
        [TestCase("0")]
        public void TryValidate_RejectsIncompatibleBonusRate(string rateText)
        {
            decimal rate = decimal.Parse(
                rateText,
                System.Globalization.CultureInfo.InvariantCulture);

            AssertInvalid(Create(bonusRate: rate));
        }

        private static BattleResultBalance Create(
            decimal bonusRate =
                BattleScoreCalculator.DefaultRemainingTurnBonusPercent,
            BattleGradeThresholdEntry[] thresholds = null,
            BattleGradeRewardEntry[] firstRewards = null,
            BattleDifficultyRepeatRewardTable[] tables = null)
        {
            return new BattleResultBalance(
                bonusRate,
                thresholds ?? DefaultThresholds(),
                firstRewards ?? DefaultFirstRewards(),
                tables ?? DefaultTables());
        }

        private static BattleResultBalance DefaultBalance()
        {
            return BattleResultBalanceDefaults.Create();
        }

        private static BattleGradeThresholdEntry[] DefaultThresholds()
        {
            return BattleResultBalanceDefaults.CreateGradeThresholds();
        }

        private static BattleGradeRewardEntry[] DefaultFirstRewards()
        {
            return BattleResultBalanceDefaults.CreateFirstGradeRewards();
        }

        private static BattleDifficultyRepeatRewardTable[] DefaultTables()
        {
            return BattleResultBalanceDefaults
                .CreateDifficultyRepeatRewards();
        }

        private static void AssertInvalid(BattleResultBalance balance)
        {
            Assert.That(
                BattleResultBalanceValidator.TryValidate(
                    balance,
                    out string error),
                Is.False);
            Assert.That(error, Is.Not.Empty);
        }
    }
}
