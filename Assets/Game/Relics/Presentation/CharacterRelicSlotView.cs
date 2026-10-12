using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Relics.Presentation
{
    public sealed class CharacterRelicSlotView : MonoBehaviour
    {
        private Button button;
        private Image iconImage;
        private GameObject emptyBackground;
        private GameObject border;
        private TMP_Text awakeningText;
        private Color visibleIconColor;
        private bool listenerBound;

        public event Action<int> Requested;

        public int SlotIndex { get; private set; } = -1;
        public Button Button => button;
        public Image IconImage => iconImage;
        public GameObject EmptyBackground => emptyBackground;
        public GameObject Border => border;
        public TMP_Text AwakeningText => awakeningText;
        public bool IsIconVisible => iconImage != null
            && iconImage.sprite != null
            && iconImage.color.a > 0f;

        public void Configure(
            int configuredSlotIndex,
            Button configuredButton,
            Image configuredIconImage,
            GameObject configuredEmptyBackground,
            GameObject configuredBorder,
            TMP_Text configuredAwakeningText)
        {
            if (configuredSlotIndex < 0
                || configuredSlotIndex
                    >= SaveRules.RelicSlotCountPerCharacter)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(configuredSlotIndex));
            }

            UnbindListener();
            SlotIndex = configuredSlotIndex;
            button = configuredButton
                ?? throw new ArgumentNullException(nameof(configuredButton));
            iconImage = configuredIconImage
                ?? throw new ArgumentNullException(
                    nameof(configuredIconImage));
            emptyBackground = configuredEmptyBackground
                ?? throw new ArgumentNullException(
                    nameof(configuredEmptyBackground));
            border = configuredBorder
                ?? throw new ArgumentNullException(nameof(configuredBorder));
            awakeningText = configuredAwakeningText
                ?? throw new ArgumentNullException(
                    nameof(configuredAwakeningText));
            visibleIconColor = iconImage.color;
            if (visibleIconColor.a <= 0f)
            {
                visibleIconColor.a = 1f;
            }
            BindListener();
        }

        public void Render(RelicPresentationModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            border.SetActive(true);
            if (model.IsEmpty)
            {
                iconImage.sprite = null;
                iconImage.color = WithAlpha(visibleIconColor, 0f);
                emptyBackground.SetActive(true);
                awakeningText.text = string.Empty;
                awakeningText.gameObject.SetActive(false);
                return;
            }

            iconImage.sprite = model.Icon;
            iconImage.color = visibleIconColor;
            emptyBackground.SetActive(false);
            awakeningText.text = model.Awakening.ToString(
                CultureInfo.InvariantCulture);
            awakeningText.gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            UnbindListener();
        }

        private void BindListener()
        {
            if (listenerBound)
            {
                return;
            }

            button.onClick.AddListener(HandleClick);
            listenerBound = true;
        }

        private void UnbindListener()
        {
            if (!listenerBound)
            {
                return;
            }

            button.onClick.RemoveListener(HandleClick);
            listenerBound = false;
        }

        private void HandleClick()
        {
            Requested?.Invoke(SlotIndex);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
