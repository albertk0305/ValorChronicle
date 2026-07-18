using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class GameInitializationCoordinatorTests
    {
        private sealed class FixedProfileIdProvider : IProfileIdProvider
        {
            private readonly string profileId;
            public FixedProfileIdProvider(string profileId) => this.profileId = profileId;
            public int CallCount { get; private set; }
            public string CreateProfileId()
            {
                CallCount++;
                return profileId;
            }
        }

        [TestCase("loaded")]
        [TestCase("created")]
        [TestCase("repaired")]
        [TestCase("recovered")]
        public async Task InitializeAsync_WritableSaveOutcomes_LoadMainScene(string scenario)
        {
            FakeSaveRepository repository = CreateRepository(scenario);
            int mainLoads = 0;
            var order = new List<string>();
            var profileIds = new FixedProfileIdProvider("profile_created");
            var coordinator = new GameInitializationCoordinator(
                () => order.Add("initialize-content"),
                () => { order.Add("validate-content"); return true; },
                () => { order.Add("create-save"); return SaveServiceTestFactory.Create(repository); },
                profileIds,
                () => { order.Add("load-main"); mainLoads++; return Task.FromResult(true); });

            BootstrapInitializationResult result = await coordinator.InitializeAsync();

            Assert.That(result.Status, Is.EqualTo(BootstrapInitializationStatus.Success));
            Assert.That(result.SaveLoadResult.Status, Is.EqualTo(ExpectedLoadStatus(scenario)));
            Assert.That(result.SaveLoadResult.CanUseProfile, Is.True);
            Assert.That(result.SaveLoadResult.CanWriteProfile, Is.True);
            Assert.That(coordinator.SaveService, Is.Not.Null);
            Assert.That(coordinator.SaveService.HasCurrentProfile, Is.True);
            Assert.That(coordinator.SaveService.CanWriteCurrentProfile, Is.True);
            Assert.That(mainLoads, Is.EqualTo(1));
            Assert.That(profileIds.CallCount, Is.EqualTo(1));
            CollectionAssert.AreEqual(
                new[] { "initialize-content", "validate-content", "create-save", "load-main" },
                order);
        }

        [Test]
        public async Task InitializeAsync_ContentInitializationFails_DoesNotCreateSaveOrLoadMain()
        {
            int saveCreates = 0;
            int mainLoads = 0;
            var coordinator = new GameInitializationCoordinator(
                () => throw new InvalidOperationException("content init"),
                () => true,
                () => { saveCreates++; return SaveServiceTestFactory.Create(new FakeSaveRepository()); },
                new FixedProfileIdProvider("profile"),
                () => { mainLoads++; return Task.FromResult(true); });

            BootstrapInitializationResult result = await coordinator.InitializeAsync();

            Assert.That(result.Status, Is.EqualTo(BootstrapInitializationStatus.ContentInitializationFailed));
            Assert.That(saveCreates, Is.Zero);
            Assert.That(mainLoads, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task InitializeAsync_ContentValidationFails_DoesNotCreateSaveOrLoadMain(bool throws)
        {
            int saveCreates = 0;
            int mainLoads = 0;
            var coordinator = new GameInitializationCoordinator(
                () => { },
                () => throws ? throw new InvalidOperationException("validation") : false,
                () => { saveCreates++; return SaveServiceTestFactory.Create(new FakeSaveRepository()); },
                new FixedProfileIdProvider("profile"),
                () => { mainLoads++; return Task.FromResult(true); });

            BootstrapInitializationResult result = await coordinator.InitializeAsync();

            Assert.That(result.Status, Is.EqualTo(BootstrapInitializationStatus.ContentValidationFailed));
            Assert.That(saveCreates, Is.Zero);
            Assert.That(mainLoads, Is.Zero);
        }

        [TestCase("future", SaveLoadStatus.FutureVersion)]
        [TestCase("older", SaveLoadStatus.UnsupportedOlderVersion)]
        [TestCase("invalid", SaveLoadStatus.MainAndBackupInvalid)]
        [TestCase("readonly", SaveLoadStatus.RecoveredFromBackupButMainRepairFailed)]
        public async Task InitializeAsync_UnusableSave_DoesNotLoadMain(
            string scenario,
            SaveLoadStatus expectedSaveStatus)
        {
            FakeSaveRepository repository = CreateRepository(scenario);
            int mainLoads = 0;
            var coordinator = CreateCoordinator(
                repository,
                () => { mainLoads++; return Task.FromResult(true); });

            BootstrapInitializationResult result = await coordinator.InitializeAsync();

            Assert.That(result.Status, Is.EqualTo(BootstrapInitializationStatus.SaveInitializationFailed));
            Assert.That(result.SaveLoadResult.Status, Is.EqualTo(expectedSaveStatus));
            Assert.That(mainLoads, Is.Zero);
            Assert.That(repository.Count(nameof(FakeSaveRepository.WriteTemp)),
                Is.EqualTo(scenario == "readonly" ? 1 : 0));
        }

        [Test]
        public async Task InitializeAsync_SaveCompositionException_DoesNotLoadMain()
        {
            int mainLoads = 0;
            var coordinator = new GameInitializationCoordinator(
                () => { },
                () => true,
                () => throw new InvalidOperationException("save composition"),
                new FixedProfileIdProvider("profile"),
                () => { mainLoads++; return Task.FromResult(true); });

            BootstrapInitializationResult result = await coordinator.InitializeAsync();

            Assert.That(result.Status, Is.EqualTo(BootstrapInitializationStatus.SaveInitializationFailed));
            Assert.That(result.Exception, Is.TypeOf<InvalidOperationException>());
            Assert.That(mainLoads, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task InitializeAsync_MainSceneFailure_ReturnsSceneLoadFailed(bool throws)
        {
            var repository = new FakeSaveRepository { MainText = SaveTestDataBuilder.ValidJson() };
            Func<Task<bool>> load = throws
                ? () => throw new InvalidOperationException("scene")
                : () => Task.FromResult(false);
            GameInitializationCoordinator coordinator = CreateCoordinator(repository, load);

            BootstrapInitializationResult result = await coordinator.InitializeAsync();

            Assert.That(result.Status, Is.EqualTo(BootstrapInitializationStatus.SceneLoadFailed));
            Assert.That(result.SaveLoadResult.Status, Is.EqualTo(SaveLoadStatus.LoadedMain));
        }

        [Test]
        public async Task InitializeAsync_TransactionDataRemainsAvailableThroughExposedSaveService()
        {
            var repository = new FakeSaveRepository { MainText = SaveTestDataBuilder.ValidJson() };
            GameInitializationCoordinator coordinator = CreateCoordinator(
                repository,
                () => Task.FromResult(true));
            Assert.That((await coordinator.InitializeAsync()).IsSuccess, Is.True);

            SaveTransactionResult transaction = coordinator.SaveService.ExecuteTransaction(
                profile => profile.Currencies.GachaCurrency += 100);

            Assert.That(transaction.IsSuccess, Is.True);
            Assert.That(coordinator.SaveService.GetCurrentProfileSnapshot().Currencies.GachaCurrency,
                Is.EqualTo(100));
        }

        private static GameInitializationCoordinator CreateCoordinator(
            FakeSaveRepository repository,
            Func<Task<bool>> loadMain)
        {
            return new GameInitializationCoordinator(
                () => { },
                () => true,
                () => SaveServiceTestFactory.Create(repository),
                new FixedProfileIdProvider("profile_created"),
                loadMain);
        }

        private static FakeSaveRepository CreateRepository(string scenario)
        {
            switch (scenario)
            {
                case "created":
                    return new FakeSaveRepository();
                case "repaired":
                    return new FakeSaveRepository
                    {
                        MainText = SaveTestDataBuilder.Json(SaveTestDataBuilder.RepairableNegativeCurrency())
                    };
                case "recovered":
                    return new FakeSaveRepository
                    {
                        MainText = SaveTestDataBuilder.CorruptJson(),
                        BackupText = SaveTestDataBuilder.ValidJson("backup")
                    };
                case "future":
                    return new FakeSaveRepository { MainText = SaveTestDataBuilder.FutureJson() };
                case "older":
                    return new FakeSaveRepository { MainText = SaveTestDataBuilder.UnsupportedOlderJson() };
                case "invalid":
                    return new FakeSaveRepository { MainText = "bad", BackupText = "also-bad" };
                case "readonly":
                    return new FakeSaveRepository
                    {
                        MainText = "bad",
                        BackupText = SaveTestDataBuilder.ValidJson("backup"),
                        FailPromoteTempToMain = true
                    };
                default:
                    return new FakeSaveRepository { MainText = SaveTestDataBuilder.ValidJson() };
            }
        }

        private static SaveLoadStatus ExpectedLoadStatus(string scenario)
        {
            switch (scenario)
            {
                case "created": return SaveLoadStatus.CreatedNewProfile;
                case "repaired": return SaveLoadStatus.LoadedAndRepairedMain;
                case "recovered": return SaveLoadStatus.RecoveredFromBackup;
                default: return SaveLoadStatus.LoadedMain;
            }
        }
    }
}
