using System.Collections.Generic;

namespace ValorChronicle.Save.DTO
{
    public sealed class ProfileSaveData
    {
        public int SaveVersion { get; set; }
        public string ProfileId { get; set; }
        public long CreatedAtUtcUnixSeconds { get; set; }
        public long LastSavedAtUtcUnixSeconds { get; set; }
        public CurrencySaveData Currencies { get; set; }
        public List<CharacterSaveData> Characters { get; set; }
        public List<RelicInstanceSaveData> RelicInstances { get; set; }
        public PartySaveData Party { get; set; }
        public List<GachaStateSaveData> GachaStates { get; set; }
        public List<BossRecordSaveData> BossRecords { get; set; }
        public List<string> UnlockedContentIds { get; set; }
        public List<string> CompletedTutorialIds { get; set; }
    }
}
