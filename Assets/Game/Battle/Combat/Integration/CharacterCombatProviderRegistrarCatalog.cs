using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class CharacterCombatProviderRegistrarCatalog
    {
        private readonly Dictionary<Type, ICharacterCombatProviderRegistrar>
            registrarsByConfigType;

        public CharacterCombatProviderRegistrarCatalog(
            IReadOnlyList<ICharacterCombatProviderRegistrar> registrars)
        {
            if (registrars == null)
            {
                throw new ArgumentNullException(nameof(registrars));
            }

            registrarsByConfigType =
                new Dictionary<Type, ICharacterCombatProviderRegistrar>(
                    registrars.Count);
            for (int index = 0; index < registrars.Count; index++)
            {
                ICharacterCombatProviderRegistrar registrar =
                    registrars[index];
                if (registrar == null)
                {
                    throw new ArgumentException(
                        $"Registrar entry {index} is null.",
                        nameof(registrars));
                }

                Type configType = registrar.SupportedCombatConfigType;
                if (configType == null
                    || !typeof(CombatConfigDefinition).IsAssignableFrom(
                        configType))
                {
                    throw new ArgumentException(
                        $"Registrar entry {index} has an invalid supported "
                            + "CombatConfig type.",
                        nameof(registrars));
                }

                if (registrarsByConfigType.ContainsKey(configType))
                {
                    throw new ArgumentException(
                        $"A registrar for '{configType.Name}' is already "
                            + "registered.",
                        nameof(registrars));
                }

                registrarsByConfigType.Add(configType, registrar);
            }
        }

        public CharacterCombatProviderRegistrationResult Register(
            BattlePartyMemberInput member,
            CharacterBattleState character,
            BossBattleState boss,
            MatchEventActionProviderRegistry matchProviders,
            ActiveAbilityActionProviderRegistry activeProviders)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            CombatConfigDefinition config =
                member.CharacterDefinition.CombatConfig;
            if (config == null)
            {
                throw new InvalidOperationException(
                    $"Character '{member.CharacterId}' has no CombatConfig.");
            }

            if (!registrarsByConfigType.TryGetValue(
                config.GetType(),
                out ICharacterCombatProviderRegistrar registrar))
            {
                throw new InvalidOperationException(
                    $"Character '{member.CharacterId}' uses unsupported "
                        + $"CombatConfig '{config.GetType().Name}'.");
            }

            return registrar.Register(
                    member,
                    character,
                    boss,
                    matchProviders,
                    activeProviders)
                ?? throw new InvalidOperationException(
                    $"The registrar for '{member.CharacterId}' returned "
                        + "no registration result.");
        }
    }
}
