using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Battle.Results.Persistence
{
    public static class BattleResultProfileUpdater
    {
        public static BattleProfileUpdateResult Apply(
            ProfileSaveData profile,
            BattleFinalResult battleResult)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            ValidateBattleResult(battleResult);
            if (profile.Currencies == null)
            {
                throw new InvalidOperationException(
                    "Profile currencies must be initialized.");
            }

            if (profile.Currencies.GachaCurrency < 0)
            {
                throw new InvalidOperationException(
                    "Gacha currency cannot be negative.");
            }

            if (profile.BossRecords == null)
            {
                throw new InvalidOperationException(
                    "Profile boss records must be initialized.");
            }

            BossRecordSaveData record = FindRecord(
                profile.BossRecords,
                battleResult.BossId,
                battleResult.DifficultyId);
            bool isNewRecord = record == null;
            if (isNewRecord)
            {
                record = CreateRecord(
                    battleResult.BossId,
                    battleResult.DifficultyId);
            }

            ValidateRecord(record);
            long previousHighScore = record.HighScore;
            string previousHighestGradeId =
                record.HighestGradeId ?? string.Empty;
            bool wasPreviouslyCleared = record.IsCleared;
            bool isNewHighScore =
                battleResult.FinalScore > previousHighScore;
            long savedHighScore = isNewHighScore
                ? battleResult.FinalScore
                : previousHighScore;

            BattleGrade previousHighestGrade = ResolveSavedGrade(
                previousHighestGradeId);
            bool isNewHighestGrade =
                battleResult.Grade != BattleGrade.BelowC
                && BattleGradeRules.GetOrder(battleResult.Grade)
                    > BattleGradeRules.GetOrder(previousHighestGrade);
            string savedHighestGradeId = isNewHighestGrade
                ? battleResult.GradeId
                : previousHighestGradeId;

            HashSet<string> claimedGradeIds = CreateClaimedGradeSet(
                record.ClaimedFirstRewardGradeIds);
            BattleFirstGradeRewardEntry[] newFirstRewards =
                ResolveNewFirstRewards(
                    battleResult,
                    claimedGradeIds,
                    out long firstRewardAmount);
            long totalRewardAmount = checked(
                battleResult.RepeatRewardAmount + firstRewardAmount);
            long savedGachaCurrency = checked(
                profile.Currencies.GachaCurrency + totalRewardAmount);

            if (isNewRecord)
            {
                profile.BossRecords.Add(record);
            }

            profile.Currencies.GachaCurrency = savedGachaCurrency;
            record.HasAttempted = true;
            record.IsCleared = wasPreviouslyCleared
                || battleResult.EndReason == BattleResultKind.Victory;
            record.HighScore = savedHighScore;
            record.HighestGradeId = savedHighestGradeId;
            if (battleResult.EndReason == BattleResultKind.Victory
                && isNewHighScore)
            {
                record.BestDefeatTurn = battleResult.FinishedTurn;
                record.BestRemainingTurns = battleResult.RemainingTurns;
            }

            for (int index = 0; index < newFirstRewards.Length; index++)
            {
                record.ClaimedFirstRewardGradeIds.Add(
                    newFirstRewards[index].GradeId);
            }

            return new BattleProfileUpdateResult(
                previousHighScore,
                savedHighScore,
                isNewHighScore,
                previousHighestGradeId,
                savedHighestGradeId,
                isNewHighestGrade,
                battleResult.EndReason == BattleResultKind.Victory
                    && !wasPreviouslyCleared,
                battleResult.RepeatRewardAmount,
                newFirstRewards,
                firstRewardAmount,
                totalRewardAmount);
        }

        private static BossRecordSaveData FindRecord(
            IReadOnlyList<BossRecordSaveData> records,
            string bossId,
            string difficultyId)
        {
            BossRecordSaveData found = null;
            for (int index = 0; index < records.Count; index++)
            {
                BossRecordSaveData record = records[index]
                    ?? throw new InvalidOperationException(
                        "Profile boss records cannot contain null.");
                if (!string.Equals(
                        record.BossId,
                        bossId,
                        StringComparison.Ordinal)
                    || !string.Equals(
                        record.DifficultyId,
                        difficultyId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (found != null)
                {
                    throw new InvalidOperationException(
                        $"Duplicate boss record: {bossId}/{difficultyId}.");
                }

                found = record;
            }

            return found;
        }

        private static BossRecordSaveData CreateRecord(
            string bossId,
            string difficultyId)
        {
            return new BossRecordSaveData
            {
                BossId = bossId,
                DifficultyId = difficultyId,
                HighestGradeId = string.Empty,
                ClaimedFirstRewardGradeIds = new List<string>()
            };
        }

        private static void ValidateRecord(BossRecordSaveData record)
        {
            if (record.HighScore < 0
                || record.BestDefeatTurn < 0
                || record.BestRemainingTurns < 0)
            {
                throw new InvalidOperationException(
                    "Boss record numeric values cannot be negative.");
            }

            record.ClaimedFirstRewardGradeIds ??= new List<string>();
        }

        private static HashSet<string> CreateClaimedGradeSet(
            IReadOnlyList<string> claimedIds)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < claimedIds.Count; index++)
            {
                string gradeId = claimedIds[index];
                if (!BattleGradeIds.TryGetRewardGrade(
                        gradeId,
                        out _)
                    || !result.Add(gradeId))
                {
                    throw new InvalidOperationException(
                        "Claimed first reward grade IDs must be known, "
                            + "non-empty, and unique.");
                }
            }

            return result;
        }

        private static BattleFirstGradeRewardEntry[] ResolveNewFirstRewards(
            BattleFinalResult battleResult,
            ISet<string> claimedGradeIds,
            out long amount)
        {
            var progressionIds = new HashSet<string>(StringComparer.Ordinal);
            var newRewards = new List<BattleFirstGradeRewardEntry>();
            amount = 0L;
            IReadOnlyList<BattleFirstGradeRewardEntry> progression =
                battleResult.FirstGradeProgression;
            for (int index = 0; index < progression.Count; index++)
            {
                BattleFirstGradeRewardEntry entry = progression[index]
                    ?? throw new InvalidOperationException(
                        "First grade progression cannot contain null.");
                if (entry.Amount < 0
                    || !BattleGradeIds.TryGetRewardGrade(
                        entry.GradeId,
                        out BattleGrade idGrade)
                    || idGrade != entry.Grade
                    || !progressionIds.Add(entry.GradeId))
                {
                    throw new InvalidOperationException(
                        "First grade progression contains an invalid entry.");
                }

                if (claimedGradeIds.Contains(entry.GradeId))
                {
                    continue;
                }

                amount = checked(amount + entry.Amount);
                newRewards.Add(entry);
            }

            return newRewards.ToArray();
        }

        private static BattleGrade ResolveSavedGrade(string gradeId)
        {
            if (string.IsNullOrEmpty(gradeId))
            {
                return BattleGrade.BelowC;
            }

            if (!BattleGradeIds.TryGetRewardGrade(
                gradeId,
                out BattleGrade grade))
            {
                throw new InvalidOperationException(
                    $"Unknown saved highest grade ID: '{gradeId}'.");
            }

            return grade;
        }

        private static void ValidateBattleResult(
            BattleFinalResult battleResult)
        {
            if (battleResult == null)
            {
                throw new ArgumentNullException(nameof(battleResult));
            }

            bool isVictory =
                battleResult.EndReason == BattleResultKind.Victory;
            if ((!isVictory
                    && battleResult.EndReason != BattleResultKind.Defeat
                    && battleResult.EndReason
                        != BattleResultKind.TurnLimitReached)
                || battleResult.BossDefeated != isVictory)
            {
                throw new ArgumentException(
                    "Only a consistent normal battle result can be saved.",
                    nameof(battleResult));
            }

            if (string.IsNullOrWhiteSpace(battleResult.BossId)
                || string.IsNullOrWhiteSpace(battleResult.DifficultyId)
                || battleResult.FinalScore < 0
                || battleResult.RepeatRewardAmount < 0
                || !BattleGradeRules.IsDefined(battleResult.Grade))
            {
                throw new ArgumentException(
                    "Battle result persistence values are invalid.",
                    nameof(battleResult));
            }

            bool hasGradeId = BattleGradeIds.TryGetRewardGradeId(
                battleResult.Grade,
                out string expectedGradeId);
            if ((hasGradeId
                    && !string.Equals(
                        expectedGradeId,
                        battleResult.GradeId,
                        StringComparison.Ordinal))
                || (!hasGradeId
                    && !string.IsNullOrEmpty(battleResult.GradeId)))
            {
                throw new ArgumentException(
                    "Battle grade and grade ID are inconsistent.",
                    nameof(battleResult));
            }
        }
    }
}
