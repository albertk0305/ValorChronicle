using System;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Party.Roster
{
    public sealed class CharacterRosterEntry
    {
        public CharacterRosterEntry(
            string characterId,
            ElementType element,
            int level,
            int awakening,
            long maxHp,
            long attack)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                throw new ArgumentException(
                    "Character ID is required.",
                    nameof(characterId));
            }

            CharacterId = characterId;
            Element = element;
            Level = level;
            Awakening = awakening;
            MaxHp = maxHp;
            Attack = attack;
        }

        public string CharacterId { get; }
        public ElementType Element { get; }
        public int Level { get; }
        public int Awakening { get; }
        public long MaxHp { get; }
        public long Attack { get; }
    }
}
