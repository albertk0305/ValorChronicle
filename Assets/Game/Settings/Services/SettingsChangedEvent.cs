using System;
using ValorChronicle.Settings.DTO;

namespace ValorChronicle.Settings.Services
{
    public sealed class SettingsChangedEvent
    {
        public SettingsChangedEvent(
            SettingsChangeKind changeKind,
            GameSettingsData settings)
        {
            if (changeKind == SettingsChangeKind.None)
                throw new ArgumentOutOfRangeException(nameof(changeKind));

            ChangeKind = changeKind;
            Settings = settings?.Clone()
                ?? throw new ArgumentNullException(nameof(settings));
        }

        public SettingsChangeKind ChangeKind { get; }
        public GameSettingsData Settings { get; }
    }
}
