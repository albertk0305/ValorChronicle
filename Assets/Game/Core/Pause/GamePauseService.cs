using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValorChronicle.Core.Pause
{
    public sealed class GamePauseService : IDisposable
    {
        private readonly Func<float> getTimeScale;
        private readonly Action<float> setTimeScale;
        private readonly HashSet<long> activeRequests = new HashSet<long>();
        private long nextRequestId;
        private float resumeTimeScale = 1f;
        private bool isDisposed;

        public GamePauseService()
            : this(() => Time.timeScale, value => Time.timeScale = value)
        {
        }

        public GamePauseService(
            Func<float> getTimeScale,
            Action<float> setTimeScale)
        {
            this.getTimeScale = getTimeScale
                ?? throw new ArgumentNullException(nameof(getTimeScale));
            this.setTimeScale = setTimeScale
                ?? throw new ArgumentNullException(nameof(setTimeScale));
        }

        public event Action<bool> PauseStateChanged;

        public bool IsPaused => activeRequests.Count > 0;
        public int ActiveRequestCount => activeRequests.Count;

        public IDisposable RequestPause()
        {
            ThrowIfDisposed();

            long requestId = ++nextRequestId;
            bool wasPaused = IsPaused;
            activeRequests.Add(requestId);
            if (!wasPaused)
            {
                resumeTimeScale = SanitizeResumeTimeScale(getTimeScale());
                setTimeScale(0f);
                PauseStateChanged?.Invoke(true);
            }

            return new PauseRequest(this, requestId);
        }

        public void ReleaseAll()
        {
            if (!IsPaused)
            {
                return;
            }

            activeRequests.Clear();
            RestoreTimeScaleAndNotify();
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            ReleaseAll();
            isDisposed = true;
            PauseStateChanged = null;
        }

        private void Release(long requestId)
        {
            if (!activeRequests.Remove(requestId) || IsPaused)
            {
                return;
            }

            RestoreTimeScaleAndNotify();
        }

        private void RestoreTimeScaleAndNotify()
        {
            setTimeScale(resumeTimeScale);
            PauseStateChanged?.Invoke(false);
        }

        private void ThrowIfDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(GamePauseService));
            }
        }

        private static float SanitizeResumeTimeScale(float value)
        {
            return value >= 0f
                && !float.IsNaN(value)
                && !float.IsInfinity(value)
                    ? value
                    : 1f;
        }

        private sealed class PauseRequest : IDisposable
        {
            private GamePauseService owner;
            private readonly long requestId;

            public PauseRequest(GamePauseService owner, long requestId)
            {
                this.owner = owner;
                this.requestId = requestId;
            }

            public void Dispose()
            {
                GamePauseService currentOwner = owner;
                if (currentOwner == null)
                {
                    return;
                }

                owner = null;
                currentOwner.Release(requestId);
            }
        }
    }
}
