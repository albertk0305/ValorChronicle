using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Localization;

namespace ValorChronicle.Tests.EditMode.Localization
{
    public sealed class LocalizationServiceTests
    {
        [Test]
        public void Parser_ReadsHeaderLocalesAndMultilineEscape()
        {
            LocalizationTable table = LocalizationTableParser.Parse(
                "test.tsv",
                "key\ten\tko\tja\nline\tFirst\\nSecond\t\tJapanese");

            Assert.That(table.Locales, Is.EqualTo(new[] { "en", "ko", "ja" }));
            Assert.That(table.Entries["line"]["en"],
                Is.EqualTo("First\nSecond"));
            Assert.That(table.Entries["line"]["ko"], Is.Empty);
            Assert.That(table.Entries["line"]["ja"],
                Is.EqualTo("Japanese"));
        }

        [Test]
        public void MultipleTablesMergeAndEnglishLookupSucceeds()
        {
            LocalizationService service = Service(
                "key\ten\tko\nfirst\tFirst\t",
                "key\ten\tko\nsecond\tSecond\t");

            Assert.That(service.GetText("first"), Is.EqualTo("First"));
            Assert.That(service.GetText("second"), Is.EqualTo("Second"));
        }

        [Test]
        public void DuplicateKeyAcrossTablesIsRejected()
        {
            LocalizationTable first = Table(
                "key\ten\nshared\tFirst",
                "first.tsv");
            LocalizationTable second = Table(
                "key\ten\nshared\tSecond",
                "second.tsv");

            Assert.Throws<LocalizationDataException>(
                () => new LocalizationService(
                    new[] { first, second }));
        }

        [Test]
        public void EmptyKeyIsRejected()
        {
            Assert.Throws<LocalizationDataException>(
                () => Table("key\ten\n\tValue"));
        }

        [TestCase("key\tko\nname\tName")]
        [TestCase("key\ten\tko\nname\t\tName")]
        public void MissingDefaultLocaleIsRejected(string tsv)
        {
            Assert.Throws<LocalizationDataException>(
                () => Service(tsv));
        }

        [Test]
        public void EmptyCurrentLocaleFallsBackToEnglish()
        {
            LocalizationService service = Service(
                "key\ten\tko\nname\tEnglish Name\t");

            service.SetLocale("ko");

            Assert.That(service.GetText("name"),
                Is.EqualTo("English Name"));
        }

        [Test]
        public void MissingKeyUsesExplicitFallback()
        {
            LocalizationService service = Service(
                "key\ten\nknown\tKnown");

            Assert.That(service.GetText("unknown"),
                Is.EqualTo("[[unknown]]"));
        }

        [Test]
        public void FormatSubstitutesNamedArgumentsInvariantly()
        {
            LocalizationService service = Service(
                "key\ten\nskill\tDeals {coefficient}% for {duration} turns");
            var arguments = new Dictionary<string, object>
            {
                ["coefficient"] = 25.5,
                ["duration"] = 3
            };

            Assert.That(service.Format("skill", arguments),
                Is.EqualTo("Deals 25.5% for 3 turns"));
        }

        [Test]
        public void MissingFormatArgumentIsNonFatalAndReported()
        {
            LocalizationService service = Service(
                "key\ten\nskill\tLasts {duration} turns");

            bool success = service.TryFormat(
                "skill",
                new Dictionary<string, object>(),
                out string result,
                out string error);

            Assert.That(success, Is.False);
            Assert.That(result, Is.EqualTo("Lasts {duration} turns"));
            Assert.That(error, Does.Contain("duration"));
        }

        [Test]
        public void LocalePlaceholderMismatchIsRejected()
        {
            Assert.Throws<LocalizationDataException>(() => Service(
                "key\ten\tko\nskill\tLasts {duration} turns\t{turns}턴 지속"));
        }

        [Test]
        public void SetLocaleRaisesExactlyOnceOnlyWhenChanged()
        {
            LocalizationService service = Service(
                "key\ten\tko\nname\tEnglish\tKorean");
            int eventCount = 0;
            string receivedLocale = null;
            service.LocaleChanged += locale =>
            {
                eventCount++;
                receivedLocale = locale;
            };

            Assert.That(service.SetLocale("ko"), Is.True);
            Assert.That(service.SetLocale("ko"), Is.False);

            Assert.That(service.CurrentLocale, Is.EqualTo("ko"));
            Assert.That(receivedLocale, Is.EqualTo("ko"));
            Assert.That(eventCount, Is.EqualTo(1));
        }

        [Test]
        public void UnknownLocaleIsRejectedWithoutChangingCurrentLocale()
        {
            LocalizationService service = Service(
                "key\ten\tko\nname\tEnglish\tKorean");

            Assert.Throws<System.ArgumentException>(
                () => service.SetLocale("fr"));
            Assert.That(service.CurrentLocale, Is.EqualTo("en"));
        }

        [Test]
        public void ProductionCatalogResolvesMareaDisplayNameKey()
        {
            CharacterDefinition definition = AssetDatabase.LoadAssetAtPath<
                CharacterDefinition>(
                "Assets/Data/Characters/marea_bluefang.asset");
            LocalizationCatalog catalog = AssetDatabase.LoadAssetAtPath<
                LocalizationCatalog>(
                "Assets/Data/Localization/LocalizationCatalog.asset");

            Assert.That(definition, Is.Not.Null);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.CreateService().GetText(
                definition.DisplayNameKey), Is.EqualTo("Marea Bluefang"));
        }

        private static LocalizationService Service(params string[] tables)
        {
            var parsed = new LocalizationTable[tables.Length];
            for (int index = 0; index < tables.Length; index++)
            {
                parsed[index] = Table(tables[index], $"table{index}.tsv");
            }

            return new LocalizationService(parsed);
        }

        private static LocalizationTable Table(
            string tsv,
            string sourceName = "test.tsv")
        {
            return LocalizationTableParser.Parse(sourceName, tsv);
        }
    }
}
