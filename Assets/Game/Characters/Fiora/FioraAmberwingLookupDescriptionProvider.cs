using System;
using System.Collections.Generic;
using System.Globalization;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Fiora
{
    public sealed class FioraAmberwingLookupDescriptionProvider
        : ICharacterLookupDescriptionProvider
    {
        private const string AwakeningTitleKey =
            "character.lookup.awakening.title";

        private readonly FioraAmberwingBuildModifier buildModifier =
            new FioraAmberwingBuildModifier();

        public string CharacterId => FioraAmberwingRules.CharacterId;

        public CharacterLookupDescriptionDefinition ResolveSkill(
            CharacterDefinition definition,
            int awakening,
            CharacterLookupSkillType skillType)
        {
            FioraAmberwingResolvedCombatRules rules =
                FioraAmberwingCombatRuleResolver.Resolve(
                    RequireConfig(definition),
                    awakening);
            switch (skillType)
            {
                case CharacterLookupSkillType.Match3:
                    return Skill(
                        "fiora.skill.match3.name",
                        "fiora.skill.match3.description",
                        Arguments(
                            ("healing", Percent(
                                rules.Match3HealingCoefficient))));
                case CharacterLookupSkillType.Match4:
                    return Skill(
                        "fiora.skill.match4.name",
                        "fiora.skill.match4.description",
                        Arguments(
                            ("healing", Percent(
                                rules.Match4HealingCoefficient)),
                            ("attack_increase", Percent(
                                rules.Match4AttackIncreaseRate)),
                            ("duration", rules.Match4BuffDurationTurns)));
                case CharacterLookupSkillType.Match5:
                    return Skill(
                        "fiora.skill.match5.name",
                        "fiora.skill.match5.description",
                        Arguments(
                            ("healing", Percent(
                                rules.Match5HealingCoefficient)),
                            ("attack_increase", Percent(
                                rules.Match5AttackIncreaseRate)),
                            ("duration", rules.Match5BuffDurationTurns)));
                case CharacterLookupSkillType.Passive:
                    return Skill(
                        "fiora.skill.passive.name",
                        "fiora.skill.passive.description",
                        Arguments(
                            ("conversion", Percent(
                                rules.PassiveConversionRate)),
                            ("instance_cap", Percent(
                                rules.PassivePerInstanceCap)),
                            ("group_cap", Percent(
                                rules.PassiveGroupCap)),
                            ("duration", rules.PassiveDurationTurns)));
                case CharacterLookupSkillType.Active:
                    return Skill(
                        "fiora.skill.active.name",
                        "fiora.skill.active.description",
                        Arguments(
                            ("instant_healing", Percent(
                                rules.ActiveInstantHealingCoefficient)),
                            ("follow_up_healing", Percent(
                                rules.ActiveFollowUpHealingCoefficient)),
                            ("grant_count",
                                rules.ActiveFollowUpHealGrantCount),
                            ("max_count",
                                rules.ActiveFollowUpHealMaxCount),
                            ("cooldown", rules.ActiveCooldownTurns)));
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(skillType),
                        skillType,
                        "Skill type is not supported.");
            }
        }

        public CharacterLookupDescriptionDefinition ResolveAwakening(
            CharacterDefinition definition,
            int awakeningStage)
        {
            if (awakeningStage < 1 || awakeningStage > 6)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(awakeningStage));
            }

            FioraAmberwingCombatConfig config = RequireConfig(definition);
            CharacterLookupLocalizedText description;
            if (awakeningStage == 1)
            {
                var before = buildModifier.GetStatMultipliers(0);
                var after = buildModifier.GetStatMultipliers(1);
                description = Text(
                    "fiora.awakening.1.description",
                    Arguments(
                        ("multiplier_before", Percent(
                            before.MaxHpMultiplier)),
                        ("multiplier_after", Percent(
                            after.MaxHpMultiplier))));
            }
            else
            {
                FioraAmberwingResolvedCombatRules before =
                    FioraAmberwingCombatRuleResolver.Resolve(
                        config,
                        awakeningStage - 1);
                FioraAmberwingResolvedCombatRules after =
                    FioraAmberwingCombatRuleResolver.Resolve(
                        config,
                        awakeningStage);
                description = AwakeningDescription(
                    awakeningStage,
                    before,
                    after);
            }

            return new CharacterLookupDescriptionDefinition(
                Text(
                    AwakeningTitleKey,
                    Arguments(
                        ("stage", awakeningStage),
                        ("name", Text(
                            $"fiora.awakening.{awakeningStage}.name")))),
                new[] { description });
        }

        private static CharacterLookupLocalizedText AwakeningDescription(
            int awakeningStage,
            FioraAmberwingResolvedCombatRules before,
            FioraAmberwingResolvedCombatRules after)
        {
            switch (awakeningStage)
            {
                case 2:
                    return Text(
                        "fiora.awakening.2.description",
                        Arguments(
                            ("healing_before", Percent(
                                before.ActionLocalHealingIncreaseRate)),
                            ("healing_after", Percent(
                                after.ActionLocalHealingIncreaseRate))));
                case 3:
                    return Text(
                        "fiora.awakening.3.description",
                        Arguments(
                            ("conversion_before", Percent(
                                before.PassiveConversionRate)),
                            ("conversion_after", Percent(
                                after.PassiveConversionRate))));
                case 4:
                    return Text(
                        "fiora.awakening.4.description",
                        Arguments(
                            ("match3_before", Percent(
                                before.Match3HealingCoefficient)),
                            ("match3_after", Percent(
                                after.Match3HealingCoefficient)),
                            ("match4_before", Percent(
                                before.Match4HealingCoefficient)),
                            ("match4_after", Percent(
                                after.Match4HealingCoefficient))));
                case 5:
                    return Text(
                        "fiora.awakening.5.description",
                        Arguments(
                            ("instant_before", Percent(
                                before.ActiveInstantHealingCoefficient)),
                            ("instant_after", Percent(
                                after.ActiveInstantHealingCoefficient)),
                            ("follow_up_before", Percent(
                                before.ActiveFollowUpHealingCoefficient)),
                            ("follow_up_after", Percent(
                                after.ActiveFollowUpHealingCoefficient))));
                case 6:
                    return Text(
                        "fiora.awakening.6.description",
                        Arguments(
                            ("normal_conversion", Percent(
                                after.PassiveConversionRate)),
                            ("full_overheal_conversion", Percent(
                                after.Awakening6FullOverhealConversionRate))));
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(awakeningStage));
            }
        }

        private static CharacterLookupDescriptionDefinition Skill(
            string titleKey,
            string descriptionKey,
            IReadOnlyDictionary<string, object> arguments)
        {
            return new CharacterLookupDescriptionDefinition(
                Text(titleKey),
                new[] { Text(descriptionKey, arguments) });
        }

        private static FioraAmberwingCombatConfig RequireConfig(
            CharacterDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!string.Equals(
                    definition.Id,
                    FioraAmberwingRules.CharacterId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Fiora's lookup provider requires Fiora's definition.",
                    nameof(definition));
            }

            return definition.CombatConfig as FioraAmberwingCombatConfig
                ?? throw new InvalidOperationException(
                    "Fiora requires FioraAmberwingCombatConfig.");
        }

        private static CharacterLookupLocalizedText Text(
            string key,
            IReadOnlyDictionary<string, object> arguments = null)
        {
            return new CharacterLookupLocalizedText(key, arguments);
        }

        private static IReadOnlyDictionary<string, object> Arguments(
            params (string key, object value)[] values)
        {
            var arguments = new Dictionary<string, object>(
                values.Length,
                StringComparer.Ordinal);
            for (int index = 0; index < values.Length; index++)
            {
                arguments.Add(values[index].key, values[index].value);
            }

            return arguments;
        }

        private static string Percent(double rate)
        {
            return (rate * 100d).ToString(
                "0.##",
                CultureInfo.InvariantCulture);
        }
    }
}
