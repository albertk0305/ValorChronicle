using System;
using ValorChronicle.Settings.DTO;
using ValorChronicle.Settings.Repository;
using ValorChronicle.Settings.Serialization;
using ValorChronicle.Settings.Validation;

namespace ValorChronicle.Settings.Services
{
    public sealed class SettingsService
    {
        private readonly object syncRoot = new object();
        private readonly ISettingsRepository repository;
        private readonly ISettingsSerializer serializer;
        private readonly SettingsValidator validator;
        private GameSettingsData current = GameSettingsData.CreateDefault();
        private bool isDirty;

        public SettingsService(
            ISettingsRepository repository,
            ISettingsSerializer serializer,
            SettingsValidator validator)
        {
            this.repository = repository;
            this.serializer = serializer;
            this.validator = validator
                ?? throw new ArgumentNullException(nameof(validator));
        }

        public event Action<SettingsChangedEvent> SettingsChanged;
        public event Action SaveRequested;

        public GameSettingsData Current
        {
            get
            {
                lock (syncRoot)
                {
                    return current.Clone();
                }
            }
        }

        public bool IsDirty
        {
            get
            {
                lock (syncRoot)
                {
                    return isDirty;
                }
            }
        }

        public static SettingsService CreateInMemoryDefaults()
        {
            return new SettingsService(null, null, new SettingsValidator());
        }

        public SettingsLoadResult LoadOrCreate()
        {
            if (repository == null || serializer == null)
            {
                lock (syncRoot)
                {
                    current = GameSettingsData.CreateDefault();
                    isDirty = false;
                }

                return new SettingsLoadResult(
                    SettingsLoadStatus.InMemoryFallback,
                    message: "Settings persistence is unavailable.");
            }

            TryDeleteStaleTemp();

            if (TryReadValid(() => repository.MainExists, repository.ReadMain,
                    out SettingsValidationResult main))
            {
                SetLoaded(main.Settings, main.WasModified);
                if (!main.WasModified)
                    return new SettingsLoadResult(SettingsLoadStatus.LoadedMain);

                SettingsWriteResult repair = Persist(rotateValidMainToBackup: false);
                return new SettingsLoadResult(
                    repair.IsSuccess
                        ? SettingsLoadStatus.LoadedAndRepairedMain
                        : SettingsLoadStatus.LoadedAndRepairedMainButWriteFailed,
                    repair,
                    repair.IsSuccess ? main.Message : repair.Message,
                    repair.Exception);
            }

            if (TryReadValid(() => repository.BackupExists, repository.ReadBackup,
                    out SettingsValidationResult backup))
            {
                SetLoaded(backup.Settings, dirty: true);
                SettingsWriteResult recovery = Persist(rotateValidMainToBackup: false);
                return new SettingsLoadResult(
                    recovery.IsSuccess
                        ? SettingsLoadStatus.RecoveredFromBackup
                        : SettingsLoadStatus.RecoveredFromBackupButMainRepairFailed,
                    recovery,
                    recovery.Message,
                    recovery.Exception);
            }

            bool hadStoredCandidate = SafeExists(() => repository.MainExists)
                || SafeExists(() => repository.BackupExists);
            SetLoaded(GameSettingsData.CreateDefault(), dirty: true);
            SettingsWriteResult defaultsWrite = Persist(
                rotateValidMainToBackup: false);
            SettingsLoadStatus status;
            if (!defaultsWrite.IsSuccess)
            {
                status = SettingsLoadStatus.RecoveredDefaultsButWriteFailed;
            }
            else
            {
                status = hadStoredCandidate
                    ? SettingsLoadStatus.RecoveredDefaults
                    : SettingsLoadStatus.CreatedDefaults;
            }

            return new SettingsLoadResult(
                status,
                defaultsWrite,
                defaultsWrite.Message,
                defaultsWrite.Exception);
        }

        public bool SetMasterVolume(int value)
        {
            return Change(
                SettingsChangeKind.MasterVolume,
                data => data.MasterVolume,
                (data, normalized) => data.MasterVolume = normalized,
                SettingsValidator.NormalizeVolume(value));
        }

        public bool SetBgmVolume(int value)
        {
            return Change(
                SettingsChangeKind.BgmVolume,
                data => data.BgmVolume,
                (data, normalized) => data.BgmVolume = normalized,
                SettingsValidator.NormalizeVolume(value));
        }

        public bool SetSfxVolume(int value)
        {
            return Change(
                SettingsChangeKind.SfxVolume,
                data => data.SfxVolume,
                (data, normalized) => data.SfxVolume = normalized,
                SettingsValidator.NormalizeVolume(value));
        }

        public bool SetLocale(string localeId)
        {
            string normalized = SettingsValidator.NormalizeLocaleId(localeId);
            return Change(
                SettingsChangeKind.LocaleId,
                data => data.LocaleId,
                (data, value) => data.LocaleId = value,
                normalized);
        }

        public bool SetBattleSpeed(int value)
        {
            return Change(
                SettingsChangeKind.BattleSpeed,
                data => data.BattleSpeed,
                (data, normalized) => data.BattleSpeed = normalized,
                SettingsValidator.NormalizeBattleSpeed(value));
        }

        public void RequestSave()
        {
            if (IsDirty)
                SaveRequested?.Invoke();
        }

        public SettingsWriteResult Flush()
        {
            lock (syncRoot)
            {
                if (!isDirty)
                    return new SettingsWriteResult(SettingsWriteStatus.NoChanges);
            }

            return Persist(rotateValidMainToBackup: true);
        }

        private bool Change<T>(
            SettingsChangeKind kind,
            Func<GameSettingsData, T> getter,
            Action<GameSettingsData, T> setter,
            T value)
        {
            GameSettingsData snapshot;
            lock (syncRoot)
            {
                if (Equals(getter(current), value))
                    return false;

                GameSettingsData next = current.Clone();
                setter(next, value);
                current = next;
                isDirty = true;
                snapshot = current.Clone();
            }

            try
            {
                SettingsChanged?.Invoke(new SettingsChangedEvent(kind, snapshot));
            }
            finally
            {
                SaveRequested?.Invoke();
            }

            return true;
        }

        private SettingsWriteResult Persist(bool rotateValidMainToBackup)
        {
            if (repository == null || serializer == null)
            {
                return new SettingsWriteResult(
                    SettingsWriteStatus.PersistenceUnavailable,
                    "Settings persistence is unavailable.");
            }

            GameSettingsData snapshot;
            lock (syncRoot)
            {
                snapshot = current.Clone();
            }

            string json;
            try
            {
                json = serializer.Serialize(snapshot);
            }
            catch (Exception exception)
            {
                return Failure(SettingsWriteStatus.SerializationFailed,
                    "Settings serialization failed.", exception);
            }

            if (rotateValidMainToBackup && HasFullyValidMain())
            {
                try
                {
                    repository.CopyMainToBackup();
                }
                catch (Exception exception)
                {
                    return Failure(SettingsWriteStatus.BackupRotationFailed,
                        "Valid settings backup rotation failed.", exception);
                }
            }

            try
            {
                repository.WriteTemp(json);
            }
            catch (Exception exception)
            {
                return Failure(SettingsWriteStatus.TempWriteFailed,
                    "Temporary settings write failed.", exception);
            }

            string tempJson;
            try
            {
                tempJson = repository.ReadTemp();
            }
            catch (Exception exception)
            {
                return Failure(SettingsWriteStatus.TempReadFailed,
                    "Temporary settings verification read failed.", exception);
            }

            try
            {
                GameSettingsData roundTrip = serializer.Deserialize(tempJson);
                SettingsValidationResult validation = validator.Validate(roundTrip);
                if (!validation.IsValid
                    || validation.WasModified
                    || !SettingsValidator.AreEqual(snapshot, validation.Settings))
                {
                    return new SettingsWriteResult(
                        SettingsWriteStatus.TempVerificationFailed,
                        "Temporary settings content did not pass verification.");
                }
            }
            catch (Exception exception)
            {
                return Failure(SettingsWriteStatus.TempVerificationFailed,
                    "Temporary settings content could not be verified.", exception);
            }

            try
            {
                repository.PromoteTempToMain();
            }
            catch (Exception exception)
            {
                return Failure(SettingsWriteStatus.PromoteFailed,
                    "Temporary settings promotion failed.", exception);
            }

            lock (syncRoot)
            {
                if (SettingsValidator.AreEqual(current, snapshot))
                    isDirty = false;
            }

            return new SettingsWriteResult(SettingsWriteStatus.Success);
        }

        private bool HasFullyValidMain()
        {
            return TryReadValid(() => repository.MainExists, repository.ReadMain,
                    out SettingsValidationResult validation)
                && !validation.WasModified;
        }

        private bool TryReadValid(
            Func<bool> exists,
            Func<string> read,
            out SettingsValidationResult validation)
        {
            validation = null;
            try
            {
                if (!exists())
                    return false;

                validation = validator.Validate(serializer.Deserialize(read()));
                return validation.IsValid;
            }
            catch
            {
                return false;
            }
        }

        private static bool SafeExists(Func<bool> exists)
        {
            try
            {
                return exists();
            }
            catch
            {
                return false;
            }
        }

        private void SetLoaded(GameSettingsData settings, bool dirty)
        {
            lock (syncRoot)
            {
                current = settings.Clone();
                isDirty = dirty;
            }
        }

        private void TryDeleteStaleTemp()
        {
            try
            {
                repository.DeleteTempIfExists();
            }
            catch
            {
                // A later verified temp write remains the authoritative attempt.
            }
        }

        private static SettingsWriteResult Failure(
            SettingsWriteStatus status,
            string message,
            Exception exception)
        {
            return new SettingsWriteResult(status, message, exception);
        }
    }
}
