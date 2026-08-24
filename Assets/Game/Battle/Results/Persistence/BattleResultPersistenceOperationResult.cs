using ValorChronicle.Save.Services;

namespace ValorChronicle.Battle.Results.Persistence
{
    public enum BattleResultPersistenceStatus
    {
        Success = 0,
        AlreadyPersisted = 1,
        SaveFailed = 2
    }

    public sealed class BattleResultPersistenceOperationResult
    {
        internal BattleResultPersistenceOperationResult(
            BattleResultPersistenceStatus status,
            BattlePersistedResult persistedResult,
            SaveTransactionResult saveTransactionResult)
        {
            Status = status;
            PersistedResult = persistedResult;
            SaveTransactionResult = saveTransactionResult;
        }

        public BattleResultPersistenceStatus Status { get; }
        public bool IsSuccess =>
            Status == BattleResultPersistenceStatus.Success
            || Status == BattleResultPersistenceStatus.AlreadyPersisted;
        public bool WasAlreadyPersisted =>
            Status == BattleResultPersistenceStatus.AlreadyPersisted;
        public BattlePersistedResult PersistedResult { get; }
        public SaveTransactionResult SaveTransactionResult { get; }
    }
}
