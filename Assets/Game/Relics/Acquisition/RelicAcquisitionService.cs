using System;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;
using ValorChronicle.Save.Validation;

namespace ValorChronicle.Relics.Acquisition
{
    /// <summary>Grants relics through the authoritative save transaction.</summary>
    public sealed class RelicAcquisitionService
    {
        private readonly object syncRoot = new object();
        private readonly SaveService saveService;
        private readonly ISaveContentCatalog contentCatalog;
        private readonly RelicAcquisitionProfileUpdater profileUpdater;

        public RelicAcquisitionService(
            SaveService saveService,
            ISaveContentCatalog contentCatalog,
            IRelicInstanceIdProvider instanceIdProvider)
        {
            this.saveService = saveService
                ?? throw new ArgumentNullException(nameof(saveService));
            this.contentCatalog = contentCatalog
                ?? throw new ArgumentNullException(nameof(contentCatalog));
            profileUpdater = new RelicAcquisitionProfileUpdater(
                instanceIdProvider);
        }

        public RelicAcquisitionPersistenceOperationResult GrantRelic(
            string relicDefinitionId)
        {
            if (string.IsNullOrWhiteSpace(relicDefinitionId))
            {
                return CreateResult(
                    RelicAcquisitionPersistenceStatus.InvalidDefinitionId);
            }

            SaveContentLookupResult lookup =
                contentCatalog.LookupRelic(relicDefinitionId);
            if (lookup == SaveContentLookupResult.Missing)
            {
                return CreateResult(
                    RelicAcquisitionPersistenceStatus.DefinitionNotFound);
            }

            if (lookup != SaveContentLookupResult.Exists)
            {
                return CreateResult(
                    RelicAcquisitionPersistenceStatus.PersistenceFailed);
            }

            lock (syncRoot)
            {
                RelicAcquisitionResult acquisitionResult = null;
                RelicAcquisitionPersistenceStatus rejectedStatus =
                    RelicAcquisitionPersistenceStatus.PersistenceFailed;
                bool wasRejected = false;
                SaveTransactionResult transaction =
                    saveService.ExecuteTransaction(profile =>
                    {
                        if (profileUpdater.TryApply(
                                profile,
                                relicDefinitionId,
                                out acquisitionResult,
                                out rejectedStatus))
                        {
                            return;
                        }

                        wasRejected = true;
                        throw new AcquisitionRejectedException(
                            rejectedStatus);
                    });

                if (transaction.IsSuccess
                    && acquisitionResult != null)
                {
                    return new RelicAcquisitionPersistenceOperationResult(
                        RelicAcquisitionPersistenceStatus.Success,
                        saveService.GetCurrentProfileSnapshot(),
                        acquisitionResult,
                        transaction);
                }

                RelicAcquisitionPersistenceStatus status;
                if (wasRejected)
                {
                    status = rejectedStatus;
                }
                else if (transaction.Status
                    == SaveTransactionStatus.SaveFailed)
                {
                    status = RelicAcquisitionPersistenceStatus.SaveFailed;
                }
                else
                {
                    status = RelicAcquisitionPersistenceStatus
                        .PersistenceFailed;
                }

                return new RelicAcquisitionPersistenceOperationResult(
                    status,
                    profileSnapshot: null,
                    acquisitionResult: null,
                    transaction);
            }
        }

        private static RelicAcquisitionPersistenceOperationResult
            CreateResult(RelicAcquisitionPersistenceStatus status)
        {
            return new RelicAcquisitionPersistenceOperationResult(
                status,
                profileSnapshot: null,
                acquisitionResult: null,
                saveTransactionResult: null);
        }

        private sealed class AcquisitionRejectedException
            : InvalidOperationException
        {
            public AcquisitionRejectedException(
                RelicAcquisitionPersistenceStatus status)
                : base($"Relic acquisition was rejected: {status}.")
            {
            }
        }
    }
}
