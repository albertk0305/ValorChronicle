namespace ValorChronicle.Relics.Inventory
{
    /// <summary>Immutable UI-facing projection of one owned relic instance.</summary>
    public sealed class RelicInventoryEntry
    {
        internal RelicInventoryEntry(
            string instanceId,
            string relicDefinitionId,
            int awakening,
            string displayName,
            bool isLocked,
            bool isNew,
            bool isEquipped,
            string equippedCharacterId,
            int equippedSlotIndex,
            bool isEquippedInTargetSlot,
            bool isDimmed,
            bool isInteractable,
            RelicEquipAvailabilityReason availabilityReason)
        {
            InstanceId = instanceId;
            RelicDefinitionId = relicDefinitionId;
            Awakening = awakening;
            DisplayName = displayName;
            IsLocked = isLocked;
            IsNew = isNew;
            IsEquipped = isEquipped;
            EquippedCharacterId = equippedCharacterId;
            EquippedSlotIndex = equippedSlotIndex;
            IsEquippedInTargetSlot = isEquippedInTargetSlot;
            IsDimmed = isDimmed;
            IsInteractable = isInteractable;
            AvailabilityReason = availabilityReason;
        }

        public string InstanceId { get; }
        public string RelicDefinitionId { get; }
        public int Awakening { get; }
        public string DisplayName { get; }
        public bool IsLocked { get; }
        public bool IsNew { get; }
        public bool IsEquipped { get; }
        public string EquippedCharacterId { get; }
        public int EquippedSlotIndex { get; }
        public bool IsEquippedInTargetSlot { get; }
        public bool IsDimmed { get; }
        public bool IsInteractable { get; }
        public RelicEquipAvailabilityReason AvailabilityReason { get; }
    }
}
