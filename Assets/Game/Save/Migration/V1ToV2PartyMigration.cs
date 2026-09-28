using System;
using System.Collections.Generic;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Save.Migration
{
    /// <summary>
    /// Expands version 1 party data to five ordered five-slot presets.
    /// </summary>
    public sealed class V1ToV2PartyMigration : ISaveMigrationStep
    {
        public int FromVersion => 1;
        public int ToVersion => 2;

        public ProfileSaveData Migrate(ProfileSaveData source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (source.SaveVersion != FromVersion)
            {
                throw new ArgumentException(
                    $"Migration requires save version {FromVersion}.",
                    nameof(source));
            }

            source.Party ??= new PartySaveData();
            source.Party.Presets ??= new List<PartyPresetSaveData>();

            if (source.Party.Presets.Count > SaveRules.PartyPresetCount)
            {
                source.Party.Presets.RemoveRange(
                    SaveRules.PartyPresetCount,
                    source.Party.Presets.Count
                        - SaveRules.PartyPresetCount);
            }

            while (source.Party.Presets.Count < SaveRules.PartyPresetCount)
            {
                source.Party.Presets.Add(CreateEmptyPreset(
                    source.Party.Presets.Count));
            }

            for (int presetIndex = 0;
                presetIndex < source.Party.Presets.Count;
                presetIndex++)
            {
                PartyPresetSaveData preset =
                    source.Party.Presets[presetIndex];
                if (preset == null)
                {
                    preset = CreateEmptyPreset(presetIndex);
                    source.Party.Presets[presetIndex] = preset;
                }

                preset.CharacterSlotIds ??= new List<string>();
                for (int slotIndex = 0;
                    slotIndex < preset.CharacterSlotIds.Count;
                    slotIndex++)
                {
                    preset.CharacterSlotIds[slotIndex] ??=
                        SaveRules.EmptyId;
                }

                while (preset.CharacterSlotIds.Count
                    < SaveRules.PartySlotCount)
                {
                    preset.CharacterSlotIds.Add(SaveRules.EmptyId);
                }

                if (preset.CharacterSlotIds.Count
                    > SaveRules.PartySlotCount)
                {
                    preset.CharacterSlotIds.RemoveRange(
                        SaveRules.PartySlotCount,
                        preset.CharacterSlotIds.Count
                            - SaveRules.PartySlotCount);
                }
            }

            if (source.Party.ActivePresetIndex < 0
                || source.Party.ActivePresetIndex
                    >= SaveRules.PartyPresetCount)
            {
                source.Party.ActivePresetIndex = 0;
            }

            source.SaveVersion = ToVersion;
            return source;
        }

        private static PartyPresetSaveData CreateEmptyPreset(
            int presetIndex)
        {
            var slots = new List<string>(SaveRules.PartySlotCount);
            for (int slotIndex = 0;
                slotIndex < SaveRules.PartySlotCount;
                slotIndex++)
            {
                slots.Add(SaveRules.EmptyId);
            }

            return new PartyPresetSaveData
            {
                PresetId = SaveRules.GetDefaultPartyPresetId(presetIndex),
                CharacterSlotIds = slots
            };
        }
    }
}
