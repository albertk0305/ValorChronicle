using NUnit.Framework;
using ValorChronicle.Redeem;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode.Settings
{
    public sealed class RedeemCodeServiceTests
    {
        [TestCase("NEAT3")]
        [TestCase("neat3")]
        [TestCase(" NeAt3 ")]
        public void RegisteredCode_GrantsGachaCurrencyAndPersists(
            string input)
        {
            TestContext context = CreateContext();
            ProfileSaveData before =
                context.SaveService.GetCurrentProfileSnapshot();

            RedeemCodeResult result = context.RedeemService.Redeem(input);
            ProfileSaveData after =
                context.SaveService.GetCurrentProfileSnapshot();

            Assert.That(result.Status, Is.EqualTo(RedeemCodeStatus.Success));
            Assert.That(result.Definition.Code, Is.EqualTo("NEAT3"));
            Assert.That(result.Definition.RewardType,
                Is.EqualTo(RedeemRewardType.GachaCurrency));
            Assert.That(result.Definition.Amount, Is.EqualTo(10000L));
            Assert.That(after.Currencies.GachaCurrency,
                Is.EqualTo(before.Currencies.GachaCurrency + 10000L));
            Assert.That(result.CurrencyBefore,
                Is.EqualTo(before.Currencies.GachaCurrency));
            Assert.That(result.CurrencyAfter,
                Is.EqualTo(after.Currencies.GachaCurrency));
            Assert.That(result.TransactionResult.IsSuccess, Is.True);
            Assert.That(context.Repository.MainText, Is.Not.Empty);
            AssertProfileDataExceptCurrencyAndTimestampUnchanged(
                before,
                after);
        }

        [TestCase(null, RedeemCodeStatus.EmptyCode)]
        [TestCase("", RedeemCodeStatus.EmptyCode)]
        [TestCase("   ", RedeemCodeStatus.EmptyCode)]
        [TestCase("NEAT4", RedeemCodeStatus.InvalidCode)]
        [TestCase("unknown", RedeemCodeStatus.InvalidCode)]
        public void InvalidInput_GrantsNothingAndDoesNotWrite(
            string input,
            RedeemCodeStatus expectedStatus)
        {
            TestContext context = CreateContext();
            long before = context.SaveService.GetCurrentProfileSnapshot()
                .Currencies.GachaCurrency;
            context.Repository.Calls.Clear();

            RedeemCodeResult result = context.RedeemService.Redeem(input);

            Assert.That(result.Status, Is.EqualTo(expectedStatus));
            Assert.That(context.SaveService.GetCurrentProfileSnapshot()
                .Currencies.GachaCurrency, Is.EqualTo(before));
            Assert.That(context.Repository.Calls,
                Does.Not.Contain(nameof(FakeSaveRepository.WriteTemp)));
        }

        [Test]
        public void SameCodeTwice_GrantsTwentyThousandWithoutUsageHistory()
        {
            TestContext context = CreateContext();
            ProfileSaveData before =
                context.SaveService.GetCurrentProfileSnapshot();

            Assert.That(context.RedeemService.Redeem("NEAT3").IsSuccess,
                Is.True);
            Assert.That(context.RedeemService.Redeem("NEAT3").IsSuccess,
                Is.True);
            ProfileSaveData after =
                context.SaveService.GetCurrentProfileSnapshot();

            Assert.That(after.Currencies.GachaCurrency,
                Is.EqualTo(before.Currencies.GachaCurrency + 20000L));
            Assert.That(typeof(ProfileSaveData).GetProperty("RedeemCodes"),
                Is.Null);
            Assert.That(typeof(CurrencySaveData).GetProperty("Gold"),
                Is.Null);
        }

        [Test]
        public void SaveFailure_PreservesAuthoritativeCurrency()
        {
            TestContext context = CreateContext();
            long before = context.SaveService.GetCurrentProfileSnapshot()
                .Currencies.GachaCurrency;
            context.Repository.FailWriteTemp = true;

            RedeemCodeResult result =
                context.RedeemService.Redeem("NEAT3");

            Assert.That(result.Status,
                Is.EqualTo(RedeemCodeStatus.PersistenceFailed));
            Assert.That(result.TransactionResult.Status,
                Is.EqualTo(SaveTransactionStatus.SaveFailed));
            Assert.That(context.SaveService.GetCurrentProfileSnapshot()
                .Currencies.GachaCurrency, Is.EqualTo(before));
        }

        [Test]
        public void Overflow_IsRejectedWithoutChangingCurrency()
        {
            TestContext context = CreateContext();
            Assert.That(context.SaveService.ExecuteTransaction(profile =>
            {
                profile.Currencies.GachaCurrency = long.MaxValue;
            }).IsSuccess, Is.True);

            RedeemCodeResult result =
                context.RedeemService.Redeem("NEAT3");

            Assert.That(result.Status,
                Is.EqualTo(RedeemCodeStatus.PersistenceFailed));
            Assert.That(result.TransactionResult.Status,
                Is.EqualTo(SaveTransactionStatus.MutationThrewException));
            Assert.That(context.SaveService.GetCurrentProfileSnapshot()
                .Currencies.GachaCurrency, Is.EqualTo(long.MaxValue));
        }

        [Test]
        public void PersistedReward_ReloadsFromMainSave()
        {
            TestContext context = CreateContext();
            long before = context.SaveService.GetCurrentProfileSnapshot()
                .Currencies.GachaCurrency;
            Assert.That(context.RedeemService.Redeem("NEAT3").IsSuccess,
                Is.True);
            var reloaded = SaveServiceTestFactory.Create(
                context.Repository,
                new FixedUnixTimeProvider(300L));

            SaveLoadResult load = reloaded.LoadOrCreate("unused");

            Assert.That(load.IsSuccess, Is.True);
            Assert.That(reloaded.GetCurrentProfileSnapshot()
                .Currencies.GachaCurrency,
                Is.EqualTo(before + 10000L));
        }

        private static TestContext CreateContext()
        {
            var repository = new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.ValidJson()
            };
            SaveService saveService = SaveServiceTestFactory.Create(
                repository,
                new FixedUnixTimeProvider(100L, 200L));
            Assert.That(saveService.LoadOrCreate("unused").IsSuccess, Is.True);
            return new TestContext(
                repository,
                saveService,
                new RedeemCodeService(saveService));
        }

        private static void AssertProfileDataExceptCurrencyAndTimestampUnchanged(
            ProfileSaveData before,
            ProfileSaveData after)
        {
            Assert.That(after.SaveVersion, Is.EqualTo(before.SaveVersion));
            Assert.That(after.ProfileId, Is.EqualTo(before.ProfileId));
            Assert.That(after.CreatedAtUtcUnixSeconds,
                Is.EqualTo(before.CreatedAtUtcUnixSeconds));
            Assert.That(after.Currencies.BattleRecords,
                Is.EqualTo(before.Currencies.BattleRecords));
            Assert.That(after.Currencies.HeroTokens,
                Is.EqualTo(before.Currencies.HeroTokens));
            Assert.That(after.Currencies.RelicTokens,
                Is.EqualTo(before.Currencies.RelicTokens));
            Assert.That(after.Characters.Count,
                Is.EqualTo(before.Characters.Count));
            Assert.That(after.RelicInstances.Count,
                Is.EqualTo(before.RelicInstances.Count));
            Assert.That(after.GachaStates.Count,
                Is.EqualTo(before.GachaStates.Count));
            Assert.That(after.BossRecords.Count,
                Is.EqualTo(before.BossRecords.Count));
        }

        private sealed class TestContext
        {
            public TestContext(
                FakeSaveRepository repository,
                SaveService saveService,
                RedeemCodeService redeemService)
            {
                Repository = repository;
                SaveService = saveService;
                RedeemService = redeemService;
            }

            public FakeSaveRepository Repository { get; }
            public SaveService SaveService { get; }
            public RedeemCodeService RedeemService { get; }
        }
    }
}
