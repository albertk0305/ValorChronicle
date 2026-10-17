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

        public AudioService(
            SettingsService settingsService,
            IAudioVolumeOutput volumeOutput,
            IBgmPlaybackBackend bgmPlayback,
            IAudioEffectOutput effectOutput,
            ISceneChangeSource sceneChangeSource,
            SceneBgmPolicy sceneBgmPolicy = null)
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
                bgmPlayback ?? throw new ArgumentNullException(nameof(bgmPlayback)));
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
            HandleSceneChanged(sceneChangeSource.CurrentSceneName);
            return true;
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
            IsInitialized = false;
        }

        private void HandleSceneChanged(string sceneName)
        {
            bgmCoordinator.ApplyScene(sceneBgmPolicy.Resolve(sceneName));
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
