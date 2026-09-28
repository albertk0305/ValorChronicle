using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Party
{
    public sealed class PartyPresetEditResult
    {
        private readonly ReadOnlyCollection<string> resultingSlots;

        internal PartyPresetEditResult(
            PartyPresetEditOperationType operationType,
            IReadOnlyList<string> resultingSlots,
            int? sourceSlotIndex,
            int targetSlotIndex)
        {
            if (!Enum.IsDefined(
                typeof(PartyPresetEditOperationType),
                operationType))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(operationType));
            }

            if (resultingSlots == null)
            {
                throw new ArgumentNullException(nameof(resultingSlots));
            }

            if (resultingSlots.Count != SaveRules.PartySlotCount)
            {
                throw new ArgumentException(
                    $"Party presets must contain exactly "
                        + $"{SaveRules.PartySlotCount} slots.",
                    nameof(resultingSlots));
            }

            OperationType = operationType;
            SourceSlotIndex = sourceSlotIndex;
            TargetSlotIndex = targetSlotIndex;

            var copy = new string[resultingSlots.Count];
            for (int index = 0; index < resultingSlots.Count; index++)
            {
                copy[index] = resultingSlots[index];
            }

            this.resultingSlots = Array.AsReadOnly(copy);
        }

        public PartyPresetEditOperationType OperationType { get; }
        public IReadOnlyList<string> ResultingSlots => resultingSlots;
        public int? SourceSlotIndex { get; }
        public int TargetSlotIndex { get; }
    }
}
