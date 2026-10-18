using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Logging;
using ValorChronicle.Core.Scene;
using ValorChronicle.Localization;

namespace ValorChronicle.UI
{
    public sealed class InitScreenController : MonoBehaviour
    {
        public const string ReadyTextKey = "ui.init.press_to_start";
        public const string FailureTextKey =
            "ui.init.initialization_failed";
        public const string ReadyText = "Press to Start";
        public const string FailureText =
            "Game initialization failed. Please restart the game.";

        [SerializeField]
        private Button startButton = null;

        [SerializeField]
        private TMP_Text initText = null;

        private GameBootstrapper subscribedBootstrapper;
        private LocalizationService subscribedLocalizationService;
        private BootstrapInitializationResult presentedResult;
        private bool isTransitioning;
        private PromptState promptState;

        private enum PromptState
        {
            Initializing,
            Ready,
            Failure
        }

        public Button StartButton => startButton;
        public TMP_Text InitText => initText;
        public bool IsTransitioning => isTransitioning;
        public bool IsConfigured => startButton != null && initText != null;

        private void Awake()
        {
            ShowInitializing();
        }

        private void OnEnable()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(HandleStartClick);
            }

            TryBindBootstrapper(logIfMissing: false);
        }

        private void Start()
        {
            TryBindBootstrapper(logIfMissing: true);
        }

        private void OnDisable()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(HandleStartClick);
            }

            UnsubscribeBootstrapper();
            UnsubscribeLocalization();
        }

        private void TryBindBootstrapper(bool logIfMissing)
        {
            GameBootstrapper bootstrapper = GameBootstrapper.Instance;
            if (bootstrapper == null)
            {
                if (logIfMissing)
                {
                    GameLogger.Error(
                        "[InitScreenController] GameBootstrapper is missing.",
                        this);
                    ShowFailure();
                }

                return;
            }

            if (!ReferenceEquals(subscribedBootstrapper, bootstrapper))
            {
                UnsubscribeBootstrapper();
                subscribedBootstrapper = bootstrapper;
                subscribedBootstrapper.InitializationCompleted +=
                    HandleInitializationCompleted;
            }

            BindLocalization(bootstrapper.LocalizationService);

            PresentInitializationResult(
                subscribedBootstrapper.InitializationResult);
        }

        private void UnsubscribeBootstrapper()
        {
            if (subscribedBootstrapper == null)
            {
                return;
            }

            subscribedBootstrapper.InitializationCompleted -=
                HandleInitializationCompleted;
            subscribedBootstrapper = null;
            presentedResult = null;
        }

        private void HandleInitializationCompleted(
            BootstrapInitializationResult result)
        {
            BindLocalization(subscribedBootstrapper?.LocalizationService);
            PresentInitializationResult(result);
        }

        private void BindLocalization(LocalizationService localization)
        {
            if (ReferenceEquals(
                    subscribedLocalizationService,
                    localization))
            {
                return;
            }

            UnsubscribeLocalization();
            subscribedLocalizationService = localization;
            if (subscribedLocalizationService == null)
            {
                return;
            }

            subscribedLocalizationService.LocaleChanged +=
                HandleLocaleChanged;
            RefreshLocalizedPrompt();
        }

        private void UnsubscribeLocalization()
        {
            if (subscribedLocalizationService == null)
            {
                return;
            }

            subscribedLocalizationService.LocaleChanged -=
                HandleLocaleChanged;
            subscribedLocalizationService = null;
        }

        private void HandleLocaleChanged(string _)
        {
            RefreshLocalizedPrompt();
        }

        private void RefreshLocalizedPrompt()
        {
            switch (promptState)
            {
                case PromptState.Ready:
                    ShowReady();
                    break;
                case PromptState.Failure:
                    ShowFailure();
                    break;
            }
        }

        private void PresentInitializationResult(
            BootstrapInitializationResult result)
        {
            if (result == null)
            {
                ShowInitializing();
                return;
            }

            if (ReferenceEquals(presentedResult, result))
            {
                return;
            }

            presentedResult = result;

            if (result.IsSuccess)
            {
                ShowReady();
                return;
            }

            GameLogger.Error(
                "[InitScreenController] Game initialization failed. "
                    + $"Status={result.Status}. {result.Message}",
                this);
            ShowFailure();
        }

        private async void HandleStartClick()
        {
            if (isTransitioning
                || subscribedBootstrapper?.InitializationResult == null
                || !subscribedBootstrapper.InitializationResult.IsSuccess)
            {
                return;
            }

            SceneService sceneService = subscribedBootstrapper.SceneService;
            if (sceneService == null)
            {
                GameLogger.Error(
                    "[InitScreenController] SceneService is not initialized.",
                    this);
                return;
            }

            isTransitioning = true;
            if (startButton != null)
            {
                startButton.interactable = false;
            }

            bool loadSucceeded = false;
            try
            {
                loadSucceeded = await sceneService.LoadAsync(GameScene.Main);
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[InitScreenController] Main scene loading failed.",
                    this);
            }
            finally
            {
                if (!loadSucceeded && this != null)
                {
                    isTransitioning = false;
                    ShowReady();
                }
            }
        }

        private void ShowInitializing()
        {
            promptState = PromptState.Initializing;
            isTransitioning = false;
            if (initText != null)
            {
                initText.gameObject.SetActive(false);
            }

            if (startButton != null)
            {
                startButton.interactable = false;
            }
        }

        private void ShowReady()
        {
            promptState = PromptState.Ready;
            if (initText != null)
            {
                initText.text = ResolveLocalizedText(
                    ReadyTextKey,
                    ReadyText);
                initText.gameObject.SetActive(true);
            }

            if (startButton != null)
            {
                startButton.interactable = !isTransitioning;
            }

            subscribedBootstrapper?.AudioService?
                .NotifyInitialInitReady();
        }

        private void ShowFailure()
        {
            promptState = PromptState.Failure;
            isTransitioning = false;
            if (initText != null)
            {
                initText.text = ResolveLocalizedText(
                    FailureTextKey,
                    FailureText);
                initText.gameObject.SetActive(true);
            }

            if (startButton != null)
            {
                startButton.interactable = false;
            }
        }

        private string ResolveLocalizedText(string key, string fallback)
        {
            if (subscribedLocalizationService == null)
            {
                return fallback;
            }

            string localized = subscribedLocalizationService.GetText(key);
            return string.Equals(
                    localized,
                    LocalizationService.MissingKey(key),
                    StringComparison.Ordinal)
                ? fallback
                : localized;
        }

        private void OnValidate()
        {
            if (!IsConfigured)
            {
                GameLogger.Warning(
                    "[InitScreenController] Start Button and Init Text "
                        + "references are required.",
                    this);
            }
        }
    }
}
