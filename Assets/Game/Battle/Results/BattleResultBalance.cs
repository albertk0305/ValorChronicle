using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValorChronicle.Battle.Results
{
    [Serializable]
    public sealed class BattleGradeThresholdEntry
    {
        [SerializeField]
        private BattleGrade grade;

        [SerializeField]
        private string gradeId;

        [SerializeField]
        private int thresholdPercent;

        public BattleGradeThresholdEntry(
            BattleGrade grade,
            string gradeId,
            int thresholdPercent)
        {
            this.grade = grade;
            this.gradeId = gradeId;
            this.thresholdPercent = thresholdPercent;
        }

        public BattleGrade Grade => grade;
        public string GradeId => gradeId;
        public int ThresholdPercent => thresholdPercent;
    }

    [Serializable]
    public sealed class BattleGradeRewardEntry
    {
        [SerializeField]
        private BattleGrade grade;

        [SerializeField]
        private long amount;

        public BattleGradeRewardEntry(BattleGrade grade, long amount)
        {
            this.grade = grade;
            this.amount = amount;
        }

        public BattleGrade Grade => grade;
        public long Amount => amount;
    }

    [Serializable]
    public sealed class BattleDifficultyRepeatRewardTable
    {
        [SerializeField]
        private string difficultyId;

        [SerializeField]
        private BattleGradeRewardEntry[] rewards =
            Array.Empty<BattleGradeRewardEntry>();

        public BattleDifficultyRepeatRewardTable(
            string difficultyId,
            BattleGradeRewardEntry[] rewards)
        {
            this.difficultyId = difficultyId;
            this.rewards = rewards == null
                ? Array.Empty<BattleGradeRewardEntry>()
                : (BattleGradeRewardEntry[])rewards.Clone();
        }

        public string DifficultyId => difficultyId;
        public IReadOnlyList<BattleGradeRewardEntry> Rewards =>
            Array.AsReadOnly(
                rewards ?? Array.Empty<BattleGradeRewardEntry>());
    }

    public sealed class BattleResultBalance
    {
        private readonly BattleGradeThresholdEntry[] gradeThresholds;
        private readonly BattleGradeRewardEntry[] firstGradeRewards;
        private readonly BattleDifficultyRepeatRewardTable[]
            difficultyRepeatRewards;

        public BattleResultBalance(
            decimal remainingTurnBonusRate,
            BattleGradeThresholdEntry[] gradeThresholds,
            BattleGradeRewardEntry[] firstGradeRewards,
            BattleDifficultyRepeatRewardTable[] difficultyRepeatRewards)
        {
            RemainingTurnBonusRate = remainingTurnBonusRate;
            this.gradeThresholds = CopyOrEmpty(gradeThresholds);
            this.firstGradeRewards = CopyOrEmpty(firstGradeRewards);
            this.difficultyRepeatRewards =
                CopyOrEmpty(difficultyRepeatRewards);
        }

        public decimal RemainingTurnBonusRate { get; }
        public IReadOnlyList<BattleGradeThresholdEntry> GradeThresholds =>
            Array.AsReadOnly(gradeThresholds);
        public IReadOnlyList<BattleGradeRewardEntry> FirstGradeRewards =>
            Array.AsReadOnly(firstGradeRewards);
        public IReadOnlyList<BattleDifficultyRepeatRewardTable>
            DifficultyRepeatRewards =>
                Array.AsReadOnly(difficultyRepeatRewards);

        private static T[] CopyOrEmpty<T>(T[] source)
        {
            return source == null ? Array.Empty<T>() : (T[])source.Clone();
        }
    }

    public static class BattleResultBalanceDefaults
    {
        public const int RemainingTurnBonusPercent = 30;

        public static BattleResultBalance Create()
        {
            return new BattleResultBalance(
                RemainingTurnBonusPercent / 100m,
                CreateGradeThresholds(),
                CreateFirstGradeRewards(),
                CreateDifficultyRepeatRewards());
        }

        public static BattleGradeThresholdEntry[] CreateGradeThresholds()
        {
            return new[]
            {
                new BattleGradeThresholdEntry(
                    BattleGrade.C,
                    BattleGradeIds.C,
                    20),
                new BattleGradeThresholdEntry(
                    BattleGrade.B,
                    BattleGradeIds.B,
                    40),
                new BattleGradeThresholdEntry(
                    BattleGrade.A,
                    BattleGradeIds.A,
                    60),
                new BattleGradeThresholdEntry(
                    BattleGrade.S,
                    BattleGradeIds.S,
                    80),
                new BattleGradeThresholdEntry(
                    BattleGrade.SS,
                    BattleGradeIds.SS,
                    100),
                new BattleGradeThresholdEntry(
                    BattleGrade.SSS,
                    BattleGradeIds.SSS,
                    115)
            };
        }

        public static BattleGradeRewardEntry[] CreateFirstGradeRewards()
        {
            return new[]
            {
                new BattleGradeRewardEntry(BattleGrade.C, 20L),
                new BattleGradeRewardEntry(BattleGrade.B, 30L),
                new BattleGradeRewardEntry(BattleGrade.A, 50L),
                new BattleGradeRewardEntry(BattleGrade.S, 100L),
                new BattleGradeRewardEntry(BattleGrade.SS, 200L),
                new BattleGradeRewardEntry(BattleGrade.SSS, 300L)
            };
        }

        public static BattleDifficultyRepeatRewardTable[]
            CreateDifficultyRepeatRewards()
        {
            return new[]
            {
                Table(
                    BattleDifficultyIds.Intro,
                    20L, 50L, 80L, 120L, 180L, 250L, 320L),
                Table(
                    BattleDifficultyIds.Normal,
                    20L, 60L, 100L, 150L, 230L, 320L, 420L),
                Table(
                    BattleDifficultyIds.Advanced,
                    20L, 70L, 120L, 190L, 290L, 420L, 560L),
                Table(
                    BattleDifficultyIds.Hard,
                    20L, 80L, 140L, 230L, 360L, 550L, 750L),
                Table(
                    BattleDifficultyIds.Challenge,
                    20L, 100L, 180L, 300L, 480L, 700L, 1000L)
            };
        }

        private static BattleDifficultyRepeatRewardTable Table(
            string difficultyId,
            long belowC,
            long c,
            long b,
            long a,
            long s,
            long ss,
            long sss)
        {
            return new BattleDifficultyRepeatRewardTable(
                difficultyId,
                new[]
                {
                    new BattleGradeRewardEntry(BattleGrade.BelowC, belowC),
                    new BattleGradeRewardEntry(BattleGrade.C, c),
                    new BattleGradeRewardEntry(BattleGrade.B, b),
                    new BattleGradeRewardEntry(BattleGrade.A, a),
                    new BattleGradeRewardEntry(BattleGrade.S, s),
                    new BattleGradeRewardEntry(BattleGrade.SS, ss),
                    new BattleGradeRewardEntry(BattleGrade.SSS, sss)
                });
        }
    }
}
