using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Relics
{
    public enum RelicEquipEditOperationType
    {
        None = 0,
        NoChange = 1,
        Equip = 2,
        Clear = 3
    }

    /// <summary>Contains an immutable calculated slot edit.</summary>
    public sealed class RelicEquipEditResult
    {
        private readonly ReadOnlyCollection<string> resultingSlots;

        internal RelicEquipEditResult(
            RelicEquipEditOperationType operationType,
            RelicEquipAvailability availability,
            IReadOnlyList<string> resultingSlots)
        {
            OperationType = operationType;
            Availability = availability
                ?? throw new ArgumentNullException(nameof(availability));

            var copy = new string[resultingSlots?.Count ?? 0];
            if (resultingSlots != null)
            {
                for (int index = 0;
                    index < resultingSlots.Count;
                    index++)
                {
                    copy[index] = resultingSlots[index];
                }
            }

            this.resultingSlots = Array.AsReadOnly(copy);
        }

        public bool IsSuccess => Availability.CanSelect;
        public RelicEquipEditOperationType OperationType { get; }
        public RelicEquipAvailability Availability { get; }
        public IReadOnlyList<string> ResultingSlots => resultingSlots;
    }
}
