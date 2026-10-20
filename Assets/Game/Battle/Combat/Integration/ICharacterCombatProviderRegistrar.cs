using System;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Battle.Combat.Integration
{
    public interface ICharacterCombatProviderRegistrar
    {
        string CharacterId { get; }

        Type SupportedCombatConfigType { get; }

        CharacterCombatProviderRegistrationResult Register(
            BattlePartyMemberInput member,
            CharacterBattleState character,
            BossBattleState boss,
            CombatActionIdSequence actionIds,
            MatchEventActionProviderRegistry matchProviders,
            ActiveAbilityActionProviderRegistry activeProviders);
    }
}
