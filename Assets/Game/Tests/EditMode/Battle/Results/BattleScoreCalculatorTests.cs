using System;
using NUnit.Framework;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Results;

namespace ValorChronicle.Tests.EditMode.Battle.Results
{
    public sealed class BattleScoreCalculatorTests
    {
        [TestCase(1, 24)]
        [TestCase(10, 15)]
        [TestCase(25, 0)]
        public void Calculate_VictoryCalculatesRemainingTurns(
            int finishedTurn,
            int expectedRemainingTurns)
        {
            BattleScoreResult result = Calculate(
                BattleResultKind.Victory,
                bossMaxHp: 100000L,
                turnLimit: 25,
                finishedTurn: finishedTurn,
                damageScore: 100000L);

            Assert.That(
                result.RemainingTurns,
                Is.EqualTo(expectedRemainingTurns));
            Assert.That(result.BossDefeated, Is.True);
        }

        [TestCase(BattleResultKind.Defeat, 10)]
        [TestCase(BattleResultKind.TurnLimitReached, 25)]
        public void Calculate_NonVictoryHasNoRemainingTurnsOrBonus(
            BattleResultKind endReason,
            int finishedTurn)
        {
            BattleScoreResult result = Calculate(
                endReason,
                bossMaxHp: 100000L,
                turnLimit: 25,
                finishedTurn: finishedTurn,
                damageScore: 45000L);

            Assert.That(result.RemainingTurns, Is.Zero);
            Assert.That(result.RemainingTurnBonus, Is.Zero);
            Assert.That(result.FinalScore, Is.EqualTo(45000L));
            Assert.That(result.BossDefeated, Is.False);
        }

        [Test]
        public void Calculate_RepresentativeVictoryMatchesConfirmedFormula()
        {
            BattleScoreResult result = Calculate(
                BattleResultKind.Victory,
                bossMaxHp: 100000L,
                turnLimit: 25,
                finishedTurn: 10,
                damageScore: 100000L);

            Assert.That(result.EndReason, Is.EqualTo(BattleResultKind.Victory));
            Assert.That(result.FinishedTurn, Is.EqualTo(10));
            Assert.That(result.RemainingTurns, Is.EqualTo(15));
            Assert.That(result.DamageScore, Is.EqualTo(100000L));
            Assert.That(result.RemainingTurnBonus, Is.EqualTo(18000L));
            Assert.That(result.FinalScore, Is.EqualTo(118000L));
        }

        [Test]
        public void Calculate_FloorsOnlyTheFinalFractionalBonus()
        {
            BattleScoreResult result = Calculate(
                BattleResultKind.Victory,
                bossMaxHp: 101L,
                turnLimit: 3,
                finishedTurn: 2,
                damageScore: 80L);

            Assert.That(result.RemainingTurns, Is.EqualTo(1));
            Assert.That(result.RemainingTurnBonus, Is.EqualTo(10L));
            Assert.That(result.FinalScore, Is.EqualTo(90L));
        }

        [Test]
        public void Calculate_DoesNotFloorBeforeApplyingBonusPercent()
        {
            BattleScoreResult result = Calculate(
                BattleResultKind.Victory,
                bossMaxHp: 100L,
                turnLimit: 3,
                finishedTurn: 1,
                damageScore: 80L);

            Assert.That(result.RemainingTurns, Is.EqualTo(2));
            Assert.That(result.RemainingTurnBonus, Is.EqualTo(20L));
            Assert.That(result.FinalScore, Is.EqualTo(100L));
        }

        [Test]
        public void Calculate_RejectsNullInput()
        {
            Assert.Throws<ArgumentNullException>(
                () => BattleScoreCalculator.Calculate(null));
        }

        [TestCase(0L)]
        [TestCase(-1L)]
        public void Calculate_RejectsNonPositiveBossMaxHp(long bossMaxHp)
        {
            AssertInvalid(
                CreateInput(bossMaxHp: bossMaxHp),
                typeof(ArgumentOutOfRangeException));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Calculate_RejectsNonPositiveTurnLimit(int turnLimit)
        {
            AssertInvalid(
                CreateInput(turnLimit: turnLimit),
                typeof(ArgumentOutOfRangeException));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(26)]
        public void Calculate_RejectsFinishedTurnOutsideLimit(int finishedTurn)
        {
            AssertInvalid(
                CreateInput(finishedTurn: finishedTurn),
                typeof(ArgumentOutOfRangeException));
        }

        [Test]
        public void Calculate_RejectsEarlyTurnLimitResult()
        {
            AssertInvalid(
                CreateInput(
                    endReason: BattleResultKind.TurnLimitReached,
                    finishedTurn: 24),
                typeof(ArgumentException));
        }

        [TestCase(BattleResultKind.None)]
        [TestCase(BattleResultKind.Aborted)]
        [TestCase((BattleResultKind)999)]
        public void Calculate_RejectsUnsupportedEndReason(
            BattleResultKind endReason)
        {
            AssertInvalid(
                CreateInput(endReason: endReason),
                typeof(ArgumentOutOfRangeException));
        }

        [Test]
        public void Calculate_RejectsNegativeDamageScore()
        {
            AssertInvalid(
                CreateInput(damageScore: -1L),
                typeof(ArgumentOutOfRangeException));
        }

        [Test]
        public void Calculate_RejectsDamageScoreAboveBossMaxHp()
        {
            AssertInvalid(
                CreateInput(bossMaxHp: 100L, damageScore: 101L),
                typeof(ArgumentOutOfRangeException));
        }

        [TestCase("0")]
        [TestCase("-0.01")]
        [TestCase("1.01")]
        public void Calculate_RejectsInvalidBonusPercent(string percentText)
        {
            decimal percent = decimal.Parse(
                percentText,
                System.Globalization.CultureInfo.InvariantCulture);

            AssertInvalid(
                CreateInput(remainingTurnBonusPercent: percent),
                typeof(ArgumentOutOfRangeException));
        }

        [Test]
        public void Calculate_ThrowsWhenFinalScoreOverflows()
        {
            BattleScoreInput input = CreateInput(
                bossMaxHp: long.MaxValue,
                turnLimit: 2,
                finishedTurn: 1,
                damageScore: long.MaxValue);

            Assert.Throws<OverflowException>(
                () => BattleScoreCalculator.Calculate(input));
        }

        private static BattleScoreResult Calculate(
            BattleResultKind endReason,
            long bossMaxHp,
            int turnLimit,
            int finishedTurn,
            long damageScore)
        {
            return BattleScoreCalculator.Calculate(
                new BattleScoreInput(
                    endReason,
                    bossMaxHp,
                    turnLimit,
                    finishedTurn,
                    damageScore));
        }

        private static BattleScoreInput CreateInput(
            BattleResultKind endReason = BattleResultKind.Victory,
            long bossMaxHp = 100L,
            int turnLimit = 25,
            int finishedTurn = 10,
            long damageScore = 100L,
            decimal remainingTurnBonusPercent =
                BattleScoreCalculator.DefaultRemainingTurnBonusPercent)
        {
            return new BattleScoreInput(
                endReason,
                bossMaxHp,
                turnLimit,
                finishedTurn,
                damageScore,
                remainingTurnBonusPercent);
        }

        private static void AssertInvalid(
            BattleScoreInput input,
            Type expectedExceptionType)
        {
            Assert.Throws(
                expectedExceptionType,
                () => BattleScoreCalculator.Calculate(input));
        }
    }
}
