using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Characters.Koharu
{
    public sealed class SakuragiKoharuCombatProviderRegistrar
        : ICharacterCombatProviderRegistrar
    {
        public string CharacterId => SakuragiKoharuRules.CharacterId;

        public Type SupportedCombatConfigType =>
            typeof(SakuragiKoharuCombatConfig);

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
                    CharacterId,
                    StringComparison.Ordinal)
                || !string.Equals(
                    character.CharacterId,
                    member.CharacterId,
                    StringComparison.Ordinal)
                || member.PartySlotIndex != character.PartySlotIndex
                || character.Element != ElementType.Grass)
            {
                throw new InvalidOperationException(
                    "Koharu's registrar requires the canonical Grass "
                        + "character and matching party slot.");
            }

            var config = member.CharacterDefinition.CombatConfig
                as SakuragiKoharuCombatConfig;
            if (config == null)
            {
                throw new InvalidOperationException(
                    "Koharu requires SakuragiKoharuCombatConfig.");
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new InvalidOperationException(
                    $"Koharu's CombatConfig is invalid. {errorMessage}");
            }

            var battleState = new SakuragiKoharuBattleState(character);
            var cooldownHandle =
                new ActiveAbilityCooldownReductionHandle(
                    character.CharacterId,
                    character.PartySlotIndex);
            var matchProvider = new SakuragiKoharuMatchActionProvider(
                config,
                member.Awakening);
            var spiritRule = new SakuragiKoharuSpiritEnergyRule(
                battleState,
                config,
                member.Awakening,
                cooldownHandle);
            var activeProvider =
                new SakuragiKoharuActiveBoardPlanProvider(
                    config,
                    member.Awakening,
                    battleState,
                    cooldownHandle);

            matchProviders.Register(CharacterId, matchProvider);
            activeProviders.RegisterBoard(
                CharacterId,
                SakuragiKoharuRules.ActiveAbilityId,
                activeProvider);

            return CharacterCombatProviderRegistrationResult
                .WithActiveAbility(
                    SakuragiKoharuRules.ActiveAbilityId,
                    activeProvider.CooldownTurns,
                    new ICombatTriggerRule[] { spiritRule },
                    Array.Empty<CombatResourceRequirement>(),
                    new ICharacterCombatStatusSource[] { battleState })
                .WithActiveAbilityCooldownReductionHandle(
                    cooldownHandle);
        }
    }
}
