using System;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Characters.Progression
{
    public sealed class CharacterLevelUpPersistenceService
    {
        private readonly object syncRoot = new object();
        private readonly SaveService saveService;

        public CharacterLevelUpPersistenceService(SaveService saveService)
        {
            this.saveService = saveService
                ?? throw new ArgumentNullException(nameof(saveService));
        }

        public CharacterLevelUpPersistenceOperationResult LevelUp(
            string characterId,
            int targetLevel)
        {
            lock (syncRoot)
            {
                CharacterLevelUpResult levelUpResult = null;
                CharacterLevelUpPersistenceStatus failureStatus =
                    CharacterLevelUpPersistenceStatus.PersistenceFailed;
                bool wasRejected = false;

                SaveTransactionResult transaction =
                    saveService.ExecuteTransaction(profile =>
                    {
                        if (CharacterLevelUpProfileUpdater.TryApply(
                            profile,
                            characterId,
                            targetLevel,
                            out levelUpResult,
                            out failureStatus))
                        {
                            return;
                        }

                        wasRejected = true;
                        throw new LevelUpRejectedException(failureStatus);
                    });

                if (transaction.IsSuccess && levelUpResult != null)
                {
                    return new CharacterLevelUpPersistenceOperationResult(
                        CharacterLevelUpPersistenceStatus.Success,
                        levelUpResult,
                        transaction);
                }

                return new CharacterLevelUpPersistenceOperationResult(
                    wasRejected
                        ? failureStatus
                        : CharacterLevelUpPersistenceStatus.PersistenceFailed,
                    levelUpResult: null,
                    saveTransactionResult: transaction);
            }
        }

        private sealed class LevelUpRejectedException : InvalidOperationException
        {
            public LevelUpRejectedException(
                CharacterLevelUpPersistenceStatus status)
                : base($"Character level-up was rejected: {status}.")
            {
            }
        }
    }
}
