using UnityEngine;

namespace ValorChronicle.Battle.Flow.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class BattleScreenShake : MonoBehaviour
    {
        public const float DefaultDurationSeconds = 0.15f;
        public const float DefaultMaximumOffset = 8f;

        private const float TwoPi = Mathf.PI * 2f;
        private const float HorizontalCycles = 3f;
        private const float VerticalCycles = 4f;
        private const float HorizontalPhase = Mathf.PI * 0.25f;
        private const float VerticalPhase = Mathf.PI / 6f;

        [SerializeField]
        [Min(0f)]
        private float durationSeconds = DefaultDurationSeconds;

        [SerializeField]
        [Min(0f)]
        private float maximumOffset = DefaultMaximumOffset;

        private RectTransform target;
        private Vector2 basePosition;
        private Vector2 currentOffset;
        private float elapsedSeconds;

        public bool IsShaking { get; private set; }
        public Vector2 BasePosition => basePosition;
        public Vector2 CurrentOffset => currentOffset;
        public float DurationSeconds => ResolveDurationSeconds();
        public float MaximumOffset => ResolveMaximumOffset();

        private void Update()
        {
            Advance(Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            StopAndRestore();
        }

        private void OnDestroy()
        {
            StopAndRestore();
        }

        public bool Shake()
        {
            if (!isActiveAndEnabled || !TryResolveTarget())
            {
                return false;
            }

            if (!IsShaking)
            {
                basePosition = target.anchoredPosition;
            }

            elapsedSeconds = 0f;
            IsShaking = true;
            ApplyOffset(progress: 0f);
            return true;
        }

        public void Advance(float unscaledDeltaTime)
        {
            if (!IsShaking
                || unscaledDeltaTime <= 0f
                || float.IsNaN(unscaledDeltaTime)
                || float.IsInfinity(unscaledDeltaTime))
            {
                return;
            }

            elapsedSeconds += unscaledDeltaTime;
            float duration = ResolveDurationSeconds();
            if (elapsedSeconds >= duration)
            {
                StopAndRestore();
                return;
            }

            ApplyOffset(Mathf.Clamp01(elapsedSeconds / duration));
        }

        public void StopAndRestore()
        {
            if (IsShaking && TryResolveTarget())
            {
                target.anchoredPosition = basePosition;
            }

            IsShaking = false;
            elapsedSeconds = 0f;
            currentOffset = Vector2.zero;
        }

        private void ApplyOffset(float progress)
        {
            float envelope = 1f - Mathf.Clamp01(progress);
            envelope *= envelope;
            float horizontalAngle =
                progress * TwoPi * HorizontalCycles + HorizontalPhase;
            float verticalAngle =
                progress * TwoPi * VerticalCycles + VerticalPhase;
            var direction = new Vector2(
                Mathf.Sin(horizontalAngle),
                Mathf.Cos(verticalAngle));
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                direction = Vector2.right;
            }
            else
            {
                direction.Normalize();
            }

            float limit = ResolveMaximumOffset();
            currentOffset = Vector2.ClampMagnitude(
                direction * (limit * envelope),
                limit);
            target.anchoredPosition = basePosition + currentOffset;
        }

        private bool TryResolveTarget()
        {
            if (target == null)
            {
                target = GetComponent<RectTransform>();
            }

            return target != null;
        }

        private float ResolveDurationSeconds()
        {
            return durationSeconds > 0f
                && !float.IsNaN(durationSeconds)
                && !float.IsInfinity(durationSeconds)
                    ? durationSeconds
                    : DefaultDurationSeconds;
        }

        private float ResolveMaximumOffset()
        {
            return maximumOffset >= 0f
                && !float.IsNaN(maximumOffset)
                && !float.IsInfinity(maximumOffset)
                    ? maximumOffset
                    : DefaultMaximumOffset;
        }
    }
}
