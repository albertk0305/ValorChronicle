using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Gacha;
using ValorChronicle.Relics.Acquisition;
using ValorChronicle.Rewards;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;
using ValorChronicle.Save.Serialization;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class RewardGrantProfileUpdaterTests
    {
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

        [Test]
        public void TwoCharacterRewards_ProcessFiveToSixThenConversion()
        {
            ProfileSaveData profile = Profile();
            profile.Characters.Add(Character(5));
            var provider = new SequentialIdProvider("unused");
            RewardGrantBatchResult result = Updater(provider).Apply(
                profile,
                new[]
                {
                    CharacterRequest(0),
                    CharacterRequest(1)
                });

            Assert.That(profile.Characters[0].Awakening, Is.EqualTo(6));
            Assert.That(profile.Currencies.HeroTokens, Is.EqualTo(60L));
            Assert.That(result.Grants[0].GrantKind,
                Is.EqualTo(RewardGrantKind
                    .CharacterAwakeningIncreased));
            Assert.That(result.Grants[0].PreviousAwakening, Is.EqualTo(5));
            Assert.That(result.Grants[0].NewAwakening, Is.EqualTo(6));
            Assert.That(result.Grants[1].GrantKind,
                Is.EqualTo(RewardGrantKind
                    .MaxAwakeningCharacterConvertedToHeroTokens));
            Assert.That(result.Grants[1].ContentId,
                Is.EqualTo(CharacterId));
            Assert.That(result.Grants[1].CurrencyGrant.Amount,
                Is.EqualTo(60L));
            Assert.That(provider.CallCount, Is.Zero);
        }

        [Test]
        public void ThreeRelicRewards_ProcessFourToFiveNewThenAwakenNew()
        {
            ProfileSaveData profile = Profile();
            profile.RelicInstances.Add(Relic("old", 4));
            var provider = new SequentialIdProvider("new");

            RewardGrantBatchResult result = Updater(provider).Apply(
                profile,
                new[]
                {
                    RelicRequest(0),
                    RelicRequest(1),
                    RelicRequest(2)
                });

            Assert.That(profile.RelicInstances, Has.Count.EqualTo(2));
            Assert.That(profile.RelicInstances[0].Awakening, Is.EqualTo(5));
            Assert.That(profile.RelicInstances[1].InstanceId,
                Is.EqualTo("new"));
            Assert.That(profile.RelicInstances[1].Awakening, Is.EqualTo(1));
            Assert.That(result.Grants[0].GrantKind,
                Is.EqualTo(RewardGrantKind.RelicAwakeningIncreased));
            Assert.That(result.Grants[0].PreviousAwakening, Is.EqualTo(4));
            Assert.That(result.Grants[0].NewAwakening, Is.EqualTo(5));
            Assert.That(result.Grants[1].GrantKind,
                Is.EqualTo(RewardGrantKind.NewRelicInstanceCreated));
            Assert.That(result.Grants[1].RelicCreatedAfterMaxAwakening,
                Is.True);
            Assert.That(result.Grants[1].RelicInstanceId, Is.EqualTo("new"));
            Assert.That(result.Grants[2].GrantKind,
                Is.EqualTo(RewardGrantKind.RelicAwakeningIncreased));
            Assert.That(result.Grants[2].RelicInstanceId, Is.EqualTo("new"));
            Assert.That(result.Grants[2].PreviousAwakening, Is.Zero);
            Assert.That(result.Grants[2].NewAwakening, Is.EqualTo(1));
        }

        [Test]
        public void CurrencyRewards_AccumulateEveryConfirmedAmount()
        {
            ProfileSaveData profile = Profile();
            profile.Currencies.HeroTokens = 10L;
            profile.Currencies.RelicTokens = 20L;
            profile.Currencies.BattleRecords = 30L;
            RewardGrantRequest[] requests =
            {
                CurrencyRequest(0, GachaRewardKind.HeroTokens, 1),
                CurrencyRequest(1, GachaRewardKind.HeroTokens, 3),
                CurrencyRequest(2, GachaRewardKind.RelicTokens, 1),
                CurrencyRequest(3, GachaRewardKind.RelicTokens, 3),
                CurrencyRequest(4, GachaRewardKind.BattleRecords, 1000),
                CurrencyRequest(5, GachaRewardKind.BattleRecords, 3000),
                CurrencyRequest(6, GachaRewardKind.BattleRecords, 6000),
                CurrencyRequest(7, GachaRewardKind.BattleRecords, 12000)
            };

            RewardGrantBatchResult result = Updater().Apply(
                profile,
                requests);

            Assert.That(profile.Currencies.HeroTokens, Is.EqualTo(14L));
            Assert.That(profile.Currencies.RelicTokens, Is.EqualTo(24L));
            Assert.That(profile.Currencies.BattleRecords,
                Is.EqualTo(22030L));
            Assert.That(result.Grants, Has.Count.EqualTo(8));
            Assert.That(result.Grants.All(grant =>
                grant.GrantKind == RewardGrantKind.CurrencyGranted),
                Is.True);
            Assert.That(result.Grants.Select(grant => grant.Index),
                Is.EqualTo(Enumerable.Range(0, 8)));
        }

        [Test]
        public void ExplicitRequests_CreateNewCharacterAndRelic()
        {
            ProfileSaveData profile = Profile();
            RewardGrantBatchResult result = Updater(
                new SequentialIdProvider("instance_new")).Apply(
                    profile,
                    new[]
                    {
                        CharacterRequest(0),
                        RelicRequest(1)
                    });

            Assert.That(result.Grants[0].GrantKind,
                Is.EqualTo(RewardGrantKind.NewCharacter));
            Assert.That(profile.Characters[0].Level, Is.EqualTo(1));
            Assert.That(profile.Characters[0].Awakening, Is.Zero);
            Assert.That(result.Grants[1].GrantKind,
                Is.EqualTo(RewardGrantKind.NewRelicInstanceCreated));
            Assert.That(result.Grants[1].RelicCreatedAfterMaxAwakening,
                Is.False);
            Assert.That(profile.RelicInstances[0].InstanceId,
                Is.EqualTo("instance_new"));
        }

        [Test]
        public void GachaCalculationAdapter_PreservesSourcePullInformation()
        {
            Assert.That(database.TryGetGacha(
                "gacha_character_standard",
                out GachaDefinition definition), Is.True);
            var calculator = new GachaCalculator(
                database,
                new SeededRandomSource(125));
            var state = new GachaStateSaveData
            {
                GachaId = definition.Id,
                PityCount = 49,
                TotalPullCount = 49
            };
            GachaCalculationResult calculation =
                calculator.GenerateSingle(definition.Id, state);
            ProfileSaveData profile = Profile();

            RewardGrantBatchResult result = Updater().Apply(
                profile,
                calculation);

            Assert.That(result.Grants, Has.Count.EqualTo(1));
            Assert.That(result.Grants[0].SourcePull,
                Is.SameAs(calculation.Pulls[0]));
            Assert.That(result.Grants[0].SourcePull.WasGuaranteedByPity,
                Is.True);
            Assert.That(result.Grants[0].OriginalRewardKind,
                Is.EqualTo(GachaRewardKind.RandomCharacter));
            Assert.That(state.PityCount, Is.EqualTo(49));
            Assert.That(profile.GachaStates, Is.Empty);
        }

        [Test]
        public void BatchFailure_DoesNotCommitEarlierStagedRewards()
        {
            ProfileSaveData profile = Profile();
            string before = new NewtonsoftJsonSaveSerializer()
                .Serialize(profile);

            Assert.Throws<KeyNotFoundException>(() => Updater().Apply(
                profile,
                new[]
                {
                    CurrencyRequest(0, GachaRewardKind.HeroTokens, 3),
                    new RewardGrantRequest(
                        1,
                        GachaRewardKind.RandomCharacter,
                        "character_missing",
                        1)
                }));
            Assert.That(
                new NewtonsoftJsonSaveSerializer().Serialize(profile),
                Is.EqualTo(before));
        }

        [Test]
        public void CurrencyOverflow_IsAtomicForWholeBatch()
        {
            ProfileSaveData profile = Profile();
            profile.Currencies.HeroTokens = long.MaxValue - 1L;
            string before = new NewtonsoftJsonSaveSerializer()
                .Serialize(profile);

            Assert.Throws<OverflowException>(() => Updater().Apply(
                profile,
                new[]
                {
                    CurrencyRequest(0, GachaRewardKind.RelicTokens, 3),
                    CurrencyRequest(1, GachaRewardKind.HeroTokens, 3)
                }));
            Assert.That(
                new NewtonsoftJsonSaveSerializer().Serialize(profile),
                Is.EqualTo(before));
        }

        [TestCase(0L)]
        [TestCase(-1L)]
        public void NonPositiveRewardAmount_IsRejected(long amount)
        {
            ProfileSaveData profile = Profile();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Updater().Apply(
                    profile,
                    new[]
                    {
                        CurrencyRequest(
                            0,
                            GachaRewardKind.HeroTokens,
                            amount)
                    }));
            Assert.That(profile.Currencies.HeroTokens, Is.Zero);
        }

        [Test]
        public void InvalidIdsKindsAndOrder_AreRejectedWithoutMutation()
        {
            ProfileSaveData profile = Profile();
            string before = new NewtonsoftJsonSaveSerializer()
                .Serialize(profile);
            RewardGrantProfileUpdater updater = Updater();

            Assert.Throws<ArgumentException>(() => updater.Apply(
                profile,
                new[] { CharacterRequest(1) }));
            Assert.Throws<ArgumentException>(() => updater.Apply(
                profile,
                new[]
                {
                    new RewardGrantRequest(
                        0,
                        GachaRewardKind.RandomRelic,
                        string.Empty,
                        1)
                }));
            Assert.Throws<KeyNotFoundException>(() => updater.Apply(
                profile,
                new[]
                {
                    new RewardGrantRequest(
                        0,
                        GachaRewardKind.RandomRelic,
                        "relic_missing",
                        1)
                }));
            Assert.Throws<ArgumentOutOfRangeException>(() => updater.Apply(
                profile,
                new[]
                {
                    new RewardGrantRequest(
                        0,
                        (GachaRewardKind)999,
                        string.Empty,
                        1)
                }));
            Assert.That(
                new NewtonsoftJsonSaveSerializer().Serialize(profile),
                Is.EqualTo(before));
        }

        [Test]
        public void CurrencyUpdater_RejectsUnsupportedCurrencyAndOverflow()
        {
            ProfileSaveData profile = Profile();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CurrencyProfileUpdater.Add(
                    profile,
                    (RewardCurrencyKind)999,
                    1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CurrencyProfileUpdater.Add(
                    profile,
                    RewardCurrencyKind.HeroTokens,
                    0));
            profile.Currencies.BattleRecords = long.MaxValue;
            Assert.Throws<OverflowException>(() =>
                CurrencyProfileUpdater.Add(
                    profile,
                    RewardCurrencyKind.BattleRecords,
                    1));
            Assert.That(profile.Currencies.BattleRecords,
                Is.EqualTo(long.MaxValue));
        }

        [Test]
        public void RewardApplication_DoesNotMutateDefinitions()
        {
            Assert.That(database.TryGetCharacter(
                CharacterId,
                out var characterDefinition), Is.True);
            Assert.That(database.TryGetRelic(
                RelicId,
                out var relicDefinition), Is.True);
            string characterIdBefore = characterDefinition.Id;
            string relicIdBefore = relicDefinition.Id;

            Updater(new SequentialIdProvider("instance_a")).Apply(
                Profile(),
                new[] { CharacterRequest(0), RelicRequest(1) });

            Assert.That(characterDefinition.Id,
                Is.EqualTo(characterIdBefore));
            Assert.That(relicDefinition.Id, Is.EqualTo(relicIdBefore));
        }

        private RewardGrantProfileUpdater Updater(
            IRelicInstanceIdProvider provider = null)
        {
            return new RewardGrantProfileUpdater(
                database,
                provider ?? new SequentialIdProvider("unused"));
        }

        private static ProfileSaveData Profile()
        {
            return SaveTestDataBuilder.Valid();
        }

        private static CharacterSaveData Character(int awakening)
        {
            return new CharacterSaveData
            {
                CharacterId = CharacterId,
                Level = 10,
                Awakening = awakening
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

        private static RewardGrantRequest CharacterRequest(int index)
        {
            return new RewardGrantRequest(
                index,
                GachaRewardKind.RandomCharacter,
                CharacterId,
                1);
        }

        private static RewardGrantRequest RelicRequest(int index)
        {
            return new RewardGrantRequest(
                index,
                GachaRewardKind.RandomRelic,
                RelicId,
                1);
        }

        private static RewardGrantRequest CurrencyRequest(
            int index,
            GachaRewardKind kind,
            long amount)
        {
            return new RewardGrantRequest(index, kind, string.Empty, amount);
        }

        private sealed class SequentialIdProvider
            : IRelicInstanceIdProvider
        {
            private readonly Queue<string> ids;
            private string lastId;

            public SequentialIdProvider(params string[] ids)
            {
                this.ids = new Queue<string>(ids);
                lastId = ids.Length == 0 ? null : ids[ids.Length - 1];
            }

            public int CallCount { get; private set; }

            public string CreateRelicInstanceId()
            {
                CallCount++;
                if (ids.Count > 0)
                {
                    lastId = ids.Dequeue();
                }

                return lastId;
            }
        }
    }
}
