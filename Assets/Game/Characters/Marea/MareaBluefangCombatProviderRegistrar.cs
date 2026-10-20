using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Characters.Marea
{
    public sealed class MareaBluefangCombatProviderRegistrar
        : ICharacterCombatProviderRegistrar
    {
        public string CharacterId => MareaBluefangRules.CharacterId;

        public Type SupportedCombatConfigType =>
            typeof(MareaBluefangCombatConfig);

        public CharacterCombatProviderRegistrationResult Register(
            BattlePartyMemberInput member,
            CharacterBattleState character,
            BossBattleState boss,
            CombatActionIdSequence actionIds,
            MatchEventActionProviderRegistry matchProviders,
            ActiveAbilityActionProviderRegistry activeProviders)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (character == null)
            {
                throw new ArgumentNullException(nameof(character));
            }

            if (boss == null)
            {
                throw new ArgumentNullException(nameof(boss));
            }

            if (actionIds == null)
            {
                throw new ArgumentNullException(nameof(actionIds));
            }

            if (!string.Equals(
                    member.CharacterId,
                    MareaBluefangRules.CharacterId,
                    StringComparison.Ordinal)
                || !string.Equals(
                    character.CharacterId,
                    member.CharacterId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Marea's registrar requires the canonical Marea "
                        + "character input and state.");
            }

            var config = member.CharacterDefinition.CombatConfig
                as MareaBluefangCombatConfig;
            if (config == null)
            {
                throw new InvalidOperationException(
                    "Marea requires MareaBluefangCombatConfig.");
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new InvalidOperationException(
                    $"Marea's CombatConfig is invalid. {errorMessage}");
            }

            MareaBluefangCombatProviderRegistration.Register(
                matchProviders,
                activeProviders,
                config,
                member.Awakening);
            return CharacterCombatProviderRegistrationResult
                .WithActiveAbility(
                    MareaBluefangRules.ActiveAbilityId,
                    config.ActiveCooldownTurns,
                    Array.Empty<ICombatTriggerRule>(),
                    new CombatResourceRequirement[]
                    {
                        new CombatResourceRequirement(
                            WaterElementResource.Id,
                            WaterElementResource.MaxAmount)
                    });
        }
    }
}
