using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Exchange;
using ValorChronicle.Exchange.Persistence;
using ValorChronicle.Relics.Acquisition;
using ValorChronicle.Rewards;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Serialization;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class ExchangeTransactionServiceTests
    {
        private const string CharacterId = "character_a";
        private const string OtherCharacterId = "character_b";
        private const string RelicId = "relic_a";
        private const string OtherRelicId = "relic_b";

        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();
        private DefinitionDatabase database;
        private ExchangeCatalogDefinition catalog;

        [SetUp]
        public void SetUp()
        {
            CharacterDefinition character = Definition<CharacterDefinition>(
                CharacterId);
            CharacterDefinition otherCharacter =
                Definition<CharacterDefinition>(OtherCharacterId);
            RelicDefinition relic = Definition<RelicDefinition>(RelicId);
            RelicDefinition otherRelic =
                Definition<RelicDefinition>(OtherRelicId);
            catalog = ScriptableObject
                .CreateInstance<ExchangeCatalogDefinition>();
            createdObjects.Add(catalog);
            SetField(catalog, "characterIds", new[] { CharacterId });
            SetField(catalog, "relicIds", new[] { RelicId });

            database = ScriptableObject
                .CreateInstance<DefinitionDatabase>();
            createdObjects.Add(database);
            SetField(database, "characters", new[]
            {
                character,
                otherCharacter
            });
            SetField(database, "relics", new[] { relic, otherRelic });
            SetField(database, "exchangeCatalog", catalog);
            database.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = 0; index < createdObjects.Count; index++)
                UnityEngine.Object.DestroyImmediate(createdObjects[index]);
            createdObjects.Clear();
        }

        [Test]
        public void Character_UnownedIsCreatedAtLevelOneAndZeroAwakening()
        {
            TestContext context = Context(Profile(60L, 0L));

            ExchangeTransactionOperationResult operation =
                CharacterExchange(context);
            ProfileSaveData current = Current(context);

            AssertSuccess(operation, 60L, 0L);
            CharacterSaveData character = current.Characters.Single();
            Assert.That(character.CharacterId, Is.EqualTo(CharacterId));
            Assert.That(character.Level, Is.EqualTo(1));
            Assert.That(character.Awakening, Is.Zero);
            Assert.That(character.IsNew, Is.True);
            Assert.That(operation.TransactionResult.IsNewCharacter, Is.True);
            Assert.That(operation.TransactionResult.GrantKind,
                Is.EqualTo(RewardGrantKind.NewCharacter));
        }

        [TestCase(0, 1)]
        [TestCase(5, 6)]
        public void Character_OwnedAwakensOnce(
            int before,
            int after)
        {
            ProfileSaveData profile = Profile(60L, 0L);
            profile.Characters.Add(Character(before));
            TestContext context = Context(profile);

            ExchangeTransactionOperationResult operation =
                CharacterExchange(context);

            AssertSuccess(operation, 60L, 0L);
            Assert.That(Current(context).Characters.Single().Awakening,
                Is.EqualTo(after));
            Assert.That(operation.TransactionResult.PreviousAwakening,
                Is.EqualTo(before));
            Assert.That(operation.TransactionResult.NewAwakening,
                Is.EqualTo(after));
        }

        [Test]
        public void Character_MaxAwakeningIsRejectedWithoutCurrencyChange()
        {
            ProfileSaveData profile = Profile(60L, 0L);
            profile.Characters.Add(Character(6));
            TestContext context = Context(profile);
            string mainBefore = context.Repository.MainText;

            ExchangeTransactionOperationResult operation =
                CharacterExchange(context);

            Assert.That(operation.Status,
                Is.EqualTo(ExchangeTransactionStatus.MaximumAwakening));
            Assert.That(operation.TransactionResult, Is.Null);
            Assert.That(Current(context).Currencies.HeroTokens,
                Is.EqualTo(60L));
            Assert.That(Current(context).Characters.Single().Awakening,
                Is.EqualTo(6));
            Assert.That(context.Repository.MainText, Is.EqualTo(mainBefore));
            AssertNoWrite(context);
        }

        [Test]
        public void Relic_UnownedCreatesCanonicalZeroAwakeningInstance()
        {
            TestContext context = Context(
                Profile(0L, 25L),
                new SequentialIdProvider("new_relic"));

            ExchangeTransactionOperationResult operation =
                RelicExchange(context);
            RelicInstanceSaveData relic = Current(context)
                .RelicInstances.Single();

            AssertSuccess(operation, 25L, 0L);
            Assert.That(relic.InstanceId, Is.EqualTo("new_relic"));
            Assert.That(relic.RelicDefinitionId, Is.EqualTo(RelicId));
            Assert.That(relic.Awakening, Is.Zero);
            Assert.That(relic.IsLocked, Is.False);
            Assert.That(relic.IsNew, Is.True);
            Assert.That(relic.EquippedCharacterId, Is.Empty);
            Assert.That(relic.EquippedSlotIndex, Is.EqualTo(-1));
            Assert.That(operation.TransactionResult.IsNewRelicInstance,
                Is.True);
            Assert.That(operation.TransactionResult.RelicInstanceId,
                Is.EqualTo("new_relic"));
        }

        [TestCase(0, 1)]
        [TestCase(4, 5)]
        public void Relic_AwakenableInstanceAwakensOnce(
            int before,
            int after)
        {
            ProfileSaveData profile = Profile(0L, 25L);
            profile.RelicInstances.Add(Relic("owned", RelicId, before));
            TestContext context = Context(profile);

            ExchangeTransactionOperationResult operation =
                RelicExchange(context);

            AssertSuccess(operation, 25L, 0L);
            Assert.That(Current(context).RelicInstances.Single().Awakening,
                Is.EqualTo(after));
            Assert.That(operation.TransactionResult.RelicInstanceId,
                Is.EqualTo("owned"));
            Assert.That(operation.TransactionResult.PreviousAwakening,
                Is.EqualTo(before));
            Assert.That(operation.TransactionResult.NewAwakening,
                Is.EqualTo(after));
        }

        [Test]
        public void Relic_AllMaximumCreatesNewZeroAwakeningInstance()
        {
            ProfileSaveData profile = Profile(0L, 25L);
            profile.RelicInstances.Add(Relic("max_a", RelicId, 5));
            profile.RelicInstances.Add(Relic("max_b", RelicId, 5));
            TestContext context = Context(
                profile,
                new SequentialIdProvider("new_after_max"));

            ExchangeTransactionOperationResult operation =
                RelicExchange(context);
            ProfileSaveData current = Current(context);

            AssertSuccess(operation, 25L, 0L);
            Assert.That(current.RelicInstances, Has.Count.EqualTo(3));
            Assert.That(current.RelicInstances.Last().Awakening, Is.Zero);
            Assert.That(operation.TransactionResult
                .RelicCreatedAfterMaxAwakening, Is.True);
            Assert.That(operation.TransactionResult.RelicInstanceId,
                Is.EqualTo("new_after_max"));
        }

        [Test]
        public void Relic_HighestSubMaximumInstanceIsSelected()
        {
            ProfileSaveData profile = Profile(0L, 25L);
            profile.RelicInstances.Add(Relic("low", RelicId, 1));
            profile.RelicInstances.Add(Relic("target", RelicId, 4));
            profile.RelicInstances.Add(Relic("max", RelicId, 5));
            TestContext context = Context(profile);

            ExchangeTransactionOperationResult operation =
                RelicExchange(context);

            Assert.That(operation.IsSuccess, Is.True, operation.Message);
            Assert.That(Current(context).RelicInstances
                .Single(item => item.InstanceId == "target").Awakening,
                Is.EqualTo(5));
            Assert.That(Current(context).RelicInstances
                .Single(item => item.InstanceId == "low").Awakening,
                Is.EqualTo(1));
        }

        [Test]
        public void Relic_EqualAwakeningUsesEarlierProfileEntry()
        {
            ProfileSaveData profile = Profile(0L, 25L);
            profile.RelicInstances.Add(Relic("earlier", RelicId, 3));
            profile.RelicInstances.Add(Relic("later", RelicId, 3));
            TestContext context = Context(profile);

            ExchangeTransactionOperationResult operation =
                RelicExchange(context);

            Assert.That(operation.IsSuccess, Is.True, operation.Message);
            Assert.That(operation.TransactionResult.RelicInstanceId,
                Is.EqualTo("earlier"));
            Assert.That(Current(context).RelicInstances[0].Awakening,
                Is.EqualTo(4));
            Assert.That(Current(context).RelicInstances[1].Awakening,
                Is.EqualTo(3));
        }

        [TestCase(59L)]
        [TestCase(0L)]
        public void Character_InsufficientCurrencyIsRejected(long balance)
        {
            TestContext context = Context(Profile(balance, 0L));

            ExchangeTransactionOperationResult operation =
                CharacterExchange(context);

            Assert.That(operation.Status,
                Is.EqualTo(ExchangeTransactionStatus.InsufficientCurrency));
            Assert.That(Current(context).Currencies.HeroTokens,
                Is.EqualTo(balance));
            Assert.That(Current(context).Characters, Is.Empty);
            AssertNoWrite(context);
        }

        [TestCase(24L)]
        [TestCase(0L)]
        public void Relic_InsufficientCurrencyIsRejected(long balance)
        {
            TestContext context = Context(Profile(0L, balance));

            ExchangeTransactionOperationResult operation =
                RelicExchange(context);

            Assert.That(operation.Status,
                Is.EqualTo(ExchangeTransactionStatus.InsufficientCurrency));
            Assert.That(Current(context).Currencies.RelicTokens,
                Is.EqualTo(balance));
            Assert.That(Current(context).RelicInstances, Is.Empty);
            AssertNoWrite(context);
        }

        [Test]
        public void ConfiguredCostIsUsedInsteadOfDefaultConstant()
        {
            SetField(catalog, "characterExchangeCost", 7L);
            TestContext context = Context(Profile(7L, 0L));

            ExchangeTransactionOperationResult operation =
                CharacterExchange(context);

            AssertSuccess(operation, 7L, 0L);
            Assert.That(operation.TransactionResult.Cost, Is.EqualTo(7L));
        }

        [Test]
        public void UnknownAndUnregisteredContentAreRejectedBeforeSave()
        {
            TestContext context = Context(Profile(120L, 50L));

            ExchangeTransactionOperationResult unknown = context.Service
                .Execute(new ExchangeTransactionRequest(
                    ExchangeContentKind.Character,
                    "character_missing"));
            ExchangeTransactionOperationResult unregistered = context.Service
                .Execute(new ExchangeTransactionRequest(
                    ExchangeContentKind.Character,
                    OtherCharacterId));

            Assert.That(unknown.Status,
                Is.EqualTo(ExchangeTransactionStatus.UnknownContent));
            Assert.That(unregistered.Status,
                Is.EqualTo(ExchangeTransactionStatus.NotExchangeable));
            AssertNoWrite(context);
        }

        [Test]
        public void RewardFailureRollsBackCurrencyAndProfile()
        {
            ProfileSaveData profile = Profile(0L, 25L);
            profile.RelicInstances.Add(
                Relic("collision", OtherRelicId, 0));
            TestContext context = Context(
                profile,
                new ConstantIdProvider("collision"));
            string before = Serialize(Current(context));

            ExchangeTransactionOperationResult operation =
                RelicExchange(context);

            Assert.That(operation.Status,
                Is.EqualTo(ExchangeTransactionStatus.RewardGrantFailed));
            Assert.That(Serialize(Current(context)), Is.EqualTo(before));
            AssertNoWrite(context);
        }

        [Test]
        public void PersistenceFailurePreservesProfileAndMainSave()
        {
            TestContext context = Context(Profile(60L, 0L));
            string profileBefore = Serialize(Current(context));
            string mainBefore = context.Repository.MainText;
            context.Repository.FailWriteTemp = true;

            ExchangeTransactionOperationResult operation =
                CharacterExchange(context);

            Assert.That(operation.Status,
                Is.EqualTo(ExchangeTransactionStatus.PersistenceFailed));
            Assert.That(operation.WasPersisted, Is.False);
            Assert.That(Serialize(Current(context)),
                Is.EqualTo(profileBefore));
            Assert.That(context.Repository.MainText, Is.EqualTo(mainBefore));

            context.Repository.FailWriteTemp = false;
            ExchangeTransactionOperationResult retry =
                CharacterExchange(context);
            Assert.That(retry.IsSuccess, Is.True, retry.Message);
        }

        [Test]
        public void InvalidRequestAndInvalidCatalogCostAreRejected()
        {
            TestContext context = Context(Profile(60L, 0L));

            ExchangeTransactionOperationResult nullRequest =
                context.Service.Execute(null);
            ExchangeTransactionOperationResult invalidKind =
                context.Service.Execute(new ExchangeTransactionRequest(
                    (ExchangeContentKind)999,
                    CharacterId));
            SetField(catalog, "characterExchangeCost", 0L);
            ExchangeTransactionOperationResult invalidCost =
                CharacterExchange(context);

            Assert.That(nullRequest.Status,
                Is.EqualTo(ExchangeTransactionStatus.InvalidRequest));
            Assert.That(invalidKind.Status,
                Is.EqualTo(ExchangeTransactionStatus.InvalidRequest));
            Assert.That(invalidCost.Status,
                Is.EqualTo(ExchangeTransactionStatus.NotExchangeable));
            AssertNoWrite(context);
        }

        [Test]
        public void SuccessfulExchangeSurvivesReloadAndWritesOnce()
        {
            TestContext context = Context(Profile(60L, 0L));

            ExchangeTransactionOperationResult operation =
                CharacterExchange(context);
            SaveService reloaded = SaveServiceTestFactory.Create(
                context.Repository,
                new FixedUnixTimeProvider(200L));
            SaveLoadResult load = reloaded.LoadOrCreate("ignored");

            Assert.That(operation.IsSuccess, Is.True, operation.Message);
            Assert.That(load.CanUseProfile, Is.True, load.Message);
            Assert.That(reloaded.GetCurrentProfileSnapshot()
                .Characters.Single().CharacterId, Is.EqualTo(CharacterId));
            Assert.That(reloaded.GetCurrentProfileSnapshot()
                .Currencies.HeroTokens, Is.Zero);
            Assert.That(context.Repository.Count(nameof(
                FakeSaveRepository.WriteTemp)), Is.EqualTo(1));
        }

        [Test]
        public void OwnedCharacterAwakeningSurvivesReload()
        {
            ProfileSaveData profile = Profile(60L, 0L);
            profile.Characters.Add(Character(4));
            TestContext context = Context(profile);

            ExchangeTransactionOperationResult operation =
                CharacterExchange(context);
            ProfileSaveData reloaded = Reload(context);

            Assert.That(operation.IsSuccess, Is.True, operation.Message);
            Assert.That(reloaded.Currencies.HeroTokens, Is.Zero);
            Assert.That(reloaded.Characters.Single().Awakening,
                Is.EqualTo(5));
        }

        [Test]
        public void NewRelicSurvivesReload()
        {
            TestContext context = Context(
                Profile(0L, 25L),
                new SequentialIdProvider("persisted_new_relic"));

            ExchangeTransactionOperationResult operation =
                RelicExchange(context);
            ProfileSaveData reloaded = Reload(context);

            Assert.That(operation.IsSuccess, Is.True, operation.Message);
            Assert.That(reloaded.Currencies.RelicTokens, Is.Zero);
            RelicInstanceSaveData relic = reloaded.RelicInstances.Single();
            Assert.That(relic.InstanceId,
                Is.EqualTo("persisted_new_relic"));
            Assert.That(relic.Awakening, Is.Zero);
        }

        [Test]
        public void OwnedRelicAwakeningSurvivesReload()
        {
            ProfileSaveData profile = Profile(0L, 25L);
            profile.RelicInstances.Add(Relic("owned", RelicId, 3));
            TestContext context = Context(profile);

            ExchangeTransactionOperationResult operation =
                RelicExchange(context);
            ProfileSaveData reloaded = Reload(context);

            Assert.That(operation.IsSuccess, Is.True, operation.Message);
            Assert.That(reloaded.Currencies.RelicTokens, Is.Zero);
            Assert.That(reloaded.RelicInstances.Single().Awakening,
                Is.EqualTo(4));
        }

        [Test]
        public void MaximumRelicCreatesPersistedNewInstance()
        {
            ProfileSaveData profile = Profile(0L, 25L);
            profile.RelicInstances.Add(Relic("maximum", RelicId, 5));
            TestContext context = Context(
                profile,
                new SequentialIdProvider("persisted_after_max"));

            ExchangeTransactionOperationResult operation =
                RelicExchange(context);
            ProfileSaveData reloaded = Reload(context);

            Assert.That(operation.IsSuccess, Is.True, operation.Message);
            Assert.That(reloaded.Currencies.RelicTokens, Is.Zero);
            Assert.That(reloaded.RelicInstances, Has.Count.EqualTo(2));
            Assert.That(reloaded.RelicInstances.Single(item =>
                item.InstanceId == "persisted_after_max").Awakening,
                Is.Zero);
        }

        [Test]
        public void ExchangeDoesNotChangeAnyGachaState()
        {
            ProfileSaveData profile = Profile(60L, 0L);
            profile.GachaStates.Add(new GachaStateSaveData
            {
                GachaId = "gacha_a",
                PityCount = 7,
                TotalPullCount = 20L
            });
            profile.GachaStates.Add(new GachaStateSaveData
            {
                GachaId = "gacha_b",
                PityCount = 3,
                TotalPullCount = 9L
            });
            TestContext context = Context(profile);

            ExchangeTransactionOperationResult operation =
                CharacterExchange(context);
            ProfileSaveData reloaded = Reload(context);

            Assert.That(operation.IsSuccess, Is.True, operation.Message);
            Assert.That(reloaded.GachaStates.Select(state =>
                    $"{state.GachaId}:{state.PityCount}:{state.TotalPullCount}"),
                Is.EqualTo(new[] { "gacha_a:7:20", "gacha_b:3:9" }));
        }

        [Test]
        public void ReentrantRequestIsRejectedAndLockIsReleased()
        {
            var provider = new ReentrantIdProvider("first_instance");
            TestContext context = Context(Profile(0L, 50L), provider);
            provider.OnCreate = () => provider.Nested =
                RelicExchange(context);

            ExchangeTransactionOperationResult first =
                RelicExchange(context);
            ExchangeTransactionOperationResult second =
                RelicExchange(context);

            Assert.That(first.IsSuccess, Is.True, first.Message);
            Assert.That(provider.Nested.Status,
                Is.EqualTo(ExchangeTransactionStatus.TransactionBusy));
            Assert.That(second.IsSuccess, Is.True, second.Message);
            Assert.That(Current(context).Currencies.RelicTokens, Is.Zero);
            Assert.That(Current(context).RelicInstances.Single().Awakening,
                Is.EqualTo(1));
            Assert.That(context.Repository.Count(nameof(
                FakeSaveRepository.WriteTemp)), Is.EqualTo(2));
        }

        private TestContext Context(
            ProfileSaveData profile,
            IRelicInstanceIdProvider idProvider = null)
        {
            var repository = new FakeSaveRepository
            {
                MainText = Serialize(profile)
            };
            SaveService saveService = SaveServiceTestFactory.Create(
                repository,
                new FixedUnixTimeProvider(100L));
            SaveLoadResult load = saveService.LoadOrCreate("ignored");
            Assert.That(load.CanUseProfile, Is.True, load.Message);
            repository.Calls.Clear();
            return new TestContext(
                repository,
                saveService,
                new ExchangeTransactionService(
                    saveService,
                    database,
                    idProvider ?? new SequentialIdProvider("new_relic")));
        }

        private static ExchangeTransactionOperationResult CharacterExchange(
            TestContext context)
        {
            return context.Service.Execute(new ExchangeTransactionRequest(
                ExchangeContentKind.Character,
                CharacterId));
        }

        private static ExchangeTransactionOperationResult RelicExchange(
            TestContext context)
        {
            return context.Service.Execute(new ExchangeTransactionRequest(
                ExchangeContentKind.Relic,
                RelicId));
        }

        private static void AssertSuccess(
            ExchangeTransactionOperationResult operation,
            long before,
            long after)
        {
            Assert.That(operation.IsSuccess, Is.True, operation.Message);
            Assert.That(operation.WasPersisted, Is.True);
            Assert.That(operation.TransactionResult.CurrencyBefore,
                Is.EqualTo(before));
            Assert.That(operation.TransactionResult.CurrencyAfter,
                Is.EqualTo(after));
        }

        private static void AssertNoWrite(TestContext context)
        {
            Assert.That(context.Repository.Count(nameof(
                FakeSaveRepository.WriteTemp)), Is.Zero);
        }

        private T Definition<T>(string id) where T : GameDefinition
        {
            T definition = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(definition);
            SetField(definition, "id", id);
            return definition;
        }

        private static ProfileSaveData Profile(
            long heroTokens,
            long relicTokens)
        {
            ProfileSaveData profile = SaveTestDataBuilder.Valid();
            profile.Currencies.HeroTokens = heroTokens;
            profile.Currencies.RelicTokens = relicTokens;
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
            string relicId,
            int awakening)
        {
            return new RelicInstanceSaveData
            {
                InstanceId = instanceId,
                RelicDefinitionId = relicId,
                Awakening = awakening,
                IsNew = false
            };
        }

        private static ProfileSaveData Current(TestContext context)
        {
            return context.SaveService.GetCurrentProfileSnapshot();
        }

        private static ProfileSaveData Reload(TestContext context)
        {
            SaveService reloaded = SaveServiceTestFactory.Create(
                context.Repository,
                new FixedUnixTimeProvider(200L));
            SaveLoadResult load = reloaded.LoadOrCreate("ignored");
            Assert.That(load.CanUseProfile, Is.True, load.Message);
            return reloaded.GetCurrentProfileSnapshot();
        }

        private static string Serialize(ProfileSaveData profile)
        {
            return new NewtonsoftJsonSaveSerializer().Serialize(profile);
        }

        private static void SetField(
            object target,
            string fieldName,
            object value)
        {
            Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(
                    fieldName,
                    BindingFlags.Instance
                        | BindingFlags.NonPublic
                        | BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            throw new MissingFieldException(
                target.GetType().FullName,
                fieldName);
        }

        private sealed class TestContext
        {
            public TestContext(
                FakeSaveRepository repository,
                SaveService saveService,
                ExchangeTransactionService service)
            {
                Repository = repository;
                SaveService = saveService;
                Service = service;
            }

            public FakeSaveRepository Repository { get; }
            public SaveService SaveService { get; }
            public ExchangeTransactionService Service { get; }
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
                    last = ids.Dequeue();
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

        private sealed class ReentrantIdProvider
            : IRelicInstanceIdProvider
        {
            private readonly string id;
            private bool invoked;

            public ReentrantIdProvider(string id)
            {
                this.id = id;
            }

            public Action OnCreate { get; set; }
            public ExchangeTransactionOperationResult Nested { get; set; }

            public string CreateRelicInstanceId()
            {
                if (!invoked)
                {
                    invoked = true;
                    OnCreate?.Invoke();
                }

                return id;
            }
        }
    }
}
