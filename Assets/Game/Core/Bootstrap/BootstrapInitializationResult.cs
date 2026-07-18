using System;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Core.Bootstrap
{
    /// <summary>Describes the subsystem in which game initialization completed or stopped.</summary>
    public enum BootstrapInitializationStatus
    {
        Success,
        ContentInitializationFailed,
        ContentValidationFailed,
        SaveInitializationFailed,
        SceneLoadFailed,
        UnexpectedFailure
    }

    /// <summary>Contains a bootstrap outcome and preserves detailed save initialization information.</summary>
    public sealed class BootstrapInitializationResult
    {
        internal BootstrapInitializationResult(
            BootstrapInitializationStatus status,
            SaveLoadResult saveLoadResult = null,
            Exception exception = null,
            string message = "")
        {
            Status = status;
            SaveLoadResult = saveLoadResult;
            Exception = exception;
            Message = message ?? string.Empty;
        }

        /// <summary>Gets the high-level initialization outcome.</summary>
        public BootstrapInitializationStatus Status { get; }
        /// <summary>Gets whether all initialization phases and Main scene loading succeeded.</summary>
        public bool IsSuccess => Status == BootstrapInitializationStatus.Success;
        /// <summary>Gets the detailed save result when save loading was attempted.</summary>
        public SaveLoadResult SaveLoadResult { get; }
        /// <summary>Gets the caught exception when one stopped initialization.</summary>
        public Exception Exception { get; }
        /// <summary>Gets a safe diagnostic that contains no profile data or save JSON.</summary>
        public string Message { get; }
    }
}
