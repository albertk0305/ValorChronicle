using System.Linq;
using NUnit.Framework;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Results;
using ValorChronicle.Battle.Results.Persistence;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode.Battle.Results.Persistence
{
    public sealed class BattleResultPersistenceServiceTests
    {
        [Test]
        public void Persist_SavesCurrencyRecordAndClaimsInOneTransaction()
        {
            var repository = new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.ValidJson()
            };
            SaveService saveService = LoadedService(repository);
            var persistence = new BattleResultPersistenceService(saveService);

            BattleResultPersistenceOperationResult operation =
                persistence.Persist(ChallengeSss());
            ProfileSaveData saved = saveService.GetCurrentProfileSnapshot();
            BossRecordSaveData record = saved.BossRecords.Single();

            Assert.That(operation.Status,
                Is.EqualTo(BattleResultPersistenceStatus.Success));
            Assert.That(operation.SaveTransactionResult.IsSuccess, Is.True);
            Assert.That(operation.PersistedResult, Is.Not.Null);
            Assert.That(operation.PersistedResult.TotalRewardAmount,
                Is.EqualTo(1700L));
            Assert.That(saved.Currencies.GachaCurrency, Is.EqualTo(1700L));
            Assert.That(record.HasAttempted, Is.True);
            Assert.That(record.IsCleared, Is.True);
            Assert.That(record.HighScore,
                Is.EqualTo(operation.PersistedResult.BattleFinalResult.FinalScore));
            Assert.That(record.HighestGradeId,
                Is.EqualTo(BattleGradeIds.SSS));
            Assert.That(record.BestDefeatTurn, Is.EqualTo(1));
            Assert.That(record.BestRemainingTurns, Is.EqualTo(24));
            Assert.That(record.ClaimedFirstRewardGradeIds,
                Is.EqualTo(AllGradeIds()));
        }

        [Test]
        public void Persist_SaveFailureReturnsNoPersistedResultAndRollsBack()
        {
            var repository = new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.ValidJson()
            };
            SaveService saveService = LoadedService(repository);
            ProfileSaveData before = saveService.GetCurrentProfileSnapshot();
            repository.FailWriteTemp = true;
            var persistence = new BattleResultPersistenceService(saveService);

            BattleResultPersistenceOperationResult operation =
                persistence.Persist(ChallengeSss());
            ProfileSaveData after = saveService.GetCurrentProfileSnapshot();

            Assert.That(operation.Status,
                Is.EqualTo(BattleResultPersistenceStatus.SaveFailed));
            Assert.That(operation.IsSuccess, Is.False);
            Assert.That(operation.PersistedResult, Is.Null);
            Assert.That(operation.SaveTransactionResult.Status,
                Is.EqualTo(SaveTransactionStatus.SaveFailed));
            Assert.That(after.Currencies.GachaCurrency,
                Is.EqualTo(before.Currencies.GachaCurrency));
            Assert.That(after.BossRecords, Is.Empty);
        }

        [Test]
        public void Persist_SameSnapshotTwiceDoesNotPayOrSaveTwice()
        {
            var repository = new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.ValidJson()
            };
            SaveService saveService = LoadedService(repository);
            var persistence = new BattleResultPersistenceService(saveService);
            BattleFinalResult finalResult = ChallengeSss();

            BattleResultPersistenceOperationResult first =
                persistence.Persist(finalResult);
            int writeCountAfterFirst = repository.Count(
                nameof(FakeSaveRepository.WriteTemp));
            BattleResultPersistenceOperationResult second =
                persistence.Persist(finalResult);
            ProfileSaveData saved = saveService.GetCurrentProfileSnapshot();

            Assert.That(first.Status,
                Is.EqualTo(BattleResultPersistenceStatus.Success));
            Assert.That(second.Status,
                Is.EqualTo(BattleResultPersistenceStatus.AlreadyPersisted));
            Assert.That(second.WasAlreadyPersisted, Is.True);
            Assert.That(second.PersistedResult,
                Is.SameAs(first.PersistedResult));
            Assert.That(second.SaveTransactionResult, Is.Null);
            Assert.That(repository.Count(nameof(FakeSaveRepository.WriteTemp)),
                Is.EqualTo(writeCountAfterFirst));
            Assert.That(saved.Currencies.GachaCurrency, Is.EqualTo(1700L));
            Assert.That(saved.BossRecords, Has.Count.EqualTo(1));
        }

        [Test]
        public void PersistedProfile_ReloadsRewardRecordAndClaims()
        {
            var repository = new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.ValidJson()
            };
            SaveService firstService = LoadedService(repository);
            var persistence = new BattleResultPersistenceService(
                firstService);

            BattleResultPersistenceOperationResult operation =
                persistence.Persist(ChallengeSss());
            Assert.That(operation.IsSuccess, Is.True);

            SaveService reloadedService = LoadedService(repository);
            ProfileSaveData reloaded =
                reloadedService.GetCurrentProfileSnapshot();
            BossRecordSaveData record = reloaded.BossRecords.Single();

            Assert.That(reloaded.Currencies.GachaCurrency,
                Is.EqualTo(1700L));
            Assert.That(record.HasAttempted, Is.True);
            Assert.That(record.IsCleared, Is.True);
            Assert.That(record.HighestGradeId,
                Is.EqualTo(BattleGradeIds.SSS));
            Assert.That(record.BestRemainingTurns, Is.EqualTo(24));
            Assert.That(record.ClaimedFirstRewardGradeIds,
                Is.EqualTo(AllGradeIds()));
        }

        [Test]
        public void Persist_FailedAttemptCanBeRetried()
        {
            var repository = new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.ValidJson(),
                FailWriteTemp = true
            };
            SaveService saveService = LoadedService(repository);
            var persistence = new BattleResultPersistenceService(saveService);
            BattleFinalResult finalResult = ChallengeSss();

            BattleResultPersistenceOperationResult failed =
                persistence.Persist(finalResult);
            repository.FailWriteTemp = false;
            BattleResultPersistenceOperationResult retried =
                persistence.Persist(finalResult);

            Assert.That(failed.Status,
                Is.EqualTo(BattleResultPersistenceStatus.SaveFailed));
            Assert.That(retried.Status,
                Is.EqualTo(BattleResultPersistenceStatus.Success));
            Assert.That(
                saveService.GetCurrentProfileSnapshot()
                    .Currencies.GachaCurrency,
                Is.EqualTo(1700L));
        }

        [Test]
        public void Persist_MutationFailureDoesNotReturnSuccessResult()
        {
            ProfileSaveData invalid = SaveTestDataBuilder.Valid();
            invalid.BossRecords.Add(new BossRecordSaveData
            {
                BossId = "boss_test",
                DifficultyId = BattleDifficultyIds.Challenge,
                HasAttempted = true,
                HighestGradeId = "grade_unknown",
                ClaimedFirstRewardGradeIds =
                    new System.Collections.Generic.List<string>()
            });
            var repository = new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.Json(invalid)
            };
            SaveService saveService = LoadedService(repository);
            var persistence = new BattleResultPersistenceService(saveService);

            BattleResultPersistenceOperationResult operation =
                persistence.Persist(ChallengeSss());

            Assert.That(operation.Status,
                Is.EqualTo(BattleResultPersistenceStatus.SaveFailed));
            Assert.That(operation.PersistedResult, Is.Null);
            Assert.That(operation.SaveTransactionResult.Status,
                Is.EqualTo(SaveTransactionStatus.MutationThrewException));
            Assert.That(
                saveService.GetCurrentProfileSnapshot()
                    .Currencies.GachaCurrency,
                Is.Zero);
        }

        private static SaveService LoadedService(FakeSaveRepository repository)
        {
            SaveService service = SaveServiceTestFactory.Create(repository);
            SaveLoadResult load = service.LoadOrCreate("ignored");
            Assert.That(load.CanUseProfile, Is.True, load.Message);
            return service;
        }

        private static BattleFinalResult ChallengeSss()
        {
            return BattleResultFinalizer.Create(
                new BattleFinalizationInput(
                    BattleResultKind.Victory,
                    "boss_test",
                    BattleDifficultyIds.Challenge,
                    100000L,
                    25,
                    1,
                    100000L,
                    BattleResultBalanceDefaults.Create()));
        }

        private static string[] AllGradeIds()
        {
            return new[]
            {
                BattleGradeIds.C,
                BattleGradeIds.B,
                BattleGradeIds.A,
                BattleGradeIds.S,
                BattleGradeIds.SS,
                BattleGradeIds.SSS
            };
        }
    }
}
