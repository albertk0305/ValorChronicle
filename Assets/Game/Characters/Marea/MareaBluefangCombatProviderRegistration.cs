using System;
using ValorChronicle.Battle.Combat.Integration;

namespace ValorChronicle.Characters.Marea
{
    public static class MareaBluefangCombatProviderRegistration
    {
        public static void Register(
            MatchEventActionProviderRegistry matchProviders,
            ActiveAbilityActionProviderRegistry activeProviders,
            MareaBluefangCombatConfig config)
        {
            if (matchProviders == null)
            {
                throw new ArgumentNullException(nameof(matchProviders));
            }

            if (activeProviders == null)
            {
                throw new ArgumentNullException(nameof(activeProviders));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(config));
            }

            matchProviders.Register(
                MareaBluefangRules.CharacterId,
                new MareaBluefangMatchActionProvider(config));
            activeProviders.Register(
                MareaBluefangRules.CharacterId,
                MareaBluefangRules.ActiveAbilityId,
                new MareaBluefangActiveActionProvider(config));
        }
    }
}
