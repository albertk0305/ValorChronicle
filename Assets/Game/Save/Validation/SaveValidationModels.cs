using System;
using System.Collections.Generic;
using System.Linq;

namespace ValorChronicle.Save.Validation
{
    /// <summary>Describes the impact of a save validation issue.</summary>
    public enum SaveValidationSeverity
    {
        Warning,
        RecoverableError,
        FatalError
    }

    /// <summary>Stably identifies a save validation rule.</summary>
    public enum SaveValidationCode
    {
        UnexpectedSaveVersion,
        MissingProfileId,
        InvalidCreatedTimestamp,
        InvalidLastSavedTimestamp,
        LastSavedBeforeCreated,
        NegativeCurrency,
        MissingCharacterId,
        DuplicateCharacterId,
        CharacterNotFound,
        CharacterLevelOutOfRange,
        CharacterAwakeningOutOfRange,
        MissingRelicInstanceId,
        DuplicateRelicInstanceId,
        MissingRelicDefinitionId,
        RelicDefinitionNotFound,
        RelicAwakeningOutOfRange,
        InvalidRelicEquipPair,
        InvalidRelicSlot,
        RelicEquippedToUnknownCharacter,
        RelicEquippedToUnownedCharacter,
        RelicSlotCollision,
        DuplicateRelicDefinitionEquipped,
        MissingPartyPresetId,
        DuplicatePartyPresetId,
        InvalidActivePresetIndex,
        InvalidPartySlotCount,
        DuplicatePartyCharacter,
        PartyCharacterNotOwned,
        PartyCharacterNotFound,
        MissingGachaId,
        DuplicateGachaId,
        GachaNotFound,
        NegativeGachaPity,
        NegativeGachaTotal,
        GachaPityExceedsTotal,
        MissingBossId,
        MissingDifficultyId,
        DuplicateBossRecord,
        BossNotFound,
        BossDifficultyNotFound,
        NegativeBossScore,
        InvalidBossAttemptState,
        InvalidBossClearState,
        InvalidBestDefeatTurn,
        NegativeBestRemainingTurns,
        EmptyRewardGradeId,
        DuplicateRewardGradeId,
        RewardGradeNotFound,
        EmptyUnlockedContentId,
        DuplicateUnlockedContentId,
        EmptyTutorialId,
        DuplicateTutorialId,
        ReferenceCatalogUnavailable
    }

    /// <summary>Represents one immutable validation finding.</summary>
    public sealed class SaveValidationIssue
    {
        /// <summary>Creates an immutable validation finding.</summary>
        public SaveValidationIssue(
            SaveValidationCode code,
            SaveValidationSeverity severity,
            string fieldPath,
            string message,
            bool canAutoCorrect,
            bool wasAutoCorrected = false)
        {
            Code = code;
            Severity = severity;
            FieldPath = fieldPath ?? string.Empty;
            Message = message ?? string.Empty;
            CanAutoCorrect = canAutoCorrect;
            WasAutoCorrected = wasAutoCorrected;
        }

        /// <summary>Gets the stable rule code.</summary>
        public SaveValidationCode Code { get; }
        /// <summary>Gets the issue severity.</summary>
        public SaveValidationSeverity Severity { get; }
        /// <summary>Gets the DTO field path.</summary>
        public string FieldPath { get; }
        /// <summary>Gets the diagnostic message intended for humans.</summary>
        public string Message { get; }
        /// <summary>Gets whether the issue belongs to the repair allowlist.</summary>
        public bool CanAutoCorrect { get; }
        /// <summary>Gets whether a repair pass resolved this finding.</summary>
        public bool WasAutoCorrected { get; }

        internal SaveValidationIssue MarkAutoCorrected()
        {
            return new SaveValidationIssue(Code, Severity, FieldPath, Message, CanAutoCorrect, true);
        }
    }

    /// <summary>Aggregates all findings from a validation pass.</summary>
    public sealed class SaveValidationReport
    {
        private readonly List<SaveValidationIssue> issues = new List<SaveValidationIssue>();

        /// <summary>Gets all findings in deterministic discovery order.</summary>
        public IReadOnlyList<SaveValidationIssue> Issues => issues;
        /// <summary>Gets whether warnings are present.</summary>
        public bool HasWarnings => issues.Any(issue => issue.Severity == SaveValidationSeverity.Warning);
        /// <summary>Gets whether recoverable errors are present.</summary>
        public bool HasRecoverableErrors => issues.Any(issue => issue.Severity == SaveValidationSeverity.RecoverableError);
        /// <summary>Gets whether fatal errors are present.</summary>
        public bool HasFatalErrors => issues.Any(issue => issue.Severity == SaveValidationSeverity.FatalError);
        /// <summary>Gets whether any finding was marked as auto-corrected.</summary>
        public bool WasModified => issues.Any(issue => issue.WasAutoCorrected);

        /// <summary>Returns whether a finding with the specified code exists.</summary>
        public bool Contains(SaveValidationCode code) => issues.Any(issue => issue.Code == code);

        /// <summary>Returns findings matching both stable code and exact field path.</summary>
        public IEnumerable<SaveValidationIssue> Find(SaveValidationCode code, string fieldPath)
        {
            return issues.Where(issue => issue.Code == code && issue.FieldPath == fieldPath);
        }

        /// <summary>Adds one finding to the report.</summary>
        public void Add(SaveValidationIssue issue)
        {
            if (issue == null)
            {
                throw new ArgumentNullException(nameof(issue));
            }

            issues.Add(issue);
        }

        /// <summary>Adds findings to the report in enumeration order.</summary>
        public void AddRange(IEnumerable<SaveValidationIssue> findings)
        {
            if (findings == null)
            {
                throw new ArgumentNullException(nameof(findings));
            }

            foreach (SaveValidationIssue issue in findings)
            {
                Add(issue);
            }
        }
    }
}
