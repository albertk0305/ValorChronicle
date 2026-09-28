using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Party.Persistence
{
    public enum PartyPersistenceStatus
    {
        ChangedAndSaved = 0,
        NoChange = 1,
        Failed = 2
    }

    public sealed class PartyPersistenceOperationResult
    {
        internal PartyPersistenceOperationResult(
            PartyPersistenceStatus status,
            ProfileSaveData profileSnapshot,
            PartyPresetEditResult editResult,
            SaveTransactionResult saveTransactionResult)
        {
            Status = status;
            ProfileSnapshot = profileSnapshot;
            EditResult = editResult;
            SaveTransactionResult = saveTransactionResult;
        }

        public PartyPersistenceStatus Status { get; }
        public bool IsSuccess => Status != PartyPersistenceStatus.Failed;
        public bool WasChanged =>
            Status == PartyPersistenceStatus.ChangedAndSaved;
        public ProfileSaveData ProfileSnapshot { get; }
        public PartyPresetEditResult EditResult { get; }
        public SaveTransactionResult SaveTransactionResult { get; }
    }
}
