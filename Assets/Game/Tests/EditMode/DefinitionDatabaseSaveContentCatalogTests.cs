using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Save.Validation;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class DefinitionDatabaseSaveContentCatalogTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object createdObject in createdObjects)
                UnityEngine.Object.DestroyImmediate(createdObject);
            createdObjects.Clear();
        }

        [Test]
        public void CharacterLookup_DistinguishesExistingAndMissingIds()
        {
            DefinitionDatabaseSaveContentCatalog catalog = CreateCatalog(
                characters: new[] { CreateDefinition<CharacterDefinition>("character_test") });

            Assert.That(catalog.LookupCharacter("character_test"), Is.EqualTo(SaveContentLookupResult.Exists));
            Assert.That(catalog.LookupCharacter("character_missing"), Is.EqualTo(SaveContentLookupResult.Missing));
        }

        [Test]
        public void RelicLookup_DistinguishesExistingAndMissingIds()
        {
            DefinitionDatabaseSaveContentCatalog catalog = CreateCatalog(
                relics: new[] { CreateDefinition<RelicDefinition>("relic_test") });

            Assert.That(catalog.LookupRelic("relic_test"), Is.EqualTo(SaveContentLookupResult.Exists));
            Assert.That(catalog.LookupRelic("relic_missing"), Is.EqualTo(SaveContentLookupResult.Missing));
        }

        [Test]
        public void BossLookup_DistinguishesExistingAndMissingIds()
        {
            DefinitionDatabaseSaveContentCatalog catalog = CreateCatalog(
                bosses: new[] { CreateDefinition<BossDefinition>("boss_test") });

            Assert.That(catalog.LookupBoss("boss_test"), Is.EqualTo(SaveContentLookupResult.Exists));
            Assert.That(catalog.LookupBoss("boss_missing"), Is.EqualTo(SaveContentLookupResult.Missing));
        }

        [Test]
        public void UnsupportedProductionDefinitions_ReturnUnavailable()
        {
            DefinitionDatabaseSaveContentCatalog catalog = CreateCatalog();

            Assert.That(catalog.LookupBossDifficulty("boss", "difficulty"), Is.EqualTo(SaveContentLookupResult.Unavailable));
            Assert.That(catalog.LookupGacha("gacha"), Is.EqualTo(SaveContentLookupResult.Unavailable));
            Assert.That(catalog.LookupRewardGrade("boss", "difficulty", "grade"), Is.EqualTo(SaveContentLookupResult.Unavailable));
        }

        [Test]
        public void Constructor_UninitializedDatabase_ThrowsClearly()
        {
            DefinitionDatabase database = ScriptableObject.CreateInstance<DefinitionDatabase>();
            createdObjects.Add(database);

            Assert.Throws<InvalidOperationException>(
                () => new DefinitionDatabaseSaveContentCatalog(database));
        }

        private DefinitionDatabaseSaveContentCatalog CreateCatalog(
            CharacterDefinition[] characters = null,
            BossDefinition[] bosses = null,
            RelicDefinition[] relics = null)
        {
            DefinitionDatabase database = ScriptableObject.CreateInstance<DefinitionDatabase>();
            createdObjects.Add(database);
            var serialized = new SerializedObject(database);
            SetObjects(serialized.FindProperty("characters"), characters);
            SetObjects(serialized.FindProperty("bosses"), bosses);
            SetObjects(serialized.FindProperty("relics"), relics);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            database.Initialize();
            return new DefinitionDatabaseSaveContentCatalog(database);
        }

        private T CreateDefinition<T>(string id) where T : GameDefinition
        {
            T definition = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(definition);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static void SetObjects<T>(SerializedProperty property, T[] values)
            where T : UnityEngine.Object
        {
            values ??= Array.Empty<T>();
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
