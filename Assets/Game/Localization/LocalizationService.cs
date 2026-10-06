using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ValorChronicle.Localization
{
    public sealed class LocalizationService
    {
        public const string EnglishLocale = "en";

        private static readonly Regex PlaceholderPattern = new Regex(
            @"\{([A-Za-z][A-Za-z0-9_]*)\}",
            RegexOptions.CultureInvariant);

        private readonly Dictionary<
            string,
            IReadOnlyDictionary<string, string>> entries;
        private readonly HashSet<string> supportedLocales;

        public LocalizationService(
            IEnumerable<LocalizationTable> tables,
            string defaultLocale = EnglishLocale,
            string fallbackLocale = EnglishLocale)
        {
            if (tables == null)
            {
                throw new ArgumentNullException(nameof(tables));
            }

            DefaultLocale = RequireLocaleId(
                defaultLocale,
                nameof(defaultLocale));
            FallbackLocale = RequireLocaleId(
                fallbackLocale,
                nameof(fallbackLocale));
            entries = new Dictionary<
                string,
                IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
            supportedLocales = new HashSet<string>(StringComparer.Ordinal);

            foreach (LocalizationTable table in tables)
            {
                Merge(table ?? throw new LocalizationDataException(
                    "A localization table cannot be null."));
            }

            ValidateCatalog();
            CurrentLocale = DefaultLocale;
        }

        public event Action<string> LocaleChanged;

        public string DefaultLocale { get; }
        public string FallbackLocale { get; }
        public string CurrentLocale { get; private set; }

        public string GetText(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException(
                    "A localization key is required.",
                    nameof(key));
            }

            if (!entries.TryGetValue(key, out var translations))
            {
                return MissingKey(key);
            }

            if (TryGetNonEmpty(translations, CurrentLocale, out string text)
                || TryGetNonEmpty(
                    translations,
                    FallbackLocale,
                    out text))
            {
                return text;
            }

            return MissingKey(key);
        }

        public string Format(
            string key,
            IReadOnlyDictionary<string, object> arguments)
        {
            TryFormat(key, arguments, out string result, out _);
            return result;
        }

        public bool TryFormat(
            string key,
            IReadOnlyDictionary<string, object> arguments,
            out string result,
            out string error)
        {
            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            string template = GetText(key);
            string missingArgument = null;
            result = PlaceholderPattern.Replace(
                template,
                match =>
                {
                    string argumentName = match.Groups[1].Value;
                    if (!arguments.TryGetValue(
                            argumentName,
                            out object value))
                    {
                        missingArgument ??= argumentName;
                        return match.Value;
                    }

                    return Convert.ToString(
                        value,
                        CultureInfo.InvariantCulture) ?? string.Empty;
                });

            if (missingArgument == null)
            {
                error = null;
                return true;
            }

            error = $"Localization key '{key}' is missing named argument "
                + $"'{missingArgument}'.";
            return false;
        }

        public bool SetLocale(string localeId)
        {
            localeId = RequireLocaleId(localeId, nameof(localeId));
            if (!supportedLocales.Contains(localeId))
            {
                throw new ArgumentException(
                    $"Unknown locale '{localeId}'.",
                    nameof(localeId));
            }

            if (string.Equals(
                    CurrentLocale,
                    localeId,
                    StringComparison.Ordinal))
            {
                return false;
            }

            CurrentLocale = localeId;
            LocaleChanged?.Invoke(localeId);
            return true;
        }

        public static string MissingKey(string key)
        {
            return $"[[{key}]]";
        }

        private void Merge(LocalizationTable table)
        {
            for (int index = 0; index < table.Locales.Count; index++)
            {
                supportedLocales.Add(table.Locales[index]);
            }

            foreach (var pair in table.Entries)
            {
                if (entries.ContainsKey(pair.Key))
                {
                    throw new LocalizationDataException(
                        $"Localization key '{pair.Key}' is duplicated while "
                        + $"merging table '{table.SourceName}'.");
                }

                entries.Add(pair.Key, pair.Value);
            }
        }

        private void ValidateCatalog()
        {
            if (!supportedLocales.Contains(DefaultLocale))
            {
                throw new LocalizationDataException(
                    $"Default locale '{DefaultLocale}' is not present in "
                    + "the localization table headers.");
            }

            if (!supportedLocales.Contains(FallbackLocale))
            {
                throw new LocalizationDataException(
                    $"Fallback locale '{FallbackLocale}' is not present in "
                    + "the localization table headers.");
            }

            foreach (var entry in entries)
            {
                if (!TryGetNonEmpty(
                        entry.Value,
                        DefaultLocale,
                        out string defaultText))
                {
                    throw new LocalizationDataException(
                        $"Localization key '{entry.Key}' has no value for "
                        + $"the default locale '{DefaultLocale}'.");
                }

                HashSet<string> expected = ExtractPlaceholders(
                    entry.Key,
                    DefaultLocale,
                    defaultText);
                foreach (var translation in entry.Value)
                {
                    if (string.IsNullOrEmpty(translation.Value)
                        || string.Equals(
                            translation.Key,
                            DefaultLocale,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    HashSet<string> actual = ExtractPlaceholders(
                        entry.Key,
                        translation.Key,
                        translation.Value);
                    if (!expected.SetEquals(actual))
                    {
                        throw new LocalizationDataException(
                            $"Localization key '{entry.Key}' has different "
                            + $"named placeholders in locale "
                            + $"'{translation.Key}'.");
                    }
                }
            }
        }

        private static HashSet<string> ExtractPlaceholders(
            string key,
            string locale,
            string text)
        {
            var placeholders = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match match in PlaceholderPattern.Matches(text))
            {
                placeholders.Add(match.Groups[1].Value);
            }

            string remainder = PlaceholderPattern.Replace(text, string.Empty);
            if (remainder.IndexOf('{') >= 0 || remainder.IndexOf('}') >= 0)
            {
                throw new LocalizationDataException(
                    $"Localization key '{key}' has an invalid named "
                    + $"placeholder in locale '{locale}'.");
            }

            return placeholders;
        }

        private static bool TryGetNonEmpty(
            IReadOnlyDictionary<string, string> translations,
            string locale,
            out string value)
        {
            return translations.TryGetValue(locale, out value)
                && !string.IsNullOrEmpty(value);
        }

        private static string RequireLocaleId(string localeId, string name)
        {
            if (string.IsNullOrWhiteSpace(localeId))
            {
                throw new ArgumentException(
                    "A locale ID is required.",
                    name);
            }

            return localeId.Trim();
        }
    }
}
