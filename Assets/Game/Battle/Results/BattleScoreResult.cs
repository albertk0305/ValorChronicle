using ValorChronicle.Battle.Flow;

namespace ValorChronicle.Battle.Results
{
    public sealed class BattleScoreResult
    {
        internal BattleScoreResult(
            BattleResultKind endReason,
            int finishedTurn,
            int remainingTurns,
            long damageScore,
            long remainingTurnBonus,
            long finalScore)
        {
            EndReason = endReason;
            FinishedTurn = finishedTurn;
            RemainingTurns = remainingTurns;
            DamageScore = damageScore;
            RemainingTurnBonus = remainingTurnBonus;
            FinalScore = finalScore;
        }

        public BattleResultKind EndReason { get; }
        public bool BossDefeated => EndReason == BattleResultKind.Victory;
        public int FinishedTurn { get; }
        public int RemainingTurns { get; }
        public long DamageScore { get; }
        public long RemainingTurnBonus { get; }
        public long FinalScore { get; }
    }
}
