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
            Dictionary<string, SkillDefinition> skillsById =
                CollectSkillsById(database.Skills);

            ValidateCharacters(
                database.Characters,
                skillsById,
                globalIds,
                report);
            ValidateBosses(database.Bosses, skillsById, globalIds, report);
            ValidateSkills(database.Skills, globalIds, report);
            ValidateEffects(database.Effects, globalIds, report);
            ValidateResources(database.Resources, globalIds, report);
            ValidateRelics(database.Relics, globalIds, report);
            ValidateBossPresentations(
                database.BossPresentations,
                database.Bosses,
                report);

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

        private static Dictionary<string, SkillDefinition> CollectSkillsById(
            IReadOnlyList<SkillDefinition> skills)
        {
            var definitions = new Dictionary<string, SkillDefinition>(
                StringComparer.Ordinal);

            for (int i = 0; i < skills.Count; i++)
            {
                SkillDefinition skill = skills[i];

                if (skill != null && !string.IsNullOrEmpty(skill.Id))
                {
                    if (!definitions.ContainsKey(skill.Id))
                    {
                        definitions.Add(skill.Id, skill);
                    }
                }
            }

            return definitions;
        }

        private static void ValidateCharacters(
            IReadOnlyList<CharacterDefinition> characters,
            IReadOnlyDictionary<string, SkillDefinition> skillsById,
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

                if (definition.CombatConfig == null)
                {
                    AddWarning(
                        report,
                        definition,
                        "Combat config is missing");
                }
                else if (!definition.CombatConfig.TryValidate(
                    out string configError))
                {
                    AddError(
                        report,
                        definition,
                        $"Combat config is invalid. {configError}");
                }

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

                ValidateSkillReferences(
                    definition.SkillIds,
                    skillsById,
                    "skill",
                    SkillReferenceOwner.Character,
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
            IReadOnlyDictionary<string, SkillDefinition> skillsById,
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

                if (definition.CombatConfig == null)
                {
                    AddWarning(
                        report,
                        definition,
                        "Combat config is missing");
                }
                else if (!definition.CombatConfig.TryValidate(
                    out string configError))
                {
                    AddError(
                        report,
                        definition,
                        $"Combat config is invalid. {configError}");
                }

                if (definition.TurnLimit <= 0)
                {
                    AddError(report, definition, "Turn limit must be greater than zero");
                }

                ValidateBossDifficulties(definition, report);

                ValidateSkillReferences(
                    definition.ActionOrSkillIds,
                    skillsById,
                    "action or skill",
                    SkillReferenceOwner.Boss,
                    definition,
                    report);

                if (definition.ActionOrSkillIds.Count == 0)
                {
                    AddWarning(report, definition, "Action or skill ID list is empty");
                }
            }
        }

        private static void ValidateBossDifficulties(
            BossDefinition definition,
            ValidationReport report)
        {
            var difficultyIds = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<BossDifficultyStats> stats =
                definition.DifficultyStats;

            for (int index = 0; index < stats.Count; index++)
            {
                BossDifficultyStats entry = stats[index];
                if (entry == null)
                {
                    AddError(
                        report,
                        definition,
                        $"Difficulty stat at index {index} is null");
                    continue;
                }

                if (!ContentIdValidator.TryValidate(
                    entry.DifficultyId,
                    out string idError))
                {
                    string displayedId = string.IsNullOrEmpty(
                        entry.DifficultyId)
                        ? "<empty>"
                        : entry.DifficultyId;
                    AddError(
                        report,
                        definition,
                        $"Invalid difficulty ID {displayedId}. {idError}");
                }

                if (!string.IsNullOrEmpty(entry.DifficultyId)
                    && !difficultyIds.Add(entry.DifficultyId))
                {
                    AddError(
                        report,
                        definition,
                        $"Duplicate difficulty ID {entry.DifficultyId}");
                }

                if (entry.MaxHp <= 0)
                {
                    AddError(
                        report,
                        definition,
                        $"Difficulty Max HP must be greater than zero "
                            + $"({entry.DifficultyId})");
                }

                if (double.IsNaN(entry.Attack)
                    || double.IsInfinity(entry.Attack)
                    || entry.Attack < 0d)
                {
                    AddError(
                        report,
                        definition,
                        $"Difficulty ATK must be finite and non-negative "
                            + $"({entry.DifficultyId})");
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
                WarnIfDescriptionKeyMissing(
                    definition.DescriptionKey,
                    definition,
                    report);
                if (definition.SkillKind == SkillKind.BossAction)
                {
                    WarnIfIconMissing(definition.Icon, definition, report);
                }

                if (!Enum.IsDefined(
                        typeof(SkillKind),
                        definition.SkillKind))
                {
                    AddError(report, definition, "Skill kind is invalid");
                }
            }
        }

        private static void ValidateEffects(
            IReadOnlyList<EffectDefinition> effects,
            IDictionary<string, string> globalIds,
            ValidationReport report)
        {
            var typeIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < effects.Count; index++)
            {
                EffectDefinition definition = effects[index];
                if (definition == null)
                {
                    AddNullDefinition(
                        report,
                        nameof(EffectDefinition),
                        index);
                    continue;
                }

                ValidateCommon(
                    definition,
                    nameof(EffectDefinition),
                    typeIds,
                    globalIds,
                    report);
                WarnIfDisplayNameKeyMissing(
                    definition.DisplayNameKey,
                    definition,
                    report);
                WarnIfDescriptionKeyMissing(
                    definition.DescriptionKey,
                    definition,
                    report);
                WarnIfIconMissing(definition.Icon, definition, report);
            }
        }

        private static void ValidateResources(
            IReadOnlyList<ResourceDefinition> resources,
            IDictionary<string, string> globalIds,
            ValidationReport report)
        {
            var typeIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < resources.Count; index++)
            {
                ResourceDefinition definition = resources[index];
                if (definition == null)
                {
                    AddNullDefinition(
                        report,
                        nameof(ResourceDefinition),
                        index);
                    continue;
                }

                ValidateCommon(
                    definition,
                    nameof(ResourceDefinition),
                    typeIds,
                    globalIds,
                    report);
                WarnIfDisplayNameKeyMissing(
                    definition.DisplayNameKey,
                    definition,
                    report);
                WarnIfDescriptionKeyMissing(
                    definition.DescriptionKey,
                    definition,
                    report);
                WarnIfIconMissing(definition.Icon, definition, report);
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

        private static void ValidateSkillReferences(
            IReadOnlyList<string> referenceIds,
            IReadOnlyDictionary<string, SkillDefinition> skillsById,
            string referenceType,
            SkillReferenceOwner ownerType,
            GameDefinition owner,
            ValidationReport report)
        {
            var referencedIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < referenceIds.Count; i++)
            {
                string referenceId = referenceIds[i];

                if (!string.IsNullOrEmpty(referenceId)
                    && !referencedIds.Add(referenceId))
                {
                    AddError(
                        report,
                        owner,
                        $"Duplicate {referenceType} reference: "
                            + referenceId);
                    continue;
                }

                if (string.IsNullOrEmpty(referenceId)
                    || !skillsById.TryGetValue(
                        referenceId,
                        out SkillDefinition skill))
                {
                    string displayedId = string.IsNullOrEmpty(referenceId)
                        ? "<empty>"
                        : referenceId;
                    report.Add(
                        ValidationSeverity.Error,
                        $"[DataValidator] Missing {referenceType} "
                            + $"reference: {displayedId}",
                        owner.Id,
                        owner);
                    continue;
                }

                if (ownerType == SkillReferenceOwner.Character
                    && skill.SkillKind == SkillKind.BossAction)
                {
                    AddError(
                        report,
                        owner,
                        $"Character cannot reference BossAction skill: "
                            + referenceId);
                }
                else if (ownerType == SkillReferenceOwner.Boss
                    && skill.SkillKind != SkillKind.BossAction)
                {
                    AddError(
                        report,
                        owner,
                        $"Boss action list must reference BossAction skill: "
                            + referenceId);
                }
            }
        }

        private static void ValidateBossPresentations(
            IReadOnlyList<BossPresentationDefinition> presentations,
            IReadOnlyList<BossDefinition> bosses,
            ValidationReport report)
        {
            var bossIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < bosses.Count; index++)
            {
                BossDefinition boss = bosses[index];
                if (boss != null && !string.IsNullOrEmpty(boss.Id))
                {
                    bossIds.Add(boss.Id);
                }
            }

            var presentedBossIds = new HashSet<string>(
                StringComparer.Ordinal);
            for (int index = 0; index < presentations.Count; index++)
            {
                BossPresentationDefinition presentation =
                    presentations[index];
                if (presentation == null)
                {
                    report.Add(
                        ValidationSeverity.Error,
                        $"[DataValidator] Null "
                            + $"{nameof(BossPresentationDefinition)} at "
                            + $"index {index}.");
                    continue;
                }

                string bossId = presentation.BossId;
                if (!ContentIdValidator.TryValidate(
                        bossId,
                        out string bossIdError))
                {
                    string displayedId = string.IsNullOrEmpty(bossId)
                        ? "<empty>"
                        : bossId;
                    report.Add(
                        ValidationSeverity.Error,
                        $"[DataValidator] Invalid BossPresentation Boss ID "
                            + $"{displayedId}. {bossIdError}",
                        bossId,
                        presentation);
                }
                else
                {
                    if (!bossIds.Contains(bossId))
                    {
                        report.Add(
                            ValidationSeverity.Error,
                            $"[DataValidator] Missing boss reference: "
                                + bossId,
                            bossId,
                            presentation);
                    }

                    if (!presentedBossIds.Add(bossId))
                    {
                        report.Add(
                            ValidationSeverity.Error,
                            $"[DataValidator] Duplicate boss presentation "
                                + $"for Boss ID: {bossId}",
                            bossId,
                            presentation);
                    }
                }

                if (presentation.DefaultSprite == null)
                {
                    report.Add(
                        ValidationSeverity.Error,
                        $"[DataValidator] Boss presentation default sprite "
                            + $"is missing: {bossId}",
                        bossId,
                        presentation);
                }

                ValidateBossVisualStates(presentation, report);
            }
        }

        private static void ValidateBossVisualStates(
            BossPresentationDefinition presentation,
            ValidationReport report)
        {
            var visualStateIds = new HashSet<string>(
                StringComparer.Ordinal);
            IReadOnlyList<BossVisualStateEntry> visuals =
                presentation.StateVisuals;
            for (int index = 0; index < visuals.Count; index++)
            {
                BossVisualStateEntry entry = visuals[index];
                if (entry == null)
                {
                    report.Add(
                        ValidationSeverity.Error,
                        $"[DataValidator] Null boss visual state at index "
                            + $"{index}: {presentation.BossId}",
                        presentation.BossId,
                        presentation);
                    continue;
                }

                if (!ContentIdValidator.TryValidate(
                        entry.VisualStateId,
                        out string stateIdError))
                {
                    string displayedId = string.IsNullOrEmpty(
                        entry.VisualStateId)
                        ? "<empty>"
                        : entry.VisualStateId;
                    report.Add(
                        ValidationSeverity.Error,
                        $"[DataValidator] Invalid boss visual state ID "
                            + $"{displayedId}. {stateIdError}",
                        presentation.BossId,
                        presentation);
                }
                else if (!visualStateIds.Add(entry.VisualStateId))
                {
                    report.Add(
                        ValidationSeverity.Error,
                        $"[DataValidator] Duplicate boss visual state ID: "
                            + entry.VisualStateId,
                        presentation.BossId,
                        presentation);
                }

                if (entry.Sprite == null)
                {
                    report.Add(
                        ValidationSeverity.Error,
                        $"[DataValidator] Boss visual state sprite is "
                            + $"missing: {entry.VisualStateId}",
                        presentation.BossId,
                        presentation);
                }
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

        private static void WarnIfDescriptionKeyMissing(
            string descriptionKey,
            GameDefinition definition,
            ValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(descriptionKey))
            {
                AddWarning(
                    report,
                    definition,
                    "Description localization key is empty");
            }
        }

        private static void WarnIfIconMissing(
            UnityEngine.Sprite icon,
            GameDefinition definition,
            ValidationReport report)
        {
            if (icon == null)
            {
                AddWarning(report, definition, "Icon is missing");
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

        private enum SkillReferenceOwner
        {
            Character,
            Boss
        }
    }
}
