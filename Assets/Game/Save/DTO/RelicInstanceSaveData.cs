namespace ValorChronicle.Save.DTO
{
    public sealed class RelicInstanceSaveData
    {
        public string InstanceId { get; set; }
        public string RelicDefinitionId { get; set; }
        public int Awakening { get; set; }
        public string EquippedCharacterId { get; set; }
        public int EquippedSlotIndex { get; set; }
        public bool IsLocked { get; set; }
        public bool IsNew { get; set; }
    }
}
