using System;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Runtime
{
    public sealed class CharacterRuntimeRegistration
    {
        private readonly Func<ICharacterBuildModifier>
            buildModifierFactory;
        private readonly Func<ICharacterCombatProviderRegistrar>
            combatRegistrarFactory;
        private readonly Func<ICharacterLookupDescriptionProvider>
            lookupProviderFactory;

        public CharacterRuntimeRegistration(
            string characterId,
            Func<ICharacterBuildModifier> buildModifierFactory,
            Func<ICharacterCombatProviderRegistrar> combatRegistrarFactory,
            Func<ICharacterLookupDescriptionProvider> lookupProviderFactory)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                throw new ArgumentException(
                    "A stable character ID is required.",
                    nameof(characterId));
            }

            this.buildModifierFactory = buildModifierFactory
                ?? throw new ArgumentNullException(
                    nameof(buildModifierFactory));
            this.combatRegistrarFactory = combatRegistrarFactory
                ?? throw new ArgumentNullException(
                    nameof(combatRegistrarFactory));
            this.lookupProviderFactory = lookupProviderFactory
                ?? throw new ArgumentNullException(
                    nameof(lookupProviderFactory));
            CharacterId = characterId;

            CreateBuildModifier();
            ICharacterCombatProviderRegistrar registrar =
                CreateCombatRegistrar();
            SupportedCombatConfigType =
                registrar.SupportedCombatConfigType;
            CreateLookupProvider();
        }

        public string CharacterId { get; }
        public Type SupportedCombatConfigType { get; }

        public ICharacterBuildModifier CreateBuildModifier()
        {
            ICharacterBuildModifier modifier = buildModifierFactory()
                ?? throw new InvalidOperationException(
                    $"Build modifier factory for '{CharacterId}' returned "
                        + "null.");
            ValidateCharacterId(
                modifier.CharacterId,
                "Build modifier");
            return modifier;
        }

        public ICharacterCombatProviderRegistrar CreateCombatRegistrar()
        {
            ICharacterCombatProviderRegistrar registrar =
                combatRegistrarFactory()
                ?? throw new InvalidOperationException(
                    $"Combat registrar factory for '{CharacterId}' returned "
                        + "null.");
            ValidateCharacterId(
                registrar.CharacterId,
                "Combat registrar");
            Type configType = registrar.SupportedCombatConfigType;
            if (configType == null
                || !typeof(CombatConfigDefinition).IsAssignableFrom(
                    configType))
            {
                throw new InvalidOperationException(
                    $"Combat registrar for '{CharacterId}' has an invalid "
                        + "supported CombatConfig type.");
            }

            if (SupportedCombatConfigType != null
                && configType != SupportedCombatConfigType)
            {
                throw new InvalidOperationException(
                    $"Combat registrar factory for '{CharacterId}' returned "
                        + "inconsistent CombatConfig types.");
            }

            return registrar;
        }

        public ICharacterLookupDescriptionProvider CreateLookupProvider()
        {
            ICharacterLookupDescriptionProvider provider =
                lookupProviderFactory()
                ?? throw new InvalidOperationException(
                    $"Lookup provider factory for '{CharacterId}' returned "
                        + "null.");
            ValidateCharacterId(
                provider.CharacterId,
                "Lookup provider");
            return provider;
        }

        private void ValidateCharacterId(
            string componentCharacterId,
            string componentName)
        {
            if (!string.Equals(
                    componentCharacterId,
                    CharacterId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"{componentName} character ID "
                        + $"'{componentCharacterId ?? "<null>"}' does not "
                        + $"match registration ID '{CharacterId}'.");
            }
        }
    }
}
