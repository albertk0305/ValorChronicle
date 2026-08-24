using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Results;
using ValorChronicle.Battle.Results.Persistence;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode.Battle.Results.Persistence
{
    public sealed class BattleResultPersistenceCoordinatorTests
    {
        [Test]
        public void ResultFinalized_PersistsAndPublishesAfterStateIsStored()
        {
            var repository = Repository();
            var source = new FakeFinalResultSource();
            var persistence = new BattleResultPersistenceService(
                LoadedService(repository));
            var coordinator = new BattleResultPersistenceCoordinator(
                source,
                persistence);
            BattleFinalResult finalResult = FinalResult(
                BattleResultKind.Victory);
            int eventCount = 0;
            BattlePersistedResult eventResult = null;
            coordinator.ResultPersisted += persisted =>
            {
                eventCount++;
                eventResult = persisted;
                Assert.That(coordinator.LastPersistedResult,
                    Is.SameAs(persisted));
                Assert.That(coordinator.IsPersistenceCompleted, Is.True);
            };

            source.Publish(finalResult);

            Assert.That(repository.Count(
                nameof(FakeSaveRepository.WriteTemp)), Is.EqualTo(1));
            Assert.That(coordinator.LastPersistenceOperation.Status,
                Is.EqualTo(BattleResultPersistenceStatus.Success));
            Assert.That(coordinator.LastPersistedResult, Is.SameAs(eventResult));
            Assert.That(coordinator.HasPendingPersistence, Is.False);
            Assert.That(eventCount, Is.EqualTo(1));
        }

        [Test]
        public void DuplicateFinalResult_DoesNotSaveOrPublishTwice()
        {
            var repository = Repository();
            var source = new FakeFinalResultSource();
            var coordinator = new BattleResultPersistenceCoordinator(
                source,
                new BattleResultPersistenceService(LoadedService(repository)));
            BattleFinalResult finalResult = FinalResult(
                BattleResultKind.Victory);
            int eventCount = 0;
            coordinator.ResultPersisted += _ => eventCount++;

            source.Publish(finalResult);
            source.Publish(finalResult);

            Assert.That(repository.Count(
                nameof(FakeSaveRepository.WriteTemp)), Is.EqualTo(1));
            Assert.That(eventCount, Is.EqualTo(1));
        }

        [Test]
        public void AlreadyPersisted_ReusesResultAndPublishesOnce()
        {
            var repository = Repository();
            var persistence = new BattleResultPersistenceService(
                LoadedService(repository));
            BattleFinalResult finalResult = FinalResult(
                BattleResultKind.Victory);
            BattlePersistedResult existing =
                persistence.Persist(finalResult).PersistedResult;
            int writeCount = repository.Count(
                nameof(FakeSaveRepository.WriteTemp));
            var source = new FakeFinalResultSource();
            var coordinator = new BattleResultPersistenceCoordinator(
                source,
                persistence);
            int eventCount = 0;
            coordinator.ResultPersisted += _ => eventCount++;

            source.Publish(finalResult);
            source.Publish(finalResult);

            Assert.That(coordinator.LastPersistenceOperation.Status,
                Is.EqualTo(BattleResultPersistenceStatus.AlreadyPersisted));
            Assert.That(coordinator.LastPersistedResult, Is.SameAs(existing));
            Assert.That(repository.Count(
                nameof(FakeSaveRepository.WriteTemp)), Is.EqualTo(writeCount));
            Assert.That(eventCount, Is.EqualTo(1));
        }

        [Test]
        public void SaveFailure_KeepsPendingAndRetryCanPublishOnce()
        {
            var repository = Repository();
            SaveService saveService = LoadedService(repository);
            ProfileSaveData before = saveService.GetCurrentProfileSnapshot();
            repository.FailWriteTemp = true;
            var source = new FakeFinalResultSource();
            var coordinator = new BattleResultPersistenceCoordinator(
                source,
                new BattleResultPersistenceService(saveService));
            int eventCount = 0;
            coordinator.ResultPersisted += _ => eventCount++;
            LogAssert.Expect(
                LogType.Error,
                new Regex("BattleResultPersistence.*Save failed"));

            source.Publish(FinalResult(BattleResultKind.Victory));

            ProfileSaveData failed = saveService.GetCurrentProfileSnapshot();
            Assert.That(coordinator.LastPersistenceOperation.Status,
                Is.EqualTo(BattleResultPersistenceStatus.SaveFailed));
            Assert.That(coordinator.LastPersistedResult, Is.Null);
            Assert.That(coordinator.IsPersistenceCompleted, Is.False);
            Assert.That(coordinator.HasPendingPersistence, Is.True);
            Assert.That(eventCount, Is.Zero);
            Assert.That(failed.Currencies.GachaCurrency,
                Is.EqualTo(before.Currencies.GachaCurrency));
            Assert.That(failed.BossRecords, Is.Empty);

            repository.FailWriteTemp = false;
            BattleResultPersistenceOperationResult retried =
                coordinator.RetryPendingPersistence();

            Assert.That(retried.Status,
                Is.EqualTo(BattleResultPersistenceStatus.Success));
            Assert.That(coordinator.HasPendingPersistence, Is.False);
            Assert.That(coordinator.LastPersistedResult, Is.Not.Null);
            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(saveService.GetCurrentProfileSnapshot().BossRecords,
                Has.Count.EqualTo(1));
        }

        [TestCase(BattleResultKind.Victory)]
        [TestCase(BattleResultKind.Defeat)]
        [TestCase(BattleResultKind.TurnLimitReached)]
        public void NormalResultKinds_ArePersisted(BattleResultKind resultKind)
        {
            var source = new FakeFinalResultSource();
            var coordinator = new BattleResultPersistenceCoordinator(
                source,
                new BattleResultPersistenceService(
                    LoadedService(Repository())));

            source.Publish(FinalResult(resultKind));

            Assert.That(coordinator.LastPersistedResult, Is.Not.Null);
            Assert.That(
                coordinator.LastPersistedResult.BattleFinalResult.EndReason,
                Is.EqualTo(resultKind));
        }

        [Test]
        public void NoFinalResult_DoesNotAttemptPersistence()
        {
            var repository = Repository();
            var source = new FakeFinalResultSource();
            var coordinator = new BattleResultPersistenceCoordinator(
                source,
                new BattleResultPersistenceService(LoadedService(repository)));

            Assert.That(coordinator.LastPersistenceOperation, Is.Null);
            Assert.That(coordinator.LastPersistedResult, Is.Null);
            Assert.That(coordinator.HasPendingPersistence, Is.False);
            Assert.That(repository.Count(
                nameof(FakeSaveRepository.WriteTemp)), Is.Zero);
        }

        [Test]
        public void NewCoordinator_HasCleanBattleScopedState()
        {
            SaveService saveService = LoadedService(Repository());
            var firstSource = new FakeFinalResultSource();
            var first = new BattleResultPersistenceCoordinator(
                firstSource,
                new BattleResultPersistenceService(saveService));
            firstSource.Publish(FinalResult(BattleResultKind.Victory));

            var second = new BattleResultPersistenceCoordinator(
                new FakeFinalResultSource(),
                new BattleResultPersistenceService(saveService));

            Assert.That(first.LastPersistedResult, Is.Not.Null);
            Assert.That(second.LastPersistedResult, Is.Null);
            Assert.That(second.LastPersistenceOperation, Is.Null);
            Assert.That(second.HasPendingPersistence, Is.False);
            Assert.That(second.IsPersistenceCompleted, Is.False);
        }

        [Test]
        public void Dispose_UnsubscribesOnceAndPreventsLaterPersistence()
        {
            var repository = Repository();
            var source = new FakeFinalResultSource();
            var coordinator = new BattleResultPersistenceCoordinator(
                source,
                new BattleResultPersistenceService(LoadedService(repository)));
            Assert.That(source.SubscriberCount, Is.EqualTo(1));

            coordinator.Dispose();
            coordinator.Dispose();
            source.Publish(FinalResult(BattleResultKind.Victory));

            Assert.That(source.SubscriberCount, Is.Zero);
            Assert.That(coordinator.LastPersistenceOperation, Is.Null);
            Assert.That(repository.Count(
                nameof(FakeSaveRepository.WriteTemp)), Is.Zero);
        }

        private static FakeSaveRepository Repository()
        {
            return new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.ValidJson()
            };
        }

        private static SaveService LoadedService(FakeSaveRepository repository)
        {
            SaveService service = SaveServiceTestFactory.Create(repository);
            SaveLoadResult load = service.LoadOrCreate("ignored");
            Assert.That(load.CanUseProfile, Is.True, load.Message);
            return service;
        }

        private static BattleFinalResult FinalResult(
            BattleResultKind resultKind)
        {
            int finishedTurn = resultKind ==
                BattleResultKind.TurnLimitReached ? 25 : 10;
            long damageScore = resultKind == BattleResultKind.Victory
                ? 100000L
                : 50000L;
            return BattleResultFinalizer.Create(
                new BattleFinalizationInput(
                    resultKind,
                    "boss_test",
                    BattleDifficultyIds.Normal,
                    100000L,
                    25,
                    finishedTurn,
                    damageScore,
                    BattleResultBalanceDefaults.Create()));
        }

        private sealed class FakeFinalResultSource : IBattleFinalResultSource
        {
            private Action<BattleFinalResult> handlers;

            public event Action<BattleFinalResult> ResultFinalized
            {
                add
                {
                    handlers += value;
                    SubscriberCount++;
                }
                remove
                {
                    handlers -= value;
                    SubscriberCount--;
                }
            }

            public int SubscriberCount { get; private set; }

            public void Publish(BattleFinalResult result)
            {
                handlers?.Invoke(result);
            }
        }
    }
}
