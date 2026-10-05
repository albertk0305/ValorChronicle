using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Editor.SaveDebug;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class SaveDebugProfileServiceTests
    {
        private const string MareaId =
            SaveDebugProfileService.MareaCharacterId;

        private FakeSaveRepository repository;
        private SaveService saveService;
        private SaveDebugProfileService debugService;

        [TestCase(SaveDebugCurrency.GachaCurrency, 101L)]
        [TestCase(SaveDebugCurrency.BattleRecords, 202L)]
        [TestCase(SaveDebugCurrency.HeroTokens, 303L)]
        [TestCase(SaveDebugCurrency.RelicTokens, 404L)]
        public void SetCurrency_ValidValuePersists(
            SaveDebugCurrency currency,
            long expected)
        {
            Initialize();

            SaveDebugOperationResult result =
                debugService.SetCurrency(currency, expected);

            Assert.That(result.IsSuccess, Is.True, result.Message);
            Assert.That(ReadCurrency(Current(), currency), Is.EqualTo(expected));
        }

        [Test]
        public void AddCurrency_ValidPositiveAndNegativeAmountsPersist()
        {
            ProfileSaveData profile = CreateProfile();
            profile.Currencies.BattleRecords = 100;
            Initialize(profile);

            Assert.That(
                debugService.AddCurrency(
                    SaveDebugCurrency.BattleRecords,
                    50).IsSuccess,
                Is.True);
            Assert.That(
                debugService.AddCurrency(
                    SaveDebugCurrency.BattleRecords,
                    -25).IsSuccess,
                Is.True);

            Assert.That(Current().Currencies.BattleRecords, Is.EqualTo(125));
        }

        [Test]
        public void AddCurrency_NegativeResultAndOverflowAreRejectedBeforeTransaction()
        {
            ProfileSaveData profile = CreateProfile();
            profile.Currencies.BattleRecords = 10;
            profile.Currencies.GachaCurrency = long.MaxValue;
            Initialize(profile);
            int writesBefore = repository.Count(nameof(repository.WriteTemp));

            SaveDebugOperationResult negative = debugService.AddCurrency(
                SaveDebugCurrency.BattleRecords,
                -11);
            SaveDebugOperationResult overflow = debugService.AddCurrency(
                SaveDebugCurrency.GachaCurrency,
                1);

            Assert.That(
                negative.Status,
                Is.EqualTo(SaveDebugOperationStatus.InvalidInput));
            Assert.That(
                overflow.Status,
                Is.EqualTo(SaveDebugOperationStatus.InvalidInput));
            Assert.That(Current().Currencies.BattleRecords, Is.EqualTo(10));
            Assert.That(Current().Currencies.GachaCurrency, Is.EqualTo(long.MaxValue));
            Assert.That(
                repository.Count(nameof(repository.WriteTemp)),
                Is.EqualTo(writesBefore));
        }

        [Test]
        public void SetCurrency_NegativeValueIsRejectedBeforeTransaction()
        {
            ProfileSaveData profile = CreateProfile();
            profile.Currencies.HeroTokens = 12;
            Initialize(profile);
            int writesBefore = repository.Count(nameof(repository.WriteTemp));

            SaveDebugOperationResult result = debugService.SetCurrency(
                SaveDebugCurrency.HeroTokens,
                -1);

            Assert.That(
                result.Status,
                Is.EqualTo(SaveDebugOperationStatus.InvalidInput));
            Assert.That(Current().Currencies.HeroTokens, Is.EqualTo(12));
            Assert.That(
                repository.Count(nameof(repository.WriteTemp)),
                Is.EqualTo(writesBefore));
        }

        [Test]
        public void GrantCharacter_UnownedMareaCreatesLevelOneAwakeningZero()
        {
            Initialize();

            SaveDebugOperationResult result =
                debugService.GrantCharacter(MareaId);

            Assert.That(result.IsSuccess, Is.True, result.Message);
            CharacterSaveData marea = Current().Characters[0];
            Assert.That(marea.CharacterId, Is.EqualTo(MareaId));
            Assert.That(marea.Level, Is.EqualTo(1));
            Assert.That(marea.Awakening, Is.Zero);
            Assert.That(marea.IsFavorite, Is.False);
            Assert.That(marea.IsNew, Is.False);
        }

        [Test]
        public void GrantCharacter_AlreadyOwnedIsNoOpWithoutDuplicateReward()
        {
            ProfileSaveData profile = CreateProfile();
            profile.Characters.Add(CreateMarea(7, 3));
            Initialize(profile);
            int writesBefore = repository.Count(nameof(repository.WriteTemp));

            SaveDebugOperationResult result =
                debugService.GrantCharacter(MareaId);

            Assert.That(
                result.Status,
                Is.EqualTo(SaveDebugOperationStatus.NoChange));
            Assert.That(Current().Characters, Has.Count.EqualTo(1));
            Assert.That(Current().Characters[0].Level, Is.EqualTo(7));
            Assert.That(Current().Characters[0].Awakening, Is.EqualTo(3));
            Assert.That(
                repository.Count(nameof(repository.WriteTemp)),
                Is.EqualTo(writesBefore));
        }

        [TestCase(1)]
        [TestCase(100)]
        public void SetCharacterLevel_BoundariesAreAllowed(int level)
        {
            InitializeWithMarea();

            SaveDebugOperationResult result =
                debugService.SetCharacterLevel(MareaId, level);

            Assert.That(result.IsSuccess, Is.True, result.Message);
            Assert.That(Current().Characters[0].Level, Is.EqualTo(level));
        }

        [TestCase(0)]
        [TestCase(101)]
        public void SetCharacterLevel_OutOfRangeIsRejected(int level)
        {
            InitializeWithMarea(level: 10);

            SaveDebugOperationResult result =
                debugService.SetCharacterLevel(MareaId, level);

            Assert.That(
                result.Status,
                Is.EqualTo(SaveDebugOperationStatus.InvalidInput));
            Assert.That(Current().Characters[0].Level, Is.EqualTo(10));
        }

        [TestCase(0)]
        [TestCase(6)]
        public void SetCharacterAwakening_BoundariesAreAllowed(int awakening)
        {
            InitializeWithMarea(awakening: 3);

            SaveDebugOperationResult result =
                debugService.SetCharacterAwakening(MareaId, awakening);

            Assert.That(result.IsSuccess, Is.True, result.Message);
            Assert.That(
                Current().Characters[0].Awakening,
                Is.EqualTo(awakening));
        }

        [TestCase(-1)]
        [TestCase(7)]
        public void SetCharacterAwakening_OutOfRangeIsRejected(int awakening)
        {
            InitializeWithMarea(awakening: 3);

            SaveDebugOperationResult result =
                debugService.SetCharacterAwakening(MareaId, awakening);

            Assert.That(
                result.Status,
                Is.EqualTo(SaveDebugOperationStatus.InvalidInput));
            Assert.That(Current().Characters[0].Awakening, Is.EqualTo(3));
        }

        [Test]
        public void TransactionFailure_PreservesCurrentProfileAndMainSave()
        {
            ProfileSaveData profile = CreateProfile();
            profile.Currencies.BattleRecords = 15;
            Initialize(profile);
            string mainBefore = repository.MainText;
            repository.FailWriteTemp = true;

            SaveDebugOperationResult result = debugService.SetCurrency(
                SaveDebugCurrency.BattleRecords,
                999);

            Assert.That(
                result.Status,
                Is.EqualTo(SaveDebugOperationStatus.PersistenceFailed));
            Assert.That(Current().Currencies.BattleRecords, Is.EqualTo(15));
            Assert.That(repository.MainText, Is.EqualTo(mainBefore));
        }

        [Test]
        public void Preset_PreservesUnrelatedCurrenciesPartyAndBossRecord()
        {
            ProfileSaveData profile = CreateProfile();
            profile.Currencies.GachaCurrency = 11;
            profile.Currencies.HeroTokens = 22;
            profile.Currencies.RelicTokens = 33;
            profile.Party.ActivePresetIndex = 2;
            profile.Party.Presets[2].CharacterSlotIds[4] = string.Empty;
            profile.BossRecords.Add(new BossRecordSaveData
            {
                BossId = "boss_a",
                DifficultyId = "normal",
                HasAttempted = true,
                IsCleared = true,
                HighScore = 456,
                HighestGradeId = "grade_s",
                BestDefeatTurn = 8,
                BestRemainingTurns = 17,
                ClaimedFirstRewardGradeIds = new List<string>
                {
                    "grade_c"
                }
            });
            Initialize(profile);

            SaveDebugOperationResult result = debugService.ApplyStage10Preset(
                Stage10SavePreset.FreshMarea);

            Assert.That(result.IsSuccess, Is.True, result.Message);
            ProfileSaveData saved = Current();
            Assert.That(saved.Currencies.GachaCurrency, Is.EqualTo(11));
            Assert.That(saved.Currencies.HeroTokens, Is.EqualTo(22));
            Assert.That(saved.Currencies.RelicTokens, Is.EqualTo(33));
            Assert.That(saved.Party.ActivePresetIndex, Is.EqualTo(2));
            Assert.That(
                saved.Party.Presets[2].CharacterSlotIds[4],
                Is.EqualTo(string.Empty));
            Assert.That(saved.BossRecords, Has.Count.EqualTo(1));
            Assert.That(saved.BossRecords[0].HighScore, Is.EqualTo(456));
            Assert.That(saved.BossRecords[0].HighestGradeId, Is.EqualTo("grade_s"));
            Assert.That(saved.BossRecords[0].BestDefeatTurn, Is.EqualTo(8));
            Assert.That(saved.BossRecords[0].BestRemainingTurns, Is.EqualTo(17));
            Assert.That(
                saved.BossRecords[0].ClaimedFirstRewardGradeIds,
                Is.EqualTo(new[] { "grade_c" }));
        }

        [TestCase(Stage10SavePreset.FreshMarea, 1, 0, 150000L)]
        [TestCase(Stage10SavePreset.Insufficient, 1, 0, 0L)]
        [TestCase(Stage10SavePreset.Level99Boundary, 99, 0, 2080L)]
        [TestCase(Stage10SavePreset.MaxAwakening, 100, 6, 777L)]
        public void Stage10Preset_AppliesExactValuesInOneTransaction(
            Stage10SavePreset preset,
            int expectedLevel,
            int expectedAwakening,
            long expectedBattleRecords)
        {
            ProfileSaveData profile = CreateProfile();
            profile.Currencies.BattleRecords = 777;
            Initialize(profile);
            int writesBefore = repository.Count(nameof(repository.WriteTemp));

            SaveDebugOperationResult result =
                debugService.ApplyStage10Preset(preset);

            Assert.That(result.IsSuccess, Is.True, result.Message);
            ProfileSaveData saved = Current();
            Assert.That(saved.Characters, Has.Count.EqualTo(1));
            Assert.That(saved.Characters[0].CharacterId, Is.EqualTo(MareaId));
            Assert.That(saved.Characters[0].Level, Is.EqualTo(expectedLevel));
            Assert.That(
                saved.Characters[0].Awakening,
                Is.EqualTo(expectedAwakening));
            Assert.That(
                saved.Currencies.BattleRecords,
                Is.EqualTo(expectedBattleRecords));
            Assert.That(
                repository.Count(nameof(repository.WriteTemp)) - writesBefore,
                Is.EqualTo(1));
        }

        private void InitializeWithMarea(int level = 10, int awakening = 0)
        {
            ProfileSaveData profile = CreateProfile();
            profile.Characters.Add(CreateMarea(level, awakening));
            Initialize(profile);
        }

        private void Initialize(ProfileSaveData profile = null)
        {
            repository = new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.Json(profile ?? CreateProfile())
            };
            saveService = SaveServiceTestFactory.Create(
                repository,
                new FixedUnixTimeProvider(100, 101, 102, 103));
            SaveLoadResult load = saveService.LoadOrCreate("unused");
            Assert.That(load.IsSuccess, Is.True, load.Message);
            debugService = new SaveDebugProfileService(
                saveService,
                new[] { MareaId });
        }

        private ProfileSaveData Current()
        {
            return saveService.GetCurrentProfileSnapshot();
        }

        private static ProfileSaveData CreateProfile()
        {
            return SaveTestDataBuilder.Valid();
        }

        private static CharacterSaveData CreateMarea(
            int level,
            int awakening)
        {
            return new CharacterSaveData
            {
                CharacterId = MareaId,
                Level = level,
                Awakening = awakening
            };
        }

        private static long ReadCurrency(
            ProfileSaveData profile,
            SaveDebugCurrency currency)
        {
            switch (currency)
            {
                case SaveDebugCurrency.GachaCurrency:
                    return profile.Currencies.GachaCurrency;
                case SaveDebugCurrency.BattleRecords:
                    return profile.Currencies.BattleRecords;
                case SaveDebugCurrency.HeroTokens:
                    return profile.Currencies.HeroTokens;
                case SaveDebugCurrency.RelicTokens:
                    return profile.Currencies.RelicTokens;
                default:
                    throw new System.ArgumentOutOfRangeException(
                        nameof(currency),
                        currency,
                        null);
            }
        }
    }
}
