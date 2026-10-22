using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Characters.Miyuki
{
    public sealed class ShimotsukiMiyukiCombatProviderRegistrar
        : ICharacterCombatProviderRegistrar
    {
        public string CharacterId => ShimotsukiMiyukiRules.CharacterId;

        public Type SupportedCombatConfigType =>
            typeof(ShimotsukiMiyukiCombatConfig);

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
                    ShimotsukiMiyukiRules.CharacterId,
                    StringComparison.Ordinal)
                || !string.Equals(
                    character.CharacterId,
                    member.CharacterId,
                    StringComparison.Ordinal)
                || character.Element != ElementType.Water)
            {
                throw new InvalidOperationException(
                    "Miyuki's registrar requires the canonical Water "
                        + "character input and state.");
            }

            var config = member.CharacterDefinition.CombatConfig
                as ShimotsukiMiyukiCombatConfig;
            if (config == null)
            {
                throw new InvalidOperationException(
                    "Miyuki requires ShimotsukiMiyukiCombatConfig.");
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new InvalidOperationException(
                    $"Miyuki's CombatConfig is invalid. {errorMessage}");
            }

            ShimotsukiMiyukiResolvedCombatRules rules =
                ShimotsukiMiyukiCombatRuleResolver.Resolve(
                    config,
                    member.Awakening);
            var battleState = new ShimotsukiMiyukiBattleState(
                character,
                rules.HasFrostEcho);
            var activeProvider =
                new ShimotsukiMiyukiActiveBoardPlanProvider(
                    config,
                    member.Awakening,
                    battleState);
            matchProviders.Register(
                CharacterId,
                new ShimotsukiMiyukiMatchActionProvider(
                    config,
                    member.Awakening,
                    battleState));
            activeProviders.RegisterBoard(
                CharacterId,
                ShimotsukiMiyukiRules.ActiveAbilityId,
                activeProvider);

            return CharacterCombatProviderRegistrationResult
                .WithActiveAbility(
                    ShimotsukiMiyukiRules.ActiveAbilityId,
                    activeProvider.CooldownTurns,
                    new ICombatTriggerRule[]
                    {
                        new ShimotsukiMiyukiFrostEchoConsumptionRule(
                            battleState,
                            config,
                            member.Awakening)
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
                    })
                .WithInitialPartyEffects(
                    new InitialPartyEffectDefinition[]
                    {
                        new InitialPartyEffectDefinition(
                            ShimotsukiMiyukiRules.PassiveEffectId,
                            CharacterId,
                            EffectCategory.Buff,
                            EffectModifierType.ElementDamageIncrease,
                            rules.PassiveWaterDamageIncreaseRate,
                            remainingTurns: null,
                            EffectStackPolicy.Unique,
                            elementFilter: ElementType.Water)
                    });
        }
    }
}
