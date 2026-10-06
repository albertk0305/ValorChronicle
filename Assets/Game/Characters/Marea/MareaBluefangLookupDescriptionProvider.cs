using System;
using System.Collections.Generic;
using System.Globalization;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Marea
{
    public sealed class MareaBluefangLookupDescriptionProvider
        : ICharacterLookupDescriptionProvider
    {
        private const string AwakeningTitleKey =
            "character.lookup.awakening.title";

        private readonly MareaBluefangBuildModifier buildModifier =
            new MareaBluefangBuildModifier();

        public string CharacterId => MareaBluefangRules.CharacterId;

        public CharacterLookupDescriptionDefinition ResolveSkill(
            CharacterDefinition definition,
            int awakening,
            CharacterLookupSkillType skillType)
        {
            MareaBluefangCombatConfig config = RequireConfig(definition);
            MareaBluefangResolvedCombatRules rules =
                MareaBluefangCombatRuleResolver.Resolve(config, awakening);
            switch (skillType)
            {
                case CharacterLookupSkillType.Match3:
                    return Skill(
                        "marea.skill.match3.name",
                        "marea.skill.match3.description",
                        Arguments(
                            ("coefficient", Percent(
                                rules.Match3Coefficient))),
                        LingeringUseFragment(rules));
                case CharacterLookupSkillType.Match4:
                    return Skill(
                        "marea.skill.match4.name",
                        "marea.skill.match4.description",
                        Arguments(
                            ("base_coefficient", Percent(
                                rules.Match4BaseCoefficient)),
                            ("conditional_coefficient", Percent(
                                rules.Match4WaterElementBonusCoefficient))),
                        LingeringUseFragment(rules));
                case CharacterLookupSkillType.Match5:
                    return Skill(
                        "marea.skill.match5.name",
                        "marea.skill.match5.description",
                        Arguments(
                            ("base_coefficient", Percent(
                                rules.Match5BaseCoefficient)),
                            ("per_water_coefficient", Percent(
                                rules.Match5CoefficientPerWaterElement))),
                        LingeringGrantFragment(rules));
                case CharacterLookupSkillType.Passive:
                    return Skill(
                        "marea.skill.passive.name",
                        "marea.skill.passive.description",
                        Arguments(
                            ("damage_increase", Percent(
                                rules.PassiveDealtDamageIncreaseRate))));
                case CharacterLookupSkillType.Active:
                    return Skill(
                        "marea.skill.active.name",
                        "marea.skill.active.description",
                        Arguments(
                            ("damage_increase", Percent(
                                config.ActiveWaterDamageIncreaseRate)),
                            ("duration", config.ActiveDurationTurns),
                            ("cooldown", config.ActiveCooldownTurns)));
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

            MareaBluefangCombatConfig config = RequireConfig(definition);
            CharacterLookupLocalizedText description;
            switch (awakeningStage)
            {
                case 1:
                    description = BuildStatDescription(
                        awakeningStage,
                        attack: true);
                    break;
                case 2:
                    description = BuildStatDescription(
                        awakeningStage,
                        attack: false);
                    break;
                case 3:
                {
                    MareaBluefangResolvedCombatRules before =
                        Resolve(config, awakeningStage - 1);
                    MareaBluefangResolvedCombatRules after =
                        Resolve(config, awakeningStage);
                    description = Text(
                        "marea.awakening.3.description",
                        Arguments(
                            ("passive_before", Percent(
                                before.PassiveDealtDamageIncreaseRate)),
                            ("passive_after", Percent(
                                after.PassiveDealtDamageIncreaseRate))));
                    break;
                }
                case 4:
                {
                    MareaBluefangResolvedCombatRules before =
                        Resolve(config, awakeningStage - 1);
                    MareaBluefangResolvedCombatRules after =
                        Resolve(config, awakeningStage);
                    description = Text(
                        "marea.awakening.4.description",
                        Arguments(
                            ("match3_before", Percent(
                                before.Match3Coefficient)),
                            ("match3_after", Percent(
                                after.Match3Coefficient)),
                            ("match4_base_before", Percent(
                                before.Match4BaseCoefficient)),
                            ("match4_base_after", Percent(
                                after.Match4BaseCoefficient)),
                            ("match4_conditional_before", Percent(
                                before.Match4WaterElementBonusCoefficient)),
                            ("match4_conditional_after", Percent(
                                after.Match4WaterElementBonusCoefficient))));
                    break;
                }
                case 5:
                {
                    MareaBluefangResolvedCombatRules before =
                        Resolve(config, awakeningStage - 1);
                    MareaBluefangResolvedCombatRules after =
                        Resolve(config, awakeningStage);
                    description = Text(
                        "marea.awakening.5.description",
                        Arguments(
                            ("per_water_before", Percent(
                                before.Match5CoefficientPerWaterElement)),
                            ("per_water_after", Percent(
                                after.Match5CoefficientPerWaterElement))));
                    break;
                }
                case 6:
                {
                    MareaBluefangResolvedCombatRules before =
                        Resolve(config, awakeningStage - 1);
                    MareaBluefangResolvedCombatRules after =
                        Resolve(config, awakeningStage);
                    if (before.HasLingeringSurge
                        || !after.HasLingeringSurge)
                    {
                        throw new InvalidOperationException(
                            "Lingering Surge must be introduced at Awakening 6.");
                    }

                    description = Text(
                        "marea.awakening.6.description",
                        LingeringArguments(after));
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(awakeningStage));
            }

            return new CharacterLookupDescriptionDefinition(
                Text(
                    AwakeningTitleKey,
                    Arguments(
                        ("stage", awakeningStage),
                        ("name", Text(
                            $"marea.awakening.{awakeningStage}.name")))),
                new[] { description });
        }

        private CharacterLookupLocalizedText BuildStatDescription(
            int awakeningStage,
            bool attack)
        {
            var before = buildModifier.GetStatMultipliers(
                awakeningStage - 1);
            var after = buildModifier.GetStatMultipliers(awakeningStage);
            return Text(
                attack
                    ? "marea.awakening.1.description"
                    : "marea.awakening.2.description",
                Arguments(
                    ("multiplier_before", Percent(
                        attack
                            ? before.AttackMultiplier
                            : before.MaxHpMultiplier)),
                    ("multiplier_after", Percent(
                        attack
                            ? after.AttackMultiplier
                            : after.MaxHpMultiplier))));
        }

        private static CharacterLookupDescriptionDefinition Skill(
            string titleKey,
            string descriptionKey,
            IReadOnlyDictionary<string, object> arguments,
            CharacterLookupLocalizedText conditionalFragment = null)
        {
            var sections = new List<CharacterLookupLocalizedText>
            {
                Text(descriptionKey, arguments)
            };
            if (conditionalFragment != null)
            {
                sections.Add(conditionalFragment);
            }

            return new CharacterLookupDescriptionDefinition(
                Text(titleKey),
                sections.AsReadOnly());
        }

        private static CharacterLookupLocalizedText LingeringUseFragment(
            MareaBluefangResolvedCombatRules rules)
        {
            return rules.HasLingeringSurge
                ? Text(
                    "marea.skill.lingering_surge.use",
                    Arguments(
                        ("damage_increase", Percent(
                            rules.LingeringSurgeDamageIncreaseRate))))
                : null;
        }

        private static CharacterLookupLocalizedText LingeringGrantFragment(
            MareaBluefangResolvedCombatRules rules)
        {
            return rules.HasLingeringSurge
                ? Text(
                    "marea.skill.lingering_surge.grant",
                    LingeringArguments(rules))
                : null;
        }

        private static IReadOnlyDictionary<string, object> LingeringArguments(
            MareaBluefangResolvedCombatRules rules)
        {
            return Arguments(
                ("required_water", MareaBluefangCombatRuleResolver
                    .LingeringSurgeRequiredWaterAmount),
                ("damage_increase", Percent(
                    rules.LingeringSurgeDamageIncreaseRate)));
        }

        private static MareaBluefangResolvedCombatRules Resolve(
            MareaBluefangCombatConfig config,
            int awakening)
        {
            return MareaBluefangCombatRuleResolver.Resolve(config, awakening);
        }

        private static MareaBluefangCombatConfig RequireConfig(
            CharacterDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!string.Equals(
                    definition.Id,
                    MareaBluefangRules.CharacterId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Marea's lookup provider requires Marea's definition.",
                    nameof(definition));
            }

            return definition.CombatConfig as MareaBluefangCombatConfig
                ?? throw new InvalidOperationException(
                    "Marea requires MareaBluefangCombatConfig.");
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
