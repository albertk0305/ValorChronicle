using System;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;
using ValorChronicle.Save.Validation;

namespace ValorChronicle.Tests.EditMode
{
    internal static class SaveValidationTestSupport
    {
        public static ProfileSaveData CreateValidProfile() => new NewProfileFactory().Create("profile_test", 10);
    }

    internal sealed class FakeSaveContentCatalog : ISaveContentCatalog
    {
        public SaveContentLookupResult CharacterResult { get; set; } = SaveContentLookupResult.Exists;
        public SaveContentLookupResult RelicResult { get; set; } = SaveContentLookupResult.Exists;
        public SaveContentLookupResult BossResult { get; set; } = SaveContentLookupResult.Exists;
        public SaveContentLookupResult DifficultyResult { get; set; } = SaveContentLookupResult.Exists;
        public SaveContentLookupResult GachaResult { get; set; } = SaveContentLookupResult.Exists;
        public SaveContentLookupResult RewardGradeResult { get; set; } = SaveContentLookupResult.Exists;
        public Func<string, SaveContentLookupResult> CharacterLookup { get; set; }

        public SaveContentLookupResult LookupCharacter(string characterId) => CharacterLookup?.Invoke(characterId) ?? CharacterResult;
        public SaveContentLookupResult LookupRelic(string relicDefinitionId) => RelicResult;
        public SaveContentLookupResult LookupBoss(string bossId) => BossResult;
        public SaveContentLookupResult LookupBossDifficulty(string bossId, string difficultyId) => DifficultyResult;
        public SaveContentLookupResult LookupGacha(string gachaId) => GachaResult;
        public SaveContentLookupResult LookupRewardGrade(string bossId, string difficultyId, string gradeId) => RewardGradeResult;
    }
}
