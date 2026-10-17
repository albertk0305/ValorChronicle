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
        private PlaybackState normalState;
        private PlaybackState specialState;
        private BgmTrackKind specialTrack;

        public BgmPlaybackCoordinator(IBgmPlaybackBackend backend)
        {
            this.backend = backend
                ?? throw new ArgumentNullException(nameof(backend));
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

        public void StopAll()
        {
            if (normalState != PlaybackState.Stopped)
                backend.StopNormal();
            if (specialState != PlaybackState.Stopped)
                backend.StopSpecial();

            normalState = PlaybackState.Stopped;
            specialState = PlaybackState.Stopped;
            specialTrack = BgmTrackKind.None;
            CurrentContext = SceneAudioContext.Unknown;
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
