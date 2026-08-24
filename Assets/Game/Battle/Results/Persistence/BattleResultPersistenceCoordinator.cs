using System;
using ValorChronicle.Core.Logging;

namespace ValorChronicle.Battle.Results.Persistence
{
    public sealed class BattleResultPersistenceCoordinator : IDisposable
    {
        private readonly IBattleFinalResultSource resultSource;
        private readonly BattleResultPersistenceService persistenceService;
        private BattleFinalResult acceptedResult;
        private BattleFinalResult pendingResult;
        private bool resultPublished;
        private bool isDisposed;

        public BattleResultPersistenceCoordinator(
            IBattleFinalResultSource resultSource,
            BattleResultPersistenceService persistenceService)
        {
            this.resultSource = resultSource
                ?? throw new ArgumentNullException(nameof(resultSource));
            this.persistenceService = persistenceService
                ?? throw new ArgumentNullException(nameof(persistenceService));
            resultSource.ResultFinalized += HandleResultFinalized;
        }

        public event Action<BattlePersistedResult> ResultPersisted;

        public BattlePersistedResult LastPersistedResult { get; private set; }
        public BattleResultPersistenceOperationResult LastPersistenceOperation
        {
            get;
            private set;
        }
        public bool IsPersistenceCompleted { get; private set; }
        public bool HasPendingPersistence => pendingResult != null;

        public BattleResultPersistenceOperationResult
            RetryPendingPersistence()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (pendingResult == null)
            {
                return null;
            }

            return PersistPendingResult();
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            resultSource.ResultFinalized -= HandleResultFinalized;
            isDisposed = true;
        }

        private void HandleResultFinalized(BattleFinalResult finalResult)
        {
            if (finalResult == null)
            {
                GameLogger.Error(
                    "[BattleResultPersistence] ResultFinalized supplied no result.");
                return;
            }

            if (acceptedResult != null)
            {
                if (!ReferenceEquals(acceptedResult, finalResult))
                {
                    GameLogger.Error(
                        "[BattleResultPersistence] A battle runtime produced "
                            + "more than one final result.");
                }

                return;
            }

            acceptedResult = finalResult;
            pendingResult = finalResult;
            PersistPendingResult();
        }

        private BattleResultPersistenceOperationResult PersistPendingResult()
        {
            BattleFinalResult result = pendingResult;
            BattleResultPersistenceOperationResult operation =
                persistenceService.Persist(result);
            LastPersistenceOperation = operation;

            if (!operation.IsSuccess || operation.PersistedResult == null)
            {
                LogPersistenceFailure(operation);
                return operation;
            }

            pendingResult = null;
            LastPersistedResult = operation.PersistedResult;
            IsPersistenceCompleted = true;
            if (!resultPublished)
            {
                resultPublished = true;
                ResultPersisted?.Invoke(LastPersistedResult);
            }

            return operation;
        }

        private static void LogPersistenceFailure(
            BattleResultPersistenceOperationResult operation)
        {
            string transactionStatus = operation.SaveTransactionResult == null
                ? "Unavailable"
                : operation.SaveTransactionResult.Status.ToString();
            string message = operation.SaveTransactionResult?.Message;
            GameLogger.Error(
                $"[BattleResultPersistence] Save failed. "
                    + $"TransactionStatus={transactionStatus}. {message}");
        }
    }
}
