using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ValorChronicle.Characters.Presentation
{
    public sealed class CharacterLevelUpPressHoldInput : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler
    {
        public const float DefaultInitialRepeatDelay = 0.45f;
        public const float DefaultRepeatInterval = 0.12f;

        [SerializeField]
        private float initialRepeatDelay = DefaultInitialRepeatDelay;
        [SerializeField]
        private float repeatInterval = DefaultRepeatInterval;

        private Button button;
        private Func<bool> canStart;
        private int activePointerId;
        private float heldDuration;
        private float nextRepeatAt;
        private bool hasActivePointer;
        private bool repeatEnabled;

        public event Action PressStarted;
        public event Action RepeatRequested;
        public event Action CommitRequested;
        public event Action CancelRequested;

        public bool HasActivePointer => hasActivePointer;
        public float InitialRepeatDelay => initialRepeatDelay;
        public float RepeatInterval => repeatInterval;

        public void Configure(
            Button configuredButton,
            Func<bool> configuredCanStart)
        {
            button = configuredButton
                ?? throw new ArgumentNullException(nameof(configuredButton));
            canStart = configuredCanStart
                ?? throw new ArgumentNullException(
                    nameof(configuredCanStart));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData != null)
            {
                TryBeginPress(eventData.pointerId);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData != null)
            {
                Release(eventData.pointerId);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (eventData != null)
            {
                Cancel(eventData.pointerId);
            }
        }

        public bool TryBeginPress(int pointerId)
        {
            if (hasActivePointer
                || button == null
                || !button.interactable
                || canStart?.Invoke() != true)
            {
                return false;
            }

            hasActivePointer = true;
            repeatEnabled = true;
            activePointerId = pointerId;
            heldDuration = 0f;
            nextRepeatAt = initialRepeatDelay;
            PressStarted?.Invoke();
            return hasActivePointer;
        }

        public void Release(int pointerId)
        {
            if (!hasActivePointer || pointerId != activePointerId)
            {
                return;
            }

            ResetState();
            CommitRequested?.Invoke();
        }

        public void Cancel(int pointerId)
        {
            if (!hasActivePointer || pointerId != activePointerId)
            {
                return;
            }

            CancelActivePress(notify: true);
        }

        public void CancelActivePress(bool notify)
        {
            if (!hasActivePointer)
            {
                return;
            }

            ResetState();
            if (notify)
            {
                CancelRequested?.Invoke();
            }
        }

        public void StopRepeating()
        {
            repeatEnabled = false;
        }

        public void AdvanceTimeForTesting(float unscaledDeltaTime)
        {
            Advance(unscaledDeltaTime);
        }

        private void Update()
        {
            Advance(Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            CancelActivePress(notify: true);
        }

        private void Advance(float unscaledDeltaTime)
        {
            if (!hasActivePointer || !repeatEnabled)
            {
                return;
            }

            if (unscaledDeltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(unscaledDeltaTime));
            }

            heldDuration += unscaledDeltaTime;
            while (hasActivePointer
                && repeatEnabled
                && heldDuration >= nextRepeatAt)
            {
                nextRepeatAt += repeatInterval;
                RepeatRequested?.Invoke();
            }
        }

        private void ResetState()
        {
            hasActivePointer = false;
            repeatEnabled = false;
            activePointerId = 0;
            heldDuration = 0f;
            nextRepeatAt = 0f;
        }

        private void OnValidate()
        {
            if (initialRepeatDelay < 0f)
            {
                initialRepeatDelay = DefaultInitialRepeatDelay;
            }

            if (repeatInterval <= 0f)
            {
                repeatInterval = DefaultRepeatInterval;
            }
        }
    }
}
