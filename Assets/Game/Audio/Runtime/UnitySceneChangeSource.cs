using System;
using UnityEngine.SceneManagement;
using ValorChronicle.Audio.Core;

namespace ValorChronicle.Audio.Runtime
{
    public sealed class UnitySceneChangeSource : ISceneChangeSource, IDisposable
    {
        private Action<string> handlers;
        private bool isSubscribed;

        public string CurrentSceneName => SceneManager.GetActiveScene().name;

        public void Subscribe(Action<string> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            handlers += handler;
            if (isSubscribed)
                return;

            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
            isSubscribed = true;
        }

        public void Unsubscribe(Action<string> handler)
        {
            if (handler == null)
                return;

            handlers -= handler;
            if (handlers != null || !isSubscribed)
                return;

            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            isSubscribed = false;
        }

        public void Dispose()
        {
            handlers = null;
            if (!isSubscribed)
                return;

            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            isSubscribed = false;
        }

        private void HandleActiveSceneChanged(Scene previous, Scene next)
        {
            handlers?.Invoke(next.name);
        }
    }
}
