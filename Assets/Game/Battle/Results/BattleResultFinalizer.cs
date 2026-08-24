using System;

namespace ValorChronicle.Battle.Results
{
    public static class BattleResultFinalizer
    {
        public static BattleFinalResult Create(
            BattleFinalizationInput input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            ValidateId(input.BossId, nameof(input.BossId), "Boss");
            ValidateId(
                input.DifficultyId,
                nameof(input.DifficultyId),
                "Difficulty");
            if (input.Balance == null)
            {
                throw new ArgumentNullException(nameof(input.Balance));
            }

            BattleScoreResult score = BattleScoreCalculator.Calculate(
                new BattleScoreInput(
                    input.EndReason,
                    input.BossMaxHp,
                    input.TurnLimit,
                    input.FinishedTurn,
                    input.DamageScore,
                    input.Balance.RemainingTurnBonusRate));
            BattleGradeRewardResult gradeReward =
                BattleGradeRewardCalculator.Calculate(
                    input.Balance,
                    input.DifficultyId,
                    input.BossMaxHp,
                    score.FinalScore);
            return new BattleFinalResult(
                input.BossId,
                input.DifficultyId,
                input.TurnLimit,
                score,
                gradeReward);
        }

        private static void ValidateId(
            string value,
            string parameterName,
            string displayName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    $"{displayName} ID cannot be null or whitespace.",
                    parameterName);
            }
        }
    }
}
