using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ValorChronicle.Core.Scene;
using ValorChronicle.Party.Presentation;
using ValorChronicle.UI.Navigation;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class PartySceneConnectionTests
    {
        private const string PartyScenePath = "Assets/Scenes/Party.unity";

        [Test]
        public void PartySceneHasConnectedFiveByFiveViewsAndNoMissingScripts()
        {
            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();
            Scene scene = default;

            try
            {
                scene = EditorSceneManager.OpenScene(
                    PartyScenePath,
                    OpenSceneMode.Single);
                Assert.That(scene.IsValid(), Is.True);
                Assert.That(scene.isLoaded, Is.True);

                PartyScreenController[] controllers =
                    Object.FindObjectsByType<PartyScreenController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                PartyPresetView[] presets =
                    Object.FindObjectsByType<PartyPresetView>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                PartySlotView[] slots =
                    Object.FindObjectsByType<PartySlotView>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                CharacterSelectController[] characterSelectControllers =
                    Object.FindObjectsByType<CharacterSelectController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                CharacterSelectView[] characterSelectViews =
                    Object.FindObjectsByType<CharacterSelectView>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                CharacterRosterCellView[] rosterCells =
                    Object.FindObjectsByType<CharacterRosterCellView>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                ElementFilterButtonView[] elementFilters =
                    Object.FindObjectsByType<ElementFilterButtonView>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                VirtualizedCharacterGridView[] virtualGrids =
                    Object.FindObjectsByType<VirtualizedCharacterGridView>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                ScrollRect[] scrollRects =
                    Object.FindObjectsByType<ScrollRect>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                RectMask2D[] masks =
                    Object.FindObjectsByType<RectMask2D>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

                Assert.That(controllers, Has.Length.EqualTo(1));
                Assert.That(presets, Has.Length.EqualTo(5));
                Assert.That(slots, Has.Length.EqualTo(25));
                Assert.That(characterSelectControllers,
                    Has.Length.EqualTo(1));
                Assert.That(characterSelectViews, Has.Length.EqualTo(1));
                Assert.That(rosterCells, Has.Length.EqualTo(31));
                Assert.That(elementFilters, Has.Length.EqualTo(5));
                Assert.That(virtualGrids, Has.Length.EqualTo(1));
                Assert.That(scrollRects, Has.Length.EqualTo(1));
                Assert.That(masks, Has.Length.EqualTo(1));
                Assert.That(controllers[0].IsConfigured, Is.True);
                Assert.That(characterSelectControllers[0].IsConfigured,
                    Is.True);
                Assert.That(characterSelectViews[0].IsConfigured, Is.True);
                Assert.That(virtualGrids[0].IsConfigured, Is.True);
                Assert.That(
                    characterSelectViews[0].CharacterGrid,
                    Is.SameAs(virtualGrids[0]));
                Assert.That(scrollRects[0].horizontal, Is.False);
                Assert.That(scrollRects[0].vertical, Is.True);
                Assert.That(scrollRects[0].viewport, Is.Not.Null);
                Assert.That(scrollRects[0].content, Is.Not.Null);
                Assert.That(
                    scrollRects[0].viewport.GetComponent<RectMask2D>(),
                    Is.SameAs(masks[0]));
                var gridData = new SerializedObject(virtualGrids[0]);
                Assert.That(
                    gridData.FindProperty("scrollRect").objectReferenceValue,
                    Is.SameAs(scrollRects[0]));
                Assert.That(
                    gridData.FindProperty("viewport").objectReferenceValue,
                    Is.SameAs(scrollRects[0].viewport));
                Assert.That(
                    gridData.FindProperty("content").objectReferenceValue,
                    Is.SameAs(scrollRects[0].content));
                Assert.That(
                    gridData.FindProperty("initialCells").arraySize,
                    Is.EqualTo(31));
                Assert.That(
                    characterSelectViews[0].ConfirmButton.name,
                    Is.EqualTo("SelectButton"));
                Assert.That(
                    characterSelectViews[0].SecondaryActionButton.name,
                    Is.EqualTo("Empty Button"));
                Assert.That(
                    characterSelectViews[0].ConfirmButton.transform.parent.name,
                    Is.EqualTo("ChosenCharacter"));
                Assert.That(
                    characterSelectViews[0]
                        .SecondaryActionButton.transform.parent.name,
                    Is.EqualTo("ChosenCharacter"));
                Assert.That(presets, Has.All.Matches<PartyPresetView>(
                    view => view.IsConfigured));
                Assert.That(slots, Has.All.Matches<PartySlotView>(
                    view => view.IsConfigured));
                Assert.That(rosterCells,
                    Has.All.Matches<CharacterRosterCellView>(
                        view => view.IsConfigured));
                Assert.That(elementFilters,
                    Has.All.Matches<ElementFilterButtonView>(
                        view => view.IsConfigured));

                var controllerData = new SerializedObject(controllers[0]);
                SerializedProperty presetViews =
                    controllerData.FindProperty("presetViews");
                Assert.That(presetViews.arraySize, Is.EqualTo(5));
                Assert.That(
                    controllerData.FindProperty("partyInputGroup")
                        .objectReferenceValue,
                    Is.Not.Null);
                GameObject characterSelect = (GameObject)controllerData
                    .FindProperty("characterSelect").objectReferenceValue;
                Button exitButton = (Button)controllerData
                    .FindProperty("exitButton").objectReferenceValue;
                Assert.That(characterSelect, Is.Not.Null);
                Assert.That(characterSelect.activeSelf, Is.False);
                Assert.That(exitButton, Is.Not.Null);

                for (int presetIndex = 0;
                    presetIndex < presetViews.arraySize;
                    presetIndex++)
                {
                    var preset = (PartyPresetView)presetViews
                        .GetArrayElementAtIndex(presetIndex)
                        .objectReferenceValue;
                    Assert.That(preset, Is.Not.Null);
                    var presetData = new SerializedObject(preset);
                    Assert.That(
                        presetData.FindProperty("slotViews").arraySize,
                        Is.EqualTo(5));
                }

                SceneNavigationButton navigation =
                    exitButton.GetComponent<SceneNavigationButton>();
                Assert.That(navigation, Is.Not.Null);
                var navigationData = new SerializedObject(navigation);
                Assert.That(
                    navigationData.FindProperty("destination").enumValueIndex,
                    Is.EqualTo((int)GameScene.Main));

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Transform transform in
                        root.GetComponentsInChildren<Transform>(true))
                    {
                        Assert.That(
                            GameObjectUtility
                                .GetMonoBehavioursWithMissingScriptCount(
                                    transform.gameObject),
                            Is.Zero,
                            $"Missing script on {GetPath(transform)}.");
                    }
                }
            }
            finally
            {
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
        }

        private static string GetPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }
    }
}
