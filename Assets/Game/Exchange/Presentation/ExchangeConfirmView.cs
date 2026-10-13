using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ValorChronicle.Exchange.Presentation
{
    public sealed class ExchangeConfirmView : MonoBehaviour
    {
        public const string UnownedText = "Unowned";
        public const string NewInstanceText = "New 0";

        private GameObject root;
        private Image contentImage;
        private GameObject awakeningIcon;
        private TMP_Text awakeningText;
        private Image priceIcon;
        private TMP_Text priceText;
        private Button confirmButton;
        private Button cancelButton;
        private CanvasGroup canvasGroup;
        private bool subscribed;
        private bool canConfirm;

        public event Action ConfirmRequested;
        public event Action CancelRequested;

        public GameObject Root => root;
        public Image ContentImage => contentImage;
        public GameObject AwakeningIcon => awakeningIcon;
        public TMP_Text AwakeningText => awakeningText;
        public Image PriceIcon => priceIcon;
        public TMP_Text PriceText => priceText;
        public Button ConfirmButton => confirmButton;
        public Button CancelButton => cancelButton;
        public bool IsOpen => root != null && root.activeSelf;
        public bool IsProcessing { get; private set; }
        public bool CanConfirm => canConfirm && !IsProcessing;
        public bool IsConfigured => root != null
            && contentImage != null
            && awakeningIcon != null
            && awakeningText != null
            && priceIcon != null
            && priceText != null
            && confirmButton != null
            && cancelButton != null
            && canvasGroup != null;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(
            GameObject configuredRoot,
            Image configuredContentImage,
            GameObject configuredAwakeningIcon,
            TMP_Text configuredAwakeningText,
            Image configuredPriceIcon,
            TMP_Text configuredPriceText,
            Button configuredConfirmButton,
            Button configuredCancelButton,
            CanvasGroup configuredCanvasGroup)
        {
            Unsubscribe();
            root = Require(configuredRoot, nameof(configuredRoot));
            contentImage = Require(
                configuredContentImage,
                nameof(configuredContentImage));
            awakeningIcon = Require(
                configuredAwakeningIcon,
                nameof(configuredAwakeningIcon));
            awakeningText = Require(
                configuredAwakeningText,
                nameof(configuredAwakeningText));
            priceIcon = Require(
                configuredPriceIcon,
                nameof(configuredPriceIcon));
            priceText = Require(
                configuredPriceText,
                nameof(configuredPriceText));
            confirmButton = Require(
                configuredConfirmButton,
                nameof(configuredConfirmButton));
            cancelButton = Require(
                configuredCancelButton,
                nameof(configuredCancelButton));
            canvasGroup = Require(
                configuredCanvasGroup,
                nameof(configuredCanvasGroup));

            contentImage.raycastTarget = false;
            priceIcon.raycastTarget = false;
            awakeningText.raycastTarget = false;
            priceText.raycastTarget = false;
            Close();
            if (isActiveAndEnabled)
                Subscribe();
        }

        public void Open(
            ExchangeConfirmDisplayData data,
            Sprite resolvedContentSprite,
            Sprite resolvedPriceSprite)
        {
            EnsureConfigured();
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.UnavailableReasons.HasFlag(
                    ExchangeUnavailableReason.MaximumAwakening))
            {
                throw new InvalidOperationException(
                    "A maximum-awakening character cannot be confirmed.");
            }

            SetSprite(contentImage, resolvedContentSprite);
            SetSprite(priceIcon, resolvedPriceSprite);
            awakeningIcon.SetActive(data.Progress.ShowAwakeningIcon);
            awakeningText.text = GetProgressText(data.Progress);
            priceText.text = "X" + data.Cost.ToString(
                CultureInfo.InvariantCulture);
            canConfirm = data.CanConfirm;
            IsProcessing = false;
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            root.transform.SetAsLastSibling();
            root.SetActive(true);
            ApplyInputState();
        }

        public void SetProcessing(bool processing)
        {
            IsProcessing = processing;
            ApplyInputState();
        }

        public void Close()
        {
            canConfirm = false;
            IsProcessing = false;
            if (confirmButton != null)
                confirmButton.interactable = false;
            if (cancelButton != null)
                cancelButton.interactable = false;
            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            if (root != null)
                root.SetActive(false);
        }

        public static string GetProgressText(
            ExchangeProgressDisplayData progress)
        {
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));

            switch (progress.Kind)
            {
                case ExchangeProgressKind.Unowned:
                    return UnownedText;
                case ExchangeProgressKind.AwakeningIncrease:
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} \u2192 {1}",
                        progress.CurrentAwakening,
                        progress.ResultingAwakening);
                case ExchangeProgressKind
                    .NewInstanceAfterMaximumAwakening:
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} \u2192 {1}",
                        progress.CurrentAwakening,
                        NewInstanceText);
                default:
                    throw new InvalidOperationException(
                        "The selected Exchange progress cannot be confirmed.");
            }
        }

        private void ApplyInputState()
        {
            if (confirmButton != null)
                confirmButton.interactable = canConfirm && !IsProcessing;
            if (cancelButton != null)
                cancelButton.interactable = !IsProcessing && IsOpen;
        }

        private void Subscribe()
        {
            if (subscribed || !IsConfigured)
                return;

            confirmButton.onClick.AddListener(HandleConfirmRequested);
            cancelButton.onClick.AddListener(HandleCancelRequested);
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
                return;

            confirmButton.onClick.RemoveListener(HandleConfirmRequested);
            cancelButton.onClick.RemoveListener(HandleCancelRequested);
            subscribed = false;
        }

        private void HandleConfirmRequested()
        {
            if (CanConfirm && IsOpen)
                ConfirmRequested?.Invoke();
        }

        private void HandleCancelRequested()
        {
            if (!IsProcessing && IsOpen)
                CancelRequested?.Invoke();
        }

        private void EnsureConfigured()
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    "ExchangeConfirmView must be configured before use.");
            }
        }

        private static void SetSprite(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            Color color = image.color;
            color.a = sprite == null ? 0f : 1f;
            image.color = color;
        }

        private static T Require<T>(T value, string parameterName)
            where T : UnityEngine.Object
        {
            return value != null
                ? value
                : throw new ArgumentNullException(parameterName);
        }
    }
}
