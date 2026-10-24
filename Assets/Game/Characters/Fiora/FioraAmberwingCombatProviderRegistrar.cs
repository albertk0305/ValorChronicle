using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Characters.Fiora
{
    public sealed class FioraAmberwingCombatProviderRegistrar
        : ICharacterCombatProviderRegistrar
    {
        public string CharacterId => FioraAmberwingRules.CharacterId;
        public Type SupportedCombatConfigType => typeof(FioraAmberwingCombatConfig);

        public CharacterCombatProviderRegistrationResult Register(
            BattlePartyMemberInput member,
            CharacterBattleState character,
            BossBattleState boss,
            CombatActionIdSequence actionIds,
            MatchEventActionProviderRegistry matchProviders,
            ActiveAbilityActionProviderRegistry activeProviders)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));
            if (character == null) throw new ArgumentNullException(nameof(character));
            if (boss == null) throw new ArgumentNullException(nameof(boss));
            if (actionIds == null) throw new ArgumentNullException(nameof(actionIds));
            if (matchProviders == null) throw new ArgumentNullException(nameof(matchProviders));
            if (activeProviders == null) throw new ArgumentNullException(nameof(activeProviders));
            if (!string.Equals(member.CharacterId, CharacterId, StringComparison.Ordinal)
                || !string.Equals(character.CharacterId, CharacterId, StringComparison.Ordinal)
                || character.Element != ElementType.Fire)
                throw new InvalidOperationException("Fiora's registrar requires the canonical Fire Fiora character input and state.");

            var config = member.CharacterDefinition.CombatConfig as FioraAmberwingCombatConfig;
            if (config == null)
                throw new InvalidOperationException("Fiora requires FioraAmberwingCombatConfig.");
            if (!config.TryValidate(out string errorMessage))
                throw new InvalidOperationException($"Fiora's CombatConfig is invalid. {errorMessage}");

            FioraAmberwingResolvedCombatRules rules =
                FioraAmberwingCombatRuleResolver.Resolve(config, member.Awakening);
            var battleState = new FioraAmberwingBattleState(
                character,
                rules.ActiveFollowUpHealMaxCount);
            var matchProvider = new FioraAmberwingMatchActionProvider(
                config,
                member.Awakening,
                battleState);
            var activeProvider = new FioraAmberwingActiveActionProvider(
                config,
                member.Awakening,
                battleState);
            var followUpHealingProvider =
                new FioraAmberwingFollowUpHealingProvider(
                    config,
                    member.Awakening,
                    battleState);

            matchProviders.Register(CharacterId, matchProvider);
            activeProviders.Register(CharacterId, FioraAmberwingRules.ActiveAbilityId, activeProvider);

            return CharacterCombatProviderRegistrationResult
                .WithActiveAbility(
                    FioraAmberwingRules.ActiveAbilityId,
                    rules.ActiveCooldownTurns,
                    new ICombatTriggerRule[]
                    {
                        new FioraAmberwingOverhealBlessingRule(
                            battleState,
                            config,
                            member.Awakening,
                            actionIds)
                    },
                    Array.Empty<CombatResourceRequirement>(),
                    new ICharacterCombatStatusSource[] { battleState })
                .WithPreMatchQueueActionProviders(
                    new IPreMatchQueueActionProvider[]
                    {
                        followUpHealingProvider
                    });
        }
    }
}
