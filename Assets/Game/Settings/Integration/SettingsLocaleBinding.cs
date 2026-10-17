using System;
using ValorChronicle.Localization;
using ValorChronicle.Settings.Services;

namespace ValorChronicle.Settings.Integration
{
    public sealed class SettingsLocaleBinding : IDisposable
    {
        private readonly SettingsService settingsService;
        private readonly LocalizationService localizationService;
        private bool isDisposed;

        public SettingsLocaleBinding(
            SettingsService settingsService,
            LocalizationService localizationService)
        {
            this.settingsService = settingsService
                ?? throw new ArgumentNullException(nameof(settingsService));
            this.localizationService = localizationService
                ?? throw new ArgumentNullException(nameof(localizationService));

            localizationService.SetLocale(settingsService.Current.LocaleId);
            settingsService.SettingsChanged += HandleSettingsChanged;
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            settingsService.SettingsChanged -= HandleSettingsChanged;
            isDisposed = true;
        }

        private void HandleSettingsChanged(SettingsChangedEvent changed)
        {
            if ((changed.ChangeKind & SettingsChangeKind.LocaleId) != 0)
                localizationService.SetLocale(changed.Settings.LocaleId);
        }
    }
}
