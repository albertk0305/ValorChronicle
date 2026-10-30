using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ValorChronicle.Exchange.Presentation
{
    public sealed class ExchangeSlotView : MonoBehaviour
    {
        public const float MaximumCharacterAlpha = 0.45f;
        public const string UnownedText = "New";

        [SerializeField] private Button button;
        [SerializeField] private Image contentImage;
        [SerializeField] private GameObject awakeningIcon;
        [SerializeField] private TMP_Text awakeningText;
        [SerializeField] private Image priceIcon;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private CanvasGroup canvasGroup;

        private bool buttonSubscribed;
        private bool inputEnabled = true;

        public event Action<ExchangeContentKind, string> Clicked;

        public ExchangeContentKind ContentKind { get; private set; }
        public string ContentId { get; private set; } = string.Empty;
        public bool IsBound { get; private set; }
        public bool IsMaximumCharacter { get; private set; }
        public Button Button => button;
        public Image ContentImage => contentImage;
        public GameObject AwakeningIcon => awakeningIcon;
        public TMP_Text AwakeningText => awakeningText;
        public Image PriceIcon => priceIcon;
        public TMP_Text PriceText => priceText;
        public CanvasGroup CanvasGroup => canvasGroup;
        public bool IsConfigured => button != null
            && contentImage != null
            && awakeningIcon != null
            && awakeningText != null
            && priceIcon != null
            && priceText != null
            && canvasGroup != null;

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
            Image configuredContentImage,
            GameObject configuredAwakeningIcon,
            TMP_Text configuredAwakeningText,
            Image configuredPriceIcon,
            TMP_Text configuredPriceText,
            CanvasGroup configuredCanvasGroup)
        {
            UnsubscribeButton();
            button = configuredButton
                ?? throw new ArgumentNullException(nameof(configuredButton));
            contentImage = configuredContentImage
                ?? throw new ArgumentNullException(
                    nameof(configuredContentImage));
            awakeningIcon = configuredAwakeningIcon
                ?? throw new ArgumentNullException(
                    nameof(configuredAwakeningIcon));
            awakeningText = configuredAwakeningText
                ?? throw new ArgumentNullException(
                    nameof(configuredAwakeningText));
            priceIcon = configuredPriceIcon
                ?? throw new ArgumentNullException(
                    nameof(configuredPriceIcon));
            priceText = configuredPriceText
                ?? throw new ArgumentNullException(
                    nameof(configuredPriceText));
            canvasGroup = configuredCanvasGroup
                ?? throw new ArgumentNullException(
                    nameof(configuredCanvasGroup));
            button.targetGraphic = contentImage;
            contentImage.raycastTarget = true;
            if (isActiveAndEnabled)
                SubscribeButton();
        }

        public void Bind(
            ExchangeListItem item,
            Sprite resolvedContentSprite,
            Sprite resolvedPriceSprite)
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    $"Exchange slot '{name}' is not configured.");
            }

            if (item == null)
                throw new ArgumentNullException(nameof(item));

            gameObject.SetActive(true);
            ContentKind = item.ContentKind;
            ContentId = item.ContentId;
            IsBound = true;
            IsMaximumCharacter = item.ContentKind
                == ExchangeContentKind.Character
                && item.UnavailableReasons.HasFlag(
                    ExchangeUnavailableReason.MaximumAwakening);

            SetSprite(contentImage, resolvedContentSprite);
            SetSprite(priceIcon, resolvedPriceSprite);
            awakeningIcon.SetActive(item.Progress.ShowAwakeningIcon);
            awakeningText.text = GetAwakeningText(item.Progress);
            priceText.text = string.Format(
                CultureInfo.InvariantCulture,
                "X{0}",
                item.Cost);

            button.interactable = inputEnabled && !IsMaximumCharacter;
            canvasGroup.alpha = IsMaximumCharacter
                ? MaximumCharacterAlpha
                : 1f;
            canvasGroup.interactable = inputEnabled && !IsMaximumCharacter;
            canvasGroup.blocksRaycasts = inputEnabled && !IsMaximumCharacter;
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (!IsBound || !IsConfigured)
                return;

            bool interactable = enabled && !IsMaximumCharacter;
            button.interactable = interactable;
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }

        public void HideAndClear()
        {
            ContentId = string.Empty;
            IsBound = false;
            IsMaximumCharacter = false;
            if (button != null)
                button.interactable = false;
            if (contentImage != null)
                SetSprite(contentImage, null);
            if (priceIcon != null)
                SetSprite(priceIcon, null);
            if (awakeningIcon != null)
                awakeningIcon.SetActive(false);
            if (awakeningText != null)
                awakeningText.text = string.Empty;
            if (priceText != null)
                priceText.text = string.Empty;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        private static string GetAwakeningText(
            ExchangeProgressDisplayData progress)
        {
            if (progress.Kind == ExchangeProgressKind.Unowned)
                return UnownedText;

            if (!progress.CurrentAwakening.HasValue)
                return UnownedText;

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} / {1}",
                progress.CurrentAwakening.Value,
                progress.MaximumAwakening);
        }

        private void SubscribeButton()
        {
            if (buttonSubscribed || button == null)
                return;

            button.onClick.AddListener(HandleClick);
            buttonSubscribed = true;
        }

        private void UnsubscribeButton()
        {
            if (!buttonSubscribed || button == null)
                return;

            button.onClick.RemoveListener(HandleClick);
            buttonSubscribed = false;
        }

        private void HandleClick()
        {
            if (IsBound && button != null && button.interactable)
                Clicked?.Invoke(ContentKind, ContentId);
        }

        private static void SetSprite(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            Color color = image.color;
            color.a = sprite == null ? 0f : 1f;
            image.color = color;
        }
    }
}
