using System.Collections.Generic;

namespace ValorChronicle.Save.DTO
{
    public sealed class PartyPresetSaveData
    {
        public string PresetId { get; set; }
        public List<string> CharacterSlotIds { get; set; }
    }
}
