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
            var characterSlotIds = new List<string>(SaveRules.PartySlotCount);

            for (int i = 0; i < SaveRules.PartySlotCount; i++)
            {
                characterSlotIds.Add(SaveRules.EmptyId);
            }

            return new PartySaveData
            {
                ActivePresetIndex = 0,
                Presets = new List<PartyPresetSaveData>
                {
                    new PartyPresetSaveData
                    {
                        PresetId = SaveRules.DefaultPartyPresetId,
                        CharacterSlotIds = characterSlotIds
                    }
                },
                LastBossId = SaveRules.EmptyId,
                LastDifficultyId = SaveRules.EmptyId
            };
        }
    }
}
