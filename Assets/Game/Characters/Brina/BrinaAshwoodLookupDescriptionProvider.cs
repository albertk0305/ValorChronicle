using System;
using System.Collections.Generic;
using System.Globalization;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Brina
{
    public sealed class BrinaAshwoodLookupDescriptionProvider
        : ICharacterLookupDescriptionProvider
    {
        private const string AwakeningTitleKey =
            "character.lookup.awakening.title";

        private readonly BrinaAshwoodBuildModifier buildModifier =
            new BrinaAshwoodBuildModifier();

        public string CharacterId => BrinaAshwoodRules.CharacterId;

        public CharacterLookupDescriptionDefinition ResolveSkill(
            CharacterDefinition definition,
            int awakening,
            CharacterLookupSkillType skillType)
        {
            BrinaAshwoodCombatConfig config = RequireConfig(definition);
            BrinaAshwoodResolvedCombatRules rules =
                BrinaAshwoodCombatRuleResolver.Resolve(
                    config,
                    awakening);
            switch (skillType)
            {
                case CharacterLookupSkillType.Match3:
                    return MatchSkill(
                        "match3",
                        rules.Match3Coefficient,
                        rules.Match3WoodcutGainAmount);
                case CharacterLookupSkillType.Match4:
                    return MatchSkill(
                        "match4",
                        rules.Match4Coefficient,
                        rules.Match4WoodcutGainAmount);
                case CharacterLookupSkillType.Match5:
                    return MatchSkill(
                        "match5",
                        rules.Match5Coefficient,
                        rules.Match5WoodcutGainAmount);
                case CharacterLookupSkillType.Passive:
                    return Skill(
                        "brina.skill.passive.name",
                        "brina.skill.passive.description",
                        Arguments(
                            ("threshold",
                                rules.WoodcutTriggerThreshold),
                            ("coefficient", Percent(
                                rules.PassiveAdditionalCoefficient))));
                case CharacterLookupSkillType.Active:
                    return Skill(
                        "brina.skill.active.name",
                        "brina.skill.active.description",
                        Arguments(
                            ("woodcut_multiplier", Percent(
                                rules.ActiveWoodcutGainMultiplier)),
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

            BrinaAshwoodCombatConfig config = RequireConfig(definition);
            CharacterLookupLocalizedText description;
            if (awakeningStage <= 2)
            {
                description = BuildStatDescription(awakeningStage);
            }
            else
            {
                BrinaAshwoodResolvedCombatRules before =
                    BrinaAshwoodCombatRuleResolver.Resolve(
                        config,
                        awakeningStage - 1);
                BrinaAshwoodResolvedCombatRules after =
                    BrinaAshwoodCombatRuleResolver.Resolve(
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
                            $"brina.awakening.{awakeningStage}.name")))),
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
                $"brina.awakening.{awakeningStage}.description",
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
            BrinaAshwoodResolvedCombatRules before,
            BrinaAshwoodResolvedCombatRules after)
        {
            switch (stage)
            {
                case 3:
                    return Text(
                        "brina.awakening.3.description",
                        Arguments(
                            ("coefficient_before", Percent(
                                before.PassiveAdditionalCoefficient)),
                            ("coefficient_after", Percent(
                                after.PassiveAdditionalCoefficient))));
                case 4:
                    return Text(
                        "brina.awakening.4.description",
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
                        "brina.awakening.5.description",
                        Arguments(
                            ("cooldown_before",
                                before.ActiveCooldownTurns),
                            ("cooldown_after",
                                after.ActiveCooldownTurns)));
                case 6:
                    return Text(
                        "brina.awakening.6.description",
                        Arguments(
                            ("first_gain_before",
                                before.FirstWoodcutBonusAmount),
                            ("first_gain_after",
                                after.FirstWoodcutBonusAmount)));
                default:
                    throw new ArgumentOutOfRangeException(nameof(stage));
            }
        }

        private static CharacterLookupDescriptionDefinition MatchSkill(
            string tier,
            double coefficient,
            int woodcutGain)
        {
            return Skill(
                $"brina.skill.{tier}.name",
                $"brina.skill.{tier}.description",
                Arguments(
                    ("coefficient", Percent(coefficient)),
                    ("woodcut_gain", woodcutGain)));
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

        private static BrinaAshwoodCombatConfig RequireConfig(
            CharacterDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!string.Equals(
                    definition.Id,
                    BrinaAshwoodRules.CharacterId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Brina's lookup provider requires Brina's definition.",
                    nameof(definition));
            }

            return definition.CombatConfig as BrinaAshwoodCombatConfig
                ?? throw new InvalidOperationException(
                    "Brina requires BrinaAshwoodCombatConfig.");
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
