using System;

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

    public interface IBgmFadePlaybackBackend
    {
        void PlayInitialInit(float durationSeconds);
        void CrossfadeToNormal(float durationSeconds);
        void CrossfadeToSpecial(
            BgmTrackKind trackKind,
            bool preserveNormalPosition,
            float durationSeconds);
        void FadeOutSpecialAndStop(float durationSeconds);
        void StopAllImmediate();
        void Advance(float unscaledDeltaTime);
    }

    public sealed class BgmStartupFadeClock
    {
        private float durationSeconds;
        private float elapsedSeconds;

        public bool IsWaitingForPlaybackStart { get; private set; }
        public bool IsFading { get; private set; }
        public float Gain { get; private set; } = 1f;

        public void Begin(float duration)
        {
            durationSeconds = Math.Max(0f, duration);
            elapsedSeconds = 0f;
            Gain = durationSeconds > 0f ? 0f : 1f;
            IsWaitingForPlaybackStart = true;
            IsFading = durationSeconds > 0f;
        }

        public bool TryStartPlayback(bool isReady)
        {
            if (!IsWaitingForPlaybackStart || !isReady)
                return false;

            IsWaitingForPlaybackStart = false;
            return true;
        }

        public void Advance(float unscaledDeltaTime)
        {
            if (unscaledDeltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(unscaledDeltaTime));
            if (IsWaitingForPlaybackStart || !IsFading)
                return;

            elapsedSeconds += unscaledDeltaTime;
            Gain = BgmFadeMath.EvaluateLinear(
                0f,
                1f,
                elapsedSeconds,
                durationSeconds);
            if (elapsedSeconds >= durationSeconds)
                IsFading = false;
        }

        public void Reset()
        {
            durationSeconds = 0f;
            elapsedSeconds = 0f;
            IsWaitingForPlaybackStart = false;
            IsFading = false;
            Gain = 1f;
        }
    }

    public readonly struct BgmLoopFadeSettings
    {
        public BgmLoopFadeSettings(
            bool enabled,
            float fadeInSeconds,
            float fadeOutSeconds)
        {
            Enabled = enabled;
            FadeInSeconds = Math.Max(0f, fadeInSeconds);
            FadeOutSeconds = Math.Max(0f, fadeOutSeconds);
        }

        public bool Enabled { get; }
        public float FadeInSeconds { get; }
        public float FadeOutSeconds { get; }
    }

    public sealed class BgmFadeSettings
    {
        public const float DefaultSceneCrossfadeSeconds = 0.8f;
        public const float DefaultResultFadeOutSeconds = 1.2f;
        public const float DefaultLoopFadeSeconds = 0.6f;

        public BgmFadeSettings(
            float sceneCrossfadeSeconds = DefaultSceneCrossfadeSeconds,
            float resultFadeOutSeconds = DefaultResultFadeOutSeconds,
            BgmLoopFadeSettings? initLoop = null,
            BgmLoopFadeSettings? normalLoop = null,
            BgmLoopFadeSettings? battleLoop = null)
        {
            SceneCrossfadeSeconds = Math.Max(0f, sceneCrossfadeSeconds);
            ResultFadeOutSeconds = Math.Max(0f, resultFadeOutSeconds);
            BgmLoopFadeSettings defaults = new BgmLoopFadeSettings(
                true,
                DefaultLoopFadeSeconds,
                DefaultLoopFadeSeconds);
            InitLoop = initLoop ?? defaults;
            NormalLoop = normalLoop ?? defaults;
            BattleLoop = battleLoop ?? defaults;
        }

        public float SceneCrossfadeSeconds { get; }
        public float ResultFadeOutSeconds { get; }
        public BgmLoopFadeSettings InitLoop { get; }
        public BgmLoopFadeSettings NormalLoop { get; }
        public BgmLoopFadeSettings BattleLoop { get; }

        public BgmLoopFadeSettings GetLoop(BgmTrackKind trackKind)
        {
            return trackKind switch
            {
                BgmTrackKind.Init => InitLoop,
                BgmTrackKind.Normal => NormalLoop,
                BgmTrackKind.Battle => BattleLoop,
                _ => new BgmLoopFadeSettings(false, 0f, 0f)
            };
        }
    }

    public static class BgmFadeMath
    {
        public static float EvaluateLinear(
            float startGain,
            float targetGain,
            float elapsedSeconds,
            float durationSeconds)
        {
            if (durationSeconds <= 0f)
                return Clamp01(targetGain);

            float progress = Clamp01(elapsedSeconds / durationSeconds);
            return Clamp01(startGain + ((targetGain - startGain) * progress));
        }

        public static float EvaluateLoopGain(
            float playbackSeconds,
            float clipLengthSeconds,
            BgmLoopFadeSettings settings)
        {
            if (!settings.Enabled || clipLengthSeconds <= 0f)
                return 1f;

            float fadeIn = settings.FadeInSeconds;
            float fadeOut = settings.FadeOutSeconds;
            if (fadeIn <= 0f && fadeOut <= 0f)
                return 1f;

            float combined = fadeIn + fadeOut;
            if (combined > clipLengthSeconds && combined > 0f)
            {
                float scale = clipLengthSeconds / combined;
                fadeIn *= scale;
                fadeOut *= scale;
            }

            float time = Math.Max(
                0f,
                Math.Min(clipLengthSeconds, playbackSeconds));
            float fadeInGain = fadeIn <= 0f
                ? 1f
                : Clamp01(time / fadeIn);
            float remaining = clipLengthSeconds - time;
            float fadeOutGain = fadeOut <= 0f
                ? 1f
                : Clamp01(remaining / fadeOut);
            return Math.Min(fadeInGain, fadeOutGain);
        }

        private static float Clamp01(float value)
        {
            return Math.Max(0f, Math.Min(1f, value));
        }
    }
}
