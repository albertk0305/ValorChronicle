using System.Collections.Generic;

namespace ValorChronicle.Battle.Results.Persistence
{
    public sealed class BattlePersistedResult
    {
        internal BattlePersistedResult(
            BattleFinalResult battleFinalResult,
            BattleProfileUpdateResult updateResult)
        {
            BattleFinalResult = battleFinalResult;
            UpdateResult = updateResult;
        }

        public BattleFinalResult BattleFinalResult { get; }
        public BattleProfileUpdateResult UpdateResult { get; }
        public long PreviousHighScore => UpdateResult.PreviousHighScore;
        public long SavedHighScore => UpdateResult.SavedHighScore;
        public bool IsNewHighScore => UpdateResult.IsNewHighScore;
        public string PreviousHighestGradeId =>
            UpdateResult.PreviousHighestGradeId;
        public string SavedHighestGradeId =>
            UpdateResult.SavedHighestGradeId;
        public bool IsNewHighestGrade => UpdateResult.IsNewHighestGrade;
        public bool WasFirstClear => UpdateResult.WasFirstClear;
        public long RepeatRewardAmount => UpdateResult.RepeatRewardAmount;
        public IReadOnlyList<BattleFirstGradeRewardEntry>
            NewFirstGradeRewardEntries =>
                UpdateResult.NewFirstGradeRewardEntries;
        public long FirstGradeRewardAmount =>
            UpdateResult.FirstGradeRewardAmount;
        public long TotalRewardAmount => UpdateResult.TotalRewardAmount;
    }
}
