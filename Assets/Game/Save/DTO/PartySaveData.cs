using System.Collections.Generic;

namespace ValorChronicle.Save.DTO
{
    public sealed class PartySaveData
    {
        public int ActivePresetIndex { get; set; }
        public List<PartyPresetSaveData> Presets { get; set; }
        public string LastBossId { get; set; }
        public string LastDifficultyId { get; set; }
    }
}
