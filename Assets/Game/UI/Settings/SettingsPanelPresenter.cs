using System;
using System.Collections.Generic;
using System.Globalization;
using ValorChronicle.Core.Logging;
using ValorChronicle.Localization;
using ValorChronicle.Redeem;
using ValorChronicle.Settings.Services;

namespace ValorChronicle.UI.Settings
{
    public sealed class SettingsPanelPresenter : IDisposable
    {
        private readonly SettingsService settingsService;
        private readonly SettingsPanelView view;
        private readonly IRedeemCodeService redeemCodeService;
        private readonly LocalizationService localizationService;
        private RedeemCodeResult currentRedeemResult;
        private bool redeemInProgress;
        private bool disposed;

        public SettingsPanelPresenter(
            SettingsService settingsService,
            SettingsPanelView view)
            : this(settingsService, view, null, null)
        {
        }

        public SettingsPanelPresenter(
            SettingsService settingsService,
            SettingsPanelView view,
            IRedeemCodeService redeemCodeService,
            LocalizationService localizationService)
        {
            this.settingsService = settingsService
                ?? throw new ArgumentNullException(nameof(settingsService));
            this.view = view
                ?? throw new ArgumentNullException(nameof(view));
            this.redeemCodeService = redeemCodeService;
            this.localizationService = localizationService;

            view.MasterVolumeChanged += HandleMasterVolumeChanged;
            view.BgmVolumeChanged += HandleBgmVolumeChanged;
            view.SfxVolumeChanged += HandleSfxVolumeChanged;
            view.LocaleChanged += HandleLocaleChanged;
            view.BattleSpeedChanged += HandleBattleSpeedChanged;
            view.RedeemCodeRequested += HandleRedeemCodeRequested;
            view.RedeemResultDismissed += HandleRedeemResultDismissed;
            view.CloseRequested += HandleCloseRequested;
            settingsService.SettingsChanged += HandleSettingsChanged;
            if (localizationService != null)
                localizationService.LocaleChanged += HandleLocalizationChanged;
        }

        public event Action Opened;
        public event Action<SettingsWriteResult> Closed;

        public bool IsOpen => !disposed && view.IsOpen;

        public void Open()
        {
            ThrowIfDisposed();
            currentRedeemResult = null;
            redeemInProgress = false;
            view.ResetRedeemUi();
            RefreshRedeemLocalization();
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
            view.RedeemCodeRequested -= HandleRedeemCodeRequested;
            view.RedeemResultDismissed -= HandleRedeemResultDismissed;
            view.CloseRequested -= HandleCloseRequested;
            settingsService.SettingsChanged -= HandleSettingsChanged;
            if (localizationService != null)
                localizationService.LocaleChanged -= HandleLocalizationChanged;
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

        private void HandleRedeemCodeRequested(string input)
        {
            if (disposed
                || redeemInProgress
                || view.IsRedeemResultVisible)
            {
                return;
            }

            redeemInProgress = true;
            view.SetRedeemBusy(true);
            try
            {
                currentRedeemResult = redeemCodeService == null
                    ? new RedeemCodeResult(
                        RedeemCodeStatus.PersistenceFailed)
                    : redeemCodeService.Redeem(input);
                if (currentRedeemResult == null)
                {
                    currentRedeemResult = new RedeemCodeResult(
                        RedeemCodeStatus.PersistenceFailed);
                }

                RefreshRedeemResult();
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception);
                currentRedeemResult = new RedeemCodeResult(
                    RedeemCodeStatus.PersistenceFailed);
                RefreshRedeemResult();
            }
            finally
            {
                redeemInProgress = false;
                if (!view.IsRedeemResultVisible)
                    view.SetRedeemBusy(false);
            }
        }

        private void HandleRedeemResultDismissed()
        {
            currentRedeemResult = null;
        }

        private void HandleLocalizationChanged(string _)
        {
            if (disposed)
                return;

            RefreshRedeemLocalization();
            if (currentRedeemResult != null
                && view.IsRedeemResultVisible)
            {
                RefreshRedeemResult();
            }
        }

        private void RefreshRedeemLocalization()
        {
            if (!view.IsRedeemConfigured || localizationService == null)
                return;

            view.SetRedeemPlaceholder(localizationService.GetText(
                "ui.settings.redeem_code_placeholder"));
        }

        private void RefreshRedeemResult()
        {
            if (currentRedeemResult == null
                || !view.IsRedeemConfigured
                || localizationService == null)
            {
                return;
            }

            string message;
            switch (currentRedeemResult.Status)
            {
                case RedeemCodeStatus.Success:
                    string amount = currentRedeemResult.Definition.Amount
                        .ToString("N0", CultureInfo.InvariantCulture);
                    string reward = localizationService.Format(
                        "ui.settings.redeem_reward_gacha_currency",
                        new Dictionary<string, object>
                        {
                            { "amount", amount }
                        });
                    message = localizationService.GetText(
                        "ui.settings.redeem_success")
                        + "\n"
                        + reward;
                    break;
                case RedeemCodeStatus.EmptyCode:
                    message = localizationService.GetText(
                        "ui.settings.redeem_empty");
                    break;
                case RedeemCodeStatus.InvalidCode:
                    message = localizationService.GetText(
                        "ui.settings.redeem_invalid");
                    break;
                default:
                    message = localizationService.GetText(
                        "ui.settings.redeem_save_failed");
                    break;
            }

            view.ShowRedeemResult(message);
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(SettingsPanelPresenter));
        }
    }
}
