using System;
using UnityEngine;
using UnityEngine.UI;

namespace ValorChronicle.Battle.Board.Presentation
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public sealed class BlockView : MonoBehaviour
    {
        [SerializeField]
        private Image image;

        private RectTransform rectTransform;

        public long RuntimeId { get; private set; }
        public BoardPosition Position { get; private set; }
        public Image Image => image;
        public RectTransform RectTransform => GetRectTransform();

        public void Bind(
            BoardBlock block,
            BoardPosition position,
            Sprite sprite)
        {
            if (block == null)
            {
                throw new ArgumentNullException(nameof(block));
            }

            if (sprite == null)
            {
                throw new ArgumentNullException(nameof(sprite));
            }

            EnsureImage();
            RuntimeId = block.RuntimeId;
            Position = position;
            image.sprite = sprite;
            GetRectTransform().anchoredPosition =
                BoardViewLayout.GetAnchoredPosition(position);
            gameObject.SetActive(true);
        }

        public void ResetForPool()
        {
            EnsureImage();
            RuntimeId = 0;
            Position = default(BoardPosition);
            image.sprite = null;
            gameObject.SetActive(false);
        }

        public void Configure(Image targetImage)
        {
            image = targetImage
                ?? throw new ArgumentNullException(nameof(targetImage));
        }

        private RectTransform GetRectTransform()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            return rectTransform;
        }

        private void EnsureImage()
        {
            if (image == null)
            {
                image = GetComponent<Image>();
            }

            if (image == null)
            {
                throw new InvalidOperationException(
                    "BlockView requires an Image component.");
            }
        }
    }
}
