using System;
using System.Collections.Generic;

namespace ValorChronicle.Battle.Results
{
    public static class BattleResultBalanceValidator
    {
        public static bool TryValidate(
            BattleResultBalance balance,
            out string errorMessage)
        {
            if (balance == null)
            {
                errorMessage = "Battle result balance is required.";
                return false;
            }

            if (balance.RemainingTurnBonusRate
                != BattleScoreCalculator
                    .DefaultRemainingTurnBonusPercent)
            {
                errorMessage =
                    "Remaining-turn bonus rate must match the score rule.";
                return false;
            }

            if (!ValidateGradeThresholds(balance, out errorMessage))
            {
                return false;
            }

            if (!ValidateFirstGradeRewards(balance, out errorMessage))
            {
                return false;
            }

            if (!ValidateDifficultyRewards(balance, out errorMessage))
            {
                return false;
            }

            errorMessage = null;
            return true;
        }

        public static void ValidateOrThrow(BattleResultBalance balance)
        {
            if (!TryValidate(balance, out string errorMessage))
            {
                throw new ArgumentException(
                    errorMessage,
                    nameof(balance));
            }
        }

        private static bool ValidateGradeThresholds(
            BattleResultBalance balance,
            out string errorMessage)
        {
            IReadOnlyList<BattleGradeThresholdEntry> thresholds =
                balance.GradeThresholds;
            IReadOnlyList<BattleGrade> required =
                BattleGradeRules.ProgressionGrades;
            if (thresholds.Count != required.Count)
            {
                errorMessage = "Grade thresholds must contain C through SSS.";
                return false;
            }

            var grades = new HashSet<BattleGrade>();
            var gradeIds = new HashSet<string>(StringComparer.Ordinal);
            int previousThreshold = -1;
            for (int index = 0; index < thresholds.Count; index++)
            {
                BattleGradeThresholdEntry entry = thresholds[index];
                if (entry == null)
                {
                    errorMessage =
                        $"Grade threshold at index {index} is null.";
                    return false;
                }

                if (!BattleGradeRules.IsDefined(entry.Grade)
                    || entry.Grade == BattleGrade.BelowC
                    || !grades.Add(entry.Grade))
                {
                    errorMessage =
                        $"Grade threshold {entry.Grade} is invalid or duplicate.";
                    return false;
                }

                if (entry.Grade != required[index])
                {
                    errorMessage =
                        "Grade thresholds must be ordered C through SSS.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(entry.GradeId)
                    || !gradeIds.Add(entry.GradeId))
                {
                    errorMessage = "Grade IDs cannot be empty or duplicate.";
                    return false;
                }

                if (!string.Equals(
                    entry.GradeId,
                    BattleGradeIds.GetRequiredRewardGradeId(entry.Grade),
                    StringComparison.Ordinal))
                {
                    errorMessage =
                        $"Grade ID for {entry.Grade} is not the stable ID.";
                    return false;
                }

                if (entry.ThresholdPercent <= 0
                    || entry.ThresholdPercent <= previousThreshold)
                {
                    errorMessage =
                        "Grade thresholds must be positive and strictly "
                            + "increasing.";
                    return false;
                }

                previousThreshold = entry.ThresholdPercent;
            }

            errorMessage = null;
            return true;
        }

        private static bool ValidateFirstGradeRewards(
            BattleResultBalance balance,
            out string errorMessage)
        {
            IReadOnlyList<BattleGradeRewardEntry> rewards =
                balance.FirstGradeRewards;
            IReadOnlyList<BattleGrade> required =
                BattleGradeRules.ProgressionGrades;
            if (rewards.Count != required.Count)
            {
                errorMessage =
                    "First-grade rewards must contain C through SSS.";
                return false;
            }

            var grades = new HashSet<BattleGrade>();
            for (int index = 0; index < rewards.Count; index++)
            {
                BattleGradeRewardEntry entry = rewards[index];
                if (entry == null)
                {
                    errorMessage =
                        $"First-grade reward at index {index} is null.";
                    return false;
                }

                if (entry.Grade != required[index]
                    || !grades.Add(entry.Grade))
                {
                    errorMessage =
                        "First-grade rewards must be unique and ordered C "
                            + "through SSS.";
                    return false;
                }

                if (entry.Amount < 0)
                {
                    errorMessage = "First-grade rewards cannot be negative.";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        private static bool ValidateDifficultyRewards(
            BattleResultBalance balance,
            out string errorMessage)
        {
            var difficultyIds = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<BattleDifficultyRepeatRewardTable> tables =
                balance.DifficultyRepeatRewards;
            for (int index = 0; index < tables.Count; index++)
            {
                BattleDifficultyRepeatRewardTable table = tables[index];
                if (table == null)
                {
                    errorMessage =
                        $"Difficulty reward table at index {index} is null.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(table.DifficultyId)
                    || !difficultyIds.Add(table.DifficultyId))
                {
                    errorMessage =
                        "Difficulty IDs cannot be empty or duplicate.";
                    return false;
                }

                if (!ValidateRepeatRewardTable(table, out errorMessage))
                {
                    return false;
                }
            }

            IReadOnlyList<string> required = BattleDifficultyIds.RequiredIds;
            for (int index = 0; index < required.Count; index++)
            {
                if (!difficultyIds.Contains(required[index]))
                {
                    errorMessage =
                        $"Required difficulty reward is missing: "
                            + $"{required[index]}.";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        private static bool ValidateRepeatRewardTable(
            BattleDifficultyRepeatRewardTable table,
            out string errorMessage)
        {
            IReadOnlyList<BattleGradeRewardEntry> rewards = table.Rewards;
            int expectedCount = BattleGradeRules.ProgressionGrades.Count + 1;
            if (rewards.Count != expectedCount)
            {
                errorMessage =
                    $"Repeat rewards for {table.DifficultyId} must contain "
                        + "BelowC through SSS.";
                return false;
            }

            var grades = new HashSet<BattleGrade>();
            for (int index = 0; index < rewards.Count; index++)
            {
                BattleGradeRewardEntry entry = rewards[index];
                BattleGrade expectedGrade = (BattleGrade)index;
                if (entry == null
                    || !BattleGradeRules.IsDefined(entry.Grade)
                    || entry.Grade != expectedGrade
                    || !grades.Add(entry.Grade))
                {
                    errorMessage =
                        $"Repeat rewards for {table.DifficultyId} must be "
                            + "unique and ordered BelowC through SSS.";
                    return false;
                }

                if (entry.Amount < 0)
                {
                    errorMessage = "Repeat rewards cannot be negative.";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }
    }
}
