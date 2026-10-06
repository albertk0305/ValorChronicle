using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ValorChronicle.Characters.Presentation
{
    public sealed class CharacterLookupScreenView : MonoBehaviour
    {
        private Button returnButton;
        private Button awakeningModeButton;
        private Button skillsModeButton;
        private TMP_Text descriptionText;
        private GameObject awakeningLookupRoot;
        private GameObject skillLookupRoot;
        private Button[] awakeningButtons;
        private Button[] skillButtons;
        private UnityAction[] awakeningActions;
        private UnityAction[] skillActions;
        private bool listenersBound;

        public event Action ReturnRequested;
        public event Action<CharacterLookupMode> ModeRequested;
        public event Action<int> AwakeningStageRequested;
        public event Action<CharacterLookupSkillType> SkillTypeRequested;

        public CharacterLookupPresentationModel CurrentModel { get; private set; }
        public Button ReturnButton => returnButton;
        public Button AwakeningModeButton => awakeningModeButton;
        public Button SkillsModeButton => skillsModeButton;
        public TMP_Text DescriptionText => descriptionText;
        public GameObject AwakeningLookupRoot => awakeningLookupRoot;
        public GameObject SkillLookupRoot => skillLookupRoot;
        public IReadOnlyList<Button> AwakeningButtons => awakeningButtons;
        public IReadOnlyList<Button> SkillButtons => skillButtons;

        public void Configure(
            Button configuredReturnButton,
            Button configuredAwakeningModeButton,
            Button configuredSkillsModeButton,
            TMP_Text configuredDescriptionText,
            GameObject configuredAwakeningLookupRoot,
            GameObject configuredSkillLookupRoot,
            Button[] configuredAwakeningButtons,
            Button[] configuredSkillButtons)
        {
            UnbindListeners();
            returnButton = configuredReturnButton
                ?? throw new ArgumentNullException(
                    nameof(configuredReturnButton));
            awakeningModeButton = configuredAwakeningModeButton
                ?? throw new ArgumentNullException(
                    nameof(configuredAwakeningModeButton));
            skillsModeButton = configuredSkillsModeButton
                ?? throw new ArgumentNullException(
                    nameof(configuredSkillsModeButton));
            descriptionText = configuredDescriptionText
                ?? throw new ArgumentNullException(
                    nameof(configuredDescriptionText));
            awakeningLookupRoot = configuredAwakeningLookupRoot
                ?? throw new ArgumentNullException(
                    nameof(configuredAwakeningLookupRoot));
            skillLookupRoot = configuredSkillLookupRoot
                ?? throw new ArgumentNullException(
                    nameof(configuredSkillLookupRoot));
            awakeningButtons = RequireButtons(
                configuredAwakeningButtons,
                6,
                nameof(configuredAwakeningButtons));
            skillButtons = RequireButtons(
                configuredSkillButtons,
                5,
                nameof(configuredSkillButtons));
            descriptionText.text = string.Empty;
            BindListeners();
        }

        public void Render(CharacterLookupPresentationModel model)
        {
            CurrentModel = model
                ?? throw new ArgumentNullException(nameof(model));
            bool isAwakening = model.Mode == CharacterLookupMode.Awakening;
            awakeningLookupRoot.SetActive(isAwakening);
            skillLookupRoot.SetActive(!isAwakening);
            awakeningModeButton.interactable = !isAwakening;
            skillsModeButton.interactable = isAwakening;

            for (int index = 0; index < awakeningButtons.Length; index++)
            {
                awakeningButtons[index].interactable =
                    index + 1 != model.SelectedAwakeningStage;
            }

            for (int index = 0; index < skillButtons.Length; index++)
            {
                skillButtons[index].interactable =
                    index != (int)model.SelectedSkillType;
            }

            SetDescription(model.Description);
        }

        public void SetDescription(string text)
        {
            descriptionText.text = text ?? string.Empty;
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
            awakeningModeButton.onClick.AddListener(
                HandleAwakeningModeRequested);
            skillsModeButton.onClick.AddListener(HandleSkillsModeRequested);

            awakeningActions = new UnityAction[awakeningButtons.Length];
            for (int index = 0; index < awakeningButtons.Length; index++)
            {
                int stage = index + 1;
                awakeningActions[index] = () =>
                    AwakeningStageRequested?.Invoke(stage);
                awakeningButtons[index].onClick.AddListener(
                    awakeningActions[index]);
            }

            skillActions = new UnityAction[skillButtons.Length];
            for (int index = 0; index < skillButtons.Length; index++)
            {
                CharacterLookupSkillType skillType =
                    (CharacterLookupSkillType)index;
                skillActions[index] = () =>
                    SkillTypeRequested?.Invoke(skillType);
                skillButtons[index].onClick.AddListener(skillActions[index]);
            }

            listenersBound = true;
        }

        private void UnbindListeners()
        {
            if (!listenersBound)
            {
                return;
            }

            returnButton.onClick.RemoveListener(HandleReturnRequested);
            awakeningModeButton.onClick.RemoveListener(
                HandleAwakeningModeRequested);
            skillsModeButton.onClick.RemoveListener(HandleSkillsModeRequested);

            for (int index = 0; index < awakeningButtons.Length; index++)
            {
                awakeningButtons[index].onClick.RemoveListener(
                    awakeningActions[index]);
            }

            for (int index = 0; index < skillButtons.Length; index++)
            {
                skillButtons[index].onClick.RemoveListener(
                    skillActions[index]);
            }

            awakeningActions = null;
            skillActions = null;
            listenersBound = false;
        }

        private void HandleReturnRequested()
        {
            ReturnRequested?.Invoke();
        }

        private void HandleAwakeningModeRequested()
        {
            ModeRequested?.Invoke(CharacterLookupMode.Awakening);
        }

        private void HandleSkillsModeRequested()
        {
            ModeRequested?.Invoke(CharacterLookupMode.Skills);
        }

        private static Button[] RequireButtons(
            Button[] buttons,
            int expectedCount,
            string argumentName)
        {
            if (buttons == null || buttons.Length != expectedCount)
            {
                throw new ArgumentException(
                    $"Exactly {expectedCount} buttons are required.",
                    argumentName);
            }

            var result = new Button[buttons.Length];
            for (int index = 0; index < buttons.Length; index++)
            {
                result[index] = buttons[index]
                    ?? throw new ArgumentException(
                        $"Button {index} is missing.",
                        argumentName);
            }

            return result;
        }
    }
}
