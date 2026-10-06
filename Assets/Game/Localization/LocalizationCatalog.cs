using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValorChronicle.Localization
{
    [CreateAssetMenu(
        fileName = "LocalizationCatalog",
        menuName = "Valor Chronicle/Localization/Catalog")]
    public sealed class LocalizationCatalog : ScriptableObject
    {
        [SerializeField] private string defaultLocale =
            LocalizationService.EnglishLocale;
        [SerializeField] private string fallbackLocale =
            LocalizationService.EnglishLocale;
        [SerializeField] private TextAsset[] tables = Array.Empty<TextAsset>();

        public LocalizationService CreateService()
        {
            if (tables == null || tables.Length == 0)
            {
                throw new LocalizationDataException(
                    "LocalizationCatalog requires at least one TSV table.");
            }

            var parsed = new List<LocalizationTable>(tables.Length);
            for (int index = 0; index < tables.Length; index++)
            {
                TextAsset table = tables[index];
                if (table == null)
                {
                    throw new LocalizationDataException(
                        $"LocalizationCatalog table {index} is missing.");
                }

                parsed.Add(LocalizationTableParser.Parse(
                    table.name,
                    table.text));
            }

            return new LocalizationService(
                parsed,
                defaultLocale,
                fallbackLocale);
        }
    }
}
