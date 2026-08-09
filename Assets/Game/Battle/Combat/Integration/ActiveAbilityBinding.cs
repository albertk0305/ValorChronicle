using System;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class ActiveAbilityBinding
    {
        public ActiveAbilityBinding(
            int activeAbilityIndex,
            int partySlotIndex,
            string characterId,
            string activeAbilityId)
        {
            if (activeAbilityIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(activeAbilityIndex));
            }

            if (partySlotIndex < CharacterBattleState.MinimumPartySlotIndex
                || partySlotIndex
                    > CharacterBattleState.MaximumPartySlotIndex)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(partySlotIndex));
            }

            ValidateId(characterId, nameof(characterId));
            ValidateId(activeAbilityId, nameof(activeAbilityId));
            ActiveAbilityIndex = activeAbilityIndex;
            PartySlotIndex = partySlotIndex;
            CharacterId = characterId;
            ActiveAbilityId = activeAbilityId;
        }

        public int ActiveAbilityIndex { get; }
        public int PartySlotIndex { get; }
        public string CharacterId { get; }
        public string ActiveAbilityId { get; }

        private static void ValidateId(string id, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(
                    "ID cannot be null or whitespace.",
                    parameterName);
            }
        }
    }
}
