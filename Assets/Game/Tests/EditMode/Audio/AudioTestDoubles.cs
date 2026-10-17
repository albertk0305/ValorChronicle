using System;
using System.Collections.Generic;
using UnityEngine;
using ValorChronicle.Audio.Core;

namespace ValorChronicle.Tests.EditMode.Audio
{
    internal sealed class RecordingAudioOutput :
        IAudioVolumeOutput,
        IBgmPlaybackBackend,
        IAudioEffectOutput
    {
        public readonly List<(AudioVolumeChannel Channel, float Decibels)>
            Volumes = new List<(AudioVolumeChannel, float)>();
        public readonly List<string> Operations = new List<string>();

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
        public BgmTrackKind LastSpecialTrack { get; private set; }

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
        public void PlaySfx(AudioClip clip) => SfxCount++;
        public void PlayUiSfx(AudioClip clip) => UiSfxCount++;
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
}
