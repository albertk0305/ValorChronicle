using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Localization;
using ValorChronicle.Main.Presentation;

namespace ValorChronicle.Tests.PlayMode
{
    public sealed class MainBossSelectionPlayModeTests
    {
        [UnityTest]
        public IEnumerator BattleButton_ShowsPanelWithoutLoadingBattle()
        {
            ExpectGoldRefreshFailureIfNecessary();
            yield return SceneManager.LoadSceneAsync(
                "Main",
                LoadSceneMode.Single);
            MainScreenController[] controllers =
                Object.FindObjectsByType<MainScreenController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            Assert.That(controllers, Has.Length.EqualTo(1));
            MainScreenController controller = controllers[0];
            Assert.That(controller.BattlePanel.activeSelf, Is.False);
            Assert.That(controller.LevelSelect.activeSelf, Is.False);
            Assert.That(controller.UnvalidParty.activeSelf, Is.False);
            Assert.That(controller.SelectedBossId, Is.Null);
            Assert.That(controller.IsBattleEntryInProgress, Is.False);
            Assert.That(controller.InputCanvasGroup.interactable, Is.True);
            Assert.That(controller.BossScrollPanel.activeSelf, Is.True);

            if (GameBootstrapper.Instance?.SaveService?.HasCurrentProfile
                == true)
            {
                long expectedGold = GameBootstrapper.Instance.SaveService
                    .GetCurrentProfileSnapshot().Currencies.GachaCurrency;
                Assert.That(
                    controller.GoldText.text,
                    Is.EqualTo(MainScreenController.FormatGold(expectedGold)));
            }
            else
            {
                Assert.That(controller.GoldText.text, Is.EqualTo("-"));
            }

            if (GameBootstrapper.Instance?.DefinitionDatabase == null
                || GameBootstrapper.Instance.LocalizationService == null)
            {
                LogAssert.Expect(
                    LogType.Error,
                    new Regex("Initialized content services"));
            }

            controller.BattleButton.onClick.Invoke();
            yield return null;

            Assert.That(controller.BattlePanel.activeSelf, Is.True);
            Assert.That(controller.LevelSelect.activeSelf, Is.False);
            Assert.That(controller.UnvalidParty.activeSelf, Is.False);
            Assert.That(SceneManager.GetActiveScene().name,
                Is.EqualTo("Main"));

            Scene cleanupScene = SceneManager.CreateScene(
                "MainBossSelectionPlayModeCleanup");
            SceneManager.SetActiveScene(cleanupScene);
            yield return SceneManager.UnloadSceneAsync("Main");
        }

        [UnityTest]
        public IEnumerator TouchPanel_DragDoesNotSelectAndClickSelectsOnce()
        {
            var eventSystemObject = new GameObject(
                "EventSystem",
                typeof(EventSystem));
            var slotObject = new GameObject(
                "Slot",
                typeof(RectTransform),
                typeof(BossSlotView));
            Image face = CreateImage("Face", slotObject.transform);
            TMP_Text name = CreateText("Name", slotObject.transform);
            Image type = CreateImage("Type", slotObject.transform);
            TMP_Text score = CreateText("Score", slotObject.transform);
            Image grade = CreateImage("Grade", slotObject.transform);
            Image touch = CreateImage("TouchPanel", slotObject.transform);
            BossSlotView slot = slotObject.GetComponent<BossSlotView>();
            slot.Configure(face, name, type, score, grade, touch);
            int selectionCount = 0;
            string selectedId = null;
            slot.Bind(
                new BossSelectionItem(
                    "kragmor",
                    "Kragmor",
                    null,
                    null,
                    "0",
                    null),
                id =>
                {
                    selectionCount++;
                    selectedId = id;
                });

            var dragged = new PointerEventData(
                eventSystemObject.GetComponent<EventSystem>())
            {
                button = PointerEventData.InputButton.Left,
                dragging = true
            };
            ExecuteEvents.ExecuteHierarchy(
                touch.gameObject,
                dragged,
                ExecuteEvents.pointerClickHandler);
            Assert.That(selectionCount, Is.Zero);

            var clicked = new PointerEventData(
                eventSystemObject.GetComponent<EventSystem>())
            {
                button = PointerEventData.InputButton.Left,
                dragging = false
            };
            ExecuteEvents.ExecuteHierarchy(
                touch.gameObject,
                clicked,
                ExecuteEvents.pointerClickHandler);

            Assert.That(selectionCount, Is.EqualTo(1));
            Assert.That(selectedId, Is.EqualTo("kragmor"));

            Object.Destroy(slotObject);
            Object.Destroy(eventSystemObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LevelSelect_EmptyPartyPopupClosesAndReturnRestoresList()
        {
            ExpectGoldRefreshFailureIfNecessary();
            yield return SceneManager.LoadSceneAsync(
                "Main",
                LoadSceneMode.Single);
            MainScreenController controller =
                Object.FindFirstObjectByType<MainScreenController>(
                    FindObjectsInactive.Include);
            Assert.That(controller, Is.Not.Null);
            controller.BattlePanel.SetActive(true);
            controller.BossScrollPanel.SetActive(false);
            controller.LevelSelect.SetActive(true);
            controller.gameObject.SetActive(false);
            ExpectGoldRefreshFailureIfNecessary();
            controller.gameObject.SetActive(true);
            yield return null;

            Assert.That(controller.BattlePanel.activeSelf, Is.True);
            Assert.That(controller.LevelSelect.activeSelf, Is.True);
            Assert.That(controller.BossScrollPanel.activeSelf, Is.False);

            controller.UnvalidPartyView.ShowEmptyParty(
                CreateMainLocalization());

            yield return null;
            Assert.That(SceneManager.GetActiveScene().name,
                Is.EqualTo("Main"));
            Assert.That(controller.LevelSelect.activeSelf, Is.True);
            Assert.That(controller.BossScrollPanel.activeSelf, Is.False);
            Assert.That(controller.UnvalidParty.activeSelf, Is.True);
            Assert.That(controller.UnvalidPartyView.MessageText.text,
                Is.EqualTo(UnvalidPartyView.EmptyPartyMessage));
            Assert.That(controller.InputCanvasGroup.interactable, Is.True);

            controller.UnvalidPartyView.DismissButton.onClick.Invoke();
            yield return null;

            Assert.That(controller.UnvalidParty.activeSelf, Is.False);
            Assert.That(controller.LevelSelect.activeSelf, Is.True);

            controller.LevelSelectView.ReturnButton.onClick.Invoke();
            yield return null;

            Assert.That(controller.LevelSelect.activeSelf, Is.False);
            Assert.That(controller.BossScrollPanel.activeSelf, Is.True);
            Assert.That(controller.BattlePanel.activeSelf, Is.True);

            Scene cleanupScene = SceneManager.CreateScene(
                "LevelSelectPlayModeCleanup");
            SceneManager.SetActiveScene(cleanupScene);
            yield return SceneManager.UnloadSceneAsync("Main");
        }

        private static void ExpectGoldRefreshFailureIfNecessary()
        {
            GameBootstrapper bootstrapper = GameBootstrapper.Instance;
            if (bootstrapper == null)
            {
                LogAssert.Expect(
                    LogType.Error,
                    new Regex("GameBootstrapper is missing while refreshing Gold"));
                return;
            }

            if (bootstrapper.SaveService == null)
            {
                LogAssert.Expect(
                    LogType.Error,
                    new Regex("SaveService is unavailable while refreshing Gold"));
                return;
            }

            if (!bootstrapper.SaveService.HasCurrentProfile)
            {
                LogAssert.Expect(
                    LogType.Error,
                    new Regex("No current profile is available while refreshing Gold"));
            }
        }

        private static LocalizationService CreateMainLocalization()
        {
            LocalizationTable table = LocalizationTableParser.Parse(
                "main_play_mode_test",
                "key\ten\tko\n"
                    + $"{UnvalidPartyView.EmptyPartyMessageKey}\t"
                    + $"{UnvalidPartyView.EmptyPartyMessage}\t"
                    + UnvalidPartyView.EmptyPartyMessage);
            return new LocalizationService(new[] { table });
        }

        private static Image CreateImage(string name, Transform parent)
        {
            var value = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image));
            value.transform.SetParent(parent, false);
            return value.GetComponent<Image>();
        }

        private static TMP_Text CreateText(string name, Transform parent)
        {
            var value = new GameObject(
                name,
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            value.transform.SetParent(parent, false);
            return value.GetComponent<TMP_Text>();
        }
    }
}
