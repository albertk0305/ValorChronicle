using System;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Save.Migration
{
    /// <summary>
    /// Contains a migration status and, only on success, the migrated profile copy.
    /// </summary>
    public sealed class SaveMigrationResult
    {
        internal SaveMigrationResult(
            SaveMigrationStatus status,
            int sourceVersion,
            int targetVersion,
            ProfileSaveData data,
            string message,
            Exception exception)
        {
            Status = status;
            SourceVersion = sourceVersion;
            TargetVersion = targetVersion;
            Data = data;
            Message = message;
            Exception = exception;
        }

        /// <summary>Gets the migration outcome.</summary>
        public SaveMigrationStatus Status { get; }
        /// <summary>Gets the version found on the input profile.</summary>
        public int SourceVersion { get; }
        /// <summary>Gets the current version targeted by the runner.</summary>
        public int TargetVersion { get; }
        /// <summary>Gets the migrated deep copy, or null when migration did not succeed.</summary>
        public ProfileSaveData Data { get; }
        /// <summary>Gets a diagnostic message for failed outcomes.</summary>
        public string Message { get; }
        /// <summary>Gets the caught step exception, when one caused the failure.</summary>
        public Exception Exception { get; }
        /// <summary>Gets whether the migration completed successfully.</summary>
        public bool IsSuccess => Status == SaveMigrationStatus.Success;
    }
}
