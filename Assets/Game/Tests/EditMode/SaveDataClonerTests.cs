using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Save.Copying;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Serialization;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class SaveDataClonerTests
    {
        private readonly SaveDataCloner cloner = new SaveDataCloner();

        [Test]
        public void Clone_CopiesEveryFieldAndSeparatesMutableReferences()
        {
            ProfileSaveData source = CreatePopulatedProfile();

            ProfileSaveData copy = cloner.Clone(source);

            var serializer = new NewtonsoftJsonSaveSerializer();
            Assert.That(serializer.Serialize(copy), Is.EqualTo(serializer.Serialize(source)));
            Assert.That(copy, Is.Not.SameAs(source));
            Assert.That(copy.Currencies, Is.Not.SameAs(source.Currencies));
            Assert.That(copy.Characters, Is.Not.SameAs(source.Characters));
            Assert.That(copy.Characters[0], Is.Not.SameAs(source.Characters[0]));
            Assert.That(copy.RelicInstances, Is.Not.SameAs(source.RelicInstances));
            Assert.That(copy.RelicInstances[0], Is.Not.SameAs(source.RelicInstances[0]));
            Assert.That(copy.Party, Is.Not.SameAs(source.Party));
            Assert.That(copy.Party.Presets, Is.Not.SameAs(source.Party.Presets));
            Assert.That(copy.Party.Presets[0], Is.Not.SameAs(source.Party.Presets[0]));
            Assert.That(
                copy.Party.Presets[0].CharacterSlotIds,
                Is.Not.SameAs(source.Party.Presets[0].CharacterSlotIds));
            Assert.That(copy.GachaStates, Is.Not.SameAs(source.GachaStates));
            Assert.That(copy.GachaStates[0], Is.Not.SameAs(source.GachaStates[0]));
            Assert.That(copy.BossRecords, Is.Not.SameAs(source.BossRecords));
            Assert.That(copy.BossRecords[0], Is.Not.SameAs(source.BossRecords[0]));
            Assert.That(
                copy.BossRecords[0].ClaimedFirstRewardGradeIds,
                Is.Not.SameAs(source.BossRecords[0].ClaimedFirstRewardGradeIds));
            Assert.That(copy.UnlockedContentIds, Is.Not.SameAs(source.UnlockedContentIds));
            Assert.That(copy.CompletedTutorialIds, Is.Not.SameAs(source.CompletedTutorialIds));
        }

        [Test]
        public void Clone_PreservesNullStateNullEntriesAndOrder()
        {
            var source = new ProfileSaveData
            {
                Currencies = null,
                Characters = new List<CharacterSaveData>
                {
                    new CharacterSaveData { CharacterId = "first" },
                    null,
                    new CharacterSaveData { CharacterId = "third" }
                },
                RelicInstances = null,
                Party = new PartySaveData
                {
                    Presets = new List<PartyPresetSaveData>
                    {
                        null,
                        new PartyPresetSaveData
                        {
                            CharacterSlotIds = new List<string> { "first", null, "third" }
                        }
                    }
                },
                GachaStates = new List<GachaStateSaveData> { null },
                BossRecords = new List<BossRecordSaveData> { null },
                UnlockedContentIds = new List<string> { "one", null, "three" },
                CompletedTutorialIds = null
            };

            ProfileSaveData copy = cloner.Clone(source);

            Assert.That(copy.Currencies, Is.Null);
            Assert.That(copy.RelicInstances, Is.Null);
            Assert.That(copy.CompletedTutorialIds, Is.Null);
            Assert.That(copy.Characters[0].CharacterId, Is.EqualTo("first"));
            Assert.That(copy.Characters[1], Is.Null);
            Assert.That(copy.Characters[2].CharacterId, Is.EqualTo("third"));
            Assert.That(copy.Party.Presets[0], Is.Null);
            CollectionAssert.AreEqual(
                new string[] { "first", null, "third" },
                copy.Party.Presets[1].CharacterSlotIds);
            Assert.That(copy.GachaStates[0], Is.Null);
            Assert.That(copy.BossRecords[0], Is.Null);
            CollectionAssert.AreEqual(
                new string[] { "one", null, "three" },
                copy.UnlockedContentIds);
        }

        [Test]
        public void Clone_ModifyingCopyDoesNotChangeSource()
        {
            ProfileSaveData source = CreatePopulatedProfile();
            ProfileSaveData copy = cloner.Clone(source);

            copy.ProfileId = "changed";
            copy.Currencies.GachaCurrency = 999;
            copy.Characters[0].Level = 99;
            copy.Party.Presets[0].CharacterSlotIds[0] = "changed_character";
            copy.BossRecords[0].ClaimedFirstRewardGradeIds.Add("grade_s");
            copy.UnlockedContentIds.Clear();

            Assert.That(source.ProfileId, Is.EqualTo("profile_source"));
            Assert.That(source.Currencies.GachaCurrency, Is.EqualTo(10));
            Assert.That(source.Characters[0].Level, Is.EqualTo(20));
            Assert.That(source.Party.Presets[0].CharacterSlotIds[0], Is.EqualTo("character_a"));
            CollectionAssert.AreEqual(
                new[] { "grade_c", "grade_b" },
                source.BossRecords[0].ClaimedFirstRewardGradeIds);
            CollectionAssert.AreEqual(new[] { "content_one", "content_two" }, source.UnlockedContentIds);
        }

        [Test]
        public void Clone_RejectsNullSource()
        {
            Assert.Throws<ArgumentNullException>(() => cloner.Clone(null));
        }

        private static ProfileSaveData CreatePopulatedProfile()
        {
            return new ProfileSaveData
            {
                SaveVersion = 1,
                ProfileId = "profile_source",
                CreatedAtUtcUnixSeconds = 100,
                LastSavedAtUtcUnixSeconds = 200,
                Currencies = new CurrencySaveData
                {
                    GachaCurrency = 10,
                    BattleRecords = 20,
                    HeroTokens = 30,
                    RelicTokens = 40
                },
                Characters = new List<CharacterSaveData>
                {
                    new CharacterSaveData
                    {
                        CharacterId = "character_a",
                        Level = 20,
                        Awakening = 3,
                        IsFavorite = true,
                        IsNew = false
                    }
                },
                RelicInstances = new List<RelicInstanceSaveData>
                {
                    new RelicInstanceSaveData
                    {
                        InstanceId = "relic_instance_a",
                        RelicDefinitionId = "relic_a",
                        Awakening = 2,
                        EquippedCharacterId = "character_a",
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
                                "character_a", "", "", "", ""
                            }
                        }
                    },
                    LastBossId = "boss_a",
                    LastDifficultyId = "difficulty_normal"
                },
                GachaStates = new List<GachaStateSaveData>
                {
                    new GachaStateSaveData
                    {
                        GachaId = "gacha_a",
                        PityCount = 4,
                        TotalPullCount = 50
                    }
                },
                BossRecords = new List<BossRecordSaveData>
                {
                    new BossRecordSaveData
                    {
                        BossId = "boss_a",
                        DifficultyId = "difficulty_normal",
                        HasAttempted = true,
                        IsCleared = true,
                        HighScore = 1234,
                        HighestGradeId = "grade_b",
                        BestDefeatTurn = 8,
                        BestRemainingTurns = 17,
                        ClaimedFirstRewardGradeIds = new List<string> { "grade_c", "grade_b" }
                    }
                },
                UnlockedContentIds = new List<string> { "content_one", "content_two" },
                CompletedTutorialIds = new List<string> { "tutorial_one" }
            };
        }
    }
}
