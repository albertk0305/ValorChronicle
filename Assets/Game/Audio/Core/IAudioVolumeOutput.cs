namespace ValorChronicle.Audio.Core
{
    public interface IAudioVolumeOutput
    {
        void ApplyVolume(AudioVolumeChannel channel, float decibels);
    }
}
