using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using ValorChronicle.Battle.Entry;
using ValorChronicle.Battle.Results;
using ValorChronicle.Core.Logging;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Localization;
using ValorChronicle.Party.Presentation;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Main.Presentation
{
    public static class BossSelectionListBuilder
    {
        public const string MasterChallengePromptKey =
            "main.boss_selection.master_challenge_prompt";
        public const string MasterChallengePrompt =
            "Try Master Mode";
        public const string MissingBossName = "Unknown Boss";

        public static IReadOnlyList<BossSelectionItem> Build(
            DefinitionDatabase database,
            LocalizationService localization,
            ElementIconSet elementIcons,
            ProfileSaveData profile,
            Func<string, Sprite> gradeSpriteResolver,
            UnityEngine.Object logContext = null)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            if (localization == null)
            {
                throw new ArgumentNullException(nameof(localization));
            }

            var supportedBosses = new List<BossDefinition>();
            IReadOnlyList<BossDefinition> bosses = database.Bosses;
            for (int index = 0; index < bosses.Count; index++)
            {
                BossDefinition boss = bosses[index];
                if (BattleEncounterFactory.IsSupportedBoss(boss, out _))
                {
                    supportedBosses.Add(boss);
                }
            }

            supportedBosses.Sort((left, right) => string.Compare(
                left.Id,
                right.Id,
                StringComparison.Ordinal));

            var items = new List<BossSelectionItem>(supportedBosses.Count);
            for (int index = 0; index < supportedBosses.Count; index++)
            {
                BossDefinition boss = supportedBosses[index];
                string displayName = ResolveDisplayName(
                    boss,
                    localization,
                    logContext);
                Sprite face = ResolveFace(database, boss.Id);
                Sprite element = ResolveElementIcon(
                    boss,
                    elementIcons,
                    logContext);
                ResolveMasterRecord(
                    localization,
                    profile,
                    boss.Id,
                    gradeSpriteResolver,
                    logContext,
                    out string scoreText,
                    out Sprite gradeSprite);

                items.Add(new BossSelectionItem(
                    boss.Id,
                    displayName,
                    face,
                    element,
                    scoreText,
                    gradeSprite));
            }

            return items;
        }

        private static string ResolveDisplayName(
            BossDefinition boss,
            LocalizationService localization,
            UnityEngine.Object logContext)
        {
            string localized = localization.GetText(boss.DisplayNameKey);
            if (!string.Equals(
                    localized,
                    LocalizationService.MissingKey(boss.DisplayNameKey),
                    StringComparison.Ordinal))
            {
                return localized;
            }

            GameLogger.Error(
                $"[BossSelection] Localization is missing for key "
                    + $"'{boss.DisplayNameKey}'.",
                logContext);
            return MissingBossName;
        }

        private static Sprite ResolveFace(
            DefinitionDatabase database,
            string bossId)
        {
            return database.TryGetBossPresentation(
                    bossId,
                    out BossPresentationDefinition presentation)
                ? presentation.FaceSprite
                : null;
        }

        private static Sprite ResolveElementIcon(
            BossDefinition boss,
            ElementIconSet elementIcons,
            UnityEngine.Object logContext)
        {
            if (elementIcons == null)
            {
                GameLogger.Error(
                    "[BossSelection] ElementIconSet is missing.",
                    logContext);
                return null;
            }

            try
            {
                return elementIcons.GetIcon(boss.Element);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                GameLogger.Exception(exception, logContext);
                return null;
            }
        }

        private static void ResolveMasterRecord(
            LocalizationService localization,
            ProfileSaveData profile,
            string bossId,
            Func<string, Sprite> gradeSpriteResolver,
            UnityEngine.Object logContext,
            out string scoreText,
            out Sprite gradeSprite)
        {
            BossRecordSaveData record = FindRecord(
                profile,
                bossId,
                BattleDifficultyIds.Challenge);
            if (record == null || !record.HasAttempted)
            {
                scoreText = ResolveLocalizedText(
                    localization,
                    MasterChallengePromptKey,
                    MasterChallengePrompt,
                    logContext);
                gradeSprite = null;
                return;
            }

            scoreText = record.HighScore.ToString(
                "N0",
                CultureInfo.InvariantCulture);
            gradeSprite = ResolveBestGradeSprite(
                record,
                gradeSpriteResolver,
                logContext);
        }

        private static string ResolveLocalizedText(
            LocalizationService localization,
            string key,
            string fallback,
            UnityEngine.Object logContext)
        {
            string localized = localization.GetText(key);
            if (!string.Equals(
                    localized,
                    LocalizationService.MissingKey(key),
                    StringComparison.Ordinal))
            {
                return localized;
            }

            GameLogger.Error(
                $"[BossSelection] Localization is missing for key "
                    + $"'{key}'. Using the English fallback.",
                logContext);
            return fallback;
        }

        public static BossRecordSaveData FindRecord(
            ProfileSaveData profile,
            string bossId,
            string difficultyId)
        {
            if (profile?.BossRecords == null)
            {
                return null;
            }

            for (int index = 0; index < profile.BossRecords.Count; index++)
            {
                BossRecordSaveData record = profile.BossRecords[index];
                if (record != null
                    && string.Equals(
                        record.BossId,
                        bossId,
                        StringComparison.Ordinal)
                    && string.Equals(
                        record.DifficultyId,
                        difficultyId,
                        StringComparison.Ordinal))
                {
                    return record;
                }
            }

            return null;
        }

        public static Sprite ResolveBestGradeSprite(
            BossRecordSaveData record,
            Func<string, Sprite> gradeSpriteResolver,
            UnityEngine.Object logContext = null)
        {
            if (record == null
                || !record.HasAttempted
                || string.IsNullOrEmpty(record.HighestGradeId))
            {
                return null;
            }

            if (!BattleGradeIds.TryGetRewardGrade(
                    record.HighestGradeId,
                    out _))
            {
                GameLogger.Error(
                    $"[BossSelection] Unknown grade ID "
                        + $"'{record.HighestGradeId}' for boss "
                        + $"'{record.BossId}'.",
                    logContext);
                return null;
            }

            return gradeSpriteResolver?.Invoke(record.HighestGradeId);
        }
    }
}
