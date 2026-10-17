using ValorChronicle.Settings.DTO;

namespace ValorChronicle.Settings.Validation
{
    public sealed class SettingsValidationResult
    {
        internal SettingsValidationResult(
            bool isValid,
            GameSettingsData settings,
            bool wasModified,
            string message)
        {
            IsValid = isValid;
            this.settings = settings?.Clone();
            WasModified = wasModified;
            Message = message ?? string.Empty;
        }

        public bool IsValid { get; }
        public GameSettingsData Settings => settings?.Clone();
        public bool WasModified { get; }
        public string Message { get; }

        private readonly GameSettingsData settings;
    }
}
