using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Characters.Serena
{
    public sealed class SerenaCoralspearCombatProviderRegistrar
        : ICharacterCombatProviderRegistrar
    {
        public string CharacterId => SerenaCoralspearRules.CharacterId;

        public Type SupportedCombatConfigType =>
            typeof(SerenaCoralspearCombatConfig);

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
                    SerenaCoralspearRules.CharacterId,
                    StringComparison.Ordinal)
                || !string.Equals(
                    character.CharacterId,
                    member.CharacterId,
                    StringComparison.Ordinal)
                || character.Element != ElementType.Water)
            {
                throw new InvalidOperationException(
                    "Serena's registrar requires the canonical Water "
                        + "Serena character input and state.");
            }

            var config = member.CharacterDefinition.CombatConfig
                as SerenaCoralspearCombatConfig;
            if (config == null)
            {
                throw new InvalidOperationException(
                    "Serena requires SerenaCoralspearCombatConfig.");
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new InvalidOperationException(
                    $"Serena's CombatConfig is invalid. {errorMessage}");
            }

            SerenaCoralspearResolvedCombatRules rules =
                SerenaCoralspearCombatRuleResolver.Resolve(
                    config,
                    member.Awakening);
            var battleState = new SerenaCoralspearBattleState(
                character,
                rules.EmpoweredChaseMaxCharges);
            var matchProvider = new SerenaCoralspearMatchActionProvider(
                config,
                member.Awakening);
            var activeProvider = new SerenaCoralspearActiveActionProvider(
                config,
                member.Awakening);

            matchProviders.Register(CharacterId, matchProvider);
            activeProviders.Register(
                CharacterId,
                SerenaCoralspearRules.ActiveAbilityId,
                activeProvider);

            return CharacterCombatProviderRegistrationResult
                .WithActiveAbility(
                    SerenaCoralspearRules.ActiveAbilityId,
                    activeProvider.CooldownTurns,
                    new ICombatTriggerRule[]
                    {
                        new SerenaCoralspearMatch5CompletionRule(
                            battleState),
                        new SerenaCoralspearPassiveChaseRule(
                            battleState,
                            config,
                            member.Awakening,
                            actionIds)
                    },
                    new CombatResourceRequirement[]
                    {
                        new CombatResourceRequirement(
                            WaterElementResource.Id,
                            WaterElementResource.MaxAmount)
                    },
                    new ICharacterCombatStatusSource[]
                    {
                        battleState
                    });
        }
    }
}
