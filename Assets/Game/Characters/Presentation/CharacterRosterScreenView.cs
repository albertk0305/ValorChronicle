using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Presentation;
using ValorChronicle.Party.Roster;

namespace ValorChronicle.Characters.Presentation
{
    public sealed class CharacterRosterScreenView : MonoBehaviour
    {
        private VirtualizedCharacterGridView characterGrid;
        private ElementFilterButtonView[] filterButtons =
            Array.Empty<ElementFilterButtonView>();
        private Button levelSortButton;
        private Button awakeningSortButton;
        private TMP_Text goldText;
        private TMP_Text battleRecordsText;
        private bool subscribed;

        public event Action<string> CharacterRequested;
        public event Action<ElementType> ElementFilterRequested;
        public event Action<CharacterRosterSortMode> SortRequested;

        public VirtualizedCharacterGridView CharacterGrid => characterGrid;
        public IReadOnlyList<ElementFilterButtonView> FilterButtons =>
            filterButtons;
        public Button LevelSortButton => levelSortButton;
        public Button AwakeningSortButton => awakeningSortButton;
        public TMP_Text GoldText => goldText;
        public TMP_Text BattleRecordsText => battleRecordsText;
        public bool IsConfigured =>
            characterGrid != null
            && characterGrid.IsConfigured
            && filterButtons.Length == 5
            && Array.TrueForAll(filterButtons, item => item != null)
            && levelSortButton != null
            && awakeningSortButton != null
            && goldText != null
            && battleRecordsText != null;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(
            VirtualizedCharacterGridView configuredGrid,
            ElementFilterButtonView[] configuredFilterButtons,
            Button configuredLevelSortButton,
            Button configuredAwakeningSortButton,
            TMP_Text configuredGoldText,
            TMP_Text configuredBattleRecordsText)
        {
            Unsubscribe();
            characterGrid = configuredGrid;
            filterButtons = configuredFilterButtons
                ?? Array.Empty<ElementFilterButtonView>();
            levelSortButton = configuredLevelSortButton;
            awakeningSortButton = configuredAwakeningSortButton;
            goldText = configuredGoldText;
            battleRecordsText = configuredBattleRecordsText;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        public void BindCurrencies(long gachaCurrency, long battleRecords)
        {
            goldText.text = gachaCurrency.ToString(
                CultureInfo.InvariantCulture);
            battleRecordsText.text = battleRecords.ToString(
                CultureInfo.InvariantCulture);
        }

        public void BindControls(
            ElementType? selectedElement,
            CharacterRosterSortMode sortMode)
        {
            for (int index = 0; index < filterButtons.Length; index++)
            {
                ElementFilterButtonView filter = filterButtons[index];
                filter.SetSelected(
                    selectedElement.HasValue
                    && selectedElement.Value == filter.Element);
            }

            levelSortButton.interactable =
                sortMode != CharacterRosterSortMode.Level;
            awakeningSortButton.interactable =
                sortMode != CharacterRosterSortMode.Awakening;
        }

        public void BindRoster(
            IReadOnlyList<CharacterRosterEntry> roster,
            string selectedCharacterId,
            Func<string, Sprite> faceResolver,
            Func<ElementType, Sprite> elementIconResolver,
            bool resetScroll)
        {
            characterGrid.SetItems(
                roster,
                selectedCharacterId,
                faceResolver,
                elementIconResolver,
                resetScroll);
        }

        public void RefreshSelection(string selectedCharacterId)
        {
            characterGrid.RefreshSelection(selectedCharacterId);
        }

        public void Clear()
        {
            characterGrid?.Clear();
            if (goldText != null)
            {
                goldText.text = string.Empty;
            }

            if (battleRecordsText != null)
            {
                battleRecordsText.text = string.Empty;
            }
        }

        private void Subscribe()
        {
            if (subscribed || !IsConfigured)
            {
                return;
            }

            characterGrid.CharacterRequested += HandleCharacterRequested;
            for (int index = 0; index < filterButtons.Length; index++)
            {
                filterButtons[index].Clicked += HandleFilterRequested;
            }

            levelSortButton.onClick.AddListener(HandleLevelSortRequested);
            awakeningSortButton.onClick.AddListener(
                HandleAwakeningSortRequested);
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            characterGrid.CharacterRequested -= HandleCharacterRequested;
            for (int index = 0; index < filterButtons.Length; index++)
            {
                filterButtons[index].Clicked -= HandleFilterRequested;
            }

            levelSortButton.onClick.RemoveListener(
                HandleLevelSortRequested);
            awakeningSortButton.onClick.RemoveListener(
                HandleAwakeningSortRequested);
            subscribed = false;
        }

        private void HandleCharacterRequested(string characterId)
        {
            CharacterRequested?.Invoke(characterId);
        }

        private void HandleFilterRequested(ElementType element)
        {
            ElementFilterRequested?.Invoke(element);
        }

        private void HandleLevelSortRequested()
        {
            SortRequested?.Invoke(CharacterRosterSortMode.Level);
        }

        private void HandleAwakeningSortRequested()
        {
            SortRequested?.Invoke(CharacterRosterSortMode.Awakening);
        }
    }
}
