using System;
using System.Collections.Generic;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Party.Roster
{
    public static class CharacterRosterQuery
    {
        public static IReadOnlyList<CharacterRosterEntry> Apply(
            IReadOnlyList<CharacterRosterEntry> roster,
            ElementType? selectedElement,
            CharacterRosterSortMode sortMode)
        {
            if (roster == null)
            {
                throw new ArgumentNullException(nameof(roster));
            }

            var result = new List<CharacterRosterEntry>(roster.Count);
            for (int index = 0; index < roster.Count; index++)
            {
                CharacterRosterEntry entry = roster[index]
                    ?? throw new ArgumentException(
                        "Roster entries cannot be null.",
                        nameof(roster));
                if (!selectedElement.HasValue
                    || entry.Element == selectedElement.Value)
                {
                    result.Add(entry);
                }
            }

            switch (sortMode)
            {
                case CharacterRosterSortMode.Level:
                    result.Sort(CompareByLevel);
                    break;
                case CharacterRosterSortMode.Awakening:
                    result.Sort(CompareByAwakening);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(sortMode),
                        sortMode,
                        "Unsupported roster sort mode.");
            }

            return result;
        }

        private static int CompareByLevel(
            CharacterRosterEntry left,
            CharacterRosterEntry right)
        {
            int comparison = right.Level.CompareTo(left.Level);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = right.Awakening.CompareTo(left.Awakening);
            if (comparison != 0)
            {
                return comparison;
            }

            return CompareContentOrderThenId(left, right);
        }

        private static int CompareByAwakening(
            CharacterRosterEntry left,
            CharacterRosterEntry right)
        {
            int comparison = right.Awakening.CompareTo(left.Awakening);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = right.Level.CompareTo(left.Level);
            if (comparison != 0)
            {
                return comparison;
            }

            return CompareContentOrderThenId(left, right);
        }

        private static int CompareContentOrderThenId(
            CharacterRosterEntry left,
            CharacterRosterEntry right)
        {
            int comparison = left.ContentOrder.CompareTo(
                right.ContentOrder);
            return comparison != 0
                ? comparison
                : StringComparer.Ordinal.Compare(
                    left.CharacterId,
                    right.CharacterId);
        }
    }
}
