using UnityEngine;
using UnityEngine.Audio;
using ValorChronicle.Audio.Core;

namespace ValorChronicle.Audio.Configuration
{
    [CreateAssetMenu(
        fileName = "AudioConfiguration",
        menuName = "Valor Chronicle/Audio/Audio Configuration")]
    public sealed class AudioConfiguration : ScriptableObject,
        ISfxClipProvider
    {
        [SerializeField]
        private AudioMixer audioMixer;
        [SerializeField]
        private AudioMixerGroup bgmMixerGroup;
        [SerializeField]
        private AudioMixerGroup sfxMixerGroup;
        [SerializeField]
        private AudioMixerGroup uiMixerGroup;
        [SerializeField]
        private AudioClip initBgm;
        [SerializeField]
        private AudioClip normalBgm;
        [SerializeField]
        private AudioClip battleBgm;
        [SerializeField]
        private AudioClip buttonPressSfx;
        [SerializeField]
        private AudioClip blockPopSfx;
        [SerializeField]
        private AudioClip hitSfx;
        [SerializeField]
        [Min(0f)]
        private float sceneCrossfadeSeconds =
            BgmFadeSettings.DefaultSceneCrossfadeSeconds;
        [SerializeField]
        [Min(0f)]
        private float resultFadeOutSeconds =
            BgmFadeSettings.DefaultResultFadeOutSeconds;
        [SerializeField] private bool initLoopFadeEnabled = true;
        [SerializeField] private bool normalLoopFadeEnabled = true;
        [SerializeField] private bool battleLoopFadeEnabled = true;
        [SerializeField]
        [Min(0f)]
        private float initLoopFadeInSeconds =
            BgmFadeSettings.DefaultLoopFadeSeconds;
        [SerializeField]
        [Min(0f)]
        private float initLoopFadeOutSeconds =
            BgmFadeSettings.DefaultLoopFadeSeconds;
        [SerializeField]
        [Min(0f)]
        private float normalLoopFadeInSeconds =
            BgmFadeSettings.DefaultLoopFadeSeconds;
        [SerializeField]
        [Min(0f)]
        private float normalLoopFadeOutSeconds =
            BgmFadeSettings.DefaultLoopFadeSeconds;
        [SerializeField]
        [Min(0f)]
        private float battleLoopFadeInSeconds =
            BgmFadeSettings.DefaultLoopFadeSeconds;
        [SerializeField]
        [Min(0f)]
        private float battleLoopFadeOutSeconds =
            BgmFadeSettings.DefaultLoopFadeSeconds;

        public AudioMixer AudioMixer => audioMixer;
        public AudioMixerGroup BgmMixerGroup => bgmMixerGroup;
        public AudioMixerGroup SfxMixerGroup => sfxMixerGroup;
        public AudioMixerGroup UiMixerGroup => uiMixerGroup;
        public AudioClip InitBgm => initBgm;
        public AudioClip NormalBgm => normalBgm;
        public AudioClip BattleBgm => battleBgm;
        public AudioClip ButtonPressSfx => buttonPressSfx;
        public AudioClip BlockPopSfx => blockPopSfx;
        public AudioClip HitSfx => hitSfx;

        public bool TryGetSfxClip(SfxCue cue, out AudioClip clip)
        {
            switch (cue)
            {
                case SfxCue.ButtonPress:
                    clip = buttonPressSfx;
                    break;
                case SfxCue.BlockPop:
                    clip = blockPopSfx;
                    break;
                case SfxCue.Hit:
                    clip = hitSfx;
                    break;
                default:
                    clip = null;
                    return false;
            }

            return clip != null;
        }

        public BgmFadeSettings CreateFadeSettings()
        {
            return new BgmFadeSettings(
                sceneCrossfadeSeconds,
                resultFadeOutSeconds,
                new BgmLoopFadeSettings(
                    initLoopFadeEnabled,
                    initLoopFadeInSeconds,
                    initLoopFadeOutSeconds),
                new BgmLoopFadeSettings(
                    normalLoopFadeEnabled,
                    normalLoopFadeInSeconds,
                    normalLoopFadeOutSeconds),
                new BgmLoopFadeSettings(
                    battleLoopFadeEnabled,
                    battleLoopFadeInSeconds,
                    battleLoopFadeOutSeconds));
        }
    }
}
