using System;
using System.Collections.Generic;

namespace ValorChronicle.Battle.Results.Persistence
{
    public sealed class BattleProfileUpdateResult
    {
        private readonly BattleFirstGradeRewardEntry[]
            newFirstGradeRewardEntries;

        internal BattleProfileUpdateResult(
            long previousHighScore,
            long savedHighScore,
            bool isNewHighScore,
            string previousHighestGradeId,
            string savedHighestGradeId,
            bool isNewHighestGrade,
            bool wasFirstClear,
            long repeatRewardAmount,
            BattleFirstGradeRewardEntry[] newFirstGradeRewardEntries,
            long firstGradeRewardAmount,
            long totalRewardAmount)
        {
            PreviousHighScore = previousHighScore;
            SavedHighScore = savedHighScore;
            IsNewHighScore = isNewHighScore;
            PreviousHighestGradeId = previousHighestGradeId;
            SavedHighestGradeId = savedHighestGradeId;
            IsNewHighestGrade = isNewHighestGrade;
            WasFirstClear = wasFirstClear;
            RepeatRewardAmount = repeatRewardAmount;
            this.newFirstGradeRewardEntries =
                newFirstGradeRewardEntries == null
                    ? Array.Empty<BattleFirstGradeRewardEntry>()
                    : (BattleFirstGradeRewardEntry[])
                        newFirstGradeRewardEntries.Clone();
            FirstGradeRewardAmount = firstGradeRewardAmount;
            TotalRewardAmount = totalRewardAmount;
        }

        public long PreviousHighScore { get; }
        public long SavedHighScore { get; }
        public bool IsNewHighScore { get; }
        public string PreviousHighestGradeId { get; }
        public string SavedHighestGradeId { get; }
        public bool IsNewHighestGrade { get; }
        public bool WasFirstClear { get; }
        public long RepeatRewardAmount { get; }
        public IReadOnlyList<BattleFirstGradeRewardEntry>
            NewFirstGradeRewardEntries =>
                Array.AsReadOnly(newFirstGradeRewardEntries);
        public long FirstGradeRewardAmount { get; }
        public long TotalRewardAmount { get; }
    }
}
