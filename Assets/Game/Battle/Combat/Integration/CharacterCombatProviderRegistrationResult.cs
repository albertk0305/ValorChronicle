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
            IReadOnlyList<InitialPartyEffectDefinition> initialPartyEffects,
            IReadOnlyList<ShieldConditionalBossEffectDefinition>
                shieldConditionalBossEffects,
            IReadOnlyList<IPreMatchQueueActionProvider>
                preMatchQueueActionProviders,
            ActiveAbilityCooldownReductionHandle
                activeAbilityCooldownReductionHandle)
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
            ShieldConditionalBossEffects =
                CopyShieldConditionalBossEffects(
                    shieldConditionalBossEffects);
            PreMatchQueueActionProviders =
                CopyPreMatchQueueActionProviders(
                    preMatchQueueActionProviders);
            ActiveAbilityCooldownReductionHandle =
                activeAbilityCooldownReductionHandle;
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
        public IReadOnlyList<ShieldConditionalBossEffectDefinition>
            ShieldConditionalBossEffects { get; }
        public IReadOnlyList<IPreMatchQueueActionProvider>
            PreMatchQueueActionProviders { get; }
        public ActiveAbilityCooldownReductionHandle
            ActiveAbilityCooldownReductionHandle { get; }

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
                Array.Empty<InitialPartyEffectDefinition>(),
                Array.Empty<ShieldConditionalBossEffectDefinition>(),
                Array.Empty<IPreMatchQueueActionProvider>(),
                activeAbilityCooldownReductionHandle: null);
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
                Array.Empty<InitialPartyEffectDefinition>(),
                Array.Empty<ShieldConditionalBossEffectDefinition>(),
                Array.Empty<IPreMatchQueueActionProvider>(),
                activeAbilityCooldownReductionHandle: null);
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
                Array.Empty<InitialPartyEffectDefinition>(),
                Array.Empty<ShieldConditionalBossEffectDefinition>(),
                Array.Empty<IPreMatchQueueActionProvider>(),
                activeAbilityCooldownReductionHandle: null);
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
                Array.Empty<InitialPartyEffectDefinition>(),
                Array.Empty<ShieldConditionalBossEffectDefinition>(),
                Array.Empty<IPreMatchQueueActionProvider>(),
                activeAbilityCooldownReductionHandle: null);
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
                initialPartyEffects,
                ShieldConditionalBossEffects,
                PreMatchQueueActionProviders,
                ActiveAbilityCooldownReductionHandle);
        }

        public CharacterCombatProviderRegistrationResult
            WithShieldConditionalBossEffects(
                IReadOnlyList<ShieldConditionalBossEffectDefinition>
                    shieldConditionalBossEffects)
        {
            return new CharacterCombatProviderRegistrationResult(
                ActiveAbilityId,
                ActiveCooldownTurns,
                TriggerRules,
                ResourceRequirements,
                CombatStatusSources,
                InitialPartyEffects,
                shieldConditionalBossEffects,
                PreMatchQueueActionProviders,
                ActiveAbilityCooldownReductionHandle);
        }

        public CharacterCombatProviderRegistrationResult
            WithPreMatchQueueActionProviders(
                IReadOnlyList<IPreMatchQueueActionProvider> providers)
        {
            return new CharacterCombatProviderRegistrationResult(
                ActiveAbilityId,
                ActiveCooldownTurns,
                TriggerRules,
                ResourceRequirements,
                CombatStatusSources,
                InitialPartyEffects,
                ShieldConditionalBossEffects,
                providers,
                ActiveAbilityCooldownReductionHandle);
        }

        public CharacterCombatProviderRegistrationResult
            WithActiveAbilityCooldownReductionHandle(
                ActiveAbilityCooldownReductionHandle handle)
        {
            if (!HasActiveAbility)
            {
                throw new InvalidOperationException(
                    "A cooldown reduction handle requires an active "
                        + "ability registration.");
            }

            if (handle == null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            return new CharacterCombatProviderRegistrationResult(
                ActiveAbilityId,
                ActiveCooldownTurns,
                TriggerRules,
                ResourceRequirements,
                CombatStatusSources,
                InitialPartyEffects,
                ShieldConditionalBossEffects,
                PreMatchQueueActionProviders,
                handle);
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

        private static ReadOnlyCollection<
            ShieldConditionalBossEffectDefinition>
            CopyShieldConditionalBossEffects(
                IReadOnlyList<ShieldConditionalBossEffectDefinition>
                    effects)
        {
            if (effects == null)
            {
                throw new ArgumentNullException(nameof(effects));
            }

            var copy = new ShieldConditionalBossEffectDefinition[
                effects.Count];
            for (int index = 0; index < effects.Count; index++)
            {
                copy[index] = effects[index]
                    ?? throw new ArgumentException(
                        $"Shield-conditional Boss Effect entry {index} "
                            + "is null.",
                        nameof(effects));
            }

            return Array.AsReadOnly(copy);
        }

        private static ReadOnlyCollection<IPreMatchQueueActionProvider>
            CopyPreMatchQueueActionProviders(
                IReadOnlyList<IPreMatchQueueActionProvider> providers)
        {
            if (providers == null)
            {
                throw new ArgumentNullException(nameof(providers));
            }

            var copy = new IPreMatchQueueActionProvider[providers.Count];
            for (int index = 0; index < providers.Count; index++)
            {
                copy[index] = providers[index]
                    ?? throw new ArgumentException(
                        $"Pre-match queue provider entry {index} is null.",
                        nameof(providers));
            }

            return Array.AsReadOnly(copy);
        }
    }
}
