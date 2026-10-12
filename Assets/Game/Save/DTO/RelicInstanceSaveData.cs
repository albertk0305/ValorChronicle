using ValorChronicle.Save.Rules;

namespace ValorChronicle.Save.DTO
{
    public sealed class RelicInstanceSaveData
    {
        public string InstanceId { get; set; }
        public string RelicDefinitionId { get; set; }
        public int Awakening { get; set; }
        // Legacy v2 migration source only. V3 equip state lives on CharacterSaveData.
        public string EquippedCharacterId { get; set; } = SaveRules.EmptyId;
        public int EquippedSlotIndex { get; set; } =
            SaveRules.UnequippedRelicSlotIndex;
        public bool IsLocked { get; set; }
        public bool IsNew { get; set; }
    }
}
