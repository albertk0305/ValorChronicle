using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ValorChronicle.Audio.Core;
using ValorChronicle.Audio.Runtime;
using ValorChronicle.Settings.Repository;
using ValorChronicle.Settings.Serialization;
using ValorChronicle.Settings.Services;
using ValorChronicle.Settings.Validation;

namespace ValorChronicle.Tests.EditMode.Audio
{
    public sealed class AudioServiceTests
    {
        [Test]
        public void Initialize_AppliesCurrentSceneAndPreventsDuplicateSubscription()
        {
            SettingsService settings = SettingsService.CreateInMemoryDefaults();
            var output = new RecordingAudioOutput();
            var scenes = new FakeSceneChangeSource("Main");
            var service = CreateService(settings, output, scenes);

            Assert.That(service.Initialize(), Is.True);
            Assert.That(service.Initialize(), Is.False);

            Assert.That(scenes.SubscriberCount, Is.EqualTo(1));
            Assert.That(service.CurrentBgm, Is.EqualTo(BgmTrackKind.Normal));
            Assert.That(output.PlayNormalCount, Is.EqualTo(1));
            Assert.That(output.Volumes.Count, Is.EqualTo(3));
        }

        [Test]
        public void Initialize_AppliesStoredVolumesBeforeFirstBgmWithoutSettingsEvent()
        {
            string root = Path.Combine(
                Path.GetTempPath(),
                "ValorChronicleAudioTests",
                Guid.NewGuid().ToString("N"));
            try
            {
                SettingsService writer = CreatePersistentSettings(root);
                writer.LoadOrCreate();
                writer.SetMasterVolume(10);
                writer.SetBgmVolume(35);
                writer.SetSfxVolume(0);
                Assert.That(writer.Flush().IsSuccess, Is.True);

                SettingsService loaded = CreatePersistentSettings(root);
                Assert.That(
                    loaded.LoadOrCreate().Status,
                    Is.EqualTo(SettingsLoadStatus.LoadedMain));
                var output = new RecordingAudioOutput();
                var service = CreateService(
                    loaded,
                    output,
                    new FakeSceneChangeSource("Init"));

                service.Initialize();

                Assert.That(output.Volumes.Count, Is.EqualTo(3));
                Assert.That(
                    output.Volumes[0].Decibels,
                    Is.EqualTo(AudioVolumeMath.PercentageToDecibels(10))
                        .Within(0.0001f));
                Assert.That(
                    output.Volumes[1].Decibels,
                    Is.EqualTo(AudioVolumeMath.PercentageToDecibels(35))
                        .Within(0.0001f));
                Assert.That(output.Volumes[2].Decibels, Is.EqualTo(-80f));
                Assert.That(
                    output.Operations,
                    Is.EqualTo(new[]
                    {
                        "Volume:Master",
                        "Volume:Bgm",
                        "Volume:Sfx",
                        "Play:Init"
                    }));
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }

        [Test]
        public void SceneEvents_ApplyPolicyAndDisposeUnsubscribes()
        {
            SettingsService settings = SettingsService.CreateInMemoryDefaults();
            var output = new RecordingAudioOutput();
            var scenes = new FakeSceneChangeSource("Main");
            var service = CreateService(settings, output, scenes);
            service.Initialize();

            scenes.ChangeTo("Character");
            scenes.ChangeTo("Battle");
            scenes.ChangeTo("Main");
            service.Dispose();

            Assert.That(output.PlayNormalCount, Is.EqualTo(1));
            Assert.That(output.PauseNormalCount, Is.EqualTo(1));
            Assert.That(output.ResumeNormalCount, Is.EqualTo(1));
            Assert.That(scenes.SubscriberCount, Is.Zero);

            int volumeCount = output.Volumes.Count;
            settings.SetMasterVolume(20);
            Assert.That(output.Volumes.Count, Is.EqualTo(volumeCount));
        }

        [Test]
        public void SettingsEvents_UpdateOnlyRelevantVolume()
        {
            SettingsService settings = SettingsService.CreateInMemoryDefaults();
            var output = new RecordingAudioOutput();
            var service = CreateService(
                settings,
                output,
                new FakeSceneChangeSource("Init"));
            service.Initialize();
            output.Volumes.Clear();

            settings.SetSfxVolume(35);
            settings.SetBattleSpeed(2);

            Assert.That(output.Volumes.Count, Is.EqualTo(1));
            Assert.That(output.Volumes[0].Channel,
                Is.EqualTo(AudioVolumeChannel.Sfx));
        }

        [Test]
        public void NullEffects_AreIgnoredAndChannelsRemainSeparate()
        {
            SettingsService settings = SettingsService.CreateInMemoryDefaults();
            var output = new RecordingAudioOutput();
            var service = CreateService(
                settings,
                output,
                new FakeSceneChangeSource("Init"));
            service.Initialize();

            service.PlaySfx(null);
            service.PlayUiSfx(null);

            Assert.That(output.SfxCount, Is.Zero);
            Assert.That(output.UiSfxCount, Is.Zero);
        }

        [Test]
        public void SfxAndUiSfx_UseTheirDedicatedPlaybackRoutes()
        {
            SettingsService settings = SettingsService.CreateInMemoryDefaults();
            var output = new RecordingAudioOutput();
            var service = CreateService(
                settings,
                output,
                new FakeSceneChangeSource("Init"));
            service.Initialize();
            AudioClip clip = AudioClip.Create("test", 64, 1, 44100, false);
            try
            {
                service.PlaySfx(clip);
                service.PlayUiSfx(clip);

                Assert.That(output.SfxCount, Is.EqualTo(1));
                Assert.That(output.UiSfxCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void CorruptSettingsRecovery_StillAllowsAudioInitialization()
        {
            string root = Path.Combine(
                Path.GetTempPath(),
                "ValorChronicleAudioTests",
                Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(root);
                var paths = new SettingsPaths(root);
                File.WriteAllText(paths.MainPath, "broken-main");
                File.WriteAllText(paths.BackupPath, "broken-backup");
                var settings = new SettingsService(
                    new SettingsRepository(paths),
                    new NewtonsoftJsonSettingsSerializer(),
                    new SettingsValidator());
                settings.LoadOrCreate();
                var output = new RecordingAudioOutput();
                var service = CreateService(
                    settings,
                    output,
                    new FakeSceneChangeSource("Main"));

                Assert.DoesNotThrow(() => service.Initialize());
                Assert.That(output.Volumes.Count, Is.EqualTo(3));
                Assert.That(service.CurrentBgm, Is.EqualTo(BgmTrackKind.Normal));
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }

        [Test]
        public void AudioRuntime_DuplicateInitializeReusesServiceAndSources()
        {
            var owner = new GameObject("AudioRuntimeTest");
            try
            {
                LogAssert.Expect(
                    LogType.Warning,
                    "[Audio] AudioConfiguration is not assigned. "
                        + "Audio remains available without clips or mixer routing.");
                AudioRuntime runtime = owner.AddComponent<AudioRuntime>();
                SettingsService settings = SettingsService.CreateInMemoryDefaults();

                AudioService first = runtime.Initialize(settings, null);
                AudioService second = runtime.Initialize(settings, null);

                Assert.That(second, Is.SameAs(first));
                Assert.That(
                    owner.GetComponentsInChildren<AudioSource>().Length,
                    Is.EqualTo(4));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        private static AudioService CreateService(
            SettingsService settings,
            RecordingAudioOutput output,
            ISceneChangeSource scenes)
        {
            return new AudioService(
                settings,
                output,
                output,
                output,
                scenes);
        }

        private static SettingsService CreatePersistentSettings(string root)
        {
            return new SettingsService(
                new SettingsRepository(new SettingsPaths(root)),
                new NewtonsoftJsonSettingsSerializer(),
                new SettingsValidator());
        }
    }
}
