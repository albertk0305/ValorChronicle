using System;
using System.Collections.Generic;

namespace ValorChronicle.Localization
{
    public static class LocalizationTableParser
    {
        private const string KeyHeader = "key";

        public static LocalizationTable Parse(
            string sourceName,
            string tsv)
        {
            if (string.IsNullOrWhiteSpace(sourceName))
            {
                throw new ArgumentException(
                    "A localization table source name is required.",
                    nameof(sourceName));
            }

            if (string.IsNullOrWhiteSpace(tsv))
            {
                throw Error(sourceName, "The table is empty.");
            }

            string[] lines = tsv
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n');
            string[] headers = lines[0].Split('\t');
            if (headers.Length < 2)
            {
                throw Error(
                    sourceName,
                    "The header must contain 'key' and at least one locale.");
            }

            headers[0] = headers[0].TrimStart('\uFEFF').Trim();
            if (!string.Equals(
                    headers[0],
                    KeyHeader,
                    StringComparison.Ordinal))
            {
                throw Error(
                    sourceName,
                    "The first header column must be 'key'.");
            }

            var locales = new List<string>(headers.Length - 1);
            var localeSet = new HashSet<string>(StringComparer.Ordinal);
            for (int column = 1; column < headers.Length; column++)
            {
                string locale = headers[column].Trim();
                if (locale.Length == 0)
                {
                    throw Error(
                        sourceName,
                        $"Locale column {column + 1} is empty.");
                }

                if (!localeSet.Add(locale))
                {
                    throw Error(
                        sourceName,
                        $"Locale column '{locale}' is duplicated.");
                }

                locales.Add(locale);
            }

            var entries = new Dictionary<
                string,
                IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
            for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] cells = line.Split('\t');
                if (cells.Length > headers.Length)
                {
                    throw Error(
                        sourceName,
                        $"Line {lineIndex + 1} has more columns than the header.");
                }

                string key = cells[0].Trim();
                if (key.Length == 0)
                {
                    throw Error(
                        sourceName,
                        $"Line {lineIndex + 1} has an empty key.");
                }

                if (entries.ContainsKey(key))
                {
                    throw Error(
                        sourceName,
                        $"Localization key '{key}' is duplicated.");
                }

                var translations = new Dictionary<string, string>(
                    StringComparer.Ordinal);
                for (int column = 1; column < headers.Length; column++)
                {
                    string value = column < cells.Length
                        ? Unescape(cells[column])
                        : string.Empty;
                    translations.Add(locales[column - 1], value);
                }

                entries.Add(key, translations);
            }

            return new LocalizationTable(sourceName, locales, entries);
        }

        private static string Unescape(string value)
        {
            return value.Replace("\\n", "\n");
        }

        private static LocalizationDataException Error(
            string sourceName,
            string message)
        {
            return new LocalizationDataException(
                $"Localization table '{sourceName}': {message}");
        }
    }
}
