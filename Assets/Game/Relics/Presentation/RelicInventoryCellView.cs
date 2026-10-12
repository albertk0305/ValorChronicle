using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Relics.Inventory;

namespace ValorChronicle.Relics.Presentation
{
    public sealed class RelicInventoryCellView : MonoBehaviour
    {
        public const float DimmedAlpha = 0.45f;

        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject border;
        [SerializeField] private TMP_Text awakeningText;
        private Color iconColor;
        private Color awakeningColor;
        private bool buttonSubscribed;

        public event Action<string> Clicked;

        public string InstanceId { get; private set; } = string.Empty;
        public bool IsBound { get; private set; }
        public bool IsDimmed { get; private set; }
        public Button Button => button;
        public Image IconImage => iconImage;
        public GameObject Border => border;
        public TMP_Text AwakeningText => awakeningText;
        public bool IsConfigured => button != null
            && iconImage != null
            && border != null
            && awakeningText != null;

        private void OnEnable()
        {
            SubscribeButton();
        }

        private void OnDisable()
        {
            UnsubscribeButton();
        }

        public void Configure(
            Button configuredButton,
            Image configuredIconImage,
            GameObject configuredBorder,
            TMP_Text configuredAwakeningText)
        {
            UnsubscribeButton();
            button = configuredButton
                ?? throw new ArgumentNullException(nameof(configuredButton));
            iconImage = configuredIconImage
                ?? throw new ArgumentNullException(
                    nameof(configuredIconImage));
            border = configuredBorder
                ?? throw new ArgumentNullException(nameof(configuredBorder));
            awakeningText = configuredAwakeningText
                ?? throw new ArgumentNullException(
                    nameof(configuredAwakeningText));
            iconColor = iconImage.color;
            iconColor.a = 1f;
            awakeningColor = awakeningText.color;
            awakeningColor.a = 1f;
            button.targetGraphic = iconImage;
            button.transition = Selectable.Transition.None;
            iconImage.raycastTarget = true;
            if (isActiveAndEnabled)
            {
                SubscribeButton();
            }
        }

        public void Bind(RelicInventoryEntry entry, Sprite icon)
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    $"Relic inventory cell '{name}' is not configured.");
            }

            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            gameObject.SetActive(true);
            InstanceId = entry.InstanceId;
            IsBound = true;
            IsDimmed = entry.IsDimmed;
            iconImage.sprite = icon;
            iconImage.enabled = true;
            awakeningText.text = entry.Awakening.ToString(
                CultureInfo.InvariantCulture);
            awakeningText.gameObject.SetActive(true);
            float alpha = entry.IsDimmed ? DimmedAlpha : 1f;
            iconImage.color = WithAlpha(iconColor, alpha);
            awakeningText.color = WithAlpha(awakeningColor, alpha);
            border.SetActive(true);
            button.interactable = entry.IsInteractable;
        }

        public void HideAndClear()
        {
            InstanceId = string.Empty;
            IsBound = false;
            IsDimmed = false;
            if (button != null)
            {
                button.interactable = false;
            }

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.color = iconColor;
            }

            if (awakeningText != null)
            {
                awakeningText.text = string.Empty;
                awakeningText.color = awakeningColor;
            }

            if (border != null)
            {
                border.SetActive(true);
            }

            gameObject.SetActive(false);
        }

        private void SubscribeButton()
        {
            if (buttonSubscribed || button == null)
            {
                return;
            }

            button.onClick.AddListener(HandleClick);
            buttonSubscribed = true;
        }

        private void UnsubscribeButton()
        {
            if (!buttonSubscribed || button == null)
            {
                return;
            }

            button.onClick.RemoveListener(HandleClick);
            buttonSubscribed = false;
        }

        private void HandleClick()
        {
            if (IsBound && button.interactable)
            {
                Clicked?.Invoke(InstanceId);
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a *= alpha;
            return color;
        }
    }
}
