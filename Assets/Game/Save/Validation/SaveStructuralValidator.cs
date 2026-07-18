using System;
using System.Collections.Generic;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Save.Validation
{
    /// <summary>Validates profile structure and value relationships without content lookup.</summary>
    public sealed class SaveStructuralValidator
    {
        /// <summary>Collects all structural findings without modifying the profile.</summary>
        public SaveValidationReport Validate(ProfileSaveData profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var report = new SaveValidationReport();
            ValidateProfile(profile, report);
            ValidateCurrencies(profile.Currencies, report);
            ValidateCharacters(profile.Characters, report);
            ValidateRelics(profile.RelicInstances, report);
            ValidateParty(profile.Party, report);
            ValidateGacha(profile.GachaStates, report);
            ValidateBosses(profile.BossRecords, report);
            ValidateStringList(profile.UnlockedContentIds, "UnlockedContentIds", SaveValidationCode.EmptyUnlockedContentId, SaveValidationCode.DuplicateUnlockedContentId, report);
            ValidateStringList(profile.CompletedTutorialIds, "CompletedTutorialIds", SaveValidationCode.EmptyTutorialId, SaveValidationCode.DuplicateTutorialId, report);
            return report;
        }

        private static void ValidateProfile(ProfileSaveData profile, SaveValidationReport report)
        {
            if (profile.SaveVersion != SaveRules.CurrentSaveVersion)
                Add(report, SaveValidationCode.UnexpectedSaveVersion, SaveValidationSeverity.FatalError, "SaveVersion", false);
            if (string.IsNullOrWhiteSpace(profile.ProfileId))
                Add(report, SaveValidationCode.MissingProfileId, SaveValidationSeverity.FatalError, "ProfileId", false);
            if (profile.CreatedAtUtcUnixSeconds < 0)
                Add(report, SaveValidationCode.InvalidCreatedTimestamp, SaveValidationSeverity.RecoverableError, "CreatedAtUtcUnixSeconds", true);
            if (profile.LastSavedAtUtcUnixSeconds < 0)
                Add(report, SaveValidationCode.InvalidLastSavedTimestamp, SaveValidationSeverity.RecoverableError, "LastSavedAtUtcUnixSeconds", true);
            if (profile.LastSavedAtUtcUnixSeconds < profile.CreatedAtUtcUnixSeconds)
                Add(report, SaveValidationCode.LastSavedBeforeCreated, SaveValidationSeverity.RecoverableError, "LastSavedAtUtcUnixSeconds", true);
        }

        private static void ValidateCurrencies(CurrencySaveData currencies, SaveValidationReport report)
        {
            if (currencies == null) return;
            CheckNegativeCurrency(currencies.GachaCurrency, "Currencies.GachaCurrency", report);
            CheckNegativeCurrency(currencies.BattleRecords, "Currencies.BattleRecords", report);
            CheckNegativeCurrency(currencies.HeroTokens, "Currencies.HeroTokens", report);
            CheckNegativeCurrency(currencies.RelicTokens, "Currencies.RelicTokens", report);
        }

        private static void CheckNegativeCurrency(long value, string path, SaveValidationReport report)
        {
            if (value < 0) Add(report, SaveValidationCode.NegativeCurrency, SaveValidationSeverity.RecoverableError, path, true);
        }

        private static void ValidateCharacters(IList<CharacterSaveData> characters, SaveValidationReport report)
        {
            if (characters == null) return;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < characters.Count; i++)
            {
                CharacterSaveData character = characters[i];
                if (character == null) continue;
                string prefix = $"Characters[{i}]";
                if (string.IsNullOrWhiteSpace(character.CharacterId))
                    Add(report, SaveValidationCode.MissingCharacterId, SaveValidationSeverity.FatalError, $"{prefix}.CharacterId", false);
                else if (!ids.Add(character.CharacterId))
                    Add(report, SaveValidationCode.DuplicateCharacterId, SaveValidationSeverity.FatalError, $"{prefix}.CharacterId", false);
                if (character.Level < SaveRules.CharacterMinLevel || character.Level > SaveRules.CharacterMaxLevel)
                    Add(report, SaveValidationCode.CharacterLevelOutOfRange, SaveValidationSeverity.RecoverableError, $"{prefix}.Level", true);
                if (character.Awakening < SaveRules.CharacterMinAwakening || character.Awakening > SaveRules.CharacterMaxAwakening)
                    Add(report, SaveValidationCode.CharacterAwakeningOutOfRange, SaveValidationSeverity.RecoverableError, $"{prefix}.Awakening", true);
            }
        }

        private static void ValidateRelics(IList<RelicInstanceSaveData> relics, SaveValidationReport report)
        {
            if (relics == null) return;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var slots = new HashSet<string>(StringComparer.Ordinal);
            var definitions = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < relics.Count; i++)
            {
                RelicInstanceSaveData relic = relics[i];
                if (relic == null) continue;
                string prefix = $"RelicInstances[{i}]";
                if (string.IsNullOrWhiteSpace(relic.InstanceId))
                    Add(report, SaveValidationCode.MissingRelicInstanceId, SaveValidationSeverity.FatalError, $"{prefix}.InstanceId", false);
                else if (!ids.Add(relic.InstanceId))
                    Add(report, SaveValidationCode.DuplicateRelicInstanceId, SaveValidationSeverity.FatalError, $"{prefix}.InstanceId", false);
                if (string.IsNullOrWhiteSpace(relic.RelicDefinitionId))
                    Add(report, SaveValidationCode.MissingRelicDefinitionId, SaveValidationSeverity.FatalError, $"{prefix}.RelicDefinitionId", false);
                if (relic.Awakening < SaveRules.RelicMinAwakening || relic.Awakening > SaveRules.RelicMaxAwakening)
                    Add(report, SaveValidationCode.RelicAwakeningOutOfRange, SaveValidationSeverity.RecoverableError, $"{prefix}.Awakening", true);

                bool hasCharacter = !string.IsNullOrEmpty(relic.EquippedCharacterId);
                bool isUnequippedSlot = relic.EquippedSlotIndex == SaveRules.UnequippedRelicSlotIndex;
                bool validSlot = relic.EquippedSlotIndex >= SaveRules.EquippedRelicSlotMinIndex && relic.EquippedSlotIndex <= SaveRules.EquippedRelicSlotMaxIndex;
                if (hasCharacter != !isUnequippedSlot)
                    Add(report, SaveValidationCode.InvalidRelicEquipPair, SaveValidationSeverity.RecoverableError, $"{prefix}.EquippedCharacterId", true);
                else if (hasCharacter && !validSlot)
                    Add(report, SaveValidationCode.InvalidRelicSlot, SaveValidationSeverity.RecoverableError, $"{prefix}.EquippedSlotIndex", true);

                if (!hasCharacter || !validSlot) continue;
                if (!slots.Add($"{relic.EquippedCharacterId}\u001f{relic.EquippedSlotIndex}"))
                    Add(report, SaveValidationCode.RelicSlotCollision, SaveValidationSeverity.RecoverableError, $"{prefix}.EquippedSlotIndex", true);
                if (!definitions.Add($"{relic.EquippedCharacterId}\u001f{relic.RelicDefinitionId}"))
                    Add(report, SaveValidationCode.DuplicateRelicDefinitionEquipped, SaveValidationSeverity.RecoverableError, $"{prefix}.RelicDefinitionId", true);
            }
        }

        private static void ValidateParty(PartySaveData party, SaveValidationReport report)
        {
            if (party?.Presets == null || party.Presets.Count == 0)
            {
                Add(report, SaveValidationCode.InvalidPartySlotCount, SaveValidationSeverity.RecoverableError, "Party.Presets", true);
                return;
            }
            if (party.ActivePresetIndex < 0 || party.ActivePresetIndex >= party.Presets.Count)
                Add(report, SaveValidationCode.InvalidActivePresetIndex, SaveValidationSeverity.RecoverableError, "Party.ActivePresetIndex", true);
            var presetIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < party.Presets.Count; i++)
            {
                PartyPresetSaveData preset = party.Presets[i];
                if (preset == null) continue;
                string prefix = $"Party.Presets[{i}]";
                if (string.IsNullOrWhiteSpace(preset.PresetId))
                    Add(report, SaveValidationCode.MissingPartyPresetId, SaveValidationSeverity.RecoverableError, $"{prefix}.PresetId", true);
                else if (!presetIds.Add(preset.PresetId))
                    Add(report, SaveValidationCode.DuplicatePartyPresetId, SaveValidationSeverity.RecoverableError, $"{prefix}.PresetId", true);
                if (preset.CharacterSlotIds == null || preset.CharacterSlotIds.Count != SaveRules.PartySlotCount)
                    Add(report, SaveValidationCode.InvalidPartySlotCount, SaveValidationSeverity.RecoverableError, $"{prefix}.CharacterSlotIds", true);
                if (preset.CharacterSlotIds == null) continue;
                var characterIds = new HashSet<string>(StringComparer.Ordinal);
                for (int slot = 0; slot < preset.CharacterSlotIds.Count; slot++)
                {
                    string id = preset.CharacterSlotIds[slot];
                    if (!string.IsNullOrEmpty(id) && !characterIds.Add(id))
                        Add(report, SaveValidationCode.DuplicatePartyCharacter, SaveValidationSeverity.RecoverableError, $"{prefix}.CharacterSlotIds[{slot}]", true);
                }
            }
        }

        private static void ValidateGacha(IList<GachaStateSaveData> states, SaveValidationReport report)
        {
            if (states == null) return;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < states.Count; i++)
            {
                GachaStateSaveData state = states[i];
                if (state == null) continue;
                string prefix = $"GachaStates[{i}]";
                if (string.IsNullOrWhiteSpace(state.GachaId))
                    Add(report, SaveValidationCode.MissingGachaId, SaveValidationSeverity.FatalError, $"{prefix}.GachaId", false);
                else if (!ids.Add(state.GachaId))
                    Add(report, SaveValidationCode.DuplicateGachaId, SaveValidationSeverity.FatalError, $"{prefix}.GachaId", false);
                if (state.PityCount < 0) Add(report, SaveValidationCode.NegativeGachaPity, SaveValidationSeverity.RecoverableError, $"{prefix}.PityCount", true);
                if (state.TotalPullCount < 0) Add(report, SaveValidationCode.NegativeGachaTotal, SaveValidationSeverity.RecoverableError, $"{prefix}.TotalPullCount", true);
                if (state.PityCount > state.TotalPullCount) Add(report, SaveValidationCode.GachaPityExceedsTotal, SaveValidationSeverity.RecoverableError, $"{prefix}.PityCount", true);
            }
        }

        private static void ValidateBosses(IList<BossRecordSaveData> records, SaveValidationReport report)
        {
            if (records == null) return;
            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < records.Count; i++)
            {
                BossRecordSaveData record = records[i];
                if (record == null) continue;
                string prefix = $"BossRecords[{i}]";
                if (string.IsNullOrWhiteSpace(record.BossId)) Add(report, SaveValidationCode.MissingBossId, SaveValidationSeverity.FatalError, $"{prefix}.BossId", false);
                if (string.IsNullOrWhiteSpace(record.DifficultyId)) Add(report, SaveValidationCode.MissingDifficultyId, SaveValidationSeverity.FatalError, $"{prefix}.DifficultyId", false);
                if (!string.IsNullOrWhiteSpace(record.BossId) && !string.IsNullOrWhiteSpace(record.DifficultyId) && !keys.Add($"{record.BossId}\u001f{record.DifficultyId}"))
                    Add(report, SaveValidationCode.DuplicateBossRecord, SaveValidationSeverity.FatalError, $"{prefix}.DifficultyId", false);
                if (record.HighScore < 0) Add(report, SaveValidationCode.NegativeBossScore, SaveValidationSeverity.RecoverableError, $"{prefix}.HighScore", true);
                if (record.BestRemainingTurns < 0) Add(report, SaveValidationCode.NegativeBestRemainingTurns, SaveValidationSeverity.RecoverableError, $"{prefix}.BestRemainingTurns", true);
                bool hasRecord = record.HighScore != 0 || !string.IsNullOrEmpty(record.HighestGradeId) || record.BestDefeatTurn != 0 || record.BestRemainingTurns != 0 || (record.ClaimedFirstRewardGradeIds?.Count ?? 0) > 0;
                if (!record.HasAttempted && (record.IsCleared || hasRecord)) Add(report, SaveValidationCode.InvalidBossAttemptState, SaveValidationSeverity.RecoverableError, $"{prefix}.HasAttempted", true);
                if (record.IsCleared && record.BestDefeatTurn < 1)
                    Add(report, SaveValidationCode.InvalidBestDefeatTurn, SaveValidationSeverity.FatalError, $"{prefix}.BestDefeatTurn", false);
                if (!record.IsCleared && (record.BestDefeatTurn != 0 || record.BestRemainingTurns != 0))
                    Add(report, SaveValidationCode.InvalidBossClearState, SaveValidationSeverity.RecoverableError, $"{prefix}.IsCleared", true);
                ValidateStringList(record.ClaimedFirstRewardGradeIds, $"{prefix}.ClaimedFirstRewardGradeIds", SaveValidationCode.EmptyRewardGradeId, SaveValidationCode.DuplicateRewardGradeId, report);
            }
        }

        private static void ValidateStringList(IList<string> values, string path, SaveValidationCode emptyCode, SaveValidationCode duplicateCode, SaveValidationReport report)
        {
            if (values == null) return;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < values.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(values[i])) Add(report, emptyCode, SaveValidationSeverity.RecoverableError, $"{path}[{i}]", true);
                else if (!seen.Add(values[i])) Add(report, duplicateCode, SaveValidationSeverity.RecoverableError, $"{path}[{i}]", true);
            }
        }

        private static void Add(SaveValidationReport report, SaveValidationCode code, SaveValidationSeverity severity, string path, bool canCorrect)
        {
            report.Add(new SaveValidationIssue(code, severity, path, code.ToString(), canCorrect));
        }
    }
}
