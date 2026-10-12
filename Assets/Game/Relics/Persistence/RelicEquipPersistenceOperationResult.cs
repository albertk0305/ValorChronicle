using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Relics.Persistence
{
    public enum RelicEquipPersistenceStatus
    {
        ChangedAndSaved = 0,
        NoChange = 1,
        NoRelicSelected = 2,
        StaleSession = 3,
        Rejected = 4,
        SaveFailed = 5,
        PersistenceFailed = 6
    }

    /// <summary>Describes one persisted relic equip command.</summary>
    public sealed class RelicEquipPersistenceOperationResult
    {
        internal RelicEquipPersistenceOperationResult(
            RelicEquipPersistenceStatus status,
            ProfileSaveData profileSnapshot,
            RelicEquipEditResult editResult,
            SaveTransactionResult saveTransactionResult)
        {
            Status = status;
            ProfileSnapshot = profileSnapshot;
            EditResult = editResult;
            SaveTransactionResult = saveTransactionResult;
        }

        public RelicEquipPersistenceStatus Status { get; }
        public bool IsSuccess =>
            Status == RelicEquipPersistenceStatus.ChangedAndSaved
            || Status == RelicEquipPersistenceStatus.NoChange;
        public bool WasChanged =>
            Status == RelicEquipPersistenceStatus.ChangedAndSaved;
        public ProfileSaveData ProfileSnapshot { get; }
        public RelicEquipEditResult EditResult { get; }
        public RelicEquipAvailability Availability =>
            EditResult?.Availability;
        public SaveTransactionResult SaveTransactionResult { get; }
    }
}
