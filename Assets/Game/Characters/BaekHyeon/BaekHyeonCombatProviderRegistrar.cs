using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Characters.BaekHyeon
{
    public sealed class BaekHyeonCombatProviderRegistrar
        : ICharacterCombatProviderRegistrar
    {
        public string CharacterId => BaekHyeonRules.CharacterId;

        public Type SupportedCombatConfigType =>
            typeof(BaekHyeonCombatConfig);

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
                    BaekHyeonRules.CharacterId,
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
                    "Baek Hyeon's registrar requires the canonical Grass "
                        + "Baek Hyeon party member and battle state.");
            }

            var config = member.CharacterDefinition.CombatConfig
                as BaekHyeonCombatConfig;
            if (config == null)
            {
                throw new InvalidOperationException(
                    "Baek Hyeon requires BaekHyeonCombatConfig.");
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new InvalidOperationException(
                    $"Baek Hyeon's CombatConfig is invalid. {errorMessage}");
            }

            var matchProvider = new BaekHyeonMatchActionProvider(
                config,
                member.Awakening);
            var activeProvider = new BaekHyeonActiveActionProvider(
                config,
                member.Awakening);

            matchProviders.Register(CharacterId, matchProvider);
            activeProviders.Register(
                CharacterId,
                BaekHyeonRules.ActiveAbilityId,
                activeProvider);

            return CharacterCombatProviderRegistrationResult
                .WithActiveAbility(
                    BaekHyeonRules.ActiveAbilityId,
                    activeProvider.CooldownTurns,
                    new ICombatTriggerRule[]
                    {
                        new BaekHyeonMarkConsumptionRule(
                            config,
                            member.Awakening,
                            actionIds),
                        new BaekHyeonExposureApplicationRule(
                            config,
                            member.Awakening,
                            actionIds)
                    },
                    Array.Empty<CombatResourceRequirement>(),
                    Array.Empty<ICharacterCombatStatusSource>());
        }
    }
}
