using System;
using System.Collections.Generic;

namespace ValorChronicle.Battle.Results
{
    public static class BattleGradeRewardCalculator
    {
        public static BattleGradeRewardResult Calculate(
            BattleResultBalance balance,
            string difficultyId,
            long bossMaxHp,
            long finalScore)
        {
            BattleResultBalanceValidator.ValidateOrThrow(balance);

            if (string.IsNullOrWhiteSpace(difficultyId))
            {
                throw new ArgumentException(
                    "Difficulty ID cannot be null or whitespace.",
                    nameof(difficultyId));
            }

            if (bossMaxHp <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bossMaxHp),
                    bossMaxHp,
                    "Boss maximum HP must be positive.");
            }

            if (finalScore < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(finalScore),
                    finalScore,
                    "Final score cannot be negative.");
            }

            BattleGrade grade = ResolveGrade(
                balance.GradeThresholds,
                bossMaxHp,
                finalScore);
            string gradeId = ResolveGradeId(
                balance.GradeThresholds,
                grade);
            long repeatReward = ResolveRepeatReward(
                balance.DifficultyRepeatRewards,
                difficultyId,
                grade);
            BattleFirstGradeRewardEntry[] progression =
                ResolveFirstGradeProgressionCore(balance, grade);

            return new BattleGradeRewardResult(
                grade,
                gradeId,
                repeatReward,
                progression);
        }

        public static IReadOnlyList<BattleFirstGradeRewardEntry>
            GetFirstGradeProgression(
                BattleResultBalance balance,
                BattleGrade grade)
        {
            BattleResultBalanceValidator.ValidateOrThrow(balance);
            if (!BattleGradeRules.IsDefined(grade))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(grade),
                    grade,
                    "Grade must be a defined battle grade.");
            }

            return Array.AsReadOnly(
                ResolveFirstGradeProgressionCore(balance, grade));
        }

        private static BattleGrade ResolveGrade(
            IReadOnlyList<BattleGradeThresholdEntry> thresholds,
            long bossMaxHp,
            long finalScore)
        {
            BattleGrade resolved = BattleGrade.BelowC;
            decimal scaledScore = (decimal)finalScore * 100m;
            for (int index = 0; index < thresholds.Count; index++)
            {
                BattleGradeThresholdEntry threshold = thresholds[index];
                decimal scaledThreshold =
                    (decimal)bossMaxHp * threshold.ThresholdPercent;
                if (scaledScore < scaledThreshold)
                {
                    break;
                }

                resolved = threshold.Grade;
            }

            return resolved;
        }

        private static string ResolveGradeId(
            IReadOnlyList<BattleGradeThresholdEntry> thresholds,
            BattleGrade grade)
        {
            if (grade == BattleGrade.BelowC)
            {
                return string.Empty;
            }

            for (int index = 0; index < thresholds.Count; index++)
            {
                if (thresholds[index].Grade == grade)
                {
                    return thresholds[index].GradeId;
                }
            }

            throw new InvalidOperationException(
                $"Grade ID was not found for {grade}.");
        }

        private static long ResolveRepeatReward(
            IReadOnlyList<BattleDifficultyRepeatRewardTable> tables,
            string difficultyId,
            BattleGrade grade)
        {
            for (int tableIndex = 0;
                tableIndex < tables.Count;
                tableIndex++)
            {
                BattleDifficultyRepeatRewardTable table = tables[tableIndex];
                if (!string.Equals(
                    table.DifficultyId,
                    difficultyId,
                    StringComparison.Ordinal))
                {
                    continue;
                }

                IReadOnlyList<BattleGradeRewardEntry> rewards =
                    table.Rewards;
                for (int rewardIndex = 0;
                    rewardIndex < rewards.Count;
                    rewardIndex++)
                {
                    if (rewards[rewardIndex].Grade == grade)
                    {
                        return rewards[rewardIndex].Amount;
                    }
                }

                break;
            }

            throw new ArgumentException(
                $"Repeat reward was not found for difficulty "
                    + $"'{difficultyId}' and grade {grade}.",
                nameof(difficultyId));
        }

        private static BattleFirstGradeRewardEntry[]
            ResolveFirstGradeProgressionCore(
                BattleResultBalance balance,
                BattleGrade grade)
        {
            if (grade == BattleGrade.BelowC)
            {
                return Array.Empty<BattleFirstGradeRewardEntry>();
            }

            int gradeOrder = BattleGradeRules.GetOrder(grade);
            var progression =
                new List<BattleFirstGradeRewardEntry>(gradeOrder);
            IReadOnlyList<BattleGradeRewardEntry> rewards =
                balance.FirstGradeRewards;
            for (int index = 0; index < rewards.Count; index++)
            {
                BattleGradeRewardEntry reward = rewards[index];
                if (BattleGradeRules.GetOrder(reward.Grade) > gradeOrder)
                {
                    break;
                }

                progression.Add(new BattleFirstGradeRewardEntry(
                    reward.Grade,
                    ResolveGradeId(balance.GradeThresholds, reward.Grade),
                    reward.Amount));
            }

            return progression.ToArray();
        }
    }
}
