using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ValorChronicle.Gacha.Presentation
{
    public sealed class GachaScreenView : MonoBehaviour
    {
        private TMP_Text heroTokensText;
        private TMP_Text relicTokensText;
        private TMP_Text gachaCurrencyText;
        private TMP_Text characterPityText;
        private TMP_Text relicPityText;
        private TMP_Text characterSingleCostText;
        private TMP_Text characterTenCostText;
        private TMP_Text relicSingleCostText;
        private TMP_Text relicTenCostText;
        private Button characterSingleButton;
        private Button characterTenButton;
        private Button relicSingleButton;
        private Button relicTenButton;
        private Button settingButton;
        private Button exchangeButton;
        private GameObject infoPanel;
        private GameObject gachaPresentation;

        public TMP_Text HeroTokensText => heroTokensText;
        public TMP_Text RelicTokensText => relicTokensText;
        public TMP_Text GachaCurrencyText => gachaCurrencyText;
        public TMP_Text CharacterPityText => characterPityText;
        public TMP_Text RelicPityText => relicPityText;
        public TMP_Text CharacterSingleCostText => characterSingleCostText;
        public TMP_Text CharacterTenCostText => characterTenCostText;
        public TMP_Text RelicSingleCostText => relicSingleCostText;
        public TMP_Text RelicTenCostText => relicTenCostText;
        public Button CharacterSingleButton => characterSingleButton;
        public Button CharacterTenButton => characterTenButton;
        public Button RelicSingleButton => relicSingleButton;
        public Button RelicTenButton => relicTenButton;
        public Button SettingButton => settingButton;
        public Button ExchangeButton => exchangeButton;
        public GameObject InfoPanel => infoPanel;
        public GameObject GachaPresentation => gachaPresentation;

        public bool IsConfigured =>
            heroTokensText != null
            && relicTokensText != null
            && gachaCurrencyText != null
            && characterPityText != null
            && relicPityText != null
            && characterSingleCostText != null
            && characterTenCostText != null
            && relicSingleCostText != null
            && relicTenCostText != null
            && characterSingleButton != null
            && characterTenButton != null
            && relicSingleButton != null
            && relicTenButton != null
            && settingButton != null
            && exchangeButton != null
            && infoPanel != null
            && gachaPresentation != null;

        public void Configure(
            TMP_Text configuredHeroTokensText,
            TMP_Text configuredRelicTokensText,
            TMP_Text configuredGachaCurrencyText,
            TMP_Text configuredCharacterPityText,
            TMP_Text configuredRelicPityText,
            TMP_Text configuredCharacterSingleCostText,
            TMP_Text configuredCharacterTenCostText,
            TMP_Text configuredRelicSingleCostText,
            TMP_Text configuredRelicTenCostText,
            Button configuredCharacterSingleButton,
            Button configuredCharacterTenButton,
            Button configuredRelicSingleButton,
            Button configuredRelicTenButton,
            Button configuredSettingButton,
            Button configuredExchangeButton,
            GameObject configuredInfoPanel,
            GameObject configuredGachaPresentation)
        {
            heroTokensText = Require(
                configuredHeroTokensText,
                nameof(configuredHeroTokensText));
            relicTokensText = Require(
                configuredRelicTokensText,
                nameof(configuredRelicTokensText));
            gachaCurrencyText = Require(
                configuredGachaCurrencyText,
                nameof(configuredGachaCurrencyText));
            characterPityText = Require(
                configuredCharacterPityText,
                nameof(configuredCharacterPityText));
            relicPityText = Require(
                configuredRelicPityText,
                nameof(configuredRelicPityText));
            characterSingleCostText = Require(
                configuredCharacterSingleCostText,
                nameof(configuredCharacterSingleCostText));
            characterTenCostText = Require(
                configuredCharacterTenCostText,
                nameof(configuredCharacterTenCostText));
            relicSingleCostText = Require(
                configuredRelicSingleCostText,
                nameof(configuredRelicSingleCostText));
            relicTenCostText = Require(
                configuredRelicTenCostText,
                nameof(configuredRelicTenCostText));
            characterSingleButton = Require(
                configuredCharacterSingleButton,
                nameof(configuredCharacterSingleButton));
            characterTenButton = Require(
                configuredCharacterTenButton,
                nameof(configuredCharacterTenButton));
            relicSingleButton = Require(
                configuredRelicSingleButton,
                nameof(configuredRelicSingleButton));
            relicTenButton = Require(
                configuredRelicTenButton,
                nameof(configuredRelicTenButton));
            settingButton = Require(
                configuredSettingButton,
                nameof(configuredSettingButton));
            exchangeButton = Require(
                configuredExchangeButton,
                nameof(configuredExchangeButton));
            infoPanel = Require(
                configuredInfoPanel,
                nameof(configuredInfoPanel));
            gachaPresentation = Require(
                configuredGachaPresentation,
                nameof(configuredGachaPresentation));

            infoPanel.SetActive(false);
            gachaPresentation.SetActive(false);
            SetUnavailable();
        }

        public void Bind(GachaScreenDisplayData data)
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    "GachaScreenView must be configured before binding.");
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            heroTokensText.text = FormatCurrency(data.HeroTokens);
            relicTokensText.text = FormatCurrency(data.RelicTokens);
            gachaCurrencyText.text = FormatCurrency(data.GachaCurrency);
            BindBanner(
                data.CharacterBanner,
                characterPityText,
                characterSingleCostText,
                characterTenCostText);
            BindBanner(
                data.RelicBanner,
                relicPityText,
                relicSingleCostText,
                relicTenCostText);
        }

        public void SetPullButtonsInteractable(
            bool characterSingle,
            bool characterTen,
            bool relicSingle,
            bool relicTen)
        {
            SetInteractable(characterSingleButton, characterSingle);
            SetInteractable(characterTenButton, characterTen);
            SetInteractable(relicSingleButton, relicSingle);
            SetInteractable(relicTenButton, relicTen);
            SetInteractable(settingButton, false);
            SetInteractable(exchangeButton, false);
        }

        public void SetUnavailable()
        {
            SetText(heroTokensText, string.Empty);
            SetText(relicTokensText, string.Empty);
            SetText(gachaCurrencyText, string.Empty);
            SetText(characterPityText, string.Empty);
            SetText(relicPityText, string.Empty);
            SetText(characterSingleCostText, string.Empty);
            SetText(characterTenCostText, string.Empty);
            SetText(relicSingleCostText, string.Empty);
            SetText(relicTenCostText, string.Empty);
            SetActionButtonsInteractable(false);
        }

        private void SetActionButtonsInteractable(bool interactable)
        {
            SetPullButtonsInteractable(
                interactable,
                interactable,
                interactable,
                interactable);
        }

        private static void BindBanner(
            GachaBannerDisplayData banner,
            TMP_Text pityText,
            TMP_Text singleCostText,
            TMP_Text tenCostText)
        {
            pityText.text = string.Format(
                CultureInfo.InvariantCulture,
                "{0} / {1}",
                banner.PityCount,
                banner.PityMaximum);
            singleCostText.text = "X" + banner.SinglePullCost.ToString(
                CultureInfo.InvariantCulture);
            tenCostText.text = "X" + banner.TenPullCost.ToString(
                CultureInfo.InvariantCulture);
        }

        private static string FormatCurrency(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static T Require<T>(T value, string parameterName)
            where T : UnityEngine.Object
        {
            return value != null
                ? value
                : throw new ArgumentNullException(parameterName);
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private static void SetInteractable(
            Selectable selectable,
            bool interactable)
        {
            if (selectable != null)
            {
                selectable.interactable = interactable;
            }
        }
    }
}
