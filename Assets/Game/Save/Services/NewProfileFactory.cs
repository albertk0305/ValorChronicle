using System;
using System.Collections.Generic;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Save.Services
{
    public sealed class NewProfileFactory
    {
        public ProfileSaveData Create(string profileId, long utcUnixSeconds)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                throw new ArgumentException("Profile ID cannot be empty or whitespace.", nameof(profileId));
            }

            return new ProfileSaveData
            {
                SaveVersion = SaveRules.CurrentSaveVersion,
                ProfileId = profileId,
                CreatedAtUtcUnixSeconds = utcUnixSeconds,
                LastSavedAtUtcUnixSeconds = utcUnixSeconds,
                Currencies = new CurrencySaveData(),
                Characters = new List<CharacterSaveData>(),
                RelicInstances = new List<RelicInstanceSaveData>(),
                Party = CreateDefaultParty(),
                GachaStates = new List<GachaStateSaveData>(),
                BossRecords = new List<BossRecordSaveData>(),
                UnlockedContentIds = new List<string>(),
                CompletedTutorialIds = new List<string>()
            };
        }

        private static PartySaveData CreateDefaultParty()
        {
            var presets = new List<PartyPresetSaveData>(
                SaveRules.PartyPresetCount);
            for (int presetIndex = 0;
                presetIndex < SaveRules.PartyPresetCount;
                presetIndex++)
            {
                presets.Add(CreateEmptyPreset(presetIndex));
            }

            return new PartySaveData
            {
                ActivePresetIndex = 0,
                Presets = presets,
                LastBossId = SaveRules.EmptyId,
                LastDifficultyId = SaveRules.EmptyId
            };
        }

        private static PartyPresetSaveData CreateEmptyPreset(int presetIndex)
        {
            var characterSlotIds = new List<string>(
                SaveRules.PartySlotCount);
            for (int slotIndex = 0;
                slotIndex < SaveRules.PartySlotCount;
                slotIndex++)
            {
                characterSlotIds.Add(SaveRules.EmptyId);
            }

            return new PartyPresetSaveData
            {
                PresetId = SaveRules.GetDefaultPartyPresetId(presetIndex),
                CharacterSlotIds = characterSlotIds
            };
        }
    }
}
