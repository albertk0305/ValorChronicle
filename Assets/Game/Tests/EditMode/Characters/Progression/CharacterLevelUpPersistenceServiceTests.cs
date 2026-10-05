using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ValorChronicle.Characters.Progression;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Serialization;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode.Characters.Progression
{
    public sealed class CharacterLevelUpPersistenceServiceTests
    {
        [Test]
        public void LevelUp_OneLevel_SavesCalculatedCostAndReturnsDetails()
        {
            var repository = RepositoryWithProfile(Profile(1, 500));
            SaveService saveService = LoadedService(repository);
            var persistence = new CharacterLevelUpPersistenceService(
                saveService);

            CharacterLevelUpPersistenceOperationResult result =
                persistence.LevelUp("hero_a", 2);

            AssertSuccess(result, 1, 2, 120, 380);
            AssertProfile(saveService, 2, 380);
        }

        [Test]
        public void LevelUp_MultipleLevels_UsesCurrentToTargetCost()
        {
            var repository = RepositoryWithProfile(Profile(10, 2000));
            SaveService saveService = LoadedService(repository);
            var persistence = new CharacterLevelUpPersistenceService(
                saveService);

            CharacterLevelUpPersistenceOperationResult result =
                persistence.LevelUp("hero_a", 15);

            AssertSuccess(result, 10, 15, 1700, 300);
            AssertProfile(saveService, 15, 300);
        }

        [Test]
        public void LevelUp_ExactBalance_SucceedsWithZeroRemaining()
        {
            var repository = RepositoryWithProfile(Profile(99, 2080));
            SaveService saveService = LoadedService(repository);
            var persistence = new CharacterLevelUpPersistenceService(
                saveService);

            CharacterLevelUpPersistenceOperationResult result =
                persistence.LevelUp("hero_a", 100);

            AssertSuccess(result, 99, 100, 2080, 0);
            AssertProfile(saveService, 100, 0);
        }

        [Test]
        public void LevelUp_InsufficientRecords_FailsWithoutAnyChange()
        {
            AssertRejectedWithoutChange(
                Profile(1, 119),
                "hero_a",
                2,
                CharacterLevelUpPersistenceStatus
                    .InsufficientBattleRecords);
        }

        [Test]
        public void LevelUp_MaximumLevel_FailsWithoutAnyChange()
        {
            AssertRejectedWithoutChange(
                Profile(100, 10000),
                "hero_a",
                100,
                CharacterLevelUpPersistenceStatus.MaximumLevelReached);
        }

        [TestCase(20)]
        [TestCase(19)]
        public void LevelUp_NonIncreasingTarget_FailsWithoutAnyChange(
            int targetLevel)
        {
            AssertRejectedWithoutChange(
                Profile(20, 10000),
                "hero_a",
                targetLevel,
                CharacterLevelUpPersistenceStatus.TargetLevelNotHigher);
        }

        [TestCase(0)]
        [TestCase(101)]
        public void LevelUp_TargetOutsideRange_FailsWithoutAnyChange(
            int targetLevel)
        {
            AssertRejectedWithoutChange(
                Profile(1, 10000),
                "hero_a",
                targetLevel,
                CharacterLevelUpPersistenceStatus.InvalidTargetLevel);
        }

        [Test]
        public void LevelUp_EmptyCharacterId_FailsWithoutAnyChange()
        {
            AssertRejectedWithoutChange(
                Profile(1, 10000),
                " ",
                2,
                CharacterLevelUpPersistenceStatus.InvalidCharacterId);
        }

        [Test]
        public void LevelUp_UnknownCharacter_FailsWithoutAnyChange()
        {
            AssertRejectedWithoutChange(
                Profile(1, 10000),
                "missing",
                2,
                CharacterLevelUpPersistenceStatus.CharacterNotOwned);
        }

        [Test]
        public void LevelUp_PersistsLevelAndRecordsInOneTransaction()
        {
            var repository = RepositoryWithProfile(Profile(1, 500));
            SaveService saveService = LoadedService(repository);
            var persistence = new CharacterLevelUpPersistenceService(
                saveService);
            int writesBefore = WriteCount(repository);

            CharacterLevelUpPersistenceOperationResult result =
                persistence.LevelUp("hero_a", 2);

            ProfileSaveData persisted = new NewtonsoftJsonSaveSerializer()
                .Deserialize(repository.MainText);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(WriteCount(repository), Is.EqualTo(writesBefore + 1));
            Assert.That(persisted.Characters[0].Level, Is.EqualTo(2));
            Assert.That(persisted.Currencies.BattleRecords, Is.EqualTo(380));
        }

        [Test]
        public void LevelUp_SaveFailure_RollsBackMemoryAndDisk()
        {
            var repository = RepositoryWithProfile(Profile(1, 500));
            string diskBefore = repository.MainText;
            SaveService saveService = LoadedService(repository);
            var persistence = new CharacterLevelUpPersistenceService(
                saveService);
            repository.FailWriteTemp = true;

            CharacterLevelUpPersistenceOperationResult result =
                persistence.LevelUp("hero_a", 2);

            Assert.That(
                result.Status,
                Is.EqualTo(CharacterLevelUpPersistenceStatus
                    .PersistenceFailed));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.LevelUpResult, Is.Null);
            Assert.That(
                result.SaveTransactionResult.Status,
                Is.EqualTo(SaveTransactionStatus.SaveFailed));
            AssertProfile(saveService, 1, 500);
            Assert.That(repository.MainText, Is.EqualTo(diskBefore));
        }

        [Test]
        public void LevelUp_MutationRejection_RollsBackWithoutWriting()
        {
            var repository = RepositoryWithProfile(Profile(1, 119));
            SaveService saveService = LoadedService(repository);
            var persistence = new CharacterLevelUpPersistenceService(
                saveService);
            int writesBefore = WriteCount(repository);

            CharacterLevelUpPersistenceOperationResult result =
                persistence.LevelUp("hero_a", 2);

            Assert.That(
                result.Status,
                Is.EqualTo(CharacterLevelUpPersistenceStatus
                    .InsufficientBattleRecords));
            Assert.That(
                result.SaveTransactionResult.Status,
                Is.EqualTo(SaveTransactionStatus.MutationThrewException));
            AssertProfile(saveService, 1, 119);
            Assert.That(WriteCount(repository), Is.EqualTo(writesBefore));
        }

        [Test]
        public void LevelUp_ConcurrentDuplicateRequests_ApplyOnlyOnce()
        {
            var repository = RepositoryWithProfile(Profile(1, 240));
            SaveService saveService = LoadedService(repository);
            var persistence = new CharacterLevelUpPersistenceService(
                saveService);
            int writesBefore = WriteCount(repository);
            using (var gate = new ManualResetEventSlim(false))
            {
                Task<CharacterLevelUpPersistenceOperationResult> first =
                    Task.Run(() =>
                    {
                        gate.Wait();
                        return persistence.LevelUp("hero_a", 2);
                    });
                Task<CharacterLevelUpPersistenceOperationResult> second =
                    Task.Run(() =>
                    {
                        gate.Wait();
                        return persistence.LevelUp("hero_a", 2);
                    });

                gate.Set();
                Task.WaitAll(first, second);

                CharacterLevelUpPersistenceOperationResult[] results =
                    { first.Result, second.Result };
                Assert.That(results.Count(value => value.IsSuccess),
                    Is.EqualTo(1));
                Assert.That(
                    results.Count(value => value.Status ==
                        CharacterLevelUpPersistenceStatus
                            .TargetLevelNotHigher),
                    Is.EqualTo(1));
            }

            AssertProfile(saveService, 2, 120);
            Assert.That(WriteCount(repository), Is.EqualTo(writesBefore + 1));
        }

        [Test]
        public void LevelUp_LongMaximumBalance_SubtractsWithoutNarrowing()
        {
            var repository = RepositoryWithProfile(Profile(1, long.MaxValue));
            SaveService saveService = LoadedService(repository);
            var persistence = new CharacterLevelUpPersistenceService(
                saveService);

            CharacterLevelUpPersistenceOperationResult result =
                persistence.LevelUp("hero_a", 2);

            AssertSuccess(
                result,
                1,
                2,
                120,
                long.MaxValue - 120);
            AssertProfile(saveService, 2, long.MaxValue - 120);
        }

        [Test]
        public void LevelUp_DoesNotChangeOtherCurrenciesOrCharacterState()
        {
            ProfileSaveData profile = Profile(1, 500);
            profile.Currencies.GachaCurrency = 101;
            profile.Currencies.HeroTokens = 202;
            profile.Currencies.RelicTokens = 303;
            profile.Characters[0].Awakening = 4;
            profile.Characters[0].IsFavorite = true;
            var repository = RepositoryWithProfile(profile);
            SaveService saveService = LoadedService(repository);
            var persistence = new CharacterLevelUpPersistenceService(
                saveService);

            CharacterLevelUpPersistenceOperationResult result =
                persistence.LevelUp("hero_a", 2);
            ProfileSaveData saved = saveService.GetCurrentProfileSnapshot();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(saved.Currencies.GachaCurrency, Is.EqualTo(101));
            Assert.That(saved.Currencies.HeroTokens, Is.EqualTo(202));
            Assert.That(saved.Currencies.RelicTokens, Is.EqualTo(303));
            Assert.That(saved.Characters[0].Awakening, Is.EqualTo(4));
            Assert.That(saved.Characters[0].IsFavorite, Is.True);
        }

        private static void AssertRejectedWithoutChange(
            ProfileSaveData profile,
            string characterId,
            int targetLevel,
            CharacterLevelUpPersistenceStatus expectedStatus)
        {
            int levelBefore = profile.Characters[0].Level;
            long recordsBefore = profile.Currencies.BattleRecords;
            var repository = RepositoryWithProfile(profile);
            SaveService saveService = LoadedService(repository);
            var persistence = new CharacterLevelUpPersistenceService(
                saveService);
            int writesBefore = WriteCount(repository);

            CharacterLevelUpPersistenceOperationResult result =
                persistence.LevelUp(characterId, targetLevel);

            Assert.That(result.Status, Is.EqualTo(expectedStatus));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.LevelUpResult, Is.Null);
            AssertProfile(saveService, levelBefore, recordsBefore);
            Assert.That(WriteCount(repository), Is.EqualTo(writesBefore));
        }

        private static void AssertSuccess(
            CharacterLevelUpPersistenceOperationResult result,
            int previousLevel,
            int newLevel,
            long spent,
            long remaining)
        {
            Assert.That(
                result.Status,
                Is.EqualTo(CharacterLevelUpPersistenceStatus.Success));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.SaveTransactionResult.IsSuccess, Is.True);
            Assert.That(result.LevelUpResult.CharacterId,
                Is.EqualTo("hero_a"));
            Assert.That(result.LevelUpResult.PreviousLevel,
                Is.EqualTo(previousLevel));
            Assert.That(result.LevelUpResult.NewLevel,
                Is.EqualTo(newLevel));
            Assert.That(result.LevelUpResult.SpentBattleRecords,
                Is.EqualTo(spent));
            Assert.That(result.LevelUpResult.RemainingBattleRecords,
                Is.EqualTo(remaining));
        }

        private static void AssertProfile(
            SaveService saveService,
            int expectedLevel,
            long expectedBattleRecords)
        {
            ProfileSaveData snapshot =
                saveService.GetCurrentProfileSnapshot();
            Assert.That(snapshot.Characters[0].Level,
                Is.EqualTo(expectedLevel));
            Assert.That(snapshot.Currencies.BattleRecords,
                Is.EqualTo(expectedBattleRecords));
        }

        private static ProfileSaveData Profile(int level, long battleRecords)
        {
            ProfileSaveData profile = SaveTestDataBuilder.Valid();
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = "hero_a",
                Level = level,
                Awakening = 0
            });
            profile.Currencies.BattleRecords = battleRecords;
            return profile;
        }

        private static FakeSaveRepository RepositoryWithProfile(
            ProfileSaveData profile)
        {
            return new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.Json(profile)
            };
        }

        private static SaveService LoadedService(
            FakeSaveRepository repository)
        {
            SaveService service = SaveServiceTestFactory.Create(repository);
            SaveLoadResult load = service.LoadOrCreate("ignored");
            Assert.That(load.CanUseProfile, Is.True, load.Message);
            return service;
        }

        private static int WriteCount(FakeSaveRepository repository)
        {
            return repository.Count(nameof(FakeSaveRepository.WriteTemp));
        }
    }
}
