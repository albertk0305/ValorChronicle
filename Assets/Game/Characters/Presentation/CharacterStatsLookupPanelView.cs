using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ValorChronicle.Characters.Presentation
{
    public sealed class CharacterStatsLookupPanelView : MonoBehaviour
    {
        private const float VerticalPadding = 40f;

        private Button closeButton;
        private TMP_Text contentText;
        private RectTransform content;
        private RectTransform viewport;
        private ScrollRect scrollRect;
        private bool listenerBound;

        public event Action CloseRequested;

        public TMP_Text ContentText => contentText;
        public ScrollRect ScrollRect => scrollRect;
        public bool IsVisible => gameObject.activeSelf;

        private void OnEnable()
        {
            BindListener();
        }

        private void OnDisable()
        {
            UnbindListener();
        }

        private void OnDestroy()
        {
            UnbindListener();
        }

        public void Configure(
            Button configuredCloseButton,
            TMP_Text configuredContentText,
            RectTransform configuredViewport)
        {
            UnbindListener();
            closeButton = configuredCloseButton
                ?? throw new ArgumentNullException(
                    nameof(configuredCloseButton));
            contentText = configuredContentText
                ?? throw new ArgumentNullException(
                    nameof(configuredContentText));
            content = contentText.rectTransform;
            viewport = configuredViewport
                ?? throw new ArgumentNullException(
                    nameof(configuredViewport));

            if (viewport.GetComponent<RectMask2D>() == null)
            {
                viewport.gameObject.AddComponent<RectMask2D>();
            }

            scrollRect = viewport.GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            }

            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.elasticity = 0.1f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 1f;
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, content.sizeDelta.y);
            contentText.textWrappingMode = TextWrappingModes.Normal;
            contentText.raycastTarget = false;

            if (isActiveAndEnabled)
            {
                BindListener();
            }
        }

        public void Show(string text)
        {
            if (contentText == null || scrollRect == null)
            {
                throw new InvalidOperationException(
                    "Stats lookup panel is not configured.");
            }

            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            contentText.text = text ?? string.Empty;
            contentText.ForceMeshUpdate();
            float height = Mathf.Max(
                viewport.rect.height,
                contentText.preferredHeight + VerticalPadding);
            content.sizeDelta = new Vector2(0f, height);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            Canvas.ForceUpdateCanvases();
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1f;
        }

        public void Hide()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        private void BindListener()
        {
            if (listenerBound || closeButton == null)
            {
                return;
            }

            closeButton.onClick.AddListener(HandleCloseRequested);
            listenerBound = true;
        }

        private void UnbindListener()
        {
            if (!listenerBound)
            {
                return;
            }

            closeButton.onClick.RemoveListener(HandleCloseRequested);
            listenerBound = false;
        }

        private void HandleCloseRequested()
        {
            CloseRequested?.Invoke();
        }
    }
}
