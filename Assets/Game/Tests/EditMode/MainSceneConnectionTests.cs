using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ValorChronicle.Battle.Results;
using ValorChronicle.Main.Presentation;
using ValorChronicle.UI.Audio;
using ValorChronicle.UI.Navigation;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class MainSceneConnectionTests
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        [Test]
        public void MainSceneHasConnectedBossSelectionAndNoMissingScripts()
        {
            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();

            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    MainScenePath,
                    OpenSceneMode.Single);
                Assert.That(scene.IsValid(), Is.True);
                Assert.That(scene.isLoaded, Is.True);

                MainScreenController[] controllers =
                    Object.FindObjectsByType<MainScreenController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                BossSelectionListView[] lists =
                    Object.FindObjectsByType<BossSelectionListView>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                LevelSelectView[] levelSelectViews =
                    Object.FindObjectsByType<LevelSelectView>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                UnvalidPartyView[] unvalidPartyViews =
                    Object.FindObjectsByType<UnvalidPartyView>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                BossSlotView[] slots =
                    Object.FindObjectsByType<BossSlotView>(
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
                EventSystem[] eventSystems =
                    Object.FindObjectsByType<EventSystem>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                GraphicRaycaster[] raycasters =
                    Object.FindObjectsByType<GraphicRaycaster>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

                Assert.That(controllers, Has.Length.EqualTo(1));
                Assert.That(lists, Has.Length.EqualTo(1));
                Assert.That(levelSelectViews, Has.Length.EqualTo(1));
                Assert.That(unvalidPartyViews, Has.Length.EqualTo(1));
                Assert.That(slots, Has.Length.EqualTo(3));
                Assert.That(scrollRects, Has.Length.EqualTo(1));
                Assert.That(masks, Has.Length.EqualTo(1));
                Assert.That(eventSystems, Has.Length.EqualTo(1));
                Assert.That(raycasters, Has.Length.EqualTo(1));

                MainScreenController controller = controllers[0];
                BossSelectionListView list = lists[0];
                LevelSelectView levelSelectView = levelSelectViews[0];
                UnvalidPartyView unvalidPartyView = unvalidPartyViews[0];
                ScrollRect scrollRect = scrollRects[0];
                Assert.That(controller.IsConfigured, Is.True);
                Assert.That(list.IsConfigured, Is.True);
                Assert.That(levelSelectView.IsConfigured, Is.True);
                Assert.That(unvalidPartyView.IsConfigured, Is.True);
                Assert.That(controller.LevelSelectView,
                    Is.SameAs(levelSelectView));
                Assert.That(controller.UnvalidPartyView,
                    Is.SameAs(unvalidPartyView));
                Assert.That(controller.BossScrollPanel,
                    Is.SameAs(scrollRect.gameObject));
                Assert.That(controller.InputCanvasGroup, Is.Not.Null);
                Assert.That(controller.InputCanvasGroup.gameObject.name,
                    Is.EqualTo("GameContent"));
                Assert.That(controller.InputCanvasGroup.alpha,
                    Is.EqualTo(1f));
                Assert.That(controller.InputCanvasGroup.interactable,
                    Is.True);
                Assert.That(controller.InputCanvasGroup.blocksRaycasts,
                    Is.True);
                Assert.That(controller.GoldText, Is.Not.Null);
                Assert.That(controller.GoldText.name, Is.EqualTo("GoldText"));
                Assert.That(controller.GoldText.transform.parent.name,
                    Is.EqualTo("Gold"));
                Assert.That(unvalidPartyView.gameObject,
                    Is.SameAs(controller.UnvalidParty));
                Assert.That(unvalidPartyView.gameObject.activeSelf,
                    Is.False);
                Assert.That(unvalidPartyView.DismissButton.name,
                    Is.EqualTo("BackgroundPanel"));
                Assert.That(
                    unvalidPartyView.DismissButton.GetComponent<
                        ButtonPressSfx>(),
                    Is.Null);
                ButtonPressSfx[] buttonPressSfx =
                    Object.FindObjectsByType<ButtonPressSfx>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                Assert.That(buttonPressSfx, Has.Length.EqualTo(12));
                Assert.That(
                    buttonPressSfx,
                    Has.All.Matches<ButtonPressSfx>(component =>
                        component.GetComponent<Button>() != null));
                Assert.That(
                    unvalidPartyView.DismissButton.targetGraphic.raycastTarget,
                    Is.True);
                Assert.That(unvalidPartyView.transform.parent,
                    Is.SameAs(controller.InputCanvasGroup.transform));
                Assert.That(unvalidPartyView.transform.GetSiblingIndex(),
                    Is.EqualTo(
                        unvalidPartyView.transform.parent.childCount - 1));
                RectTransform modalRoot = unvalidPartyView
                    .GetComponent<RectTransform>();
                Assert.That(modalRoot.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(modalRoot.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(modalRoot.sizeDelta, Is.EqualTo(Vector2.zero));
                RectTransform popupBlocker = unvalidPartyView
                    .DismissButton.GetComponent<RectTransform>();
                Assert.That(popupBlocker.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(popupBlocker.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(popupBlocker.sizeDelta, Is.EqualTo(Vector2.zero));
                RectTransform gameContent = controller.InputCanvasGroup
                    .GetComponent<RectTransform>();
                Assert.That(popupBlocker.rect.size,
                    Is.EqualTo(gameContent.rect.size));
                Assert.That(slots, Has.All.Matches<BossSlotView>(
                    slot => slot.IsConfigured));
                Assert.That(controller.BattleButton.name,
                    Is.EqualTo("BattleButton"));
                Assert.That(controller.BattlePanel.activeSelf, Is.False);
                Assert.That(controller.LevelSelect.activeSelf, Is.False);
                Assert.That(
                    controller.BattleButton.GetComponent<
                        SceneNavigationButton>(),
                    Is.Null);
                Assert.That(
                    GameObject.Find("SettingButton")
                        .GetComponent<SceneNavigationButton>(),
                    Is.Null);

                Assert.That(scrollRect.horizontal, Is.False);
                Assert.That(scrollRect.vertical, Is.True);
                Assert.That(scrollRect.movementType,
                    Is.EqualTo(ScrollRect.MovementType.Clamped));
                Assert.That(scrollRect.inertia, Is.True);
                Assert.That(scrollRect.decelerationRate,
                    Is.EqualTo(0.135f));
                Assert.That(scrollRect.viewport,
                    Is.SameAs(scrollRect.GetComponent<RectTransform>()));
                Assert.That(scrollRect.content, Is.SameAs(list.Content));
                Assert.That(scrollRect.content.parent,
                    Is.SameAs(scrollRect.viewport));
                Assert.That(
                    scrollRect.viewport.GetComponent<RectMask2D>(),
                    Is.SameAs(masks[0]));
                Assert.That(
                    scrollRect.viewport.GetComponent<Image>().raycastTarget,
                    Is.True);

                var serializedList = new SerializedObject(list);
                Assert.That(
                    serializedList.FindProperty("initialSlots").arraySize,
                    Is.EqualTo(3));
                Assert.That(list.ElementIconSet, Is.Not.Null);
                Assert.That(list.ResolveGradeSprite("grade_c"), Is.Not.Null);
                Assert.That(list.ResolveGradeSprite("grade_sss"), Is.Not.Null);

                AssertDifficultyMapping(
                    levelSelectView,
                    "BeginnerButton",
                    BattleDifficultyIds.Intro);
                AssertDifficultyMapping(
                    levelSelectView,
                    "StandardButton",
                    BattleDifficultyIds.Normal);
                AssertDifficultyMapping(
                    levelSelectView,
                    "AdvancedButton",
                    BattleDifficultyIds.Advanced);
                AssertDifficultyMapping(
                    levelSelectView,
                    "ExpertButton",
                    BattleDifficultyIds.Hard);
                AssertDifficultyMapping(
                    levelSelectView,
                    "MasterButton",
                    BattleDifficultyIds.Challenge);
                Assert.That(
                    levelSelectView.BossNameText.raycastTarget,
                    Is.False);
                Assert.That(
                    levelSelectView.BestScoreText.raycastTarget,
                    Is.False);
                Transform summarySlot = controller.LevelSelect.transform
                    .Find("Slot");
                Assert.That(summarySlot, Is.Not.Null);
                foreach (Image image in
                    summarySlot.GetComponentsInChildren<Image>(true))
                {
                    Assert.That(
                        image.raycastTarget,
                        Is.False,
                        image.name);
                }

                foreach (Image image in controller.LevelSelect
                    .GetComponentsInChildren<Image>(true))
                {
                    if (image.name == "BestGradeIcon")
                    {
                        Assert.That(image.raycastTarget, Is.False);
                    }
                }

                foreach (BossSlotView slot in slots)
                {
                    Assert.That(slot.BossNameText.raycastTarget, Is.False);
                    Assert.That(slot.BestScoreText.raycastTarget, Is.False);
                }

                Image mainBackground = Object.FindObjectsByType<Image>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                    .Single(image => image.name == "MainBackground");
                Assert.That(mainBackground.raycastTarget, Is.False);
                Image[] bestScorePanels = Object.FindObjectsByType<Image>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                    .Where(image => image.name == "BestScorePanel")
                    .ToArray();
                Assert.That(bestScorePanels, Has.Length.EqualTo(4));
                Assert.That(bestScorePanels,
                    Has.All.Matches<Image>(image => !image.raycastTarget));

                var loadingStateHandler = typeof(MainScreenController)
                    .GetMethod(
                        "HandleSceneLoadingStateChanged",
                        System.Reflection.BindingFlags.Instance
                            | System.Reflection.BindingFlags.NonPublic);
                Assert.That(loadingStateHandler, Is.Not.Null);
                loadingStateHandler.Invoke(controller, new object[] { true });
                Assert.That(controller.InputCanvasGroup.interactable,
                    Is.False);
                Assert.That(controller.InputCanvasGroup.blocksRaycasts,
                    Is.True);
                loadingStateHandler.Invoke(controller, new object[] { false });
                Assert.That(controller.InputCanvasGroup.interactable,
                    Is.True);

                string[] retainedNavigationButtons =
                {
                    "PartyButton",
                    "CharacterButton",
                    "GachaButton",
                    "ExitButton"
                };
                foreach (string buttonName in retainedNavigationButtons)
                {
                    GameObject button = GameObject.Find(buttonName);
                    Assert.That(button, Is.Not.Null, buttonName);
                    Assert.That(
                        button.GetComponent<SceneNavigationButton>(),
                        Is.Not.Null,
                        buttonName);
                }

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
                            $"Missing Script: {child.name}");
                    }
                }
            }
            finally
            {
                if (previousSetup != null && previousSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(
                        previousSetup);
                }
                else
                {
                    EditorSceneManager.NewScene(
                        NewSceneSetup.EmptyScene,
                        NewSceneMode.Single);
                }
            }
        }

        private static void AssertDifficultyMapping(
            LevelSelectView levelSelectView,
            string buttonName,
            string expectedDifficultyId)
        {
            Button button = null;
            foreach (Button candidate in levelSelectView
                .GetComponentsInChildren<Button>(true))
            {
                if (candidate.name == buttonName)
                {
                    button = candidate;
                    break;
                }
            }

            Assert.That(button, Is.Not.Null, buttonName);
            Assert.That(
                levelSelectView.TryGetDifficultyId(
                    button,
                    out string actualDifficultyId),
                Is.True,
                buttonName);
            Assert.That(actualDifficultyId, Is.EqualTo(expectedDifficultyId));
        }
    }
}
