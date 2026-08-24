using System;
using System.Linq;
using NUnit.Framework;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Results;

namespace ValorChronicle.Tests.EditMode.Battle.Results
{
    public sealed class BattleResultFinalizerTests
    {
        [Test]
        public void Create_VictoryCombinesScoreGradeAndRewards()
        {
            BattleFinalResult result = Create(
                BattleResultKind.Victory,
                BattleDifficultyIds.Challenge,
                bossMaxHp: 100000L,
                turnLimit: 25,
                finishedTurn: 10,
                damageScore: 100000L);

            Assert.That(result.EndReason, Is.EqualTo(BattleResultKind.Victory));
            Assert.That(result.BossDefeated, Is.True);
            Assert.That(result.RemainingTurns, Is.EqualTo(15));
            Assert.That(result.RemainingTurnBonus, Is.EqualTo(18000L));
            Assert.That(result.FinalScore, Is.EqualTo(118000L));
            Assert.That(result.Grade, Is.EqualTo(BattleGrade.SSS));
            Assert.That(result.RepeatRewardAmount, Is.EqualTo(1000L));
        }

        [TestCase(BattleResultKind.Defeat, 10)]
        [TestCase(BattleResultKind.TurnLimitReached, 25)]
        public void Create_NonVictoryStillCalculatesGradeAndRepeatReward(
            BattleResultKind endReason,
            int finishedTurn)
        {
            BattleFinalResult result = Create(
                endReason,
                BattleDifficultyIds.Normal,
                bossMaxHp: 100000L,
                turnLimit: 25,
                finishedTurn: finishedTurn,
                damageScore: 80000L);

            Assert.That(result.BossDefeated, Is.False);
            Assert.That(result.RemainingTurns, Is.Zero);
            Assert.That(result.RemainingTurnBonus, Is.Zero);
            Assert.That(result.FinalScore, Is.EqualTo(80000L));
            Assert.That(result.Grade, Is.EqualTo(BattleGrade.S));
            Assert.That(result.RepeatRewardAmount, Is.EqualTo(230L));
        }

        [Test]
        public void Create_RejectsAbortedResult()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(
                BattleResultKind.Aborted,
                BattleDifficultyIds.Normal,
                100000L,
                25,
                10,
                50000L));
        }

        [Test]
        public void Create_PreservesBossAndDifficultyIds()
        {
            BattleFinalResult result = Create(
                BattleResultKind.Victory,
                BattleDifficultyIds.Hard,
                100000L,
                25,
                25,
                100000L);

            Assert.That(result.BossId, Is.EqualTo("boss_test"));
            Assert.That(
                result.DifficultyId,
                Is.EqualTo(BattleDifficultyIds.Hard));
            Assert.That(result.TurnLimit, Is.EqualTo(25));
        }

        [Test]
        public void Create_SsContainsProgressionThroughSs()
        {
            BattleFinalResult result = Create(
                BattleResultKind.Victory,
                BattleDifficultyIds.Normal,
                100000L,
                25,
                25,
                100000L);

            Assert.That(
                result.FirstGradeProgression.Select(entry => entry.Grade),
                Is.EqualTo(new[]
                {
                    BattleGrade.C,
                    BattleGrade.B,
                    BattleGrade.A,
                    BattleGrade.S,
                    BattleGrade.SS
                }));
        }

        [Test]
        public void Create_ChallengeSssUsesRepeatReward1000()
        {
            BattleFinalResult result = Create(
                BattleResultKind.Victory,
                BattleDifficultyIds.Challenge,
                100000L,
                25,
                1,
                100000L);

            Assert.That(result.Grade, Is.EqualTo(BattleGrade.SSS));
            Assert.That(result.RepeatRewardAmount, Is.EqualTo(1000L));
        }

        private static BattleFinalResult Create(
            BattleResultKind endReason,
            string difficultyId,
            long bossMaxHp,
            int turnLimit,
            int finishedTurn,
            long damageScore)
        {
            return BattleResultFinalizer.Create(
                new BattleFinalizationInput(
                    endReason,
                    "boss_test",
                    difficultyId,
                    bossMaxHp,
                    turnLimit,
                    finishedTurn,
                    damageScore,
                    BattleResultBalanceDefaults.Create()));
        }
    }
}
