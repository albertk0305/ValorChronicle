using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Data.Validation;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class GachaDefinitionTests
    {
        private const string DatabaseAssetPath =
            "Assets/Data/Database/DefinitionDatabase.asset";

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
        public void CanonicalDatabase_ResolvesIndependentGachaIds()
        {
            DefinitionDatabase database = LoadCanonicalDatabase();

            Assert.That(
                database.TryGetGacha(
                    "gacha_character_standard",
                    out GachaDefinition characterGacha),
                Is.True);
            Assert.That(
                database.TryGetGacha(
                    "gacha_relic_standard",
                    out GachaDefinition relicGacha),
                Is.True);
            Assert.That(characterGacha, Is.Not.SameAs(relicGacha));
            Assert.That(characterGacha.Id, Is.Not.EqualTo(relicGacha.Id));
        }

        [Test]
        public void CanonicalGachas_ContainConfirmedCostsPityPoolsAndRewards()
        {
            DefinitionDatabase database = LoadCanonicalDatabase();
            Assert.That(
                database.TryGetGacha(
                    "gacha_character_standard",
                    out GachaDefinition characterGacha),
                Is.True);
            Assert.That(
                database.TryGetGacha(
                    "gacha_relic_standard",
                    out GachaDefinition relicGacha),
                Is.True);

            AssertGacha(
                characterGacha,
                GachaKind.Character,
                50,
                new[]
                {
                    Expected(GachaRewardKind.RandomCharacter, 1, 20),
                    Expected(GachaRewardKind.RandomRelic, 1, 10),
                    Expected(GachaRewardKind.HeroTokens, 1, 200),
                    Expected(GachaRewardKind.HeroTokens, 3, 100),
                    Expected(GachaRewardKind.RelicTokens, 1, 80),
                    Expected(GachaRewardKind.BattleRecords, 1000, 320),
                    Expected(GachaRewardKind.BattleRecords, 3000, 200),
                    Expected(GachaRewardKind.BattleRecords, 6000, 60),
                    Expected(GachaRewardKind.BattleRecords, 12000, 10)
                });
            AssertGacha(
                relicGacha,
                GachaKind.Relic,
                20,
                new[]
                {
                    Expected(GachaRewardKind.RandomCharacter, 1, 5),
                    Expected(GachaRewardKind.RandomRelic, 1, 50),
                    Expected(GachaRewardKind.HeroTokens, 1, 50),
                    Expected(GachaRewardKind.RelicTokens, 1, 200),
                    Expected(GachaRewardKind.RelicTokens, 3, 100),
                    Expected(GachaRewardKind.BattleRecords, 1000, 340),
                    Expected(GachaRewardKind.BattleRecords, 3000, 200),
                    Expected(GachaRewardKind.BattleRecords, 6000, 50),
                    Expected(GachaRewardKind.BattleRecords, 12000, 5)
                });

            AssertPoolsReferenceKnownDefinitions(characterGacha, database);
            AssertPoolsReferenceKnownDefinitions(relicGacha, database);
        }

        [Test]
        public void CanonicalGachas_PassGachaValidation()
        {
            DefinitionDatabase database = LoadCanonicalDatabase();

            ValidationReport report = DataValidator.Validate(database);

            Assert.That(
                report.Issues.Where(issue =>
                    issue.Context is GachaDefinition),
                Is.Empty);
        }

        [Test]
        public void Validate_DetectsNonPositiveWeightAndWrongTotal()
        {
            GachaDefinition gacha = CreateGacha(
                "gacha_invalid_weight",
                rewards: new[]
                {
                    Reward(GachaRewardKind.RandomCharacter, 1, 0),
                    Reward(GachaRewardKind.BattleRecords, 1000, 999)
                });

            ValidationReport report = Validate(gacha);

            AssertGachaError(report, "Reward weight must be greater than zero");
            AssertGachaError(report, "Reward weight total must be 1000");
        }

        [Test]
        public void Validate_DetectsTenPullCostMismatch()
        {
            GachaDefinition gacha = CreateGacha(
                "gacha_invalid_ten_cost",
                singleCost: 100,
                tenCost: 999);

            ValidationReport report = Validate(gacha);

            AssertGachaError(
                report,
                "Ten-pull cost must equal ten single pulls");
        }

        [Test]
        public void Validate_DetectsMissingPoolContentIds()
        {
            GachaDefinition gacha = CreateGacha(
                "gacha_missing_content",
                characterPoolIds: new[] { "character_missing" },
                relicPoolIds: new[] { "relic_missing" });

            ValidationReport report = Validate(gacha);

            AssertGachaError(
                report,
                "Gacha character pool references missing ID");
            AssertGachaError(
                report,
                "Gacha relic pool references missing ID");
        }

        [Test]
        public void Validate_DetectsDuplicateGachaId()
        {
            GachaDefinition first = CreateGacha("gacha_duplicate");
            GachaDefinition second = CreateGacha("gacha_duplicate");
            DefinitionDatabase database = CreateDatabase(
                new[] { first, second });

            ValidationReport report = DataValidator.Validate(database);

            AssertGachaError(report, "Duplicate GachaDefinition ID");
        }

        [Test]
        public void Validate_DetectsEmptyRewardsAndRequiredPools()
        {
            GachaDefinition emptyRewards = CreateGacha(
                "gacha_empty_rewards",
                rewards: Array.Empty<GachaRewardEntry>());
            GachaDefinition emptyPools = CreateGacha(
                "gacha_empty_pools",
                characterPoolIds: Array.Empty<string>(),
                relicPoolIds: Array.Empty<string>());

            ValidationReport report = DataValidator.Validate(
                CreateDatabase(new[] { emptyRewards, emptyPools }));

            AssertGachaError(report, "Reward list is empty");
            AssertGachaError(
                report,
                "Character reward requires a non-empty character pool");
            AssertGachaError(
                report,
                "Relic reward requires a non-empty relic pool");
            AssertGachaError(
                report,
                "Character pity requires a non-empty character pool");
        }

        [Test]
        public void Validate_DetectsInvalidRulesAndDuplicateEntries()
        {
            GachaDefinition gacha = CreateGacha(
                string.Empty,
                kind: (GachaKind)999,
                singleCost: 0,
                tenCost: 0,
                pity: 0,
                rewards: new[]
                {
                    Reward((GachaRewardKind)999, 0, 500),
                    Reward((GachaRewardKind)999, 0, 500)
                },
                characterPoolIds: new[]
                {
                    "character_marea_bluefang",
                    "character_marea_bluefang"
                },
                relicPoolIds: new[]
                {
                    "relic_victors_broken_sword",
                    "relic_victors_broken_sword"
                });

            ValidationReport report = Validate(gacha);

            AssertGachaError(report, "Missing content ID");
            AssertGachaError(report, "Gacha kind is invalid");
            AssertGachaError(report, "Single-pull cost must be greater than zero");
            AssertGachaError(report, "Ten-pull cost must be greater than zero");
            AssertGachaError(report, "Pity pull count must be greater than zero");
            AssertGachaError(report, "Reward kind is invalid");
            AssertGachaError(report, "Reward quantity must be greater than zero");
            AssertGachaError(report, "Duplicate reward kind and quantity");
            AssertGachaError(report, "Duplicate Gacha character pool ID");
            AssertGachaError(report, "Duplicate Gacha relic pool ID");
        }

        [Test]
        public void CanonicalDatabase_ExistingDefinitionLookupsStillWork()
        {
            DefinitionDatabase database = LoadCanonicalDatabase();

            Assert.That(
                database.TryGetCharacter(
                    "character_marea_bluefang",
                    out _),
                Is.True);
            Assert.That(
                database.TryGetRelic(
                    "relic_victors_broken_sword",
                    out _),
                Is.True);
            Assert.That(database.Bosses, Is.Not.Empty);
            Assert.That(database.Skills, Is.Not.Empty);
        }

        private DefinitionDatabase LoadCanonicalDatabase()
        {
            DefinitionDatabase database =
                AssetDatabase.LoadAssetAtPath<DefinitionDatabase>(
                    DatabaseAssetPath);
            Assert.That(database, Is.Not.Null);
            database.Initialize();
            return database;
        }

        private ValidationReport Validate(GachaDefinition gacha)
        {
            return DataValidator.Validate(
                CreateDatabase(new[] { gacha }));
        }

        private DefinitionDatabase CreateDatabase(GachaDefinition[] gachas)
        {
            DefinitionDatabase database =
                ScriptableObject.CreateInstance<DefinitionDatabase>();
            createdObjects.Add(database);

            CharacterDefinition character =
                CreateDefinition<CharacterDefinition>(
                    "character_marea_bluefang");
            RelicDefinition firstRelic = CreateDefinition<RelicDefinition>(
                "relic_victors_broken_sword");
            RelicDefinition secondRelic = CreateDefinition<RelicDefinition>(
                "relic_world_tree_bark");
            ExchangeCatalogDefinition exchangeCatalog = ScriptableObject
                .CreateInstance<ExchangeCatalogDefinition>();
            createdObjects.Add(exchangeCatalog);

            SetObjectArray(
                database,
                "characters",
                new[] { character });
            SetObjectArray(
                database,
                "relics",
                new[] { firstRelic, secondRelic });
            SetObjectArray(database, "gachas", gachas);
            SetValue(database, "exchangeCatalog", exchangeCatalog);
            return database;
        }

        private GachaDefinition CreateGacha(
            string id,
            GachaKind kind = GachaKind.Character,
            long singleCost = 100,
            long tenCost = 1000,
            int pity = 50,
            GachaRewardEntry[] rewards = null,
            string[] characterPoolIds = null,
            string[] relicPoolIds = null)
        {
            GachaDefinition definition =
                CreateDefinition<GachaDefinition>(id);
            SetValue(definition, "gachaKind", kind);
            SetValue(definition, "singlePullCost", singleCost);
            SetValue(definition, "tenPullCost", tenCost);
            SetValue(definition, "pityPullCount", pity);
            SetValue(
                definition,
                "rewardEntries",
                rewards ?? new[]
                {
                    Reward(GachaRewardKind.RandomCharacter, 1, 20),
                    Reward(GachaRewardKind.RandomRelic, 1, 10),
                    Reward(GachaRewardKind.BattleRecords, 1000, 970)
                });
            SetValue(
                definition,
                "characterPoolIds",
                characterPoolIds ?? new[] { "character_marea_bluefang" });
            SetValue(
                definition,
                "relicPoolIds",
                relicPoolIds ?? new[]
                {
                    "relic_victors_broken_sword",
                    "relic_world_tree_bark"
                });
            return definition;
        }

        private T CreateDefinition<T>(string id)
            where T : GameDefinition
        {
            T definition = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(definition);
            SetValue(definition, "id", id);
            return definition;
        }

        private static GachaRewardEntry Reward(
            GachaRewardKind kind,
            long quantity,
            int weight)
        {
            var reward = new GachaRewardEntry();
            SetValue(reward, "rewardKind", kind);
            SetValue(reward, "quantity", quantity);
            SetValue(reward, "weight", weight);
            return reward;
        }

        private static Tuple<GachaRewardKind, long, int> Expected(
            GachaRewardKind kind,
            long quantity,
            int weight)
        {
            return Tuple.Create(kind, quantity, weight);
        }

        private static void AssertGacha(
            GachaDefinition definition,
            GachaKind expectedKind,
            int expectedPity,
            IReadOnlyList<Tuple<GachaRewardKind, long, int>> expectedRewards)
        {
            Assert.That(definition.GachaKind, Is.EqualTo(expectedKind));
            Assert.That(definition.SinglePullCost, Is.EqualTo(100L));
            Assert.That(definition.TenPullCost, Is.EqualTo(1000L));
            Assert.That(definition.PityPullCount, Is.EqualTo(expectedPity));
            Assert.That(
                definition.RewardEntries.Sum(entry => entry.Weight),
                Is.EqualTo(GachaDefinition.RequiredTotalWeight));
            Assert.That(
                definition.RewardEntries.Select(entry =>
                    Tuple.Create(
                        entry.RewardKind,
                        entry.Quantity,
                        entry.Weight)),
                Is.EqualTo(expectedRewards));
        }

        private static void AssertPoolsReferenceKnownDefinitions(
            GachaDefinition definition,
            DefinitionDatabase database)
        {
            Assert.That(definition.CharacterPoolIds, Is.Not.Empty);
            Assert.That(
                definition.CharacterPoolIds.Distinct(StringComparer.Ordinal)
                    .Count(),
                Is.EqualTo(definition.CharacterPoolIds.Count));
            Assert.That(
                definition.CharacterPoolIds.All(id =>
                    database.TryGetCharacter(id, out _)),
                Is.True);

            Assert.That(definition.RelicPoolIds, Is.Not.Empty);
            Assert.That(
                definition.RelicPoolIds.Distinct(StringComparer.Ordinal)
                    .Count(),
                Is.EqualTo(definition.RelicPoolIds.Count));
            Assert.That(
                definition.RelicPoolIds.All(id =>
                    database.TryGetRelic(id, out _)),
                Is.True);
        }

        private static void AssertGachaError(
            ValidationReport report,
            string messageFragment)
        {
            Assert.That(
                report.Issues.Any(issue =>
                    issue.Context is GachaDefinition
                    && issue.Message.Contains(messageFragment)),
                Is.True,
                messageFragment);
        }

        private static void SetObjectArray<TObject>(
            UnityEngine.Object target,
            string propertyName,
            TObject[] values)
            where TObject : UnityEngine.Object
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property =
                serializedObject.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue =
                    values[index];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetValue(
            object target,
            string fieldName,
            object value)
        {
            Type type = target.GetType();
            while (type != null)
            {
                System.Reflection.FieldInfo field = type.GetField(
                    fieldName,
                    System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            Assert.Fail($"Field was not found: {fieldName}");
        }
    }
}
