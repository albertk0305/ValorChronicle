using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Relics.Acquisition
{
    public enum RelicAcquisitionPersistenceStatus
    {
        Success = 0,
        InvalidDefinitionId = 1,
        DefinitionNotFound = 2,
        InvalidProfile = 3,
        InstanceIdGenerationFailed = 4,
        SaveFailed = 5,
        PersistenceFailed = 6
    }

    /// <summary>Describes one persistent relic grant operation.</summary>
    public sealed class RelicAcquisitionPersistenceOperationResult
    {
        internal RelicAcquisitionPersistenceOperationResult(
            RelicAcquisitionPersistenceStatus status,
            ProfileSaveData profileSnapshot,
            RelicAcquisitionResult acquisitionResult,
            SaveTransactionResult saveTransactionResult)
        {
            Status = status;
            ProfileSnapshot = profileSnapshot;
            AcquisitionResult = acquisitionResult;
            SaveTransactionResult = saveTransactionResult;
        }

        public RelicAcquisitionPersistenceStatus Status { get; }
        public bool IsSuccess =>
            Status == RelicAcquisitionPersistenceStatus.Success;
        public ProfileSaveData ProfileSnapshot { get; }
        public RelicAcquisitionResult AcquisitionResult { get; }
        public SaveTransactionResult SaveTransactionResult { get; }
    }
}
