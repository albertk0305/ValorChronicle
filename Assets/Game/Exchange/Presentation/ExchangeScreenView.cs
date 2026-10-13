using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ValorChronicle.Exchange.Presentation
{
    public enum ExchangeTab
    {
        Hero = 0,
        Artifact = 1
    }

    public sealed class ExchangeScreenView : MonoBehaviour
    {
        private Button entryButton;
        private Button exitButton;
        private Button heroButton;
        private Button artifactButton;
        private GameObject screenRoot;
        private GameObject confirmRoot;
        private TMP_Text heroTokensText;
        private TMP_Text relicTokensText;
        private TMP_Text gachaCurrencyText;
        private CanvasGroup screenCanvasGroup;
        private ColorBlock heroColors;
        private ColorBlock artifactColors;
        private bool subscribed;
        private bool colorsCaptured;

        public event Action OpenRequested;
        public event Action CloseRequested;
        public event Action HeroTabRequested;
        public event Action ArtifactTabRequested;

        public Button EntryButton => entryButton;
        public Button ExitButton => exitButton;
        public Button HeroButton => heroButton;
        public Button ArtifactButton => artifactButton;
        public GameObject ScreenRoot => screenRoot;
        public GameObject ConfirmRoot => confirmRoot;
        public TMP_Text HeroTokensText => heroTokensText;
        public TMP_Text RelicTokensText => relicTokensText;
        public TMP_Text GachaCurrencyText => gachaCurrencyText;
        public bool IsOpen => screenRoot != null && screenRoot.activeSelf;
        public bool IsConfigured => entryButton != null
            && exitButton != null
            && heroButton != null
            && artifactButton != null
            && screenRoot != null
            && confirmRoot != null
            && heroTokensText != null
            && relicTokensText != null
            && gachaCurrencyText != null
            && screenCanvasGroup != null;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(
            Button configuredEntryButton,
            Button configuredExitButton,
            Button configuredHeroButton,
            Button configuredArtifactButton,
            GameObject configuredScreenRoot,
            GameObject configuredConfirmRoot,
            TMP_Text configuredHeroTokensText,
            TMP_Text configuredRelicTokensText,
            TMP_Text configuredGachaCurrencyText,
            CanvasGroup configuredScreenCanvasGroup)
        {
            Unsubscribe();
            bool buttonsChanged = heroButton != configuredHeroButton
                || artifactButton != configuredArtifactButton;
            entryButton = Require(
                configuredEntryButton,
                nameof(configuredEntryButton));
            exitButton = Require(
                configuredExitButton,
                nameof(configuredExitButton));
            heroButton = Require(
                configuredHeroButton,
                nameof(configuredHeroButton));
            artifactButton = Require(
                configuredArtifactButton,
                nameof(configuredArtifactButton));
            screenRoot = Require(
                configuredScreenRoot,
                nameof(configuredScreenRoot));
            confirmRoot = Require(
                configuredConfirmRoot,
                nameof(configuredConfirmRoot));
            heroTokensText = Require(
                configuredHeroTokensText,
                nameof(configuredHeroTokensText));
            relicTokensText = Require(
                configuredRelicTokensText,
                nameof(configuredRelicTokensText));
            gachaCurrencyText = Require(
                configuredGachaCurrencyText,
                nameof(configuredGachaCurrencyText));
            screenCanvasGroup = Require(
                configuredScreenCanvasGroup,
                nameof(configuredScreenCanvasGroup));

            if (!colorsCaptured || buttonsChanged)
            {
                heroColors = heroButton.colors;
                artifactColors = artifactButton.colors;
                colorsCaptured = true;
            }

            Close();
            ClearCurrencies();
            if (isActiveAndEnabled)
                Subscribe();
        }

        public void Open()
        {
            EnsureConfigured();
            confirmRoot.SetActive(false);
            screenCanvasGroup.alpha = 1f;
            screenCanvasGroup.interactable = true;
            screenCanvasGroup.blocksRaycasts = true;
            screenRoot.transform.SetAsLastSibling();
            screenRoot.SetActive(true);
            SetNavigationInteractable(true, true);
        }

        public void Close()
        {
            if (confirmRoot != null)
                confirmRoot.SetActive(false);
            if (screenCanvasGroup != null)
            {
                screenCanvasGroup.interactable = false;
                screenCanvasGroup.blocksRaycasts = false;
            }
            if (screenRoot != null)
                screenRoot.SetActive(false);
        }

        public void BindCurrencies(
            long heroTokens,
            long relicTokens,
            long gachaCurrency)
        {
            EnsureConfigured();
            heroTokensText.text = FormatCurrency(heroTokens);
            relicTokensText.text = FormatCurrency(relicTokens);
            gachaCurrencyText.text = FormatCurrency(gachaCurrency);
        }

        public void ClearCurrencies()
        {
            SetText(heroTokensText, string.Empty);
            SetText(relicTokensText, string.Empty);
            SetText(gachaCurrencyText, string.Empty);
        }

        public void SetSelectedTab(ExchangeTab selectedTab)
        {
            EnsureConfigured();
            ApplyTabColors(
                heroButton,
                heroColors,
                selectedTab == ExchangeTab.Hero);
            ApplyTabColors(
                artifactButton,
                artifactColors,
                selectedTab == ExchangeTab.Artifact);
        }

        public void SetNavigationInteractable(
            bool tabsInteractable,
            bool exitInteractable)
        {
            if (heroButton != null)
                heroButton.interactable = tabsInteractable;
            if (artifactButton != null)
                artifactButton.interactable = tabsInteractable;
            if (exitButton != null)
                exitButton.interactable = exitInteractable;
        }

        private void Subscribe()
        {
            if (subscribed || !IsConfigured)
                return;

            entryButton.onClick.AddListener(HandleOpenRequested);
            exitButton.onClick.AddListener(HandleCloseRequested);
            heroButton.onClick.AddListener(HandleHeroTabRequested);
            artifactButton.onClick.AddListener(
                HandleArtifactTabRequested);
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
                return;

            entryButton.onClick.RemoveListener(HandleOpenRequested);
            exitButton.onClick.RemoveListener(HandleCloseRequested);
            heroButton.onClick.RemoveListener(HandleHeroTabRequested);
            artifactButton.onClick.RemoveListener(
                HandleArtifactTabRequested);
            subscribed = false;
        }

        private void HandleOpenRequested() => OpenRequested?.Invoke();
        private void HandleCloseRequested() => CloseRequested?.Invoke();
        private void HandleHeroTabRequested() => HeroTabRequested?.Invoke();
        private void HandleArtifactTabRequested() =>
            ArtifactTabRequested?.Invoke();

        private static void ApplyTabColors(
            Button button,
            ColorBlock original,
            bool selected)
        {
            ColorBlock colors = original;
            if (selected)
            {
                colors.normalColor = original.pressedColor;
                colors.highlightedColor = original.pressedColor;
                colors.selectedColor = original.pressedColor;
            }

            button.colors = colors;
            if (button.targetGraphic != null)
            {
                button.targetGraphic.color = selected
                    ? original.pressedColor
                    : original.normalColor;
            }
        }

        private void EnsureConfigured()
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    "ExchangeScreenView must be configured before use.");
            }
        }

        private static string FormatCurrency(long value) =>
            value.ToString(CultureInfo.InvariantCulture);

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value;
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
