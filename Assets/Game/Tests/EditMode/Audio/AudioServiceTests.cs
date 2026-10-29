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
                Assert.That(output.PlaySpecialCount, Is.Zero);
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
                        "Volume:Sfx"
                    }));

                Assert.That(service.NotifyInitialInitReady(), Is.True);
                Assert.That(output.Operations[^1], Is.EqualTo("Play:Init"));
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }

        [Test]
        public void InitialInit_WaitsForReadyAndIgnoresDuplicateReady()
        {
            SettingsService settings = SettingsService.CreateInMemoryDefaults();
            var output = new RecordingFadingAudioOutput();
            var scenes = new FakeSceneChangeSource("Init");
            var service = CreateService(settings, output, scenes);

            service.Initialize();
            service.Advance(4f);

            Assert.That(service.CurrentBgm, Is.EqualTo(BgmTrackKind.None));
            Assert.That(output.PlaySpecialCount, Is.Zero);
            Assert.That(service.NotifyInitialInitReady(), Is.True);
            Assert.That(service.NotifyInitialInitReady(), Is.False);
            Assert.That(service.CurrentBgm, Is.EqualTo(BgmTrackKind.Init));
            Assert.That(output.PlaySpecialCount, Is.Zero);
            Assert.That(output.InitialInitCount, Is.EqualTo(1));
            Assert.That(output.LastSpecialTrack, Is.EqualTo(BgmTrackKind.Init));
            Assert.That(output.LastDuration, Is.EqualTo(0.6f).Within(0.0001f));
        }

        [Test]
        public void InitialInit_SceneChangeCancelsPendingReadyAndReentryUsesPolicy()
        {
            SettingsService settings = SettingsService.CreateInMemoryDefaults();
            var output = new RecordingFadingAudioOutput();
            var scenes = new FakeSceneChangeSource("Init");
            var service = CreateService(settings, output, scenes);

            service.Initialize();
            scenes.ChangeTo("Main");

            Assert.That(output.PlaySpecialCount, Is.Zero);
            Assert.That(output.PlayNormalCount, Is.EqualTo(1));
            Assert.That(service.NotifyInitialInitReady(), Is.False);

            scenes.ChangeTo("Init");

            Assert.That(output.CrossfadeToSpecialCount, Is.EqualTo(1));
            Assert.That(output.LastSpecialTrack, Is.EqualTo(BgmTrackKind.Init));
            Assert.That(output.LastDuration, Is.EqualTo(0.8f).Within(0.0001f));
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
        public void FadingBackend_UsesSceneGroupsResultFadeAndUnscaledAdvance()
        {
            SettingsService settings = SettingsService.CreateInMemoryDefaults();
            var output = new RecordingFadingAudioOutput();
            var scenes = new FakeSceneChangeSource("Main");
            var service = CreateService(settings, output, scenes);
            service.Initialize();

            scenes.ChangeTo("Character");
            Assert.That(output.CrossfadeToNormalCount, Is.Zero);

            scenes.ChangeTo("Battle");
            Assert.That(output.CrossfadeToSpecialCount, Is.EqualTo(1));
            Assert.That(output.LastPreserveNormal, Is.True);
            Assert.That(service.FadeOutBattleResultBgm(), Is.True);
            Assert.That(service.FadeOutBattleResultBgm(), Is.False);
            service.Advance(0.4f);

            Assert.That(output.ResultFadeCount, Is.EqualTo(1));
            Assert.That(output.AdvancedSeconds,
                Is.EqualTo(0.4f).Within(0.0001f));

            scenes.ChangeTo("Main");
            Assert.That(output.CrossfadeToNormalCount, Is.EqualTo(1));
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
        public void CueSfx_SelectsConfiguredClipsAndDedicatedRoutes()
        {
            var output = new RecordingAudioOutput();
            var clips = new FakeSfxClipProvider();
            var clock = new FakeUnscaledTimeSource();
            AudioClip button = AudioClip.Create(
                "button",
                64,
                1,
                44100,
                false);
            AudioClip pop = AudioClip.Create("pop", 64, 1, 44100, false);
            AudioClip hit = AudioClip.Create("hit", 64, 1, 44100, false);
            try
            {
                clips.SetClip(SfxCue.ButtonPress, button);
                clips.SetClip(SfxCue.BlockPop, pop);
                clips.SetClip(SfxCue.Hit, hit);
                AudioService service = CreateService(
                    SettingsService.CreateInMemoryDefaults(),
                    output,
                    new FakeSceneChangeSource("Init"),
                    clips,
                    clock);
                service.Initialize();

                Assert.That(service.PlayUiSfx(SfxCue.ButtonPress), Is.True);
                Assert.That(service.PlaySfx(SfxCue.BlockPop), Is.True);
                Assert.That(service.PlaySfx(SfxCue.Hit), Is.True);

                Assert.That(output.UiSfxClips, Is.EqualTo(new[] { button }));
                Assert.That(output.SfxClips, Is.EqualTo(new[] { pop, hit }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(button);
                UnityEngine.Object.DestroyImmediate(pop);
                UnityEngine.Object.DestroyImmediate(hit);
            }
        }

        [TestCase(
            SfxCue.ButtonPress,
            SfxCuePlaybackPolicy.ButtonPressMinimumIntervalSeconds)]
        [TestCase(
            SfxCue.BlockPop,
            SfxCuePlaybackPolicy.BlockPopMinimumIntervalSeconds)]
        [TestCase(
            SfxCue.Hit,
            SfxCuePlaybackPolicy.HitMinimumIntervalSeconds)]
        public void CueSfx_BlocksWithinIntervalAndAllowsAtBoundary(
            SfxCue cue,
            double interval)
        {
            var output = new RecordingAudioOutput();
            var clips = new FakeSfxClipProvider();
            var clock = new FakeUnscaledTimeSource();
            AudioClip clip = AudioClip.Create(
                cue.ToString(),
                64,
                1,
                44100,
                false);
            try
            {
                clips.SetClip(cue, clip);
                AudioService service = CreateService(
                    SettingsService.CreateInMemoryDefaults(),
                    output,
                    new FakeSceneChangeSource("Init"),
                    clips,
                    clock);
                service.Initialize();

                Assert.That(service.PlaySfx(cue), Is.True);
                clock.Advance(interval - 0.001d);
                Assert.That(service.PlaySfx(cue), Is.False);
                clock.CurrentTime = interval;
                Assert.That(service.PlaySfx(cue), Is.True);
                Assert.That(output.SfxCount, Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void CueSfx_UsesInjectedUnscaledTimeInsteadOfTimeScale()
        {
            var output = new RecordingAudioOutput();
            var clips = new FakeSfxClipProvider();
            var clock = new FakeUnscaledTimeSource();
            AudioClip clip = AudioClip.Create("hit", 64, 1, 44100, false);
            float originalTimeScale = Time.timeScale;
            try
            {
                clips.SetClip(SfxCue.Hit, clip);
                AudioService service = CreateService(
                    SettingsService.CreateInMemoryDefaults(),
                    output,
                    new FakeSceneChangeSource("Init"),
                    clips,
                    clock);
                service.Initialize();

                Time.timeScale = 0.5f;
                Assert.That(service.PlaySfx(SfxCue.Hit), Is.True);
                Time.timeScale = 1.5f;
                Assert.That(service.PlaySfx(SfxCue.Hit), Is.False);
                clock.Advance(
                    SfxCuePlaybackPolicy.HitMinimumIntervalSeconds);
                Assert.That(service.PlaySfx(SfxCue.Hit), Is.True);
            }
            finally
            {
                Time.timeScale = originalTimeScale;
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void CueSfx_MissingClipDoesNotConsumeCooldown()
        {
            var output = new RecordingAudioOutput();
            var clips = new FakeSfxClipProvider();
            var clock = new FakeUnscaledTimeSource();
            AudioClip clip = AudioClip.Create("pop", 64, 1, 44100, false);
            try
            {
                AudioService service = CreateService(
                    SettingsService.CreateInMemoryDefaults(),
                    output,
                    new FakeSceneChangeSource("Init"),
                    clips,
                    clock);
                service.Initialize();

                Assert.That(service.PlaySfx(SfxCue.BlockPop), Is.False);
                clips.SetClip(SfxCue.BlockPop, clip);
                Assert.That(service.PlaySfx(SfxCue.BlockPop), Is.True);
                Assert.That(output.SfxClips, Is.EqualTo(new[] { clip }));
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
            ISceneChangeSource scenes,
            ISfxClipProvider sfxClipProvider = null,
            IUnscaledTimeSource unscaledTimeSource = null)
        {
            return new AudioService(
                settings,
                output,
                output,
                output,
                scenes,
                sfxClipProvider: sfxClipProvider,
                unscaledTimeSource: unscaledTimeSource);
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
