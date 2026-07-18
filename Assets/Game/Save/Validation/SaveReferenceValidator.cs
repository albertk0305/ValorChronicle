using System;
using System.Collections.Generic;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Save.Validation
{
    /// <summary>Validates save references through an abstract content catalog.</summary>
    public sealed class SaveReferenceValidator
    {
        /// <summary>Collects all supported catalog and ownership findings without modifying the profile.</summary>
        public SaveValidationReport Validate(ProfileSaveData profile, ISaveContentCatalog catalog)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));

            var report = new SaveValidationReport();
            var unavailable = new HashSet<string>(StringComparer.Ordinal);
            var ownedCharacters = GetOwnedCharacters(profile.Characters);

            ValidateOwnedCharacters(profile.Characters, catalog, report, unavailable);
            ValidateRelics(profile.RelicInstances, ownedCharacters, catalog, report, unavailable);
            ValidateParty(profile.Party, ownedCharacters, catalog, report, unavailable);
            ValidateGacha(profile.GachaStates, catalog, report, unavailable);
            ValidateBosses(profile.BossRecords, catalog, report, unavailable);
            return report;
        }

        private static HashSet<string> GetOwnedCharacters(IList<CharacterSaveData> characters)
        {
            var owned = new HashSet<string>(StringComparer.Ordinal);
            if (characters == null) return owned;
            foreach (CharacterSaveData character in characters)
                if (character != null && !string.IsNullOrWhiteSpace(character.CharacterId)) owned.Add(character.CharacterId);
            return owned;
        }

        private static void ValidateOwnedCharacters(IList<CharacterSaveData> characters, ISaveContentCatalog catalog, SaveValidationReport report, ISet<string> unavailable)
        {
            if (characters == null) return;
            for (int i = 0; i < characters.Count; i++)
            {
                string id = characters[i]?.CharacterId;
                if (string.IsNullOrWhiteSpace(id)) continue;
                Check(catalog.LookupCharacter(id), "Character", SaveValidationCode.CharacterNotFound, SaveValidationSeverity.FatalError, $"Characters[{i}].CharacterId", false, report, unavailable);
            }
        }

        private static void ValidateRelics(IList<RelicInstanceSaveData> relics, ISet<string> owned, ISaveContentCatalog catalog, SaveValidationReport report, ISet<string> unavailable)
        {
            if (relics == null) return;
            for (int i = 0; i < relics.Count; i++)
            {
                RelicInstanceSaveData relic = relics[i];
                if (relic == null) continue;
                if (!string.IsNullOrWhiteSpace(relic.RelicDefinitionId))
                    Check(catalog.LookupRelic(relic.RelicDefinitionId), "Relic", SaveValidationCode.RelicDefinitionNotFound, SaveValidationSeverity.FatalError, $"RelicInstances[{i}].RelicDefinitionId", false, report, unavailable);
                if (string.IsNullOrEmpty(relic.EquippedCharacterId)) continue;
                if (!owned.Contains(relic.EquippedCharacterId))
                    Add(report, SaveValidationCode.RelicEquippedToUnownedCharacter, SaveValidationSeverity.RecoverableError, $"RelicInstances[{i}].EquippedCharacterId", true);
                Check(catalog.LookupCharacter(relic.EquippedCharacterId), "Character", SaveValidationCode.RelicEquippedToUnknownCharacter, SaveValidationSeverity.RecoverableError, $"RelicInstances[{i}].EquippedCharacterId", true, report, unavailable);
            }
        }

        private static void ValidateParty(PartySaveData party, ISet<string> owned, ISaveContentCatalog catalog, SaveValidationReport report, ISet<string> unavailable)
        {
            if (party?.Presets == null) return;
            for (int presetIndex = 0; presetIndex < party.Presets.Count; presetIndex++)
            {
                IList<string> slots = party.Presets[presetIndex]?.CharacterSlotIds;
                if (slots == null) continue;
                for (int slotIndex = 0; slotIndex < slots.Count; slotIndex++)
                {
                    string id = slots[slotIndex];
                    if (string.IsNullOrEmpty(id)) continue;
                    string path = $"Party.Presets[{presetIndex}].CharacterSlotIds[{slotIndex}]";
                    if (!owned.Contains(id)) Add(report, SaveValidationCode.PartyCharacterNotOwned, SaveValidationSeverity.RecoverableError, path, true);
                    Check(catalog.LookupCharacter(id), "Character", SaveValidationCode.PartyCharacterNotFound, SaveValidationSeverity.RecoverableError, path, true, report, unavailable);
                }
            }
        }

        private static void ValidateGacha(IList<GachaStateSaveData> states, ISaveContentCatalog catalog, SaveValidationReport report, ISet<string> unavailable)
        {
            if (states == null) return;
            for (int i = 0; i < states.Count; i++)
            {
                string id = states[i]?.GachaId;
                if (!string.IsNullOrWhiteSpace(id))
                    Check(catalog.LookupGacha(id), "Gacha", SaveValidationCode.GachaNotFound, SaveValidationSeverity.FatalError, $"GachaStates[{i}].GachaId", false, report, unavailable);
            }
        }

        private static void ValidateBosses(IList<BossRecordSaveData> records, ISaveContentCatalog catalog, SaveValidationReport report, ISet<string> unavailable)
        {
            if (records == null) return;
            for (int i = 0; i < records.Count; i++)
            {
                BossRecordSaveData record = records[i];
                if (record == null || string.IsNullOrWhiteSpace(record.BossId)) continue;
                Check(catalog.LookupBoss(record.BossId), "Boss", SaveValidationCode.BossNotFound, SaveValidationSeverity.FatalError, $"BossRecords[{i}].BossId", false, report, unavailable);
                if (!string.IsNullOrWhiteSpace(record.DifficultyId))
                    Check(catalog.LookupBossDifficulty(record.BossId, record.DifficultyId), "BossDifficulty", SaveValidationCode.BossDifficultyNotFound, SaveValidationSeverity.FatalError, $"BossRecords[{i}].DifficultyId", false, report, unavailable);
                if (record.ClaimedFirstRewardGradeIds == null) continue;
                for (int gradeIndex = 0; gradeIndex < record.ClaimedFirstRewardGradeIds.Count; gradeIndex++)
                {
                    string gradeId = record.ClaimedFirstRewardGradeIds[gradeIndex];
                    if (!string.IsNullOrWhiteSpace(gradeId))
                        Check(catalog.LookupRewardGrade(record.BossId, record.DifficultyId, gradeId), "RewardGrade", SaveValidationCode.RewardGradeNotFound, SaveValidationSeverity.FatalError, $"BossRecords[{i}].ClaimedFirstRewardGradeIds[{gradeIndex}]", false, report, unavailable);
                }
            }
        }

        private static void Check(SaveContentLookupResult result, string kind, SaveValidationCode missingCode, SaveValidationSeverity missingSeverity, string path, bool canCorrect, SaveValidationReport report, ISet<string> unavailable)
        {
            if (result == SaveContentLookupResult.Missing)
                Add(report, missingCode, missingSeverity, path, canCorrect);
            else if (result == SaveContentLookupResult.Unavailable && unavailable.Add(kind))
                Add(report, SaveValidationCode.ReferenceCatalogUnavailable, SaveValidationSeverity.Warning, $"Catalog.{kind}", false);
        }

        private static void Add(SaveValidationReport report, SaveValidationCode code, SaveValidationSeverity severity, string path, bool canCorrect)
        {
            report.Add(new SaveValidationIssue(code, severity, path, code.ToString(), canCorrect));
        }
    }
}
