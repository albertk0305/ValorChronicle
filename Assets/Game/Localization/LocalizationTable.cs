using System;
using System.Collections.Generic;

namespace ValorChronicle.Localization
{
    public sealed class LocalizationTable
    {
        internal LocalizationTable(
            string sourceName,
            IReadOnlyList<string> locales,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>
                entries)
        {
            SourceName = sourceName;
            Locales = locales;
            Entries = entries;
        }

        public string SourceName { get; }
        public IReadOnlyList<string> Locales { get; }
        public IReadOnlyDictionary<
            string,
            IReadOnlyDictionary<string, string>> Entries { get; }
    }

    public sealed class LocalizationDataException : Exception
    {
        public LocalizationDataException(string message)
            : base(message)
        {
        }
    }
}
