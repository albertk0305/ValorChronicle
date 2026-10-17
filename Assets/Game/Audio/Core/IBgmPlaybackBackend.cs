namespace ValorChronicle.Audio.Core
{
    public interface IBgmPlaybackBackend
    {
        void PlayNormalFromStart();
        void PauseNormal();
        void ResumeNormal();
        void StopNormal();
        void PlaySpecial(BgmTrackKind trackKind);
        void PauseSpecial();
        void ResumeSpecial();
        void StopSpecial();
    }
}
