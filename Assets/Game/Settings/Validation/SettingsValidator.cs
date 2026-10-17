using System;
using ValorChronicle.Localization;
using ValorChronicle.Settings.DTO;

namespace ValorChronicle.Settings.Validation
{
    public sealed class SettingsValidator
    {
        public const int MinimumVolume = 0;
        public const int MaximumVolume = 100;
        public const int NormalBattleSpeed = 1;
        public const int FastBattleSpeed = 2;

        public SettingsValidationResult Validate(GameSettingsData source)
        {
            if (source == null)
            {
                return new SettingsValidationResult(
                    false,
                    null,
                    false,
                    "Settings data is missing.");
            }

            if (source.SchemaVersion
                != GameSettingsData.CurrentSchemaVersion)
            {
                return new SettingsValidationResult(
                    false,
                    null,
                    false,
                    $"Unsupported settings schema version "
                        + $"'{source.SchemaVersion}'.");
            }

            GameSettingsData normalized = source.Clone();
            normalized.MasterVolume = NormalizeVolume(
                source.MasterVolume);
            normalized.BgmVolume = NormalizeVolume(source.BgmVolume);
            normalized.SfxVolume = NormalizeVolume(source.SfxVolume);
            normalized.LocaleId = NormalizeLocaleId(source.LocaleId);
            normalized.BattleSpeed = NormalizeBattleSpeed(
                source.BattleSpeed);

            return new SettingsValidationResult(
                true,
                normalized,
                !AreEqual(source, normalized),
                string.Empty);
        }

        public static int NormalizeVolume(int value)
        {
            return Math.Max(MinimumVolume, Math.Min(MaximumVolume, value));
        }

        public static string NormalizeLocaleId(string localeId)
        {
            string normalized = localeId?.Trim();
            return string.Equals(
                       normalized,
                       LocalizationService.EnglishLocale,
                       StringComparison.Ordinal)
                   || string.Equals(
                       normalized,
                       LocalizationCultureResolver.KoreanLocale,
                       StringComparison.Ordinal)
                ? normalized
                : LocalizationService.EnglishLocale;
        }

        public static int NormalizeBattleSpeed(int value)
        {
            return value == NormalBattleSpeed || value == FastBattleSpeed
                ? value
                : NormalBattleSpeed;
        }

        public static bool AreEqual(
            GameSettingsData first,
            GameSettingsData second)
        {
            if (ReferenceEquals(first, second))
                return true;
            if (first == null || second == null)
                return false;

            return first.SchemaVersion == second.SchemaVersion
                && first.MasterVolume == second.MasterVolume
                && first.BgmVolume == second.BgmVolume
                && first.SfxVolume == second.SfxVolume
                && string.Equals(
                    first.LocaleId,
                    second.LocaleId,
                    StringComparison.Ordinal)
                && first.BattleSpeed == second.BattleSpeed;
        }
    }
}
