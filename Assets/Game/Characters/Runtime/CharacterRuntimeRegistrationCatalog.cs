using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Brina;
using ValorChronicle.Characters.Fiora;
using ValorChronicle.Characters.Koharu;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Characters.Miyuki;
using ValorChronicle.Characters.Naira;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Characters.Serena;
using ValorChronicle.Data.Database;
using ValorChronicle.Localization;

namespace ValorChronicle.Characters.Runtime
{
    public sealed class CharacterRuntimeRegistrationCatalog
    {
        private readonly ReadOnlyCollection<CharacterRuntimeRegistration>
            registrations;

        public CharacterRuntimeRegistrationCatalog(
            IReadOnlyList<CharacterRuntimeRegistration> registrations)
        {
            if (registrations == null)
            {
                throw new ArgumentNullException(nameof(registrations));
            }

            var copied = new CharacterRuntimeRegistration[
                registrations.Count];
            var characterIds = new HashSet<string>(StringComparer.Ordinal);
            var combatConfigTypes = new HashSet<Type>();
            for (int index = 0; index < registrations.Count; index++)
            {
                CharacterRuntimeRegistration registration =
                    registrations[index]
                    ?? throw new ArgumentException(
                        $"Runtime registration entry {index} is null.",
                        nameof(registrations));
                if (!characterIds.Add(registration.CharacterId))
                {
                    throw new ArgumentException(
                        "Duplicate character runtime registration ID: "
                            + registration.CharacterId,
                        nameof(registrations));
                }

                if (!combatConfigTypes.Add(
                        registration.SupportedCombatConfigType))
                {
                    throw new ArgumentException(
                        "Duplicate character CombatConfig registration: "
                            + registration.SupportedCombatConfigType.Name,
                        nameof(registrations));
                }

                copied[index] = registration;
            }

            this.registrations = Array.AsReadOnly(copied);
        }

        public IReadOnlyList<CharacterRuntimeRegistration> Registrations =>
            registrations;

        public static CharacterRuntimeRegistrationCatalog CreateDefault()
        {
            return new CharacterRuntimeRegistrationCatalog(
                new[]
                {
                    new CharacterRuntimeRegistration(
                        MareaBluefangRules.CharacterId,
                        () => new MareaBluefangBuildModifier(),
                        () => new MareaBluefangCombatProviderRegistrar(),
                        () => new MareaBluefangLookupDescriptionProvider()),
                    new CharacterRuntimeRegistration(
                        SerenaCoralspearRules.CharacterId,
                        () => new SerenaCoralspearBuildModifier(),
                        () => new SerenaCoralspearCombatProviderRegistrar(),
                        () => new SerenaCoralspearLookupDescriptionProvider()),
                    new CharacterRuntimeRegistration(
                        ShimotsukiMiyukiRules.CharacterId,
                        () => new ShimotsukiMiyukiBuildModifier(),
                        () => new ShimotsukiMiyukiCombatProviderRegistrar(),
                        () => new ShimotsukiMiyukiLookupDescriptionProvider()),
                    new CharacterRuntimeRegistration(
                        FioraAmberwingRules.CharacterId,
                        () => new FioraAmberwingBuildModifier(),
                        () => new FioraAmberwingCombatProviderRegistrar(),
                        () => new FioraAmberwingLookupDescriptionProvider()),
                    new CharacterRuntimeRegistration(
                        BrinaAshwoodRules.CharacterId,
                        () => new BrinaAshwoodBuildModifier(),
                        () => new BrinaAshwoodCombatProviderRegistrar(),
                        () => new BrinaAshwoodLookupDescriptionProvider()),
                    new CharacterRuntimeRegistration(
                        NairaWindhornRules.CharacterId,
                        () => new NairaWindhornBuildModifier(),
                        () => new NairaWindhornCombatProviderRegistrar(),
                        () => new NairaWindhornLookupDescriptionProvider()),
                    new CharacterRuntimeRegistration(
                        SakuragiKoharuRules.CharacterId,
                        () => new SakuragiKoharuBuildModifier(),
                        () => new SakuragiKoharuCombatProviderRegistrar(),
                        () => new SakuragiKoharuLookupDescriptionProvider())
                });
        }

        public CharacterBuildResolver CreateBuildResolver()
        {
            var modifiers = new ICharacterBuildModifier[
                registrations.Count];
            for (int index = 0; index < registrations.Count; index++)
            {
                modifiers[index] = registrations[index]
                    .CreateBuildModifier();
            }

            return new CharacterBuildResolver(modifiers);
        }

        public CharacterCombatProviderRegistrarCatalog
            CreateCombatRegistrarCatalog()
        {
            var registrars = new ICharacterCombatProviderRegistrar[
                registrations.Count];
            for (int index = 0; index < registrations.Count; index++)
            {
                registrars[index] = registrations[index]
                    .CreateCombatRegistrar();
            }

            return new CharacterCombatProviderRegistrarCatalog(registrars);
        }

        public CharacterLookupDescriptionResolver
            CreateLookupDescriptionResolver(
                DefinitionDatabase definitionDatabase,
                LocalizationService localizationService)
        {
            var providers = new ICharacterLookupDescriptionProvider[
                registrations.Count];
            for (int index = 0; index < registrations.Count; index++)
            {
                providers[index] = registrations[index]
                    .CreateLookupProvider();
            }

            return new CharacterLookupDescriptionResolver(
                definitionDatabase,
                localizationService,
                providers);
        }
    }
}
