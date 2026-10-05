using System;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Core.Logging;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Party.Presentation
{
    public sealed class PartySlotView : MonoBehaviour
    {
        [SerializeField]
        private Button button = null;

        [SerializeField]
        private Image characterImage = null;

        [SerializeField]
        private Image typeImage = null;

        public event Action<int, int> Clicked;

        public int PresetIndex { get; private set; } = -1;
        public int SlotIndex { get; private set; } = -1;
        public string CharacterId { get; private set; } = SaveRules.EmptyId;
        public bool IsEmpty => CharacterId == SaveRules.EmptyId;
        public Button Button => button;
        public Image CharacterImage => characterImage;
        public Image TypeImage => typeImage;
        public bool IsConfigured =>
            button != null && characterImage != null && typeImage != null;

        private void OnEnable()
        {
            if (button != null)
            {
                button.onClick.AddListener(HandleClick);
            }
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        public void Bind(
            int presetIndex,
            int slotIndex,
            string characterId,
            bool inputEnabled,
            Sprite characterSprite = null,
            Sprite typeSprite = null)
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    $"PartySlotView '{name}' is not fully configured.");
            }

            if (presetIndex < 0
                || presetIndex >= SaveRules.PartyPresetCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(presetIndex));
            }

            if (slotIndex < 0
                || slotIndex >= SaveRules.PartySlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            }

            CharacterId = characterId
                ?? throw new ArgumentNullException(nameof(characterId));
            PresetIndex = presetIndex;
            SlotIndex = slotIndex;
            button.interactable = inputEnabled;

            SetSprite(
                characterImage,
                IsEmpty ? null : characterSprite);
            SetSprite(typeImage, IsEmpty ? null : typeSprite);
        }

        public void SetInputEnabled(bool inputEnabled)
        {
            if (button != null)
            {
                button.interactable = inputEnabled;
            }
        }

        private void HandleClick()
        {
            if (button == null || !button.interactable)
            {
                return;
            }

            Clicked?.Invoke(PresetIndex, SlotIndex);
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
                    $"[PartySlotView] '{name}' requires Button, "
                        + "CharacterImage, and TypeImage references.",
                    this);
            }
        }
    }
}
