using System;

namespace ValorChronicle.Settings.Services
{
    [Flags]
    public enum SettingsChangeKind
    {
        None = 0,
        MasterVolume = 1 << 0,
        BgmVolume = 1 << 1,
        SfxVolume = 1 << 2,
        LocaleId = 1 << 3,
        BattleSpeed = 1 << 4
    }
}
