using ValorChronicle.Battle.Flow;

namespace ValorChronicle.Battle.Results
{
    public sealed class BattleScoreInput
    {
        public BattleScoreInput(
            BattleResultKind endReason,
            long bossMaxHp,
            int turnLimit,
            int finishedTurn,
            long damageScore,
            decimal remainingTurnBonusPercent =
                BattleScoreCalculator.DefaultRemainingTurnBonusPercent)
        {
            EndReason = endReason;
            BossMaxHp = bossMaxHp;
            TurnLimit = turnLimit;
            FinishedTurn = finishedTurn;
            DamageScore = damageScore;
            RemainingTurnBonusPercent = remainingTurnBonusPercent;
        }

        public BattleResultKind EndReason { get; }
        public long BossMaxHp { get; }
        public int TurnLimit { get; }
        public int FinishedTurn { get; }
        public long DamageScore { get; }
        public decimal RemainingTurnBonusPercent { get; }
    }
}
