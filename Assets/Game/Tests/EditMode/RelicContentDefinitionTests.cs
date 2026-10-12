using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Data.Validation;
using ValorChronicle.Localization;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class RelicContentDefinitionTests
    {
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string LocalizationCatalogPath =
            "Assets/Data/Localization/LocalizationCatalog.asset";
        private static readonly double[] ExpectedRates =
        {
            0.05d,
            0.06d,
            0.07d,
            0.08d,
            0.09d,
            0.10d
        };

        [TestCase(
            "relic_victors_broken_sword",
            RelicEffectType.AttackIncrease)]
        [TestCase(
            "relic_world_tree_bark",
            RelicEffectType.MaxHpIncrease)]
        public void ProductionDatabase_ResolvesConfirmedRelicData(
            string relicId,
            RelicEffectType expectedEffectType)
        {
            DefinitionDatabase database = LoadDatabase();

            Assert.That(database.TryGetRelic(
                relicId,
                out RelicDefinition relic), Is.True);
            Assert.That(relic.EffectType, Is.EqualTo(expectedEffectType));
            Assert.That(relic.EffectRates, Has.Count.EqualTo(6));
            Assert.That(relic.EffectRates, Is.EqualTo(ExpectedRates));
            for (int awakening = 0;
                awakening < ExpectedRates.Length;
                awakening++)
            {
                Assert.That(
                    relic.GetEffectRate(awakening),
                    Is.EqualTo(ExpectedRates[awakening]));
            }

            Assert.That(database.TryGetRelicPresentation(
                relicId,
                out RelicPresentationDefinition presentation), Is.True);
            Assert.That(presentation.Icon, Is.Not.Null);
        }

        [Test]
        public void ProductionRelicContent_PassesDataValidation()
        {
            ValidationReport report = DataValidator.Validate(LoadDatabase());

            Assert.That(
                report.HasErrors,
                Is.False,
                string.Join("\n", report.Issues));
        }

        [Test]
        public void ProductionLocalization_ResolvesRelicNamesAndDescriptions()
        {
            DefinitionDatabase database = LoadDatabase();
            LocalizationCatalog catalog =
                AssetDatabase.LoadAssetAtPath<LocalizationCatalog>(
                    LocalizationCatalogPath);
            Assert.That(catalog, Is.Not.Null);
            LocalizationService localization = catalog.CreateService();
            localization.SetLocale("ko");

            AssertLocalized(
                database,
                localization,
                "relic_victors_broken_sword",
                "승전자의 부러진 검",
                "장착한 캐릭터의 공격력이 5% 증가합니다.");
            AssertLocalized(
                database,
                localization,
                "relic_world_tree_bark",
                "세계수의 수피",
                "장착한 캐릭터의 최대 HP가 5% 증가합니다.");
        }

        private static DefinitionDatabase LoadDatabase()
        {
            DefinitionDatabase database =
                AssetDatabase.LoadAssetAtPath<DefinitionDatabase>(
                    DatabasePath);
            Assert.That(database, Is.Not.Null);
            database.Initialize();
            return database;
        }

        private static void AssertLocalized(
            DefinitionDatabase database,
            LocalizationService localization,
            string relicId,
            string expectedName,
            string expectedDescription)
        {
            Assert.That(database.TryGetRelic(
                relicId,
                out RelicDefinition relic), Is.True);
            Assert.That(
                localization.GetText(relic.DisplayNameKey),
                Is.EqualTo(expectedName));
            Assert.That(
                localization.Format(
                    relic.DescriptionKey,
                    new Dictionary<string, object>
                    {
                        ["rate"] = 5
                    }),
                Is.EqualTo(expectedDescription));
        }
    }
}
