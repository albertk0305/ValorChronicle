using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using ValorChronicle.Audio.Core;
using ValorChronicle.Core.Logging;
using GameAudioConfiguration =
    ValorChronicle.Audio.Configuration.AudioConfiguration;

namespace ValorChronicle.Audio.Runtime
{
    public sealed class UnityAudioOutput :
        IAudioVolumeOutput,
        IBgmPlaybackBackend,
        IBgmFadePlaybackBackend,
        IAudioEffectOutput
    {
        public const string MasterVolumeParameter = "MasterVolumeDb";
        public const string BgmVolumeParameter = "BgmVolumeDb";
        public const string SfxVolumeParameter = "SfxVolumeDb";

        private readonly GameAudioConfiguration configuration;
        private readonly BgmFadeSettings fadeSettings;
        private readonly AudioSource normalBgmSource;
        private readonly AudioSource specialBgmSource;
        private readonly AudioSource sfxSource;
        private readonly AudioSource uiSfxSource;
        private readonly bool hasCompleteMixerRouting;
        private readonly HashSet<AudioVolumeChannel> failedMixerParameters =
            new HashSet<AudioVolumeChannel>();
        private float masterLinear = 1f;
        private float bgmLinear = 1f;
        private float sfxLinear = 1f;
        private float normalTransitionGain = 1f;
        private float specialTransitionGain = 1f;
        private BgmTrackKind specialTrack = BgmTrackKind.None;
        private readonly BgmStartupFadeClock initialInitFade =
            new BgmStartupFadeClock();
        private bool suppressInitialInitLoopFadeIn;
        private FadeTransition transition;

        private enum FadeCompletion
        {
            None,
            PauseNormal,
            StopNormal,
            StopSpecial
        }

        private struct FadeTransition
        {
            public bool IsActive;
            public float Elapsed;
            public float Duration;
            public float NormalStart;
            public float NormalTarget;
            public float SpecialStart;
            public float SpecialTarget;
            public FadeCompletion Completion;
        }

        public UnityAudioOutput(
            GameObject owner,
            GameAudioConfiguration configuration)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            this.configuration = configuration;
            fadeSettings = configuration?.CreateFadeSettings()
                ?? new BgmFadeSettings();
            normalBgmSource = CreateSource(
                owner.transform,
                "Audio_NormalBgm",
                configuration?.BgmMixerGroup,
                loop: true,
                ignoreListenerPause: true);
            specialBgmSource = CreateSource(
                owner.transform,
                "Audio_SpecialBgm",
                configuration?.BgmMixerGroup,
                loop: true,
                ignoreListenerPause: true);
            sfxSource = CreateSource(
                owner.transform,
                "Audio_Sfx",
                configuration?.SfxMixerGroup,
                loop: false,
                ignoreListenerPause: false);
            uiSfxSource = CreateSource(
                owner.transform,
                "Audio_UiSfx",
                configuration?.UiMixerGroup,
                loop: false,
                ignoreListenerPause: true);
            hasCompleteMixerRouting = configuration?.AudioMixer != null
                && configuration.BgmMixerGroup != null
                && configuration.SfxMixerGroup != null
                && configuration.UiMixerGroup != null;

            LogConfigurationWarnings(owner);
        }

        public void ApplyVolume(AudioVolumeChannel channel, float decibels)
        {
            if (hasCompleteMixerRouting)
            {
                string parameter = GetMixerParameter(channel);
                if (configuration.AudioMixer.SetFloat(parameter, decibels))
                    return;

                if (failedMixerParameters.Add(channel))
                {
                    GameLogger.Warning(
                        $"[Audio] Mixer parameter '{parameter}' is not exposed.");
                }
            }

            ApplyFallbackVolume(channel, decibels);
        }

        private void ApplyFallbackVolume(
            AudioVolumeChannel channel,
            float decibels)
        {
            float linear = AudioVolumeMath.DecibelsToLinear(decibels);
            switch (channel)
            {
                case AudioVolumeChannel.Master:
                    masterLinear = linear;
                    break;
                case AudioVolumeChannel.Bgm:
                    bgmLinear = linear;
                    break;
                case AudioVolumeChannel.Sfx:
                    sfxLinear = linear;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(channel));
            }

            RefreshSourceVolumes();
        }

        public void PlayNormalFromStart()
        {
            AudioClip clip = configuration?.NormalBgm;
            if (clip == null)
                return;

            PlayFromStart(normalBgmSource, clip);
            normalTransitionGain = 1f;
            RefreshSourceVolumes();
        }

        public void PlayInitialInit(float durationSeconds)
        {
            AudioClip clip = configuration?.InitBgm;
            if (clip == null)
                return;

            CancelTransition();
            specialBgmSource.Stop();
            specialBgmSource.clip = clip;
            specialBgmSource.time = 0f;
            specialTrack = BgmTrackKind.Init;
            initialInitFade.Begin(durationSeconds);
            suppressInitialInitLoopFadeIn = durationSeconds > 0f;
            specialTransitionGain = initialInitFade.Gain;
            RefreshSourceVolumes();
        }

        public void PauseNormal()
        {
            CancelTransition();
            if (normalBgmSource.isPlaying)
                normalBgmSource.Pause();
        }

        public void ResumeNormal()
        {
            if (normalBgmSource.clip != null)
                normalBgmSource.UnPause();
        }

        public void StopNormal()
        {
            CancelTransition();
            StopAndClear(normalBgmSource);
            normalTransitionGain = 0f;
            RefreshSourceVolumes();
        }

        public void PlaySpecial(BgmTrackKind trackKind)
        {
            AudioClip clip = trackKind switch
            {
                BgmTrackKind.Init => configuration?.InitBgm,
                BgmTrackKind.Battle => configuration?.BattleBgm,
                _ => null
            };
            if (clip == null)
                return;

            ResetInitialInitFade();
            PlayFromStart(specialBgmSource, clip);
            specialTrack = trackKind;
            specialTransitionGain = 1f;
            RefreshSourceVolumes();
        }

        public void PauseSpecial()
        {
            CancelTransition();
            if (specialBgmSource.isPlaying)
                specialBgmSource.Pause();
        }

        public void ResumeSpecial()
        {
            if (specialBgmSource.clip != null)
                specialBgmSource.UnPause();
        }

        public void StopSpecial()
        {
            CancelTransition();
            ResetInitialInitFade();
            StopAndClear(specialBgmSource);
            specialTrack = BgmTrackKind.None;
            specialTransitionGain = 0f;
            RefreshSourceVolumes();
        }

        public void CrossfadeToNormal(float durationSeconds)
        {
            CancelInitialInitFadeForOutgoingTransition();
            AudioClip normalClip = configuration?.NormalBgm;
            if (normalClip != null)
            {
                if (normalBgmSource.clip == normalClip)
                    normalBgmSource.UnPause();
                else
                {
                    PlayFromStart(normalBgmSource, normalClip);
                    normalTransitionGain = 0f;
                }
            }

            BeginTransition(
                normalTarget: 1f,
                specialTarget: 0f,
                durationSeconds,
                FadeCompletion.StopSpecial);
        }

        public void CrossfadeToSpecial(
            BgmTrackKind trackKind,
            bool preserveNormalPosition,
            float durationSeconds)
        {
            ResetInitialInitFade();
            AudioClip clip = ResolveSpecialClip(trackKind);
            if (clip != null)
            {
                if (specialBgmSource.clip == clip)
                    specialBgmSource.UnPause();
                else
                {
                    PlayFromStart(specialBgmSource, clip);
                    specialTransitionGain = 0f;
                }
            }

            specialTrack = trackKind;
            BeginTransition(
                normalTarget: 0f,
                specialTarget: 1f,
                durationSeconds,
                preserveNormalPosition
                    ? FadeCompletion.PauseNormal
                    : FadeCompletion.StopNormal);
        }

        public void FadeOutSpecialAndStop(float durationSeconds)
        {
            if (specialBgmSource.clip == null)
                return;

            BeginTransition(
                normalTransitionGain,
                specialTarget: 0f,
                durationSeconds,
                FadeCompletion.StopSpecial);
        }

        public void StopAllImmediate()
        {
            CancelTransition();
            ResetInitialInitFade();
            StopAndClear(normalBgmSource);
            StopAndClear(specialBgmSource);
            normalTransitionGain = 0f;
            specialTransitionGain = 0f;
            specialTrack = BgmTrackKind.None;
            RefreshSourceVolumes();
        }

        public void Advance(float unscaledDeltaTime)
        {
            if (unscaledDeltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(unscaledDeltaTime));

            if (AdvanceInitialInitFade(unscaledDeltaTime))
            {
                RefreshSourceVolumes();
                return;
            }

            if (transition.IsActive)
            {
                transition.Elapsed += unscaledDeltaTime;
                normalTransitionGain = BgmFadeMath.EvaluateLinear(
                    transition.NormalStart,
                    transition.NormalTarget,
                    transition.Elapsed,
                    transition.Duration);
                specialTransitionGain = BgmFadeMath.EvaluateLinear(
                    transition.SpecialStart,
                    transition.SpecialTarget,
                    transition.Elapsed,
                    transition.Duration);
                if (transition.Elapsed >= transition.Duration)
                    CompleteTransition();
            }

            RefreshSourceVolumes();
        }

        public void PlaySfx(AudioClip clip)
        {
            if (clip != null)
            {
                sfxSource.pitch = 1f;
                sfxSource.PlayOneShot(clip);
            }
        }

        public void PlayUiSfx(AudioClip clip)
        {
            if (clip != null)
            {
                uiSfxSource.pitch = 1f;
                uiSfxSource.PlayOneShot(clip);
            }
        }

        private void RefreshSourceVolumes()
        {
            float bgmBase = masterLinear * bgmLinear;
            normalBgmSource.volume = bgmBase
                * normalTransitionGain
                * GetLoopGain(normalBgmSource, BgmTrackKind.Normal);
            specialBgmSource.volume = bgmBase
                * specialTransitionGain
                * GetLoopGain(specialBgmSource, specialTrack);
            sfxSource.volume = masterLinear * sfxLinear;
            uiSfxSource.volume = masterLinear * sfxLinear;
        }

        private float GetLoopGain(AudioSource source, BgmTrackKind trackKind)
        {
            if (source.clip == null || configuration == null)
                return 1f;

            BgmLoopFadeSettings loopSettings = fadeSettings.GetLoop(trackKind);
            if (ReferenceEquals(source, specialBgmSource)
                && trackKind == BgmTrackKind.Init
                && suppressInitialInitLoopFadeIn)
            {
                if (!initialInitFade.IsFading
                    && !initialInitFade.IsWaitingForPlaybackStart
                    && source.time >= loopSettings.FadeInSeconds)
                {
                    suppressInitialInitLoopFadeIn = false;
                }
                else
                {
                    loopSettings = new BgmLoopFadeSettings(
                        loopSettings.Enabled,
                        0f,
                        loopSettings.FadeOutSeconds);
                }
            }

            return BgmFadeMath.EvaluateLoopGain(
                source.time,
                source.clip.length,
                loopSettings);
        }

        private bool AdvanceInitialInitFade(float unscaledDeltaTime)
        {
            if (initialInitFade.IsWaitingForPlaybackStart)
            {
                AudioClip clip = specialBgmSource.clip;
                if (clip == null || clip.loadState == AudioDataLoadState.Failed)
                {
                    ResetInitialInitFade();
                    return false;
                }

                if (clip.loadState == AudioDataLoadState.Unloaded)
                {
                    clip.LoadAudioData();
                    return true;
                }

                if (clip.loadState != AudioDataLoadState.Loaded)
                    return true;

                if (initialInitFade.TryStartPlayback(isReady: true))
                {
                    specialBgmSource.Stop();
                    specialBgmSource.time = 0f;
                    specialBgmSource.Play();
                    specialTransitionGain = initialInitFade.Gain;
                }

                return true;
            }

            if (!initialInitFade.IsFading)
                return false;
            if (!specialBgmSource.isPlaying)
                return true;

            initialInitFade.Advance(unscaledDeltaTime);
            specialTransitionGain = initialInitFade.Gain;
            return false;
        }

        private void CancelInitialInitFadeForOutgoingTransition()
        {
            bool wasPending = initialInitFade.IsWaitingForPlaybackStart;
            initialInitFade.Reset();
            if (wasPending)
                suppressInitialInitLoopFadeIn = false;
        }

        private void ResetInitialInitFade()
        {
            initialInitFade.Reset();
            suppressInitialInitLoopFadeIn = false;
        }

        private AudioClip ResolveSpecialClip(BgmTrackKind trackKind)
        {
            return trackKind switch
            {
                BgmTrackKind.Init => configuration?.InitBgm,
                BgmTrackKind.Battle => configuration?.BattleBgm,
                _ => null
            };
        }

        private void BeginTransition(
            float normalTarget,
            float specialTarget,
            float durationSeconds,
            FadeCompletion completion)
        {
            transition = new FadeTransition
            {
                IsActive = durationSeconds > 0f,
                Elapsed = 0f,
                Duration = Math.Max(0f, durationSeconds),
                NormalStart = normalTransitionGain,
                NormalTarget = Mathf.Clamp01(normalTarget),
                SpecialStart = specialTransitionGain,
                SpecialTarget = Mathf.Clamp01(specialTarget),
                Completion = completion
            };
            if (!transition.IsActive)
            {
                normalTransitionGain = transition.NormalTarget;
                specialTransitionGain = transition.SpecialTarget;
                CompleteTransition();
            }

            RefreshSourceVolumes();
        }

        private void CompleteTransition()
        {
            FadeCompletion completion = transition.Completion;
            transition = default;
            switch (completion)
            {
                case FadeCompletion.PauseNormal:
                    if (normalBgmSource.isPlaying)
                        normalBgmSource.Pause();
                    break;
                case FadeCompletion.StopNormal:
                    StopAndClear(normalBgmSource);
                    break;
                case FadeCompletion.StopSpecial:
                    StopAndClear(specialBgmSource);
                    ResetInitialInitFade();
                    specialTrack = BgmTrackKind.None;
                    break;
            }
        }

        private void CancelTransition()
        {
            transition = default;
        }

        private void LogConfigurationWarnings(GameObject context)
        {
            if (configuration == null)
            {
                GameLogger.Warning(
                    "[Audio] AudioConfiguration is not assigned. "
                        + "Audio remains available without clips or mixer routing.",
                    context);
                return;
            }

            if (configuration.AudioMixer == null)
            {
                GameLogger.Warning(
                    "[Audio] AudioMixer is not assigned. "
                        + "AudioSource fallback volume control will be used.",
                    context);
            }

            if (configuration.BgmMixerGroup == null
                || configuration.SfxMixerGroup == null
                || configuration.UiMixerGroup == null)
            {
                GameLogger.Warning(
                    "[Audio] One or more AudioMixerGroup references are missing.",
                    context);
            }
        }

        private static AudioSource CreateSource(
            Transform parent,
            string name,
            AudioMixerGroup mixerGroup,
            bool loop,
            bool ignoreListenerPause)
        {
            var sourceObject = new GameObject(name);
            sourceObject.transform.SetParent(parent, false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.pitch = 1f;
            source.ignoreListenerPause = ignoreListenerPause;
            source.outputAudioMixerGroup = mixerGroup;
            return source;
        }

        private static void PlayFromStart(AudioSource source, AudioClip clip)
        {
            if (source.clip == clip && source.isPlaying)
                return;

            source.Stop();
            source.clip = clip;
            source.time = 0f;
            source.Play();
        }

        private static void StopAndClear(AudioSource source)
        {
            source.Stop();
            source.clip = null;
        }

        private static string GetMixerParameter(AudioVolumeChannel channel)
        {
            return channel switch
            {
                AudioVolumeChannel.Master => MasterVolumeParameter,
                AudioVolumeChannel.Bgm => BgmVolumeParameter,
                AudioVolumeChannel.Sfx => SfxVolumeParameter,
                _ => throw new ArgumentOutOfRangeException(nameof(channel))
            };
        }
    }
}
