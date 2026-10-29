using System;
using System.Collections.Generic;
using System.Globalization;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Koharu
{
    public sealed class SakuragiKoharuLookupDescriptionProvider
        : ICharacterLookupDescriptionProvider
    {
        private readonly SakuragiKoharuBuildModifier buildModifier =
            new SakuragiKoharuBuildModifier();

        public string CharacterId => SakuragiKoharuRules.CharacterId;

        public CharacterLookupDescriptionDefinition ResolveSkill(
            CharacterDefinition definition,
            int awakening,
            CharacterLookupSkillType skillType)
        {
            SakuragiKoharuResolvedCombatRules rules =
                SakuragiKoharuCombatRuleResolver.Resolve(
                    RequireConfig(definition),
                    awakening);
            switch (skillType)
            {
                case CharacterLookupSkillType.Match3:
                    return Skill(
                        "koharu.skill.match3.name",
                        "koharu.skill.match3.description",
                        Arguments(
                            ("coefficient", Percent(rules.Match3Coefficient)),
                            ("spirit_gain", rules.Match3SpiritEnergyGain)));
                case CharacterLookupSkillType.Match4:
                    return Skill(
                        "koharu.skill.match4.name",
                        "koharu.skill.match4.description",
                        Arguments(
                            ("coefficient", Percent(rules.Match4Coefficient)),
                            ("spirit_gain", rules.Match4SpiritEnergyGain)));
                case CharacterLookupSkillType.Match5:
                    return Skill(
                        "koharu.skill.match5.name",
                        "koharu.skill.match5.description",
                        Arguments(
                            ("coefficient", Percent(rules.Match5Coefficient)),
                            ("spirit_gain", rules.Match5SpiritEnergyGain),
                            ("damage_increase", Percent(rules
                                .Match5AdditionalAndChaseDamageIncreaseRate)),
                            ("duration", rules.Match5EffectDurationTurns)));
                case CharacterLookupSkillType.Passive:
                    return Skill(
                        "koharu.skill.passive.name",
                        "koharu.skill.passive.description",
                        Arguments(("spirit_cost",
                            rules.SpiritEnergyCostPerCooldownReduction)));
                case CharacterLookupSkillType.Active:
                {
                    var sections = new List<CharacterLookupLocalizedText>
                    {
                        Text(
                            "koharu.skill.active.description",
                            Arguments(
                                ("block_count",
                                    rules.ActiveBlockConversionCount),
                                ("cooldown", rules.ActiveCooldownTurns)))
                    };
                    if (rules
                        .HasAwakening6AdditionalAndChaseDamageIncrease)
                    {
                        sections.Add(Text(
                            "koharu.skill.active.awakening6",
                            Arguments(
                                ("damage_increase", Percent(rules
                                    .Awakening6AdditionalAndChaseDamageIncreaseRate)),
                                ("duration",
                                    rules.Awakening6EffectDurationTurns))));
                    }

                    return new CharacterLookupDescriptionDefinition(
                        Text("koharu.skill.active.name"),
                        sections.AsReadOnly());
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(skillType));
            }
        }

        public CharacterLookupDescriptionDefinition ResolveAwakening(
            CharacterDefinition definition,
            int awakeningStage)
        {
            if (awakeningStage < 1 || awakeningStage > 6)
            {
                throw new ArgumentOutOfRangeException(nameof(awakeningStage));
            }

            SakuragiKoharuCombatConfig config = RequireConfig(definition);
            IReadOnlyDictionary<string, object> arguments;
            if (awakeningStage <= 2)
            {
                CharacterBuildStatMultipliers before = buildModifier
                    .GetStatMultipliers(awakeningStage - 1);
                CharacterBuildStatMultipliers after = buildModifier
                    .GetStatMultipliers(awakeningStage);
                bool hp = awakeningStage == 1;
                arguments = Arguments(
                    ("multiplier_before", Percent(hp
                        ? before.MaxHpMultiplier
                        : before.AttackMultiplier)),
                    ("multiplier_after", Percent(hp
                        ? after.MaxHpMultiplier
                        : after.AttackMultiplier)));
            }
            else
            {
                SakuragiKoharuResolvedCombatRules before =
                    SakuragiKoharuCombatRuleResolver.Resolve(
                        config,
                        awakeningStage - 1);
                SakuragiKoharuResolvedCombatRules after =
                    SakuragiKoharuCombatRuleResolver.Resolve(
                        config,
                        awakeningStage);
                arguments = AwakeningArguments(
                    awakeningStage,
                    before,
                    after);
            }

            return new CharacterLookupDescriptionDefinition(
                Text(
                    "character.lookup.awakening.title",
                    Arguments(
                        ("stage", awakeningStage),
                        ("name", Text(
                            $"koharu.awakening.{awakeningStage}.name")))),
                new[]
                {
                    Text(
                        $"koharu.awakening.{awakeningStage}.description",
                        arguments)
                });
        }

        private static IReadOnlyDictionary<string, object>
            AwakeningArguments(
                int stage,
                SakuragiKoharuResolvedCombatRules before,
                SakuragiKoharuResolvedCombatRules after)
        {
            switch (stage)
            {
                case 3:
                    return Arguments(
                        ("match3_before", Percent(before.Match3Coefficient)),
                        ("match3_after", Percent(after.Match3Coefficient)),
                        ("match4_before", Percent(before.Match4Coefficient)),
                        ("match4_after", Percent(after.Match4Coefficient)));
                case 4:
                    return Arguments(
                        ("match5_before", Percent(before.Match5Coefficient)),
                        ("match5_after", Percent(after.Match5Coefficient)),
                        ("buff_before", Percent(before
                            .Match5AdditionalAndChaseDamageIncreaseRate)),
                        ("buff_after", Percent(after
                            .Match5AdditionalAndChaseDamageIncreaseRate)));
                case 5:
                    return Arguments(
                        ("spirit_cost_before",
                            before.SpiritEnergyCostPerCooldownReduction),
                        ("spirit_cost_after",
                            after.SpiritEnergyCostPerCooldownReduction));
                case 6:
                    return Arguments(
                        ("damage_increase", Percent(after
                            .Awakening6AdditionalAndChaseDamageIncreaseRate)),
                        ("duration", after.Awakening6EffectDurationTurns));
                default:
                    throw new ArgumentOutOfRangeException(nameof(stage));
            }
        }

        private static SakuragiKoharuCombatConfig RequireConfig(
            CharacterDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!string.Equals(
                    definition.Id,
                    SakuragiKoharuRules.CharacterId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Koharu's lookup provider requires Koharu's definition.",
                    nameof(definition));
            }

            return definition.CombatConfig as SakuragiKoharuCombatConfig
                ?? throw new InvalidOperationException(
                    "Koharu requires SakuragiKoharuCombatConfig.");
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

        private static CharacterLookupLocalizedText Text(
            string key,
            IReadOnlyDictionary<string, object> arguments = null)
        {
            return new CharacterLookupLocalizedText(key, arguments);
        }

        private static IReadOnlyDictionary<string, object> Arguments(
            params (string key, object value)[] values)
        {
            var result = new Dictionary<string, object>(
                values.Length,
                StringComparer.Ordinal);
            for (int index = 0; index < values.Length; index++)
            {
                result.Add(values[index].key, values[index].value);
            }

            return result;
        }

        private static string Percent(double rate)
        {
            return (rate * 100d).ToString(
                "0.##",
                CultureInfo.InvariantCulture);
        }
    }
}
