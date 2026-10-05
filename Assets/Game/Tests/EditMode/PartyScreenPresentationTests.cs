using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ValorChronicle.Party.Presentation;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class PartyScreenPresentationTests
    {
        private readonly List<GameObject> createdObjects =
            new List<GameObject>();
        private readonly List<ScreenFixture> createdFixtures =
            new List<ScreenFixture>();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdFixtures.Count - 1;
                index >= 0;
                index--)
            {
                createdFixtures[index].Deactivate();
            }

            createdFixtures.Clear();
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
        public void Initialize_BindsFiveByFiveAndActivePresetState()
        {
            ProfileSaveData profile = Profile();
            profile.Party.Presets[0].CharacterSlotIds[0] = "a";
            ScreenFixture fixture = CreateFixture(profile);

            for (int presetIndex = 0; presetIndex < 5; presetIndex++)
            {
                PartyPresetView preset = fixture.Presets[presetIndex];
                Assert.That(preset.PresetIndex, Is.EqualTo(presetIndex));
                Assert.That(preset.IsActive, Is.EqualTo(presetIndex == 0));
                Assert.That(
                    preset.SelectHighlight.activeSelf,
                    Is.EqualTo(presetIndex == 0));
                Assert.That(
                    preset.SelectButton.interactable,
                    Is.EqualTo(presetIndex != 0));

                for (int slotIndex = 0; slotIndex < 5; slotIndex++)
                {
                    PartySlotView slot = preset.SlotViews[slotIndex];
                    Assert.That(slot.PresetIndex, Is.EqualTo(presetIndex));
                    Assert.That(slot.SlotIndex, Is.EqualTo(slotIndex));
                    Assert.That(
                        slot.CharacterId,
                        Is.EqualTo(
                            profile.Party.Presets[presetIndex]
                                .CharacterSlotIds[slotIndex]));
                    Assert.That(slot.CharacterImage.sprite, Is.Null);
                    Assert.That(slot.TypeImage.sprite, Is.Null);
                    Assert.That(slot.CharacterImage.color.a, Is.Zero);
                    Assert.That(slot.TypeImage.color.a, Is.Zero);
                }
            }
        }

        [Test]
        public void SelectPreset_SaveSuccessRendersAuthoritativeSnapshot()
        {
            ScreenFixture fixture = CreateFixture(Profile());

            fixture.Presets[2].SelectButton.onClick.Invoke();

            Assert.That(fixture.Presets[0].IsActive, Is.False);
            Assert.That(fixture.Presets[2].IsActive, Is.True);
            Assert.That(fixture.Presets[2].SelectHighlight.activeSelf, Is.True);
            Assert.That(fixture.Presets[2].SelectButton.interactable, Is.False);
            Assert.That(
                fixture.SaveService.GetCurrentProfileSnapshot()
                    .Party.ActivePresetIndex,
                Is.EqualTo(2));
        }

        [Test]
        public void SelectPreset_SaveFailureKeepsRenderedActivePreset()
        {
            ScreenFixture fixture = CreateFixture(Profile());
            fixture.Repository.FailWriteTemp = true;
            LogAssert.Expect(
                LogType.Error,
                new Regex("Active preset save failed"));

            fixture.Presets[2].SelectButton.onClick.Invoke();

            Assert.That(fixture.Presets[0].IsActive, Is.True);
            Assert.That(fixture.Presets[0].SelectHighlight.activeSelf, Is.True);
            Assert.That(fixture.Presets[2].IsActive, Is.False);
            Assert.That(
                fixture.SaveService.GetCurrentProfileSnapshot()
                    .Party.ActivePresetIndex,
                Is.Zero);
        }

        [Test]
        public void SlotClick_OpensSessionBlocksInputAndCloseRestoresIt()
        {
            ProfileSaveData profile = Profile();
            profile.Party.Presets[3].CharacterSlotIds[4] = "a";
            ScreenFixture fixture = CreateFixture(profile);

            fixture.Presets[3].SlotViews[4].Button.onClick.Invoke();

            Assert.That(fixture.Controller.IsCharacterSelectOpen, Is.True);
            Assert.That(fixture.CharacterSelect.activeSelf, Is.True);
            Assert.That(fixture.Controller.CurrentEditSession.PresetIndex,
                Is.EqualTo(3));
            Assert.That(fixture.Controller.CurrentEditSession.SlotIndex,
                Is.EqualTo(4));
            Assert.That(
                fixture.Controller.CurrentEditSession.OriginalCharacterId,
                Is.EqualTo("a"));
            Assert.That(
                fixture.Controller.CurrentEditSession.PreviewCharacterId,
                Is.EqualTo("a"));
            Assert.That(fixture.PartyInputGroup.interactable, Is.False);
            Assert.That(fixture.ExitButton.interactable, Is.True);
            foreach (PartyPresetView preset in fixture.Presets)
            {
                Assert.That(preset.SelectButton.interactable, Is.False);
                foreach (PartySlotView slot in preset.SlotViews)
                {
                    Assert.That(slot.Button.interactable, Is.False);
                }
            }

            fixture.Controller.CloseCharacterSelectWithoutSaving();

            Assert.That(fixture.Controller.CurrentEditSession, Is.Null);
            Assert.That(fixture.CharacterSelect.activeSelf, Is.False);
            Assert.That(fixture.PartyInputGroup.interactable, Is.True);
            Assert.That(fixture.ExitButton.interactable, Is.True);
            Assert.That(fixture.Presets[0].SelectButton.interactable, Is.False);
        }

        [Test]
        public void DisableWithOpenCharacterSelect_DiscardsSession()
        {
            ScreenFixture fixture = CreateFixture(Profile());
            fixture.Presets[1].SlotViews[2].Button.onClick.Invoke();
            Assert.That(fixture.Controller.CurrentEditSession, Is.Not.Null);

            fixture.Deactivate();

            Assert.That(fixture.Controller.CurrentEditSession, Is.Null);
            Assert.That(fixture.CharacterSelect.activeSelf, Is.False);
        }

        [Test]
        public void ExitClickWithOpenCharacterSelect_DiscardsWithoutSaving()
        {
            ScreenFixture fixture = CreateFixture(Profile());
            int writesBefore = fixture.Repository.Count(
                nameof(FakeSaveRepository.WriteTemp));
            fixture.Presets[1].SlotViews[2].Button.onClick.Invoke();

            fixture.ExitButton.onClick.Invoke();

            Assert.That(fixture.Controller.CurrentEditSession, Is.Null);
            Assert.That(fixture.CharacterSelect.activeSelf, Is.False);
            Assert.That(fixture.PartyInputGroup.interactable, Is.True);
            Assert.That(
                fixture.Repository.Count(nameof(FakeSaveRepository.WriteTemp)),
                Is.EqualTo(writesBefore));
        }

        [Test]
        public void DisableEnable_DoesNotDuplicateSelectionListeners()
        {
            ScreenFixture fixture = CreateFixture(Profile());
            fixture.Deactivate();
            fixture.Activate();
            int writesBefore = fixture.Repository.Count(
                nameof(FakeSaveRepository.WriteTemp));

            fixture.Presets[1].SelectButton.onClick.Invoke();

            Assert.That(
                fixture.Repository.Count(nameof(FakeSaveRepository.WriteTemp)),
                Is.EqualTo(writesBefore + 1));
        }

        [Test]
        public void Confirm_PlaceThenReplace_RendersSavedSnapshotAndCloses()
        {
            ProfileSaveData profile = Profile();
            AddOwnedCharacter(profile, "b");
            ScreenFixture fixture = CreateFixture(profile);
            int writesBefore = WriteCount(fixture);

            OpenAndPreview(fixture, 0, 0, "a");
            fixture.Controller.ConfirmCharacterSelection();

            AssertClosedWithSlots(
                fixture,
                writesBefore + 1,
                "a", "", "", "", "");

            OpenAndPreview(fixture, 0, 0, "b");
            fixture.Controller.ConfirmCharacterSelection();

            AssertClosedWithSlots(
                fixture,
                writesBefore + 2,
                "b", "", "", "", "");
        }

        [Test]
        public void ConfirmAndClear_RenderConfiguredFaceAndTypeSprites()
        {
            Sprite face = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f));
            Sprite type = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f));
            try
            {
                ScreenFixture fixture = CreateFixture(Profile());
                fixture.Controller.ConfigureSlotPresentationResolvers(
                    characterId => characterId == "a" ? face : null,
                    characterId => characterId == "a" ? type : null);

                OpenAndPreview(fixture, 0, 0, "a");
                fixture.Controller.ConfirmCharacterSelection();

                PartySlotView slot = fixture.Presets[0].SlotViews[0];
                Assert.That(slot.CharacterId, Is.EqualTo("a"));
                Assert.That(slot.CharacterImage.sprite, Is.SameAs(face));
                Assert.That(slot.TypeImage.sprite, Is.SameAs(type));
                Assert.That(slot.CharacterImage.color.a, Is.EqualTo(1f));
                Assert.That(slot.TypeImage.color.a, Is.EqualTo(1f));

                slot.Button.onClick.Invoke();
                fixture.Controller.PerformCharacterSelectSecondaryAction();

                Assert.That(slot.CharacterId, Is.Empty);
                Assert.That(slot.CharacterImage.sprite, Is.Null);
                Assert.That(slot.TypeImage.sprite, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(face);
                Object.DestroyImmediate(type);
            }
        }

        [Test]
        public void Confirm_MoveAndSwap_RenderAllAffectedSlots()
        {
            ProfileSaveData moveProfile = ProfileWithSlots(
                "a", "b", "c", "", "");
            ScreenFixture move = CreateFixture(moveProfile);
            int moveWrites = WriteCount(move);

            OpenAndPreview(move, 0, 3, "b");
            move.Controller.ConfirmCharacterSelection();

            AssertClosedWithSlots(
                move,
                moveWrites + 1,
                "a", "", "c", "b", "");

            ProfileSaveData swapProfile = ProfileWithSlots(
                "a", "b", "c", "", "");
            ScreenFixture swap = CreateFixture(swapProfile);
            int swapWrites = WriteCount(swap);

            OpenAndPreview(swap, 0, 0, "c");
            swap.Controller.ConfirmCharacterSelection();

            AssertClosedWithSlots(
                swap,
                swapWrites + 1,
                "c", "b", "a", "", "");
        }

        [Test]
        public void Confirm_NoChange_ClosesWithoutWrite()
        {
            ScreenFixture fixture = CreateFixture(ProfileWithSlots(
                "a", "", "", "", ""));
            int writesBefore = WriteCount(fixture);

            fixture.Presets[0].SlotViews[0].Button.onClick.Invoke();
            fixture.Controller.ConfirmCharacterSelection();

            AssertClosedWithSlots(
                fixture,
                writesBefore,
                "a", "", "", "", "");
        }

        [Test]
        public void Confirm_FailureKeepsSessionPreviewAndRenderedParty()
        {
            ScreenFixture fixture = CreateFixture(Profile());
            OpenAndPreview(fixture, 0, 0, "a");
            fixture.Repository.FailWriteTemp = true;
            LogAssert.Expect(
                LogType.Error,
                new Regex("Confirm save failed"));

            fixture.Controller.ConfirmCharacterSelection();

            Assert.That(fixture.Controller.IsCharacterSelectOpen, Is.True);
            Assert.That(
                fixture.Controller.CurrentEditSession.PreviewCharacterId,
                Is.EqualTo("a"));
            Assert.That(fixture.Presets[0].SlotViews[0].CharacterId,
                Is.Empty);
            Assert.That(fixture.Controller.IsOperationInProgress, Is.False);
        }

        [Test]
        public void SecondaryAction_RevertThenClear_SavesOnlyClear()
        {
            ScreenFixture fixture = CreateFixture(ProfileWithSlots(
                "a", "", "", "", ""));
            int writesBefore = WriteCount(fixture);
            OpenAndPreview(fixture, 0, 0, "b");

            fixture.Controller.PerformCharacterSelectSecondaryAction();

            Assert.That(fixture.Controller.IsCharacterSelectOpen, Is.True);
            Assert.That(
                fixture.Controller.CurrentEditSession.PreviewCharacterId,
                Is.EqualTo("a"));
            Assert.That(WriteCount(fixture), Is.EqualTo(writesBefore));

            fixture.Controller.CharacterSelectInteractionChanged += enabled =>
            {
                if (!enabled)
                {
                    fixture.Controller
                        .PerformCharacterSelectSecondaryAction();
                }
            };
            fixture.Controller.PerformCharacterSelectSecondaryAction();

            AssertClosedWithSlots(
                fixture,
                writesBefore + 1,
                "", "", "", "", "");
        }

        [Test]
        public void SecondaryAction_RevertToEmptyThenClose_DoesNotSave()
        {
            ScreenFixture fixture = CreateFixture(Profile());
            int writesBefore = WriteCount(fixture);
            OpenAndPreview(fixture, 0, 0, "a");

            fixture.Controller.PerformCharacterSelectSecondaryAction();

            Assert.That(fixture.Controller.IsCharacterSelectOpen, Is.True);
            Assert.That(
                fixture.Controller.CurrentEditSession.PreviewCharacterId,
                Is.Empty);
            Assert.That(WriteCount(fixture), Is.EqualTo(writesBefore));

            fixture.Controller.PerformCharacterSelectSecondaryAction();

            Assert.That(fixture.Controller.IsCharacterSelectOpen, Is.False);
            Assert.That(fixture.Controller.CurrentEditSession, Is.Null);
            Assert.That(WriteCount(fixture), Is.EqualTo(writesBefore));
        }

        [Test]
        public void Confirm_ReentrantRequestDuringPersistence_IsIgnored()
        {
            ScreenFixture fixture = CreateFixture(Profile());
            int writesBefore = WriteCount(fixture);
            OpenAndPreview(fixture, 0, 0, "a");
            fixture.Controller.CharacterSelectInteractionChanged += enabled =>
            {
                if (!enabled)
                {
                    fixture.Controller.ConfirmCharacterSelection();
                }
            };

            fixture.Controller.ConfirmCharacterSelection();

            AssertClosedWithSlots(
                fixture,
                writesBefore + 1,
                "a", "", "", "", "");
        }

        private ScreenFixture CreateFixture(ProfileSaveData profile)
        {
            var repository = new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.Json(profile)
            };
            SaveService saveService = SaveServiceTestFactory.Create(repository);
            SaveLoadResult loadResult = saveService.LoadOrCreate("ignored");
            Assert.That(loadResult.CanUseProfile, Is.True, loadResult.Message);

            GameObject root = CreateObject("PartyScreenTestRoot");
            root.SetActive(false);
            GameObject partyRoot = CreateObject("Party");
            partyRoot.transform.SetParent(root.transform);
            CanvasGroup group = partyRoot.AddComponent<CanvasGroup>();
            PartyScreenController controller =
                partyRoot.AddComponent<PartyScreenController>();

            GameObject characterSelect = CreateObject("CharSelect");
            characterSelect.transform.SetParent(root.transform);
            characterSelect.SetActive(false);
            Button exitButton = CreateButton("ExitButton", root.transform);
            var presets = new PartyPresetView[5];

            for (int presetIndex = 0; presetIndex < 5; presetIndex++)
            {
                GameObject presetObject = CreateObject(
                    $"Preset{presetIndex}");
                presetObject.transform.SetParent(partyRoot.transform);
                PartyPresetView preset =
                    presetObject.AddComponent<PartyPresetView>();
                Button selectButton = CreateButton(
                    "SelectButton",
                    presetObject.transform);
                GameObject highlight = CreateObject("SelectHighlight");
                highlight.transform.SetParent(presetObject.transform);
                var slots = new PartySlotView[5];

                for (int slotIndex = 0; slotIndex < 5; slotIndex++)
                {
                    GameObject slotObject = CreateObject(
                        $"Slot{slotIndex}");
                    slotObject.transform.SetParent(presetObject.transform);
                    PartySlotView slot =
                        slotObject.AddComponent<PartySlotView>();
                    Button slotButton = CreateButton(
                        "CharacterButton",
                        slotObject.transform);
                    Image typeImage = CreateObject("TypeImage")
                        .AddComponent<Image>();
                    typeImage.transform.SetParent(slotObject.transform);
                    ConfigureSlot(
                        slot,
                        slotButton,
                        slotButton.GetComponent<Image>(),
                        typeImage);
                    slots[slotIndex] = slot;
                }

                ConfigurePreset(
                    preset,
                    selectButton,
                    highlight,
                    slots);
                presets[presetIndex] = preset;
            }

            ConfigureController(
                controller,
                presets,
                group,
                characterSelect,
                exitButton);
            Assert.That(controller.IsConfigured, Is.True);
            var fixture = new ScreenFixture(
                root,
                controller,
                presets,
                group,
                characterSelect,
                exitButton,
                repository,
                saveService);
            createdFixtures.Add(fixture);
            fixture.Activate();
            controller.Initialize(saveService);

            Assert.That(root.activeInHierarchy, Is.True);
            Assert.That(controller.isActiveAndEnabled, Is.True);
            Assert.That(presets[0].isActiveAndEnabled, Is.True);
            Assert.That(
                presets[0].SlotViews[0].isActiveAndEnabled,
                Is.True);
            return fixture;
        }

        private GameObject CreateObject(string name)
        {
            var gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private Button CreateButton(string name, Transform parent)
        {
            GameObject buttonObject = CreateObject(name);
            buttonObject.transform.SetParent(parent);
            buttonObject.AddComponent<Image>();
            return buttonObject.AddComponent<Button>();
        }

        private static void ConfigureSlot(
            PartySlotView slot,
            Button button,
            Image characterImage,
            Image typeImage)
        {
            var serialized = new UnityEditor.SerializedObject(slot);
            serialized.FindProperty("button").objectReferenceValue = button;
            serialized.FindProperty("characterImage").objectReferenceValue =
                characterImage;
            serialized.FindProperty("typeImage").objectReferenceValue =
                typeImage;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePreset(
            PartyPresetView preset,
            Button selectButton,
            GameObject selectHighlight,
            PartySlotView[] slots)
        {
            var serialized = new UnityEditor.SerializedObject(preset);
            serialized.FindProperty("selectButton").objectReferenceValue =
                selectButton;
            serialized.FindProperty("selectHighlight").objectReferenceValue =
                selectHighlight;
            UnityEditor.SerializedProperty property =
                serialized.FindProperty("slotViews");
            property.arraySize = slots.Length;
            for (int index = 0; index < slots.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue =
                    slots[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureController(
            PartyScreenController controller,
            PartyPresetView[] presets,
            CanvasGroup partyInputGroup,
            GameObject characterSelect,
            Button exitButton)
        {
            var serialized = new UnityEditor.SerializedObject(controller);
            UnityEditor.SerializedProperty presetViews =
                serialized.FindProperty("presetViews");
            presetViews.arraySize = presets.Length;
            for (int index = 0; index < presets.Length; index++)
            {
                presetViews.GetArrayElementAtIndex(index)
                    .objectReferenceValue = presets[index];
            }

            serialized.FindProperty("partyInputGroup").objectReferenceValue =
                partyInputGroup;
            serialized.FindProperty("characterSelect").objectReferenceValue =
                characterSelect;
            serialized.FindProperty("exitButton").objectReferenceValue =
                exitButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ProfileSaveData Profile()
        {
            ProfileSaveData profile = SaveTestDataBuilder.Valid();
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = "a",
                Level = 1
            });
            return profile;
        }

        private static ProfileSaveData ProfileWithSlots(
            params string[] characterIds)
        {
            ProfileSaveData profile = Profile();
            profile.Party.Presets[0].CharacterSlotIds =
                new List<string>(characterIds);
            for (int index = 0; index < characterIds.Length; index++)
            {
                AddOwnedCharacter(profile, characterIds[index]);
            }

            return profile;
        }

        private static void AddOwnedCharacter(
            ProfileSaveData profile,
            string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                return;
            }

            for (int index = 0; index < profile.Characters.Count; index++)
            {
                if (profile.Characters[index].CharacterId == characterId)
                {
                    return;
                }
            }

            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = characterId,
                Level = 1
            });
        }

        private static void OpenAndPreview(
            ScreenFixture fixture,
            int presetIndex,
            int slotIndex,
            string characterId)
        {
            fixture.Presets[presetIndex]
                .SlotViews[slotIndex]
                .Button.onClick.Invoke();
            Assert.That(
                fixture.Controller.CurrentEditSession,
                Is.Not.Null,
                "Button -> PartySlotView -> PartyPresetView -> "
                    + "PartyScreenController event chain did not open a "
                    + "PartyEditSession.");
            fixture.Controller.CurrentEditSession.SetPreviewCharacterId(
                characterId);
        }

        private static int WriteCount(ScreenFixture fixture)
        {
            return fixture.Repository.Count(
                nameof(FakeSaveRepository.WriteTemp));
        }

        private static void AssertClosedWithSlots(
            ScreenFixture fixture,
            int expectedWriteCount,
            params string[] expectedSlots)
        {
            Assert.That(fixture.Controller.IsCharacterSelectOpen, Is.False);
            Assert.That(fixture.Controller.CurrentEditSession, Is.Null);
            Assert.That(WriteCount(fixture), Is.EqualTo(expectedWriteCount));
            for (int index = 0; index < expectedSlots.Length; index++)
            {
                Assert.That(
                    fixture.Presets[0].SlotViews[index].CharacterId,
                    Is.EqualTo(expectedSlots[index]),
                    $"Slot {index}");
            }
        }

        private sealed class ScreenFixture
        {
            private bool lifecycleActive;

            public ScreenFixture(
                GameObject root,
                PartyScreenController controller,
                PartyPresetView[] presets,
                CanvasGroup partyInputGroup,
                GameObject characterSelect,
                Button exitButton,
                FakeSaveRepository repository,
                SaveService saveService)
            {
                Root = root;
                Controller = controller;
                Presets = presets;
                PartyInputGroup = partyInputGroup;
                CharacterSelect = characterSelect;
                ExitButton = exitButton;
                Repository = repository;
                SaveService = saveService;
            }

            public GameObject Root { get; }
            public PartyScreenController Controller { get; }
            public PartyPresetView[] Presets { get; }
            public CanvasGroup PartyInputGroup { get; }
            public GameObject CharacterSelect { get; }
            public Button ExitButton { get; }
            public FakeSaveRepository Repository { get; }
            public SaveService SaveService { get; }

            public void Activate()
            {
                if (lifecycleActive)
                {
                    return;
                }

                Root.SetActive(true);
                foreach (PartyPresetView preset in Presets)
                {
                    foreach (PartySlotView slot in preset.SlotViews)
                    {
                        InvokeLifecycle(slot, "OnEnable");
                    }

                    InvokeLifecycle(preset, "OnEnable");
                }

                InvokeLifecycle(Controller, "OnEnable");
                lifecycleActive = true;
            }

            public void Deactivate()
            {
                if (!lifecycleActive || Root == null)
                {
                    return;
                }

                InvokeLifecycle(Controller, "OnDisable");
                foreach (PartyPresetView preset in Presets)
                {
                    InvokeLifecycle(preset, "OnDisable");
                    foreach (PartySlotView slot in preset.SlotViews)
                    {
                        InvokeLifecycle(slot, "OnDisable");
                    }
                }

                Root.SetActive(false);
                lifecycleActive = false;
            }
        }

        private static void InvokeLifecycle(
            MonoBehaviour target,
            string methodName)
        {
            System.Reflection.MethodInfo method = target.GetType().GetMethod(
                methodName,
                System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic);
            Assert.That(
                method,
                Is.Not.Null,
                $"{target.GetType().Name}.{methodName} was not found.");
            method.Invoke(target, null);
        }
    }
}
