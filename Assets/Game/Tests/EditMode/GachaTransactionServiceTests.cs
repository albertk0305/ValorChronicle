using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Gacha;
using ValorChronicle.Gacha.Persistence;
using ValorChronicle.Relics.Acquisition;
using ValorChronicle.Rewards;
using ValorChronicle.Save.Copying;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Migration;
using ValorChronicle.Save.Processing;
using ValorChronicle.Save.Repository;
using ValorChronicle.Save.Serialization;
using ValorChronicle.Save.Services;
using ValorChronicle.Save.Validation;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class GachaTransactionServiceTests
    {
        private const string CharacterGachaId =
            "gacha_character_standard";
        private const string RelicGachaId = "gacha_relic_standard";
        private const string CharacterId = "character_marea_bluefang";
        private const string RelicId = "relic_victors_broken_sword";

        private DefinitionDatabase database;

        [SetUp]
        public void SetUp()
        {
            database = AssetDatabase.LoadAssetAtPath<DefinitionDatabase>(
                "Assets/Data/Database/DefinitionDatabase.asset");
            Assert.That(database, Is.Not.Null);
            database.Initialize();
        }

        [TestCase(CharacterGachaId, 1, 100L)]
        [TestCase(CharacterGachaId, 10, 1000L)]
        [TestCase(RelicGachaId, 1, 100L)]
        [TestCase(RelicGachaId, 10, 1000L)]
        public void Pull_SucceedsAndPersistsExactlyOneTransaction(
            string gachaId,
            int pullCount,
            long expectedCost)
        {
            ProfileSaveData profile = Profile(2000L);
            var random = new RewardRollRandomSource(
                gachaId == RelicGachaId ? 5 : 0);
            TestContext context = Context(
                profile,
                random,
                new SequentialIdProvider(
                    Enumerable.Range(0, 10)
                        .Select(index => $"relic_{index}")
                        .ToArray()));

            GachaTransactionOperationResult operation = pullCount == 1
                ? context.Service.PullSingle(gachaId)
                : context.Service.PullTen(gachaId);

            Assert.That(operation.IsSuccess, Is.True, operation.Message);
            Assert.That(operation.WasPersisted, Is.True);
            Assert.That(operation.TransactionResult.PullCount,
                Is.EqualTo(pullCount));
            Assert.That(operation.TransactionResult.GoldSpent,
                Is.EqualTo(expectedCost));
            Assert.That(operation.TransactionResult.GoldBefore,
                Is.EqualTo(2000L));
            Assert.That(operation.TransactionResult.GoldAfter,
                Is.EqualTo(2000L - expectedCost));
            Assert.That(operation.TransactionResult.Pulls,
                Has.Count.EqualTo(pullCount));
            Assert.That(context.Repository.Count(nameof(
                FakeSaveRepository.WriteTemp)), Is.EqualTo(1));
            Assert.That(Current(context).Currencies.GachaCurrency,
                Is.EqualTo(2000L - expectedCost));
            Assert.That(
                Current(context).GachaStates.Single(state =>
                    state.GachaId == gachaId).TotalPullCount,
                Is.EqualTo(pullCount));
        }

        [Test]
        public void ExactGoldBalance_IsAllowedAndReachesZero()
        {
            TestContext context = Context(
                Profile(100L),
                new RewardRollRandomSource(410),
                new SequentialIdProvider("unused"));

            GachaTransactionOperationResult operation =
                context.Service.PullSingle(CharacterGachaId);

            Assert.That(operation.IsSuccess, Is.True, operation.Message);
            Assert.That(operation.TransactionResult.GoldAfter, Is.Zero);
            Assert.That(Current(context).Currencies.GachaCurrency, Is.Zero);
        }

        [Test]
        public void InsufficientGold_DoesNotCalculateOrSave()
        {
            var random = new RewardRollRandomSource(0);
            TestContext context = Context(
                Profile(99L),
                random,
                new SequentialIdProvider("unused"));
            string mainBefore = context.Repository.MainText;

            GachaTransactionOperationResult operation =
                context.Service.PullSingle(CharacterGachaId);

            Assert.That(operation.Status,
                Is.EqualTo(GachaTransactionStatus
                    .InsufficientGachaCurrency));
            Assert.That(operation.TransactionResult, Is.Null);
            Assert.That(operation.WasPersisted, Is.False);
            Assert.That(random.CallCount, Is.Zero);
            Assert.That(context.Repository.Count(nameof(
                FakeSaveRepository.WriteTemp)), Is.Zero);
            Assert.That(context.Repository.MainText, Is.EqualTo(mainBefore));
            Assert.That(Current(context).Currencies.GachaCurrency,
                Is.EqualTo(99L));
            Assert.That(Current(context).GachaStates, Is.Empty);
        }

        [Test]
        public void FirstPull_CreatesStateAndPersistsNewCharacter()
        {
            TestContext context = Context(
                Profile(100L),
                new RewardRollRandomSource(0),
                new SequentialIdProvider("unused"));

            GachaTransactionOperationResult operation =
                context.Service.PullSingle(CharacterGachaId);
            ProfileSaveData current = Current(context);

            Assert.That(operation.IsSuccess, Is.True, operation.Message);
            Assert.That(current.Characters.Single().CharacterId,
                Is.EqualTo(CharacterId));
            Assert.That(current.Characters.Single().Level, Is.EqualTo(1));
            Assert.That(current.Characters.Single().Awakening, Is.Zero);
            Assert.That(current.GachaStates.Single().GachaId,
                Is.EqualTo(CharacterGachaId));
            Assert.That(operation.TransactionResult.Pulls[0].GrantKind,
                Is.EqualTo(RewardGrantKind.NewCharacter));
        }

        [Test]
        public void ExistingStateContinuesAndOtherBannerRemainsUnchanged()
        {
            ProfileSaveData profile = Profile(100L);
            profile.GachaStates.Add(State(CharacterGachaId, 7, 30));
            profile.GachaStates.Add(State(RelicGachaId, 4, 12));
            TestContext context = Context(
                profile,
                new RewardRollRandomSource(410),
                new SequentialIdProvider("unused"));

            GachaTransactionOperationResult operation =
                context.Service.PullSingle(CharacterGachaId);
            ProfileSaveData current = Current(context);

            Assert.That(operation.IsSuccess, Is.True, operation.Message);
            AssertState(current, CharacterGachaId, 8, 31);
            AssertState(current, RelicGachaId, 4, 12);
        }

        [TestCase(CharacterGachaId, 49)]
        [TestCase(RelicGachaId, 19)]
        public void PityPull_GrantsTargetAndResetsCounter(
            string gachaId,
            int pityCount)
        {
            ProfileSaveData profile = Profile(100L);
            profile.GachaStates.Add(State(
                gachaId,
                pityCount,
                pityCount));
            TestContext context = Context(
                profile,
                new RewardRollRandomSource(410),
                new SequentialIdProvider("relic_new"));

            GachaTransactionOperationResult operation =
                context.Service.PullSingle(gachaId);

            Assert.That(operation.IsSuccess, Is.True, operation.Message);
            Assert.That(operation.TransactionResult.Pulls[0]
                .WasGuaranteedByPity, Is.True);
            Assert.That(operation.TransactionResult.ResultingState
                .PityCount, Is.Zero);
            Assert.That(operation.TransactionResult.ResultingState
                .TotalPullCount, Is.EqualTo(pityCount + 1L));
        }

        [Test]
        public void TenPull_MiddlePityResetsThenProcessesRemainingPulls()
        {
            ProfileSaveData profile = Profile(1000L);
            profile.GachaStates.Add(State(CharacterGachaId, 45, 45));
            TestContext context = Context(
                profile,
                new RewardRollRandomSource(410),
                new SequentialIdProvider("unused"));

            GachaTransactionOperationResult operation =
                context.Service.PullTen(CharacterGachaId);

            Assert.That(operation.IsSuccess, Is.True, operation.Message);
            Assert.That(operation.TransactionResult.Pulls[4]
                .WasGuaranteedByPity, Is.True);
            Assert.That(operation.TransactionResult.Pulls[4]
                .RewardKind, Is.EqualTo(GachaRewardKind.RandomCharacter));
            Assert.That(operation.TransactionResult.Pulls[5]
                .WasGuaranteedByPity, Is.False);
            Assert.That(operation.TransactionResult.ResultingState
                .PityCount, Is.EqualTo(5));
            Assert.That(operation.TransactionResult.ResultingState
                .TotalPullCount, Is.EqualTo(55L));
        }

        [Test]
        public void NaturalTargetReward_ResetsPityWithoutGuarantee()
        {
            ProfileSaveData profile = Profile(100L);
            profile.GachaStates.Add(State(CharacterGachaId, 10, 20));
            TestContext context = Context(
                profile,
                new RewardRollRandomSource(0),
                new SequentialIdProvider("unused"));

            GachaTransactionOperationResult operation =
                context.Service.PullSingle(CharacterGachaId);

            Assert.That(operation.IsSuccess, Is.True, operation.Message);
            Assert.That(operation.TransactionResult.Pulls[0]
                .WasGuaranteedByPity, Is.False);
            Assert.That(operation.TransactionResult.ResultingState
                .PityCount, Is.Zero);
        }

        [Test]
        public void SequentialCharacterDuplicates_AwakenThenConvertAndPersist()
        {
            ProfileSaveData profile = Profile(1000L);
            profile.Characters.Add(Character(5));
            TestContext context = Context(
                profile,
                new RewardRollRandomSource(0),
                new SequentialIdProvider("unused"));

            GachaTransactionOperationResult operation =
                context.Service.PullTen(CharacterGachaId);
            ProfileSaveData current = Current(context);

            Assert.That(operation.IsSuccess, Is.True, operation.Message);
            Assert.That(current.Characters.Single().Awakening,
                Is.EqualTo(6));
            Assert.That(current.Currencies.HeroTokens, Is.EqualTo(540L));
            Assert.That(operation.TransactionResult.Pulls[0].GrantKind,
                Is.EqualTo(RewardGrantKind
                    .CharacterAwakeningIncreased));
            Assert.That(operation.TransactionResult.Pulls[1].GrantKind,
                Is.EqualTo(RewardGrantKind
                    .MaxAwakeningCharacterConvertedToHeroTokens));
            Assert.That(operation.TransactionResult.Pulls[1]
                .GrantedCurrencyAmount, Is.EqualTo(60L));
        }

        [Test]
        public void SequentialRelicDuplicates_CreateAndAwakenExtraInstance()
        {
            ProfileSaveData profile = Profile(1000L);
            profile.RelicInstances.Add(Relic("old", 4));
            TestContext context = Context(
                profile,
                new RewardRollRandomSource(5),
                new SequentialIdProvider("new", "newer"));

            GachaTransactionOperationResult operation =
                context.Service.PullTen(RelicGachaId);
            ProfileSaveData current = Current(context);

            Assert.That(operation.IsSuccess, Is.True, operation.Message);
            Assert.That(current.RelicInstances, Has.Count.EqualTo(3));
            Assert.That(current.RelicInstances[0].Awakening,
                Is.EqualTo(5));
            Assert.That(current.RelicInstances[1].InstanceId,
                Is.EqualTo("new"));
            Assert.That(current.RelicInstances[1].Awakening,
                Is.EqualTo(5));
            Assert.That(current.RelicInstances[2].InstanceId,
                Is.EqualTo("newer"));
            Assert.That(current.RelicInstances[2].Awakening,
                Is.EqualTo(2));
            Assert.That(operation.TransactionResult.Pulls[1]
                .RelicCreatedAfterMaxAwakening, Is.True);
        }

        [Test]
        public void CurrencyRewardAndResultDtoMatchPersistedProfile()
        {
            TestContext context = Context(
                Profile(100L),
                new RewardRollRandomSource(410),
                new SequentialIdProvider("unused"));

            GachaTransactionOperationResult operation =
                context.Service.PullSingle(CharacterGachaId);

            Assert.That(operation.IsSuccess, Is.True, operation.Message);
            GachaTransactionPullResult pull =
                operation.TransactionResult.Pulls[0];
            Assert.That(pull.RewardKind,
                Is.EqualTo(GachaRewardKind.BattleRecords));
            Assert.That(pull.Amount, Is.EqualTo(1000L));
            Assert.That(pull.GrantedCurrencyKind,
                Is.EqualTo(RewardCurrencyKind.BattleRecords));
            Assert.That(pull.GrantedCurrencyAmount, Is.EqualTo(1000L));
            Assert.That(Current(context).Currencies.BattleRecords,
                Is.EqualTo(1000L));
            Assert.That(operation.TransactionResult.WasSanitized, Is.False);
        }

        [Test]
        public void InvalidRequestAndInvalidPityState_AreRejectedWithoutSave()
        {
            ProfileSaveData profile = Profile(1000L);
            profile.GachaStates.Add(State(CharacterGachaId, 50, 50));
            var random = new RewardRollRandomSource(0);
            TestContext context = Context(
                profile,
                random,
                new SequentialIdProvider("unused"));

            Assert.That(context.Service.PullSingle("missing").Status,
                Is.EqualTo(GachaTransactionStatus.DefinitionNotFound));
            Assert.That(context.Service.Execute(CharacterGachaId, 2).Status,
                Is.EqualTo(GachaTransactionStatus.UnsupportedPullCount));
            GachaTransactionOperationResult invalidState =
                context.Service.PullSingle(CharacterGachaId);

            Assert.That(invalidState.Status,
                Is.EqualTo(GachaTransactionStatus.InvalidGachaState));
            Assert.That(invalidState.TransactionResult, Is.Null);
            Assert.That(random.CallCount, Is.Zero);
            Assert.That(context.Repository.Count(nameof(
                FakeSaveRepository.WriteTemp)), Is.Zero);
            AssertState(Current(context), CharacterGachaId, 50, 50);
        }

        [Test]
        public void RewardFailure_RollsBackGoldPityAndRelics()
        {
            ProfileSaveData profile = Profile(100L);
            profile.RelicInstances.Add(Relic("collision", 5));
            TestContext context = Context(
                profile,
                new RewardRollRandomSource(5),
                new ConstantIdProvider("collision"));
            string before = Serialize(Current(context));

            GachaTransactionOperationResult operation =
                context.Service.PullSingle(RelicGachaId);

            Assert.That(operation.Status,
                Is.EqualTo(GachaTransactionStatus.RewardGrantFailed));
            Assert.That(operation.TransactionResult, Is.Null);
            Assert.That(operation.WasPersisted, Is.False);
            Assert.That(Serialize(Current(context)), Is.EqualTo(before));
            Assert.That(context.Repository.Count(nameof(
                FakeSaveRepository.WriteTemp)), Is.Zero);
        }

        [Test]
        public void CurrencyOverflow_RollsBackWholeTransaction()
        {
            ProfileSaveData profile = Profile(100L);
            profile.Characters.Add(Character(6));
            profile.Currencies.HeroTokens = long.MaxValue - 59L;
            TestContext context = Context(
                profile,
                new RewardRollRandomSource(0),
                new SequentialIdProvider("unused"));
            string before = Serialize(Current(context));

            GachaTransactionOperationResult operation =
                context.Service.PullSingle(CharacterGachaId);

            Assert.That(operation.Status,
                Is.EqualTo(GachaTransactionStatus.RewardGrantFailed));
            Assert.That(Serialize(Current(context)), Is.EqualTo(before));
            Assert.That(context.Repository.Count(nameof(
                FakeSaveRepository.WriteTemp)), Is.Zero);
        }

        [TestCase("write")]
        [TestCase("promote")]
        [TestCase("verify")]
        public void RepositoryFailure_PreservesCurrentProfileAndMainSave(
            string failure)
        {
            TestContext context = Context(
                Profile(100L),
                new RewardRollRandomSource(410),
                new SequentialIdProvider("unused"));
            string profileBefore = Serialize(Current(context));
            string mainBefore = context.Repository.MainText;
            context.Repository.FailWriteTemp = failure == "write";
            context.Repository.FailPromoteTempToMain =
                failure == "promote";
            if (failure == "verify")
            {
                context.Repository.TempReadOverride = "invalid";
            }

            GachaTransactionOperationResult operation =
                context.Service.PullSingle(CharacterGachaId);

            Assert.That(operation.Status,
                Is.EqualTo(GachaTransactionStatus.PersistenceFailed));
            Assert.That(operation.TransactionResult, Is.Null);
            Assert.That(operation.WasPersisted, Is.False);
            Assert.That(Serialize(Current(context)),
                Is.EqualTo(profileBefore));
            Assert.That(context.Repository.MainText, Is.EqualTo(mainBefore));
        }

        [Test]
        public void SaveValidationFailure_RollsBackWholeTransaction()
        {
            var catalog = new FakeSaveContentCatalog
            {
                CharacterResult = SaveContentLookupResult.Missing
            };
            ProfileSaveData profile = Profile(100L);
            var repository = new FakeSaveRepository
            {
                MainText = Serialize(profile)
            };
            SaveService saveService = CreateSaveService(
                repository,
                catalog,
                new FixedUnixTimeProvider(100));
            Assert.That(saveService.LoadOrCreate("ignored").CanUseProfile,
                Is.True);
            repository.Calls.Clear();
            var service = new GachaTransactionService(
                saveService,
                database,
                new RewardRollRandomSource(0),
                new SequentialIdProvider("unused"));
            string before = Serialize(
                saveService.GetCurrentProfileSnapshot());

            GachaTransactionOperationResult operation =
                service.PullSingle(CharacterGachaId);

            Assert.That(operation.Status,
                Is.EqualTo(GachaTransactionStatus.PersistenceFailed));
            Assert.That(operation.SaveTransactionResult.Status,
                Is.EqualTo(SaveTransactionStatus.ValidationFailed));
            Assert.That(operation.TransactionResult, Is.Null);
            Assert.That(Serialize(saveService.GetCurrentProfileSnapshot()),
                Is.EqualTo(before));
            Assert.That(repository.Count(nameof(
                FakeSaveRepository.WriteTemp)), Is.Zero);
        }

        [Test]
        public void SuccessfulResultSurvivesReloadAndIsDetachedFromProfiles()
        {
            TestContext context = Context(
                Profile(200L),
                new RewardRollRandomSource(0),
                new SequentialIdProvider("unused"));
            GachaTransactionOperationResult operation =
                context.Service.PullSingle(CharacterGachaId);
            ProfileSaveData changedSnapshot = Current(context);
            changedSnapshot.Currencies.GachaCurrency = 999L;
            changedSnapshot.Characters[0].Awakening = 6;
            GachaTransactionOperationResult second =
                context.Service.PullSingle(CharacterGachaId);

            SaveService reloaded = SaveServiceTestFactory.Create(
                context.Repository,
                new FixedUnixTimeProvider(200));
            SaveLoadResult load = reloaded.LoadOrCreate("ignored");

            Assert.That(load.CanUseProfile, Is.True, load.Message);
            Assert.That(second.IsSuccess, Is.True, second.Message);
            Assert.That(reloaded.GetCurrentProfileSnapshot()
                .Currencies.GachaCurrency, Is.Zero);
            Assert.That(reloaded.GetCurrentProfileSnapshot()
                .Characters.Single().Awakening, Is.EqualTo(1));
            Assert.That(operation.TransactionResult.GoldAfter,
                Is.EqualTo(100L));
            Assert.That(operation.TransactionResult.Pulls[0]
                .NewAwakening, Is.Zero);
        }

        private TestContext Context(
            ProfileSaveData profile,
            IRandomSource random,
            IRelicInstanceIdProvider idProvider)
        {
            var repository = new FakeSaveRepository
            {
                MainText = Serialize(profile)
            };
            SaveService saveService = SaveServiceTestFactory.Create(
                repository,
                new FixedUnixTimeProvider(100));
            SaveLoadResult load = saveService.LoadOrCreate("ignored");
            Assert.That(load.CanUseProfile, Is.True, load.Message);
            repository.Calls.Clear();
            return new TestContext(
                repository,
                saveService,
                new GachaTransactionService(
                    saveService,
                    database,
                    random,
                    idProvider));
        }

        private static SaveService CreateSaveService(
            ISaveRepository repository,
            ISaveContentCatalog catalog,
            IUnixTimeProvider timeProvider)
        {
            var cloner = new SaveDataCloner();
            return new SaveService(
                repository,
                new NewtonsoftJsonSaveSerializer(),
                new NewProfileFactory(),
                cloner,
                new SaveMigrationRunner(
                    cloner,
                    new ISaveMigrationStep[]
                    {
                        new V1ToV2PartyMigration(),
                        new V2ToV3RelicEquipMigration()
                    }),
                new SaveValidationProcessor(),
                catalog,
                timeProvider);
        }

        private static ProfileSaveData Profile(long gold)
        {
            ProfileSaveData profile = SaveTestDataBuilder.Valid();
            profile.Currencies.GachaCurrency = gold;
            return profile;
        }

        private static CharacterSaveData Character(int awakening)
        {
            return new CharacterSaveData
            {
                CharacterId = CharacterId,
                Level = 20,
                Awakening = awakening,
                IsNew = false
            };
        }

        private static RelicInstanceSaveData Relic(
            string instanceId,
            int awakening)
        {
            return new RelicInstanceSaveData
            {
                InstanceId = instanceId,
                RelicDefinitionId = RelicId,
                Awakening = awakening,
                IsLocked = true,
                IsNew = false
            };
        }

        private static GachaStateSaveData State(
            string gachaId,
            int pity,
            long total)
        {
            return new GachaStateSaveData
            {
                GachaId = gachaId,
                PityCount = pity,
                TotalPullCount = total
            };
        }

        private static ProfileSaveData Current(TestContext context)
        {
            return context.SaveService.GetCurrentProfileSnapshot();
        }

        private static void AssertState(
            ProfileSaveData profile,
            string gachaId,
            int pity,
            long total)
        {
            GachaStateSaveData state = profile.GachaStates.Single(value =>
                value.GachaId == gachaId);
            Assert.That(state.PityCount, Is.EqualTo(pity));
            Assert.That(state.TotalPullCount, Is.EqualTo(total));
        }

        private static string Serialize(ProfileSaveData profile)
        {
            return new NewtonsoftJsonSaveSerializer().Serialize(profile);
        }

        private sealed class TestContext
        {
            public TestContext(
                FakeSaveRepository repository,
                SaveService saveService,
                GachaTransactionService service)
            {
                Repository = repository;
                SaveService = saveService;
                Service = service;
            }

            public FakeSaveRepository Repository { get; }
            public SaveService SaveService { get; }
            public GachaTransactionService Service { get; }
        }

        private sealed class RewardRollRandomSource : IRandomSource
        {
            private readonly int rewardRoll;

            public RewardRollRandomSource(int rewardRoll)
            {
                this.rewardRoll = rewardRoll;
            }

            public int CallCount { get; private set; }

            public int Next(int minInclusive, int maxExclusive)
            {
                CallCount++;
                return maxExclusive
                    == GachaDefinition.RequiredTotalWeight
                    ? rewardRoll
                    : minInclusive;
            }

            public float NextFloat()
            {
                CallCount++;
                return 0f;
            }
        }

        private sealed class SequentialIdProvider
            : IRelicInstanceIdProvider
        {
            private readonly Queue<string> ids;
            private string last;

            public SequentialIdProvider(params string[] ids)
            {
                this.ids = new Queue<string>(ids);
                last = ids.Length > 0 ? ids[ids.Length - 1] : null;
            }

            public string CreateRelicInstanceId()
            {
                if (ids.Count > 0)
                {
                    last = ids.Dequeue();
                }

                return last;
            }
        }

        private sealed class ConstantIdProvider
            : IRelicInstanceIdProvider
        {
            private readonly string id;

            public ConstantIdProvider(string id)
            {
                this.id = id;
            }

            public string CreateRelicInstanceId()
            {
                return id;
            }
        }
    }
}
