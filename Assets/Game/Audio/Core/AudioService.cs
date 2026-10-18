using System;
using UnityEngine;
using ValorChronicle.Settings.Services;

namespace ValorChronicle.Audio.Core
{
    public sealed class AudioService : IDisposable
    {
        private readonly SettingsService settingsService;
        private readonly IAudioVolumeOutput volumeOutput;
        private readonly IAudioEffectOutput effectOutput;
        private readonly ISceneChangeSource sceneChangeSource;
        private readonly SceneBgmPolicy sceneBgmPolicy;
        private readonly BgmPlaybackCoordinator bgmCoordinator;
        private AudioSettingsVolumeBinding volumeBinding;
        private bool isInitialInitPlaybackPending;

        public AudioService(
            SettingsService settingsService,
            IAudioVolumeOutput volumeOutput,
            IBgmPlaybackBackend bgmPlayback,
            IAudioEffectOutput effectOutput,
            ISceneChangeSource sceneChangeSource,
            SceneBgmPolicy sceneBgmPolicy = null,
            BgmFadeSettings bgmFadeSettings = null)
        {
            this.settingsService = settingsService
                ?? throw new ArgumentNullException(nameof(settingsService));
            this.volumeOutput = volumeOutput
                ?? throw new ArgumentNullException(nameof(volumeOutput));
            this.effectOutput = effectOutput
                ?? throw new ArgumentNullException(nameof(effectOutput));
            this.sceneChangeSource = sceneChangeSource
                ?? throw new ArgumentNullException(nameof(sceneChangeSource));
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

        public void PlayUiSfx(AudioClip clip)
        {
            RequireInitialized();
            if (clip != null)
                effectOutput.PlayUiSfx(clip);
        }

        public void Dispose()
        {
            if (!IsInitialized)
                return;

            sceneChangeSource.Unsubscribe(HandleSceneChanged);
            volumeBinding?.Dispose();
            volumeBinding = null;
            bgmCoordinator.StopAll();
            isInitialInitPlaybackPending = false;
            IsInitialized = false;
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
