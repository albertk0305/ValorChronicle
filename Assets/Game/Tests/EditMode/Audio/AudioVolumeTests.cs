using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using ValorChronicle.Audio.Core;
using ValorChronicle.Audio.Runtime;
using ValorChronicle.Settings.Services;
using GameAudioConfiguration =
    ValorChronicle.Audio.Configuration.AudioConfiguration;
using SettingsService = ValorChronicle.Settings.Services.SettingsService;

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
        public void AudioConfiguration_ProvidesRequestedFadeDefaultsForAllTracks()
        {
            GameAudioConfiguration configuration = AssetDatabase
                .LoadAssetAtPath<GameAudioConfiguration>(
                    "Assets/Data/Audio/AudioConfiguration.asset");
            Assert.That(configuration, Is.Not.Null);

            BgmFadeSettings settings = configuration.CreateFadeSettings();

            Assert.That(settings.SceneCrossfadeSeconds,
                Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(settings.ResultFadeOutSeconds,
                Is.EqualTo(1.2f).Within(0.0001f));
            foreach (BgmTrackKind track in new[]
            {
                BgmTrackKind.Init,
                BgmTrackKind.Normal,
                BgmTrackKind.Battle
            })
            {
                BgmLoopFadeSettings loop = settings.GetLoop(track);
                Assert.That(loop.Enabled, Is.True, track.ToString());
                Assert.That(loop.FadeInSeconds,
                    Is.EqualTo(0.6f).Within(0.0001f));
                Assert.That(loop.FadeOutSeconds,
                    Is.EqualTo(0.6f).Within(0.0001f));
            }
        }

        [Test]
        public void AudioConfiguration_MapsAllSfxCuesToImportedWavClips()
        {
            GameAudioConfiguration configuration = AssetDatabase
                .LoadAssetAtPath<GameAudioConfiguration>(
                    "Assets/Data/Audio/AudioConfiguration.asset");

            Assert.That(configuration, Is.Not.Null);
            AssertCueClip(
                configuration,
                SfxCue.ButtonPress,
                "dragon-studio-button-press-382713");
            AssertCueClip(
                configuration,
                SfxCue.BlockPop,
                "dragon-studio-pop-402324");
            AssertCueClip(
                configuration,
                SfxCue.Hit,
                "universfield-classic-punch-impact-352711");
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

        [Test]
        public void MixerlessFallback_KeepsUserBgmVolumeAcrossFadeAdvance()
        {
            var owner = new GameObject("AudioBgmFallbackTest");
            try
            {
                LogAssert.Expect(
                    LogType.Warning,
                    "[Audio] AudioConfiguration is not assigned. "
                        + "Audio remains available without clips or mixer routing.");
                var output = new UnityAudioOutput(owner, null);
                output.ApplyVolume(
                    AudioVolumeChannel.Master,
                    AudioVolumeMath.PercentageToDecibels(50));
                output.ApplyVolume(
                    AudioVolumeChannel.Bgm,
                    AudioVolumeMath.PercentageToDecibels(40));

                output.Advance(0.8f);

                AudioSource normal = owner.transform.Find("Audio_NormalBgm")
                    .GetComponent<AudioSource>();
                AudioSource special = owner.transform.Find("Audio_SpecialBgm")
                    .GetComponent<AudioSource>();
                Assert.That(normal.volume,
                    Is.EqualTo(0.2f).Within(0.0001f));
                Assert.That(special.volume,
                    Is.EqualTo(0.2f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void EffectPlayback_RestoresSfxAndUiPitchToOne()
        {
            var owner = new GameObject("AudioPitchTest");
            AudioClip clip = AudioClip.Create("test", 64, 1, 44100, false);
            try
            {
                LogAssert.Expect(
                    LogType.Warning,
                    "[Audio] AudioConfiguration is not assigned. "
                        + "Audio remains available without clips or mixer routing.");
                var output = new UnityAudioOutput(owner, null);
                AudioSource sfx = owner.transform.Find("Audio_Sfx")
                    .GetComponent<AudioSource>();
                AudioSource ui = owner.transform.Find("Audio_UiSfx")
                    .GetComponent<AudioSource>();
                sfx.pitch = 1.5f;
                ui.pitch = 0.5f;

                output.PlaySfx(clip);
                output.PlayUiSfx(clip);

                Assert.That(sfx.pitch, Is.EqualTo(1f));
                Assert.That(ui.pitch, Is.EqualTo(1f));
            }
            finally
            {
                Object.DestroyImmediate(clip);
                Object.DestroyImmediate(owner);
            }
        }

        private static void AssertCueClip(
            GameAudioConfiguration configuration,
            SfxCue cue,
            string expectedName)
        {
            Assert.That(
                configuration.TryGetSfxClip(cue, out AudioClip clip),
                Is.True,
                cue.ToString());
            Assert.That(clip, Is.Not.Null, cue.ToString());
            Assert.That(clip.name, Is.EqualTo(expectedName), cue.ToString());
        }
    }
}
