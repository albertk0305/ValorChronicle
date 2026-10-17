using System;
using ValorChronicle.Settings.DTO;
using ValorChronicle.Settings.Services;

namespace ValorChronicle.Audio.Core
{
    public sealed class AudioSettingsVolumeBinding : IDisposable
    {
        private readonly SettingsService settingsService;
        private readonly IAudioVolumeOutput output;
        private int masterVolume = -1;
        private int bgmVolume = -1;
        private int sfxVolume = -1;
        private bool isDisposed;

        public AudioSettingsVolumeBinding(
            SettingsService settingsService,
            IAudioVolumeOutput output)
        {
            this.settingsService = settingsService
                ?? throw new ArgumentNullException(nameof(settingsService));
            this.output = output
                ?? throw new ArgumentNullException(nameof(output));

            ApplyInitial(settingsService.Current);
            settingsService.SettingsChanged += HandleSettingsChanged;
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            settingsService.SettingsChanged -= HandleSettingsChanged;
            isDisposed = true;
        }

        private void ApplyInitial(GameSettingsData settings)
        {
            ApplyMaster(settings.MasterVolume);
            ApplyBgm(settings.BgmVolume);
            ApplySfx(settings.SfxVolume);
        }

        private void HandleSettingsChanged(SettingsChangedEvent changed)
        {
            if ((changed.ChangeKind & SettingsChangeKind.MasterVolume) != 0)
                ApplyMaster(changed.Settings.MasterVolume);
            if ((changed.ChangeKind & SettingsChangeKind.BgmVolume) != 0)
                ApplyBgm(changed.Settings.BgmVolume);
            if ((changed.ChangeKind & SettingsChangeKind.SfxVolume) != 0)
                ApplySfx(changed.Settings.SfxVolume);
        }

        private void ApplyMaster(int volume)
        {
            if (masterVolume == volume)
                return;

            masterVolume = volume;
            output.ApplyVolume(
                AudioVolumeChannel.Master,
                AudioVolumeMath.PercentageToDecibels(volume));
        }

        private void ApplyBgm(int volume)
        {
            if (bgmVolume == volume)
                return;

            bgmVolume = volume;
            output.ApplyVolume(
                AudioVolumeChannel.Bgm,
                AudioVolumeMath.PercentageToDecibels(volume));
        }

        private void ApplySfx(int volume)
        {
            if (sfxVolume == volume)
                return;

            sfxVolume = volume;
            output.ApplyVolume(
                AudioVolumeChannel.Sfx,
                AudioVolumeMath.PercentageToDecibels(volume));
        }
    }
}
