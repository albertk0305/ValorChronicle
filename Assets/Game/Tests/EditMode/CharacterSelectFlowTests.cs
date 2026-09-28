using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ValorChronicle.Characters.Stats;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Presentation;
using ValorChronicle.Party.Roster;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class CharacterSelectFlowTests
    {
        private const string PartyScenePath = "Assets/Scenes/Party.unity";
        private readonly List<MonoBehaviour> activeLifecycleComponents =
            new List<MonoBehaviour>();

        [Test]
        public void OpenSelectFilterAndSort_UpdateViewWithoutSaving()
        {
            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();

            try
            {
                EditorSceneManager.OpenScene(
                    PartyScenePath,
                    OpenSceneMode.Single);
                DefinitionDatabase database = AssetDatabase.LoadAssetAtPath<
                    DefinitionDatabase>(
                        "Assets/Data/Database/DefinitionDatabase.asset");
                database.Initialize();

                var repository = new FakeSaveRepository();
                ProfileSaveData profile = SaveTestDataBuilder.Valid();
                profile.Characters.Add(new CharacterSaveData
                {
                    CharacterId = "character_marea_bluefang",
                    Level = 50,
                    Awakening = 3
                });
                profile.Party.Presets[0].CharacterSlotIds[1] =
                    "character_marea_bluefang";
                repository.MainText = SaveTestDataBuilder.Json(profile);
                SaveService saveService =
                    SaveServiceTestFactory.Create(repository);
                SaveLoadResult load = saveService.LoadOrCreate("ignored");
                Assert.That(load.CanUseProfile, Is.True, load.Message);

                PartyScreenController party = Object.FindFirstObjectByType<
                    PartyScreenController>(FindObjectsInactive.Include);
                CharacterSelectController controller =
                    Object.FindFirstObjectByType<CharacterSelectController>(
                        FindObjectsInactive.Include);
                CharacterSelectView view =
                    Object.FindFirstObjectByType<CharacterSelectView>(
                        FindObjectsInactive.Include);
                ActivateCharacterSelectLifecycle(controller, view);
                party.Initialize(saveService);
                int writesBefore = repository.Count(
                    nameof(FakeSaveRepository.WriteTemp));

                party.OpenCharacterSelect(0, 0);

                Assert.That(controller.FullRoster.Count, Is.EqualTo(1));
                Assert.That(controller.VisibleRoster.Count, Is.EqualTo(1));
                Assert.That(controller.SelectedElement, Is.Null);
                Assert.That(controller.SortMode,
                    Is.EqualTo(CharacterRosterSortMode.Level));
                Assert.That(view.DisplayedCellCount, Is.EqualTo(1));
                Assert.That(view.CellViews[0].CharacterId,
                    Is.EqualTo("character_marea_bluefang"));
                // A character already in this preset remains selectable so
                // the later persistence step can move or swap it.
                Assert.That(view.CellViews[0].Button.interactable, Is.True);
                Assert.That(view.ConfirmButton.interactable, Is.False);
                Assert.That(view.LevelText.text, Is.Empty);

                view.CellViews[0].Button.onClick.Invoke();

                CharacterStatValues stats = CharacterStatCalculator.Calculate(
                    database.Characters[0],
                    50);
                Assert.That(party.CurrentEditSession.PreviewCharacterId,
                    Is.EqualTo("character_marea_bluefang"));
                Assert.That(view.ConfirmButton.interactable, Is.True);
                Assert.That(view.LevelText.text, Is.EqualTo("50"));
                Assert.That(view.AwakeningText.text, Is.EqualTo("3"));
                Assert.That(view.HpText.text,
                    Is.EqualTo(stats.MaxHp.ToString()));
                Assert.That(view.AttackText.text,
                    Is.EqualTo(stats.Attack.ToString()));
                Assert.That(view.CellViews[0].IsSelected, Is.True);

                ElementFilterButtonView fire = view.FilterButtons.Single(
                    item => item.Element == ElementType.Fire);
                ElementFilterButtonView water = view.FilterButtons.Single(
                    item => item.Element == ElementType.Water);
                fire.Button.onClick.Invoke();
                Assert.That(controller.SelectedElement,
                    Is.EqualTo(ElementType.Fire));
                Assert.That(view.DisplayedCellCount, Is.Zero);
                Assert.That(party.CurrentEditSession.PreviewCharacterId,
                    Is.EqualTo("character_marea_bluefang"));
                Assert.That(view.LevelText.text, Is.EqualTo("50"));

                fire.Button.onClick.Invoke();
                Assert.That(controller.SelectedElement, Is.Null);
                Assert.That(view.DisplayedCellCount, Is.EqualTo(1));

                fire.Button.onClick.Invoke();
                water.Button.onClick.Invoke();
                Assert.That(controller.SelectedElement,
                    Is.EqualTo(ElementType.Water));
                Assert.That(view.DisplayedCellCount, Is.EqualTo(1));
                Assert.That(water.Button.interactable, Is.True);

                view.AwakeningSortButton.onClick.Invoke();
                Assert.That(controller.SortMode,
                    Is.EqualTo(CharacterRosterSortMode.Awakening));
                Assert.That(view.AwakeningSortButton.interactable, Is.False);
                Assert.That(view.LevelSortButton.interactable, Is.True);
                Assert.That(party.CurrentEditSession.PreviewCharacterId,
                    Is.EqualTo("character_marea_bluefang"));
                Assert.That(repository.Count(
                    nameof(FakeSaveRepository.WriteTemp)),
                    Is.EqualTo(writesBefore));

                var failureRoster = new List<CharacterRosterEntry>();
                for (int index = 0; index < 100; index++)
                {
                    failureRoster.Add(new CharacterRosterEntry(
                        $"failure_{index:D2}",
                        ElementType.Dark,
                        level: 1,
                        awakening: 0,
                        maxHp: 1,
                        attack: 1));
                }

                view.BindCells(
                    failureRoster,
                    party.CurrentEditSession.PreviewCharacterId,
                    _ => null,
                    _ => null,
                    resetScroll: false);
                view.CharacterGrid.SetScrollOffsetForTesting(float.MaxValue);
                float failedOperationScrollOffset =
                    view.CharacterGrid.ScrollOffset;
                repository.FailWriteTemp = true;
                LogAssert.Expect(
                    LogType.Error,
                    new Regex("Confirm save failed"));
                bool observedInteractionLock = false;
                party.CharacterSelectInteractionChanged += enabled =>
                {
                    if (enabled)
                    {
                        return;
                    }

                    observedInteractionLock = true;
                    Assert.That(view.ConfirmButton.interactable, Is.False);
                    Assert.That(
                        view.SecondaryActionButton.interactable,
                        Is.False);
                    Assert.That(view.CellViews,
                        Has.All.Matches<CharacterRosterCellView>(
                            cell => !cell.Button.interactable));
                    Assert.That(view.FilterButtons,
                        Has.All.Matches<ElementFilterButtonView>(
                            filter => !filter.Button.interactable));
                    Assert.That(view.LevelSortButton.interactable, Is.False);
                    Assert.That(
                        view.AwakeningSortButton.interactable,
                        Is.False);
                };
                view.ConfirmButton.onClick.Invoke();

                Assert.That(observedInteractionLock, Is.True);
                Assert.That(party.IsCharacterSelectOpen, Is.True);
                Assert.That(
                    party.CurrentEditSession.PreviewCharacterId,
                    Is.EqualTo("character_marea_bluefang"));
                Assert.That(controller.SelectedElement,
                    Is.EqualTo(ElementType.Water));
                Assert.That(controller.SortMode,
                    Is.EqualTo(CharacterRosterSortMode.Awakening));
                Assert.That(view.IsInteractionEnabled, Is.True);
                Assert.That(view.ConfirmButton.interactable, Is.True);
                Assert.That(view.CharacterGrid.ScrollOffset,
                    Is.EqualTo(failedOperationScrollOffset).Within(0.01f));
            }
            finally
            {
                Restore(previousSetup);
            }
        }

        [Test]
        public void ViewVirtualizesLargeRosterAndReachesLastCharacter()
        {
            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();

            try
            {
                EditorSceneManager.OpenScene(
                    PartyScenePath,
                    OpenSceneMode.Single);
                CharacterSelectView view =
                    Object.FindFirstObjectByType<CharacterSelectView>(
                        FindObjectsInactive.Include);
                view.gameObject.SetActive(true);
                ActivateCharacterSelectViewLifecycle(view);
                var roster = new List<CharacterRosterEntry>();
                for (int index = 0; index < 100; index++)
                {
                    roster.Add(new CharacterRosterEntry(
                        $"character_{index:D2}",
                        ElementType.Dark,
                        level: 1,
                        awakening: 0,
                        maxHp: 1,
                        attack: 1));
                }

                view.BindCells(
                    roster,
                    selectedCharacterId: "character_99",
                    faceResolver: _ => null,
                    elementIconResolver: _ => null,
                    resetScroll: true);

                Assert.That(view.CharacterGrid.ItemCount, Is.EqualTo(100));
                Assert.That(view.CharacterGrid.PoolSize, Is.GreaterThan(0));
                Assert.That(view.CharacterGrid.PoolSize, Is.LessThan(100));
                Assert.That(view.CellViews[0].CharacterId,
                    Is.EqualTo("character_00"));
                Assert.That(view.CellViews[0].CharacterImage.sprite, Is.Null);
                Assert.That(view.CellViews[0].TypeImage.sprite, Is.Null);
                Assert.That(view.CellViews.Any(cell => cell.IsSelected),
                    Is.False);
                CharacterRosterCellView recycledCell = view.CellViews[0];
                string clickedCharacterId = string.Empty;
                recycledCell.Clicked += characterId =>
                    clickedCharacterId = characterId;

                view.CharacterGrid.SetScrollOffsetForTesting(
                    view.CharacterGrid.ContentHeight * 0.5f);
                Assert.That(view.CellViews.Any(cell =>
                    cell.IsBound
                    && cell.CharacterId == "character_50"), Is.True);

                view.CharacterGrid.SetScrollOffsetForTesting(float.MaxValue);
                Assert.That(view.CellViews.Any(cell =>
                    cell.IsBound
                    && cell.CharacterId == "character_99"), Is.True);
                Assert.That(view.CellViews.Any(cell =>
                    cell.IsSelected
                    && cell.CharacterId == "character_99"), Is.True);
                string reboundCharacterId = recycledCell.CharacterId;
                recycledCell.Button.onClick.Invoke();
                Assert.That(clickedCharacterId,
                    Is.EqualTo(reboundCharacterId));
                Assert.That(clickedCharacterId,
                    Is.Not.EqualTo("character_00"));

                view.BindCells(
                    roster.Take(6).ToArray(),
                    selectedCharacterId: string.Empty,
                    faceResolver: _ => null,
                    elementIconResolver: _ => null,
                    resetScroll: true);

                Assert.That(view.DisplayedCellCount, Is.EqualTo(6));
                Assert.That(view.CellViews[0].gameObject.activeSelf, Is.True);
                Assert.That(view.CellViews[5].gameObject.activeSelf, Is.True);
                Assert.That(view.CellViews[6].gameObject.activeSelf, Is.False);
                Assert.That(view.CellViews[6].CharacterId, Is.Empty);

                foreach (int partialCount in new[] { 11, 32 })
                {
                    view.BindCells(
                        roster.Take(partialCount).ToArray(),
                        selectedCharacterId: string.Empty,
                        faceResolver: _ => null,
                        elementIconResolver: _ => null,
                        resetScroll: true);
                    Assert.That(view.DisplayedCellCount,
                        Is.EqualTo(partialCount));
                    Assert.That(
                        view.CellViews[partialCount - 1].IsBound,
                        Is.True);
                    Assert.That(
                        view.CellViews.Skip(partialCount),
                        Has.All.Matches<CharacterRosterCellView>(
                            cell => !cell.IsBound));
                }

                view.BindCells(
                    System.Array.Empty<CharacterRosterEntry>(),
                    selectedCharacterId: string.Empty,
                    faceResolver: _ => null,
                    elementIconResolver: _ => null,
                    resetScroll: true);
                Assert.That(view.DisplayedCellCount, Is.Zero);
                Assert.That(view.CharacterGrid.IsAtTop, Is.True);
                Assert.That(view.CellViews,
                    Has.All.Matches<CharacterRosterCellView>(
                        cell => !cell.IsBound));
            }
            finally
            {
                Restore(previousSetup);
            }
        }

        [Test]
        public void GridResetFlag_ResetsFilterSortButPreservesRevertOffset()
        {
            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();

            try
            {
                EditorSceneManager.OpenScene(
                    PartyScenePath,
                    OpenSceneMode.Single);
                CharacterSelectView view =
                    Object.FindFirstObjectByType<CharacterSelectView>(
                        FindObjectsInactive.Include);
                ActivateCharacterSelectViewLifecycle(view);
                var roster = new List<CharacterRosterEntry>();
                for (int index = 0; index < 100; index++)
                {
                    roster.Add(new CharacterRosterEntry(
                        $"character_{index:D2}",
                        ElementType.Dark,
                        level: 1,
                        awakening: 0,
                        maxHp: 1,
                        attack: 1));
                }

                view.BindCells(
                    roster,
                    selectedCharacterId: "character_50",
                    faceResolver: _ => null,
                    elementIconResolver: _ => null,
                    resetScroll: true);
                view.CharacterGrid.SetScrollOffsetForTesting(float.MaxValue);
                float bottomOffset = view.CharacterGrid.ScrollOffset;

                view.BindCells(
                    roster,
                    selectedCharacterId: "character_50",
                    faceResolver: _ => null,
                    elementIconResolver: _ => null,
                    resetScroll: false);
                Assert.That(view.CharacterGrid.ScrollOffset,
                    Is.EqualTo(bottomOffset).Within(0.01f));

                view.BindCells(
                    roster,
                    selectedCharacterId: "character_50",
                    faceResolver: _ => null,
                    elementIconResolver: _ => null,
                    resetScroll: true);
                Assert.That(view.CharacterGrid.IsAtTop, Is.True);
            }
            finally
            {
                Restore(previousSetup);
            }
        }

        [Test]
        public void CellRebind_DoesNotDuplicateClickListener()
        {
            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();

            try
            {
                EditorSceneManager.OpenScene(
                    PartyScenePath,
                    OpenSceneMode.Single);
                CharacterSelectView view =
                    Object.FindFirstObjectByType<CharacterSelectView>(
                        FindObjectsInactive.Include);
                view.gameObject.SetActive(true);
                ActivateCharacterSelectViewLifecycle(view);
                view.BindCells(
                    System.Array.Empty<CharacterRosterEntry>(),
                    selectedCharacterId: string.Empty,
                    faceResolver: _ => null,
                    elementIconResolver: _ => null,
                    resetScroll: true);
                CharacterRosterCellView cell = view.CellViews[0];
                var entry = new CharacterRosterEntry(
                    "character_test",
                    ElementType.Fire,
                    level: 1,
                    awakening: 0,
                    maxHp: 1,
                    attack: 1);
                int clickCount = 0;
                cell.Clicked += _ => clickCount++;

                cell.Bind(entry, null, null, selected: false);
                cell.Bind(entry, null, null, selected: true);
                cell.Button.onClick.Invoke();

                Assert.That(clickCount, Is.EqualTo(1));
            }
            finally
            {
                Restore(previousSetup);
            }
        }

        [Test]
        public void EmptyOwnedRoster_OpensWithSafeEmptyGridAndPreview()
        {
            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();

            try
            {
                EditorSceneManager.OpenScene(
                    PartyScenePath,
                    OpenSceneMode.Single);
                DefinitionDatabase database = AssetDatabase.LoadAssetAtPath<
                    DefinitionDatabase>(
                        "Assets/Data/Database/DefinitionDatabase.asset");
                database.Initialize();

                var repository = new FakeSaveRepository
                {
                    MainText = SaveTestDataBuilder.Json(
                        SaveTestDataBuilder.Valid())
                };
                SaveService saveService =
                    SaveServiceTestFactory.Create(repository);
                Assert.That(
                    saveService.LoadOrCreate("ignored").CanUseProfile,
                    Is.True);
                PartyScreenController party =
                    Object.FindFirstObjectByType<PartyScreenController>(
                        FindObjectsInactive.Include);
                CharacterSelectController controller =
                    Object.FindFirstObjectByType<CharacterSelectController>(
                        FindObjectsInactive.Include);
                CharacterSelectView view =
                    Object.FindFirstObjectByType<CharacterSelectView>(
                        FindObjectsInactive.Include);
                ActivateCharacterSelectLifecycle(controller, view);
                party.Initialize(saveService);

                party.OpenCharacterSelect(0, 0);

                Assert.That(view.CharacterGrid.ItemCount, Is.Zero);
                Assert.That(view.DisplayedCellCount, Is.Zero);
                Assert.That(view.CharacterGrid.IsAtTop, Is.True);
                Assert.That(view.CellViews,
                    Has.All.Matches<CharacterRosterCellView>(
                        cell => !cell.IsBound));
                Assert.That(view.ConfirmButton.interactable, Is.False);
                Assert.That(view.LevelText.text, Is.Empty);
            }
            finally
            {
                Restore(previousSetup);
            }
        }

        [Test]
        public void SecondaryButton_RevertsToEmptyThenClosesWithoutSaving()
        {
            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();

            try
            {
                EditorSceneManager.OpenScene(
                    PartyScenePath,
                    OpenSceneMode.Single);
                DefinitionDatabase database = AssetDatabase.LoadAssetAtPath<
                    DefinitionDatabase>(
                        "Assets/Data/Database/DefinitionDatabase.asset");
                database.Initialize();

                var repository = new FakeSaveRepository();
                ProfileSaveData profile = SaveTestDataBuilder.Valid();
                profile.Characters.Add(new CharacterSaveData
                {
                    CharacterId = "character_marea_bluefang",
                    Level = 10,
                    Awakening = 2
                });
                repository.MainText = SaveTestDataBuilder.Json(profile);
                SaveService saveService =
                    SaveServiceTestFactory.Create(repository);
                Assert.That(
                    saveService.LoadOrCreate("ignored").CanUseProfile,
                    Is.True);

                PartyScreenController party =
                    Object.FindFirstObjectByType<PartyScreenController>(
                        FindObjectsInactive.Include);
                CharacterSelectController controller =
                    Object.FindFirstObjectByType<CharacterSelectController>(
                        FindObjectsInactive.Include);
                CharacterSelectView view =
                    Object.FindFirstObjectByType<CharacterSelectView>(
                        FindObjectsInactive.Include);
                ActivateCharacterSelectLifecycle(controller, view);
                party.Initialize(saveService);
                int writesBefore = repository.Count(
                    nameof(FakeSaveRepository.WriteTemp));

                party.OpenCharacterSelect(0, 0);
                Assert.That(view.ConfirmButton.interactable, Is.False);
                view.CellViews[0].Button.onClick.Invoke();
                Assert.That(view.ConfirmButton.interactable, Is.True);

                view.SecondaryActionButton.onClick.Invoke();

                Assert.That(party.IsCharacterSelectOpen, Is.True);
                Assert.That(
                    party.CurrentEditSession.PreviewCharacterId,
                    Is.Empty);
                Assert.That(view.ConfirmButton.interactable, Is.False);
                Assert.That(view.LevelText.text, Is.Empty);
                Assert.That(view.AwakeningText.text, Is.Empty);
                Assert.That(view.HpText.text, Is.Empty);
                Assert.That(view.AttackText.text, Is.Empty);
                Assert.That(view.CellViews[0].IsSelected, Is.False);
                Assert.That(view.CharacterGrid.IsAtTop, Is.True);
                Assert.That(repository.Count(
                    nameof(FakeSaveRepository.WriteTemp)),
                    Is.EqualTo(writesBefore));

                view.SecondaryActionButton.onClick.Invoke();

                Assert.That(party.IsCharacterSelectOpen, Is.False);
                Assert.That(party.CurrentEditSession, Is.Null);
                Assert.That(repository.Count(
                    nameof(FakeSaveRepository.WriteTemp)),
                    Is.EqualTo(writesBefore));
            }
            finally
            {
                Restore(previousSetup);
            }
        }

        [Test]
        public void SuccessfulMove_ReopenResetsTransientSelectionState()
        {
            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();

            try
            {
                EditorSceneManager.OpenScene(
                    PartyScenePath,
                    OpenSceneMode.Single);
                DefinitionDatabase database = AssetDatabase.LoadAssetAtPath<
                    DefinitionDatabase>(
                        "Assets/Data/Database/DefinitionDatabase.asset");
                database.Initialize();

                var repository = new FakeSaveRepository();
                ProfileSaveData profile = SaveTestDataBuilder.Valid();
                profile.Characters.Add(new CharacterSaveData
                {
                    CharacterId = "character_marea_bluefang",
                    Level = 20,
                    Awakening = 1
                });
                profile.Party.Presets[0].CharacterSlotIds[1] =
                    "character_marea_bluefang";
                repository.MainText = SaveTestDataBuilder.Json(profile);
                SaveService saveService =
                    SaveServiceTestFactory.Create(repository);
                Assert.That(
                    saveService.LoadOrCreate("ignored").CanUseProfile,
                    Is.True);

                PartyScreenController party =
                    Object.FindFirstObjectByType<PartyScreenController>(
                        FindObjectsInactive.Include);
                CharacterSelectController controller =
                    Object.FindFirstObjectByType<CharacterSelectController>(
                        FindObjectsInactive.Include);
                CharacterSelectView view =
                    Object.FindFirstObjectByType<CharacterSelectView>(
                        FindObjectsInactive.Include);
                var partyData = new SerializedObject(party);
                PartyPresetView preset = (PartyPresetView)partyData
                    .FindProperty("presetViews")
                    .GetArrayElementAtIndex(0)
                    .objectReferenceValue;
                ActivateCharacterSelectLifecycle(controller, view);
                party.Initialize(saveService);
                int writesBefore = repository.Count(
                    nameof(FakeSaveRepository.WriteTemp));

                party.OpenCharacterSelect(0, 0);
                Assert.That(view.CellViews[0].Button.interactable, Is.True);
                view.CellViews[0].Button.onClick.Invoke();
                view.FilterButtons.Single(item =>
                    item.Element == ElementType.Fire).Button.onClick.Invoke();
                view.AwakeningSortButton.onClick.Invoke();
                view.ConfirmButton.onClick.Invoke();

                Assert.That(party.IsCharacterSelectOpen, Is.False);
                Assert.That(preset.SlotViews[0].CharacterId,
                    Is.EqualTo("character_marea_bluefang"));
                Assert.That(preset.SlotViews[1].CharacterId, Is.Empty);
                Assert.That(repository.Count(
                    nameof(FakeSaveRepository.WriteTemp)),
                    Is.EqualTo(writesBefore + 1));

                party.OpenCharacterSelect(0, 2);

                Assert.That(controller.SelectedElement, Is.Null);
                Assert.That(controller.SortMode,
                    Is.EqualTo(CharacterRosterSortMode.Level));
                Assert.That(
                    party.CurrentEditSession.PreviewCharacterId,
                    Is.Empty);
                Assert.That(view.ConfirmButton.interactable, Is.False);
                Assert.That(view.CellViews[0].IsSelected, Is.False);
                Assert.That(view.CharacterGrid.IsAtTop, Is.True);
            }
            finally
            {
                Restore(previousSetup);
            }
        }

        [Test]
        public void SecondaryButton_RevertPreservesFilteredOriginalPreview()
        {
            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();

            try
            {
                EditorSceneManager.OpenScene(
                    PartyScenePath,
                    OpenSceneMode.Single);
                DefinitionDatabase database = AssetDatabase.LoadAssetAtPath<
                    DefinitionDatabase>(
                        "Assets/Data/Database/DefinitionDatabase.asset");
                database.Initialize();

                var repository = new FakeSaveRepository();
                ProfileSaveData profile = SaveTestDataBuilder.Valid();
                profile.Characters.Add(new CharacterSaveData
                {
                    CharacterId = "character_marea_bluefang",
                    Level = 30,
                    Awakening = 4
                });
                profile.Party.Presets[0].CharacterSlotIds[0] =
                    "character_marea_bluefang";
                repository.MainText = SaveTestDataBuilder.Json(profile);
                SaveService saveService =
                    SaveServiceTestFactory.Create(repository);
                Assert.That(
                    saveService.LoadOrCreate("ignored").CanUseProfile,
                    Is.True);

                PartyScreenController party =
                    Object.FindFirstObjectByType<PartyScreenController>(
                        FindObjectsInactive.Include);
                CharacterSelectController controller =
                    Object.FindFirstObjectByType<CharacterSelectController>(
                        FindObjectsInactive.Include);
                CharacterSelectView view =
                    Object.FindFirstObjectByType<CharacterSelectView>(
                        FindObjectsInactive.Include);
                ActivateCharacterSelectLifecycle(controller, view);
                party.Initialize(saveService);
                int writesBefore = repository.Count(
                    nameof(FakeSaveRepository.WriteTemp));

                party.OpenCharacterSelect(0, 0);
                view.FilterButtons.Single(item =>
                    item.Element == ElementType.Fire).Button.onClick.Invoke();
                Assert.That(view.DisplayedCellCount, Is.Zero);
                party.CurrentEditSession.SetPreviewCharacterId(
                    "temporary_preview");

                view.SecondaryActionButton.onClick.Invoke();

                Assert.That(party.IsCharacterSelectOpen, Is.True);
                Assert.That(
                    party.CurrentEditSession.PreviewCharacterId,
                    Is.EqualTo("character_marea_bluefang"));
                Assert.That(controller.SelectedElement,
                    Is.EqualTo(ElementType.Fire));
                Assert.That(view.DisplayedCellCount, Is.Zero);
                Assert.That(view.LevelText.text, Is.EqualTo("30"));
                Assert.That(view.AwakeningText.text, Is.EqualTo("4"));
                Assert.That(view.ConfirmButton.interactable, Is.True);
                Assert.That(repository.Count(
                    nameof(FakeSaveRepository.WriteTemp)),
                    Is.EqualTo(writesBefore));
            }
            finally
            {
                Restore(previousSetup);
            }
        }

        private void Restore(SceneSetup[] previousSetup)
        {
            DeactivateLifecycleComponents();
            if (previousSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
            else
            {
                EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);
            }
        }

        private void ActivateCharacterSelectLifecycle(
            CharacterSelectController controller,
            CharacterSelectView view)
        {
            ActivateCharacterSelectViewLifecycle(view);
            ActivateLifecycleComponent(controller);
        }

        private void ActivateCharacterSelectViewLifecycle(
            CharacterSelectView view)
        {
            ActivateLifecycleComponent(view.CharacterGrid);
            foreach (CharacterRosterCellView cell
                in view.CharacterGrid.PoolCells)
            {
                ActivateLifecycleComponent(cell);
            }

            foreach (ElementFilterButtonView filter in view.FilterButtons)
            {
                ActivateLifecycleComponent(filter);
            }

            ActivateLifecycleComponent(view);
        }

        private void ActivateLifecycleComponent(MonoBehaviour component)
        {
            if (activeLifecycleComponents.Contains(component))
            {
                return;
            }

            InvokeLifecycle(component, "OnEnable");
            activeLifecycleComponents.Add(component);
        }

        private void DeactivateLifecycleComponents()
        {
            for (int index = activeLifecycleComponents.Count - 1;
                index >= 0;
                index--)
            {
                MonoBehaviour component = activeLifecycleComponents[index];
                if (component != null)
                {
                    InvokeLifecycle(component, "OnDisable");
                }
            }

            activeLifecycleComponents.Clear();
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
