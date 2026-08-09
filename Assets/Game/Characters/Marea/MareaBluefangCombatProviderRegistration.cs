using System;
using ValorChronicle.Battle.Combat.Integration;

namespace ValorChronicle.Characters.Marea
{
    public static class MareaBluefangCombatProviderRegistration
    {
        public static void Register(
            MatchEventActionProviderRegistry matchProviders,
            ActiveAbilityActionProviderRegistry activeProviders)
        {
            if (matchProviders == null)
            {
                throw new ArgumentNullException(nameof(matchProviders));
            }

            if (activeProviders == null)
            {
                throw new ArgumentNullException(nameof(activeProviders));
            }

            matchProviders.Register(
                MareaBluefangRules.CharacterId,
                new MareaBluefangMatchActionProvider());
            activeProviders.Register(
                MareaBluefangRules.CharacterId,
                MareaBluefangRules.ActiveAbilityId,
                new MareaBluefangActiveActionProvider());
        }
    }
}
