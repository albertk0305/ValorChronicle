using System.Collections.Generic;

namespace ValorChronicle.Save.DTO
{
    public sealed class BossRecordSaveData
    {
        public string BossId { get; set; }
        public string DifficultyId { get; set; }
        public bool HasAttempted { get; set; }
        public bool IsCleared { get; set; }
        public long HighScore { get; set; }
        public string HighestGradeId { get; set; }
        public int BestDefeatTurn { get; set; }
        public int BestRemainingTurns { get; set; }
        public List<string> ClaimedFirstRewardGradeIds { get; set; }
    }
}
