using System;

namespace ValorChronicle.Audio.Core
{
    public interface ISceneChangeSource
    {
        string CurrentSceneName { get; }
        void Subscribe(Action<string> handler);
        void Unsubscribe(Action<string> handler);
    }
}
