using System;
using System.Collections.Generic;
using ValorChronicle.Save.Copying;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Save.Normalization
{
    /// <summary>
    /// Normalizes the structure of a copied profile without content lookup or semantic value correction.
    /// </summary>
    public sealed class SaveNormalizer
    {
        private readonly SaveDataCloner cloner;

        /// <summary>
        /// Creates a normalizer with an explicit deep-copy dependency.
        /// </summary>
        /// <param name="cloner">The cloner used to preserve the caller's source graph.</param>
        public SaveNormalizer(SaveDataCloner cloner)
        {
            this.cloner = cloner ?? throw new ArgumentNullException(nameof(cloner));
        }

        /// <summary>
        /// Returns an idempotently normalized deep copy of the supplied profile.
        /// </summary>
        /// <remarks>
        /// The caller must reject save versions newer than <see cref="SaveRules.CurrentSaveVersion"/>
        /// before calling this method. Normalizing or saving a future-version profile can discard
        /// fields unknown to the current application.
        /// </remarks>
        /// <param name="source">The current-version profile structure to normalize.</param>
        /// <returns>A normalized deep copy.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public ProfileSaveData NormalizeCopy(ProfileSaveData source)
        {
            ProfileSaveData result = cloner.Clone(source);

            result.ProfileId ??= SaveRules.EmptyId;
            result.Currencies ??= new CurrencySaveData();
            result.Characters ??= new List<CharacterSaveData>();
            result.RelicInstances ??= new List<RelicInstanceSaveData>();
            result.Party ??= new PartySaveData();
            result.GachaStates ??= new List<GachaStateSaveData>();
            result.BossRecords ??= new List<BossRecordSaveData>();
            result.UnlockedContentIds ??= new List<string>();
            result.CompletedTutorialIds ??= new List<string>();

            NormalizeCharacters(result.Characters);
            NormalizeRelics(result.RelicInstances);
            NormalizeParty(result.Party);
            NormalizeGachaStates(result.GachaStates);
            NormalizeBossRecords(result.BossRecords);
            NormalizeStrings(result.UnlockedContentIds);
            NormalizeStrings(result.CompletedTutorialIds);

            return result;
        }

        private static void NormalizeCharacters(IList<CharacterSaveData> characters)
        {
            for (int i = 0; i < characters.Count; i++)
            {
                CharacterSaveData character = characters[i] ?? new CharacterSaveData();
                character.CharacterId ??= SaveRules.EmptyId;
                characters[i] = character;
            }
        }

        private static void NormalizeRelics(IList<RelicInstanceSaveData> relics)
        {
            for (int i = 0; i < relics.Count; i++)
            {
                RelicInstanceSaveData relic = relics[i];

                if (relic == null)
                {
                    relic = new RelicInstanceSaveData
                    {
                        EquippedCharacterId = SaveRules.EmptyId,
                        EquippedSlotIndex = SaveRules.UnequippedRelicSlotIndex
                    };
                }

                relic.InstanceId ??= SaveRules.EmptyId;
                relic.RelicDefinitionId ??= SaveRules.EmptyId;
                relic.EquippedCharacterId ??= SaveRules.EmptyId;
                relics[i] = relic;
            }
        }

        private static void NormalizeParty(PartySaveData party)
        {
            party.Presets ??= new List<PartyPresetSaveData>();
            party.LastBossId ??= SaveRules.EmptyId;
            party.LastDifficultyId ??= SaveRules.EmptyId;

            if (party.Presets.Count == 0)
            {
                party.Presets.Add(CreateDefaultPreset());
            }

            for (int i = 0; i < party.Presets.Count; i++)
            {
                PartyPresetSaveData preset = party.Presets[i] ?? new PartyPresetSaveData();
                preset.PresetId ??= SaveRules.EmptyId;
                preset.CharacterSlotIds ??= new List<string>();
                NormalizeStrings(preset.CharacterSlotIds);
                NormalizePartySlots(preset.CharacterSlotIds);
                party.Presets[i] = preset;
            }

            if (party.ActivePresetIndex < 0 || party.ActivePresetIndex >= party.Presets.Count)
            {
                party.ActivePresetIndex = 0;
            }
        }

        private static PartyPresetSaveData CreateDefaultPreset()
        {
            var slots = new List<string>(SaveRules.PartySlotCount);

            for (int i = 0; i < SaveRules.PartySlotCount; i++)
            {
                slots.Add(SaveRules.EmptyId);
            }

            return new PartyPresetSaveData
            {
                PresetId = SaveRules.DefaultPartyPresetId,
                CharacterSlotIds = slots
            };
        }

        private static void NormalizePartySlots(List<string> slots)
        {
            while (slots.Count < SaveRules.PartySlotCount)
            {
                slots.Add(SaveRules.EmptyId);
            }

            if (slots.Count > SaveRules.PartySlotCount)
            {
                slots.RemoveRange(
                    SaveRules.PartySlotCount,
                    slots.Count - SaveRules.PartySlotCount);
            }
        }

        private static void NormalizeGachaStates(IList<GachaStateSaveData> states)
        {
            for (int i = 0; i < states.Count; i++)
            {
                GachaStateSaveData state = states[i] ?? new GachaStateSaveData();
                state.GachaId ??= SaveRules.EmptyId;
                states[i] = state;
            }
        }

        private static void NormalizeBossRecords(IList<BossRecordSaveData> records)
        {
            for (int i = 0; i < records.Count; i++)
            {
                BossRecordSaveData record = records[i] ?? new BossRecordSaveData();
                record.BossId ??= SaveRules.EmptyId;
                record.DifficultyId ??= SaveRules.EmptyId;
                record.HighestGradeId ??= SaveRules.EmptyId;
                record.ClaimedFirstRewardGradeIds ??= new List<string>();
                NormalizeStrings(record.ClaimedFirstRewardGradeIds);
                records[i] = record;
            }
        }

        private static void NormalizeStrings(IList<string> values)
        {
            for (int i = 0; i < values.Count; i++)
            {
                values[i] ??= SaveRules.EmptyId;
            }
        }
    }
}
