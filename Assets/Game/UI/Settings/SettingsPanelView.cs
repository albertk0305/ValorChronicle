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
        [SerializeField] private Toggle speedUpToggle;
        [SerializeField] private Button closeButton;

        private bool subscribed;

        public event Action<int> MasterVolumeChanged;
        public event Action<int> BgmVolumeChanged;
        public event Action<int> SfxVolumeChanged;
        public event Action<string> LocaleChanged;
        public event Action<int> BattleSpeedChanged;
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
            && speedUpToggle != null
            && closeButton != null;

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
            Toggle configuredSpeedUpToggle,
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
            speedUpToggle = configuredSpeedUpToggle
                ?? throw new ArgumentNullException(nameof(configuredSpeedUpToggle));
            closeButton = configuredCloseButton
                ?? throw new ArgumentNullException(nameof(configuredCloseButton));

            NormalizeControls();
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
            speedUpToggle.SetIsOnWithoutNotify(settings.BattleSpeed == 2);
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
            speedUpToggle.onValueChanged.AddListener(HandleBattleSpeedChanged);
            closeButton.onClick.AddListener(HandleCloseRequested);
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
            speedUpToggle.onValueChanged.RemoveListener(HandleBattleSpeedChanged);
            closeButton.onClick.RemoveListener(HandleCloseRequested);
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

        private void HandleBattleSpeedChanged(bool isOn)
        {
            BattleSpeedChanged?.Invoke(isOn ? 2 : 1);
        }

        private void HandleCloseRequested()
        {
            CloseRequested?.Invoke();
        }
    }
}
