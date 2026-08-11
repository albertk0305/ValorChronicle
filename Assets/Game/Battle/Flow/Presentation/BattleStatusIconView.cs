using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ValorChronicle.Battle.Flow.Presentation
{
    [Serializable]
    public sealed class BattleStatusIconView
    {
        [SerializeField]
        private GameObject root = null;

        [SerializeField]
        private Image iconImage = null;

        [SerializeField]
        private TMP_Text valueText = null;

        [NonSerialized]
        private Sprite defaultIconSprite;

        [NonSerialized]
        private bool defaultIconCached;

        [NonSerialized]
        private Image.Type defaultIconType;

        public GameObject Root => root;
        public Image IconImage => iconImage;
        public TMP_Text ValueText => valueText;
        public bool IsConfigured =>
            root != null && iconImage != null && valueText != null;
        public Sprite DefaultIconSprite
        {
            get
            {
                CacheDefaultIcon();
                return defaultIconSprite;
            }
        }

        public void SetVisible(bool visible)
        {
            CacheDefaultIcon();
            if (root != null)
            {
                root.SetActive(visible);
            }

            if (!visible && valueText != null)
            {
                valueText.text = string.Empty;
                valueText.gameObject.SetActive(false);
            }
        }

        public void Render(
            Sprite icon,
            int badgeValue,
            bool badgeVisible)
        {
            CacheDefaultIcon();
            if (iconImage != null)
            {
                iconImage.sprite = icon != null
                    ? icon
                    : defaultIconSprite;
                iconImage.type = icon != null
                    ? Image.Type.Simple
                    : defaultIconType;
            }

            if (valueText != null)
            {
                valueText.text = badgeVisible
                    ? badgeValue.ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                    : string.Empty;
                valueText.gameObject.SetActive(badgeVisible);
            }

            if (root != null)
            {
                root.SetActive(true);
            }
        }

        private void CacheDefaultIcon()
        {
            if (defaultIconCached)
            {
                return;
            }

            defaultIconCached = true;
            defaultIconSprite = iconImage != null
                ? iconImage.sprite
                : null;
            defaultIconType = iconImage != null
                ? iconImage.type
                : Image.Type.Simple;
        }
    }
}
