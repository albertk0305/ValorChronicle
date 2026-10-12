using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ValorChronicle.Relics.Presentation
{
    public sealed class RelicSelectionScreenView : MonoBehaviour
    {
        private Image previewIconImage;
        private GameObject previewEmptyBackground;
        private GameObject previewBorder;
        private TMP_Text previewAwakeningText;
        private TMP_Text descriptionText;
        private Button equipButton;
        private Button removeButton;
        private Color visibleIconColor;
        private bool listenersBound;

        public event Action EquipRequested;
        public event Action RemoveRequested;

        public Image PreviewIconImage => previewIconImage;
        public GameObject PreviewEmptyBackground => previewEmptyBackground;
        public GameObject PreviewBorder => previewBorder;
        public TMP_Text PreviewAwakeningText => previewAwakeningText;
        public TMP_Text DescriptionText => descriptionText;
        public Button EquipButton => equipButton;
        public Button RemoveButton => removeButton;
        public bool IsIconVisible => previewIconImage != null
            && previewIconImage.sprite != null
            && previewIconImage.color.a > 0f;

        public void Configure(
            Image configuredPreviewIconImage,
            GameObject configuredPreviewEmptyBackground,
            GameObject configuredPreviewBorder,
            TMP_Text configuredPreviewAwakeningText,
            TMP_Text configuredDescriptionText,
            Button configuredEquipButton,
            Button configuredRemoveButton)
        {
            UnbindListeners();
            previewIconImage = configuredPreviewIconImage
                ?? throw new ArgumentNullException(
                    nameof(configuredPreviewIconImage));
            previewEmptyBackground = configuredPreviewEmptyBackground
                ?? throw new ArgumentNullException(
                    nameof(configuredPreviewEmptyBackground));
            previewBorder = configuredPreviewBorder
                ?? throw new ArgumentNullException(
                    nameof(configuredPreviewBorder));
            previewAwakeningText = configuredPreviewAwakeningText
                ?? throw new ArgumentNullException(
                    nameof(configuredPreviewAwakeningText));
            descriptionText = configuredDescriptionText
                ?? throw new ArgumentNullException(
                    nameof(configuredDescriptionText));
            equipButton = configuredEquipButton
                ?? throw new ArgumentNullException(
                    nameof(configuredEquipButton));
            removeButton = configuredRemoveButton
                ?? throw new ArgumentNullException(
                    nameof(configuredRemoveButton));
            visibleIconColor = previewIconImage.color;
            if (visibleIconColor.a <= 0f)
            {
                visibleIconColor.a = 1f;
            }
            BindListeners();
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void RenderPreview(RelicPresentationModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            previewBorder.SetActive(true);
            descriptionText.text = model.Description;
            if (model.IsEmpty)
            {
                previewIconImage.sprite = null;
                previewIconImage.color = WithAlpha(visibleIconColor, 0f);
                previewEmptyBackground.SetActive(true);
                previewAwakeningText.text = string.Empty;
                previewAwakeningText.gameObject.SetActive(false);
                return;
            }

            previewIconImage.sprite = model.Icon;
            previewIconImage.color = visibleIconColor;
            previewEmptyBackground.SetActive(false);
            previewAwakeningText.text = model.Awakening.ToString(
                CultureInfo.InvariantCulture);
            previewAwakeningText.gameObject.SetActive(true);
        }

        public void Clear()
        {
            if (previewIconImage != null)
            {
                RenderPreview(RelicPresentationModel.Empty());
            }
        }

        private void OnDestroy()
        {
            UnbindListeners();
        }

        private void BindListeners()
        {
            if (listenersBound)
            {
                return;
            }

            equipButton.onClick.AddListener(HandleEquipRequested);
            removeButton.onClick.AddListener(HandleRemoveRequested);
            listenersBound = true;
        }

        private void UnbindListeners()
        {
            if (!listenersBound)
            {
                return;
            }

            equipButton.onClick.RemoveListener(HandleEquipRequested);
            removeButton.onClick.RemoveListener(HandleRemoveRequested);
            listenersBound = false;
        }

        private void HandleEquipRequested()
        {
            EquipRequested?.Invoke();
        }

        private void HandleRemoveRequested()
        {
            RemoveRequested?.Invoke();
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
