namespace ValorChronicle.Relics
{
    /// <summary>Identifies one authoritative character relic slot.</summary>
    public sealed class RelicEquipLocation
    {
        internal RelicEquipLocation(
            string relicInstanceId,
            string characterId,
            int slotIndex)
        {
            RelicInstanceId = relicInstanceId;
            CharacterId = characterId;
            SlotIndex = slotIndex;
        }

        public string RelicInstanceId { get; }
        public string CharacterId { get; }
        public int SlotIndex { get; }
    }
}
