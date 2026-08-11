using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ValorChronicle.Battle.Flow.Presentation
{
    [Serializable]
    public sealed class BattleMatchEventSlotView
    {
        [SerializeField]
        private GameObject root = null;

        [SerializeField]
        private Image elementImage = null;

        [SerializeField]
        private TMP_Text blockCountText = null;

        public GameObject Root => root;
        public Image ElementImage => elementImage;
        public TMP_Text BlockCountText => blockCountText;
        public bool IsConfigured =>
            root != null && elementImage != null && blockCountText != null;

        public void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.SetActive(visible);
            }

            if (!visible && blockCountText != null)
            {
                blockCountText.text = string.Empty;
            }
        }

        public void Render(Sprite elementSprite, int blockCount)
        {
            if (elementSprite == null)
            {
                throw new ArgumentNullException(nameof(elementSprite));
            }

            if (blockCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(blockCount),
                    blockCount,
                    "Block count must be positive.");
            }

            if (elementImage != null)
            {
                elementImage.sprite = elementSprite;
            }

            if (blockCountText != null)
            {
                blockCountText.text = blockCount.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
            }

            if (root != null)
            {
                root.SetActive(true);
            }
        }
    }
}
