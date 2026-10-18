using System;

namespace ValorChronicle.Audio.Core
{
    public sealed class BgmPlaybackCoordinator
    {
        private enum PlaybackState
        {
            Stopped,
            Playing,
            Paused
        }

        private readonly IBgmPlaybackBackend backend;
        private readonly IBgmFadePlaybackBackend fadeBackend;
        private readonly BgmFadeSettings fadeSettings;
        private PlaybackState normalState;
        private PlaybackState specialState;
        private BgmTrackKind specialTrack;
        private bool battleResultFadeRequested;

        public BgmPlaybackCoordinator(
            IBgmPlaybackBackend backend,
            BgmFadeSettings fadeSettings = null)
        {
            this.backend = backend
                ?? throw new ArgumentNullException(nameof(backend));
            fadeBackend = backend as IBgmFadePlaybackBackend;
            this.fadeSettings = fadeSettings ?? new BgmFadeSettings();
        }

        public SceneAudioContext CurrentContext { get; private set; }
        public BgmTrackKind CurrentTrack => CurrentContext switch
        {
            SceneAudioContext.Init => BgmTrackKind.Init,
            SceneAudioContext.Normal => BgmTrackKind.Normal,
            SceneAudioContext.Battle => BgmTrackKind.Battle,
            _ => BgmTrackKind.None
        };

        public void ApplyScene(SceneAudioContext context)
        {
            if (context == SceneAudioContext.Unknown
                || context == CurrentContext)
            {
                return;
            }

            switch (context)
            {
                case SceneAudioContext.Init:
                    EnterInit();
                    break;
                case SceneAudioContext.Normal:
                    EnterNormal();
                    break;
                case SceneAudioContext.Battle:
                    EnterBattle();
                    break;
            }

            if (context != SceneAudioContext.Battle)
                battleResultFadeRequested = false;
        }

        public void Play(BgmTrackKind trackKind)
        {
            switch (trackKind)
            {
                case BgmTrackKind.None:
                    StopAll();
                    break;
                case BgmTrackKind.Init:
                    ApplyScene(SceneAudioContext.Init);
                    break;
                case BgmTrackKind.Normal:
                    ApplyScene(SceneAudioContext.Normal);
                    break;
                case BgmTrackKind.Battle:
                    ApplyScene(SceneAudioContext.Battle);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(trackKind));
            }
        }

        public bool StartInitialInit()
        {
            if (CurrentContext != SceneAudioContext.Unknown)
                return false;

            if (fadeBackend != null)
            {
                fadeBackend.PlayInitialInit(
                    fadeSettings.InitLoop.FadeInSeconds);
                specialTrack = BgmTrackKind.Init;
                specialState = PlaybackState.Playing;
                CurrentContext = SceneAudioContext.Init;
                return true;
            }

            PlaySpecialIfNeeded(BgmTrackKind.Init);
            CurrentContext = SceneAudioContext.Init;
            return true;
        }

        public void StopAll()
        {
            if (fadeBackend != null)
            {
                fadeBackend.StopAllImmediate();
            }
            else
            {
                if (normalState != PlaybackState.Stopped)
                    backend.StopNormal();
                if (specialState != PlaybackState.Stopped)
                    backend.StopSpecial();
            }

            normalState = PlaybackState.Stopped;
            specialState = PlaybackState.Stopped;
            specialTrack = BgmTrackKind.None;
            CurrentContext = SceneAudioContext.Unknown;
            battleResultFadeRequested = false;
        }

        public bool FadeOutBattleResult()
        {
            if (CurrentContext != SceneAudioContext.Battle
                || specialState == PlaybackState.Stopped
                || battleResultFadeRequested)
            {
                return false;
            }

            battleResultFadeRequested = true;
            if (fadeBackend != null)
            {
                fadeBackend.FadeOutSpecialAndStop(
                    fadeSettings.ResultFadeOutSeconds);
            }
            else
            {
                backend.StopSpecial();
            }

            specialState = PlaybackState.Stopped;
            return true;
        }

        public void Advance(float unscaledDeltaTime)
        {
            if (unscaledDeltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(unscaledDeltaTime));

            fadeBackend?.Advance(unscaledDeltaTime);
        }

        public void PauseCurrent()
        {
            if (CurrentContext == SceneAudioContext.Normal
                && normalState == PlaybackState.Playing)
            {
                backend.PauseNormal();
                normalState = PlaybackState.Paused;
            }
            else if ((CurrentContext == SceneAudioContext.Init
                    || CurrentContext == SceneAudioContext.Battle)
                && specialState == PlaybackState.Playing)
            {
                backend.PauseSpecial();
                specialState = PlaybackState.Paused;
            }
        }

        public void ResumeCurrent()
        {
            if (CurrentContext == SceneAudioContext.Normal
                && normalState == PlaybackState.Paused)
            {
                backend.ResumeNormal();
                normalState = PlaybackState.Playing;
            }
            else if ((CurrentContext == SceneAudioContext.Init
                    || CurrentContext == SceneAudioContext.Battle)
                && specialState == PlaybackState.Paused)
            {
                backend.ResumeSpecial();
                specialState = PlaybackState.Playing;
            }
        }

        private void EnterInit()
        {
            if (fadeBackend != null
                && CurrentContext != SceneAudioContext.Unknown)
            {
                fadeBackend.CrossfadeToSpecial(
                    BgmTrackKind.Init,
                    preserveNormalPosition: false,
                    fadeSettings.SceneCrossfadeSeconds);
                normalState = PlaybackState.Stopped;
                specialTrack = BgmTrackKind.Init;
                specialState = PlaybackState.Playing;
                CurrentContext = SceneAudioContext.Init;
                return;
            }

            if (normalState != PlaybackState.Stopped)
            {
                backend.StopNormal();
                normalState = PlaybackState.Stopped;
            }

            PlaySpecialIfNeeded(BgmTrackKind.Init);
            CurrentContext = SceneAudioContext.Init;
        }

        private void EnterNormal()
        {
            if (fadeBackend != null
                && CurrentContext != SceneAudioContext.Unknown)
            {
                fadeBackend.CrossfadeToNormal(
                    fadeSettings.SceneCrossfadeSeconds);
                normalState = PlaybackState.Playing;
                specialState = PlaybackState.Stopped;
                specialTrack = BgmTrackKind.None;
                CurrentContext = SceneAudioContext.Normal;
                return;
            }

            StopSpecialIfNeeded();
            if (normalState == PlaybackState.Paused)
            {
                backend.ResumeNormal();
            }
            else if (normalState == PlaybackState.Stopped)
            {
                backend.PlayNormalFromStart();
            }

            normalState = PlaybackState.Playing;
            CurrentContext = SceneAudioContext.Normal;
        }

        private void EnterBattle()
        {
            battleResultFadeRequested = false;
            if (fadeBackend != null
                && CurrentContext != SceneAudioContext.Unknown)
            {
                bool preserveNormal = normalState != PlaybackState.Stopped;
                fadeBackend.CrossfadeToSpecial(
                    BgmTrackKind.Battle,
                    preserveNormal,
                    fadeSettings.SceneCrossfadeSeconds);
                if (preserveNormal)
                    normalState = PlaybackState.Paused;
                specialTrack = BgmTrackKind.Battle;
                specialState = PlaybackState.Playing;
                CurrentContext = SceneAudioContext.Battle;
                return;
            }

            if (normalState == PlaybackState.Playing)
            {
                backend.PauseNormal();
                normalState = PlaybackState.Paused;
            }

            PlaySpecialIfNeeded(BgmTrackKind.Battle);
            CurrentContext = SceneAudioContext.Battle;
        }

        private void PlaySpecialIfNeeded(BgmTrackKind trackKind)
        {
            if (specialState != PlaybackState.Stopped
                && specialTrack == trackKind)
            {
                if (specialState == PlaybackState.Paused)
                    backend.ResumeSpecial();
            }
            else
            {
                backend.PlaySpecial(trackKind);
            }

            specialTrack = trackKind;
            specialState = PlaybackState.Playing;
        }

        private void StopSpecialIfNeeded()
        {
            if (specialState == PlaybackState.Stopped)
                return;

            backend.StopSpecial();
            specialState = PlaybackState.Stopped;
            specialTrack = BgmTrackKind.None;
        }
    }
}
