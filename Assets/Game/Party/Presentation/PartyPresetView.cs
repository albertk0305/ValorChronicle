using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Core.Logging;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Party.Presentation
{
    public sealed class PartyPresetView : MonoBehaviour
    {
        [SerializeField]
        private Button selectButton = null;

        [SerializeField]
        private GameObject selectHighlight = null;

        [SerializeField]
        private PartySlotView[] slotViews =
            Array.Empty<PartySlotView>();

        public event Action<int> SelectRequested;
        public event Action<int, int> SlotRequested;

        public int PresetIndex { get; private set; } = -1;
        public bool IsActive { get; private set; }
        public Button SelectButton => selectButton;
        public GameObject SelectHighlight => selectHighlight;
        public IReadOnlyList<PartySlotView> SlotViews => slotViews;
        public bool IsConfigured =>
            selectButton != null
            && selectHighlight != null
            && slotViews != null
            && slotViews.Length == SaveRules.PartySlotCount
            && Array.TrueForAll(slotViews, view => view != null);

        private void OnEnable()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(HandleSelectClick);
            }

            SubscribeToSlots();
        }

        private void OnDisable()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(HandleSelectClick);
            }

            UnsubscribeFromSlots();
        }

        public void Bind(
            int presetIndex,
            IReadOnlyList<string> characterSlotIds,
            bool isActive,
            bool inputEnabled)
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    $"PartyPresetView '{name}' is not fully configured.");
            }

            if (presetIndex < 0
                || presetIndex >= SaveRules.PartyPresetCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(presetIndex));
            }

            if (characterSlotIds == null)
            {
                throw new ArgumentNullException(nameof(characterSlotIds));
            }

            if (characterSlotIds.Count != SaveRules.PartySlotCount)
            {
                throw new ArgumentException(
                    $"Party presets must contain exactly "
                        + $"{SaveRules.PartySlotCount} slots.",
                    nameof(characterSlotIds));
            }

            PresetIndex = presetIndex;
            IsActive = isActive;
            selectHighlight.SetActive(isActive);
            selectButton.interactable = inputEnabled && !isActive;

            for (int slotIndex = 0;
                slotIndex < SaveRules.PartySlotCount;
                slotIndex++)
            {
                slotViews[slotIndex].Bind(
                    presetIndex,
                    slotIndex,
                    characterSlotIds[slotIndex],
                    inputEnabled);
            }
        }

        public void SetInputEnabled(bool inputEnabled)
        {
            if (selectButton != null)
            {
                selectButton.interactable = inputEnabled && !IsActive;
            }

            if (slotViews == null)
            {
                return;
            }

            for (int index = 0; index < slotViews.Length; index++)
            {
                slotViews[index]?.SetInputEnabled(inputEnabled);
            }
        }

        private void HandleSelectClick()
        {
            if (selectButton == null || !selectButton.interactable)
            {
                return;
            }

            SelectRequested?.Invoke(PresetIndex);
        }

        private void HandleSlotClick(int presetIndex, int slotIndex)
        {
            SlotRequested?.Invoke(presetIndex, slotIndex);
        }

        private void SubscribeToSlots()
        {
            if (slotViews == null)
            {
                return;
            }

            for (int index = 0; index < slotViews.Length; index++)
            {
                if (slotViews[index] != null)
                {
                    slotViews[index].Clicked += HandleSlotClick;
                }
            }
        }

        private void UnsubscribeFromSlots()
        {
            if (slotViews == null)
            {
                return;
            }

            for (int index = 0; index < slotViews.Length; index++)
            {
                if (slotViews[index] != null)
                {
                    slotViews[index].Clicked -= HandleSlotClick;
                }
            }
        }

        private void OnValidate()
        {
            if (!IsConfigured)
            {
                GameLogger.Warning(
                    $"[PartyPresetView] '{name}' requires one SelectButton, "
                        + "one SelectHighlight, and exactly "
                        + $"{SaveRules.PartySlotCount} SlotViews.",
                    this);
            }
        }
    }
}
