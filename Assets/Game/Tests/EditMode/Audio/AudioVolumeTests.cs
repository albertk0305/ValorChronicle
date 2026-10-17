using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ValorChronicle.Audio.Core;
using ValorChronicle.Audio.Runtime;
using ValorChronicle.Settings.Services;

namespace ValorChronicle.Tests.EditMode.Audio
{
    public sealed class AudioVolumeTests
    {
        [Test]
        public void PercentageToDecibels_ConvertsMuteFullAndMidpoint()
        {
            Assert.That(AudioVolumeMath.PercentageToDecibels(0),
                Is.EqualTo(-80f));
            Assert.That(AudioVolumeMath.PercentageToDecibels(100),
                Is.EqualTo(0f).Within(0.0001f));
            Assert.That(AudioVolumeMath.PercentageToDecibels(50),
                Is.EqualTo(-6.0206f).Within(0.0001f));
        }

        [Test]
        public void SettingsBinding_AppliesStoredValuesAndOnlyChangedChannel()
        {
            SettingsService settings = SettingsService.CreateInMemoryDefaults();
            settings.SetMasterVolume(80);
            settings.SetBgmVolume(60);
            settings.SetSfxVolume(40);
            var output = new RecordingAudioOutput();

            using (new AudioSettingsVolumeBinding(settings, output))
            {
                Assert.That(output.Volumes.Count, Is.EqualTo(3));
                Assert.That(output.Volumes[0].Channel,
                    Is.EqualTo(AudioVolumeChannel.Master));
                Assert.That(output.Volumes[1].Channel,
                    Is.EqualTo(AudioVolumeChannel.Bgm));
                Assert.That(output.Volumes[2].Channel,
                    Is.EqualTo(AudioVolumeChannel.Sfx));

                settings.SetBgmVolume(25);
                Assert.That(output.Volumes.Count, Is.EqualTo(4));
                Assert.That(output.Volumes[3].Channel,
                    Is.EqualTo(AudioVolumeChannel.Bgm));

                settings.SetBgmVolume(25);
                settings.SetLocale("ko");
                Assert.That(output.Volumes.Count, Is.EqualTo(4));
            }
        }

        [Test]
        public void MixerlessFallback_AppliesSfxVolumeToRegularAndUiSources()
        {
            var owner = new GameObject("AudioOutputTest");
            try
            {
                LogAssert.Expect(
                    LogType.Warning,
                    "[Audio] AudioConfiguration is not assigned. "
                        + "Audio remains available without clips or mixer routing.");
                var output = new UnityAudioOutput(owner, null);

                Assert.DoesNotThrow(output.PlayNormalFromStart);
                Assert.DoesNotThrow(
                    () => output.PlaySpecial(BgmTrackKind.Battle));

                output.ApplyVolume(AudioVolumeChannel.Master, 0f);
                output.ApplyVolume(
                    AudioVolumeChannel.Sfx,
                    AudioVolumeMath.PercentageToDecibels(25));

                AudioSource sfx = owner.transform.Find("Audio_Sfx")
                    .GetComponent<AudioSource>();
                AudioSource ui = owner.transform.Find("Audio_UiSfx")
                    .GetComponent<AudioSource>();
                Assert.That(sfx.volume, Is.EqualTo(0.25f).Within(0.0001f));
                Assert.That(ui.volume, Is.EqualTo(sfx.volume).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }
    }
}
