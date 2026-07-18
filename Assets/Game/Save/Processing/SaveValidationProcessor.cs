using System;
using System.Collections.Generic;
using ValorChronicle.Save.Copying;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Normalization;
using ValorChronicle.Save.Rules;
using ValorChronicle.Save.Sanitization;
using ValorChronicle.Save.Validation;

namespace ValorChronicle.Save.Processing
{
    /// <summary>Represents the outcome of validating and repairing one current-version profile.</summary>
    public sealed class SaveValidationProcessResult
    {
        internal SaveValidationProcessResult(ProfileSaveData usableProfile, SaveValidationReport initialReport, SaveValidationReport finalReport, bool wasModified)
        {
            UsableProfile = usableProfile;
            InitialReport = initialReport ?? throw new ArgumentNullException(nameof(initialReport));
            FinalReport = finalReport ?? throw new ArgumentNullException(nameof(finalReport));
            WasModified = wasModified;
        }

        /// <summary>Gets the repaired profile only when it is safe to use; otherwise null.</summary>
        public ProfileSaveData UsableProfile { get; }
        /// <summary>Gets findings observed before each applicable repair pass.</summary>
        public SaveValidationReport InitialReport { get; }
        /// <summary>Gets findings from the final structural and reference revalidation.</summary>
        public SaveValidationReport FinalReport { get; }
        /// <summary>Gets whether normalization or an allowlisted repair changed the copy.</summary>
        public bool WasModified { get; }
        /// <summary>Gets whether fatal findings remain after processing.</summary>
        public bool HasFatalErrors => FinalReport.HasFatalErrors;
        /// <summary>Gets whether the profile is safe to expose to a caller.</summary>
        public bool CanUseProfile => UsableProfile != null && !HasFatalErrors;
    }

    /// <summary>Coordinates normalization, validation, allowlisted repair, and final revalidation.</summary>
    public sealed class SaveValidationProcessor
    {
        private readonly SaveNormalizer normalizer;
        private readonly SaveStructuralValidator structuralValidator;
        private readonly SaveReferenceValidator referenceValidator;
        private readonly SaveSanitizer sanitizer;

        /// <summary>Creates a processor with the standard save validation components.</summary>
        public SaveValidationProcessor()
        {
            var cloner = new SaveDataCloner();
            structuralValidator = new SaveStructuralValidator();
            referenceValidator = new SaveReferenceValidator();
            normalizer = new SaveNormalizer(cloner);
            sanitizer = new SaveSanitizer(cloner, structuralValidator, referenceValidator);
        }

        /// <summary>Creates a processor with explicit dependencies.</summary>
        public SaveValidationProcessor(SaveNormalizer normalizer, SaveStructuralValidator structuralValidator, SaveReferenceValidator referenceValidator, SaveSanitizer sanitizer)
        {
            this.normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer));
            this.structuralValidator = structuralValidator ?? throw new ArgumentNullException(nameof(structuralValidator));
            this.referenceValidator = referenceValidator ?? throw new ArgumentNullException(nameof(referenceValidator));
            this.sanitizer = sanitizer ?? throw new ArgumentNullException(nameof(sanitizer));
        }

        /// <summary>Validates and repairs a deep copy without performing file I/O.</summary>
        public SaveValidationProcessResult ValidateAndRepairCurrentVersion(ProfileSaveData source, ISaveContentCatalog catalog)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (source.SaveVersion != SaveRules.CurrentSaveVersion)
            {
                var versionReport = new SaveValidationReport();
                versionReport.Add(new SaveValidationIssue(
                    SaveValidationCode.UnexpectedSaveVersion,
                    SaveValidationSeverity.FatalError,
                    "SaveVersion",
                    SaveValidationCode.UnexpectedSaveVersion.ToString(),
                    false));
                return new SaveValidationProcessResult(null, versionReport, versionReport, false);
            }

            bool normalizationModified = NeedsNormalization(source);
            ProfileSaveData normalized = normalizer.NormalizeCopy(source);
            SaveSanitizationResult structural = sanitizer.SanitizeStructure(normalized);
            structuralValidator.Validate(structural.Profile);
            SaveSanitizationResult references = sanitizer.SanitizeReferences(structural.Profile, catalog);

            var initialReport = new SaveValidationReport();
            initialReport.AddRange(structural.Report.Issues);
            initialReport.AddRange(references.Report.Issues);

            var finalReport = new SaveValidationReport();
            finalReport.AddRange(structuralValidator.Validate(references.Profile).Issues);
            finalReport.AddRange(referenceValidator.Validate(references.Profile, catalog).Issues);
            bool wasModified = normalizationModified || structural.WasModified || references.WasModified;
            ProfileSaveData usable = finalReport.HasFatalErrors ? null : references.Profile;
            return new SaveValidationProcessResult(usable, initialReport, finalReport, wasModified);
        }

        private static bool NeedsNormalization(ProfileSaveData profile)
        {
            if (profile.ProfileId == null || profile.Currencies == null || profile.Characters == null ||
                profile.RelicInstances == null || profile.Party == null || profile.GachaStates == null ||
                profile.BossRecords == null || profile.UnlockedContentIds == null || profile.CompletedTutorialIds == null)
                return true;
            if (ContainsNull(profile.UnlockedContentIds) || ContainsNull(profile.CompletedTutorialIds)) return true;
            foreach (CharacterSaveData character in profile.Characters)
                if (character == null || character.CharacterId == null) return true;
            foreach (RelicInstanceSaveData relic in profile.RelicInstances)
                if (relic == null || relic.InstanceId == null || relic.RelicDefinitionId == null || relic.EquippedCharacterId == null) return true;
            foreach (GachaStateSaveData state in profile.GachaStates)
                if (state == null || state.GachaId == null) return true;
            foreach (BossRecordSaveData record in profile.BossRecords)
                if (record == null || record.BossId == null || record.DifficultyId == null || record.HighestGradeId == null || record.ClaimedFirstRewardGradeIds == null || ContainsNull(record.ClaimedFirstRewardGradeIds)) return true;
            if (profile.Party.Presets == null || profile.Party.Presets.Count == 0 || profile.Party.LastBossId == null || profile.Party.LastDifficultyId == null) return true;
            if (profile.Party.ActivePresetIndex < 0 || profile.Party.ActivePresetIndex >= profile.Party.Presets.Count) return true;
            foreach (PartyPresetSaveData preset in profile.Party.Presets)
                if (preset == null || preset.PresetId == null || preset.CharacterSlotIds == null || preset.CharacterSlotIds.Count != SaveRules.PartySlotCount || ContainsNull(preset.CharacterSlotIds)) return true;
            return false;
        }

        private static bool ContainsNull(IList<string> values)
        {
            foreach (string value in values)
                if (value == null) return true;
            return false;
        }
    }
}
