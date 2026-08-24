using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Battle.Results.Persistence
{
    public sealed class BattleResultPersistenceService
    {
        private readonly object syncRoot = new object();
        private readonly SaveService saveService;
        private readonly Dictionary<BattleFinalResult, BattlePersistedResult>
            persistedResults =
                new Dictionary<BattleFinalResult, BattlePersistedResult>(
                    BattleFinalResultReferenceComparer.Instance);

        public BattleResultPersistenceService(SaveService saveService)
        {
            this.saveService = saveService
                ?? throw new ArgumentNullException(nameof(saveService));
        }

        public BattleResultPersistenceOperationResult Persist(
            BattleFinalResult battleResult)
        {
            if (battleResult == null)
            {
                throw new ArgumentNullException(nameof(battleResult));
            }

            lock (syncRoot)
            {
                if (persistedResults.TryGetValue(
                    battleResult,
                    out BattlePersistedResult existing))
                {
                    return new BattleResultPersistenceOperationResult(
                        BattleResultPersistenceStatus.AlreadyPersisted,
                        existing,
                        saveTransactionResult: null);
                }

                BattleProfileUpdateResult updateResult = null;
                SaveTransactionResult transaction =
                    saveService.ExecuteTransaction(profile =>
                    {
                        updateResult = BattleResultProfileUpdater.Apply(
                            profile,
                            battleResult);
                    });
                if (!transaction.IsSuccess || updateResult == null)
                {
                    return new BattleResultPersistenceOperationResult(
                        BattleResultPersistenceStatus.SaveFailed,
                        persistedResult: null,
                        saveTransactionResult: transaction);
                }

                var persisted = new BattlePersistedResult(
                    battleResult,
                    updateResult);
                persistedResults.Add(battleResult, persisted);
                return new BattleResultPersistenceOperationResult(
                    BattleResultPersistenceStatus.Success,
                    persisted,
                    transaction);
            }
        }

        private sealed class BattleFinalResultReferenceComparer
            : IEqualityComparer<BattleFinalResult>
        {
            public static readonly BattleFinalResultReferenceComparer
                Instance = new BattleFinalResultReferenceComparer();

            public bool Equals(BattleFinalResult x, BattleFinalResult y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(BattleFinalResult value)
            {
                return RuntimeHelpers.GetHashCode(value);
            }
        }
    }
}
