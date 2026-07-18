using System;
using ValorChronicle.Save.Copying;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Migration;
using ValorChronicle.Save.Processing;
using ValorChronicle.Save.Repository;
using ValorChronicle.Save.Serialization;
using ValorChronicle.Save.Validation;

namespace ValorChronicle.Save.Services
{
    /// <summary>
    /// Coordinates profile loading, recovery, validation, safe persistence, and copy-based transactions.
    /// </summary>
    public sealed class SaveService
    {
        private enum SaveWriteMode
        {
            Normal,
            RestoreMainWithoutBackupRotation
        }

        private sealed class CandidateResult
        {
            public SaveCandidateStatus Status;
            public ProfileSaveData Profile;
            public bool WasModified;
            public SaveMigrationResult MigrationResult;
            public SaveValidationProcessResult ValidationResult;
            public Exception Exception;
            public string Message;
            public bool IsSuccess => Status == SaveCandidateStatus.Success && Profile != null;

            public SaveCandidateFailure ToFailure()
            {
                return IsSuccess
                    ? null
                    : new SaveCandidateFailure(
                        Status,
                        Message,
                        Exception,
                        MigrationResult,
                        ValidationResult);
            }
        }

        private readonly object syncRoot = new object();
        private readonly ISaveRepository repository;
        private readonly ISaveSerializer serializer;
        private readonly NewProfileFactory newProfileFactory;
        private readonly SaveDataCloner cloner;
        private readonly SaveMigrationRunner migrationRunner;
        private readonly SaveValidationProcessor validationProcessor;
        private readonly ISaveContentCatalog contentCatalog;
        private readonly IUnixTimeProvider timeProvider;
        private readonly SaveDataValueComparer valueComparer;

        private ProfileSaveData currentProfile;
        private bool canWriteProfile;
        private bool transactionInProgress;
        private bool saveInProgress;

        /// <summary>Creates a save service with fully injected policy and infrastructure dependencies.</summary>
        public SaveService(
            ISaveRepository repository,
            ISaveSerializer serializer,
            NewProfileFactory newProfileFactory,
            SaveDataCloner cloner,
            SaveMigrationRunner migrationRunner,
            SaveValidationProcessor validationProcessor,
            ISaveContentCatalog contentCatalog,
            IUnixTimeProvider timeProvider)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.newProfileFactory = newProfileFactory ?? throw new ArgumentNullException(nameof(newProfileFactory));
            this.cloner = cloner ?? throw new ArgumentNullException(nameof(cloner));
            this.migrationRunner = migrationRunner ?? throw new ArgumentNullException(nameof(migrationRunner));
            this.validationProcessor = validationProcessor ?? throw new ArgumentNullException(nameof(validationProcessor));
            this.contentCatalog = contentCatalog ?? throw new ArgumentNullException(nameof(contentCatalog));
            this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            valueComparer = new SaveDataValueComparer();
        }

        /// <summary>Gets whether a validated profile is currently available.</summary>
        public bool HasCurrentProfile
        {
            get { lock (syncRoot) return currentProfile != null; }
        }

        /// <summary>Gets whether the current profile accepts persistent mutations.</summary>
        public bool CanWriteCurrentProfile
        {
            get { lock (syncRoot) return currentProfile != null && canWriteProfile; }
        }

        /// <summary>Returns a new deep copy of the current profile.</summary>
        /// <exception cref="InvalidOperationException">Thrown when no usable profile is loaded.</exception>
        public ProfileSaveData GetCurrentProfileSnapshot()
        {
            lock (syncRoot)
            {
                if (currentProfile == null)
                {
                    throw new InvalidOperationException("No current profile is available.");
                }

                return cloner.Clone(currentProfile);
            }
        }

        /// <summary>Loads main, recovers backup, or creates a new profile only when both files are absent.</summary>
        public SaveLoadResult LoadOrCreate(string newProfileId)
        {
            lock (syncRoot)
            {
                if (transactionInProgress || saveInProgress)
                {
                    return LoadFailure(SaveLoadStatus.NoUsableSave, "A save operation is already active.");
                }

                currentProfile = null;
                canWriteProfile = false;

                bool mainExists;
                bool backupExists;
                try
                {
                    mainExists = repository.MainExists;
                    backupExists = repository.BackupExists;
                }
                catch (Exception exception)
                {
                    return LoadFailure(SaveLoadStatus.ReadFailed, "Save file availability could not be inspected.", exception);
                }

                if (!mainExists && !backupExists)
                {
                    return CreateAndPersistNewProfile(newProfileId);
                }

                CandidateResult main = mainExists
                    ? ProcessCandidate(repository.ReadMain)
                    : MissingCandidate("The main save does not exist.");

                if (main.Status == SaveCandidateStatus.FutureVersion)
                {
                    return LoadFailure(SaveLoadStatus.FutureVersion, main.Message, main.Exception, main.ToFailure());
                }

                if (main.Status == SaveCandidateStatus.UnsupportedOlderVersion)
                {
                    return LoadFailure(SaveLoadStatus.UnsupportedOlderVersion, main.Message, main.Exception, main.ToFailure());
                }

                if (main.IsSuccess && !main.WasModified)
                {
                    SetCurrentProfile(main.Profile, true);
                    TryDeleteStaleTemp();
                    return LoadSuccess(
                        SaveLoadStatus.LoadedMain,
                        main.Profile,
                        false,
                        false,
                        main.MigrationResult,
                        main.ValidationResult);
                }

                if (main.IsSuccess)
                {
                    SaveWriteResult repair = PersistCandidate(
                        main.Profile,
                        SaveWriteMode.Normal,
                        timeProvider.GetUtcUnixTimeSeconds(),
                        false);

                    if (repair.IsSuccess)
                    {
                        SetCurrentProfileFromPersisted(main.Profile, repair.ValidationResult, true);
                        return LoadSuccess(
                            SaveLoadStatus.LoadedAndRepairedMain,
                            currentProfile,
                            false,
                            true,
                            main.MigrationResult,
                            repair.ValidationResult);
                    }

                    if (!backupExists)
                    {
                        return LoadFailure(
                            SaveLoadStatus.WriteFailed,
                            "The repaired main save could not be persisted.",
                            repair.Exception,
                            null,
                            null,
                            main.MigrationResult,
                            repair.ValidationResult);
                    }
                }

                return RecoverBackup(main, backupExists);
            }
        }

        /// <summary>Safely persists a copy of the current profile.</summary>
        public SaveWriteResult SaveCurrentProfile()
        {
            lock (syncRoot)
            {
                if (currentProfile == null)
                    return new SaveWriteResult(SaveWriteStatus.NoCurrentProfile, message: "No current profile is available.");
                if (!canWriteProfile)
                    return new SaveWriteResult(SaveWriteStatus.ReadOnlyProfile, message: "The current profile is read-only.");
                if (transactionInProgress || saveInProgress)
                    return new SaveWriteResult(SaveWriteStatus.SaveAlreadyActive, message: "A save operation is already active.");

                saveInProgress = true;
                try
                {
                    return PersistCandidate(
                        currentProfile,
                        SaveWriteMode.Normal,
                        timeProvider.GetUtcUnixTimeSeconds(),
                        true);
                }
                catch (Exception exception)
                {
                    return new SaveWriteResult(
                        SaveWriteStatus.UnexpectedFailure,
                        exception: exception,
                        message: "An unexpected safe-save failure occurred.");
                }
                finally
                {
                    saveInProgress = false;
                }
            }
        }

        /// <summary>Mutates a detached copy and commits it only after validation and safe persistence succeed.</summary>
        public SaveTransactionResult ExecuteTransaction(Action<ProfileSaveData> mutation)
        {
            lock (syncRoot)
            {
                if (currentProfile == null)
                    return new SaveTransactionResult(SaveTransactionStatus.NoCurrentProfile, message: "No current profile is available.");
                if (!canWriteProfile)
                    return new SaveTransactionResult(SaveTransactionStatus.ReadOnlyProfile, message: "The current profile is read-only.");
                if (transactionInProgress || saveInProgress)
                    return new SaveTransactionResult(SaveTransactionStatus.TransactionAlreadyActive, message: "A transaction or save is already active.");
                if (mutation == null)
                    return new SaveTransactionResult(SaveTransactionStatus.MutationFailed, message: "The mutation callback is required.");

                transactionInProgress = true;
                try
                {
                    ProfileSaveData working = cloner.Clone(currentProfile);
                    try
                    {
                        mutation(working);
                    }
                    catch (Exception exception)
                    {
                        return new SaveTransactionResult(
                            SaveTransactionStatus.MutationThrewException,
                            exception: exception,
                            message: "The transaction mutation threw an exception.");
                    }

                    SaveValidationProcessResult validation =
                        validationProcessor.ValidateAndRepairCurrentVersion(working, contentCatalog);
                    if (!validation.CanUseProfile)
                    {
                        return new SaveTransactionResult(
                            SaveTransactionStatus.ValidationFailed,
                            validationResult: validation,
                            message: "The transaction candidate failed validation.");
                    }

                    saveInProgress = true;
                    SaveWriteResult write;
                    try
                    {
                        write = PersistCandidate(
                            validation.UsableProfile,
                            SaveWriteMode.Normal,
                            timeProvider.GetUtcUnixTimeSeconds(),
                            true);
                    }
                    finally
                    {
                        saveInProgress = false;
                    }

                    if (!write.IsSuccess)
                    {
                        return new SaveTransactionResult(
                            SaveTransactionStatus.SaveFailed,
                            validation.WasModified,
                            validation,
                            write,
                            write.Exception,
                            "The transaction candidate could not be persisted.");
                    }

                    return new SaveTransactionResult(
                        SaveTransactionStatus.Success,
                        validation.WasModified || write.WasSanitized,
                        validation,
                        write);
                }
                catch (Exception exception)
                {
                    return new SaveTransactionResult(
                        SaveTransactionStatus.UnexpectedFailure,
                        exception: exception,
                        message: "An unexpected transaction failure occurred.");
                }
                finally
                {
                    transactionInProgress = false;
                }
            }
        }

        private SaveLoadResult CreateAndPersistNewProfile(string newProfileId)
        {
            ProfileSaveData created;
            long now = timeProvider.GetUtcUnixTimeSeconds();
            try
            {
                created = newProfileFactory.Create(newProfileId, now);
            }
            catch (Exception exception)
            {
                return LoadFailure(SaveLoadStatus.NoUsableSave, "A new profile could not be created.", exception);
            }

            SaveValidationProcessResult validation =
                validationProcessor.ValidateAndRepairCurrentVersion(created, contentCatalog);
            if (!validation.CanUseProfile)
            {
                return LoadFailure(
                    SaveLoadStatus.ValidationFailed,
                    "The new profile failed validation.",
                    validationResult: validation);
            }

            SaveWriteResult write = PersistCandidate(
                validation.UsableProfile,
                SaveWriteMode.Normal,
                now,
                false);
            if (!write.IsSuccess)
            {
                return LoadFailure(
                    SaveLoadStatus.WriteFailed,
                    "The new profile could not be persisted.",
                    write.Exception,
                    validationResult: write.ValidationResult);
            }

            SetCurrentProfileFromPersisted(validation.UsableProfile, write.ValidationResult, true);
            return LoadSuccess(
                SaveLoadStatus.CreatedNewProfile,
                currentProfile,
                false,
                validation.WasModified || write.WasSanitized,
                null,
                write.ValidationResult);
        }

        private SaveLoadResult RecoverBackup(CandidateResult main, bool backupExists)
        {
            CandidateResult backup = backupExists
                ? ProcessCandidate(repository.ReadBackup)
                : MissingCandidate("The backup save does not exist.");

            if (!backup.IsSuccess)
            {
                SaveLoadStatus status = ChooseUnavailableStatus(main, backup);
                return LoadFailure(
                    status,
                    "No usable main or backup profile is available.",
                    backup.Exception ?? main.Exception,
                    main.ToFailure(),
                    backup.ToFailure(),
                    backup.MigrationResult ?? main.MigrationResult,
                    backup.ValidationResult ?? main.ValidationResult);
            }

            SaveWriteResult restore = PersistCandidate(
                backup.Profile,
                SaveWriteMode.RestoreMainWithoutBackupRotation,
                null,
                false);
            if (restore.IsSuccess)
            {
                SetCurrentProfileFromPersisted(backup.Profile, restore.ValidationResult, true);
                return new SaveLoadResult(
                    SaveLoadStatus.RecoveredFromBackup,
                    true,
                    true,
                    true,
                    backup.WasModified || restore.WasSanitized,
                    currentProfile,
                    main.ToFailure(),
                    null,
                    backup.MigrationResult,
                    restore.ValidationResult,
                    null,
                    string.Empty,
                    cloner);
            }

            ProfileSaveData readOnly = restore.ValidationResult?.UsableProfile ?? backup.Profile;
            SetCurrentProfile(readOnly, false);
            return new SaveLoadResult(
                SaveLoadStatus.RecoveredFromBackupButMainRepairFailed,
                true,
                false,
                true,
                backup.WasModified || restore.WasSanitized,
                currentProfile,
                main.ToFailure(),
                null,
                backup.MigrationResult,
                restore.ValidationResult ?? backup.ValidationResult,
                restore.Exception,
                "Backup is usable, but main could not be restored.",
                cloner);
        }

        private SaveWriteResult PersistCandidate(
            ProfileSaveData source,
            SaveWriteMode mode,
            long? lastSavedAt,
            bool replaceCurrentProfile)
        {
            ProfileSaveData candidate = cloner.Clone(source);
            if (lastSavedAt.HasValue)
            {
                candidate.LastSavedAtUtcUnixSeconds = lastSavedAt.Value;
            }

            SaveValidationProcessResult validation =
                validationProcessor.ValidateAndRepairCurrentVersion(candidate, contentCatalog);
            if (!validation.CanUseProfile)
            {
                return new SaveWriteResult(
                    SaveWriteStatus.CandidateValidationFailed,
                    validation,
                    message: "The save candidate failed validation.");
            }

            ProfileSaveData finalCandidate = cloner.Clone(validation.UsableProfile);
            string json;
            try
            {
                json = serializer.Serialize(finalCandidate);
            }
            catch (Exception exception)
            {
                return new SaveWriteResult(
                    SaveWriteStatus.SerializationFailed,
                    validation,
                    exception,
                    "The save candidate could not be serialized.");
            }

            if (mode == SaveWriteMode.Normal && repository.MainExists)
            {
                CandidateResult existingMain = ProcessCandidate(repository.ReadMain);
                if (existingMain.Status == SaveCandidateStatus.FutureVersion)
                    return new SaveWriteResult(SaveWriteStatus.ExistingMainFutureVersion, validation, existingMain.Exception, existingMain.Message);
                if (existingMain.Status == SaveCandidateStatus.UnsupportedOlderVersion)
                    return new SaveWriteResult(SaveWriteStatus.ExistingMainUnsupportedVersion, validation, existingMain.Exception, existingMain.Message);

                if (existingMain.IsSuccess && !existingMain.WasModified)
                {
                    try
                    {
                        repository.CopyMainToBackup();
                    }
                    catch (Exception exception)
                    {
                        return new SaveWriteResult(
                            SaveWriteStatus.BackupRotationFailed,
                            validation,
                            exception,
                            "The existing main save could not be rotated to backup.");
                    }
                }
            }

            try
            {
                repository.WriteTemp(json);
            }
            catch (Exception exception)
            {
                return new SaveWriteResult(SaveWriteStatus.TempWriteFailed, validation, exception, "The temporary save could not be written.");
            }

            string tempJson;
            try
            {
                tempJson = repository.ReadTemp();
            }
            catch (Exception exception)
            {
                return new SaveWriteResult(SaveWriteStatus.TempReadFailed, validation, exception, "The temporary save could not be read.");
            }

            CandidateResult temp = ProcessJson(tempJson);
            if (!temp.IsSuccess || temp.WasModified || !valueComparer.Equals(finalCandidate, temp.Profile))
            {
                return new SaveWriteResult(
                    SaveWriteStatus.TempVerificationFailed,
                    temp.ValidationResult ?? validation,
                    temp.Exception,
                    "The temporary save failed full verification.",
                    wasSanitized: validation.WasModified);
            }

            try
            {
                repository.PromoteTempToMain();
            }
            catch (Exception exception)
            {
                return new SaveWriteResult(SaveWriteStatus.PromoteFailed, validation, exception, "The temporary save could not be promoted.");
            }

            if (replaceCurrentProfile)
            {
                SetCurrentProfile(temp.Profile, true);
            }

            return new SaveWriteResult(
                SaveWriteStatus.Success,
                temp.ValidationResult,
                currentProfileChanged: replaceCurrentProfile,
                wasSanitized: validation.WasModified);
        }

        private CandidateResult ProcessCandidate(Func<string> read)
        {
            string json;
            try
            {
                json = read();
            }
            catch (Exception exception)
            {
                return FailureCandidate(SaveCandidateStatus.ReadFailed, "The save candidate could not be read.", exception);
            }

            return ProcessJson(json);
        }

        private CandidateResult ProcessJson(string json)
        {
            ProfileSaveData deserialized;
            try
            {
                deserialized = serializer.Deserialize(json);
            }
            catch (Exception exception)
            {
                return FailureCandidate(SaveCandidateStatus.DeserializationFailed, "The save candidate could not be deserialized.", exception);
            }

            SaveMigrationResult migration;
            try
            {
                migration = migrationRunner.Migrate(deserialized);
            }
            catch (Exception exception)
            {
                return FailureCandidate(SaveCandidateStatus.MigrationFailed, "The save candidate migration failed.", exception);
            }

            if (!migration.IsSuccess)
            {
                SaveCandidateStatus status = migration.Status == SaveMigrationStatus.FutureVersion
                    ? SaveCandidateStatus.FutureVersion
                    : migration.Status == SaveMigrationStatus.UnsupportedOlderVersion || migration.Status == SaveMigrationStatus.MissingMigrationStep
                        ? SaveCandidateStatus.UnsupportedOlderVersion
                        : SaveCandidateStatus.MigrationFailed;
                return new CandidateResult
                {
                    Status = status,
                    MigrationResult = migration,
                    Exception = migration.Exception,
                    Message = migration.Message
                };
            }

            SaveValidationProcessResult validation;
            try
            {
                validation = validationProcessor.ValidateAndRepairCurrentVersion(migration.Data, contentCatalog);
            }
            catch (Exception exception)
            {
                return FailureCandidate(SaveCandidateStatus.ValidationFailed, "The save candidate validation failed.", exception, migration);
            }

            if (!validation.CanUseProfile)
            {
                return new CandidateResult
                {
                    Status = SaveCandidateStatus.ValidationFailed,
                    MigrationResult = migration,
                    ValidationResult = validation,
                    Message = "The save candidate contains fatal validation findings."
                };
            }

            return new CandidateResult
            {
                Status = SaveCandidateStatus.Success,
                Profile = cloner.Clone(validation.UsableProfile),
                WasModified = validation.WasModified || migration.SourceVersion != migration.TargetVersion,
                MigrationResult = migration,
                ValidationResult = validation,
                Message = string.Empty
            };
        }

        private void SetCurrentProfileFromPersisted(
            ProfileSaveData fallback,
            SaveValidationProcessResult persistedValidation,
            bool writable)
        {
            SetCurrentProfile(persistedValidation?.UsableProfile ?? fallback, writable);
        }

        private void SetCurrentProfile(ProfileSaveData profile, bool writable)
        {
            currentProfile = cloner.Clone(profile);
            canWriteProfile = writable;
        }

        private void TryDeleteStaleTemp()
        {
            try
            {
                repository.DeleteTempIfExists();
            }
            catch
            {
                // Stale temp cleanup is best-effort and never invalidates a usable main profile.
            }
        }

        private SaveLoadResult LoadSuccess(
            SaveLoadStatus status,
            ProfileSaveData profile,
            bool recovered,
            bool repaired,
            SaveMigrationResult migration,
            SaveValidationProcessResult validation)
        {
            return new SaveLoadResult(
                status,
                true,
                true,
                recovered,
                repaired,
                profile,
                null,
                null,
                migration,
                validation,
                null,
                string.Empty,
                cloner);
        }

        private SaveLoadResult LoadFailure(
            SaveLoadStatus status,
            string message,
            Exception exception = null,
            SaveCandidateFailure mainFailure = null,
            SaveCandidateFailure backupFailure = null,
            SaveMigrationResult migrationResult = null,
            SaveValidationProcessResult validationResult = null)
        {
            return new SaveLoadResult(
                status,
                false,
                false,
                false,
                false,
                null,
                mainFailure,
                backupFailure,
                migrationResult,
                validationResult,
                exception,
                message,
                cloner);
        }

        private static SaveLoadStatus ChooseUnavailableStatus(CandidateResult main, CandidateResult backup)
        {
            if (backup.Status != SaveCandidateStatus.Missing)
                return SaveLoadStatus.MainAndBackupInvalid;
            if (main.Status == SaveCandidateStatus.ReadFailed) return SaveLoadStatus.ReadFailed;
            if (main.Status == SaveCandidateStatus.DeserializationFailed) return SaveLoadStatus.DeserializationFailed;
            if (main.Status == SaveCandidateStatus.ValidationFailed) return SaveLoadStatus.ValidationFailed;
            return SaveLoadStatus.NoUsableSave;
        }

        private static CandidateResult MissingCandidate(string message) => new CandidateResult
        {
            Status = SaveCandidateStatus.Missing,
            Message = message
        };

        private static CandidateResult FailureCandidate(
            SaveCandidateStatus status,
            string message,
            Exception exception,
            SaveMigrationResult migration = null) => new CandidateResult
        {
            Status = status,
            Message = message,
            Exception = exception,
            MigrationResult = migration
        };
    }
}
