using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;

namespace ValorChronicle.Battle.Combat.Integration
{
    public interface ICharacterCombatStatusSource
    {
        string StatusId { get; }
        string CharacterId { get; }
        int PartySlotIndex { get; }
        int CurrentCount { get; }
        bool IsVisible { get; }
    }

    public sealed class CharacterCombatProviderRegistrationResult
    {
        private CharacterCombatProviderRegistrationResult(
            string activeAbilityId,
            int activeCooldownTurns,
            IReadOnlyList<ICombatTriggerRule> triggerRules,
            IReadOnlyList<CombatResourceRequirement> resourceRequirements,
            IReadOnlyList<ICharacterCombatStatusSource> combatStatusSources,
            IReadOnlyList<InitialPartyEffectDefinition> initialPartyEffects)
        {
            ActiveAbilityId = activeAbilityId;
            ActiveCooldownTurns = activeCooldownTurns;
            TriggerRules = CopyTriggerRules(triggerRules);
            ResourceRequirements = CopyResourceRequirements(
                resourceRequirements);
            CombatStatusSources = CopyCombatStatusSources(
                combatStatusSources);
            InitialPartyEffects = CopyInitialPartyEffects(
                initialPartyEffects);
        }

        public bool HasActiveAbility =>
            !string.IsNullOrEmpty(ActiveAbilityId);
        public string ActiveAbilityId { get; }
        public int ActiveCooldownTurns { get; }
        public IReadOnlyList<ICombatTriggerRule> TriggerRules { get; }
        public IReadOnlyList<CombatResourceRequirement>
            ResourceRequirements { get; }
        public IReadOnlyList<ICharacterCombatStatusSource>
            CombatStatusSources { get; }
        public IReadOnlyList<InitialPartyEffectDefinition>
            InitialPartyEffects { get; }

        public static CharacterCombatProviderRegistrationResult
            WithoutActiveAbility()
        {
            return WithoutActiveAbility(
                Array.Empty<ICombatTriggerRule>(),
                Array.Empty<CombatResourceRequirement>());
        }

        public static CharacterCombatProviderRegistrationResult
            WithoutActiveAbility(
                IReadOnlyList<ICombatTriggerRule> triggerRules,
                IReadOnlyList<CombatResourceRequirement>
                    resourceRequirements)
        {
            return new CharacterCombatProviderRegistrationResult(
                string.Empty,
                activeCooldownTurns: 0,
                triggerRules,
                resourceRequirements,
                Array.Empty<ICharacterCombatStatusSource>(),
                Array.Empty<InitialPartyEffectDefinition>());
        }

        public static CharacterCombatProviderRegistrationResult
            WithoutActiveAbility(
                IReadOnlyList<ICombatTriggerRule> triggerRules,
                IReadOnlyList<CombatResourceRequirement>
                    resourceRequirements,
                IReadOnlyList<ICharacterCombatStatusSource>
                    combatStatusSources)
        {
            return new CharacterCombatProviderRegistrationResult(
                string.Empty,
                activeCooldownTurns: 0,
                triggerRules,
                resourceRequirements,
                combatStatusSources,
                Array.Empty<InitialPartyEffectDefinition>());
        }

        public static CharacterCombatProviderRegistrationResult
            WithActiveAbility(
                string activeAbilityId,
                int activeCooldownTurns)
        {
            return WithActiveAbility(
                activeAbilityId,
                activeCooldownTurns,
                Array.Empty<ICombatTriggerRule>(),
                Array.Empty<CombatResourceRequirement>());
        }

        public static CharacterCombatProviderRegistrationResult
            WithActiveAbility(
                string activeAbilityId,
                int activeCooldownTurns,
                IReadOnlyList<ICombatTriggerRule> triggerRules,
                IReadOnlyList<CombatResourceRequirement>
                    resourceRequirements)
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
                activeCooldownTurns,
                triggerRules,
                resourceRequirements,
                Array.Empty<ICharacterCombatStatusSource>(),
                Array.Empty<InitialPartyEffectDefinition>());
        }

        public static CharacterCombatProviderRegistrationResult
            WithActiveAbility(
                string activeAbilityId,
                int activeCooldownTurns,
                IReadOnlyList<ICombatTriggerRule> triggerRules,
                IReadOnlyList<CombatResourceRequirement>
                    resourceRequirements,
                IReadOnlyList<ICharacterCombatStatusSource>
                    combatStatusSources)
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
                activeCooldownTurns,
                triggerRules,
                resourceRequirements,
                combatStatusSources,
                Array.Empty<InitialPartyEffectDefinition>());
        }

        public CharacterCombatProviderRegistrationResult
            WithInitialPartyEffects(
                IReadOnlyList<InitialPartyEffectDefinition>
                    initialPartyEffects)
        {
            return new CharacterCombatProviderRegistrationResult(
                ActiveAbilityId,
                ActiveCooldownTurns,
                TriggerRules,
                ResourceRequirements,
                CombatStatusSources,
                initialPartyEffects);
        }

        private static ReadOnlyCollection<ICombatTriggerRule>
            CopyTriggerRules(IReadOnlyList<ICombatTriggerRule> triggerRules)
        {
            if (triggerRules == null)
            {
                throw new ArgumentNullException(nameof(triggerRules));
            }

            var copy = new ICombatTriggerRule[triggerRules.Count];
            for (int index = 0; index < triggerRules.Count; index++)
            {
                copy[index] = triggerRules[index]
                    ?? throw new ArgumentException(
                        $"Trigger rule entry {index} is null.",
                        nameof(triggerRules));
            }

            return Array.AsReadOnly(copy);
        }

        private static ReadOnlyCollection<CombatResourceRequirement>
            CopyResourceRequirements(
                IReadOnlyList<CombatResourceRequirement>
                    resourceRequirements)
        {
            if (resourceRequirements == null)
            {
                throw new ArgumentNullException(nameof(resourceRequirements));
            }

            var copy = new CombatResourceRequirement[
                resourceRequirements.Count];
            for (int index = 0;
                index < resourceRequirements.Count;
                index++)
            {
                copy[index] = resourceRequirements[index]
                    ?? throw new ArgumentException(
                        $"Resource requirement entry {index} is null.",
                        nameof(resourceRequirements));
            }

            return Array.AsReadOnly(copy);
        }

        private static ReadOnlyCollection<ICharacterCombatStatusSource>
            CopyCombatStatusSources(
                IReadOnlyList<ICharacterCombatStatusSource>
                    combatStatusSources)
        {
            if (combatStatusSources == null)
            {
                throw new ArgumentNullException(nameof(combatStatusSources));
            }

            var copy = new ICharacterCombatStatusSource[
                combatStatusSources.Count];
            for (int index = 0;
                index < combatStatusSources.Count;
                index++)
            {
                copy[index] = combatStatusSources[index]
                    ?? throw new ArgumentException(
                        $"Combat status source entry {index} is null.",
                        nameof(combatStatusSources));
            }

            return Array.AsReadOnly(copy);
        }

        private static ReadOnlyCollection<InitialPartyEffectDefinition>
            CopyInitialPartyEffects(
                IReadOnlyList<InitialPartyEffectDefinition>
                    initialPartyEffects)
        {
            if (initialPartyEffects == null)
            {
                throw new ArgumentNullException(nameof(initialPartyEffects));
            }

            var copy = new InitialPartyEffectDefinition[
                initialPartyEffects.Count];
            for (int index = 0;
                index < initialPartyEffects.Count;
                index++)
            {
                copy[index] = initialPartyEffects[index]
                    ?? throw new ArgumentException(
                        $"Initial Party Effect entry {index} is null.",
                        nameof(initialPartyEffects));
            }

            return Array.AsReadOnly(copy);
        }
    }
}
