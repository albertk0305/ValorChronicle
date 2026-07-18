using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Serialization;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class SaveSerializerTests
    {
        private NewtonsoftJsonSaveSerializer serializer;

        [SetUp]
        public void SetUp()
        {
            serializer = new NewtonsoftJsonSaveSerializer();
        }

        [Test]
        public void RoundTrip_PreservesProfileDataAndCollectionOrder()
        {
            ProfileSaveData source = CreatePopulatedProfile();

            string json = serializer.Serialize(source);
            ProfileSaveData result = serializer.Deserialize(json);

            Assert.That(result.SaveVersion, Is.EqualTo(1));
            Assert.That(result.ProfileId, Is.EqualTo("profile_round_trip"));
            Assert.That(result.CreatedAtUtcUnixSeconds, Is.EqualTo(100));
            Assert.That(result.LastSavedAtUtcUnixSeconds, Is.EqualTo(200));
            Assert.That(result.Currencies.GachaCurrency, Is.EqualTo(11));
            Assert.That(result.Currencies.BattleRecords, Is.EqualTo(22));
            Assert.That(result.Currencies.HeroTokens, Is.EqualTo(33));
            Assert.That(result.Currencies.RelicTokens, Is.EqualTo(44));

            Assert.That(result.Characters, Has.Count.EqualTo(1));
            Assert.That(result.Characters[0].CharacterId, Is.EqualTo("character_test"));
            Assert.That(result.Characters[0].Level, Is.EqualTo(12));
            Assert.That(result.Characters[0].Awakening, Is.EqualTo(2));
            Assert.That(result.Characters[0].IsFavorite, Is.True);
            Assert.That(result.Characters[0].IsNew, Is.False);

            Assert.That(result.RelicInstances, Has.Count.EqualTo(1));
            Assert.That(result.RelicInstances[0].InstanceId, Is.EqualTo("relic_instance_1"));
            Assert.That(result.RelicInstances[0].RelicDefinitionId, Is.EqualTo("relic_test"));
            Assert.That(result.RelicInstances[0].Awakening, Is.EqualTo(3));
            Assert.That(result.RelicInstances[0].EquippedCharacterId, Is.EqualTo("character_test"));
            Assert.That(result.RelicInstances[0].EquippedSlotIndex, Is.EqualTo(1));
            Assert.That(result.RelicInstances[0].IsLocked, Is.True);
            Assert.That(result.RelicInstances[0].IsNew, Is.False);

            CollectionAssert.AreEqual(
                new[] { "character_first", "", "character_third", "", "" },
                result.Party.Presets[0].CharacterSlotIds);
            Assert.That(result.Party.LastBossId, Is.EqualTo("boss_test"));
            Assert.That(result.Party.LastDifficultyId, Is.EqualTo("difficulty_normal"));

            Assert.That(result.GachaStates[0].GachaId, Is.EqualTo("gacha_test"));
            Assert.That(result.GachaStates[0].PityCount, Is.EqualTo(7));
            Assert.That(result.GachaStates[0].TotalPullCount, Is.EqualTo(99));

            Assert.That(result.BossRecords[0].BossId, Is.EqualTo("boss_test"));
            Assert.That(result.BossRecords[0].DifficultyId, Is.EqualTo("difficulty_normal"));
            Assert.That(result.BossRecords[0].HasAttempted, Is.True);
            Assert.That(result.BossRecords[0].IsCleared, Is.True);
            Assert.That(result.BossRecords[0].HighScore, Is.EqualTo(123456));
            Assert.That(result.BossRecords[0].HighestGradeId, Is.EqualTo("grade_b"));
            Assert.That(result.BossRecords[0].BestDefeatTurn, Is.EqualTo(10));
            Assert.That(result.BossRecords[0].BestRemainingTurns, Is.EqualTo(15));
            CollectionAssert.AreEqual(
                new[] { "grade_c", "grade_b" },
                result.BossRecords[0].ClaimedFirstRewardGradeIds);
            CollectionAssert.AreEqual(new[] { "feature_one" }, result.UnlockedContentIds);
            CollectionAssert.AreEqual(new[] { "tutorial_intro" }, result.CompletedTutorialIds);
        }

        [Test]
        public void Serialize_UsesIndentedJsonWithoutTypeMetadata()
        {
            string json = serializer.Serialize(CreatePopulatedProfile());

            Assert.That(json, Does.Contain("\n"));
            Assert.That(json, Does.Contain("  \"SaveVersion\""));
            Assert.That(json, Does.Not.Contain("$type"));
            Assert.That(json, Does.Not.Contain(typeof(ProfileSaveData).FullName));
        }

        [Test]
        public void Deserialize_IgnoresUnknownFields()
        {
            const string json = "{\"SaveVersion\":1,\"ProfileId\":\"known\",\"FutureField\":42}";

            ProfileSaveData result = serializer.Deserialize(json);

            Assert.That(result.SaveVersion, Is.EqualTo(1));
            Assert.That(result.ProfileId, Is.EqualTo("known"));
        }

        [Test]
        public void Deserialize_DoesNotInterpretTypeMetadata()
        {
            const string json = "{\"$type\":\"System.Version, System.Private.CoreLib\",\"SaveVersion\":1,\"ProfileId\":\"safe\"}";

            ProfileSaveData result = serializer.Deserialize(json);

            Assert.That(result.GetType(), Is.EqualTo(typeof(ProfileSaveData)));
            Assert.That(result.ProfileId, Is.EqualTo("safe"));
        }

        [Test]
        public void Deserialize_DoesNotNormalizeMissingCollections()
        {
            ProfileSaveData result = serializer.Deserialize("{\"SaveVersion\":1}");

            Assert.That(result.Characters, Is.Null);
            Assert.That(result.Party, Is.Null);
        }

        [Test]
        public void Serialize_RejectsNullData()
        {
            Assert.Throws<ArgumentNullException>(() => serializer.Serialize(null));
        }

        [TestCase(null)]
        public void Deserialize_RejectsNullJson(string json)
        {
            Assert.Throws<ArgumentNullException>(() => serializer.Deserialize(json));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Deserialize_RejectsEmptyJson(string json)
        {
            Assert.Throws<ArgumentException>(() => serializer.Deserialize(json));
        }

        [Test]
        public void Deserialize_RejectsMalformedJson()
        {
            Exception exception = Assert.Catch(
                () => serializer.Deserialize("{not valid json}"));

            Assert.That(
                exception.GetType().FullName,
                Is.EqualTo("Newtonsoft.Json.JsonReaderException"));
        }

        [Test]
        public void Deserialize_RejectsNullRoot()
        {
            Exception exception = Assert.Catch(() => serializer.Deserialize("null"));

            Assert.That(
                exception.GetType().FullName,
                Is.EqualTo("Newtonsoft.Json.JsonSerializationException"));
        }

        private static ProfileSaveData CreatePopulatedProfile()
        {
            return new ProfileSaveData
            {
                SaveVersion = 1,
                ProfileId = "profile_round_trip",
                CreatedAtUtcUnixSeconds = 100,
                LastSavedAtUtcUnixSeconds = 200,
                Currencies = new CurrencySaveData
                {
                    GachaCurrency = 11,
                    BattleRecords = 22,
                    HeroTokens = 33,
                    RelicTokens = 44
                },
                Characters = new List<CharacterSaveData>
                {
                    new CharacterSaveData
                    {
                        CharacterId = "character_test",
                        Level = 12,
                        Awakening = 2,
                        IsFavorite = true,
                        IsNew = false
                    }
                },
                RelicInstances = new List<RelicInstanceSaveData>
                {
                    new RelicInstanceSaveData
                    {
                        InstanceId = "relic_instance_1",
                        RelicDefinitionId = "relic_test",
                        Awakening = 3,
                        EquippedCharacterId = "character_test",
                        EquippedSlotIndex = 1,
                        IsLocked = true,
                        IsNew = false
                    }
                },
                Party = new PartySaveData
                {
                    ActivePresetIndex = 0,
                    Presets = new List<PartyPresetSaveData>
                    {
                        new PartyPresetSaveData
                        {
                            PresetId = "party_default",
                            CharacterSlotIds = new List<string>
                            {
                                "character_first", "", "character_third", "", ""
                            }
                        }
                    },
                    LastBossId = "boss_test",
                    LastDifficultyId = "difficulty_normal"
                },
                GachaStates = new List<GachaStateSaveData>
                {
                    new GachaStateSaveData
                    {
                        GachaId = "gacha_test",
                        PityCount = 7,
                        TotalPullCount = 99
                    }
                },
                BossRecords = new List<BossRecordSaveData>
                {
                    new BossRecordSaveData
                    {
                        BossId = "boss_test",
                        DifficultyId = "difficulty_normal",
                        HasAttempted = true,
                        IsCleared = true,
                        HighScore = 123456,
                        HighestGradeId = "grade_b",
                        BestDefeatTurn = 10,
                        BestRemainingTurns = 15,
                        ClaimedFirstRewardGradeIds = new List<string> { "grade_c", "grade_b" }
                    }
                },
                UnlockedContentIds = new List<string> { "feature_one" },
                CompletedTutorialIds = new List<string> { "tutorial_intro" }
            };
        }
    }
}
