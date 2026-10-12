using System;
using System.Globalization;

namespace ValorChronicle.Localization
{
    /// <summary>Maps supported localization IDs to sorting cultures.</summary>
    public static class LocalizationCultureResolver
    {
        public const string KoreanLocale = "ko";

        public static CultureInfo Resolve(string localeId)
        {
            if (string.IsNullOrWhiteSpace(localeId))
            {
                throw new ArgumentException(
                    "A locale ID is required.",
                    nameof(localeId));
            }

            switch (localeId.Trim())
            {
                case KoreanLocale:
                    return CultureInfo.GetCultureInfo("ko-KR");
                case LocalizationService.EnglishLocale:
                    return CultureInfo.GetCultureInfo("en-US");
                default:
                    throw new ArgumentException(
                        $"No sorting culture is mapped for locale "
                            + $"'{localeId}'.",
                        nameof(localeId));
            }
        }
    }
}
