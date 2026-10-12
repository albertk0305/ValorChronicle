using System;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Save.Rules;
using ValorChronicle.Relics.Stats;

namespace ValorChronicle.Party.Battle
{
    public sealed class BattlePartyMemberInput
    {
        public BattlePartyMemberInput(
            string characterId,
            int partySlotIndex,
            int level,
            int awakening,
            CharacterDefinition characterDefinition,
            RelicStatBonusSummary relicStatBonuses = null)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                throw new ArgumentException(
                    "Character ID cannot be null or whitespace.",
                    nameof(characterId));
            }

            if (partySlotIndex < 0
                || partySlotIndex >= SaveRules.PartySlotCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(partySlotIndex));
            }

            if (level < SaveRules.CharacterMinLevel
                || level > SaveRules.CharacterMaxLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            if (awakening < SaveRules.CharacterMinAwakening
                || awakening > SaveRules.CharacterMaxAwakening)
            {
                throw new ArgumentOutOfRangeException(nameof(awakening));
            }

            if (characterDefinition == null)
            {
                throw new ArgumentNullException(nameof(characterDefinition));
            }

            if (!string.Equals(
                characterId,
                characterDefinition.Id,
                StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Character ID must match CharacterDefinition.Id.",
                    nameof(characterDefinition));
            }

            CharacterId = characterId;
            PartySlotIndex = partySlotIndex;
            Level = level;
            Awakening = awakening;
            CharacterDefinition = characterDefinition;
            RelicStatBonuses = relicStatBonuses
                ?? RelicStatBonusSummary.None;
        }

        public string CharacterId { get; }
        public int PartySlotIndex { get; }
        public int Level { get; }
        public int Awakening { get; }
        public CharacterDefinition CharacterDefinition { get; }
        public RelicStatBonusSummary RelicStatBonuses { get; }
    }
}
