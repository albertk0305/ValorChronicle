using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Localization;
using ValorChronicle.Settings.DTO;

namespace ValorChronicle.UI.Settings
{
    public sealed class SettingsPanelView : MonoBehaviour
    {
        private const int EnglishOptionIndex = 0;
        private const int KoreanOptionIndex = 1;

        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TMP_Text masterVolumeText;
        [SerializeField] private TMP_Text bgmVolumeText;
        [SerializeField] private TMP_Text sfxVolumeText;
        [SerializeField] private TMP_Dropdown languageDropdown;
        [SerializeField] private Toggle speedUp2XToggle;
        [SerializeField] private Toggle speedUp3XToggle;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_InputField redeemCodeInputField;
        [SerializeField] private Button redeemConfirmButton;
        [SerializeField] private GameObject redeemResultOverlay;
        [SerializeField] private Button redeemResultDismissButton;
        [SerializeField] private TMP_Text redeemResultText;

        private bool subscribed;

        public event Action<int> MasterVolumeChanged;
        public event Action<int> BgmVolumeChanged;
        public event Action<int> SfxVolumeChanged;
        public event Action<string> LocaleChanged;
        public event Action<int> BattleSpeedChanged;
        public event Action<string> RedeemCodeRequested;
        public event Action RedeemResultDismissed;
        public event Action CloseRequested;
        public event Action Hidden;

        public bool IsOpen => gameObject.activeSelf;
        public bool IsConfigured =>
            masterVolumeSlider != null
            && bgmVolumeSlider != null
            && sfxVolumeSlider != null
            && masterVolumeText != null
            && bgmVolumeText != null
            && sfxVolumeText != null
            && languageDropdown != null
            && speedUp2XToggle != null
            && speedUp3XToggle != null
            && closeButton != null;
        public bool IsRedeemConfigured =>
            redeemCodeInputField != null
            && redeemConfirmButton != null
            && redeemResultOverlay != null
            && redeemResultDismissButton != null
            && redeemResultText != null;
        public bool IsRedeemResultVisible =>
            redeemResultOverlay != null
            && redeemResultOverlay.activeSelf;
        public TMP_InputField RedeemCodeInputField => redeemCodeInputField;
        public Button RedeemConfirmButton => redeemConfirmButton;
        public GameObject RedeemResultOverlay => redeemResultOverlay;
        public TMP_Text RedeemResultText => redeemResultText;

        private void Awake()
        {
            NormalizeControls();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ResetRedeemUi();
            Hidden?.Invoke();
        }

        public void Configure(
            Slider configuredMasterVolumeSlider,
            Slider configuredBgmVolumeSlider,
            Slider configuredSfxVolumeSlider,
            TMP_Text configuredMasterVolumeText,
            TMP_Text configuredBgmVolumeText,
            TMP_Text configuredSfxVolumeText,
            TMP_Dropdown configuredLanguageDropdown,
            Toggle configuredSpeedUp2XToggle,
            Toggle configuredSpeedUp3XToggle,
            Button configuredCloseButton)
        {
            Unsubscribe();
            masterVolumeSlider = configuredMasterVolumeSlider
                ?? throw new ArgumentNullException(nameof(configuredMasterVolumeSlider));
            bgmVolumeSlider = configuredBgmVolumeSlider
                ?? throw new ArgumentNullException(nameof(configuredBgmVolumeSlider));
            sfxVolumeSlider = configuredSfxVolumeSlider
                ?? throw new ArgumentNullException(nameof(configuredSfxVolumeSlider));
            masterVolumeText = configuredMasterVolumeText
                ?? throw new ArgumentNullException(nameof(configuredMasterVolumeText));
            bgmVolumeText = configuredBgmVolumeText
                ?? throw new ArgumentNullException(nameof(configuredBgmVolumeText));
            sfxVolumeText = configuredSfxVolumeText
                ?? throw new ArgumentNullException(nameof(configuredSfxVolumeText));
            languageDropdown = configuredLanguageDropdown
                ?? throw new ArgumentNullException(nameof(configuredLanguageDropdown));
            speedUp2XToggle = configuredSpeedUp2XToggle
                ?? throw new ArgumentNullException(
                    nameof(configuredSpeedUp2XToggle));
            speedUp3XToggle = configuredSpeedUp3XToggle
                ?? throw new ArgumentNullException(
                    nameof(configuredSpeedUp3XToggle));
            closeButton = configuredCloseButton
                ?? throw new ArgumentNullException(nameof(configuredCloseButton));

            NormalizeControls();
            if (isActiveAndEnabled)
                Subscribe();
        }

        public void ConfigureRedeem(
            TMP_InputField configuredInputField,
            Button configuredConfirmButton,
            GameObject configuredResultOverlay,
            Button configuredResultDismissButton,
            TMP_Text configuredResultText)
        {
            Unsubscribe();
            redeemCodeInputField = configuredInputField
                ?? throw new ArgumentNullException(
                    nameof(configuredInputField));
            redeemConfirmButton = configuredConfirmButton
                ?? throw new ArgumentNullException(
                    nameof(configuredConfirmButton));
            redeemResultOverlay = configuredResultOverlay
                ?? throw new ArgumentNullException(
                    nameof(configuredResultOverlay));
            redeemResultDismissButton = configuredResultDismissButton
                ?? throw new ArgumentNullException(
                    nameof(configuredResultDismissButton));
            redeemResultText = configuredResultText
                ?? throw new ArgumentNullException(
                    nameof(configuredResultText));

            ResetRedeemUi();
            if (isActiveAndEnabled)
                Subscribe();
        }

        public void Display(GameSettingsData settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (!IsConfigured)
                throw new InvalidOperationException("SettingsPanelView is not configured.");

            SetVolume(masterVolumeSlider, masterVolumeText, settings.MasterVolume);
            SetVolume(bgmVolumeSlider, bgmVolumeText, settings.BgmVolume);
            SetVolume(sfxVolumeSlider, sfxVolumeText, settings.SfxVolume);
            languageDropdown.SetValueWithoutNotify(
                string.Equals(settings.LocaleId, LocalizationCultureResolver.KoreanLocale,
                    StringComparison.Ordinal)
                    ? KoreanOptionIndex
                    : EnglishOptionIndex);
            languageDropdown.RefreshShownValue();
            speedUp2XToggle.SetIsOnWithoutNotify(settings.BattleSpeed == 2);
            speedUp3XToggle.SetIsOnWithoutNotify(settings.BattleSpeed == 3);
        }

        public void Open()
        {
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        public void SetRedeemPlaceholder(string text)
        {
            if (redeemCodeInputField?.placeholder is TMP_Text placeholder)
                placeholder.text = text ?? string.Empty;
        }

        public void SetRedeemBusy(bool busy)
        {
            if (redeemCodeInputField != null)
                redeemCodeInputField.interactable = !busy;
            if (redeemConfirmButton != null)
                redeemConfirmButton.interactable = !busy;
        }

        public void ShowRedeemResult(string message)
        {
            if (!IsRedeemConfigured)
                return;

            redeemResultText.text = message ?? string.Empty;
            SetRedeemBusy(true);
            redeemResultOverlay.transform.SetAsLastSibling();
            redeemResultOverlay.SetActive(true);
        }

        public void HideRedeemResult()
        {
            if (redeemResultOverlay != null)
                redeemResultOverlay.SetActive(false);
            SetRedeemBusy(false);
        }

        public void ResetRedeemUi()
        {
            if (!IsRedeemConfigured)
                return;

            redeemResultText.text = string.Empty;
            HideRedeemResult();
        }

        private void NormalizeControls()
        {
            if (!IsConfigured)
                return;

            ConfigureVolumeSlider(masterVolumeSlider);
            ConfigureVolumeSlider(bgmVolumeSlider);
            ConfigureVolumeSlider(sfxVolumeSlider);
        }

        private static void ConfigureVolumeSlider(Slider slider)
        {
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.wholeNumbers = true;
        }

        private static void SetVolume(Slider slider, TMP_Text label, int value)
        {
            int normalized = Mathf.Clamp(value, 0, 100);
            slider.SetValueWithoutNotify(normalized);
            label.text = normalized.ToString();
        }

        private void Subscribe()
        {
            if (subscribed || !IsConfigured)
                return;

            masterVolumeSlider.onValueChanged.AddListener(HandleMasterVolumeChanged);
            bgmVolumeSlider.onValueChanged.AddListener(HandleBgmVolumeChanged);
            sfxVolumeSlider.onValueChanged.AddListener(HandleSfxVolumeChanged);
            languageDropdown.onValueChanged.AddListener(HandleLocaleChanged);
            speedUp2XToggle.onValueChanged.AddListener(
                HandleTwoTimesBattleSpeedChanged);
            speedUp3XToggle.onValueChanged.AddListener(
                HandleThreeTimesBattleSpeedChanged);
            closeButton.onClick.AddListener(HandleCloseRequested);
            if (IsRedeemConfigured)
            {
                redeemConfirmButton.onClick.AddListener(
                    HandleRedeemCodeRequested);
                redeemResultDismissButton.onClick.AddListener(
                    HandleRedeemResultDismissed);
            }
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
                return;

            masterVolumeSlider.onValueChanged.RemoveListener(HandleMasterVolumeChanged);
            bgmVolumeSlider.onValueChanged.RemoveListener(HandleBgmVolumeChanged);
            sfxVolumeSlider.onValueChanged.RemoveListener(HandleSfxVolumeChanged);
            languageDropdown.onValueChanged.RemoveListener(HandleLocaleChanged);
            speedUp2XToggle.onValueChanged.RemoveListener(
                HandleTwoTimesBattleSpeedChanged);
            speedUp3XToggle.onValueChanged.RemoveListener(
                HandleThreeTimesBattleSpeedChanged);
            closeButton.onClick.RemoveListener(HandleCloseRequested);
            if (redeemConfirmButton != null)
            {
                redeemConfirmButton.onClick.RemoveListener(
                    HandleRedeemCodeRequested);
            }
            if (redeemResultDismissButton != null)
            {
                redeemResultDismissButton.onClick.RemoveListener(
                    HandleRedeemResultDismissed);
            }
            subscribed = false;
        }

        private void HandleMasterVolumeChanged(float value)
        {
            int normalized = Mathf.RoundToInt(value);
            masterVolumeText.text = normalized.ToString();
            MasterVolumeChanged?.Invoke(normalized);
        }

        private void HandleBgmVolumeChanged(float value)
        {
            int normalized = Mathf.RoundToInt(value);
            bgmVolumeText.text = normalized.ToString();
            BgmVolumeChanged?.Invoke(normalized);
        }

        private void HandleSfxVolumeChanged(float value)
        {
            int normalized = Mathf.RoundToInt(value);
            sfxVolumeText.text = normalized.ToString();
            SfxVolumeChanged?.Invoke(normalized);
        }

        private void HandleLocaleChanged(int optionIndex)
        {
            LocaleChanged?.Invoke(optionIndex == KoreanOptionIndex
                ? LocalizationCultureResolver.KoreanLocale
                : LocalizationService.EnglishLocale);
        }

        private void HandleTwoTimesBattleSpeedChanged(bool isOn)
        {
            if (isOn)
                speedUp3XToggle.SetIsOnWithoutNotify(false);

            PublishBattleSpeedSelection();
        }

        private void HandleThreeTimesBattleSpeedChanged(bool isOn)
        {
            if (isOn)
                speedUp2XToggle.SetIsOnWithoutNotify(false);

            PublishBattleSpeedSelection();
        }

        private void PublishBattleSpeedSelection()
        {
            BattleSpeedChanged?.Invoke(speedUp3XToggle.isOn
                ? 3
                : speedUp2XToggle.isOn
                    ? 2
                    : 1);
        }

        private void HandleCloseRequested()
        {
            CloseRequested?.Invoke();
        }

        private void HandleRedeemCodeRequested()
        {
            if (!IsRedeemConfigured || IsRedeemResultVisible)
                return;

            RedeemCodeRequested?.Invoke(redeemCodeInputField.text);
        }

        private void HandleRedeemResultDismissed()
        {
            HideRedeemResult();
            RedeemResultDismissed?.Invoke();
        }
    }
}
