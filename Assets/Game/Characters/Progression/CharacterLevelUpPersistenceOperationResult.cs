using ValorChronicle.Save.Services;

namespace ValorChronicle.Characters.Progression
{
    public enum CharacterLevelUpPersistenceStatus
    {
        Success = 0,
        InvalidCharacterId = 1,
        InvalidProfile = 2,
        CharacterNotOwned = 3,
        InvalidTargetLevel = 4,
        MaximumLevelReached = 5,
        TargetLevelNotHigher = 6,
        InsufficientBattleRecords = 7,
        PersistenceFailed = 8
    }

    public sealed class CharacterLevelUpPersistenceOperationResult
    {
        internal CharacterLevelUpPersistenceOperationResult(
            CharacterLevelUpPersistenceStatus status,
            CharacterLevelUpResult levelUpResult,
            SaveTransactionResult saveTransactionResult)
        {
            Status = status;
            LevelUpResult = levelUpResult;
            SaveTransactionResult = saveTransactionResult;
        }

        public CharacterLevelUpPersistenceStatus Status { get; }
        public bool IsSuccess =>
            Status == CharacterLevelUpPersistenceStatus.Success;
        public CharacterLevelUpResult LevelUpResult { get; }
        public SaveTransactionResult SaveTransactionResult { get; }
    }
}
