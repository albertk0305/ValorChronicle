using ValorChronicle.Battle.Flow;

namespace ValorChronicle.Battle.Results
{
    public sealed class BattleFinalizationInput
    {
        public BattleFinalizationInput(
            BattleResultKind endReason,
            string bossId,
            string difficultyId,
            long bossMaxHp,
            int turnLimit,
            int finishedTurn,
            long damageScore,
            BattleResultBalance balance)
        {
            EndReason = endReason;
            BossId = bossId;
            DifficultyId = difficultyId;
            BossMaxHp = bossMaxHp;
            TurnLimit = turnLimit;
            FinishedTurn = finishedTurn;
            DamageScore = damageScore;
            Balance = balance;
        }

        public BattleResultKind EndReason { get; }
        public string BossId { get; }
        public string DifficultyId { get; }
        public long BossMaxHp { get; }
        public int TurnLimit { get; }
        public int FinishedTurn { get; }
        public long DamageScore { get; }
        public BattleResultBalance Balance { get; }
    }
}
