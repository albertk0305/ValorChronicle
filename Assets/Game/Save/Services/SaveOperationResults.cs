using System;
using ValorChronicle.Save.Copying;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Migration;
using ValorChronicle.Save.Processing;

namespace ValorChronicle.Save.Services
{
    /// <summary>Describes the externally relevant outcome of loading a profile.</summary>
    public enum SaveLoadStatus
    {
        LoadedMain,
        LoadedAndRepairedMain,
        CreatedNewProfile,
        RecoveredFromBackup,
        RecoveredFromBackupButMainRepairFailed,
        NoUsableSave,
        FutureVersion,
        UnsupportedOlderVersion,
        MainAndBackupInvalid,
        ReadFailed,
        DeserializationFailed,
        ValidationFailed,
        WriteFailed
    }

    /// <summary>Describes why one on-disk save candidate could not be used.</summary>
    public enum SaveCandidateStatus
    {
        Success,
        Missing,
        ReadFailed,
        DeserializationFailed,
        FutureVersion,
        UnsupportedOlderVersion,
        MigrationFailed,
        ValidationFailed
    }

    /// <summary>Contains safe diagnostics for one main, backup, or temp candidate.</summary>
    public sealed class SaveCandidateFailure
    {
        internal SaveCandidateFailure(
            SaveCandidateStatus status,
            string message,
            Exception exception,
            SaveMigrationResult migrationResult,
            SaveValidationProcessResult validationResult)
        {
            Status = status;
            Message = message ?? string.Empty;
            Exception = exception;
            MigrationResult = migrationResult;
            ValidationResult = validationResult;
        }

        /// <summary>Gets the stable failure category.</summary>
        public SaveCandidateStatus Status { get; }
        /// <summary>Gets a diagnostic that never contains the save JSON.</summary>
        public string Message { get; }
        /// <summary>Gets the caught exception, when applicable.</summary>
        public Exception Exception { get; }
        /// <summary>Gets the migration result, when migration was attempted.</summary>
        public SaveMigrationResult MigrationResult { get; }
        /// <summary>Gets the validation result, when validation was attempted.</summary>
        public SaveValidationProcessResult ValidationResult { get; }
    }

    /// <summary>Contains the result of loading, repairing, recovering, or creating a profile.</summary>
    public sealed class SaveLoadResult
    {
        private readonly ProfileSaveData profileSnapshot;
        private readonly SaveDataCloner cloner;

        internal SaveLoadResult(
            SaveLoadStatus status,
            bool canUseProfile,
            bool canWriteProfile,
            bool wasRecoveredFromBackup,
            bool wasRepaired,
            ProfileSaveData profileSnapshot,
            SaveCandidateFailure mainFailure,
            SaveCandidateFailure backupFailure,
            SaveMigrationResult migrationResult,
            SaveValidationProcessResult validationResult,
            Exception exception,
            string message,
            SaveDataCloner cloner)
        {
            Status = status;
            CanUseProfile = canUseProfile;
            CanWriteProfile = canWriteProfile;
            WasRecoveredFromBackup = wasRecoveredFromBackup;
            WasRepaired = wasRepaired;
            this.cloner = cloner ?? throw new ArgumentNullException(nameof(cloner));
            this.profileSnapshot = profileSnapshot == null ? null : cloner.Clone(profileSnapshot);
            MainFailure = mainFailure;
            BackupFailure = backupFailure;
            MigrationResult = migrationResult;
            ValidationResult = validationResult;
            Exception = exception;
            Message = message ?? string.Empty;
        }

        /// <summary>Gets the stable load outcome.</summary>
        public SaveLoadStatus Status { get; }
        /// <summary>Gets whether the requested load operation completed successfully.</summary>
        public bool IsSuccess => Status == SaveLoadStatus.LoadedMain ||
                                 Status == SaveLoadStatus.LoadedAndRepairedMain ||
                                 Status == SaveLoadStatus.CreatedNewProfile ||
                                 Status == SaveLoadStatus.RecoveredFromBackup;
        /// <summary>Gets whether a safe profile is available for read-only gameplay.</summary>
        public bool CanUseProfile { get; }
        /// <summary>Gets whether persistent mutations are permitted.</summary>
        public bool CanWriteProfile { get; }
        /// <summary>Gets whether the profile came from the backup file.</summary>
        public bool WasRecoveredFromBackup { get; }
        /// <summary>Gets whether normalization or sanitization changed the loaded data.</summary>
        public bool WasRepaired { get; }
        /// <summary>Gets a new deep copy of the usable profile, or null when none is usable.</summary>
        public ProfileSaveData ProfileSnapshot => profileSnapshot == null ? null : cloner.Clone(profileSnapshot);
        /// <summary>Gets diagnostics for a failed main candidate.</summary>
        public SaveCandidateFailure MainFailure { get; }
        /// <summary>Gets diagnostics for a failed backup candidate.</summary>
        public SaveCandidateFailure BackupFailure { get; }
        /// <summary>Gets the most relevant migration result.</summary>
        public SaveMigrationResult MigrationResult { get; }
        /// <summary>Gets the most relevant validation result.</summary>
        public SaveValidationProcessResult ValidationResult { get; }
        /// <summary>Gets the caught operation exception, when applicable.</summary>
        public Exception Exception { get; }
        /// <summary>Gets a safe diagnostic that never contains save JSON.</summary>
        public string Message { get; }
    }

    /// <summary>Describes the outcome of a safe profile write.</summary>
    public enum SaveWriteStatus
    {
        Success,
        NoCurrentProfile,
        ReadOnlyProfile,
        SaveAlreadyActive,
        ExistingMainFutureVersion,
        ExistingMainUnsupportedVersion,
        ExistingMainInvalid,
        CandidateValidationFailed,
        SerializationFailed,
        BackupRotationFailed,
        TempWriteFailed,
        TempReadFailed,
        TempVerificationFailed,
        PromoteFailed,
        UnexpectedFailure
    }

    /// <summary>Contains the result of one safe write attempt.</summary>
    public sealed class SaveWriteResult
    {
        internal SaveWriteResult(
            SaveWriteStatus status,
            SaveValidationProcessResult validationResult = null,
            Exception exception = null,
            string message = "",
            bool currentProfileChanged = false,
            bool wasSanitized = false)
        {
            Status = status;
            ValidationResult = validationResult;
            Exception = exception;
            Message = message ?? string.Empty;
            CurrentProfileChanged = currentProfileChanged;
            WasSanitized = wasSanitized;
        }

        /// <summary>Gets the stable write outcome.</summary>
        public SaveWriteStatus Status { get; }
        /// <summary>Gets whether the write completed and main was promoted.</summary>
        public bool IsSuccess => Status == SaveWriteStatus.Success;
        /// <summary>Gets the candidate validation result.</summary>
        public SaveValidationProcessResult ValidationResult { get; }
        /// <summary>Gets the caught exception, when applicable.</summary>
        public Exception Exception { get; }
        /// <summary>Gets a safe diagnostic that never contains save JSON.</summary>
        public string Message { get; }
        /// <summary>Gets whether this operation replaced the in-memory current profile.</summary>
        public bool CurrentProfileChanged { get; }
        /// <summary>Gets whether validation repaired the persisted candidate.</summary>
        public bool WasSanitized { get; }
    }

    /// <summary>Describes the outcome of a copy-based profile transaction.</summary>
    public enum SaveTransactionStatus
    {
        Success,
        NoCurrentProfile,
        ReadOnlyProfile,
        TransactionAlreadyActive,
        MutationFailed,
        MutationThrewException,
        ValidationFailed,
        SaveFailed,
        UnexpectedFailure
    }

    /// <summary>Contains the result of one copy-based transaction.</summary>
    public sealed class SaveTransactionResult
    {
        internal SaveTransactionResult(
            SaveTransactionStatus status,
            bool wasSanitized = false,
            SaveValidationProcessResult validationResult = null,
            SaveWriteResult saveWriteResult = null,
            Exception exception = null,
            string message = "")
        {
            Status = status;
            WasSanitized = wasSanitized;
            ValidationResult = validationResult;
            SaveWriteResult = saveWriteResult;
            Exception = exception;
            Message = message ?? string.Empty;
        }

        /// <summary>Gets the stable transaction outcome.</summary>
        public SaveTransactionStatus Status { get; }
        /// <summary>Gets whether mutation, validation, and persistence all succeeded.</summary>
        public bool IsSuccess => Status == SaveTransactionStatus.Success;
        /// <summary>Gets whether the working copy was normalized or sanitized.</summary>
        public bool WasSanitized { get; }
        /// <summary>Gets the working-copy validation result.</summary>
        public SaveValidationProcessResult ValidationResult { get; }
        /// <summary>Gets the nested safe-write result.</summary>
        public SaveWriteResult SaveWriteResult { get; }
        /// <summary>Gets the caught mutation or operation exception.</summary>
        public Exception Exception { get; }
        /// <summary>Gets a safe diagnostic that never contains save JSON.</summary>
        public string Message { get; }
    }
}
