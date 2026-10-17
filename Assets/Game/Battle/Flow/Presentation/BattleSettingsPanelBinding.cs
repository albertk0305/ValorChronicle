using UnityEngine;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Pause;
using ValorChronicle.Settings.Services;
using ValorChronicle.UI.Settings;

namespace ValorChronicle.Battle.Flow.Presentation
{
    public sealed class BattleSettingsPanelBinding : MonoBehaviour
    {
        [SerializeField]
        private SettingsPanelHost settingsPanelHost;

        [SerializeField]
        private BattleFlowController battleFlowController;

        private bool subscribed;
        private SettingsService settingsService;
        private GamePauseService pauseService;
        private bool settingsSubscribed;
        private bool ownsBattleTimeScale;
        private bool battleSpeedActive;

        public bool IsConfigured => settingsPanelHost != null
            && battleFlowController != null;

        private void OnEnable()
        {
            battleSpeedActive = true;
            Subscribe();
            TryResolveRuntimeServices();
            ApplyCurrentBattleSpeed();
            RefreshResultAvailability();
        }

        private void Start()
        {
            TryResolveRuntimeServices();
            ApplyCurrentBattleSpeed();
        }

        private void OnDisable()
        {
            battleSpeedActive = false;
            Unsubscribe();
            settingsPanelHost?.ClosePanel();
            RestoreNormalTimeScale();
        }

        private void OnDestroy()
        {
            battleSpeedActive = false;
            Unsubscribe();
            settingsPanelHost?.ClosePanel();
            RestoreNormalTimeScale();
        }

        private void Subscribe()
        {
            if (subscribed || battleFlowController == null)
                return;

            battleFlowController.BattleResultReached +=
                HandleBattleResultReached;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (subscribed && battleFlowController != null)
            {
                battleFlowController.BattleResultReached -=
                    HandleBattleResultReached;
            }

            subscribed = false;
            UnsubscribeSettings();
        }

        private void RefreshResultAvailability()
        {
            if (!IsConfigured)
                return;

            bool resultReached = battleFlowController.Context != null
                && battleFlowController.Context.Result
                    != BattleResultKind.None;
            settingsPanelHost.SetExternallyAvailable(!resultReached);
            if (resultReached)
                settingsPanelHost.ClosePanel();
        }

        private void HandleBattleResultReached(BattleResultKind result)
        {
            battleSpeedActive = false;
            RestoreNormalTimeScale();
            settingsPanelHost?.SetExternallyAvailable(false);
            settingsPanelHost?.ClosePanel();
        }

        private void TryResolveRuntimeServices()
        {
            GameBootstrapper bootstrapper = GameBootstrapper.Instance;
            if (bootstrapper == null)
                return;

            settingsService ??= bootstrapper.SettingsService;
            pauseService ??= bootstrapper.GamePauseService;
            SubscribeSettings();
        }

        private void SubscribeSettings()
        {
            if (settingsSubscribed || settingsService == null)
                return;

            settingsService.SettingsChanged += HandleSettingsChanged;
            settingsSubscribed = true;
        }

        private void UnsubscribeSettings()
        {
            if (!settingsSubscribed || settingsService == null)
                return;

            settingsService.SettingsChanged -= HandleSettingsChanged;
            settingsSubscribed = false;
        }

        private void HandleSettingsChanged(SettingsChangedEvent changed)
        {
            if (changed == null
                || (changed.ChangeKind & SettingsChangeKind.BattleSpeed) == 0)
            {
                return;
            }

            ApplyBattleSpeed(changed.Settings.BattleSpeed);
        }

        private void ApplyCurrentBattleSpeed()
        {
            if (settingsService == null || pauseService == null)
                return;

            ApplyBattleSpeed(settingsService.Current.BattleSpeed);
        }

        private void ApplyBattleSpeed(int battleSpeed)
        {
            if (!battleSpeedActive || pauseService == null)
                return;

            pauseService.SetResumeTimeScale(
                BattleTimeScalePolicy.Resolve(battleSpeed));
            ownsBattleTimeScale = true;
        }

        private void RestoreNormalTimeScale()
        {
            if (!ownsBattleTimeScale || pauseService == null)
                return;

            pauseService.SetResumeTimeScale(
                BattleTimeScalePolicy.NormalSceneTimeScale);
            ownsBattleTimeScale = false;
        }
    }
}
