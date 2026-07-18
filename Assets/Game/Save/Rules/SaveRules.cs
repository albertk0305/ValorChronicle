using ValorChronicle.Save.DTO;

namespace ValorChronicle.Save.Rules
{
    /// <summary>
    /// Defines shared structural and numeric rules for profile save data.
    /// </summary>
    public static class SaveRules
    {
        /// <summary>The schema version supported by the current application.</summary>
        public const int CurrentSaveVersion = SaveSchema.CurrentVersion;
        /// <summary>The fixed number of ordered character slots in every party preset.</summary>
        public const int PartySlotCount = 5;
        /// <summary>The minimum valid character level.</summary>
        public const int CharacterMinLevel = 1;
        /// <summary>The maximum valid character level.</summary>
        public const int CharacterMaxLevel = 100;
        /// <summary>The minimum valid character awakening value.</summary>
        public const int CharacterMinAwakening = 0;
        /// <summary>The maximum valid character awakening value.</summary>
        public const int CharacterMaxAwakening = 6;
        /// <summary>The minimum valid relic awakening value.</summary>
        public const int RelicMinAwakening = 0;
        /// <summary>The maximum valid relic awakening value.</summary>
        public const int RelicMaxAwakening = 5;
        /// <summary>The fixed number of relic slots available to every character.</summary>
        public const int RelicSlotCountPerCharacter = 4;
        /// <summary>The minimum equipped relic slot index.</summary>
        public const int EquippedRelicSlotMinIndex = 0;
        /// <summary>The maximum equipped relic slot index.</summary>
        public const int EquippedRelicSlotMaxIndex = RelicSlotCountPerCharacter - 1;
        /// <summary>The slot index used when a relic is not equipped.</summary>
        public const int UnequippedRelicSlotIndex = -1;
        /// <summary>The stable identifier of the required default party preset.</summary>
        public const string DefaultPartyPresetId = "party_default";

        /// <summary>The canonical representation of an empty content identifier or party slot.</summary>
        public static readonly string EmptyId = string.Empty;
    }
}
