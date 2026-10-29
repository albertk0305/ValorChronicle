using System;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.Modifiers;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Characters.Naira
{
    public sealed class NairaWindhornCombatProviderRegistrar
        : ICharacterCombatProviderRegistrar
    {
        public string CharacterId => NairaWindhornRules.CharacterId;

        public Type SupportedCombatConfigType =>
            typeof(NairaWindhornCombatConfig);

        public CharacterCombatProviderRegistrationResult Register(
            BattlePartyMemberInput member,
            CharacterBattleState character,
            BossBattleState boss,
            CombatActionIdSequence actionIds,
            MatchEventActionProviderRegistry matchProviders,
            ActiveAbilityActionProviderRegistry activeProviders)
        {
            ValidateInputs(
                member,
                character,
                boss,
                actionIds,
                matchProviders,
                activeProviders);

            var config = member.CharacterDefinition.CombatConfig
                as NairaWindhornCombatConfig;
            if (config == null)
            {
                throw new InvalidOperationException(
                    "Naira requires NairaWindhornCombatConfig.");
            }

            if (!config.TryValidate(out string errorMessage))
            {
                throw new InvalidOperationException(
                    $"Naira's CombatConfig is invalid. {errorMessage}");
            }

            NairaWindhornResolvedCombatRules rules =
                NairaWindhornCombatRuleResolver.Resolve(
                    config,
                    member.Awakening);
            var matchProvider = new NairaWindhornMatchActionProvider(
                config,
                member.Awakening);
            var activeProvider = new NairaWindhornActiveActionProvider(
                config,
                member.Awakening);

            matchProviders.Register(CharacterId, matchProvider);
            activeProviders.Register(
                CharacterId,
                NairaWindhornRules.ActiveAbilityId,
                activeProvider);

            ICombatTriggerRule[] triggerRules =
                rules.AncientGuardianEnabled
                    ? new ICombatTriggerRule[]
                    {
                        new NairaWindhornAncientGuardianRule(
                            character,
                            config,
                            member.Awakening,
                            actionIds)
                    }
                    : Array.Empty<ICombatTriggerRule>();
            var spiritWard = new ShieldConditionalBossEffectDefinition(
                NairaWindhornRules.SpiritWardEffectId,
                NairaWindhornRules.CharacterId,
                EffectCategory.Debuff,
                EffectModifierType.TargetTakenDamageIncrease,
                rules.PassiveTargetTakenDamageIncreaseRate,
                CombatModifierGroupRules
                    .AllyTargetTakenDamageIncreaseGroupId,
                CombatModifierGroupRules
                    .AllyTargetTakenDamageIncreaseCap);

            return CharacterCombatProviderRegistrationResult
                .WithActiveAbility(
                    NairaWindhornRules.ActiveAbilityId,
                    rules.ActiveCooldownTurns,
                    triggerRules,
                    Array.Empty<CombatResourceRequirement>())
                .WithShieldConditionalBossEffects(
                    new[] { spiritWard });
        }

        private static void ValidateInputs(
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
                    NairaWindhornRules.CharacterId,
                    StringComparison.Ordinal)
                || !string.Equals(
                    character.CharacterId,
                    member.CharacterId,
                    StringComparison.Ordinal)
                || character.PartySlotIndex != member.PartySlotIndex
                || character.Element != ElementType.Grass
                || member.CharacterDefinition.Element != ElementType.Grass)
            {
                throw new InvalidOperationException(
                    "Naira's registrar requires the canonical Grass Naira "
                        + "party member and battle state.");
            }
        }
    }
}
