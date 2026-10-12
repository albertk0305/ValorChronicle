using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Database;
using ValorChronicle.Localization;
using ValorChronicle.Relics.Persistence;
using ValorChronicle.Relics.Presentation;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class RelicCharacterScreenPresentationTests
    {
        private const string CharacterId = "character_relic_ui_test";
        private const string SwordDefinitionId =
            "relic_victors_broken_sword";
        private const string BarkDefinitionId =
            "relic_world_tree_bark";
        private const string DatabasePath =
            "Assets/Data/Database/DefinitionDatabase.asset";
        private const string LocalizationCatalogPath =
            "Assets/Data/Localization/LocalizationCatalog.asset";

        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    Object.DestroyImmediate(createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void Slots_RenderFourIndicesAndOpenFreshSessionFromClick()
        {
            Fixture fixture = CreateFixture(Profile(
                new[] { "sword", "", "bark", "" }));

            Assert.That(fixture.Controller.TryPresent(CharacterId), Is.True);
            Assert.That(fixture.SlotsView.Slots.Count, Is.EqualTo(4));
            for (int index = 0; index < 4; index++)
            {
                Assert.That(
                    fixture.SlotsView.Slots[index].SlotIndex,
                    Is.EqualTo(index));
                Assert.That(
                    fixture.SlotsView.Slots[index].Border.activeSelf,
                    Is.True);
            }

            Assert.That(fixture.Slots[0].IsIconVisible, Is.True);
            Assert.That(fixture.Slots[0].EmptyBackground.activeSelf, Is.False);
            Assert.That(fixture.Slots[0].AwakeningText.text, Is.EqualTo("0"));
            Assert.That(fixture.Slots[1].IsIconVisible, Is.False);
            Assert.That(fixture.Slots[1].EmptyBackground.activeSelf, Is.True);
            Assert.That(
                fixture.Slots[1].AwakeningText.gameObject.activeSelf,
                Is.False);
            Assert.That(fixture.Slots[2].AwakeningText.text, Is.EqualTo("5"));

            fixture.Slots[2].Button.onClick.Invoke();

            Assert.That(fixture.Controller.IsSelectionOpen, Is.True);
            Assert.That(
                fixture.Controller.EditSession.TargetSlotIndex,
                Is.EqualTo(2));
            Assert.That(
                fixture.Controller.EditSession.OriginalInstanceId,
                Is.EqualTo("bark"));
            Assert.That(fixture.SelectionRoot.activeSelf, Is.True);
        }

        [Test]
        public void Preview_UsesPresentationAwakeningAndLocalizedRate()
        {
            Fixture fixture = CreateFixture(Profile(
                new[] { "sword", "", "", "" }));
            fixture.Controller.TryPresent(CharacterId);

            Assert.That(fixture.Controller.Open(CharacterId, 0), Is.True);
            Assert.That(fixture.Selection.IsIconVisible, Is.True);
            Assert.That(
                fixture.Selection.PreviewAwakeningText.text,
                Is.EqualTo("0"));
            Assert.That(fixture.Selection.DescriptionText.text,
                Does.Contain("5%"));
            RelicInventoryCellView swordCell = fixture.InventoryGrid.PoolCells
                .Single(cell => cell.IsBound
                    && cell.InstanceId == "sword");
            RelicInventoryCellView initialBarkCell =
                fixture.InventoryGrid.PoolCells.Single(cell => cell.IsBound
                    && cell.InstanceId == "bark");
            Assert.That(swordCell.IsDimmed, Is.True);
            Assert.That(swordCell.Button.interactable, Is.True);
            Assert.That(initialBarkCell.IsDimmed, Is.False);
            Assert.That(initialBarkCell.Button.interactable, Is.True);

            int writesBefore = fixture.Repository.Count(
                nameof(FakeSaveRepository.WriteTemp));
            string[] inventoryOrder = fixture.InventoryGrid.Items
                .Select(entry => entry.InstanceId)
                .ToArray();
            RelicInventoryCellView barkCell = fixture.InventoryGrid.PoolCells
                .Single(cell => cell.IsBound
                    && cell.InstanceId == "bark");
            barkCell.Button.onClick.Invoke();

            Assert.That(
                fixture.Controller.EditSession.PreviewInstanceId,
                Is.EqualTo("bark"));
            Assert.That(
                fixture.Selection.PreviewAwakeningText.text,
                Is.EqualTo("5"));
            Assert.That(fixture.Selection.DescriptionText.text,
                Does.Contain("10%"));
            Assert.That(
                fixture.Repository.Count(
                    nameof(FakeSaveRepository.WriteTemp)),
                Is.EqualTo(writesBefore));
            Assert.That(CurrentSlot(fixture, 0), Is.EqualTo("sword"));
            Assert.That(
                fixture.InventoryGrid.Items
                    .Select(entry => entry.InstanceId),
                Is.EqualTo(inventoryOrder));
            Assert.That(
                fixture.Selection.PreviewIconImage.sprite,
                Is.Not.SameAs(fixture.Slots[0].IconImage.sprite));
        }

        [Test]
        public void LocaleChange_RequeriesInventoryWithoutChangingPreviewId()
        {
            Fixture fixture = CreateFixture(Profile(
                new[] { "sword", "", "", "" }));
            fixture.Controller.TryPresent(CharacterId);
            fixture.Controller.Open(CharacterId, 0);
            string previewId =
                fixture.Controller.EditSession.PreviewInstanceId;
            string[] englishNames = fixture.InventoryGrid.Items
                .Select(entry => entry.DisplayName)
                .ToArray();

            fixture.Localization.SetLocale("ko");

            Assert.That(
                fixture.Controller.EditSession.PreviewInstanceId,
                Is.EqualTo(previewId));
            Assert.That(
                fixture.InventoryGrid.Items
                    .Select(entry => entry.DisplayName),
                Is.Not.EqualTo(englishNames));
            Assert.That(fixture.InventoryGrid.IsAtTop, Is.True);
        }

        [Test]
        public void EmptyInventory_HidesCellsAndKeepsEmptySlotBehavior()
        {
            ProfileSaveData profile = Profile(
                new[] { "", "", "", "" });
            profile.RelicInstances.Clear();
            Fixture fixture = CreateFixture(profile);
            fixture.Controller.TryPresent(CharacterId);

            Assert.That(fixture.Controller.Open(CharacterId, 0), Is.True);
            Assert.That(fixture.InventoryGrid.ItemCount, Is.Zero);
            Assert.That(
                fixture.InventoryGrid.PoolCells.All(
                    cell => !cell.gameObject.activeSelf),
                Is.True);
            Assert.That(fixture.Selection.IsIconVisible, Is.False);
            fixture.Selection.EquipButton.onClick.Invoke();
            Assert.That(
                fixture.Controller.LastPersistenceResult.Status,
                Is.EqualTo(RelicEquipPersistenceStatus.NoRelicSelected));
            Assert.That(fixture.Controller.IsSelectionOpen, Is.True);

            fixture.Selection.RemoveButton.onClick.Invoke();
            Assert.That(
                fixture.Controller.LastPersistenceResult.Status,
                Is.EqualTo(RelicEquipPersistenceStatus.NoChange));
            Assert.That(fixture.Controller.IsSelectionOpen, Is.False);
        }

        [Test]
        public void Equip_ChangedNoChangeAndNoSelectionFollowScreenRules()
        {
            Fixture changed = CreateFixture(Profile(
                new[] { "sword", "", "", "" }));
            changed.Controller.TryPresent(CharacterId);
            changed.Controller.Open(CharacterId, 1);
            changed.InventoryGrid.PoolCells.Single(
                cell => cell.IsBound && cell.InstanceId == "bark")
                .Button.onClick.Invoke();

            changed.Selection.EquipButton.onClick.Invoke();

            Assert.That(
                changed.Controller.LastPersistenceResult.Status,
                Is.EqualTo(RelicEquipPersistenceStatus.ChangedAndSaved));
            Assert.That(changed.Controller.IsSelectionOpen, Is.False);
            Assert.That(CurrentSlot(changed, 1), Is.EqualTo("bark"));

            Fixture noChange = CreateFixture(Profile(
                new[] { "sword", "", "", "" }));
            noChange.Controller.TryPresent(CharacterId);
            noChange.Controller.Open(CharacterId, 0);
            noChange.Selection.EquipButton.onClick.Invoke();
            Assert.That(
                noChange.Controller.LastPersistenceResult.Status,
                Is.EqualTo(RelicEquipPersistenceStatus.NoChange));
            Assert.That(noChange.Controller.IsSelectionOpen, Is.False);

            Fixture empty = CreateFixture(Profile(
                new[] { "", "", "", "" }));
            empty.Controller.TryPresent(CharacterId);
            empty.Controller.Open(CharacterId, 0);
            empty.Selection.EquipButton.onClick.Invoke();
            Assert.That(
                empty.Controller.LastPersistenceResult.Status,
                Is.EqualTo(RelicEquipPersistenceStatus.NoRelicSelected));
            Assert.That(empty.Controller.IsSelectionOpen, Is.True);
            Assert.That(CurrentSlot(empty, 0), Is.Empty);
        }

        [Test]
        public void Remove_IgnoresPreviewAndClosesForChangedOrEmptySlot()
        {
            Fixture populated = CreateFixture(Profile(
                new[] { "sword", "", "", "" }));
            populated.Controller.TryPresent(CharacterId);
            populated.Controller.Open(CharacterId, 0);
            populated.Controller.TrySetPreview("bark");

            populated.Selection.RemoveButton.onClick.Invoke();

            Assert.That(
                populated.Controller.LastPersistenceResult.Status,
                Is.EqualTo(RelicEquipPersistenceStatus.ChangedAndSaved));
            Assert.That(CurrentSlot(populated, 0), Is.Empty);
            Assert.That(populated.Controller.IsSelectionOpen, Is.False);

            Fixture empty = CreateFixture(Profile(
                new[] { "", "", "", "" }));
            empty.Controller.TryPresent(CharacterId);
            empty.Controller.Open(CharacterId, 3);
            empty.Selection.RemoveButton.onClick.Invoke();
            Assert.That(
                empty.Controller.LastPersistenceResult.Status,
                Is.EqualTo(RelicEquipPersistenceStatus.NoChange));
            Assert.That(empty.Controller.IsSelectionOpen, Is.False);
        }

        [Test]
        public void SaveFailureAndStaleSessionRemainOpenWithoutReplacingSession()
        {
            Fixture saveFailure = CreateFixture(Profile(
                new[] { "sword", "", "", "" }));
            saveFailure.Controller.TryPresent(CharacterId);
            saveFailure.Controller.Open(CharacterId, 1);
            saveFailure.Controller.TrySetPreview("bark");
            var originalSession = saveFailure.Controller.EditSession;
            saveFailure.Repository.FailWriteTemp = true;

            saveFailure.Selection.EquipButton.onClick.Invoke();

            Assert.That(
                saveFailure.Controller.LastPersistenceResult.Status,
                Is.EqualTo(RelicEquipPersistenceStatus.SaveFailed));
            Assert.That(saveFailure.Controller.EditSession,
                Is.SameAs(originalSession));
            Assert.That(saveFailure.SelectionRoot.activeSelf, Is.True);

            Fixture stale = CreateFixture(Profile(
                new[] { "sword", "", "", "" }));
            stale.Controller.TryPresent(CharacterId);
            stale.Controller.Open(CharacterId, 0);
            SaveTransactionResult transaction = stale.SaveService
                .ExecuteTransaction(profile =>
                {
                    profile.Characters[0]
                        .EquippedRelicInstanceIds[0] = "bark";
                });
            Assert.That(transaction.IsSuccess, Is.True);

            stale.Selection.EquipButton.onClick.Invoke();

            Assert.That(
                stale.Controller.LastPersistenceResult.Status,
                Is.EqualTo(RelicEquipPersistenceStatus.StaleSession));
            Assert.That(stale.Controller.IsSelectionOpen, Is.True);
            Assert.That(CurrentSlot(stale, 0), Is.EqualTo("bark"));
        }

        [Test]
        public void CloseDiscardsPreviewAndReopenUsesLatestProfileSnapshot()
        {
            Fixture fixture = CreateFixture(Profile(
                new[] { "sword", "", "", "" }));
            fixture.Controller.TryPresent(CharacterId);
            fixture.Controller.Open(CharacterId, 0);
            fixture.Controller.TrySetPreview("bark");

            fixture.Controller.Close();

            Assert.That(fixture.Controller.EditSession, Is.Null);
            Assert.That(fixture.SelectionRoot.activeSelf, Is.False);
            SaveTransactionResult transaction = fixture.SaveService
                .ExecuteTransaction(profile =>
                {
                    profile.Characters[0]
                        .EquippedRelicInstanceIds[0] = "bark";
                });
            Assert.That(transaction.IsSuccess, Is.True);

            Assert.That(fixture.Controller.Open(CharacterId, 0), Is.True);
            Assert.That(
                fixture.Controller.EditSession.OriginalInstanceId,
                Is.EqualTo("bark"));
            Assert.That(
                fixture.Controller.EditSession.PreviewInstanceId,
                Is.EqualTo("bark"));
            Assert.That(
                fixture.Selection.PreviewAwakeningText.text,
                Is.EqualTo("5"));
        }

        private Fixture CreateFixture(ProfileSaveData profile)
        {
            var repository = new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.Json(profile)
            };
            SaveService saveService = SaveServiceTestFactory.Create(repository);
            SaveLoadResult load = saveService.LoadOrCreate("ignored");
            Assert.That(load.CanUseProfile, Is.True, load.Message);

            DefinitionDatabase database =
                AssetDatabase.LoadAssetAtPath<DefinitionDatabase>(
                    DatabasePath);
            Assert.That(database, Is.Not.Null);
            database.Initialize();
            LocalizationCatalog catalog =
                AssetDatabase.LoadAssetAtPath<LocalizationCatalog>(
                    LocalizationCatalogPath);
            Assert.That(catalog, Is.Not.Null);
            LocalizationService localization = catalog.CreateService();

            GameObject root = Create("RelicControllerRoot");
            CharacterRelicSlotsView slotsView =
                Create("Slots").AddComponent<CharacterRelicSlotsView>();
            var slots = new CharacterRelicSlotView[4];
            for (int index = 0; index < slots.Length; index++)
            {
                slots[index] = CreateSlot(slotsView.transform, index);
            }

            slotsView.Configure(slots);
            GameObject selectionRoot = Create("Selection");
            RelicSelectionScreenView selection = CreateSelection(
                selectionRoot);
            VirtualizedRelicGridView inventoryGrid = CreateInventoryGrid(
                selectionRoot.transform);
            var upgrade = root.AddComponent<
                CharacterUpgradeScreenController>();
            var controller = root.AddComponent<
                CharacterRelicScreenController>();
            controller.Configure(
                slotsView,
                selection,
                inventoryGrid,
                upgrade);
            controller.Initialize(saveService, database, localization);
            return new Fixture(
                repository,
                saveService,
                controller,
                slotsView,
                slots,
                selection,
                inventoryGrid,
                localization,
                selectionRoot);
        }

        private CharacterRelicSlotView CreateSlot(
            Transform parent,
            int index)
        {
            GameObject root = Create($"Slot{index}", parent);
            var slot = root.AddComponent<CharacterRelicSlotView>();
            GameObject buttonObject = Create("ArtifactButton", root.transform);
            Image icon = buttonObject.AddComponent<Image>();
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = icon;
            GameObject empty = Create("Empty", root.transform);
            empty.AddComponent<Image>();
            GameObject border = Create("Border", root.transform);
            border.AddComponent<Image>();
            TMP_Text awakening = Create("Awakening", root.transform)
                .AddComponent<TextMeshProUGUI>();
            slot.Configure(index, button, icon, empty, border, awakening);
            return slot;
        }

        private RelicSelectionScreenView CreateSelection(GameObject root)
        {
            var view = root.AddComponent<RelicSelectionScreenView>();
            Image icon = Create("PreviewIcon", root.transform)
                .AddComponent<Image>();
            GameObject empty = Create("PreviewEmpty", root.transform);
            empty.AddComponent<Image>();
            GameObject border = Create("PreviewBorder", root.transform);
            border.AddComponent<Image>();
            TMP_Text awakening = Create("PreviewAwakening", root.transform)
                .AddComponent<TextMeshProUGUI>();
            TMP_Text description = Create("Description", root.transform)
                .AddComponent<TextMeshProUGUI>();
            Button equip = Create("Equip", root.transform)
                .AddComponent<Button>();
            Button remove = Create("Remove", root.transform)
                .AddComponent<Button>();
            view.Configure(
                icon,
                empty,
                border,
                awakening,
                description,
                equip,
                remove);
            return view;
        }

        private VirtualizedRelicGridView CreateInventoryGrid(
            Transform parent)
        {
            RectTransform viewport = (RectTransform)Create(
                "InventoryViewport",
                parent).transform;
            viewport.sizeDelta = new Vector2(500f, 500f);
            RectTransform content = (RectTransform)Create(
                "InventoryContent",
                viewport).transform;
            var scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            RelicInventoryCellView cell = CreateInventoryCell(content);
            var grid = viewport.gameObject.AddComponent<
                VirtualizedRelicGridView>();
            grid.Configure(
                scrollRect,
                viewport,
                content,
                new[] { cell },
                new Vector2(185f, 185f),
                new Vector2(25f, 25f));
            return grid;
        }

        private RelicInventoryCellView CreateInventoryCell(
            Transform parent)
        {
            GameObject root = Create("InventoryCell", parent);
            var cell = root.AddComponent<RelicInventoryCellView>();
            GameObject buttonObject = Create("ArtifactButton", root.transform);
            Image icon = buttonObject.AddComponent<Image>();
            Button button = buttonObject.AddComponent<Button>();
            GameObject border = Create("Border", root.transform);
            border.AddComponent<Image>();
            TMP_Text awakening = Create("Awakening", root.transform)
                .AddComponent<TextMeshProUGUI>();
            cell.Configure(button, icon, border, awakening);
            return cell;
        }

        private GameObject Create(string name, Transform parent = null)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            if (parent == null)
            {
                createdObjects.Add(gameObject);
            }
            else
            {
                gameObject.transform.SetParent(parent, false);
            }

            return gameObject;
        }

        private static ProfileSaveData Profile(string[] slots)
        {
            ProfileSaveData profile = SaveTestDataBuilder.Valid();
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = CharacterId,
                Level = 1,
                EquippedRelicInstanceIds = new List<string>(slots)
            });
            profile.RelicInstances.Add(new RelicInstanceSaveData
            {
                InstanceId = "sword",
                RelicDefinitionId = SwordDefinitionId,
                Awakening = 0
            });
            profile.RelicInstances.Add(new RelicInstanceSaveData
            {
                InstanceId = "bark",
                RelicDefinitionId = BarkDefinitionId,
                Awakening = 5
            });
            return profile;
        }

        private static string CurrentSlot(Fixture fixture, int slotIndex)
        {
            return fixture.SaveService.GetCurrentProfileSnapshot()
                .Characters[0].EquippedRelicInstanceIds[slotIndex];
        }

        private sealed class Fixture
        {
            public Fixture(
                FakeSaveRepository repository,
                SaveService saveService,
                CharacterRelicScreenController controller,
                CharacterRelicSlotsView slotsView,
                CharacterRelicSlotView[] slots,
                RelicSelectionScreenView selection,
                VirtualizedRelicGridView inventoryGrid,
                LocalizationService localization,
                GameObject selectionRoot)
            {
                Repository = repository;
                SaveService = saveService;
                Controller = controller;
                SlotsView = slotsView;
                Slots = slots;
                Selection = selection;
                InventoryGrid = inventoryGrid;
                Localization = localization;
                SelectionRoot = selectionRoot;
            }

            public FakeSaveRepository Repository { get; }
            public SaveService SaveService { get; }
            public CharacterRelicScreenController Controller { get; }
            public CharacterRelicSlotsView SlotsView { get; }
            public CharacterRelicSlotView[] Slots { get; }
            public RelicSelectionScreenView Selection { get; }
            public VirtualizedRelicGridView InventoryGrid { get; }
            public LocalizationService Localization { get; }
            public GameObject SelectionRoot { get; }
        }
    }
}
