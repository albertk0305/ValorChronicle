using System;
using ValorChronicle.Settings.Services;

namespace ValorChronicle.UI.Settings
{
    public sealed class SettingsPanelPresenter : IDisposable
    {
        private readonly SettingsService settingsService;
        private readonly SettingsPanelView view;
        private bool disposed;

        public SettingsPanelPresenter(
            SettingsService settingsService,
            SettingsPanelView view)
        {
            this.settingsService = settingsService
                ?? throw new ArgumentNullException(nameof(settingsService));
            this.view = view
                ?? throw new ArgumentNullException(nameof(view));

            view.MasterVolumeChanged += HandleMasterVolumeChanged;
            view.BgmVolumeChanged += HandleBgmVolumeChanged;
            view.SfxVolumeChanged += HandleSfxVolumeChanged;
            view.LocaleChanged += HandleLocaleChanged;
            view.BattleSpeedChanged += HandleBattleSpeedChanged;
            view.CloseRequested += HandleCloseRequested;
            settingsService.SettingsChanged += HandleSettingsChanged;
        }

        public event Action Opened;
        public event Action<SettingsWriteResult> Closed;

        public bool IsOpen => !disposed && view.IsOpen;

        public void Open()
        {
            ThrowIfDisposed();
            view.Display(settingsService.Current);
            view.Open();
            Opened?.Invoke();
        }

        public SettingsWriteResult Close()
        {
            ThrowIfDisposed();
            view.Close();
            SettingsWriteResult result = settingsService.Flush();
            Closed?.Invoke(result);
            return result;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            view.MasterVolumeChanged -= HandleMasterVolumeChanged;
            view.BgmVolumeChanged -= HandleBgmVolumeChanged;
            view.SfxVolumeChanged -= HandleSfxVolumeChanged;
            view.LocaleChanged -= HandleLocaleChanged;
            view.BattleSpeedChanged -= HandleBattleSpeedChanged;
            view.CloseRequested -= HandleCloseRequested;
            settingsService.SettingsChanged -= HandleSettingsChanged;
            disposed = true;
        }

        private void HandleMasterVolumeChanged(int value)
        {
            settingsService.SetMasterVolume(value);
        }

        private void HandleBgmVolumeChanged(int value)
        {
            settingsService.SetBgmVolume(value);
        }

        private void HandleSfxVolumeChanged(int value)
        {
            settingsService.SetSfxVolume(value);
        }

        private void HandleLocaleChanged(string localeId)
        {
            settingsService.SetLocale(localeId);
        }

        private void HandleBattleSpeedChanged(int speed)
        {
            settingsService.SetBattleSpeed(speed);
        }

        private void HandleSettingsChanged(SettingsChangedEvent changed)
        {
            if (!disposed && changed != null)
                view.Display(changed.Settings);
        }

        private void HandleCloseRequested()
        {
            Close();
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(SettingsPanelPresenter));
        }
    }
}
