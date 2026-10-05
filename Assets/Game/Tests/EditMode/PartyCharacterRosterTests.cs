using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Characters.Build;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Presentation;
using ValorChronicle.Party.Roster;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class PartyCharacterRosterTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void Builder_UsesOwnedCharactersDefinitionsAndLevelStats()
        {
            CharacterDefinition ownedDefinition = Definition(
                "owned",
                ElementType.Fire,
                level1Hp: 100,
                level1Attack: 10,
                level100Hp: 1090,
                level100Attack: 100);
            DefinitionDatabase database = Database(
                ownedDefinition,
                Definition("unowned", ElementType.Water));
            var warnings = new List<string>();
            var builder = new PartyCharacterRosterBuilder(
                database,
                warnings.Add);

            IReadOnlyList<CharacterRosterEntry> result = builder.Build(
                new[]
                {
                    new CharacterSaveData
                    {
                        CharacterId = "owned",
                        Level = 50,
                        Awakening = 4
                    },
                    new CharacterSaveData
                    {
                        CharacterId = "missing",
                        Level = 1,
                        Awakening = 0
                    }
                });

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].CharacterId, Is.EqualTo("owned"));
            Assert.That(result[0].Element, Is.EqualTo(ElementType.Fire));
            Assert.That(result[0].Level, Is.EqualTo(50));
            Assert.That(result[0].Awakening, Is.EqualTo(4));
            Assert.That(result[0].MaxHp, Is.EqualTo(590));
            Assert.That(result[0].Attack, Is.EqualTo(55));
            Assert.That(result[0].ContentOrder, Is.Zero);
            Assert.That(warnings, Has.Count.EqualTo(1));
            Assert.That(warnings[0], Does.Contain("missing"));
        }

        [TestCase(100, 0, 3400L, 1050L)]
        [TestCase(100, 1, 3400L, 1103L)]
        [TestCase(100, 2, 3570L, 1103L)]
        [TestCase(100, 6, 3570L, 1103L)]
        [TestCase(50, 2, 2244L, 642L)]
        public void Builder_UsesDefaultCharacterBuildForFinalStats(
            int level,
            int awakening,
            long expectedHp,
            long expectedAttack)
        {
            CharacterDefinition marea = Definition(
                "character_marea_bluefang",
                ElementType.Water,
                level1Hp: 900,
                level1Attack: 180,
                level100Hp: 3400,
                level100Attack: 1050);
            var builder = new PartyCharacterRosterBuilder(Database(marea));

            CharacterRosterEntry entry = builder.Build(
                new[]
                {
                    new CharacterSaveData
                    {
                        CharacterId = marea.Id,
                        Level = level,
                        Awakening = awakening
                    }
                }).Single();
            ResolvedCharacterBuild expected =
                CharacterBuildResolverFactory.CreateDefault().Resolve(
                    marea,
                    level,
                    awakening);

            Assert.That(entry.MaxHp, Is.EqualTo(expectedHp));
            Assert.That(entry.Attack, Is.EqualTo(expectedAttack));
            Assert.That(entry.MaxHp, Is.EqualTo(expected.MaxHp));
            Assert.That(entry.Attack, Is.EqualTo(expected.Attack));
        }

        [Test]
        public void Builder_DoesNotTruncateRosterAtThirtyOne()
        {
            var definitions = new CharacterDefinition[40];
            var owned = new CharacterSaveData[40];
            for (int index = 0; index < definitions.Length; index++)
            {
                string id = $"character_{index:D2}";
                definitions[index] = Definition(id, ElementType.Light);
                owned[index] = new CharacterSaveData
                {
                    CharacterId = id,
                    Level = 1,
                    Awakening = 0
                };
            }

            var builder = new PartyCharacterRosterBuilder(
                Database(definitions));

            Assert.That(builder.Build(owned), Has.Count.EqualTo(40));
        }

        [Test]
        public void Query_AppliesFilterAndDeterministicSortRules()
        {
            var roster = new[]
            {
                Entry("b", ElementType.Fire, 10, 2),
                Entry("a", ElementType.Fire, 10, 2),
                Entry("c", ElementType.Fire, 9, 5),
                Entry("water", ElementType.Water, 100, 6)
            };

            IReadOnlyList<CharacterRosterEntry> level =
                CharacterRosterQuery.Apply(
                    roster,
                    ElementType.Fire,
                    CharacterRosterSortMode.Level);
            IReadOnlyList<CharacterRosterEntry> awakening =
                CharacterRosterQuery.Apply(
                    roster,
                    ElementType.Fire,
                    CharacterRosterSortMode.Awakening);

            Assert.That(
                level.Select(item => item.CharacterId),
                Is.EqualTo(new[] { "a", "b", "c" }));
            Assert.That(
                awakening.Select(item => item.CharacterId),
                Is.EqualTo(new[] { "c", "a", "b" }));
        }

        [Test]
        public void Query_UsesContentOrderBeforeCharacterIdForFinalTie()
        {
            var roster = new[]
            {
                Entry("a", ElementType.Fire, 10, 2, contentOrder: 2),
                Entry("z", ElementType.Fire, 10, 2, contentOrder: 0),
                Entry("m", ElementType.Fire, 10, 2, contentOrder: 1)
            };

            IReadOnlyList<CharacterRosterEntry> result =
                CharacterRosterQuery.Apply(
                    roster,
                    selectedElement: null,
                    CharacterRosterSortMode.Level);
            IReadOnlyList<CharacterRosterEntry> awakeningResult =
                CharacterRosterQuery.Apply(
                    roster,
                    selectedElement: null,
                    CharacterRosterSortMode.Awakening);

            Assert.That(
                result.Select(item => item.CharacterId),
                Is.EqualTo(new[] { "z", "m", "a" }));
            Assert.That(
                awakeningResult.Select(item => item.CharacterId),
                Is.EqualTo(new[] { "z", "m", "a" }));
        }

        [Test]
        public void AwakeningSort_UsesLevelThenContentOrder()
        {
            var roster = new[]
            {
                Entry("lower", ElementType.Fire, 9, 4, contentOrder: 0),
                Entry("later", ElementType.Fire, 10, 4, contentOrder: 2),
                Entry("earlier", ElementType.Fire, 10, 4, contentOrder: 1)
            };

            IReadOnlyList<CharacterRosterEntry> result =
                CharacterRosterQuery.Apply(
                    roster,
                    selectedElement: null,
                    CharacterRosterSortMode.Awakening);

            Assert.That(
                result.Select(item => item.CharacterId),
                Is.EqualTo(new[] { "earlier", "later", "lower" }));
        }

        [Test]
        public void PresentationCatalog_LookupMissingAndDuplicateAreExplicit()
        {
            CharacterPresentationDefinition first = Presentation("a");
            CharacterPresentationDefinition second = Presentation("b");
            CharacterPresentationCatalog catalog = Catalog(first, second);

            catalog.Initialize();

            Assert.That(catalog.TryGet("a", out var found), Is.True);
            Assert.That(found, Is.SameAs(first));
            Assert.That(catalog.TryGet("missing", out _), Is.False);

            CharacterPresentationCatalog duplicate = Catalog(
                first,
                Presentation("a"));
            Assert.Throws<InvalidOperationException>(duplicate.Initialize);
        }

        [Test]
        public void ProjectElementIconSet_MapsAllFiveElements()
        {
            ElementIconSet iconSet = AssetDatabase.LoadAssetAtPath<
                ElementIconSet>("Assets/Data/Party/ElementIconSet.asset");

            Assert.That(iconSet, Is.Not.Null);
            Assert.That(iconSet.IsConfigured, Is.True);
            foreach (ElementType element in Enum.GetValues(typeof(ElementType)))
            {
                Assert.That(iconSet.GetIcon(element), Is.Not.Null, element.ToString());
            }
        }

        private CharacterDefinition Definition(
            string id,
            ElementType element,
            int level1Hp = 100,
            int level1Attack = 10,
            int level100Hp = 1000,
            int level100Attack = 100)
        {
            var definition = ScriptableObject.CreateInstance<
                CharacterDefinition>();
            createdObjects.Add(definition);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("element").enumValueIndex = (int)element;
            serialized.FindProperty("level1Hp").intValue = level1Hp;
            serialized.FindProperty("level1Attack").intValue = level1Attack;
            serialized.FindProperty("level100Hp").intValue = level100Hp;
            serialized.FindProperty("level100Attack").intValue =
                level100Attack;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private DefinitionDatabase Database(
            params CharacterDefinition[] definitions)
        {
            var database = ScriptableObject.CreateInstance<
                DefinitionDatabase>();
            createdObjects.Add(database);
            SetArray(database, "characters", definitions);
            database.Initialize();
            return database;
        }

        private CharacterPresentationDefinition Presentation(string id)
        {
            var definition = ScriptableObject.CreateInstance<
                CharacterPresentationDefinition>();
            createdObjects.Add(definition);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("characterId").stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private CharacterPresentationCatalog Catalog(
            params CharacterPresentationDefinition[] definitions)
        {
            var catalog = ScriptableObject.CreateInstance<
                CharacterPresentationCatalog>();
            createdObjects.Add(catalog);
            SetArray(catalog, "definitions", definitions);
            return catalog;
        }

        private static CharacterRosterEntry Entry(
            string id,
            ElementType element,
            int level,
            int awakening,
            int contentOrder = int.MaxValue)
        {
            return new CharacterRosterEntry(
                id,
                element,
                level,
                awakening,
                maxHp: 1,
                attack: 1,
                contentOrder: contentOrder);
        }

        private static void SetArray<T>(
            UnityEngine.Object target,
            string propertyName,
            T[] values)
            where T : UnityEngine.Object
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue =
                    values[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
