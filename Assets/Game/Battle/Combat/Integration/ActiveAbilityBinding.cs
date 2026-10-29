using System;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;

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

    public interface IActiveAbilityCooldownReducer
    {
        int ReduceRemainingCooldown(int amount);
    }

    public sealed class ActiveAbilityCooldownReductionHandle
        : IActiveAbilityCooldownReducer
    {
        private ActiveAbilityRuntimeState runtimeState;

        public ActiveAbilityCooldownReductionHandle(
            string characterId,
            int partySlotIndex)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                throw new ArgumentException(
                    "Character ID cannot be null or whitespace.",
                    nameof(characterId));
            }

            if (partySlotIndex < CharacterBattleState.MinimumPartySlotIndex
                || partySlotIndex
                    > CharacterBattleState.MaximumPartySlotIndex)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(partySlotIndex));
            }

            CharacterId = characterId;
            PartySlotIndex = partySlotIndex;
        }

        public string CharacterId { get; }
        public int PartySlotIndex { get; }
        public bool IsBound => runtimeState != null;

        public int ReduceRemainingCooldown(int amount)
        {
            if (runtimeState == null)
            {
                throw new InvalidOperationException(
                    "The active ability cooldown reducer is not bound to "
                        + "a battle runtime state.");
            }

            return runtimeState.ReduceRemainingCooldown(amount);
        }

        internal void Bind(ActiveAbilityRuntimeState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (runtimeState != null)
            {
                throw new InvalidOperationException(
                    "The active ability cooldown reducer is already bound.");
            }

            runtimeState = state;
        }
    }
}
