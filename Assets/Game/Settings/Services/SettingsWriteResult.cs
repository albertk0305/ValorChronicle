using System;

namespace ValorChronicle.Settings.Services
{
    public enum SettingsWriteStatus
    {
        Success,
        NoChanges,
        PersistenceUnavailable,
        SerializationFailed,
        BackupRotationFailed,
        TempWriteFailed,
        TempReadFailed,
        TempVerificationFailed,
        PromoteFailed,
        UnexpectedFailure
    }

    public sealed class SettingsWriteResult
    {
        public SettingsWriteResult(
            SettingsWriteStatus status,
            string message = null,
            Exception exception = null)
        {
            Status = status;
            Message = message;
            Exception = exception;
        }

        public SettingsWriteStatus Status { get; }
        public string Message { get; }
        public Exception Exception { get; }
        public bool IsSuccess => Status == SettingsWriteStatus.Success
            || Status == SettingsWriteStatus.NoChanges;
    }
}
