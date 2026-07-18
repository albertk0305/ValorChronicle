using System;
using System.Collections.Generic;
using System.Linq;
using ValorChronicle.Save.Copying;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;
using ValorChronicle.Save.Validation;

namespace ValorChronicle.Save.Sanitization
{
    /// <summary>Contains a sanitized deep copy and the findings observed before sanitization.</summary>
    public sealed class SaveSanitizationResult
    {
        /// <summary>Creates a sanitization result.</summary>
        public SaveSanitizationResult(ProfileSaveData profile, SaveValidationReport report)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            Report = report ?? throw new ArgumentNullException(nameof(report));
        }

        /// <summary>Gets the sanitized deep copy.</summary>
        public ProfileSaveData Profile { get; }
        /// <summary>Gets the pre-repair findings, with resolved issues marked as corrected.</summary>
        public SaveValidationReport Report { get; }
        /// <summary>Gets whether at least one allowlisted repair was applied.</summary>
        public bool WasModified => Report.WasModified;
    }

    /// <summary>Applies only explicitly allowlisted repairs to a deep copy of current-version data.</summary>
    public sealed class SaveSanitizer
    {
        private readonly SaveDataCloner cloner;
        private readonly SaveStructuralValidator structuralValidator;
        private readonly SaveReferenceValidator referenceValidator;

        /// <summary>Creates a sanitizer with explicit copy and validation dependencies.</summary>
        public SaveSanitizer(SaveDataCloner cloner, SaveStructuralValidator structuralValidator, SaveReferenceValidator referenceValidator)
        {
            this.cloner = cloner ?? throw new ArgumentNullException(nameof(cloner));
            this.structuralValidator = structuralValidator ?? throw new ArgumentNullException(nameof(structuralValidator));
            this.referenceValidator = referenceValidator ?? throw new ArgumentNullException(nameof(referenceValidator));
        }

        /// <summary>Returns a structurally sanitized current-version deep copy.</summary>
        public SaveSanitizationResult SanitizeStructure(ProfileSaveData source)
        {
            EnsureCurrentVersion(source);
            SaveValidationReport before = structuralValidator.Validate(source);
            ProfileSaveData result = cloner.Clone(source);

            result.CreatedAtUtcUnixSeconds = Math.Max(0, result.CreatedAtUtcUnixSeconds);
            result.LastSavedAtUtcUnixSeconds = Math.Max(0, result.LastSavedAtUtcUnixSeconds);
            if (result.LastSavedAtUtcUnixSeconds < result.CreatedAtUtcUnixSeconds)
                result.LastSavedAtUtcUnixSeconds = result.CreatedAtUtcUnixSeconds;
            SanitizeCurrencies(result.Currencies);
            SanitizeCharacters(result.Characters);
            SanitizeRelics(result.RelicInstances);
            SanitizeParty(result.Party);
            SanitizeGacha(result.GachaStates);
            SanitizeBosses(result.BossRecords);
            RemoveEmptyAndDuplicates(result.UnlockedContentIds);
            RemoveEmptyAndDuplicates(result.CompletedTutorialIds);

            return new SaveSanitizationResult(result, MarkResolved(before, structuralValidator.Validate(result)));
        }

        /// <summary>Returns a deep copy with only safely detachable broken connections removed.</summary>
        public SaveSanitizationResult SanitizeReferences(ProfileSaveData source, ISaveContentCatalog catalog)
        {
            EnsureCurrentVersion(source);
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            SaveValidationReport before = referenceValidator.Validate(source, catalog);
            ProfileSaveData result = cloner.Clone(source);
            var owned = new HashSet<string>(
                result.Characters.Where(item => item != null && !string.IsNullOrWhiteSpace(item.CharacterId)).Select(item => item.CharacterId),
                StringComparer.Ordinal);

            if (result.Party?.Presets != null)
            {
                foreach (PartyPresetSaveData preset in result.Party.Presets)
                {
                    if (preset?.CharacterSlotIds == null) continue;
                    for (int i = 0; i < preset.CharacterSlotIds.Count; i++)
                    {
                        string id = preset.CharacterSlotIds[i];
                        if (!string.IsNullOrEmpty(id) && (!owned.Contains(id) || catalog.LookupCharacter(id) == SaveContentLookupResult.Missing))
                            preset.CharacterSlotIds[i] = SaveRules.EmptyId;
                    }
                }
            }

            if (result.RelicInstances != null)
            {
                foreach (RelicInstanceSaveData relic in result.RelicInstances)
                {
                    if (relic == null || string.IsNullOrEmpty(relic.EquippedCharacterId)) continue;
                    if (!owned.Contains(relic.EquippedCharacterId) || catalog.LookupCharacter(relic.EquippedCharacterId) == SaveContentLookupResult.Missing)
                        Unequip(relic);
                }
            }

            return new SaveSanitizationResult(result, MarkResolved(before, referenceValidator.Validate(result, catalog)));
        }

        private static void EnsureCurrentVersion(ProfileSaveData source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source.SaveVersion != SaveRules.CurrentSaveVersion)
                throw new ArgumentException("Only current-version profiles can be sanitized.", nameof(source));
        }

        private static void SanitizeCurrencies(CurrencySaveData data)
        {
            if (data == null) return;
            data.GachaCurrency = Math.Max(0, data.GachaCurrency);
            data.BattleRecords = Math.Max(0, data.BattleRecords);
            data.HeroTokens = Math.Max(0, data.HeroTokens);
            data.RelicTokens = Math.Max(0, data.RelicTokens);
        }

        private static void SanitizeCharacters(IList<CharacterSaveData> characters)
        {
            if (characters == null) return;
            foreach (CharacterSaveData character in characters)
            {
                if (character == null) continue;
                character.Level = Clamp(character.Level, SaveRules.CharacterMinLevel, SaveRules.CharacterMaxLevel);
                character.Awakening = Clamp(character.Awakening, SaveRules.CharacterMinAwakening, SaveRules.CharacterMaxAwakening);
            }
        }

        private static void SanitizeRelics(IList<RelicInstanceSaveData> relics)
        {
            if (relics == null) return;
            var slots = new HashSet<string>(StringComparer.Ordinal);
            var definitions = new HashSet<string>(StringComparer.Ordinal);
            foreach (RelicInstanceSaveData relic in relics)
            {
                if (relic == null) continue;
                relic.Awakening = Clamp(relic.Awakening, SaveRules.RelicMinAwakening, SaveRules.RelicMaxAwakening);
                bool hasCharacter = !string.IsNullOrEmpty(relic.EquippedCharacterId);
                bool validSlot = relic.EquippedSlotIndex >= SaveRules.EquippedRelicSlotMinIndex && relic.EquippedSlotIndex <= SaveRules.EquippedRelicSlotMaxIndex;
                bool validUnequipped = !hasCharacter && relic.EquippedSlotIndex == SaveRules.UnequippedRelicSlotIndex;
                if (!validUnequipped && (!hasCharacter || !validSlot))
                {
                    Unequip(relic);
                    continue;
                }
                if (!hasCharacter) continue;
                string slotKey = $"{relic.EquippedCharacterId}\u001f{relic.EquippedSlotIndex}";
                string definitionKey = $"{relic.EquippedCharacterId}\u001f{relic.RelicDefinitionId}";
                if (!slots.Add(slotKey) || !definitions.Add(definitionKey)) Unequip(relic);
            }
        }

        private static void SanitizeParty(PartySaveData party)
        {
            if (party?.Presets == null || party.Presets.Count == 0) return;
            if (party.ActivePresetIndex < 0 || party.ActivePresetIndex >= party.Presets.Count) party.ActivePresetIndex = 0;
            var reserved = new HashSet<string>(party.Presets.Where(item => item != null && !string.IsNullOrWhiteSpace(item.PresetId)).Select(item => item.PresetId), StringComparer.Ordinal);
            var accepted = new HashSet<string>(StringComparer.Ordinal);
            int recoveredIndex = 1;
            foreach (PartyPresetSaveData preset in party.Presets)
            {
                if (preset == null) continue;
                if (string.IsNullOrWhiteSpace(preset.PresetId) || !accepted.Add(preset.PresetId))
                {
                    string candidate;
                    do candidate = $"party_recovered_{recoveredIndex++}";
                    while (reserved.Contains(candidate));
                    preset.PresetId = candidate;
                    reserved.Add(candidate);
                    accepted.Add(candidate);
                }
                if (preset.CharacterSlotIds == null) continue;
                var characters = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < preset.CharacterSlotIds.Count; i++)
                {
                    string id = preset.CharacterSlotIds[i];
                    if (!string.IsNullOrEmpty(id) && !characters.Add(id)) preset.CharacterSlotIds[i] = SaveRules.EmptyId;
                }
            }
        }

        private static void SanitizeGacha(IList<GachaStateSaveData> states)
        {
            if (states == null) return;
            foreach (GachaStateSaveData state in states)
            {
                if (state == null) continue;
                state.PityCount = Math.Max(0, state.PityCount);
                state.TotalPullCount = Math.Max(0, state.TotalPullCount);
                if (state.PityCount > state.TotalPullCount) state.PityCount = (int)Math.Min(int.MaxValue, state.TotalPullCount);
            }
        }

        private static void SanitizeBosses(IList<BossRecordSaveData> records)
        {
            if (records == null) return;
            foreach (BossRecordSaveData record in records)
            {
                if (record == null) continue;
                record.HighScore = Math.Max(0, record.HighScore);
                record.BestRemainingTurns = Math.Max(0, record.BestRemainingTurns);
                bool hasRecord = record.HighScore != 0 || !string.IsNullOrEmpty(record.HighestGradeId) || record.BestDefeatTurn != 0 || record.BestRemainingTurns != 0 || (record.ClaimedFirstRewardGradeIds?.Count ?? 0) > 0;
                if (record.IsCleared || hasRecord) record.HasAttempted = true;
                if (!record.IsCleared)
                {
                    record.BestDefeatTurn = 0;
                    record.BestRemainingTurns = 0;
                }
                RemoveEmptyAndDuplicates(record.ClaimedFirstRewardGradeIds);
            }
        }

        private static void RemoveEmptyAndDuplicates(IList<string> values)
        {
            if (values == null) return;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = values.Count - 1; i >= 0; i--)
            {
                string value = values[i];
                if (string.IsNullOrWhiteSpace(value)) values.RemoveAt(i);
            }
            for (int i = 0; i < values.Count;)
            {
                if (!seen.Add(values[i])) values.RemoveAt(i);
                else i++;
            }
        }

        private static SaveValidationReport MarkResolved(SaveValidationReport before, SaveValidationReport after)
        {
            var result = new SaveValidationReport();
            foreach (SaveValidationIssue issue in before.Issues)
            {
                bool remains = after.Find(issue.Code, issue.FieldPath).Any();
                result.Add(issue.CanAutoCorrect && !remains ? issue.MarkAutoCorrected() : issue);
            }
            return result;
        }

        private static int Clamp(int value, int minimum, int maximum) => Math.Min(maximum, Math.Max(minimum, value));

        private static void Unequip(RelicInstanceSaveData relic)
        {
            relic.EquippedCharacterId = SaveRules.EmptyId;
            relic.EquippedSlotIndex = SaveRules.UnequippedRelicSlotIndex;
        }
    }
}
