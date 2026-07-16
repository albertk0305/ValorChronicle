using System;
using System.Collections.Generic;
using ValorChronicle.Core.IDs;
using ValorChronicle.Core.Logging;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Data.Validation
{
    public static class DataValidator
    {
        public static ValidationReport Validate(DefinitionDatabase database)
        {
            var report = new ValidationReport();

            if (database == null)
            {
                report.Add(
                    ValidationSeverity.Error,
                    "[DataValidator] DefinitionDatabase is missing.");
                return report;
            }

            var globalIds = new Dictionary<string, string>(StringComparer.Ordinal);
            var skillIds = CollectSkillIds(database.Skills);

            ValidateCharacters(database.Characters, skillIds, globalIds, report);
            ValidateBosses(database.Bosses, skillIds, globalIds, report);
            ValidateSkills(database.Skills, globalIds, report);
            ValidateRelics(database.Relics, globalIds, report);

            return report;
        }

        public static void LogReport(ValidationReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            for (int i = 0; i < report.Issues.Count; i++)
            {
                ValidationIssue issue = report.Issues[i];

                if (issue.Severity == ValidationSeverity.Warning)
                {
                    GameLogger.Warning(issue.Message, issue.Context);
                }
                else
                {
                    GameLogger.Error(issue.Message, issue.Context);
                }
            }
        }

        private static HashSet<string> CollectSkillIds(
            IReadOnlyList<SkillDefinition> skills)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < skills.Count; i++)
            {
                SkillDefinition skill = skills[i];

                if (skill != null && !string.IsNullOrEmpty(skill.Id))
                {
                    ids.Add(skill.Id);
                }
            }

            return ids;
        }

        private static void ValidateCharacters(
            IReadOnlyList<CharacterDefinition> characters,
            ISet<string> skillIds,
            IDictionary<string, string> globalIds,
            ValidationReport report)
        {
            var typeIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < characters.Count; i++)
            {
                CharacterDefinition definition = characters[i];

                if (definition == null)
                {
                    AddNullDefinition(report, nameof(CharacterDefinition), i);
                    continue;
                }

                ValidateCommon(definition, nameof(CharacterDefinition), typeIds, globalIds, report);
                WarnIfDisplayNameKeyMissing(definition.DisplayNameKey, definition, report);

                if (definition.Level1Hp <= 0)
                {
                    AddError(report, definition, "Lv.1 HP must be greater than zero");
                }

                if (definition.Level1Attack <= 0)
                {
                    AddError(report, definition, "Lv.1 ATK must be greater than zero");
                }

                if (definition.Level100Hp <= 0)
                {
                    AddError(report, definition, "Lv.100 HP must be greater than zero");
                }

                if (definition.Level100Attack <= 0)
                {
                    AddError(report, definition, "Lv.100 ATK must be greater than zero");
                }

                if (definition.Level100Hp < definition.Level1Hp)
                {
                    AddError(report, definition, "Lv.100 HP cannot be lower than Lv.1 HP");
                }

                if (definition.Level100Attack < definition.Level1Attack)
                {
                    AddError(report, definition, "Lv.100 ATK cannot be lower than Lv.1 ATK");
                }

                ValidateReferences(
                    definition.SkillIds,
                    skillIds,
                    "skill",
                    definition,
                    report);

                if (definition.SkillIds.Count == 0)
                {
                    AddWarning(report, definition, "Skill ID list is empty");
                }
            }
        }

        private static void ValidateBosses(
            IReadOnlyList<BossDefinition> bosses,
            ISet<string> skillIds,
            IDictionary<string, string> globalIds,
            ValidationReport report)
        {
            var typeIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < bosses.Count; i++)
            {
                BossDefinition definition = bosses[i];

                if (definition == null)
                {
                    AddNullDefinition(report, nameof(BossDefinition), i);
                    continue;
                }

                ValidateCommon(definition, nameof(BossDefinition), typeIds, globalIds, report);
                WarnIfDisplayNameKeyMissing(definition.DisplayNameKey, definition, report);

                if (definition.TurnLimit <= 0)
                {
                    AddError(report, definition, "Turn limit must be greater than zero");
                }

                ValidateReferences(
                    definition.ActionOrSkillIds,
                    skillIds,
                    "action or skill",
                    definition,
                    report);

                if (definition.ActionOrSkillIds.Count == 0)
                {
                    AddWarning(report, definition, "Action or skill ID list is empty");
                }
            }
        }

        private static void ValidateSkills(
            IReadOnlyList<SkillDefinition> skills,
            IDictionary<string, string> globalIds,
            ValidationReport report)
        {
            var typeIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < skills.Count; i++)
            {
                SkillDefinition definition = skills[i];

                if (definition == null)
                {
                    AddNullDefinition(report, nameof(SkillDefinition), i);
                    continue;
                }

                ValidateCommon(definition, nameof(SkillDefinition), typeIds, globalIds, report);
                WarnIfDisplayNameKeyMissing(definition.DisplayNameKey, definition, report);
            }
        }

        private static void ValidateRelics(
            IReadOnlyList<RelicDefinition> relics,
            IDictionary<string, string> globalIds,
            ValidationReport report)
        {
            var typeIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < relics.Count; i++)
            {
                RelicDefinition definition = relics[i];

                if (definition == null)
                {
                    AddNullDefinition(report, nameof(RelicDefinition), i);
                    continue;
                }

                ValidateCommon(definition, nameof(RelicDefinition), typeIds, globalIds, report);
                WarnIfDisplayNameKeyMissing(definition.DisplayNameKey, definition, report);
            }
        }

        private static void ValidateCommon(
            GameDefinition definition,
            string definitionTypeName,
            ISet<string> typeIds,
            IDictionary<string, string> globalIds,
            ValidationReport report)
        {
            string id = definition.Id;

            if (!ContentIdValidator.TryValidate(id, out string errorMessage))
            {
                string message = string.IsNullOrEmpty(id)
                    ? $"[DataValidator] Missing content ID: {definitionTypeName}"
                    : $"[DataValidator] Invalid content ID format: {id}. {errorMessage}";

                report.Add(ValidationSeverity.Error, message, id, definition);
            }

            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            if (!typeIds.Add(id))
            {
                report.Add(
                    ValidationSeverity.Error,
                    $"[DataValidator] Duplicate {definitionTypeName} ID: {id}",
                    id,
                    definition);
            }

            if (globalIds.TryGetValue(id, out string existingType))
            {
                report.Add(
                    ValidationSeverity.Error,
                    $"[DataValidator] Duplicate content ID: {id} " +
                    $"({existingType} and {definitionTypeName})",
                    id,
                    definition);
            }
            else
            {
                globalIds.Add(id, definitionTypeName);
            }
        }

        private static void ValidateReferences(
            IReadOnlyList<string> referenceIds,
            ISet<string> validSkillIds,
            string referenceType,
            GameDefinition owner,
            ValidationReport report)
        {
            for (int i = 0; i < referenceIds.Count; i++)
            {
                string referenceId = referenceIds[i];

                if (!string.IsNullOrEmpty(referenceId) && validSkillIds.Contains(referenceId))
                {
                    continue;
                }

                string displayedId = string.IsNullOrEmpty(referenceId) ? "<empty>" : referenceId;
                report.Add(
                    ValidationSeverity.Error,
                    $"[DataValidator] Missing {referenceType} reference: {displayedId}",
                    owner.Id,
                    owner);
            }
        }

        private static void WarnIfDisplayNameKeyMissing(
            string displayNameKey,
            GameDefinition definition,
            ValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(displayNameKey))
            {
                AddWarning(report, definition, "Display name localization key is empty");
            }
        }

        private static void AddNullDefinition(
            ValidationReport report,
            string definitionTypeName,
            int index)
        {
            report.Add(
                ValidationSeverity.Error,
                $"[DataValidator] Null {definitionTypeName} at index {index}.");
        }

        private static void AddError(
            ValidationReport report,
            GameDefinition definition,
            string message)
        {
            report.Add(
                ValidationSeverity.Error,
                $"[DataValidator] {message}: {definition.Id}",
                definition.Id,
                definition);
        }

        private static void AddWarning(
            ValidationReport report,
            GameDefinition definition,
            string message)
        {
            report.Add(
                ValidationSeverity.Warning,
                $"[DataValidator] {message}: {definition.Id}",
                definition.Id,
                definition);
        }
    }
}
