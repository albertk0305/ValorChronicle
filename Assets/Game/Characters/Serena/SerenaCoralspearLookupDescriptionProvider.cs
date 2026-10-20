using System;
using System.Collections.Generic;
using System.Globalization;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Serena
{
    public sealed class SerenaCoralspearLookupDescriptionProvider
        : ICharacterLookupDescriptionProvider
    {
        private const string AwakeningTitleKey =
            "character.lookup.awakening.title";

        private readonly SerenaCoralspearBuildModifier buildModifier =
            new SerenaCoralspearBuildModifier();

        public string CharacterId => SerenaCoralspearRules.CharacterId;

        public CharacterLookupDescriptionDefinition ResolveSkill(
            CharacterDefinition definition,
            int awakening,
            CharacterLookupSkillType skillType)
        {
            SerenaCoralspearCombatConfig config = RequireConfig(definition);
            SerenaCoralspearResolvedCombatRules rules =
                SerenaCoralspearCombatRuleResolver.Resolve(
                    config,
                    awakening);
            switch (skillType)
            {
                case CharacterLookupSkillType.Match3:
                    return Skill(
                        "serena.skill.match3.name",
                        "serena.skill.match3.description",
                        Arguments(
                            ("coefficient", Percent(
                                rules.Match3Coefficient)),
                            ("water_gain",
                                rules.Match3WaterElementGainAmount)));
                case CharacterLookupSkillType.Match4:
                    return Skill(
                        "serena.skill.match4.name",
                        "serena.skill.match4.description",
                        Arguments(
                            ("base_coefficient", Percent(
                                rules.Match4BaseCoefficient)),
                            ("additional_coefficient", Percent(
                                rules.Match4AdditionalCoefficient))));
                case CharacterLookupSkillType.Match5:
                    return Skill(
                        "serena.skill.match5.name",
                        "serena.skill.match5.description",
                        Arguments(
                            ("coefficient", Percent(
                                rules.Match5Coefficient)),
                            ("empowered_chase_gain", 1),
                            ("water_gain",
                                rules.Match5WaterElementGainAmount)));
                case CharacterLookupSkillType.Passive:
                    return Skill(
                        "serena.skill.passive.name",
                        "serena.skill.passive.description",
                        Arguments(
                            ("per_water_coefficient", Percent(
                                rules
                                    .PassiveChaseCoefficientPerWaterElement)),
                            ("damage_increase", Percent(
                                rules
                                    .AdditionalAndChaseDamageIncreaseRate)),
                            ("empowered_damage_increase", Percent(
                                rules.EmpoweredChaseDamageIncreaseRate)),
                            ("max_charges",
                                rules.EmpoweredChaseMaxCharges)));
                case CharacterLookupSkillType.Active:
                    return Skill(
                        "serena.skill.active.name",
                        "serena.skill.active.description",
                        Arguments(
                            ("damage_increase", Percent(
                                rules
                                    .ActiveAdditionalAndChaseDamageIncreaseRate)),
                            ("duration", rules.ActiveDurationTurns),
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

            SerenaCoralspearCombatConfig config = RequireConfig(definition);
            CharacterLookupLocalizedText description;
            if (awakeningStage == 1)
            {
                var before = buildModifier.GetStatMultipliers(0);
                var after = buildModifier.GetStatMultipliers(1);
                description = Text(
                    "serena.awakening.1.description",
                    Arguments(
                        ("multiplier_before", Percent(
                            before.AttackMultiplier)),
                        ("multiplier_after", Percent(
                            after.AttackMultiplier))));
            }
            else
            {
                SerenaCoralspearResolvedCombatRules before =
                    SerenaCoralspearCombatRuleResolver.Resolve(
                        config,
                        awakeningStage - 1);
                SerenaCoralspearResolvedCombatRules after =
                    SerenaCoralspearCombatRuleResolver.Resolve(
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
                            $"serena.awakening.{awakeningStage}.name")))),
                new[] { description });
        }

        private static CharacterLookupLocalizedText AwakeningDescription(
            int awakeningStage,
            SerenaCoralspearResolvedCombatRules before,
            SerenaCoralspearResolvedCombatRules after)
        {
            switch (awakeningStage)
            {
                case 2:
                    return Text(
                        "serena.awakening.2.description",
                        Arguments(
                            ("damage_before", Percent(before
                                .AdditionalAndChaseDamageIncreaseRate)),
                            ("damage_after", Percent(after
                                .AdditionalAndChaseDamageIncreaseRate))));
                case 3:
                    return Text(
                        "serena.awakening.3.description",
                        Arguments(
                            ("chase_before", Percent(before
                                .PassiveChaseCoefficientPerWaterElement)),
                            ("chase_after", Percent(after
                                .PassiveChaseCoefficientPerWaterElement))));
                case 4:
                    return Text(
                        "serena.awakening.4.description",
                        Arguments(
                            ("match3_before", Percent(
                                before.Match3Coefficient)),
                            ("match3_after", Percent(
                                after.Match3Coefficient)),
                            ("match4_base_before", Percent(
                                before.Match4BaseCoefficient)),
                            ("match4_base_after", Percent(
                                after.Match4BaseCoefficient)),
                            ("match4_additional_before", Percent(
                                before.Match4AdditionalCoefficient)),
                            ("match4_additional_after", Percent(
                                after.Match4AdditionalCoefficient))));
                case 5:
                    return Text(
                        "serena.awakening.5.description",
                        Arguments(
                            ("active_before", Percent(before
                                .ActiveAdditionalAndChaseDamageIncreaseRate)),
                            ("active_after", Percent(after
                                .ActiveAdditionalAndChaseDamageIncreaseRate))));
                case 6:
                    return Text(
                        "serena.awakening.6.description",
                        Arguments(
                            ("water_before",
                                before.Match5WaterElementGainAmount),
                            ("water_after",
                                after.Match5WaterElementGainAmount)));
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

        private static SerenaCoralspearCombatConfig RequireConfig(
            CharacterDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!string.Equals(
                    definition.Id,
                    SerenaCoralspearRules.CharacterId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Serena's lookup provider requires Serena's definition.",
                    nameof(definition));
            }

            return definition.CombatConfig as SerenaCoralspearCombatConfig
                ?? throw new InvalidOperationException(
                    "Serena requires SerenaCoralspearCombatConfig.");
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
