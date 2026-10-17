using System;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Logging;
using ValorChronicle.Core.Pause;
using ValorChronicle.Settings.Services;

namespace ValorChronicle.UI.Settings
{
    public sealed class SettingsPanelHost : MonoBehaviour
    {
        [SerializeField] private Button settingButton;
        [SerializeField] private GameObject settingsPanelPrefab;
        [SerializeField] private Transform panelParent;
        [SerializeField] private bool pauseWhileOpen;

        private GameBootstrapper subscribedBootstrapper;
        private SettingsPanelView panelView;
        private SettingsPanelPresenter presenter;
        private IDisposable pauseHandle;
        private bool externallyAvailable = true;

        public bool IsReady => presenter != null
            || (GameBootstrapper.Instance?.SettingsService != null
                && (!pauseWhileOpen
                    || GameBootstrapper.Instance?.GamePauseService != null)
                && settingButton != null
                && settingsPanelPrefab != null
                && panelParent != null);
        public bool IsOpen => presenter?.IsOpen == true;

        private void Awake()
        {
            if (settingButton != null)
                settingButton.interactable = false;
        }

        private void OnEnable()
        {
            if (settingButton != null)
                settingButton.onClick.AddListener(HandleOpenRequested);
        }

        private void Start()
        {
            TryBindBootstrapper();
        }

        private void OnDisable()
        {
            if (settingButton != null)
                settingButton.onClick.RemoveListener(HandleOpenRequested);

            if (pauseWhileOpen)
                ClosePanel();
        }

        private void OnDestroy()
        {
            UnbindBootstrapper();

            try
            {
                ClosePanel();
                if (presenter != null)
                {
                    presenter.Closed -= HandlePanelClosed;
                    presenter.Dispose();
                    presenter = null;
                }
            }
            finally
            {
                if (panelView != null)
                {
                    panelView.Hidden -= HandlePanelHidden;
                    panelView = null;
                }

                ReleasePauseHandle();
            }
        }

        public void SetExternallyAvailable(bool available)
        {
            externallyAvailable = available;
            if (!available)
                ClosePanel();
            RefreshAvailability();
        }

        public void ClosePanel()
        {
            if (presenter == null || !presenter.IsOpen)
            {
                ReleasePauseHandle();
                return;
            }

            try
            {
                presenter.Close();
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[SettingsPanelHost] Settings panel close failed.",
                    this);
                ReleasePauseHandle();
            }
        }

        private void TryBindBootstrapper()
        {
            GameBootstrapper bootstrapper = GameBootstrapper.Instance;
            if (bootstrapper == null)
            {
                SetButtonInteractable(false);
                GameLogger.Warning(
                    "[SettingsPanelHost] GameBootstrapper is not available; "
                        + "the settings button remains disabled.",
                    this);
                return;
            }

            if (!ReferenceEquals(subscribedBootstrapper, bootstrapper))
            {
                UnbindBootstrapper();
                subscribedBootstrapper = bootstrapper;
                subscribedBootstrapper.InitializationCompleted +=
                    HandleInitializationCompleted;
            }

            RefreshAvailability();
        }

        private void UnbindBootstrapper()
        {
            if (subscribedBootstrapper == null)
                return;

            subscribedBootstrapper.InitializationCompleted -=
                HandleInitializationCompleted;
            subscribedBootstrapper = null;
        }

        private void HandleInitializationCompleted(
            BootstrapInitializationResult result)
        {
            RefreshAvailability();
        }

        private void RefreshAvailability()
        {
            bool configured = settingButton != null
                && settingsPanelPrefab != null
                && panelParent != null;
            bool initialized = subscribedBootstrapper?.SettingsService != null
                && (!pauseWhileOpen
                    || subscribedBootstrapper.GamePauseService != null);
            SetButtonInteractable(
                externallyAvailable && configured && initialized);

            if (!configured)
            {
                GameLogger.Error(
                    "[SettingsPanelHost] Inspector references are incomplete.",
                    this);
            }
        }

        private void SetButtonInteractable(bool interactable)
        {
            if (settingButton != null)
                settingButton.interactable = interactable;
        }

        private void HandleOpenRequested()
        {
            if (!externallyAvailable)
                return;

            SettingsService settingsService =
                subscribedBootstrapper?.SettingsService;
            if (settingsService == null)
            {
                SetButtonInteractable(false);
                GameLogger.Error(
                    "[SettingsPanelHost] SettingsService is not initialized.",
                    this);
                return;
            }

            if (!EnsurePanel(settingsService))
                return;

            if (presenter.IsOpen)
                return;

            if (!TryAcquirePauseHandle())
                return;

            try
            {
                presenter.Open();
            }
            catch (Exception exception)
            {
                ReleasePauseHandle();
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[SettingsPanelHost] Settings panel open failed.",
                    this);
            }
        }

        private bool EnsurePanel(SettingsService settingsService)
        {
            if (presenter != null)
                return true;
            if (settingsPanelPrefab == null || panelParent == null)
            {
                GameLogger.Error(
                    "[SettingsPanelHost] Panel prefab or parent is missing.",
                    this);
                return false;
            }

            GameObject instance = Instantiate(
                settingsPanelPrefab,
                panelParent,
                false);
            panelView = instance.GetComponent<SettingsPanelView>();
            if (panelView == null || !panelView.IsConfigured)
            {
                GameLogger.Error(
                    "[SettingsPanelHost] SettingsPanel prefab is not configured.",
                    this);
                Destroy(instance);
                panelView = null;
                return false;
            }

            panelView.Close();
            panelView.Hidden += HandlePanelHidden;
            presenter = new SettingsPanelPresenter(settingsService, panelView);
            presenter.Closed += HandlePanelClosed;
            return true;
        }

        private void HandlePanelClosed(SettingsWriteResult result)
        {
            ReleasePauseHandle();
            LogWriteFailure(result);
        }

        private void HandlePanelHidden()
        {
            ReleasePauseHandle();
        }

        private bool TryAcquirePauseHandle()
        {
            if (!pauseWhileOpen || pauseHandle != null)
                return true;

            GamePauseService pauseService =
                subscribedBootstrapper?.GamePauseService;
            if (pauseService == null)
            {
                SetButtonInteractable(false);
                GameLogger.Error(
                    "[SettingsPanelHost] GamePauseService is not initialized.",
                    this);
                return false;
            }

            try
            {
                pauseHandle = pauseService.RequestPause();
                return true;
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[SettingsPanelHost] Failed to request game pause.",
                    this);
                return false;
            }
        }

        private void ReleasePauseHandle()
        {
            IDisposable handle = pauseHandle;
            pauseHandle = null;
            handle?.Dispose();
        }

        private void LogWriteFailure(SettingsWriteResult result)
        {
            if (result == null || result.IsSuccess)
                return;

            if (result.Exception != null)
                GameLogger.Exception(result.Exception, this);
            GameLogger.Error(
                $"[SettingsPanelHost] Settings flush failed. "
                    + $"Status={result.Status}. {result.Message}",
                this);
        }
    }
}
