using System;
using UnityEngine;
using UnityEngine.UI;

namespace ValorChronicle.Battle.Flow.Presentation
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public sealed class AttackProjectileView : MonoBehaviour
    {
        [SerializeField]
        private Image image = null;

        private RectTransform cachedRectTransform;

        public Image Image => EnsureImage();
        public RectTransform RectTransform => GetRectTransform();

        public void Configure(Image targetImage)
        {
            image = targetImage
                ?? throw new ArgumentNullException(nameof(targetImage));
            image.raycastTarget = false;
        }

        public void Prepare(
            Sprite sprite,
            Color color,
            Vector2 anchoredPosition)
        {
            if (sprite == null)
            {
                throw new ArgumentNullException(nameof(sprite));
            }

            Image targetImage = EnsureImage();
            targetImage.sprite = sprite;
            targetImage.color = color;
            targetImage.raycastTarget = false;
            targetImage.enabled = true;
            RectTransform.anchoredPosition = anchoredPosition;
            RectTransform.localScale = Vector3.one;
            gameObject.SetActive(true);
        }

        public void SetAnchoredPosition(Vector2 anchoredPosition)
        {
            RectTransform.anchoredPosition = anchoredPosition;
        }

        public void ResetForPool()
        {
            Image targetImage = EnsureImage();
            targetImage.color = Color.white;
            targetImage.raycastTarget = false;
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

        private Image EnsureImage()
        {
            if (image == null)
            {
                image = GetComponent<Image>();
            }

            if (image == null)
            {
                throw new InvalidOperationException(
                    "AttackProjectileView requires an Image component.");
            }

            return image;
        }
    }
}
