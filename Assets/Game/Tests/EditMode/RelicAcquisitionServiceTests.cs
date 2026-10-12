using NUnit.Framework;
using ValorChronicle.Relics.Acquisition;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;
using ValorChronicle.Save.Services;
using ValorChronicle.Save.Validation;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class RelicAcquisitionServiceTests
    {
        [Test]
        public void GrantRelic_FirstAcquisitionPersistsAndReturnsSnapshot()
        {
            TestContext context = CreateContext(
                Profile(),
                new RelicAcquisitionProfileUpdaterTests
                    .SequentialIdProvider("sword_a"));
            int writesBefore = context.WriteCount;

            RelicAcquisitionPersistenceOperationResult result =
                context.Service.GrantRelic("relic_sword");

            AssertSuccess(
                result,
                RelicAcquisitionKind.NewInstanceCreated,
                "sword_a",
                previousAwakening: null,
                newAwakening: 0);
            Assert.That(context.WriteCount, Is.EqualTo(writesBefore + 1));
            Assert.That(result.ProfileSnapshot.RelicInstances, Has.Count.EqualTo(1));
        }

        [Test]
        public void GrantRelic_DuplicateAwakensAndRoundTripsFlags()
        {
            ProfileSaveData profile = Profile();
            profile.RelicInstances.Add(Relic("sword_a", 2));
            profile.RelicInstances[0].IsLocked = true;
            profile.RelicInstances[0].IsNew = false;
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = "character_a",
                Level = 1
            });
            profile.Characters[0].EquippedRelicInstanceIds[0] = "sword_a";
            TestContext context = CreateContext(
                profile,
                new RelicAcquisitionProfileUpdaterTests
                    .SequentialIdProvider("unused"));

            RelicAcquisitionPersistenceOperationResult result =
                context.Service.GrantRelic("relic_sword");

            AssertSuccess(
                result,
                RelicAcquisitionKind.AwakeningIncreased,
                "sword_a",
                2,
                3);
            Assert.That(
                result.ProfileSnapshot.Characters[0]
                    .EquippedRelicInstanceIds[0],
                Is.EqualTo("sword_a"));

            SaveService reloaded = SaveServiceTestFactory.Create(
                context.Repository);
            SaveLoadResult load = reloaded.LoadOrCreate("ignored");
            RelicInstanceSaveData relic = reloaded
                .GetCurrentProfileSnapshot().RelicInstances[0];
            Assert.That(load.CanUseProfile, Is.True, load.Message);
            Assert.That(relic.InstanceId, Is.EqualTo("sword_a"));
            Assert.That(relic.RelicDefinitionId, Is.EqualTo("relic_sword"));
            Assert.That(relic.Awakening, Is.EqualTo(3));
            Assert.That(relic.IsLocked, Is.True);
            Assert.That(relic.IsNew, Is.False);
            Assert.That(relic.EquippedCharacterId, Is.Empty);
            Assert.That(
                relic.EquippedSlotIndex,
                Is.EqualTo(SaveRules.UnequippedRelicSlotIndex));
        }

        [Test]
        public void GrantRelic_MissingDefinitionFailsWithoutWrite()
        {
            var catalog = new FakeSaveContentCatalog
            {
                RelicResult = SaveContentLookupResult.Missing
            };
            TestContext context = CreateContext(
                Profile(),
                new RelicAcquisitionProfileUpdaterTests
                    .SequentialIdProvider("unused"),
                catalog);
            int writesBefore = context.WriteCount;

            RelicAcquisitionPersistenceOperationResult result =
                context.Service.GrantRelic("relic_missing");

            Assert.That(
                result.Status,
                Is.EqualTo(RelicAcquisitionPersistenceStatus
                    .DefinitionNotFound));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.SaveTransactionResult, Is.Null);
            Assert.That(context.WriteCount, Is.EqualTo(writesBefore));
            Assert.That(
                context.SaveService.GetCurrentProfileSnapshot()
                    .RelicInstances,
                Is.Empty);
        }

        [Test]
        public void GrantRelic_IdCollisionExhaustionIsAtomic()
        {
            ProfileSaveData profile = Profile();
            profile.RelicInstances.Add(Relic("duplicate", 5));
            TestContext context = CreateContext(
                profile,
                new RelicAcquisitionProfileUpdaterTests
                    .SequentialIdProvider("duplicate"));
            string before = context.Repository.MainText;
            int writesBefore = context.WriteCount;

            RelicAcquisitionPersistenceOperationResult result =
                context.Service.GrantRelic("relic_sword");

            Assert.That(
                result.Status,
                Is.EqualTo(RelicAcquisitionPersistenceStatus
                    .InstanceIdGenerationFailed));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(
                result.SaveTransactionResult.Status,
                Is.EqualTo(SaveTransactionStatus
                    .MutationThrewException));
            Assert.That(context.Repository.MainText, Is.EqualTo(before));
            Assert.That(context.WriteCount, Is.EqualTo(writesBefore));
        }

        [Test]
        public void GrantRelic_SaveFailureRollsBackAuthoritativeProfile()
        {
            TestContext context = CreateContext(
                Profile(),
                new RelicAcquisitionProfileUpdaterTests
                    .SequentialIdProvider("sword_a"));
            string diskBefore = context.Repository.MainText;
            context.Repository.FailWriteTemp = true;

            RelicAcquisitionPersistenceOperationResult result =
                context.Service.GrantRelic("relic_sword");

            Assert.That(
                result.Status,
                Is.EqualTo(RelicAcquisitionPersistenceStatus.SaveFailed));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(
                result.SaveTransactionResult.Status,
                Is.EqualTo(SaveTransactionStatus.SaveFailed));
            Assert.That(
                context.SaveService.GetCurrentProfileSnapshot()
                    .RelicInstances,
                Is.Empty);
            Assert.That(context.Repository.MainText, Is.EqualTo(diskBefore));
        }

        [Test]
        public void GrantRelic_RoundTripPreservesInstanceDataAndFlags()
        {
            TestContext context = CreateContext(
                Profile(),
                new RelicAcquisitionProfileUpdaterTests
                    .SequentialIdProvider("sword_a"));
            Assert.That(
                context.Service.GrantRelic("relic_sword").IsSuccess,
                Is.True);

            SaveService reloaded = SaveServiceTestFactory.Create(
                context.Repository);
            SaveLoadResult load = reloaded.LoadOrCreate("ignored");
            RelicInstanceSaveData relic = reloaded
                .GetCurrentProfileSnapshot().RelicInstances[0];

            Assert.That(load.CanUseProfile, Is.True, load.Message);
            Assert.That(relic.InstanceId, Is.EqualTo("sword_a"));
            Assert.That(relic.RelicDefinitionId, Is.EqualTo("relic_sword"));
            Assert.That(relic.Awakening, Is.Zero);
            Assert.That(relic.IsLocked, Is.False);
            Assert.That(relic.IsNew, Is.True);
            Assert.That(relic.EquippedCharacterId, Is.Empty);
            Assert.That(
                relic.EquippedSlotIndex,
                Is.EqualTo(SaveRules.UnequippedRelicSlotIndex));
        }

        private static TestContext CreateContext(
            ProfileSaveData profile,
            IRelicInstanceIdProvider provider,
            FakeSaveContentCatalog catalog = null)
        {
            var repository = new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.Json(profile)
            };
            SaveService saveService = SaveServiceTestFactory.Create(
                repository);
            SaveLoadResult load = saveService.LoadOrCreate("ignored");
            Assert.That(load.CanUseProfile, Is.True, load.Message);
            return new TestContext(
                repository,
                saveService,
                catalog ?? new FakeSaveContentCatalog(),
                provider);
        }

        private static ProfileSaveData Profile()
        {
            return SaveTestDataBuilder.Valid();
        }

        private static RelicInstanceSaveData Relic(
            string instanceId,
            int awakening)
        {
            return new RelicInstanceSaveData
            {
                InstanceId = instanceId,
                RelicDefinitionId = "relic_sword",
                Awakening = awakening
            };
        }

        private static void AssertSuccess(
            RelicAcquisitionPersistenceOperationResult result,
            RelicAcquisitionKind expectedKind,
            string expectedInstanceId,
            int? previousAwakening,
            int newAwakening)
        {
            Assert.That(
                result.Status,
                Is.EqualTo(RelicAcquisitionPersistenceStatus.Success));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.SaveTransactionResult.IsSuccess, Is.True);
            Assert.That(result.ProfileSnapshot, Is.Not.Null);
            Assert.That(result.AcquisitionResult.Kind, Is.EqualTo(expectedKind));
            Assert.That(
                result.AcquisitionResult.RelicDefinitionId,
                Is.EqualTo("relic_sword"));
            Assert.That(
                result.AcquisitionResult.AffectedInstanceId,
                Is.EqualTo(expectedInstanceId));
            Assert.That(
                result.AcquisitionResult.PreviousAwakening,
                Is.EqualTo(previousAwakening));
            Assert.That(
                result.AcquisitionResult.NewAwakening,
                Is.EqualTo(newAwakening));
        }

        private sealed class TestContext
        {
            public TestContext(
                FakeSaveRepository repository,
                SaveService saveService,
                FakeSaveContentCatalog catalog,
                IRelicInstanceIdProvider provider)
            {
                Repository = repository;
                SaveService = saveService;
                Service = new RelicAcquisitionService(
                    saveService,
                    catalog,
                    provider);
            }

            public FakeSaveRepository Repository { get; }
            public SaveService SaveService { get; }
            public RelicAcquisitionService Service { get; }
            public int WriteCount => Repository.Count(
                nameof(FakeSaveRepository.WriteTemp));
        }
    }
}
