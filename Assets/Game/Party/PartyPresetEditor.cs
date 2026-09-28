using System;
using System.Collections.Generic;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Party
{
    /// <summary>
    /// Calculates immutable five-slot party preset edits.
    /// </summary>
    public sealed class PartyPresetEditor
    {
        public PartyPresetEditResult Edit(
            IReadOnlyList<string> slots,
            int targetSlotIndex,
            string previewCharacterId)
        {
            string[] result = CreateValidatedCopy(slots);
            ValidateSlotIndex(targetSlotIndex, nameof(targetSlotIndex));

            if (previewCharacterId == null)
            {
                throw new ArgumentNullException(nameof(previewCharacterId));
            }

            string targetCharacterId = result[targetSlotIndex];
            if (string.Equals(
                targetCharacterId,
                previewCharacterId,
                StringComparison.Ordinal))
            {
                return CreateResult(
                    PartyPresetEditOperationType.NoChange,
                    result,
                    null,
                    targetSlotIndex);
            }

            if (previewCharacterId == SaveRules.EmptyId)
            {
                result[targetSlotIndex] = SaveRules.EmptyId;
                return CreateResult(
                    PartyPresetEditOperationType.Clear,
                    result,
                    null,
                    targetSlotIndex);
            }

            int sourceSlotIndex = FindCharacterSlot(
                result,
                previewCharacterId);
            if (sourceSlotIndex < 0)
            {
                result[targetSlotIndex] = previewCharacterId;
                PartyPresetEditOperationType operation =
                    targetCharacterId == SaveRules.EmptyId
                        ? PartyPresetEditOperationType.Place
                        : PartyPresetEditOperationType.Replace;
                return CreateResult(
                    operation,
                    result,
                    null,
                    targetSlotIndex);
            }

            result[targetSlotIndex] = previewCharacterId;
            if (targetCharacterId == SaveRules.EmptyId)
            {
                result[sourceSlotIndex] = SaveRules.EmptyId;
                return CreateResult(
                    PartyPresetEditOperationType.Move,
                    result,
                    sourceSlotIndex,
                    targetSlotIndex);
            }

            result[sourceSlotIndex] = targetCharacterId;
            return CreateResult(
                PartyPresetEditOperationType.Swap,
                result,
                sourceSlotIndex,
                targetSlotIndex);
        }

        public PartyPresetEditResult Clear(
            IReadOnlyList<string> slots,
            int targetSlotIndex)
        {
            return Edit(
                slots,
                targetSlotIndex,
                SaveRules.EmptyId);
        }

        internal static string[] CreateValidatedCopy(
            IReadOnlyList<string> slots)
        {
            if (slots == null)
            {
                throw new ArgumentNullException(nameof(slots));
            }

            if (slots.Count != SaveRules.PartySlotCount)
            {
                throw new ArgumentException(
                    $"Party presets must contain exactly "
                        + $"{SaveRules.PartySlotCount} slots.",
                    nameof(slots));
            }

            var copy = new string[SaveRules.PartySlotCount];
            var characterIds = new HashSet<string>(
                StringComparer.Ordinal);
            for (int index = 0; index < slots.Count; index++)
            {
                string characterId = slots[index];
                if (characterId == null)
                {
                    throw new ArgumentException(
                        "Party slots cannot contain null.",
                        nameof(slots));
                }

                if (characterId != SaveRules.EmptyId
                    && !characterIds.Add(characterId))
                {
                    throw new ArgumentException(
                        "A party preset cannot contain duplicate "
                            + "non-empty character IDs.",
                        nameof(slots));
                }

                copy[index] = characterId;
            }

            return copy;
        }

        internal static void ValidateSlotIndex(
            int slotIndex,
            string parameterName)
        {
            if (slotIndex < 0
                || slotIndex >= SaveRules.PartySlotCount)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    slotIndex,
                    $"Party slot index must be between 0 and "
                        + $"{SaveRules.PartySlotCount - 1}.");
            }
        }

        private static int FindCharacterSlot(
            IReadOnlyList<string> slots,
            string characterId)
        {
            for (int index = 0; index < slots.Count; index++)
            {
                if (string.Equals(
                    slots[index],
                    characterId,
                    StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        private static PartyPresetEditResult CreateResult(
            PartyPresetEditOperationType operationType,
            IReadOnlyList<string> resultingSlots,
            int? sourceSlotIndex,
            int targetSlotIndex)
        {
            return new PartyPresetEditResult(
                operationType,
                resultingSlots,
                sourceSlotIndex,
                targetSlotIndex);
        }
    }
}
