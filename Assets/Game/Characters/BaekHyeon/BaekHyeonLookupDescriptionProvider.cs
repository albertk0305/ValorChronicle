using System;
using System.Collections.Generic;
using System.Globalization;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.BaekHyeon
{
    public sealed class BaekHyeonLookupDescriptionProvider
        : ICharacterLookupDescriptionProvider
    {
        private const string AwakeningTitleKey =
            "character.lookup.awakening.title";

        private readonly BaekHyeonBuildModifier buildModifier =
            new BaekHyeonBuildModifier();

        public string CharacterId => BaekHyeonRules.CharacterId;

        public CharacterLookupDescriptionDefinition ResolveSkill(
            CharacterDefinition definition,
            int awakening,
            CharacterLookupSkillType skillType)
        {
            BaekHyeonCombatConfig config = RequireConfig(definition);
            BaekHyeonResolvedCombatRules rules =
                BaekHyeonCombatRuleResolver.Resolve(config, awakening);
            switch (skillType)
            {
                case CharacterLookupSkillType.Match3:
                    return MatchSkill("match3", 3, rules);
                case CharacterLookupSkillType.Match4:
                    return MatchSkill("match4", 4, rules);
                case CharacterLookupSkillType.Match5:
                    return MatchSkill("match5", 5, rules);
                case CharacterLookupSkillType.Passive:
                    return Skill(
                        "baek_hyeon.skill.passive.name",
                        "baek_hyeon.skill.passive.description",
                        Arguments(
                            ("match3_mark_gain",
                                rules.Match3MarkGainAmount),
                            ("match4_mark_gain",
                                rules.Match4MarkGainAmount),
                            ("match5_mark_gain",
                                rules.Match5MarkGainAmount),
                            ("mark_max", rules.MarkMaxStackCount),
                            ("additional_damage_increase_per_mark",
                                Percent(rules
                                    .MarkDamageIncreaseRatePerStack)),
                            ("additional_damage_increase_max",
                                Percent(rules
                                    .CalculateMarkDamageIncreaseRate(
                                        rules.MarkMaxStackCount)))));
                case CharacterLookupSkillType.Active:
                    return Skill(
                        "baek_hyeon.skill.active.name",
                        "baek_hyeon.skill.active.description",
                        Arguments(
                            ("base_coefficient", Percent(
                                rules.ActiveBaseCoefficient)),
                            ("per_mark_coefficient", Percent(
                                rules.ActiveMarkCoefficient)),
                            ("mark_max", rules.MarkMaxStackCount),
                            ("max_coefficient", Percent(
                                rules.CalculateActiveCoefficient(
                                    rules.MarkMaxStackCount))),
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

            BaekHyeonCombatConfig config = RequireConfig(definition);
            CharacterLookupLocalizedText description;
            if (awakeningStage <= 2)
            {
                description = BuildStatDescription(awakeningStage);
            }
            else
            {
                BaekHyeonResolvedCombatRules before =
                    BaekHyeonCombatRuleResolver.Resolve(
                        config,
                        awakeningStage - 1);
                BaekHyeonResolvedCombatRules after =
                    BaekHyeonCombatRuleResolver.Resolve(
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
                            $"baek_hyeon.awakening."
                                + $"{awakeningStage}.name")))),
                new[] { description });
        }

        private CharacterLookupLocalizedText BuildStatDescription(
            int awakeningStage)
        {
            var before = buildModifier.GetStatMultipliers(
                awakeningStage - 1);
            var after = buildModifier.GetStatMultipliers(awakeningStage);
            bool attack = awakeningStage == 1;
            return Text(
                $"baek_hyeon.awakening.{awakeningStage}.description",
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

        private static CharacterLookupLocalizedText AwakeningDescription(
            int stage,
            BaekHyeonResolvedCombatRules before,
            BaekHyeonResolvedCombatRules after)
        {
            switch (stage)
            {
                case 3:
                    return Text(
                        "baek_hyeon.awakening.3.description",
                        Arguments(
                            ("per_mark_before", Percent(before
                                .MarkDamageIncreaseRatePerStack)),
                            ("per_mark_after", Percent(after
                                .MarkDamageIncreaseRatePerStack)),
                            ("maximum_before", Percent(before
                                .CalculateMarkDamageIncreaseRate(
                                    before.MarkMaxStackCount))),
                            ("maximum_after", Percent(after
                                .CalculateMarkDamageIncreaseRate(
                                    after.MarkMaxStackCount)))));
                case 4:
                    return Text(
                        "baek_hyeon.awakening.4.description",
                        Arguments(
                            ("match3_before", Percent(
                                before.Match3BaseCoefficient)),
                            ("match3_after", Percent(
                                after.Match3BaseCoefficient)),
                            ("match4_before", Percent(
                                before.Match4BaseCoefficient)),
                            ("match4_after", Percent(
                                after.Match4BaseCoefficient))));
                case 5:
                    return Text(
                        "baek_hyeon.awakening.5.description",
                        Arguments(
                            ("base_before", Percent(
                                before.ActiveBaseCoefficient)),
                            ("base_after", Percent(
                                after.ActiveBaseCoefficient)),
                            ("per_mark_before", Percent(
                                before.ActiveMarkCoefficient)),
                            ("per_mark_after", Percent(
                                after.ActiveMarkCoefficient))));
                case 6:
                    return Text(
                        "baek_hyeon.awakening.6.description",
                        Arguments(
                            ("damage_increase", Percent(
                                after.ExposureDamageIncreaseRate)),
                            ("duration", after.ExposureDurationTurns)));
                default:
                    throw new ArgumentOutOfRangeException(nameof(stage));
            }
        }

        private static CharacterLookupDescriptionDefinition MatchSkill(
            string tier,
            int matchSize,
            BaekHyeonResolvedCombatRules rules)
        {
            return Skill(
                $"baek_hyeon.skill.{tier}.name",
                $"baek_hyeon.skill.{tier}.description",
                Arguments(
                    ("base_coefficient", Percent(
                        rules.GetMatchBaseCoefficient(matchSize))),
                    ("per_mark_coefficient", Percent(
                        rules.GetMatchMarkCoefficient(matchSize))),
                    ("mark_gain", rules.GetMarkGainAmount(matchSize)),
                    ("mark_max", rules.MarkMaxStackCount),
                    ("max_coefficient", Percent(
                        rules.CalculateMatchCoefficient(
                            matchSize,
                            rules.MarkMaxStackCount)))));
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

        private static BaekHyeonCombatConfig RequireConfig(
            CharacterDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!string.Equals(
                    definition.Id,
                    BaekHyeonRules.CharacterId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Baek Hyeon's lookup provider requires Baek Hyeon's "
                        + "definition.",
                    nameof(definition));
            }

            return definition.CombatConfig as BaekHyeonCombatConfig
                ?? throw new InvalidOperationException(
                    "Baek Hyeon requires BaekHyeonCombatConfig.");
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
