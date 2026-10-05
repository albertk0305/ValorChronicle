using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Presentation;
using ValorChronicle.Party.Roster;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode.Characters.Presentation
{
    public sealed class CharacterRosterScreenTests
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
        public void Initialize_BindsOwnedRosterCurrenciesAndDefaultSort()
        {
            CharacterDefinition[] definitions =
            {
                Definition("content_first", ElementType.Fire),
                Definition("high_awake", ElementType.Water),
                Definition("content_third", ElementType.Grass),
                Definition("low_level", ElementType.Dark),
                Definition("not_owned", ElementType.Light)
            };
            ProfileSaveData profile = Profile(
                ("content_third", 10, 3),
                ("low_level", 9, 6),
                ("content_first", 10, 1),
                ("high_awake", 10, 3));
            profile.Currencies.GachaCurrency = long.MaxValue - 1;
            profile.Currencies.BattleRecords = long.MaxValue - 2;

            Fixture fixture = CreateFixture(profile, definitions);

            Assert.That(
                fixture.Controller.FullRoster.Select(item => item.CharacterId),
                Does.Not.Contain("not_owned"));
            Assert.That(
                fixture.Controller.VisibleRoster.Select(
                    item => item.CharacterId),
                Is.EqualTo(new[]
                {
                    "high_awake",
                    "content_third",
                    "content_first",
                    "low_level"
                }));
            Assert.That(fixture.View.LevelSortButton.interactable, Is.False);
            Assert.That(
                fixture.View.AwakeningSortButton.interactable,
                Is.True);
            Assert.That(fixture.View.GoldText.text,
                Is.EqualTo((long.MaxValue - 1).ToString()));
            Assert.That(fixture.View.BattleRecordsText.text,
                Is.EqualTo((long.MaxValue - 2).ToString()));
            CharacterRosterCellView highAwakeCell = fixture.View
                .CharacterGrid.PoolCells.Single(
                    item => item.CharacterId == "high_awake");
            Assert.That(highAwakeCell.LevelText.text, Is.EqualTo("10"));
            Assert.That(highAwakeCell.AwakeningText.text, Is.EqualTo("3"));
        }

        [Test]
        public void EmptyAndLargeRostersRemainRepresentableAndVirtualized()
        {
            Fixture empty = CreateFixture(
                Profile(),
                Array.Empty<CharacterDefinition>());
            Assert.That(empty.Controller.VisibleRoster, Is.Empty);
            Assert.That(empty.View.CharacterGrid.ItemCount, Is.Zero);

            var definitions = new CharacterDefinition[35];
            var owned = new (string, int, int)[35];
            for (int index = 0; index < definitions.Length; index++)
            {
                string id = $"character_{index:D2}";
                definitions[index] = Definition(
                    id,
                    (ElementType)(index % 5));
                owned[index] = (id, index + 1, index % 7);
            }

            Fixture large = CreateFixture(Profile(owned), definitions);
            CharacterRosterCellView[] poolBefore =
                large.View.CharacterGrid.PoolCells.ToArray();

            Assert.That(large.View.CharacterGrid.ItemCount, Is.EqualTo(35));
            Assert.That(large.Controller.VisibleRoster, Has.Count.EqualTo(35));
            Assert.That(large.View.CharacterGrid.PoolSize, Is.LessThan(35));
            large.View.CharacterGrid.SetScrollOffsetForTesting(330f);
            Assert.That(large.View.CharacterGrid.FirstBoundRow,
                Is.GreaterThan(0));
            Assert.That(
                large.View.CharacterGrid.PoolCells,
                Is.EqualTo(poolBefore));
        }

        [Test]
        public void FiltersToggleAndRemainIndependentFromAwakeningSort()
        {
            CharacterDefinition[] definitions =
            {
                Definition("fire", ElementType.Fire),
                Definition("water_high_level", ElementType.Water),
                Definition("water_high_awake", ElementType.Water),
                Definition("grass", ElementType.Grass),
                Definition("light", ElementType.Light),
                Definition("dark", ElementType.Dark)
            };
            Fixture fixture = CreateFixture(
                Profile(
                    ("fire", 1, 0),
                    ("water_high_level", 20, 2),
                    ("water_high_awake", 10, 5),
                    ("grass", 1, 0),
                    ("light", 1, 0),
                    ("dark", 1, 0)),
                definitions);

            foreach (ElementType element in Enum.GetValues(
                typeof(ElementType)))
            {
                ElementFilterButtonView filter = fixture.View.FilterButtons
                    .Single(item => item.Element == element);
                filter.Button.onClick.Invoke();
                Assert.That(
                    fixture.Controller.VisibleRoster.All(
                        item => item.Element == element),
                    Is.True,
                    element.ToString());
                filter.Button.onClick.Invoke();
                Assert.That(fixture.Controller.SelectedElement, Is.Null);
                Assert.That(fixture.Controller.VisibleRoster,
                    Has.Count.EqualTo(6));
            }

            ElementFilterButtonView water = fixture.View.FilterButtons
                .Single(item => item.Element == ElementType.Water);
            water.Button.onClick.Invoke();
            fixture.View.AwakeningSortButton.onClick.Invoke();

            Assert.That(fixture.Controller.SelectedElement,
                Is.EqualTo(ElementType.Water));
            Assert.That(fixture.Controller.SortMode,
                Is.EqualTo(CharacterRosterSortMode.Awakening));
            Assert.That(
                fixture.Controller.VisibleRoster.Select(
                    item => item.CharacterId),
                Is.EqualTo(new[]
                {
                    "water_high_awake",
                    "water_high_level"
                }));
            Assert.That(fixture.View.LevelSortButton.interactable, Is.True);
            Assert.That(
                fixture.View.AwakeningSortButton.interactable,
                Is.False);
        }

        [Test]
        public void CellClickUpdatesSelectionWithoutDuplicateListeners()
        {
            Fixture fixture = CreateFixture(
                Profile(("selected", 10, 2)),
                new[] { Definition("selected", ElementType.Fire) });
            int notifications = 0;
            fixture.Controller.SelectedCharacterChanged += _ =>
                notifications++;
            CharacterRosterCellView cell = fixture.View.CharacterGrid
                .PoolCells.Single(item => item.IsBound);

            cell.Button.onClick.Invoke();
            fixture.DeactivateLifecycle();
            fixture.ActivateLifecycle();
            cell = fixture.View.CharacterGrid.PoolCells.Single(
                item => item.IsBound);
            cell.Button.onClick.Invoke();

            Assert.That(fixture.Controller.SelectedCharacterId,
                Is.EqualTo("selected"));
            Assert.That(notifications, Is.EqualTo(2));
        }

        private Fixture CreateFixture(
            ProfileSaveData profile,
            CharacterDefinition[] definitions)
        {
            DefinitionDatabase database = Database(definitions);
            SaveService saveService = SaveService(profile);
            CharacterPresentationCatalog catalog = Catalog(definitions);
            ElementIconSet icons = ScriptableObject.CreateInstance<
                ElementIconSet>();
            createdObjects.Add(icons);

            GameObject root = Object("CharacterRosterScreen");
            root.SetActive(false);
            RectTransform viewport = Rect("Viewport", root.transform);
            viewport.sizeDelta = new Vector2(500f, 300f);
            RectTransform content = Rect("Content", viewport);
            var scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;

            var initialCells = new CharacterRosterCellView[5];
            for (int index = 0; index < initialCells.Length; index++)
            {
                initialCells[index] = Cell(content, index);
            }

            var grid = viewport.gameObject.AddComponent<
                VirtualizedCharacterGridView>();
            grid.Configure(
                scrollRect,
                viewport,
                content,
                initialCells,
                new Vector2(80f, 80f),
                new Vector2(10f, 10f),
                configuredTopPadding: 0f,
                configuredBottomPadding: 0f,
                configuredBufferRows: 0);

            var filters = new ElementFilterButtonView[5];
            for (int index = 0; index < filters.Length; index++)
            {
                GameObject filterObject = Object(
                    $"Filter{index}",
                    root.transform);
                Image image = filterObject.AddComponent<Image>();
                Button button = filterObject.AddComponent<Button>();
                filters[index] = filterObject.AddComponent<
                    ElementFilterButtonView>();
                filters[index].Configure(
                    (ElementType)index,
                    button,
                    image);
            }

            Button levelSort = ButtonObject("LevelSort", root.transform);
            Button awakeningSort = ButtonObject(
                "AwakeningSort",
                root.transform);
            TMP_Text goldText = Text("Gold", root.transform);
            TMP_Text recordsText = Text("Records", root.transform);
            var view = root.AddComponent<CharacterRosterScreenView>();
            view.Configure(
                grid,
                filters,
                levelSort,
                awakeningSort,
                goldText,
                recordsText);
            var controller = root.AddComponent<
                CharacterRosterScreenController>();
            controller.Configure(view, catalog, icons);
            controller.Initialize(saveService, database);
            root.SetActive(true);

            var fixture = new Fixture(root, controller, view);
            fixture.ActivateLifecycle();
            return fixture;
        }

        private CharacterRosterCellView Cell(
            Transform parent,
            int index)
        {
            GameObject root = Object($"Cell{index}", parent);
            Image characterImage = root.AddComponent<Image>();
            Button button = root.AddComponent<Button>();
            Image typeImage = Object("Type", root.transform)
                .AddComponent<Image>();
            GameObject border = Object("Border", root.transform);
            TMP_Text level = Text("Level", root.transform);
            TMP_Text awakening = Text("Awakening", root.transform);
            var cell = root.AddComponent<CharacterRosterCellView>();
            cell.Configure(
                button,
                characterImage,
                typeImage,
                border,
                level,
                awakening);
            return cell;
        }

        private CharacterDefinition Definition(
            string id,
            ElementType element)
        {
            var definition = ScriptableObject.CreateInstance<
                CharacterDefinition>();
            createdObjects.Add(definition);
            SetField(definition, typeof(GameDefinition), "id", id);
            SetField(definition, typeof(CharacterDefinition), "element", element);
            SetField(definition, typeof(CharacterDefinition), "level1Hp", 100);
            SetField(definition, typeof(CharacterDefinition), "level1Attack", 10);
            SetField(definition, typeof(CharacterDefinition), "level100Hp", 1000);
            SetField(definition, typeof(CharacterDefinition), "level100Attack", 100);
            return definition;
        }

        private DefinitionDatabase Database(
            CharacterDefinition[] definitions)
        {
            var database = ScriptableObject.CreateInstance<
                DefinitionDatabase>();
            createdObjects.Add(database);
            SetArray(database, "characters", definitions);
            database.Initialize();
            return database;
        }

        private CharacterPresentationCatalog Catalog(
            CharacterDefinition[] definitions)
        {
            var entries = new CharacterPresentationDefinition[
                definitions.Length];
            for (int index = 0; index < definitions.Length; index++)
            {
                entries[index] = ScriptableObject.CreateInstance<
                    CharacterPresentationDefinition>();
                createdObjects.Add(entries[index]);
                SetField(
                    entries[index],
                    typeof(CharacterPresentationDefinition),
                    "characterId",
                    definitions[index].Id);
            }

            var catalog = ScriptableObject.CreateInstance<
                CharacterPresentationCatalog>();
            createdObjects.Add(catalog);
            SetArray(catalog, "definitions", entries);
            return catalog;
        }

        private SaveService SaveService(ProfileSaveData profile)
        {
            var repository = new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.Json(profile)
            };
            SaveService service = SaveServiceTestFactory.Create(repository);
            SaveLoadResult result = service.LoadOrCreate("ignored");
            Assert.That(result.CanUseProfile, Is.True, result.Message);
            return service;
        }

        private static ProfileSaveData Profile(
            params (string id, int level, int awakening)[] characters)
        {
            ProfileSaveData profile = SaveTestDataBuilder.Valid();
            for (int index = 0; index < characters.Length; index++)
            {
                profile.Characters.Add(new CharacterSaveData
                {
                    CharacterId = characters[index].id,
                    Level = characters[index].level,
                    Awakening = characters[index].awakening
                });
            }

            return profile;
        }

        private GameObject Object(string name, Transform parent = null)
        {
            var created = new GameObject(name, typeof(RectTransform));
            createdObjects.Add(created);
            if (parent != null)
            {
                created.transform.SetParent(parent, false);
            }

            return created;
        }

        private RectTransform Rect(string name, Transform parent)
        {
            return (RectTransform)Object(name, parent).transform;
        }

        private Button ButtonObject(string name, Transform parent)
        {
            GameObject created = Object(name, parent);
            created.AddComponent<Image>();
            return created.AddComponent<Button>();
        }

        private TMP_Text Text(string name, Transform parent)
        {
            return Object(name, parent).AddComponent<TextMeshProUGUI>();
        }

        private static void SetArray<T>(
            UnityEngine.Object target,
            string fieldName,
            T[] values)
            where T : UnityEngine.Object
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue =
                    values[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetField(
            UnityEngine.Object target,
            Type declaringType,
            string fieldName,
            object value)
        {
            var field = declaringType.GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static void InvokeLifecycle(
            MonoBehaviour target,
            string methodName)
        {
            var method = target.GetType().GetMethod(
                methodName,
                System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, null);
        }

        private sealed class Fixture
        {
            public Fixture(
                GameObject root,
                CharacterRosterScreenController controller,
                CharacterRosterScreenView view)
            {
                Root = root;
                Controller = controller;
                View = view;
            }

            public GameObject Root { get; }
            public CharacterRosterScreenController Controller { get; }
            public CharacterRosterScreenView View { get; }

            public void ActivateLifecycle()
            {
                Root.SetActive(true);
                foreach (CharacterRosterCellView cell
                    in View.CharacterGrid.PoolCells)
                {
                    if (cell.gameObject.activeInHierarchy)
                    {
                        InvokeLifecycle(cell, "OnEnable");
                    }
                }

                foreach (ElementFilterButtonView filter
                    in View.FilterButtons)
                {
                    InvokeLifecycle(filter, "OnEnable");
                }

                InvokeLifecycle(View, "OnEnable");
                InvokeLifecycle(Controller, "OnEnable");
            }

            public void DeactivateLifecycle()
            {
                Root.SetActive(false);
                InvokeLifecycle(Controller, "OnDisable");
                InvokeLifecycle(View, "OnDisable");
                foreach (ElementFilterButtonView filter
                    in View.FilterButtons)
                {
                    InvokeLifecycle(filter, "OnDisable");
                }

                foreach (CharacterRosterCellView cell
                    in View.CharacterGrid.PoolCells)
                {
                    InvokeLifecycle(cell, "OnDisable");
                }
            }
        }
    }
}
