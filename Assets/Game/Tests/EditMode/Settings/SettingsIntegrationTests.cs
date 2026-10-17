using NUnit.Framework;
using ValorChronicle.Localization;
using ValorChronicle.Settings.Integration;
using ValorChronicle.Settings.Services;

namespace ValorChronicle.Tests.EditMode.Settings
{
    public sealed class SettingsIntegrationTests
    {
        [Test]
        public void Scheduler_DebouncesAgainstUnscaledClockValues()
        {
            var scheduler = new SettingsSaveScheduler(0.3d);

            scheduler.NotifyChange(10d);
            Assert.That(scheduler.IsDue(10.29d), Is.False);
            scheduler.NotifyChange(10.2d);
            Assert.That(scheduler.IsDue(10.49d), Is.False);
            Assert.That(scheduler.IsDue(10.5d), Is.True);
            scheduler.Clear();
            Assert.That(scheduler.IsDue(100d), Is.False);
        }

        [Test]
        public void LocaleBinding_AppliesStoredAndSubsequentLocalesImmediately()
        {
            var settings = SettingsService.CreateInMemoryDefaults();
            settings.SetLocale("ko");
            LocalizationService localization = CreateLocalizationService();

            using (new SettingsLocaleBinding(settings, localization))
            {
                Assert.That(localization.CurrentLocale, Is.EqualTo("ko"));

                settings.SetLocale("en");

                Assert.That(localization.CurrentLocale, Is.EqualTo("en"));
            }

            settings.SetLocale("ko");
            Assert.That(localization.CurrentLocale, Is.EqualTo("en"));
        }

        private static LocalizationService CreateLocalizationService()
        {
            LocalizationTable table = LocalizationTableParser.Parse(
                "settings-test.tsv",
                "key\ten\tko\nlabel\tSettings\t설정");
            return new LocalizationService(new[] { table });
        }
    }
}
