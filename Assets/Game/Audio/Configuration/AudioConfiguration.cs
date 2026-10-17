using UnityEngine;
using UnityEngine.Audio;

namespace ValorChronicle.Audio.Configuration
{
    [CreateAssetMenu(
        fileName = "AudioConfiguration",
        menuName = "Valor Chronicle/Audio/Audio Configuration")]
    public sealed class AudioConfiguration : ScriptableObject
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

        public AudioMixer AudioMixer => audioMixer;
        public AudioMixerGroup BgmMixerGroup => bgmMixerGroup;
        public AudioMixerGroup SfxMixerGroup => sfxMixerGroup;
        public AudioMixerGroup UiMixerGroup => uiMixerGroup;
        public AudioClip InitBgm => initBgm;
        public AudioClip NormalBgm => normalBgm;
        public AudioClip BattleBgm => battleBgm;
    }
}
