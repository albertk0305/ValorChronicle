using System;
using System.Collections.Generic;
using System.Globalization;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Miyuki
{
    public sealed class ShimotsukiMiyukiLookupDescriptionProvider
        : ICharacterLookupDescriptionProvider
    {
        private const string AwakeningTitleKey =
            "character.lookup.awakening.title";

        private readonly ShimotsukiMiyukiBuildModifier buildModifier =
            new ShimotsukiMiyukiBuildModifier();

        public string CharacterId => ShimotsukiMiyukiRules.CharacterId;

        public CharacterLookupDescriptionDefinition ResolveSkill(
            CharacterDefinition definition,
            int awakening,
            CharacterLookupSkillType skillType)
        {
            ShimotsukiMiyukiCombatConfig config = RequireConfig(definition);
            ShimotsukiMiyukiResolvedCombatRules rules =
                ShimotsukiMiyukiCombatRuleResolver.Resolve(config, awakening);
            switch (skillType)
            {
                case CharacterLookupSkillType.Match3:
                    return Skill(
                        "miyuki.skill.match3.name",
                        "miyuki.skill.match3.description",
                        Arguments(
                            ("coefficient", Percent(
                                rules.Match3Coefficient)),
                            ("water_gain", rules.Match3WaterAmount)));
                case CharacterLookupSkillType.Match4:
                    return Skill(
                        "miyuki.skill.match4.name",
                        "miyuki.skill.match4.description",
                        Arguments(
                            ("coefficient", Percent(
                                rules.Match4Coefficient)),
                            ("water_gain", rules.Match4WaterAmount)));
                case CharacterLookupSkillType.Match5:
                    return Skill(
                        "miyuki.skill.match5.name",
                        "miyuki.skill.match5.description",
                        Arguments(
                            ("coefficient", Percent(
                                rules.Match5Coefficient)),
                            ("water_gain", rules.Match5WaterAmount),
                            ("damage_increase", Percent(
                                rules.Match5WaterDamageIncreaseRate)),
                            ("duration",
                                rules.Match5EffectDurationTurns)));
                case CharacterLookupSkillType.Passive:
                    return Skill(
                        "miyuki.skill.passive.name",
                        "miyuki.skill.passive.description",
                        Arguments(
                            ("damage_increase", Percent(rules
                                .PassiveWaterDamageIncreaseRate))));
                case CharacterLookupSkillType.Active:
                {
                    var sections = new List<CharacterLookupLocalizedText>
                    {
                        Text(
                            "miyuki.skill.active.description",
                            Arguments(
                                ("block_count",
                                    rules.ActiveBlockConversionCount),
                                ("cooldown",
                                    rules.ActiveCooldownTurns)))
                    };
                    if (rules.HasFrostEcho)
                    {
                        sections.Add(Text(
                            "miyuki.skill.frost_echo.grant",
                            Arguments(
                                ("water_gain",
                                    rules.FrostEchoExtraWaterAmount))));
                    }

                    return new CharacterLookupDescriptionDefinition(
                        Text("miyuki.skill.active.name"),
                        sections.AsReadOnly());
                }
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

            ShimotsukiMiyukiCombatConfig config = RequireConfig(definition);
            CharacterLookupLocalizedText description;
            if (awakeningStage <= 2)
            {
                var before = buildModifier.GetStatMultipliers(
                    awakeningStage - 1);
                var after = buildModifier.GetStatMultipliers(awakeningStage);
                bool hp = awakeningStage == 1;
                description = Text(
                    $"miyuki.awakening.{awakeningStage}.description",
                    Arguments(
                        ("multiplier_before", Percent(hp
                            ? before.MaxHpMultiplier
                            : before.AttackMultiplier)),
                        ("multiplier_after", Percent(hp
                            ? after.MaxHpMultiplier
                            : after.AttackMultiplier))));
            }
            else
            {
                ShimotsukiMiyukiResolvedCombatRules before =
                    ShimotsukiMiyukiCombatRuleResolver.Resolve(
                        config,
                        awakeningStage - 1);
                ShimotsukiMiyukiResolvedCombatRules after =
                    ShimotsukiMiyukiCombatRuleResolver.Resolve(
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
                            $"miyuki.awakening.{awakeningStage}.name")))),
                new[] { description });
        }

        private static CharacterLookupLocalizedText AwakeningDescription(
            int awakeningStage,
            ShimotsukiMiyukiResolvedCombatRules before,
            ShimotsukiMiyukiResolvedCombatRules after)
        {
            switch (awakeningStage)
            {
                case 3:
                    return Text(
                        "miyuki.awakening.3.description",
                        Arguments(
                            ("passive_before", Percent(before
                                .PassiveWaterDamageIncreaseRate)),
                            ("passive_after", Percent(after
                                .PassiveWaterDamageIncreaseRate))));
                case 4:
                    return Text(
                        "miyuki.awakening.4.description",
                        Arguments(
                            ("match3_before", Percent(
                                before.Match3Coefficient)),
                            ("match3_after", Percent(
                                after.Match3Coefficient)),
                            ("match4_before", Percent(
                                before.Match4Coefficient)),
                            ("match4_after", Percent(
                                after.Match4Coefficient))));
                case 5:
                    return Text(
                        "miyuki.awakening.5.description",
                        Arguments(
                            ("blocks_before",
                                before.ActiveBlockConversionCount),
                            ("blocks_after",
                                after.ActiveBlockConversionCount)));
                case 6:
                    return Text(
                        "miyuki.awakening.6.description",
                        Arguments(
                            ("water_gain",
                                after.FrostEchoExtraWaterAmount)));
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

        private static ShimotsukiMiyukiCombatConfig RequireConfig(
            CharacterDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!string.Equals(
                    definition.Id,
                    ShimotsukiMiyukiRules.CharacterId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Miyuki's lookup provider requires Miyuki's definition.",
                    nameof(definition));
            }

            return definition.CombatConfig as ShimotsukiMiyukiCombatConfig
                ?? throw new InvalidOperationException(
                    "Miyuki requires ShimotsukiMiyukiCombatConfig.");
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
