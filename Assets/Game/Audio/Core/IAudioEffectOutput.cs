using UnityEngine;

namespace ValorChronicle.Audio.Core
{
    public interface IAudioEffectOutput
    {
        void PlaySfx(AudioClip clip);
        void PlayUiSfx(AudioClip clip);
    }
}
