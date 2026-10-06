using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ValorChronicle.Characters.Presentation
{
    public sealed class CharacterUpgradeScreenView : MonoBehaviour
    {
        private TMP_Text characterNameText;
        private Image characterFullImage;
        private TMP_Text battleRecordsText;
        private Image typeIcon;
        private TMP_Text levelText;
        private TMP_Text awakeningText;
        private TMP_Text attackText;
        private TMP_Text hpText;
        private Image levelUpCostIcon;
        private TMP_Text levelUpCostText;
        private Button levelUpButton;
        private Button returnButton;
        private Button awakeningLookupButton;
        private Button skillsLookupButton;
        private CharacterLevelUpPressHoldInput levelUpPressHoldInput;
        private bool listenersBound;
        private bool busy;
        private bool previewActive;

        public event Action ReturnRequested;
        public event Action LevelUpPressStarted;
        public event Action LevelUpRepeatRequested;
        public event Action LevelUpCommitRequested;
        public event Action LevelUpCancelRequested;
        public event Action AwakeningLookupRequested;
        public event Action SkillsLookupRequested;
        public event Action Disabled;

        public CharacterUpgradePresentationModel CurrentModel { get; private set; }
        public TMP_Text CharacterNameText => characterNameText;
        public Image CharacterFullImage => characterFullImage;
        public TMP_Text BattleRecordsText => battleRecordsText;
        public Image TypeIcon => typeIcon;
        public TMP_Text LevelText => levelText;
        public TMP_Text AwakeningText => awakeningText;
        public TMP_Text AttackText => attackText;
        public TMP_Text HpText => hpText;
        public Image LevelUpCostIcon => levelUpCostIcon;
        public TMP_Text LevelUpCostText => levelUpCostText;
        public Button LevelUpButton => levelUpButton;
        public Button ReturnButton => returnButton;
        public Button AwakeningLookupButton => awakeningLookupButton;
        public Button SkillsLookupButton => skillsLookupButton;
        public CharacterLevelUpPressHoldInput LevelUpPressHoldInput =>
            levelUpPressHoldInput;
        public bool IsBusy => busy;
        public bool IsPreviewActive => previewActive;

        private void OnDisable()
        {
            CancelLevelUpPress(notify: false);
            Disabled?.Invoke();
        }

        public void Configure(
            TMP_Text configuredCharacterNameText,
            Image configuredCharacterFullImage,
            TMP_Text configuredBattleRecordsText,
            Image configuredTypeIcon,
            TMP_Text configuredLevelText,
            TMP_Text configuredAwakeningText,
            TMP_Text configuredAttackText,
            TMP_Text configuredHpText,
            Image configuredLevelUpCostIcon,
            TMP_Text configuredLevelUpCostText,
            Button configuredLevelUpButton,
            Button configuredReturnButton,
            Button configuredAwakeningLookupButton,
            Button configuredSkillsLookupButton)
        {
            UnbindListeners();
            characterNameText = configuredCharacterNameText
                ?? throw new ArgumentNullException(
                    nameof(configuredCharacterNameText));
            characterFullImage = configuredCharacterFullImage
                ?? throw new ArgumentNullException(
                    nameof(configuredCharacterFullImage));
            battleRecordsText = configuredBattleRecordsText
                ?? throw new ArgumentNullException(
                    nameof(configuredBattleRecordsText));
            typeIcon = configuredTypeIcon
                ?? throw new ArgumentNullException(nameof(configuredTypeIcon));
            levelText = configuredLevelText
                ?? throw new ArgumentNullException(nameof(configuredLevelText));
            awakeningText = configuredAwakeningText
                ?? throw new ArgumentNullException(
                    nameof(configuredAwakeningText));
            attackText = configuredAttackText
                ?? throw new ArgumentNullException(nameof(configuredAttackText));
            hpText = configuredHpText
                ?? throw new ArgumentNullException(nameof(configuredHpText));
            levelUpCostIcon = configuredLevelUpCostIcon
                ?? throw new ArgumentNullException(
                    nameof(configuredLevelUpCostIcon));
            levelUpCostText = configuredLevelUpCostText
                ?? throw new ArgumentNullException(
                    nameof(configuredLevelUpCostText));
            levelUpButton = configuredLevelUpButton
                ?? throw new ArgumentNullException(
                    nameof(configuredLevelUpButton));
            returnButton = configuredReturnButton
                ?? throw new ArgumentNullException(
                    nameof(configuredReturnButton));
            awakeningLookupButton = configuredAwakeningLookupButton
                ?? throw new ArgumentNullException(
                    nameof(configuredAwakeningLookupButton));
            skillsLookupButton = configuredSkillsLookupButton
                ?? throw new ArgumentNullException(
                    nameof(configuredSkillsLookupButton));
            levelUpPressHoldInput = levelUpButton.GetComponent<
                CharacterLevelUpPressHoldInput>();
            if (levelUpPressHoldInput == null)
            {
                levelUpPressHoldInput = levelUpButton.gameObject.AddComponent<
                    CharacterLevelUpPressHoldInput>();
            }

            levelUpPressHoldInput.Configure(
                levelUpButton,
                CanStartLevelUpPress);
            BindListeners();
        }

        public void Render(CharacterUpgradePresentationModel model)
        {
            CurrentModel = model
                ?? throw new ArgumentNullException(nameof(model));
            characterNameText.text = model.DisplayName;
            characterFullImage.sprite = model.FullArtSprite;
            characterFullImage.enabled = model.FullArtSprite != null;
            battleRecordsText.text = Format(model.BattleRecords);
            typeIcon.sprite = model.ElementIcon;
            typeIcon.enabled = model.ElementIcon != null;
            levelText.text = Format(model.Level);
            awakeningText.text = Format(model.Awakening);
            attackText.text = Format(model.Attack);
            hpText.text = Format(model.MaxHp);
            levelUpCostText.text = Format(model.NextLevelCost);
            levelUpCostIcon.gameObject.SetActive(model.ShowLevelUpCost);
            levelUpCostText.gameObject.SetActive(model.ShowLevelUpCost);
            RefreshInteractionState();
        }

        public void SetBusy(bool isBusy)
        {
            busy = isBusy;
            RefreshInteractionState();
        }

        public void SetPreviewActive(bool isActive)
        {
            previewActive = isActive;
            RefreshInteractionState();
        }

        public void StopLevelUpRepeating()
        {
            levelUpPressHoldInput?.StopRepeating();
        }

        public void CancelLevelUpPress(bool notify)
        {
            levelUpPressHoldInput?.CancelActivePress(notify);
        }

        public void Clear()
        {
            CurrentModel = null;
            if (characterNameText == null)
            {
                return;
            }

            characterNameText.text = string.Empty;
            characterFullImage.sprite = null;
            characterFullImage.enabled = false;
            battleRecordsText.text = string.Empty;
            typeIcon.sprite = null;
            typeIcon.enabled = false;
            levelText.text = string.Empty;
            awakeningText.text = string.Empty;
            attackText.text = string.Empty;
            hpText.text = string.Empty;
            levelUpCostText.text = string.Empty;
            levelUpCostIcon.gameObject.SetActive(false);
            levelUpCostText.gameObject.SetActive(false);
            RefreshInteractionState();
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

            returnButton.onClick.AddListener(HandleReturnRequested);
            awakeningLookupButton.onClick.AddListener(
                HandleAwakeningLookupRequested);
            skillsLookupButton.onClick.AddListener(
                HandleSkillsLookupRequested);
            levelUpPressHoldInput.PressStarted += HandleLevelUpPressStarted;
            levelUpPressHoldInput.RepeatRequested +=
                HandleLevelUpRepeatRequested;
            levelUpPressHoldInput.CommitRequested +=
                HandleLevelUpCommitRequested;
            levelUpPressHoldInput.CancelRequested +=
                HandleLevelUpCancelRequested;
            listenersBound = true;
        }

        private void UnbindListeners()
        {
            if (!listenersBound)
            {
                return;
            }

            returnButton.onClick.RemoveListener(HandleReturnRequested);
            awakeningLookupButton.onClick.RemoveListener(
                HandleAwakeningLookupRequested);
            skillsLookupButton.onClick.RemoveListener(
                HandleSkillsLookupRequested);
            levelUpPressHoldInput.PressStarted -= HandleLevelUpPressStarted;
            levelUpPressHoldInput.RepeatRequested -=
                HandleLevelUpRepeatRequested;
            levelUpPressHoldInput.CommitRequested -=
                HandleLevelUpCommitRequested;
            levelUpPressHoldInput.CancelRequested -=
                HandleLevelUpCancelRequested;
            listenersBound = false;
        }

        private void HandleReturnRequested()
        {
            if (!busy && !previewActive)
            {
                ReturnRequested?.Invoke();
            }
        }

        private void HandleLevelUpPressStarted()
        {
            LevelUpPressStarted?.Invoke();
        }

        private void HandleLevelUpRepeatRequested()
        {
            LevelUpRepeatRequested?.Invoke();
        }

        private void HandleLevelUpCommitRequested()
        {
            LevelUpCommitRequested?.Invoke();
        }

        private void HandleLevelUpCancelRequested()
        {
            LevelUpCancelRequested?.Invoke();
        }

        private void HandleAwakeningLookupRequested()
        {
            if (!busy && !previewActive && CurrentModel != null)
            {
                AwakeningLookupRequested?.Invoke();
            }
        }

        private void HandleSkillsLookupRequested()
        {
            if (!busy && !previewActive && CurrentModel != null)
            {
                SkillsLookupRequested?.Invoke();
            }
        }

        private bool CanStartLevelUpPress()
        {
            return !busy
                && !previewActive
                && CurrentModel?.CanLevelUp == true;
        }

        private void RefreshInteractionState()
        {
            if (levelUpButton != null)
            {
                levelUpButton.interactable = !busy
                    && CurrentModel?.CanLevelUp == true;
            }

            if (returnButton != null)
            {
                returnButton.interactable = !busy && !previewActive;
            }

            bool lookupEnabled = !busy
                && !previewActive
                && CurrentModel != null;
            if (awakeningLookupButton != null)
            {
                awakeningLookupButton.interactable = lookupEnabled;
            }

            if (skillsLookupButton != null)
            {
                skillsLookupButton.interactable = lookupEnabled;
            }
        }

        private static string Format(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
