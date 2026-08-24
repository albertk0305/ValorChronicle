using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Flow;

namespace ValorChronicle.Battle.Results
{
    public sealed class BattleFinalResult
    {
        private readonly BattleFirstGradeRewardEntry[]
            firstGradeProgression;

        internal BattleFinalResult(
            string bossId,
            string difficultyId,
            int turnLimit,
            BattleScoreResult score,
            BattleGradeRewardResult gradeReward)
        {
            BossId = bossId;
            DifficultyId = difficultyId;
            EndReason = score.EndReason;
            BossDefeated = score.BossDefeated;
            FinishedTurn = score.FinishedTurn;
            TurnLimit = turnLimit;
            RemainingTurns = score.RemainingTurns;
            DamageScore = score.DamageScore;
            RemainingTurnBonus = score.RemainingTurnBonus;
            FinalScore = score.FinalScore;
            Grade = gradeReward.Grade;
            GradeId = gradeReward.GradeId;
            RepeatRewardAmount = gradeReward.RepeatRewardAmount;

            IReadOnlyList<BattleFirstGradeRewardEntry> progression =
                gradeReward.FirstGradeProgression;
            firstGradeProgression =
                new BattleFirstGradeRewardEntry[progression.Count];
            for (int index = 0; index < progression.Count; index++)
            {
                firstGradeProgression[index] = progression[index];
            }
        }

        public BattleResultKind EndReason { get; }
        public bool BossDefeated { get; }
        public string BossId { get; }
        public string DifficultyId { get; }
        public int FinishedTurn { get; }
        public int TurnLimit { get; }
        public int RemainingTurns { get; }
        public long DamageScore { get; }
        public long RemainingTurnBonus { get; }
        public long FinalScore { get; }
        public BattleGrade Grade { get; }
        public string GradeId { get; }
        public long RepeatRewardAmount { get; }
        public IReadOnlyList<BattleFirstGradeRewardEntry>
            FirstGradeProgression =>
                Array.AsReadOnly(firstGradeProgression);
    }
}
