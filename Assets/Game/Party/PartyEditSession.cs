using System;
using System.Collections.Generic;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Party
{
    public enum PartyEditSecondaryAction
    {
        Revert = 0,
        Clear = 1,
        CloseWithoutChange = 2
    }

    /// <summary>
    /// Holds temporary state for editing one slot without mutating save data.
    /// </summary>
    public sealed class PartyEditSession
    {
        private readonly PartyPresetEditor editor;
        private readonly string[] originalSlots;

        public PartyEditSession(
            PartyPresetEditor editor,
            int presetIndex,
            int slotIndex,
            IReadOnlyList<string> slots)
        {
            this.editor = editor
                ?? throw new ArgumentNullException(nameof(editor));

            if (presetIndex < 0
                || presetIndex >= SaveRules.PartyPresetCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(presetIndex),
                    presetIndex,
                    $"Party preset index must be between 0 and "
                        + $"{SaveRules.PartyPresetCount - 1}.");
            }

            PartyPresetEditor.ValidateSlotIndex(
                slotIndex,
                nameof(slotIndex));

            originalSlots = PartyPresetEditor.CreateValidatedCopy(slots);
            PresetIndex = presetIndex;
            SlotIndex = slotIndex;
            OriginalCharacterId = originalSlots[slotIndex];
            PreviewCharacterId = OriginalCharacterId;
        }

        public int PresetIndex { get; }
        public int SlotIndex { get; }
        public string OriginalCharacterId { get; }
        public string PreviewCharacterId { get; private set; }

        public PartyEditSecondaryAction SecondaryAction
        {
            get
            {
                if (!string.Equals(
                    PreviewCharacterId,
                    OriginalCharacterId,
                    StringComparison.Ordinal))
                {
                    return PartyEditSecondaryAction.Revert;
                }

                return OriginalCharacterId == SaveRules.EmptyId
                    ? PartyEditSecondaryAction.CloseWithoutChange
                    : PartyEditSecondaryAction.Clear;
            }
        }

        public void SetPreviewCharacterId(string characterId)
        {
            PreviewCharacterId = characterId
                ?? throw new ArgumentNullException(nameof(characterId));
        }

        public void Revert()
        {
            PreviewCharacterId = OriginalCharacterId;
        }

        public PartyPresetEditResult Confirm()
        {
            return editor.Edit(
                originalSlots,
                SlotIndex,
                PreviewCharacterId);
        }
    }
}
