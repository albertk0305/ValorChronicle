using System;

namespace ValorChronicle.Settings.Services
{
    public enum SettingsLoadStatus
    {
        LoadedMain,
        LoadedAndRepairedMain,
        LoadedAndRepairedMainButWriteFailed,
        CreatedDefaults,
        RecoveredFromBackup,
        RecoveredFromBackupButMainRepairFailed,
        RecoveredDefaults,
        RecoveredDefaultsButWriteFailed,
        InMemoryFallback
    }

    public sealed class SettingsLoadResult
    {
        public SettingsLoadResult(
            SettingsLoadStatus status,
            SettingsWriteResult writeResult = null,
            string message = null,
            Exception exception = null)
        {
            Status = status;
            WriteResult = writeResult;
            Message = message;
            Exception = exception;
        }

        public SettingsLoadStatus Status { get; }
        public SettingsWriteResult WriteResult { get; }
        public string Message { get; }
        public Exception Exception { get; }
    }
}
