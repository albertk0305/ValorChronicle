using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Core.Logging;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Roster;

namespace ValorChronicle.Party.Presentation
{
    public sealed class CharacterSelectView : MonoBehaviour
    {
        [SerializeField]
        private VirtualizedCharacterGridView characterGrid = null;

        [SerializeField]
        private ElementFilterButtonView[] filterButtons =
            Array.Empty<ElementFilterButtonView>();

        [SerializeField] private Button levelSortButton = null;
        [SerializeField] private Button awakeningSortButton = null;
        [SerializeField] private Image previewCharacterImage = null;
        [SerializeField] private Image previewTypeImage = null;
        [SerializeField] private TMP_Text levelText = null;
        [SerializeField] private TMP_Text awakeningText = null;
        [SerializeField] private TMP_Text hpText = null;
        [SerializeField] private TMP_Text attackText = null;
        [SerializeField] private Button confirmButton = null;
        [SerializeField] private Button secondaryActionButton = null;

        public event Action<string> CharacterRequested;
        public event Action<ElementType> ElementFilterRequested;
        public event Action<CharacterRosterSortMode> SortRequested;
        public event Action ConfirmRequested;
        public event Action SecondaryActionRequested;

        private ElementType? selectedElement;
        private CharacterRosterSortMode sortMode =
            CharacterRosterSortMode.Level;
        private bool interactionEnabled = true;
        private bool hasPreview;

        public IReadOnlyList<CharacterRosterCellView> CellViews =>
            characterGrid.PoolCells;
        public VirtualizedCharacterGridView CharacterGrid => characterGrid;
        public IReadOnlyList<ElementFilterButtonView> FilterButtons =>
            filterButtons;
        public Button LevelSortButton => levelSortButton;
        public Button AwakeningSortButton => awakeningSortButton;
        public Button ConfirmButton => confirmButton;
        public Image PreviewCharacterImage => previewCharacterImage;
        public Image PreviewTypeImage => previewTypeImage;
        public TMP_Text LevelText => levelText;
        public TMP_Text AwakeningText => awakeningText;
        public TMP_Text HpText => hpText;
        public TMP_Text AttackText => attackText;
        public Button SecondaryActionButton => secondaryActionButton;
        public int DisplayedCellCount => characterGrid.BoundItemCount;
        public bool IsInteractionEnabled => interactionEnabled;
        public bool IsConfigured =>
            characterGrid != null
            && characterGrid.IsConfigured
            && filterButtons != null
            && filterButtons.Length == 5
            && Array.TrueForAll(filterButtons, button => button != null)
            && levelSortButton != null
            && awakeningSortButton != null
            && previewCharacterImage != null
            && previewTypeImage != null
            && levelText != null
            && awakeningText != null
            && hpText != null
            && attackText != null
            && confirmButton != null
            && secondaryActionButton != null;

        private void OnEnable()
        {
            characterGrid.CharacterRequested += HandleCharacterClicked;

            for (int index = 0; index < filterButtons.Length; index++)
            {
                filterButtons[index].Clicked += HandleFilterClicked;
            }

            levelSortButton?.onClick.AddListener(HandleLevelSortClicked);
            awakeningSortButton?.onClick.AddListener(
                HandleAwakeningSortClicked);
            confirmButton?.onClick.AddListener(HandleConfirmClicked);
            secondaryActionButton?.onClick.AddListener(
                HandleSecondaryActionClicked);
        }

        private void OnDisable()
        {
            if (characterGrid != null)
            {
                characterGrid.CharacterRequested -= HandleCharacterClicked;
            }

            for (int index = 0; index < filterButtons.Length; index++)
            {
                if (filterButtons[index] != null)
                {
                    filterButtons[index].Clicked -= HandleFilterClicked;
                }
            }

            levelSortButton?.onClick.RemoveListener(HandleLevelSortClicked);
            awakeningSortButton?.onClick.RemoveListener(
                HandleAwakeningSortClicked);
            confirmButton?.onClick.RemoveListener(HandleConfirmClicked);
            secondaryActionButton?.onClick.RemoveListener(
                HandleSecondaryActionClicked);
        }

        public void BindControls(
            ElementType? selectedElement,
            CharacterRosterSortMode sortMode)
        {
            this.selectedElement = selectedElement;
            this.sortMode = sortMode;
            for (int index = 0; index < filterButtons.Length; index++)
            {
                ElementFilterButtonView filter = filterButtons[index];
                filter.SetSelected(
                    selectedElement.HasValue
                    && filter.Element == selectedElement.Value);
            }

            ApplyControlState();
        }

        public void BindCells(
            IReadOnlyList<CharacterRosterEntry> visibleRoster,
            string selectedCharacterId,
            Func<string, Sprite> faceResolver,
            Func<ElementType, Sprite> elementIconResolver,
            bool resetScroll)
        {
            if (visibleRoster == null)
            {
                throw new ArgumentNullException(nameof(visibleRoster));
            }

            characterGrid.SetItems(
                visibleRoster,
                selectedCharacterId,
                faceResolver,
                elementIconResolver,
                resetScroll);
            IReadOnlyList<CharacterRosterCellView> cells =
                characterGrid.PoolCells;
            for (int index = 0; index < cells.Count; index++)
            {
                cells[index].SetAlwaysShowBorder(true);
            }
        }

        public void RefreshCellSelection(string selectedCharacterId)
        {
            characterGrid.RefreshSelection(selectedCharacterId);
        }

        public void BindPreview(
            CharacterRosterEntry entry,
            Sprite previewSprite,
            Sprite elementIcon)
        {
            if (entry == null)
            {
                ClearPreview();
                return;
            }

            SetSprite(previewCharacterImage, previewSprite);
            SetSprite(previewTypeImage, elementIcon);
            levelText.text = entry.Level.ToString(
                CultureInfo.InvariantCulture);
            awakeningText.text = entry.Awakening.ToString(
                CultureInfo.InvariantCulture);
            hpText.text = entry.MaxHp.ToString(CultureInfo.InvariantCulture);
            attackText.text = entry.Attack.ToString(
                CultureInfo.InvariantCulture);
            hasPreview = true;
            ApplyControlState();
        }

        public void ClearPreview()
        {
            SetSprite(previewCharacterImage, null);
            SetSprite(previewTypeImage, null);
            levelText.text = string.Empty;
            awakeningText.text = string.Empty;
            hpText.text = string.Empty;
            attackText.text = string.Empty;
            hasPreview = false;
            ApplyControlState();
        }

        public void ClearAll()
        {
            characterGrid.Clear();
            ClearPreview();
            if (secondaryActionButton != null)
            {
                secondaryActionButton.interactable = false;
            }
        }

        public void SetInteractionEnabled(bool enabled)
        {
            interactionEnabled = enabled;
            characterGrid.SetInteractionEnabled(enabled);
            ApplyControlState();
        }

        private void HandleCharacterClicked(string characterId)
        {
            CharacterRequested?.Invoke(characterId);
        }

        private void HandleFilterClicked(ElementType element)
        {
            ElementFilterRequested?.Invoke(element);
        }

        private void HandleLevelSortClicked()
        {
            SortRequested?.Invoke(CharacterRosterSortMode.Level);
        }

        private void HandleAwakeningSortClicked()
        {
            SortRequested?.Invoke(CharacterRosterSortMode.Awakening);
        }

        private void HandleConfirmClicked()
        {
            if (interactionEnabled && hasPreview)
            {
                ConfirmRequested?.Invoke();
            }
        }

        private void HandleSecondaryActionClicked()
        {
            if (interactionEnabled)
            {
                SecondaryActionRequested?.Invoke();
            }
        }

        private void ApplyControlState()
        {
            for (int index = 0; index < filterButtons.Length; index++)
            {
                ElementFilterButtonView filter = filterButtons[index];
                if (filter != null)
                {
                    filter.SetSelected(
                        selectedElement.HasValue
                        && filter.Element == selectedElement.Value);
                    filter.Button.interactable = interactionEnabled;
                }
            }

            if (levelSortButton != null)
            {
                levelSortButton.interactable = interactionEnabled
                    && sortMode != CharacterRosterSortMode.Level;
            }

            if (awakeningSortButton != null)
            {
                awakeningSortButton.interactable = interactionEnabled
                    && sortMode != CharacterRosterSortMode.Awakening;
            }

            if (confirmButton != null)
            {
                confirmButton.interactable = interactionEnabled
                    && hasPreview;
            }

            if (secondaryActionButton != null)
            {
                secondaryActionButton.interactable = interactionEnabled;
            }
        }

        private static void SetSprite(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            Color color = image.color;
            color.a = sprite == null ? 0f : 1f;
            image.color = color;
        }

        private void OnValidate()
        {
            if (!IsConfigured)
            {
                GameLogger.Warning(
                    "[CharacterSelectView] Requires a virtualized grid, "
                        + "five filters, two sort buttons, preview fields, "
                        + "Confirm, and Secondary Action.",
                    this);
            }
        }
    }
}
