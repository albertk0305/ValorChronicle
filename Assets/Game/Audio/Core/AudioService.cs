using System;
using System.Collections.Generic;
using UnityEngine;
using ValorChronicle.Settings.Services;

namespace ValorChronicle.Audio.Core
{
    public enum SfxCue
    {
        ButtonPress,
        BlockPop,
        Hit
    }

    public static class SfxCuePlaybackPolicy
    {
        public const double ButtonPressMinimumIntervalSeconds = 0.05d;
        public const double BlockPopMinimumIntervalSeconds = 0.06d;
        public const double HitMinimumIntervalSeconds = 0.08d;

        public static bool TryGetMinimumIntervalSeconds(
            SfxCue cue,
            out double intervalSeconds)
        {
            switch (cue)
            {
                case SfxCue.ButtonPress:
                    intervalSeconds = ButtonPressMinimumIntervalSeconds;
                    return true;
                case SfxCue.BlockPop:
                    intervalSeconds = BlockPopMinimumIntervalSeconds;
                    return true;
                case SfxCue.Hit:
                    intervalSeconds = HitMinimumIntervalSeconds;
                    return true;
                default:
                    intervalSeconds = 0d;
                    return false;
            }
        }
    }

    public interface ISfxClipProvider
    {
        bool TryGetSfxClip(SfxCue cue, out AudioClip clip);
    }

    public interface IUnscaledTimeSource
    {
        double CurrentTime { get; }
    }

    public sealed class UnityUnscaledTimeSource : IUnscaledTimeSource
    {
        public double CurrentTime => Time.unscaledTimeAsDouble;
    }

    public sealed class AudioService : IDisposable
    {
        private readonly SettingsService settingsService;
        private readonly IAudioVolumeOutput volumeOutput;
        private readonly IAudioEffectOutput effectOutput;
        private readonly ISceneChangeSource sceneChangeSource;
        private readonly ISfxClipProvider sfxClipProvider;
        private readonly IUnscaledTimeSource unscaledTimeSource;
        private readonly SceneBgmPolicy sceneBgmPolicy;
        private readonly BgmPlaybackCoordinator bgmCoordinator;
        private readonly Dictionary<SfxCue, double> lastSfxPlaybackTimes =
            new Dictionary<SfxCue, double>();
        private AudioSettingsVolumeBinding volumeBinding;
        private bool isInitialInitPlaybackPending;

        public AudioService(
            SettingsService settingsService,
            IAudioVolumeOutput volumeOutput,
            IBgmPlaybackBackend bgmPlayback,
            IAudioEffectOutput effectOutput,
            ISceneChangeSource sceneChangeSource,
            SceneBgmPolicy sceneBgmPolicy = null,
            BgmFadeSettings bgmFadeSettings = null,
            ISfxClipProvider sfxClipProvider = null,
            IUnscaledTimeSource unscaledTimeSource = null)
        {
            this.settingsService = settingsService
                ?? throw new ArgumentNullException(nameof(settingsService));
            this.volumeOutput = volumeOutput
                ?? throw new ArgumentNullException(nameof(volumeOutput));
            this.effectOutput = effectOutput
                ?? throw new ArgumentNullException(nameof(effectOutput));
            this.sceneChangeSource = sceneChangeSource
                ?? throw new ArgumentNullException(nameof(sceneChangeSource));
            this.sfxClipProvider = sfxClipProvider;
            this.unscaledTimeSource = unscaledTimeSource
                ?? new UnityUnscaledTimeSource();
            this.sceneBgmPolicy = sceneBgmPolicy ?? new SceneBgmPolicy();
            bgmCoordinator = new BgmPlaybackCoordinator(
                bgmPlayback ?? throw new ArgumentNullException(nameof(bgmPlayback)),
                bgmFadeSettings);
        }

        public bool IsInitialized { get; private set; }
        public BgmTrackKind CurrentBgm => bgmCoordinator.CurrentTrack;

        public bool Initialize()
        {
            if (IsInitialized)
                return false;

            volumeBinding = new AudioSettingsVolumeBinding(
                settingsService,
                volumeOutput);
            sceneChangeSource.Subscribe(HandleSceneChanged);
            IsInitialized = true;
            SceneAudioContext initialContext = sceneBgmPolicy.Resolve(
                sceneChangeSource.CurrentSceneName);
            if (initialContext == SceneAudioContext.Init)
            {
                isInitialInitPlaybackPending = true;
            }
            else
            {
                bgmCoordinator.ApplyScene(initialContext);
            }
            return true;
        }

        public bool NotifyInitialInitReady()
        {
            RequireInitialized();
            if (!isInitialInitPlaybackPending)
                return false;

            isInitialInitPlaybackPending = false;
            if (sceneBgmPolicy.Resolve(sceneChangeSource.CurrentSceneName)
                != SceneAudioContext.Init)
            {
                return false;
            }

            return bgmCoordinator.StartInitialInit();
        }

        public void PlayBgm(BgmTrackKind trackKind)
        {
            RequireInitialized();
            bgmCoordinator.Play(trackKind);
        }

        public void StopBgm()
        {
            RequireInitialized();
            bgmCoordinator.StopAll();
        }

        public void PauseBgm()
        {
            RequireInitialized();
            bgmCoordinator.PauseCurrent();
        }

        public void ResumeBgm()
        {
            RequireInitialized();
            bgmCoordinator.ResumeCurrent();
        }

        public bool FadeOutBattleResultBgm()
        {
            RequireInitialized();
            return bgmCoordinator.FadeOutBattleResult();
        }

        public void Advance(float unscaledDeltaTime)
        {
            if (!IsInitialized)
                return;

            bgmCoordinator.Advance(unscaledDeltaTime);
        }

        public void PlaySfx(AudioClip clip)
        {
            RequireInitialized();
            if (clip != null)
                effectOutput.PlaySfx(clip);
        }

        public bool PlaySfx(SfxCue cue)
        {
            return TryPlaySfx(cue, effectOutput.PlaySfx);
        }

        public void PlayUiSfx(AudioClip clip)
        {
            RequireInitialized();
            if (clip != null)
                effectOutput.PlayUiSfx(clip);
        }

        public bool PlayUiSfx(SfxCue cue)
        {
            return TryPlaySfx(cue, effectOutput.PlayUiSfx);
        }

        public void Dispose()
        {
            if (!IsInitialized)
                return;

            sceneChangeSource.Unsubscribe(HandleSceneChanged);
            volumeBinding?.Dispose();
            volumeBinding = null;
            bgmCoordinator.StopAll();
            lastSfxPlaybackTimes.Clear();
            isInitialInitPlaybackPending = false;
            IsInitialized = false;
        }

        private bool TryPlaySfx(
            SfxCue cue,
            Action<AudioClip> play)
        {
            RequireInitialized();
            if (sfxClipProvider == null
                || !SfxCuePlaybackPolicy.TryGetMinimumIntervalSeconds(
                    cue,
                    out double minimumInterval)
                || !sfxClipProvider.TryGetSfxClip(cue, out AudioClip clip)
                || clip == null)
            {
                return false;
            }

            double now = unscaledTimeSource.CurrentTime;
            if (lastSfxPlaybackTimes.TryGetValue(
                    cue,
                    out double lastPlaybackTime))
            {
                double elapsed = now - lastPlaybackTime;
                if (elapsed >= 0d && elapsed < minimumInterval)
                    return false;
            }

            play(clip);
            lastSfxPlaybackTimes[cue] = now;
            return true;
        }

        private void HandleSceneChanged(string sceneName)
        {
            SceneAudioContext context = sceneBgmPolicy.Resolve(sceneName);
            if (isInitialInitPlaybackPending)
            {
                if (context == SceneAudioContext.Init)
                    return;

                isInitialInitPlaybackPending = false;
            }

            bgmCoordinator.ApplyScene(context);
        }

        private void RequireInitialized()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException(
                    "AudioService must be initialized before use.");
            }
        }
    }
}
