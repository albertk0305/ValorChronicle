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
        public bool IsInputEnabled => image != null && image.raycastTarget;

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
            RectTransform targetRectTransform = GetRectTransform();
            targetRectTransform.anchoredPosition =
                BoardViewLayout.GetAnchoredPosition(position);
            targetRectTransform.localScale = Vector3.one;
            gameObject.SetActive(true);
        }

        public void SetAnchoredPosition(Vector2 anchoredPosition)
        {
            GetRectTransform().anchoredPosition = anchoredPosition;
        }

        public void SetLogicalPosition(BoardPosition position)
        {
            Position = position;
        }

        public void SetLocalScale(Vector3 localScale)
        {
            GetRectTransform().localScale = localScale;
        }

        public void SetSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                throw new ArgumentNullException(nameof(sprite));
            }

            EnsureImage();
            image.sprite = sprite;
        }

        public void SetInputEnabled(bool isEnabled)
        {
            EnsureImage();
            image.raycastTarget = isEnabled;
        }

        public void ResetForPool()
        {
            EnsureImage();
            RuntimeId = 0;
            Position = default(BoardPosition);
            image.sprite = null;
            image.raycastTarget = false;
            GetRectTransform().localScale = Vector3.one;
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
