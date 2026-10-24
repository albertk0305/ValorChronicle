using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Data.Validation;
using ValorChronicle.Exchange;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class ExchangeCatalogBuilderTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = 0; index < createdObjects.Count; index++)
            {
                UnityEngine.Object.DestroyImmediate(createdObjects[index]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void Build_SortsRegisteredIdsAndExcludesUnlistedDefinitions()
        {
            TestData data = CreateData(
                new[] { "character_c", "character_a", "character_b" },
                new[] { "relic_c", "relic_a", "relic_b" },
                new[] { "character_c", "character_a" },
                new[] { "relic_c", "relic_a" });

            ExchangeCatalogData result = ExchangeCatalogBuilder.Build(
                data.Database,
                data.Catalog,
                Profile(heroTokens: 60L, relicTokens: 25L));

            Assert.That(
                result.Characters.Select(item => item.CharacterId),
                Is.EqualTo(new[] { "character_a", "character_c" }));
            Assert.That(
                result.Relics.Select(item => item.RelicId),
                Is.EqualTo(new[] { "relic_a", "relic_c" }));
            Assert.That(
                data.Catalog.CharacterIds,
                Is.EqualTo(new[] { "character_c", "character_a" }));
            Assert.That(
                data.Catalog.RelicIds,
                Is.EqualTo(new[] { "relic_c", "relic_a" }));
        }

        [Test]
        public void Build_UnownedEntriesUseUnownedDisplayWithoutAwakeningIcon()
        {
            TestData data = CreateData(
                new[] { "character_a" },
                new[] { "relic_a" },
                new[] { "character_a" },
                new[] { "relic_a" });

            ExchangeCatalogData result = ExchangeCatalogBuilder.Build(
                data.Database,
                data.Catalog,
                Profile(heroTokens: 60L, relicTokens: 25L));

            CharacterExchangeListItem character = result.Characters[0];
            Assert.That(character.IsOwned, Is.False);
            Assert.That(character.CurrentAwakening, Is.Zero);
            Assert.That(character.Progress.Kind,
                Is.EqualTo(ExchangeProgressKind.Unowned));
            Assert.That(character.Progress.ShowAwakeningIcon, Is.False);
            Assert.That(character.Cost, Is.EqualTo(60L));
            Assert.That(character.CurrencyKind,
                Is.EqualTo(ExchangeCurrencyKind.HeroTokens));

            RelicExchangeListItem relic = result.Relics[0];
            Assert.That(relic.IsOwned, Is.False);
            Assert.That(relic.TargetAwakening, Is.Zero);
            Assert.That(relic.WillCreateNewInstance, Is.True);
            Assert.That(relic.Progress.Kind,
                Is.EqualTo(ExchangeProgressKind.Unowned));
            Assert.That(relic.Progress.ShowAwakeningIcon, Is.False);
            Assert.That(relic.Cost, Is.EqualTo(25L));
            Assert.That(relic.CurrencyKind,
                Is.EqualTo(ExchangeCurrencyKind.RelicTokens));
        }

        [Test]
        public void Build_MaxCharacterSeparatesMaximumAndCurrencyReasons()
        {
            TestData data = CreateData(
                new[] { "character_a" },
                Array.Empty<string>(),
                new[] { "character_a" },
                Array.Empty<string>());
            ProfileSaveData profile = Profile(0L, 0L);
            profile.Characters.Add(Character(
                "character_a",
                SaveRules.CharacterMaxAwakening));

            CharacterExchangeListItem item = ExchangeCatalogBuilder.Build(
                data.Database,
                data.Catalog,
                profile).Characters[0];

            Assert.That(item.IsMaximumAwakening, Is.True);
            Assert.That(item.CanExchange, Is.False);
            Assert.That(item.HasSufficientCurrency, Is.False);
            Assert.That(
                item.UnavailableReasons.HasFlag(
                    ExchangeUnavailableReason.MaximumAwakening),
                Is.True);
            Assert.That(
                item.UnavailableReasons.HasFlag(
                    ExchangeUnavailableReason.InsufficientCurrency),
                Is.True);
            Assert.That(item.Progress.Kind,
                Is.EqualTo(ExchangeProgressKind.MaximumAwakening));
            Assert.That(item.Progress.ShowAwakeningIcon, Is.True);
            Assert.That(item.CreateConfirmDisplayData().CanConfirm,
                Is.False);
        }

        [Test]
        public void Build_MaxRelicRemainsExchangeableAndPreviewsNewZeroCopy()
        {
            TestData data = RelicData();
            ProfileSaveData profile = Profile(0L, 25L);
            profile.RelicInstances.Add(Relic("max", "relic_a", 5));

            RelicExchangeListItem item = ExchangeCatalogBuilder.Build(
                data.Database,
                data.Catalog,
                profile).Relics[0];

            Assert.That(item.IsOwned, Is.True);
            Assert.That(item.TargetAwakening, Is.EqualTo(5));
            Assert.That(item.WillCreateNewInstance, Is.True);
            Assert.That(item.CanExchange, Is.True);
            Assert.That(item.Progress.Kind, Is.EqualTo(
                ExchangeProgressKind.NewInstanceAfterMaximumAwakening));
            Assert.That(item.Progress.CurrentAwakening, Is.EqualTo(5));
            Assert.That(item.Progress.ResultingAwakening, Is.Zero);
            Assert.That(item.Progress.ShowAwakeningIcon, Is.True);
        }

        [Test]
        public void Build_MultipleRelicsUseHighestSubMaximumTarget()
        {
            TestData data = RelicData();
            ProfileSaveData profile = Profile(0L, 25L);
            profile.RelicInstances.Add(Relic("first", "relic_a", 2));
            profile.RelicInstances.Add(Relic("target", "relic_a", 4));
            profile.RelicInstances.Add(Relic("max", "relic_a", 5));

            RelicExchangeListItem item = ExchangeCatalogBuilder.Build(
                data.Database,
                data.Catalog,
                profile).Relics[0];

            Assert.That(item.AwakeningTargetInstanceId,
                Is.EqualTo("target"));
            Assert.That(item.TargetAwakening, Is.EqualTo(4));
            Assert.That(item.WillCreateNewInstance, Is.False);
            Assert.That(item.Progress.ResultingAwakening, Is.EqualTo(5));
            Assert.That(
                profile.RelicInstances.Select(relic => relic.Awakening),
                Is.EqualTo(new[] { 2, 4, 5 }));
        }

        [Test]
        public void Build_EqualRelicAwakeningUsesEarlierProfileEntry()
        {
            TestData data = RelicData();
            ProfileSaveData profile = Profile(0L, 25L);
            profile.RelicInstances.Add(Relic("earlier", "relic_a", 3));
            profile.RelicInstances.Add(Relic("later", "relic_a", 3));

            RelicExchangeListItem item = ExchangeCatalogBuilder.Build(
                data.Database,
                data.Catalog,
                profile).Relics[0];

            Assert.That(item.AwakeningTargetInstanceId,
                Is.EqualTo("earlier"));
        }

        [Test]
        public void Build_CharacterConfirmPreviewsAwakeningIncrease()
        {
            TestData data = CreateData(
                new[] { "character_a" },
                Array.Empty<string>(),
                new[] { "character_a" },
                Array.Empty<string>());
            ProfileSaveData profile = Profile(60L, 0L);
            profile.Characters.Add(Character("character_a", 4));

            ExchangeConfirmDisplayData confirm = ExchangeCatalogBuilder
                .Build(data.Database, data.Catalog, profile)
                .Characters[0]
                .CreateConfirmDisplayData();

            Assert.That(confirm.CanConfirm, Is.True);
            Assert.That(confirm.Progress.Kind,
                Is.EqualTo(ExchangeProgressKind.AwakeningIncrease));
            Assert.That(confirm.Progress.CurrentAwakening, Is.EqualTo(4));
            Assert.That(confirm.Progress.ResultingAwakening, Is.EqualTo(5));
            Assert.That(confirm.Progress.ShowAwakeningIcon, Is.True);
            Assert.That(profile.Characters[0].Awakening, Is.EqualTo(4));
        }

        [Test]
        public void Build_UnregisteredCatalogIdIsRejected()
        {
            TestData data = CreateData(
                new[] { "character_a" },
                Array.Empty<string>(),
                new[] { "character_missing" },
                Array.Empty<string>());

            Assert.That(
                () => ExchangeCatalogBuilder.Build(
                    data.Database,
                    data.Catalog,
                    Profile(60L, 25L)),
                Throws.InvalidOperationException.With.Message.Contains(
                    "not registered"));
        }

        [Test]
        public void Validate_InvalidCostsDuplicateAndMissingIdsAreReported()
        {
            TestData data = CreateData(
                new[] { "character_a" },
                new[] { "relic_a" },
                new[]
                {
                    "character_a",
                    "character_a",
                    "character_missing"
                },
                new[] { "relic_missing" },
                characterCost: 0L,
                relicCost: -1L);

            ValidationReport report = DataValidator.Validate(data.Database);

            Assert.That(report.HasErrors, Is.True);
            AssertIssue(report, "Character exchange cost");
            AssertIssue(report, "Relic exchange cost");
            AssertIssue(report, "Duplicate Exchange character ID");
            AssertIssue(report, "character_missing");
            AssertIssue(report, "relic_missing");
        }

        [Test]
        public void Validate_MissingCatalogIsReported()
        {
            DefinitionDatabase database = ScriptableObject
                .CreateInstance<DefinitionDatabase>();
            createdObjects.Add(database);

            ValidationReport report = DataValidator.Validate(database);

            Assert.That(report.HasErrors, Is.True);
            AssertIssue(report, "ExchangeCatalogDefinition is missing");
        }

        [Test]
        public void CanonicalCatalogBuildsExplicitlyRegisteredEntries()
        {
            DefinitionDatabase database = AssetDatabase.LoadAssetAtPath<
                DefinitionDatabase>(
                "Assets/Data/Database/DefinitionDatabase.asset");
            Assert.That(database, Is.Not.Null);
            Assert.That(database.ExchangeCatalog, Is.Not.Null);
            database.Initialize();

            Assert.That(database.ExchangeCatalog.CharacterExchangeCost,
                Is.EqualTo(60L));
            Assert.That(database.ExchangeCatalog.RelicExchangeCost,
                Is.EqualTo(25L));

            string[] registeredCharacterIds = database.ExchangeCatalog
                .CharacterIds
                .ToArray();
            Assert.That(registeredCharacterIds.Distinct().Count(),
                Is.EqualTo(registeredCharacterIds.Length));
            for (int index = 0;
                index < registeredCharacterIds.Length;
                index++)
            {
                Assert.That(database.TryGetCharacter(
                        registeredCharacterIds[index],
                        out _),
                    Is.True,
                    registeredCharacterIds[index]);
            }

            ExchangeCatalogData result = ExchangeCatalogBuilder.Build(
                database,
                database.ExchangeCatalog,
                Profile(heroTokens: 60L, relicTokens: 25L));
            Assert.That(
                result.Characters.Select(item => item.CharacterId),
                Is.EqualTo(registeredCharacterIds.OrderBy(
                    id => id,
                    StringComparer.Ordinal)));

            Assert.That(
                database.ExchangeCatalog.RelicIds.OrderBy(
                    id => id,
                    StringComparer.Ordinal),
                Is.EqualTo(database.Relics.Select(item => item.Id)
                    .OrderBy(id => id, StringComparer.Ordinal)));
            Assert.That(
                result.Relics.Select(item => item.RelicId),
                Is.EqualTo(database.ExchangeCatalog.RelicIds.OrderBy(
                    id => id,
                    StringComparer.Ordinal)));
        }

        private TestData RelicData()
        {
            return CreateData(
                Array.Empty<string>(),
                new[] { "relic_a" },
                Array.Empty<string>(),
                new[] { "relic_a" });
        }

        private TestData CreateData(
            string[] characterDefinitionIds,
            string[] relicDefinitionIds,
            string[] exchangeCharacterIds,
            string[] exchangeRelicIds,
            long characterCost = 60L,
            long relicCost = 25L)
        {
            CharacterDefinition[] characters = characterDefinitionIds
                .Select(CreateDefinition<CharacterDefinition>)
                .ToArray();
            RelicDefinition[] relics = relicDefinitionIds
                .Select(CreateDefinition<RelicDefinition>)
                .ToArray();
            ExchangeCatalogDefinition catalog = ScriptableObject
                .CreateInstance<ExchangeCatalogDefinition>();
            createdObjects.Add(catalog);
            SetField(catalog, "characterExchangeCost", characterCost);
            SetField(catalog, "relicExchangeCost", relicCost);
            SetField(catalog, "characterIds", exchangeCharacterIds);
            SetField(catalog, "relicIds", exchangeRelicIds);

            DefinitionDatabase database = ScriptableObject
                .CreateInstance<DefinitionDatabase>();
            createdObjects.Add(database);
            SetField(database, "characters", characters);
            SetField(database, "relics", relics);
            SetField(database, "exchangeCatalog", catalog);
            database.Initialize();
            return new TestData(database, catalog);
        }

        private T CreateDefinition<T>(string id) where T : GameDefinition
        {
            T definition = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(definition);
            SetField(definition, "id", id);
            return definition;
        }

        private static ProfileSaveData Profile(
            long heroTokens,
            long relicTokens)
        {
            ProfileSaveData profile = SaveTestDataBuilder.Valid();
            profile.Currencies.HeroTokens = heroTokens;
            profile.Currencies.RelicTokens = relicTokens;
            return profile;
        }

        private static CharacterSaveData Character(
            string id,
            int awakening)
        {
            return new CharacterSaveData
            {
                CharacterId = id,
                Level = SaveRules.CharacterMinLevel,
                Awakening = awakening
            };
        }

        private static RelicInstanceSaveData Relic(
            string instanceId,
            string relicId,
            int awakening)
        {
            return new RelicInstanceSaveData
            {
                InstanceId = instanceId,
                RelicDefinitionId = relicId,
                Awakening = awakening
            };
        }

        private static void AssertIssue(
            ValidationReport report,
            string messageFragment)
        {
            Assert.That(
                report.Issues.Any(issue =>
                    issue.Message.Contains(messageFragment)),
                Is.True,
                messageFragment);
        }

        private static void SetField(
            object target,
            string fieldName,
            object value)
        {
            Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(
                    fieldName,
                    BindingFlags.Instance
                        | BindingFlags.NonPublic
                        | BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            throw new MissingFieldException(
                target.GetType().FullName,
                fieldName);
        }

        private sealed class TestData
        {
            public TestData(
                DefinitionDatabase database,
                ExchangeCatalogDefinition catalog)
            {
                Database = database;
                Catalog = catalog;
            }

            public DefinitionDatabase Database { get; }
            public ExchangeCatalogDefinition Catalog { get; }
        }
    }
}
