using ValorChronicle.Save.Rules;

namespace ValorChronicle.Relics
{
    public enum RelicEquipAvailabilityReason
    {
        Allowed = 0,
        CharacterNotFound = 1,
        InvalidTargetSlot = 2,
        RelicInstanceNotFound = 3,
        EquippedByOtherCharacter = 4,
        EquippedInOtherSlot = 5,
        DuplicateDefinitionInOtherSlot = 6
    }

    /// <summary>Describes whether and why a relic candidate can be selected.</summary>
    public sealed class RelicEquipAvailability
    {
        internal RelicEquipAvailability(
            RelicEquipAvailabilityReason reason,
            RelicEquipLocation location)
        {
            Reason = reason;
            EquippedCharacterId = location?.CharacterId
                ?? SaveRules.EmptyId;
            EquippedSlotIndex = location?.SlotIndex
                ?? SaveRules.UnequippedRelicSlotIndex;
        }

        public bool CanSelect =>
            Reason == RelicEquipAvailabilityReason.Allowed;
        public RelicEquipAvailabilityReason Reason { get; }
        public string EquippedCharacterId { get; }
        public int EquippedSlotIndex { get; }
    }
}
