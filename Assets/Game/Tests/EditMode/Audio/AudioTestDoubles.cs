using System;
using System.Collections.Generic;
using UnityEngine;
using ValorChronicle.Audio.Core;

namespace ValorChronicle.Tests.EditMode.Audio
{
    internal class RecordingAudioOutput :
        IAudioVolumeOutput,
        IBgmPlaybackBackend,
        IAudioEffectOutput
    {
        public readonly List<(AudioVolumeChannel Channel, float Decibels)>
            Volumes = new List<(AudioVolumeChannel, float)>();
        public readonly List<string> Operations = new List<string>();
        public readonly List<AudioClip> SfxClips = new List<AudioClip>();
        public readonly List<AudioClip> UiSfxClips = new List<AudioClip>();

        public int PlayNormalCount { get; private set; }
        public int PauseNormalCount { get; private set; }
        public int ResumeNormalCount { get; private set; }
        public int StopNormalCount { get; private set; }
        public int PlaySpecialCount { get; private set; }
        public int PauseSpecialCount { get; private set; }
        public int ResumeSpecialCount { get; private set; }
        public int StopSpecialCount { get; private set; }
        public int SfxCount { get; private set; }
        public int UiSfxCount { get; private set; }
        public BgmTrackKind LastSpecialTrack { get; protected set; }

        public void ApplyVolume(AudioVolumeChannel channel, float decibels)
        {
            Volumes.Add((channel, decibels));
            Operations.Add($"Volume:{channel}");
        }

        public void PlayNormalFromStart()
        {
            PlayNormalCount++;
            Operations.Add("Play:Normal");
        }
        public void PauseNormal() => PauseNormalCount++;
        public void ResumeNormal() => ResumeNormalCount++;
        public void StopNormal() => StopNormalCount++;

        public void PlaySpecial(BgmTrackKind trackKind)
        {
            PlaySpecialCount++;
            LastSpecialTrack = trackKind;
            Operations.Add($"Play:{trackKind}");
        }

        public void PauseSpecial() => PauseSpecialCount++;
        public void ResumeSpecial() => ResumeSpecialCount++;
        public void StopSpecial() => StopSpecialCount++;
        public void PlaySfx(AudioClip clip)
        {
            SfxCount++;
            SfxClips.Add(clip);
        }

        public void PlayUiSfx(AudioClip clip)
        {
            UiSfxCount++;
            UiSfxClips.Add(clip);
        }
    }

    internal sealed class RecordingFadingAudioOutput :
        RecordingAudioOutput,
        IBgmFadePlaybackBackend
    {
        public int InitialInitCount { get; private set; }
        public int CrossfadeToNormalCount { get; private set; }
        public int CrossfadeToSpecialCount { get; private set; }
        public int ResultFadeCount { get; private set; }
        public int StopAllImmediateCount { get; private set; }
        public float AdvancedSeconds { get; private set; }
        public float LastDuration { get; private set; }
        public bool LastPreserveNormal { get; private set; }

        public void PlayInitialInit(float durationSeconds)
        {
            InitialInitCount++;
            LastSpecialTrack = BgmTrackKind.Init;
            LastDuration = durationSeconds;
            Operations.Add("PlayInitial:Init");
        }

        public void CrossfadeToNormal(float durationSeconds)
        {
            CrossfadeToNormalCount++;
            LastDuration = durationSeconds;
            Operations.Add("Crossfade:Normal");
        }

        public void CrossfadeToSpecial(
            BgmTrackKind trackKind,
            bool preserveNormalPosition,
            float durationSeconds)
        {
            CrossfadeToSpecialCount++;
            LastSpecialTrack = trackKind;
            LastPreserveNormal = preserveNormalPosition;
            LastDuration = durationSeconds;
            Operations.Add($"Crossfade:{trackKind}");
        }

        public void FadeOutSpecialAndStop(float durationSeconds)
        {
            ResultFadeCount++;
            LastDuration = durationSeconds;
            Operations.Add("FadeOut:Result");
        }

        public void StopAllImmediate()
        {
            StopAllImmediateCount++;
            Operations.Add("Stop:AllImmediate");
        }

        public void Advance(float unscaledDeltaTime)
        {
            AdvancedSeconds += unscaledDeltaTime;
        }
    }

    internal sealed class FakeSceneChangeSource : ISceneChangeSource
    {
        private Action<string> handler;

        public FakeSceneChangeSource(string currentSceneName)
        {
            CurrentSceneName = currentSceneName;
        }

        public string CurrentSceneName { get; private set; }
        public int SubscriberCount => handler?.GetInvocationList().Length ?? 0;

        public void Subscribe(Action<string> callback)
        {
            handler += callback;
        }

        public void Unsubscribe(Action<string> callback)
        {
            handler -= callback;
        }

        public void ChangeTo(string sceneName)
        {
            CurrentSceneName = sceneName;
            handler?.Invoke(sceneName);
        }
    }

    internal sealed class FakeSfxClipProvider : ISfxClipProvider
    {
        private readonly Dictionary<SfxCue, AudioClip> clips =
            new Dictionary<SfxCue, AudioClip>();

        public void SetClip(SfxCue cue, AudioClip clip)
        {
            clips[cue] = clip;
        }

        public bool TryGetSfxClip(SfxCue cue, out AudioClip clip)
        {
            return clips.TryGetValue(cue, out clip) && clip != null;
        }
    }

    internal sealed class FakeUnscaledTimeSource : IUnscaledTimeSource
    {
        public double CurrentTime { get; set; }

        public void Advance(double seconds)
        {
            CurrentTime += seconds;
        }
    }
}
