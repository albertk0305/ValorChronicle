using System;
using TMPro;
using UnityEngine;

namespace ValorChronicle.Battle.Flow.Presentation
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class DamageNumberView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI text = null;

        private RectTransform cachedRectTransform;

        public TextMeshProUGUI Text => EnsureText();
        public RectTransform RectTransform => GetRectTransform();
        public string DisplayText => EnsureText().text;
        public bool BlocksRaycasts => EnsureText().raycastTarget;

        public void Configure(TextMeshProUGUI targetText)
        {
            text = targetText
                ?? throw new ArgumentNullException(nameof(targetText));
            text.raycastTarget = false;
        }

        public void Prepare(
            string value,
            float fontSize,
            Color faceColor,
            Color outlineColor,
            float outlineWidth,
            Vector2 anchoredPosition)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(
                    "A damage number must have display text.",
                    nameof(value));
            }

            TextMeshProUGUI targetText = EnsureText();
            targetText.text = value;
            targetText.fontSize = fontSize;
            targetText.color = faceColor;
            targetText.outlineColor = outlineColor;
            targetText.outlineWidth = outlineWidth;
            targetText.alpha = 1f;
            targetText.raycastTarget = false;
            targetText.enabled = true;
            RectTransform.anchoredPosition = anchoredPosition;
            RectTransform.localScale = Vector3.one;
            gameObject.SetActive(true);
        }

        public void SetPresentation(Vector2 anchoredPosition, float alpha)
        {
            RectTransform.anchoredPosition = anchoredPosition;
            EnsureText().alpha = Mathf.Clamp01(alpha);
        }

        public void ResetForPool()
        {
            TextMeshProUGUI targetText = EnsureText();
            targetText.text = string.Empty;
            targetText.color = Color.white;
            targetText.outlineColor = Color.black;
            targetText.outlineWidth = 0f;
            targetText.alpha = 1f;
            targetText.raycastTarget = false;
            RectTransform.anchoredPosition = Vector2.zero;
            RectTransform.localScale = Vector3.one;
            gameObject.SetActive(false);
        }

        private RectTransform GetRectTransform()
        {
            if (cachedRectTransform == null)
            {
                cachedRectTransform = GetComponent<RectTransform>();
            }

            return cachedRectTransform;
        }

        private TextMeshProUGUI EnsureText()
        {
            if (text == null)
            {
                text = GetComponent<TextMeshProUGUI>();
            }

            if (text == null)
            {
                throw new InvalidOperationException(
                    "DamageNumberView requires TextMeshProUGUI.");
            }

            return text;
        }
    }
}
