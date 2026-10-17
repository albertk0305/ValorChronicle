using System;
using ValorChronicle.Localization;

namespace ValorChronicle.Settings.DTO
{
    [Serializable]
    public sealed class GameSettingsData
    {
        public const int CurrentSchemaVersion = 1;
        public const int DefaultVolume = 100;
        public const int DefaultBattleSpeed = 1;

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public int MasterVolume { get; set; } = DefaultVolume;
        public int BgmVolume { get; set; } = DefaultVolume;
        public int SfxVolume { get; set; } = DefaultVolume;
        public string LocaleId { get; set; } =
            LocalizationService.EnglishLocale;
        public int BattleSpeed { get; set; } = DefaultBattleSpeed;

        public static GameSettingsData CreateDefault()
        {
            return new GameSettingsData();
        }

        public GameSettingsData Clone()
        {
            return new GameSettingsData
            {
                SchemaVersion = SchemaVersion,
                MasterVolume = MasterVolume,
                BgmVolume = BgmVolume,
                SfxVolume = SfxVolume,
                LocaleId = LocaleId,
                BattleSpeed = BattleSpeed
            };
        }
    }
}
