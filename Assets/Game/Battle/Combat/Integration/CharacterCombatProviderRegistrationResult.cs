using System;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class CharacterCombatProviderRegistrationResult
    {
        private CharacterCombatProviderRegistrationResult(
            string activeAbilityId,
            int activeCooldownTurns)
        {
            ActiveAbilityId = activeAbilityId;
            ActiveCooldownTurns = activeCooldownTurns;
        }

        public bool HasActiveAbility =>
            !string.IsNullOrEmpty(ActiveAbilityId);
        public string ActiveAbilityId { get; }
        public int ActiveCooldownTurns { get; }

        public static CharacterCombatProviderRegistrationResult
            WithoutActiveAbility()
        {
            return new CharacterCombatProviderRegistrationResult(
                string.Empty,
                activeCooldownTurns: 0);
        }

        public static CharacterCombatProviderRegistrationResult
            WithActiveAbility(
                string activeAbilityId,
                int activeCooldownTurns)
        {
            if (string.IsNullOrWhiteSpace(activeAbilityId))
            {
                throw new ArgumentException(
                    "Active ability ID cannot be null or whitespace.",
                    nameof(activeAbilityId));
            }

            if (activeCooldownTurns < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(activeCooldownTurns));
            }

            return new CharacterCombatProviderRegistrationResult(
                activeAbilityId,
                activeCooldownTurns);
        }
    }
}
