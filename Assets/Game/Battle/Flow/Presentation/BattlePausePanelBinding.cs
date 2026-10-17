using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Logging;
using ValorChronicle.Core.Pause;
using ValorChronicle.Core.Scene;
using ValorChronicle.Localization;
using ValorChronicle.UI.Settings;

namespace ValorChronicle.Battle.Flow.Presentation
{
    public sealed class BattlePausePanelBinding : MonoBehaviour
    {
        private const string PausedLocalizationKey =
            "ui.battle.pause.paused";
        private const string GiveUpConfirmLocalizationKey =
            "ui.battle.pause.give_up_confirm";

        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private LocalizedTMPText pauseText;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button giveUpButton;
        [SerializeField] private GameObject giveUpConfirmation;
        [SerializeField] private Button confirmGiveUpButton;
        [SerializeField] private Button cancelGiveUpButton;
        [SerializeField] private BattleFlowController battleFlowController;
        [SerializeField] private SettingsPanelHost settingsPanelHost;

        private GamePauseService pauseService;
        private SceneService sceneService;
        private Func<Task<bool>> loadMainSceneAsync;
        private IDisposable pauseHandle;
        private GamePauseService subscribedPauseService;
        private BattleFlowCoordinator subscribedCoordinator;
        private Coroutine coordinatorBindingCoroutine;
        private bool subscribed;
        private bool terminalResultReached;
        private bool giveUpInProgress;

        public bool IsConfigured => pauseButton != null
            && pausePanel != null
            && pauseText != null
            && resumeButton != null
            && giveUpButton != null
            && giveUpConfirmation != null
            && confirmGiveUpButton != null
            && cancelGiveUpButton != null
            && battleFlowController != null
            && settingsPanelHost != null;
        public bool IsOpen => pausePanel != null && pausePanel.activeSelf;
        public bool IsGiveUpConfirmationOpen => giveUpConfirmation != null
            && giveUpConfirmation.activeSelf;
        public bool IsGiveUpInProgress => giveUpInProgress;
        public Button PauseButton => pauseButton;
        public GameObject PausePanel => pausePanel;
        public LocalizedTMPText PauseText => pauseText;
        public Button ResumeButton => resumeButton;
        public Button GiveUpButton => giveUpButton;
        public GameObject GiveUpConfirmation => giveUpConfirmation;
        public Button ConfirmGiveUpButton => confirmGiveUpButton;
        public Button CancelGiveUpButton => cancelGiveUpButton;
        public BattleFlowController BattleFlowController =>
            battleFlowController;
        public SettingsPanelHost SettingsPanelHost => settingsPanelHost;

        private void Awake()
        {
            ResetPausePanelPresentation();
            SetPausePanelVisible(false);
        }

        private void OnEnable()
        {
            Subscribe();
            BeginCoordinatorSubscription();
            RefreshAvailability();
        }

        private void Start()
        {
            TryResolveRuntimeServices(logFailure: true);
            TrySubscribeCoordinator();
            RefreshAvailability();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ClosePausePanel(restoreSettingsAvailability: CanPauseBattle());
        }

        private void OnDestroy()
        {
            Unsubscribe();
            ClosePausePanel(restoreSettingsAvailability: false);
        }

        public void Initialize(
            GamePauseService gamePauseService,
            SceneService gameSceneService)
        {
            pauseService = gamePauseService
                ?? throw new ArgumentNullException(nameof(gamePauseService));
            sceneService = gameSceneService
                ?? throw new ArgumentNullException(nameof(gameSceneService));
            loadMainSceneAsync = () => sceneService.LoadAsync(GameScene.Main);
            if (isActiveAndEnabled)
                SubscribePauseService();
            RefreshAvailability();
        }

        private void Subscribe()
        {
            if (subscribed)
                return;

            pauseButton?.onClick.AddListener(HandlePauseRequested);
            resumeButton?.onClick.AddListener(HandleResumeRequested);
            giveUpButton?.onClick.AddListener(HandleGiveUpRequested);
            confirmGiveUpButton?.onClick.AddListener(
                HandleConfirmGiveUpRequested);
            cancelGiveUpButton?.onClick.AddListener(
                HandleCancelGiveUpRequested);
            if (battleFlowController != null)
            {
                battleFlowController.BattleResultReached +=
                    HandleBattleResultReached;
            }

            SubscribePauseService();

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (subscribed)
            {
                pauseButton?.onClick.RemoveListener(HandlePauseRequested);
                resumeButton?.onClick.RemoveListener(HandleResumeRequested);
                giveUpButton?.onClick.RemoveListener(HandleGiveUpRequested);
                confirmGiveUpButton?.onClick.RemoveListener(
                    HandleConfirmGiveUpRequested);
                cancelGiveUpButton?.onClick.RemoveListener(
                    HandleCancelGiveUpRequested);
                if (battleFlowController != null)
                {
                    battleFlowController.BattleResultReached -=
                        HandleBattleResultReached;
                }
            }

            StopCoordinatorSubscription();
            UnsubscribePauseService();
            subscribed = false;
        }

        private void HandlePauseRequested()
        {
            if (IsOpen || !CanPauseBattle())
                return;
            if (!TryResolveRuntimeServices(logFailure: true))
                return;

            settingsPanelHost.SetExternallyAvailable(false);
            try
            {
                pauseHandle = pauseService.RequestPause();
                ResetPausePanelPresentation();
                SetPauseActionsInteractable(true);
                pausePanel.transform.SetAsLastSibling();
                SetPausePanelVisible(true);
            }
            catch (Exception exception)
            {
                ReleasePauseHandle();
                settingsPanelHost.SetExternallyAvailable(true);
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[BattlePausePanel] Failed to pause battle.",
                    this);
            }
        }

        private void HandleResumeRequested()
        {
            if (!IsOpen || terminalResultReached || giveUpInProgress)
                return;

            ClosePausePanel(restoreSettingsAvailability: true);
        }

        private void HandleGiveUpRequested()
        {
            if (!IsOpen || terminalResultReached || giveUpInProgress)
                return;

            SetPauseActionsInteractable(false);
            SetGiveUpConfirmationActionsInteractable(true);
            SetPrimaryPauseActionsVisible(false);
            pauseText.SetLocalizationKey(GiveUpConfirmLocalizationKey);
            SetGiveUpConfirmationVisible(true);
            giveUpConfirmation.transform.SetAsLastSibling();
        }

        private void HandleCancelGiveUpRequested()
        {
            if (!IsGiveUpConfirmationOpen || giveUpInProgress)
                return;

            ResetPausePanelPresentation();
            SetPauseActionsInteractable(true);
        }

        private async void HandleConfirmGiveUpRequested()
        {
            if (!IsGiveUpConfirmationOpen
                || terminalResultReached
                || giveUpInProgress
                || !CanPauseBattle())
            {
                return;
            }

            if (!TryResolveRuntimeServices(logFailure: true)
                || (sceneService != null && sceneService.IsLoading))
            {
                return;
            }

            giveUpInProgress = true;
            SetAllButtonsInteractable(false);

            // Abort first. This synchronously invalidates flow callbacks and
            // releases all pause requests before scene navigation begins.
            if (!battleFlowController.AbortBattle())
            {
                giveUpInProgress = false;
                RefreshAvailability();
                return;
            }

            try
            {
                bool loaded = await loadMainSceneAsync();
                if (!loaded)
                {
                    GameLogger.Error(
                        "[BattlePausePanel] Failed to load the Main scene "
                            + "after abandoning battle.",
                        this);
                }
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[BattlePausePanel] Main scene navigation failed after "
                        + "abandoning battle.",
                    this);
            }
        }

        private void HandleBattleResultReached(BattleResultKind result)
        {
            terminalResultReached = true;
            SetAllButtonsInteractable(false);
            settingsPanelHost?.SetExternallyAvailable(false);
            ClosePausePanel(restoreSettingsAvailability: false);
        }

        private void HandleBattlePhaseChanged(BattlePhase phase)
        {
            RefreshAvailability();
        }

        private void BeginCoordinatorSubscription()
        {
            if (TrySubscribeCoordinator()
                || coordinatorBindingCoroutine != null
                || !isActiveAndEnabled)
            {
                return;
            }

            coordinatorBindingCoroutine = StartCoroutine(
                WaitForCoordinator());
        }

        private IEnumerator WaitForCoordinator()
        {
            while (isActiveAndEnabled && !TrySubscribeCoordinator())
                yield return null;

            coordinatorBindingCoroutine = null;
        }

        private bool TrySubscribeCoordinator()
        {
            BattleFlowCoordinator coordinator =
                battleFlowController?.Coordinator;
            if (ReferenceEquals(subscribedCoordinator, coordinator))
                return coordinator != null;

            UnsubscribeCoordinator();
            if (coordinator == null)
                return false;

            subscribedCoordinator = coordinator;
            subscribedCoordinator.PhaseChanged += HandleBattlePhaseChanged;
            RefreshAvailability();
            return true;
        }

        private void StopCoordinatorSubscription()
        {
            if (coordinatorBindingCoroutine != null)
            {
                StopCoroutine(coordinatorBindingCoroutine);
                coordinatorBindingCoroutine = null;
            }

            UnsubscribeCoordinator();
        }

        private void UnsubscribeCoordinator()
        {
            if (subscribedCoordinator == null)
                return;

            subscribedCoordinator.PhaseChanged -= HandleBattlePhaseChanged;
            subscribedCoordinator = null;
        }

        private void HandlePauseStateChanged(bool isPaused)
        {
            if (!isPaused
                && IsOpen
                && pauseHandle != null
                && !giveUpInProgress)
            {
                ClosePausePanel(restoreSettingsAvailability: true);
                return;
            }

            RefreshAvailability();
        }

        private bool CanPauseBattle()
        {
            BattleContext context = battleFlowController?.Context;
            return !terminalResultReached
                && !giveUpInProgress
                && context != null
                && context.Phase != BattlePhase.NotStarted
                && context.Result == BattleResultKind.None
                && (pauseHandle != null || pauseService?.IsPaused != true);
        }

        private bool TryResolveRuntimeServices(bool logFailure)
        {
            if (pauseService != null && loadMainSceneAsync != null)
            {
                SubscribePauseService();
                return true;
            }

            GameBootstrapper bootstrapper = GameBootstrapper.Instance;
            if (bootstrapper != null)
            {
                pauseService ??= bootstrapper.GamePauseService;
                sceneService ??= bootstrapper.SceneService;
                if (loadMainSceneAsync == null && sceneService != null)
                {
                    loadMainSceneAsync = () =>
                        sceneService.LoadAsync(GameScene.Main);
                }
            }

            bool resolved = pauseService != null
                && loadMainSceneAsync != null;
            if (resolved)
                SubscribePauseService();
            if (!resolved && logFailure)
            {
                GameLogger.Error(
                    "[BattlePausePanel] Runtime services are not available.",
                    this);
            }

            return resolved;
        }

        private void SubscribePauseService()
        {
            if (ReferenceEquals(subscribedPauseService, pauseService))
                return;

            UnsubscribePauseService();
            subscribedPauseService = pauseService;
            if (subscribedPauseService != null)
            {
                subscribedPauseService.PauseStateChanged +=
                    HandlePauseStateChanged;
            }
        }

        private void UnsubscribePauseService()
        {
            if (subscribedPauseService == null)
                return;

            subscribedPauseService.PauseStateChanged -=
                HandlePauseStateChanged;
            subscribedPauseService = null;
        }

        private void RefreshAvailability()
        {
            bool available = isActiveAndEnabled
                && IsConfigured
                && CanPauseBattle();
            if (pauseButton != null)
                pauseButton.interactable = available && !IsOpen;

            if (!IsOpen)
                return;

            SetPauseActionsInteractable(available);
        }

        private void ClosePausePanel(bool restoreSettingsAvailability)
        {
            ResetPausePanelPresentation();
            SetPausePanelVisible(false);
            ReleasePauseHandle();

            if (restoreSettingsAvailability && CanPauseBattle())
                settingsPanelHost?.SetExternallyAvailable(true);

            RefreshAvailability();
        }

        private void ReleasePauseHandle()
        {
            IDisposable handle = pauseHandle;
            pauseHandle = null;
            handle?.Dispose();
        }

        private void SetPausePanelVisible(bool visible)
        {
            if (pausePanel != null)
                pausePanel.SetActive(visible);
        }

        private void SetGiveUpConfirmationVisible(bool visible)
        {
            if (giveUpConfirmation != null)
                giveUpConfirmation.SetActive(visible);
        }

        private void ResetPausePanelPresentation()
        {
            SetGiveUpConfirmationVisible(false);
            SetPrimaryPauseActionsVisible(true);
            pauseText?.SetLocalizationKey(PausedLocalizationKey);
        }

        private void SetPrimaryPauseActionsVisible(bool visible)
        {
            if (resumeButton != null)
                resumeButton.gameObject.SetActive(visible);
            if (giveUpButton != null)
                giveUpButton.gameObject.SetActive(visible);
        }

        private void SetPauseActionsInteractable(bool interactable)
        {
            if (resumeButton != null)
                resumeButton.interactable = interactable;
            if (giveUpButton != null)
                giveUpButton.interactable = interactable;
            SetGiveUpConfirmationActionsInteractable(interactable);
        }

        private void SetGiveUpConfirmationActionsInteractable(
            bool interactable)
        {
            if (confirmGiveUpButton != null)
                confirmGiveUpButton.interactable = interactable;
            if (cancelGiveUpButton != null)
                cancelGiveUpButton.interactable = interactable;
        }

        private void SetAllButtonsInteractable(bool interactable)
        {
            if (pauseButton != null)
                pauseButton.interactable = interactable;
            SetPauseActionsInteractable(interactable);
        }
    }
}
