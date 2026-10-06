using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Core.Scene;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Presentation;
using ValorChronicle.Party.Roster;
using ValorChronicle.UI.Navigation;
using Object = UnityEngine.Object;

namespace ValorChronicle.Tests.EditMode.Characters.Presentation
{
    public sealed class CharacterSceneConnectionTests
    {
        private const string ScenePath = "Assets/Scenes/Character.unity";
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        [Test]
        public void CharacterSceneIsMappedAndEnabledExactlyOnceForBuild()
        {
            Assert.That(GameSceneNames.GetName(GameScene.Character),
                Is.EqualTo("Character"));
            EditorBuildSettingsScene[] matches = EditorBuildSettings.scenes
                .Where(scene => string.Equals(
                    scene.path,
                    ScenePath,
                    StringComparison.Ordinal))
                .ToArray();

            Assert.That(matches, Has.Length.EqualTo(1));
            Assert.That(matches[0].enabled, Is.True);
        }

        [Test]
        public void MainCharacterButtonUsesProductionCharacterNavigation()
        {
            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    MainScenePath,
                    OpenSceneMode.Single);
                Transform mainCanvas = scene.GetRootGameObjects()
                    .Single(root => root.name == "MainCanvas")
                    .transform;
                Transform characterButton = mainCanvas.Find(
                    "SafeAreaRoot/GamaeViewport/GameContent/"
                        + "CharacterButton");

                Assert.That(characterButton, Is.Not.Null);
                var characterRect = (RectTransform)characterButton;
                Assert.That(characterRect.anchoredPosition.x,
                    Is.EqualTo(-288f).Within(0.001f));
                Assert.That(characterRect.anchoredPosition.y,
                    Is.EqualTo(622f).Within(0.001f));
                Assert.That(characterButton.GetComponents<Button>(),
                    Has.Length.EqualTo(1));
                SceneNavigationButton[] navigation = characterButton
                    .GetComponents<SceneNavigationButton>();
                Assert.That(navigation, Has.Length.EqualTo(1));
                var navigationData = new SerializedObject(navigation[0]);
                Assert.That(navigationData.FindProperty("destination")
                    .enumValueIndex, Is.EqualTo((int)GameScene.Character));
                Assert.That(characterButton.GetComponentInChildren<TMP_Text>()
                    .text, Is.EqualTo("Character Test"));
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

        [Test]
        public void CharacterSceneHasConfiguredRosterInstallerAndMockPool()
        {
            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);
                CharacterRosterSceneInstaller[] installers =
                    Object.FindObjectsByType<CharacterRosterSceneInstaller>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

                Assert.That(installers, Has.Length.EqualTo(1));
                Assert.That(installers[0].IsConfigured, Is.True);
                Transform upgrade = installers[0].transform.parent.Find(
                    "CharacterUpgrade");
                Assert.That(upgrade, Is.Not.Null);
                Assert.That(upgrade.Find("CharacterName/NameText"), Is.Not.Null);
                Assert.That(upgrade.Find("CharacterFull"), Is.Not.Null);
                Assert.That(upgrade.Find("Stats/TypeIcon"), Is.Not.Null);
                Assert.That(upgrade.Find("Stats/LevelText"), Is.Not.Null);
                Assert.That(upgrade.Find("Stats/AwakenText"), Is.Not.Null);
                Assert.That(upgrade.Find("Stats/ATKText"), Is.Not.Null);
                Assert.That(upgrade.Find("Stats/HPText"), Is.Not.Null);
                Assert.That(upgrade.Find("LevelUp/LevelUpCostIcon"), Is.Not.Null);
                Assert.That(upgrade.Find("LevelUp/LevelUpCostText"), Is.Not.Null);
                Assert.That(upgrade.Find("LevelUp/LevelUpButton"), Is.Not.Null);
                Assert.That(upgrade.Find("ReturnButton"), Is.Not.Null);
                Assert.That(upgrade.Find("AwakeningButton"), Is.Not.Null);
                Assert.That(upgrade.Find("SkillsButton"), Is.Not.Null);
                Transform lookup = installers[0].transform.parent.Find(
                    "CharacterLookup");
                Assert.That(lookup, Is.Not.Null);
                Assert.That(lookup.Find("ReturnButton"), Is.Not.Null);
                Assert.That(lookup.Find("AwakeningButton"), Is.Not.Null);
                Assert.That(lookup.Find("SkillsButton"), Is.Not.Null);
                Assert.That(lookup.Find("Text/Text"), Is.Not.Null);
                Assert.That(lookup.Find("Text/Text")
                    .GetComponent<TMP_Text>().text, Is.Empty);
                for (int stage = 1; stage <= 6; stage++)
                {
                    Assert.That(lookup.Find(
                        $"AwakeningLookUp/Awakening{stage}"), Is.Not.Null);
                }

                foreach (string skillButton in new[]
                {
                    "3MatchButton",
                    "4MatchButton",
                    "5MatchButton",
                    "PassiveButton",
                    "ActiveButton"
                })
                {
                    Assert.That(lookup.Find(
                        $"SkillLookUp/{skillButton}"), Is.Not.Null);
                }
                Transform content = installers[0].transform.Find(
                    "CharacterSlots/CharacterSelectSlots");
                Assert.That(content, Is.Not.Null);
                Assert.That(content.childCount, Is.EqualTo(30));

                Transform selectReturn = installers[0].transform.Find(
                    "ReturnButton");
                Assert.That(selectReturn, Is.Not.Null);
                SceneNavigationButton[] navigation = selectReturn
                    .GetComponents<SceneNavigationButton>();
                Assert.That(navigation, Has.Length.EqualTo(1));
                var navigationData = new SerializedObject(navigation[0]);
                Assert.That(navigationData.FindProperty("destination")
                    .enumValueIndex, Is.EqualTo((int)GameScene.Main));

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Transform child in
                        root.GetComponentsInChildren<Transform>(true))
                    {
                        Assert.That(
                            GameObjectUtility
                                .GetMonoBehavioursWithMissingScriptCount(
                                    child.gameObject),
                            Is.Zero,
                            child.name);
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

        [Test]
        public void InstallerCanRunRepeatedlyWithoutDuplicateComponentsOrCellEvents()
        {
            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                CharacterRosterSceneInstaller installer =
                    Object.FindFirstObjectByType<CharacterRosterSceneInstaller>(
                        FindObjectsInactive.Include);
                Assert.That(installer, Is.Not.Null);

                InvokeAwake(installer);
                InvokeAwake(installer);

                Transform select = installer.transform;
                Transform parent = select.parent;
                Transform viewport = select.Find("CharacterSlots");
                Transform levelUpButton = parent.Find(
                    "CharacterUpgrade/LevelUp/LevelUpButton");
                Assert.That(select.GetComponents<CharacterRosterScreenView>(),
                    Has.Length.EqualTo(1));
                Assert.That(
                    select.GetComponents<CharacterRosterScreenController>(),
                    Has.Length.EqualTo(1));
                Assert.That(
                    parent.GetComponents<CharacterUpgradeScreenController>(),
                    Has.Length.EqualTo(1));
                Assert.That(
                    parent.GetComponents<CharacterLookupScreenController>(),
                    Has.Length.EqualTo(1));
                Assert.That(parent.GetComponents<CharacterScreenCoordinator>(),
                    Has.Length.EqualTo(1));
                Transform lookup = parent.Find("CharacterLookup");
                Assert.That(
                    lookup.GetComponents<CharacterLookupScreenView>(),
                    Has.Length.EqualTo(1));
                Assert.That(viewport.GetComponents<ScrollRect>(),
                    Has.Length.EqualTo(1));
                Assert.That(
                    viewport.GetComponents<VirtualizedCharacterGridView>(),
                    Has.Length.EqualTo(1));
                Assert.That(viewport.GetComponents<RectMask2D>(),
                    Has.Length.EqualTo(1));
                Assert.That(levelUpButton.GetComponents<
                    CharacterLevelUpPressHoldInput>(), Has.Length.EqualTo(1));

                VirtualizedCharacterGridView grid = viewport.GetComponent<
                    VirtualizedCharacterGridView>();
                ScrollRect scrollRect = viewport.GetComponent<ScrollRect>();
                Assert.That(scrollRect.movementType,
                    Is.EqualTo(ScrollRect.MovementType.Clamped));
                Assert.That(scrollRect.elasticity, Is.EqualTo(0.1f));
                Assert.That(scrollRect.inertia, Is.True);
                Assert.That(scrollRect.decelerationRate,
                    Is.EqualTo(0.135f));
                Assert.That(scrollRect.scrollSensitivity, Is.EqualTo(1f));
                Assert.That(scrollRect.horizontal, Is.False);
                Assert.That(scrollRect.vertical, Is.True);
                Transform content = viewport.Find("CharacterSelectSlots");
                Transform scrollPanel = viewport.Find("ScrollPanel");
                Assert.That(scrollPanel, Is.Not.Null);
                Assert.That(
                    content.GetSiblingIndex(),
                    Is.GreaterThan(scrollPanel.GetSiblingIndex()),
                    "Roster content must raycast above the scroll overlay.");
                Assert.That(
                    scrollPanel.GetComponent<Image>().raycastTarget,
                    Is.True,
                    "The behind-content overlay should retain empty-space drag input.");
                Assert.That(content.GetComponentsInChildren<
                    CharacterRosterCellView>(includeInactive: true),
                    Has.Length.EqualTo(grid.PoolSize));
                int requestCount = 0;
                string requestedCharacterId = string.Empty;
                grid.CharacterRequested += characterId =>
                {
                    requestCount++;
                    requestedCharacterId = characterId;
                };
                grid.SetItems(
                    new[]
                    {
                        new CharacterRosterEntry(
                            "character_listener_probe",
                            ElementType.Water,
                            1,
                            0,
                            1,
                            1)
                    },
                    string.Empty,
                    _ => null,
                    _ => null,
                    resetScroll: true);
                CharacterRosterCellView cell = grid.PoolCells.Single(
                    item => item.IsBound);
                Assert.That(cell.AlwaysShowBorder, Is.True);
                Assert.That(cell.SelectedBorder.activeSelf, Is.True);
                Assert.That(cell.Button.interactable, Is.True);
                Assert.That(cell.Button.targetGraphic, Is.Not.Null);
                Bounds cellBounds = RectTransformUtility
                    .CalculateRelativeRectTransformBounds(
                        viewport,
                        cell.transform);
                Rect viewportRect = ((RectTransform)viewport).rect;
                Assert.That(
                    cellBounds.max.y,
                    Is.LessThanOrEqualTo(viewportRect.yMax + 0.01f));
                Assert.That(
                    cellBounds.min.y,
                    Is.GreaterThanOrEqualTo(viewportRect.yMin - 0.01f));
                cell.Bind(
                    new CharacterRosterEntry(
                        "character_listener_probe",
                        ElementType.Water,
                        1,
                        0,
                        1,
                        1),
                    null,
                    null,
                    selected: false);
                InvokeLifecycle(cell, "OnEnable");

                cell.Button.onClick.Invoke();

                Assert.That(requestCount, Is.EqualTo(1));
                Assert.That(
                    requestedCharacterId,
                    Is.EqualTo("character_listener_probe"));

                CharacterLookupScreenView lookupView =
                    lookup.GetComponent<CharacterLookupScreenView>();
                int awakeningRequestCount = 0;
                int requestedStage = 0;
                lookupView.AwakeningStageRequested += stage =>
                {
                    awakeningRequestCount++;
                    requestedStage = stage;
                };
                lookupView.AwakeningButtons[4].onClick.Invoke();
                Assert.That(awakeningRequestCount, Is.EqualTo(1));
                Assert.That(requestedStage, Is.EqualTo(5));
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

        private static void InvokeAwake(CharacterRosterSceneInstaller installer)
        {
            InvokeLifecycle(installer, "Awake");
        }

        private static void InvokeLifecycle(
            MonoBehaviour target,
            string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, null);
        }
    }
}
