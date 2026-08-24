using System;
using System.Linq;
using NUnit.Framework;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Results;
using ValorChronicle.Battle.Results.Persistence;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode.Battle.Results.Persistence
{
    public sealed class BattleResultProfileUpdaterTests
    {
        [Test]
        public void Apply_NoClaimsAtCGrants20AndStoresOnlyNewClaim()
        {
            ProfileSaveData profile = Profile();

            BattleProfileUpdateResult update = Apply(
                profile,
                Defeat(score: 20000L));

            Assert.That(update.FirstGradeRewardAmount, Is.EqualTo(20L));
            Assert.That(
                update.NewFirstGradeRewardEntries.Select(x => x.GradeId),
                Is.EqualTo(new[] { BattleGradeIds.C }));
            Assert.That(
                Record(profile).ClaimedFirstRewardGradeIds,
                Is.EqualTo(new[] { BattleGradeIds.C }));
        }

        [TestCase(100000L, 400L)]
        [TestCase(115000L, 700L)]
        public void Apply_NoClaimsGrantsFullProgression(
            long finalScore,
            long expectedFirstReward)
        {
            ProfileSaveData profile = Profile();
            BattleFinalResult result = finalScore == 100000L
                ? Victory(finishedTurn: 25)
                : Victory(finishedTurn: 1);

            BattleProfileUpdateResult update = Apply(profile, result);

            Assert.That(
                result.FinalScore >= finalScore,
                Is.True);
            Assert.That(
                update.FirstGradeRewardAmount,
                Is.EqualTo(expectedFirstReward));
        }

        [Test]
        public void Apply_ClaimsCAndBThenSGrantsOnlyAAndS()
        {
            ProfileSaveData profile = Profile(Record(
                claims: new[] { BattleGradeIds.C, BattleGradeIds.B }));

            BattleProfileUpdateResult update = Apply(
                profile,
                Defeat(score: 80000L));

            Assert.That(update.FirstGradeRewardAmount, Is.EqualTo(150L));
            Assert.That(
                update.NewFirstGradeRewardEntries.Select(x => x.GradeId),
                Is.EqualTo(new[]
                {
                    BattleGradeIds.A,
                    BattleGradeIds.S
                }));
            Assert.That(
                Record(profile).ClaimedFirstRewardGradeIds,
                Is.EqualTo(new[]
                {
                    BattleGradeIds.C,
                    BattleGradeIds.B,
                    BattleGradeIds.A,
                    BattleGradeIds.S
                }));
        }

        [Test]
        public void Apply_AllClaimsThroughSsGrantsNoFirstReward()
        {
            ProfileSaveData profile = Profile(Record(claims: new[]
            {
                BattleGradeIds.C,
                BattleGradeIds.B,
                BattleGradeIds.A,
                BattleGradeIds.S,
                BattleGradeIds.SS
            }));

            BattleProfileUpdateResult update = Apply(
                profile,
                Victory(finishedTurn: 25));

            Assert.That(update.FirstGradeRewardAmount, Is.Zero);
            Assert.That(update.NewFirstGradeRewardEntries, Is.Empty);
            Assert.That(
                Record(profile).ClaimedFirstRewardGradeIds,
                Has.Count.EqualTo(5));
        }

        [Test]
        public void Apply_BelowCDoesNotStoreGradeOrFirstClaim()
        {
            ProfileSaveData profile = Profile();

            BattleProfileUpdateResult update = Apply(
                profile,
                Defeat(score: 19999L));

            Assert.That(update.FirstGradeRewardAmount, Is.Zero);
            Assert.That(Record(profile).HighestGradeId, Is.Empty);
            Assert.That(Record(profile).ClaimedFirstRewardGradeIds, Is.Empty);
        }

        [Test]
        public void Apply_ChallengeSssTotalsRepeatAndFirstRewards()
        {
            ProfileSaveData profile = Profile();

            BattleProfileUpdateResult update = Apply(
                profile,
                Victory(finishedTurn: 1));

            Assert.That(update.RepeatRewardAmount, Is.EqualTo(1000L));
            Assert.That(update.FirstGradeRewardAmount, Is.EqualTo(700L));
            Assert.That(update.TotalRewardAmount, Is.EqualTo(1700L));
            Assert.That(profile.Currencies.GachaCurrency, Is.EqualTo(1700L));
        }

        [Test]
        public void Apply_AlreadyClaimedSssStillGrantsRepeatReward()
        {
            ProfileSaveData profile = Profile(Record(claims: AllGradeIds()));

            BattleProfileUpdateResult update = Apply(
                profile,
                Victory(finishedTurn: 1));

            Assert.That(update.FirstGradeRewardAmount, Is.Zero);
            Assert.That(update.TotalRewardAmount, Is.EqualTo(1000L));
            Assert.That(profile.Currencies.GachaCurrency, Is.EqualTo(1000L));
        }

        [TestCase(BattleResultKind.Defeat, 10)]
        [TestCase(BattleResultKind.TurnLimitReached, 25)]
        public void Apply_NonVictoryGrantsRepeatAndMarksAttemptOnly(
            BattleResultKind endReason,
            int finishedTurn)
        {
            ProfileSaveData profile = Profile();
            BattleFinalResult result = Result(
                endReason,
                BattleDifficultyIds.Normal,
                score: 19999L,
                finishedTurn: finishedTurn);

            BattleProfileUpdateResult update = Apply(profile, result);

            Assert.That(update.RepeatRewardAmount, Is.EqualTo(20L));
            Assert.That(update.TotalRewardAmount, Is.EqualTo(20L));
            Assert.That(Record(profile).HasAttempted, Is.True);
            Assert.That(Record(profile).IsCleared, Is.False);
        }

        [Test]
        public void Apply_CreatesAndThenReusesBossDifficultyRecord()
        {
            ProfileSaveData profile = Profile();

            Apply(profile, Defeat(score: 20000L));
            Apply(profile, Defeat(score: 40000L));

            Assert.That(profile.BossRecords, Has.Count.EqualTo(1));
            Assert.That(Record(profile).BossId, Is.EqualTo("boss_test"));
            Assert.That(
                Record(profile).DifficultyId,
                Is.EqualTo(BattleDifficultyIds.Challenge));
            Assert.That(Record(profile).HighScore, Is.EqualTo(40000L));
        }

        [Test]
        public void Apply_DuplicateBossDifficultyRecordsFailExplicitly()
        {
            ProfileSaveData profile = Profile(Record());
            profile.BossRecords.Add(Record());

            Assert.Throws<InvalidOperationException>(
                () => Apply(profile, Defeat(score: 20000L)));
            Assert.That(profile.BossRecords, Has.Count.EqualTo(2));
        }

        [Test]
        public void Apply_ClearRemainsTrueAfterLaterDefeat()
        {
            ProfileSaveData profile = Profile();
            Apply(profile, Victory(finishedTurn: 25));

            Apply(profile, Defeat(score: 20000L));

            Assert.That(Record(profile).IsCleared, Is.True);
        }

        [Test]
        public void Apply_HigherEqualAndLowerScoresUseStrictComparison()
        {
            ProfileSaveData profile = Profile(Record(
                highScore: 40000L,
                highestGradeId: BattleGradeIds.B));

            BattleProfileUpdateResult higher = Apply(
                profile,
                Defeat(score: 60000L));
            BattleProfileUpdateResult equal = Apply(
                profile,
                Defeat(score: 60000L));
            BattleProfileUpdateResult lower = Apply(
                profile,
                Defeat(score: 20000L));

            Assert.That(higher.IsNewHighScore, Is.True);
            Assert.That(higher.SavedHighScore, Is.EqualTo(60000L));
            Assert.That(equal.IsNewHighScore, Is.False);
            Assert.That(lower.IsNewHighScore, Is.False);
            Assert.That(Record(profile).HighScore, Is.EqualTo(60000L));
        }

        [Test]
        public void Apply_OnlyHigherKnownGradeUpdatesHighestGrade()
        {
            ProfileSaveData profile = Profile(Record(
                highestGradeId: BattleGradeIds.B));

            BattleProfileUpdateResult higher = Apply(
                profile,
                Defeat(score: 80000L));
            BattleProfileUpdateResult lower = Apply(
                profile,
                Defeat(score: 20000L));

            Assert.That(higher.IsNewHighestGrade, Is.True);
            Assert.That(higher.PreviousHighestGradeId,
                Is.EqualTo(BattleGradeIds.B));
            Assert.That(higher.SavedHighestGradeId,
                Is.EqualTo(BattleGradeIds.S));
            Assert.That(lower.IsNewHighestGrade, Is.False);
            Assert.That(
                Record(profile).HighestGradeId,
                Is.EqualTo(BattleGradeIds.S));
        }

        [Test]
        public void Apply_UnknownSavedHighestGradeFailsExplicitly()
        {
            ProfileSaveData profile = Profile(Record(
                highestGradeId: "grade_unknown"));

            Assert.Throws<InvalidOperationException>(
                () => Apply(profile, Defeat(score: 20000L)));
        }

        [Test]
        public void Apply_VictoryNewHighUpdatesTurnsButTieDoesNot()
        {
            ProfileSaveData profile = Profile();
            BattleFinalResult victory = Victory(finishedTurn: 10);

            BattleProfileUpdateResult first = Apply(profile, victory);
            int savedTurn = Record(profile).BestDefeatTurn;
            int savedRemaining = Record(profile).BestRemainingTurns;
            BattleProfileUpdateResult tie = Apply(profile, victory);

            Assert.That(first.IsNewHighScore, Is.True);
            Assert.That(savedTurn, Is.EqualTo(10));
            Assert.That(savedRemaining, Is.EqualTo(15));
            Assert.That(tie.IsNewHighScore, Is.False);
            Assert.That(Record(profile).BestDefeatTurn, Is.EqualTo(savedTurn));
            Assert.That(
                Record(profile).BestRemainingTurns,
                Is.EqualTo(savedRemaining));
        }

        [Test]
        public void Apply_FailureNewHighPreservesExistingVictoryTurns()
        {
            ProfileSaveData profile = Profile(Record(
                isCleared: true,
                highScore: 10000L,
                highestGradeId: BattleGradeIds.C,
                bestDefeatTurn: 12,
                bestRemainingTurns: 13));

            BattleProfileUpdateResult update = Apply(
                profile,
                Defeat(score: 80000L));

            Assert.That(update.IsNewHighScore, Is.True);
            Assert.That(Record(profile).BestDefeatTurn, Is.EqualTo(12));
            Assert.That(Record(profile).BestRemainingTurns, Is.EqualTo(13));
        }

        [Test]
        public void Apply_CheckedCurrencyOverflowDoesNotAddRecord()
        {
            ProfileSaveData profile = Profile();
            profile.Currencies.GachaCurrency = long.MaxValue;

            Assert.Throws<OverflowException>(
                () => Apply(profile, Defeat(score: 19999L)));
            Assert.That(profile.BossRecords, Is.Empty);
            Assert.That(
                profile.Currencies.GachaCurrency,
                Is.EqualTo(long.MaxValue));
        }

        private static BattleProfileUpdateResult Apply(
            ProfileSaveData profile,
            BattleFinalResult result)
        {
            return BattleResultProfileUpdater.Apply(profile, result);
        }

        private static ProfileSaveData Profile(
            BossRecordSaveData record = null)
        {
            ProfileSaveData profile = new NewProfileFactory().Create(
                "profile_test",
                10L);
            if (record != null)
            {
                profile.BossRecords.Add(record);
            }

            return profile;
        }

        private static BossRecordSaveData Record(ProfileSaveData profile)
        {
            return profile.BossRecords.Single(record =>
                record.BossId == "boss_test");
        }

        private static BossRecordSaveData Record(
            bool isCleared = false,
            long highScore = 0L,
            string highestGradeId = "",
            int bestDefeatTurn = 0,
            int bestRemainingTurns = 0,
            string[] claims = null)
        {
            return new BossRecordSaveData
            {
                BossId = "boss_test",
                DifficultyId = BattleDifficultyIds.Challenge,
                HasAttempted = true,
                IsCleared = isCleared,
                HighScore = highScore,
                HighestGradeId = highestGradeId,
                BestDefeatTurn = bestDefeatTurn,
                BestRemainingTurns = bestRemainingTurns,
                ClaimedFirstRewardGradeIds =
                    claims?.ToList() ?? new System.Collections.Generic.List<string>()
            };
        }

        private static string[] AllGradeIds()
        {
            return new[]
            {
                BattleGradeIds.C,
                BattleGradeIds.B,
                BattleGradeIds.A,
                BattleGradeIds.S,
                BattleGradeIds.SS,
                BattleGradeIds.SSS
            };
        }

        private static BattleFinalResult Defeat(long score)
        {
            return Result(
                BattleResultKind.Defeat,
                BattleDifficultyIds.Challenge,
                score,
                finishedTurn: 10);
        }

        private static BattleFinalResult Victory(int finishedTurn)
        {
            return Result(
                BattleResultKind.Victory,
                BattleDifficultyIds.Challenge,
                score: 100000L,
                finishedTurn: finishedTurn);
        }

        private static BattleFinalResult Result(
            BattleResultKind endReason,
            string difficultyId,
            long score,
            int finishedTurn)
        {
            return BattleResultFinalizer.Create(
                new BattleFinalizationInput(
                    endReason,
                    "boss_test",
                    difficultyId,
                    bossMaxHp: 100000L,
                    turnLimit: 25,
                    finishedTurn: finishedTurn,
                    damageScore: score,
                    balance: BattleResultBalanceDefaults.Create()));
        }
    }
}
