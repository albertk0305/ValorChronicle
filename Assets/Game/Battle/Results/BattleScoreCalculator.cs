using System;
using ValorChronicle.Battle.Flow;

namespace ValorChronicle.Battle.Results
{
    public static class BattleScoreCalculator
    {
        public const decimal DefaultRemainingTurnBonusPercent = 0.30m;

        public static BattleScoreResult Calculate(BattleScoreInput input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            Validate(input);

            bool isVictory = input.EndReason == BattleResultKind.Victory;
            int remainingTurns = isVictory
                ? input.TurnLimit - input.FinishedTurn
                : 0;
            long remainingTurnBonus = isVictory
                ? CalculateRemainingTurnBonus(input, remainingTurns)
                : 0L;
            long finalScore = checked(
                input.DamageScore + remainingTurnBonus);

            return new BattleScoreResult(
                input.EndReason,
                input.FinishedTurn,
                remainingTurns,
                input.DamageScore,
                remainingTurnBonus,
                finalScore);
        }

        private static long CalculateRemainingTurnBonus(
            BattleScoreInput input,
            int remainingTurns)
        {
            decimal exactBonus = (decimal)input.BossMaxHp
                * remainingTurns
                / input.TurnLimit
                * input.RemainingTurnBonusPercent;
            decimal flooredBonus = decimal.Floor(exactBonus);
            return checked((long)flooredBonus);
        }

        private static void Validate(BattleScoreInput input)
        {
            switch (input.EndReason)
            {
                case BattleResultKind.Victory:
                case BattleResultKind.Defeat:
                case BattleResultKind.TurnLimitReached:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(input.EndReason),
                        input.EndReason,
                        "Only terminal score-bearing results are supported.");
            }

            if (input.BossMaxHp <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(input.BossMaxHp),
                    input.BossMaxHp,
                    "Boss maximum HP must be positive.");
            }

            if (input.TurnLimit <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(input.TurnLimit),
                    input.TurnLimit,
                    "Turn limit must be positive.");
            }

            if (input.FinishedTurn < 1
                || input.FinishedTurn > input.TurnLimit)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(input.FinishedTurn),
                    input.FinishedTurn,
                    "Finished turn must be within the battle turn limit.");
            }

            if (input.EndReason == BattleResultKind.TurnLimitReached
                && input.FinishedTurn != input.TurnLimit)
            {
                throw new ArgumentException(
                    "Turn-limit results must finish on the turn limit.",
                    nameof(input));
            }

            if (input.DamageScore < 0
                || input.DamageScore > input.BossMaxHp)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(input.DamageScore),
                    input.DamageScore,
                    "Damage score must be between zero and boss maximum HP.");
            }

            if (input.RemainingTurnBonusPercent <= 0m
                || input.RemainingTurnBonusPercent > 1m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(input.RemainingTurnBonusPercent),
                    input.RemainingTurnBonusPercent,
                    "Remaining-turn bonus percent must be greater than zero "
                        + "and no greater than one.");
            }
        }
    }
}
