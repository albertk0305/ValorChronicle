using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Core.Pause;
using ValorChronicle.Core.Scene;
using ValorChronicle.Localization;
using ValorChronicle.UI.Settings;

namespace ValorChronicle.Tests.EditMode.Battle.Flow.Presentation
{
    public sealed class BattlePausePanelBindingTests
    {
        private GameObject root;
        private BattleBoardController boardController;
        private BattleFlowController flowController;
        private SettingsPanelHost settingsPanelHost;
        private BattlePausePanelBinding binding;
        private GamePauseService pauseService;
        private LocalizationService localizationService;
        private LocalizedTMPText pauseText;
        private GameObject settingsPrefab;
        private float timeScale;

        [UnitySetUp]
        public IEnumerator EnterRuntimeMode()
        {
            yield return new EnterPlayMode();
        }

        [UnityTearDown]
        public IEnumerator ExitRuntimeMode()
        {
            yield return new ExitPlayMode();
        }

        [SetUp]
        public void SetUp()
        {
            timeScale = 1.25f;
            pauseService = new GamePauseService(
                () => timeScale,
                value => timeScale = value);

            root = new GameObject("BattlePausePanelBindingTests");
            root.SetActive(false);
            boardController = root.AddComponent<BattleBoardController>();
            boardController.enabled = false;
            flowController = root.AddComponent<BattleFlowController>();
            var inputBlocker = new GameObject("NotUsersTurnPanel");
            inputBlocker.transform.SetParent(root.transform, false);
            SetField(flowController, "boardController", boardController);
            SetField(flowController, "notUsersTurnPanel", inputBlocker);

            Button settingButton = CreateButton("SettingButton", root.transform);
            settingsPrefab = new GameObject("SettingsPanelPrefab");
            settingsPrefab.SetActive(false);
            settingsPanelHost = root.AddComponent<SettingsPanelHost>();
            SetField(settingsPanelHost, "settingButton", settingButton);
            SetField(settingsPanelHost, "settingsPanelPrefab", settingsPrefab);
            SetField(settingsPanelHost, "panelParent", root.transform);
            SetField(settingsPanelHost, "pauseWhileOpen", true);

            Button pauseButton = CreateButton("PauseButton", root.transform);
            var pausePanel = new GameObject("PausePanel", typeof(RectTransform));
            pausePanel.transform.SetParent(root.transform, false);
            var pauseTextObject = new GameObject(
                "PauseText",
                typeof(RectTransform));
            pauseTextObject.transform.SetParent(pausePanel.transform, false);
            TMP_Text pauseLabel = pauseTextObject
                .AddComponent<TextMeshProUGUI>();
            pauseText = pauseTextObject.AddComponent<LocalizedTMPText>();
            SetField(pauseText, "targetText", pauseLabel);
            SetField(
                pauseText,
                "localizationKey",
                "ui.battle.pause.paused");
            localizationService = new LocalizationService(new[]
            {
                LocalizationTableParser.Parse(
                    "battle-pause-panel-test.tsv",
                    "key\ten\tko\n"
                        + "ui.battle.pause.paused\tGame Paused\t일시정지\n"
                        + "ui.battle.pause.give_up_confirm\t"
                        + "Are you sure you want to give up?\t"
                        + "정말 포기하시겠습니까?")
            });
            pauseText.Initialize(localizationService);
            Button resumeButton = CreateButton("ReturnButton", pausePanel.transform);
            Button giveUpButton = CreateButton("GiveUpButton", pausePanel.transform);
            var confirmation = new GameObject(
                "GiveUpConfirm",
                typeof(RectTransform));
            confirmation.transform.SetParent(pausePanel.transform, false);
            Button yesButton = CreateButton("YesButton", confirmation.transform);
            Button noButton = CreateButton("NoButton", confirmation.transform);
            confirmation.SetActive(false);
            pausePanel.SetActive(false);

            binding = root.AddComponent<BattlePausePanelBinding>();
            SetField(binding, "pauseButton", pauseButton);
            SetField(binding, "pausePanel", pausePanel);
            SetField(binding, "pauseText", pauseText);
            SetField(binding, "resumeButton", resumeButton);
            SetField(binding, "giveUpButton", giveUpButton);
            SetField(binding, "giveUpConfirmation", confirmation);
            SetField(binding, "confirmGiveUpButton", yesButton);
            SetField(binding, "cancelGiveUpButton", noButton);
            SetField(binding, "battleFlowController", flowController);
            SetField(binding, "settingsPanelHost", settingsPanelHost);
            binding.Initialize(pauseService, new SceneService());

            flowController.InitializePauseService(pauseService);
            SetField(boardController, "initialBoardReadyPublished", true);
            flowController.Initialize(new BattleFlowSetup(25, new[] { 3 }));
            root.SetActive(true);

            Assert.That(flowController.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(binding.IsConfigured, Is.True);
            Assert.That(binding.PauseButton.interactable, Is.True);
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
                UnityEngine.Object.DestroyImmediate(root);
            if (settingsPrefab != null)
                UnityEngine.Object.DestroyImmediate(settingsPrefab);
            pauseService?.Dispose();
        }

        [Test]
        public void PauseAndResume_PreserveFlowAndReleaseOnlyOwnedHandle()
        {
            IDisposable externalPause = pauseService.RequestPause();
            BattlePhase phase = flowController.Context.Phase;
            int turn = flowController.Context.CurrentTurn;
            int cooldown = flowController.Context.ActiveAbilities[0]
                .RemainingCooldown;

            // An existing modal pause prevents a second modal from opening.
            binding.PauseButton.onClick.Invoke();
            Assert.That(binding.IsOpen, Is.False);
            Assert.That(pauseService.ActiveRequestCount, Is.EqualTo(1));
            externalPause.Dispose();

            binding.PauseButton.onClick.Invoke();
            binding.PauseButton.onClick.Invoke();

            Assert.That(binding.IsOpen, Is.True);
            Assert.That(pauseService.ActiveRequestCount, Is.EqualTo(1));
            Assert.That(pauseService.IsPaused, Is.True);
            Assert.That(timeScale, Is.Zero);
            Assert.That(binding.ResumeButton.gameObject.activeSelf, Is.True);
            Assert.That(binding.GiveUpButton.gameObject.activeSelf, Is.True);
            Assert.That(binding.IsGiveUpConfirmationOpen, Is.False);
            Assert.That(pauseText.TargetText.text, Is.EqualTo("Game Paused"));
            Assert.That(boardController.IsExternalInputEnabled, Is.False);
            Assert.That(flowController.CanUseActive(0), Is.False);

            IDisposable otherOwner = pauseService.RequestPause();
            binding.ResumeButton.onClick.Invoke();

            Assert.That(binding.IsOpen, Is.False);
            Assert.That(pauseService.ActiveRequestCount, Is.EqualTo(1));
            Assert.That(pauseService.IsPaused, Is.True);
            Assert.That(timeScale, Is.Zero);
            Assert.That(flowController.Context.Phase, Is.EqualTo(phase));
            Assert.That(flowController.Context.CurrentTurn, Is.EqualTo(turn));
            Assert.That(
                flowController.Context.ActiveAbilities[0].RemainingCooldown,
                Is.EqualTo(cooldown));

            otherOwner.Dispose();
            Assert.That(pauseService.IsPaused, Is.False);
            Assert.That(timeScale, Is.EqualTo(1.25f));
            Assert.That(boardController.IsExternalInputEnabled, Is.True);
        }

        [Test]
        public void ReopenAndDisable_DoNotDuplicateOrLeakPauseRequests()
        {
            for (int iteration = 0; iteration < 3; iteration++)
            {
                binding.PauseButton.onClick.Invoke();
                Assert.That(pauseService.ActiveRequestCount, Is.EqualTo(1));
                binding.ResumeButton.onClick.Invoke();
                Assert.That(pauseService.ActiveRequestCount, Is.Zero);
            }

            IDisposable externalPause = pauseService.RequestPause();
            externalPause.Dispose();
            binding.PauseButton.onClick.Invoke();
            IDisposable otherOwner = pauseService.RequestPause();
            Assert.That(pauseService.ActiveRequestCount, Is.EqualTo(2));

            binding.enabled = false;

            Assert.That(binding.IsOpen, Is.False);
            Assert.That(pauseService.ActiveRequestCount, Is.EqualTo(1));
            otherOwner.Dispose();
            Assert.That(pauseService.ActiveRequestCount, Is.Zero);
            Assert.That(timeScale, Is.EqualTo(1.25f));
        }

        [Test]
        public void ReleaseAll_ClosesPanelAndStaleHandleRemainsSafe()
        {
            binding.PauseButton.onClick.Invoke();
            Assert.That(binding.IsOpen, Is.True);

            pauseService.ReleaseAll();

            Assert.That(binding.IsOpen, Is.False);
            Assert.That(pauseService.ActiveRequestCount, Is.Zero);
            Assert.That(timeScale, Is.EqualTo(1.25f));
            binding.ResumeButton.onClick.Invoke();
            Assert.That(pauseService.ActiveRequestCount, Is.Zero);
        }

        [Test]
        public void GiveUp_AbortsOnceBeforeRequestingMainAndShowsNoResult()
        {
            int navigationCount = 0;
            int resultCount = 0;
            BattleResultKind observedResult = BattleResultKind.None;
            flowController.BattleResultReached += result =>
            {
                resultCount++;
                observedResult = result;
            };
            SetField(
                binding,
                "loadMainSceneAsync",
                (Func<Task<bool>>)(() =>
                {
                    navigationCount++;
                    Assert.That(flowController.Context.Result,
                        Is.EqualTo(BattleResultKind.Aborted));
                    Assert.That(pauseService.ActiveRequestCount, Is.Zero);
                    return Task.FromResult(true);
                }));

            binding.PauseButton.onClick.Invoke();
            binding.GiveUpButton.onClick.Invoke();
            Assert.That(binding.IsGiveUpConfirmationOpen, Is.True);
            Assert.That(binding.ResumeButton.gameObject.activeSelf, Is.False);
            Assert.That(binding.GiveUpButton.gameObject.activeSelf, Is.False);
            Assert.That(binding.ResumeButton.interactable, Is.False);
            Assert.That(binding.GiveUpButton.interactable, Is.False);
            Assert.That(binding.ConfirmGiveUpButton.interactable, Is.True);
            Assert.That(binding.CancelGiveUpButton.interactable, Is.True);

            binding.ConfirmGiveUpButton.onClick.Invoke();
            binding.ConfirmGiveUpButton.onClick.Invoke();

            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.Aborted));
            Assert.That(observedResult, Is.EqualTo(BattleResultKind.Aborted));
            Assert.That(resultCount, Is.EqualTo(1));
            Assert.That(navigationCount, Is.EqualTo(1));
            Assert.That(binding.IsOpen, Is.False);
            Assert.That(pauseService.ActiveRequestCount, Is.Zero);
            Assert.That(timeScale, Is.EqualTo(1.25f));
        }

        [Test]
        public void CancelGiveUp_ReturnsToPausedPanelWithoutChangingBattle()
        {
            binding.PauseButton.onClick.Invoke();
            Assert.That(pauseText.TargetText.text, Is.EqualTo("Game Paused"));
            binding.GiveUpButton.onClick.Invoke();
            Assert.That(binding.ResumeButton.gameObject.activeSelf, Is.False);
            Assert.That(binding.GiveUpButton.gameObject.activeSelf, Is.False);
            Assert.That(pauseText.TargetText.text,
                Is.EqualTo("Are you sure you want to give up?"));
            Assert.That(binding.CancelGiveUpButton.interactable, Is.True);

            localizationService.SetLocale("ko");
            Assert.That(pauseText.TargetText.text,
                Is.EqualTo("정말 포기하시겠습니까?"));
            binding.CancelGiveUpButton.onClick.Invoke();

            Assert.That(binding.IsOpen, Is.True);
            Assert.That(binding.IsGiveUpConfirmationOpen, Is.False);
            Assert.That(binding.ResumeButton.gameObject.activeSelf, Is.True);
            Assert.That(binding.GiveUpButton.gameObject.activeSelf, Is.True);
            Assert.That(pauseText.TargetText.text, Is.EqualTo("일시정지"));
            Assert.That(flowController.Context.Result,
                Is.EqualTo(BattleResultKind.None));
            Assert.That(pauseService.ActiveRequestCount, Is.EqualTo(1));
            Assert.That(binding.ResumeButton.interactable, Is.True);
            Assert.That(binding.GiveUpButton.interactable, Is.True);

            binding.GiveUpButton.onClick.Invoke();
            Assert.That(pauseText.TargetText.text,
                Is.EqualTo("정말 포기하시겠습니까?"));
            localizationService.SetLocale("en");
            Assert.That(pauseText.TargetText.text,
                Is.EqualTo("Are you sure you want to give up?"));
            binding.CancelGiveUpButton.onClick.Invoke();
            Assert.That(pauseText.TargetText.text, Is.EqualTo("Game Paused"));
            Assert.That(pauseService.ActiveRequestCount, Is.EqualTo(1));
        }

        [Test]
        public void ResultReached_DisablesPauseAndReleasesTimeScale()
        {
            binding.PauseButton.onClick.Invoke();

            Assert.That(flowController.NotifyPartyDefeated(), Is.True);
            binding.PauseButton.onClick.Invoke();

            Assert.That(binding.IsOpen, Is.False);
            Assert.That(binding.PauseButton.interactable, Is.False);
            Assert.That(pauseService.ActiveRequestCount, Is.Zero);
            Assert.That(timeScale, Is.EqualTo(1.25f));
        }

        private static Button CreateButton(string name, Transform parent)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<Button>();
        }

        private static void SetField<T>(object target, string name, T value)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }
    }
}
