using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Characters.Stats;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Localization;
using ValorChronicle.Relics.Stats;

namespace ValorChronicle.Characters.Presentation
{
    public sealed class CharacterStatsLookupTextBuilder
    {
        private const string MareaId = "character_marea_bluefang";

        private readonly LocalizationService localization;

        public CharacterStatsLookupTextBuilder(
            LocalizationService localizationService)
        {
            localization = localizationService
                ?? throw new ArgumentNullException(
                    nameof(localizationService));
        }

        public string Build(
            CharacterUpgradePresentationModel model,
            CharacterDefinition definition)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            var text = new StringBuilder(768);
            CharacterStatValues levelStats =
                CharacterStatCalculator.Calculate(definition, model.Level);
            AppendHeading(text, "ui.character.stats_lookup.title");
            text.Append(model.DisplayName).AppendLine().AppendLine();

            AppendHeading(text, "ui.character.stats_lookup.basic");
            AppendFormat(text, "ui.character.stats_lookup.level", "value", model.Level);
            AppendFormat(text, "ui.character.stats_lookup.awakening", "value", model.Awakening);
            AppendFormat(text, "ui.character.stats_lookup.level_attack", "value", levelStats.Attack);
            AppendFormat(text, "ui.character.stats_lookup.level_max_hp", "value", levelStats.MaxHp);
            AppendFormat(text, "ui.character.stats_lookup.attack", "value", model.Attack);
            AppendFormat(text, "ui.character.stats_lookup.max_hp", "value", model.MaxHp);
            AppendFormat(text, "ui.character.stats_lookup.critical_chance", "value",
                Percent(DamageContextBuildRequest.DefaultBaseCriticalChance));
            AppendFormat(text, "ui.character.stats_lookup.critical_damage", "value",
                Percent(DamageContextBuildRequest.DefaultBaseCriticalDamageMultiplier));

            bool hasPermanent = HasIncrease(model.CharacterAttackMultiplier)
                || HasIncrease(model.CharacterMaxHpMultiplier)
                || HasPermanentRelicSource(
                    model.RelicStatBonuses.Sources);
            if (hasPermanent)
            {
                text.AppendLine();
                AppendHeading(text, "ui.character.stats_lookup.permanent");
                AppendCharacterIncrease(
                    text,
                    "ui.character.stats_lookup.awakening_source",
                    "ui.character.stats_lookup.attack_increase",
                    model.CharacterAttackMultiplier - 1d);
                AppendCharacterIncrease(
                    text,
                    "ui.character.stats_lookup.awakening_source",
                    "ui.character.stats_lookup.max_hp_increase",
                    model.CharacterMaxHpMultiplier - 1d);
                AppendRelicSources(text, model.RelicStatBonuses.Sources);
            }

            if (string.Equals(model.CharacterId, MareaId, StringComparison.Ordinal)
                && definition.CombatConfig is MareaBluefangCombatConfig config)
            {
                AppendMareaConditions(text, model.Awakening, config);
            }

            return text.ToString().TrimEnd();
        }

        private void AppendMareaConditions(
            StringBuilder text,
            int awakening,
            MareaBluefangCombatConfig config)
        {
            MareaBluefangResolvedCombatRules rules =
                MareaBluefangCombatRuleResolver.Resolve(config, awakening);
            text.AppendLine().AppendLine();
            AppendHeading(text, "ui.character.stats_lookup.conditional");
            AppendConditionalEffect(
                text,
                "marea.skill.passive.name",
                "marea.stats_lookup.passive.condition",
                null,
                "marea.stats_lookup.passive.effect",
                new Dictionary<string, object>
                {
                    ["damage_increase"] = PercentNumber(
                        rules.PassiveDealtDamageIncreaseRate)
                });

            if (rules.HasLingeringSurge)
            {
                var arguments = new Dictionary<string, object>
                {
                    ["required_water"] =
                        MareaBluefangCombatRuleResolver
                            .LingeringSurgeRequiredWaterAmount,
                    ["damage_increase"] = PercentNumber(
                        rules.LingeringSurgeDamageIncreaseRate)
                };
                AppendConditionalEffect(
                    text,
                    "marea.awakening.6.name",
                    "marea.stats_lookup.lingering_surge.condition",
                    arguments,
                    "marea.stats_lookup.lingering_surge.effect",
                    arguments);
            }
        }

        private void AppendConditionalEffect(
            StringBuilder text,
            string nameKey,
            string conditionKey,
            IReadOnlyDictionary<string, object> conditionArguments,
            string effectKey,
            IReadOnlyDictionary<string, object> effectArguments)
        {
            text.Append("• <b>").Append(localization.GetText(nameKey))
                .AppendLine("</b>");
            string condition = conditionArguments == null
                ? localization.GetText(conditionKey)
                : localization.Format(conditionKey, conditionArguments);
            text.Append("  ").Append(localization.Format(
                "ui.character.stats_lookup.condition",
                new Dictionary<string, object>
                {
                    ["condition"] = condition
                })).AppendLine();
            text.Append("  ").Append(localization.Format(
                "ui.character.stats_lookup.effect",
                new Dictionary<string, object>
                {
                    ["effect"] = localization.Format(
                        effectKey,
                        effectArguments)
                })).AppendLine();
            AppendSource(text, nameKey);
        }

        private void AppendRelicSources(
            StringBuilder text,
            IReadOnlyList<RelicStatBonusSource> sources)
        {
            for (int index = 0; index < sources.Count; index++)
            {
                RelicStatBonusSource source = sources[index];
                if (source == null || !IsPositive(source.Rate))
                {
                    continue;
                }

                string effectKey;
                switch (source.EffectType)
                {
                    case RelicEffectType.AttackIncrease:
                        effectKey = "ui.character.stats_lookup.attack_increase";
                        break;
                    case RelicEffectType.MaxHpIncrease:
                        effectKey = "ui.character.stats_lookup.max_hp_increase";
                        break;
                    default:
                        continue;
                }

                string name = string.IsNullOrEmpty(source.DisplayNameKey)
                    ? source.DefinitionId
                    : localization.GetText(source.DisplayNameKey);
                text.Append("• ").Append(localization.Format(
                    "ui.character.stats_lookup.relic_source",
                    new Dictionary<string, object>
                    {
                        ["name"] = name,
                        ["awakening"] = source.Awakening,
                        ["effect"] = localization.GetText(effectKey),
                        ["value"] = Percent(source.Rate)
                    })).AppendLine();
            }
        }

        private void AppendCharacterIncrease(
            StringBuilder text,
            string sourceKey,
            string effectKey,
            double rate)
        {
            if (!IsPositive(rate))
            {
                return;
            }

            text.Append("• ").Append(localization.Format(
                "ui.character.stats_lookup.permanent_source",
                new Dictionary<string, object>
                {
                    ["source"] = localization.GetText(sourceKey),
                    ["effect"] = localization.GetText(effectKey),
                    ["value"] = Percent(rate)
                })).AppendLine();
        }

        private void AppendSource(StringBuilder text, string sourceNameKey)
        {
            text.Append("  ").Append(localization.Format(
                "ui.character.stats_lookup.source",
                new Dictionary<string, object>
                {
                    ["source"] = localization.GetText(sourceNameKey)
                })).AppendLine();
        }

        private void AppendHeading(StringBuilder text, string key)
        {
            text.Append("<b>").Append(localization.GetText(key))
                .AppendLine("</b>");
        }

        private void AppendFormat(
            StringBuilder text,
            string key,
            string argumentName,
            object value)
        {
            AppendFormat(text, key, new Dictionary<string, object>
            {
                [argumentName] = value
            });
        }

        private void AppendFormat(
            StringBuilder text,
            string key,
            IReadOnlyDictionary<string, object> arguments)
        {
            text.Append(localization.Format(key, arguments)).AppendLine();
        }

        private static bool HasIncrease(double multiplier)
        {
            return IsPositive(multiplier - 1d);
        }

        private static bool HasPermanentRelicSource(
            IReadOnlyList<RelicStatBonusSource> sources)
        {
            for (int index = 0; index < sources.Count; index++)
            {
                RelicStatBonusSource source = sources[index];
                if (source != null
                    && IsPositive(source.Rate)
                    && (source.EffectType == RelicEffectType.AttackIncrease
                        || source.EffectType
                            == RelicEffectType.MaxHpIncrease))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPositive(double value)
        {
            return value > 0.0000001d;
        }

        private static string Percent(double rate)
        {
            return PercentNumber(rate) + "%";
        }

        private static string PercentNumber(double rate)
        {
            return (rate * 100d).ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
