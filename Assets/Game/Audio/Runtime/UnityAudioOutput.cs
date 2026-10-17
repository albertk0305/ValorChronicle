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
        IAudioEffectOutput
    {
        public const string MasterVolumeParameter = "MasterVolumeDb";
        public const string BgmVolumeParameter = "BgmVolumeDb";
        public const string SfxVolumeParameter = "SfxVolumeDb";

        private readonly GameAudioConfiguration configuration;
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

        public UnityAudioOutput(
            GameObject owner,
            GameAudioConfiguration configuration)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            this.configuration = configuration;
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

            RefreshFallbackVolumes();
        }

        public void PlayNormalFromStart()
        {
            AudioClip clip = configuration?.NormalBgm;
            if (clip == null)
                return;

            PlayFromStart(normalBgmSource, clip);
        }

        public void PauseNormal()
        {
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
            StopAndClear(normalBgmSource);
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

            PlayFromStart(specialBgmSource, clip);
        }

        public void PauseSpecial()
        {
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
            StopAndClear(specialBgmSource);
        }

        public void PlaySfx(AudioClip clip)
        {
            if (clip != null)
                sfxSource.PlayOneShot(clip);
        }

        public void PlayUiSfx(AudioClip clip)
        {
            if (clip != null)
                uiSfxSource.PlayOneShot(clip);
        }

        private void RefreshFallbackVolumes()
        {
            normalBgmSource.volume = masterLinear * bgmLinear;
            specialBgmSource.volume = masterLinear * bgmLinear;
            sfxSource.volume = masterLinear * sfxLinear;
            uiSfxSource.volume = masterLinear * sfxLinear;
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
