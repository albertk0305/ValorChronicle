using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Characters.Brina
{
    public sealed class BrinaAshwoodCombatProviderRegistrar
        : ICharacterCombatProviderRegistrar
    {
        public string CharacterId => BrinaAshwoodRules.CharacterId;

        public Type SupportedCombatConfigType =>
            typeof(BrinaAshwoodCombatConfig);

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

            if (matchProviders == null)
            {
                throw new ArgumentNullException(nameof(matchProviders));
            }

            if (activeProviders == null)
            {
                throw new ArgumentNullException(nameof(activeProviders));
            }

            if (!string.Equals(
                    member.CharacterId,
                    BrinaAshwoodRules.CharacterId,
                    StringComparison.Ordinal)
                || !string.Equals(
                    character.CharacterId,
                    member.CharacterId,
                    StringComparison.Ordinal)
                || character.PartySlotIndex != member.PartySlotIndex
                || character.Element != ElementType.Grass
                || member.CharacterDefinition.Element
                    != ElementType.Grass)
            {
                throw new InvalidOperationException(
                    "Brina's registrar requires the canonical Grass "
                        + "Brina party member and battle state.");
            }

            var config = member.CharacterDefinition.CombatConfig
                as BrinaAshwoodCombatConfig;
            if (config == null)
            {
                throw new InvalidOperationException(
                    "Brina requires BrinaAshwoodCombatConfig.");
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new InvalidOperationException(
                    $"Brina's CombatConfig is invalid. {errorMessage}");
            }

            BrinaAshwoodResolvedCombatRules rules =
                BrinaAshwoodCombatRuleResolver.Resolve(
                    config,
                    member.Awakening);
            var battleState = new BrinaAshwoodBattleState(
                character,
                rules.WoodcutTriggerThreshold);
            var matchProvider = new BrinaAshwoodMatchActionProvider(
                config,
                member.Awakening);
            var activeProvider = new BrinaAshwoodActiveActionProvider(
                config,
                member.Awakening);
            var completionRule = new BrinaAshwoodMatchCompletionRule(
                battleState,
                config,
                member.Awakening,
                actionIds);

            matchProviders.Register(CharacterId, matchProvider);
            activeProviders.Register(
                CharacterId,
                BrinaAshwoodRules.ActiveAbilityId,
                activeProvider);

            return CharacterCombatProviderRegistrationResult
                .WithActiveAbility(
                    BrinaAshwoodRules.ActiveAbilityId,
                    rules.ActiveCooldownTurns,
                    new ICombatTriggerRule[] { completionRule },
                    Array.Empty<CombatResourceRequirement>(),
                    new ICharacterCombatStatusSource[] { battleState });
        }
    }
}
