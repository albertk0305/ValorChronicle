using System;
using System.Collections.Generic;
using System.Globalization;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Characters.Naira
{
    public sealed class NairaWindhornLookupDescriptionProvider
        : ICharacterLookupDescriptionProvider
    {
        private const string AwakeningTitleKey =
            "character.lookup.awakening.title";

        private readonly NairaWindhornBuildModifier buildModifier =
            new NairaWindhornBuildModifier();

        public string CharacterId => NairaWindhornRules.CharacterId;

        public CharacterLookupDescriptionDefinition ResolveSkill(
            CharacterDefinition definition,
            int awakening,
            CharacterLookupSkillType skillType)
        {
            NairaWindhornCombatConfig config = RequireConfig(definition);
            NairaWindhornResolvedCombatRules rules =
                NairaWindhornCombatRuleResolver.Resolve(config, awakening);
            switch (skillType)
            {
                case CharacterLookupSkillType.Match3:
                    return Skill(
                        "naira.skill.match3.name",
                        "naira.skill.match3.description",
                        Arguments(
                            ("shield_rate", Percent(
                                rules.Match3ShieldMaxHpRate)),
                            ("duration", rules.ShieldDurationTurns)));
                case CharacterLookupSkillType.Match4:
                    return Skill(
                        "naira.skill.match4.name",
                        "naira.skill.match4.description",
                        Arguments(
                            ("shield_rate", Percent(
                                rules.Match4ShieldMaxHpRate)),
                            ("duration", rules.ShieldDurationTurns),
                            ("base_coefficient", Percent(
                                rules.Match4BaseDamageCoefficient)),
                            ("charge_bonus", Percent(
                                rules.Match4ChargeDamageIncreaseRate))));
                case CharacterLookupSkillType.Match5:
                    return Skill(
                        "naira.skill.match5.name",
                        rules.AncientGuardianEnabled
                            ? "naira.skill.match5.awakening6_description"
                            : "naira.skill.match5.description",
                        Arguments(
                            ("shield_rate", Percent(
                                rules.Match5ShieldMaxHpRate)),
                            ("duration", rules.ShieldDurationTurns),
                            ("base_coefficient", Percent(
                                rules.Match5BaseDamageCoefficient)),
                            ("charge_bonus", Percent(
                                rules.Match5ChargeDamageIncreaseRate)),
                            ("guardian_reduction", Percent(
                                rules.AncientGuardianDamageReductionRate))));
                case CharacterLookupSkillType.Passive:
                    return Skill(
                        "naira.skill.passive.name",
                        "naira.skill.passive.description",
                        Arguments(
                            ("damage_increase", Percent(
                                rules
                                    .PassiveTargetTakenDamageIncreaseRate))));
                case CharacterLookupSkillType.Active:
                    return Skill(
                        "naira.skill.active.name",
                        "naira.skill.active.description",
                        Arguments(
                            ("shield_rate", Percent(
                                rules.ActiveShieldMaxHpRate)),
                            ("duration", rules.ShieldDurationTurns),
                            ("grace_reduction", Percent(
                                rules
                                    .WhiteStagGraceDamageReductionRate)),
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

            NairaWindhornCombatConfig config = RequireConfig(definition);
            CharacterLookupLocalizedText description;
            if (awakeningStage <= 2)
            {
                description = BuildStatDescription(awakeningStage);
            }
            else
            {
                NairaWindhornResolvedCombatRules before =
                    NairaWindhornCombatRuleResolver.Resolve(
                        config,
                        awakeningStage - 1);
                NairaWindhornResolvedCombatRules after =
                    NairaWindhornCombatRuleResolver.Resolve(
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
                            $"naira.awakening.{awakeningStage}.name")))),
                new[] { description });
        }

        private CharacterLookupLocalizedText BuildStatDescription(
            int awakeningStage)
        {
            var before = buildModifier.GetStatMultipliers(
                awakeningStage - 1);
            var after = buildModifier.GetStatMultipliers(awakeningStage);
            bool maxHp = awakeningStage == 1;
            return Text(
                $"naira.awakening.{awakeningStage}.description",
                Arguments(
                    ("multiplier_before", Percent(maxHp
                        ? before.MaxHpMultiplier
                        : before.AttackMultiplier)),
                    ("multiplier_after", Percent(maxHp
                        ? after.MaxHpMultiplier
                        : after.AttackMultiplier))));
        }

        private static CharacterLookupLocalizedText AwakeningDescription(
            int stage,
            NairaWindhornResolvedCombatRules before,
            NairaWindhornResolvedCombatRules after)
        {
            switch (stage)
            {
                case 3:
                    return Text(
                        "naira.awakening.3.description",
                        Arguments(
                            ("damage_before", Percent(before
                                .PassiveTargetTakenDamageIncreaseRate)),
                            ("damage_after", Percent(after
                                .PassiveTargetTakenDamageIncreaseRate))));
                case 4:
                    return Text(
                        "naira.awakening.4.description",
                        Arguments(
                            ("match3_before", Percent(
                                before.Match3ShieldMaxHpRate)),
                            ("match3_after", Percent(
                                after.Match3ShieldMaxHpRate)),
                            ("match4_before", Percent(
                                before.Match4ShieldMaxHpRate)),
                            ("match4_after", Percent(
                                after.Match4ShieldMaxHpRate))));
                case 5:
                    return Text(
                        "naira.awakening.5.description",
                        Arguments(
                            ("reduction_before", Percent(before
                                .WhiteStagGraceDamageReductionRate)),
                            ("reduction_after", Percent(after
                                .WhiteStagGraceDamageReductionRate))));
                case 6:
                    return Text(
                        "naira.awakening.6.description",
                        Arguments(
                            ("charge", Percent(1d)),
                            ("damage_reduction", Percent(after
                                .AncientGuardianDamageReductionRate))));
                default:
                    throw new ArgumentOutOfRangeException(nameof(stage));
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

        private static NairaWindhornCombatConfig RequireConfig(
            CharacterDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!string.Equals(
                    definition.Id,
                    NairaWindhornRules.CharacterId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Naira's lookup provider requires Naira's definition.",
                    nameof(definition));
            }

            return definition.CombatConfig as NairaWindhornCombatConfig
                ?? throw new InvalidOperationException(
                    "Naira requires NairaWindhornCombatConfig.");
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
