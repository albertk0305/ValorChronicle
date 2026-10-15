using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Logging;
using ValorChronicle.Core.Scene;

namespace ValorChronicle.UI
{
    public sealed class InitScreenController : MonoBehaviour
    {
        public const string ReadyText = "Press to Start";
        public const string FailureText =
            "게임 초기화에 실패했습니다. 게임을 다시 실행해 주세요.";

        [SerializeField]
        private Button startButton = null;

        [SerializeField]
        private TMP_Text initText = null;

        private GameBootstrapper subscribedBootstrapper;
        private BootstrapInitializationResult presentedResult;
        private bool isTransitioning;

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
            PresentInitializationResult(result);
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
            if (initText != null)
            {
                initText.text = ReadyText;
                initText.gameObject.SetActive(true);
            }

            if (startButton != null)
            {
                startButton.interactable = !isTransitioning;
            }
        }

        private void ShowFailure()
        {
            isTransitioning = false;
            if (initText != null)
            {
                initText.text = FailureText;
                initText.gameObject.SetActive(true);
            }

            if (startButton != null)
            {
                startButton.interactable = false;
            }
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
