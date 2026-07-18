using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ValorChronicle.Save.Copying;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Normalization;
using ValorChronicle.Save.Rules;
using ValorChronicle.Save.Serialization;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class SaveNormalizerTests
    {
        private SaveNormalizer normalizer;

        [SetUp]
        public void SetUp()
        {
            normalizer = new SaveNormalizer(new SaveDataCloner());
        }

        [Test]
        public void NormalizeCopy_CreatesAllMissingTopLevelStructures()
        {
            var source = new ProfileSaveData();

            ProfileSaveData result = normalizer.NormalizeCopy(source);

            Assert.That(result.ProfileId, Is.Empty);
            Assert.That(result.Currencies, Is.Not.Null);
            Assert.That(result.Characters, Is.Empty);
            Assert.That(result.RelicInstances, Is.Empty);
            Assert.That(result.Party, Is.Not.Null);
            Assert.That(result.Party.Presets, Has.Count.EqualTo(1));
            Assert.That(result.Party.Presets[0].PresetId, Is.EqualTo(SaveRules.DefaultPartyPresetId));
            Assert.That(result.Party.Presets[0].CharacterSlotIds, Has.Count.EqualTo(SaveRules.PartySlotCount));
            Assert.That(result.GachaStates, Is.Empty);
            Assert.That(result.BossRecords, Is.Empty);
            Assert.That(result.UnlockedContentIds, Is.Empty);
            Assert.That(result.CompletedTutorialIds, Is.Empty);
        }

        [Test]
        public void NormalizeCopy_ReplacesNullDtoEntriesAndNullStrings()
        {
            var source = new ProfileSaveData
            {
                Characters = new List<CharacterSaveData> { null },
                RelicInstances = new List<RelicInstanceSaveData> { null },
                Party = new PartySaveData
                {
                    Presets = new List<PartyPresetSaveData>
                    {
                        null,
                        new PartyPresetSaveData
                        {
                            PresetId = null,
                            CharacterSlotIds = new List<string> { null }
                        }
                    },
                    LastBossId = null,
                    LastDifficultyId = null
                },
                GachaStates = new List<GachaStateSaveData> { null },
                BossRecords = new List<BossRecordSaveData> { null },
                UnlockedContentIds = new List<string> { null },
                CompletedTutorialIds = new List<string> { null }
            };

            ProfileSaveData result = normalizer.NormalizeCopy(source);

            Assert.That(result.Characters[0].CharacterId, Is.Empty);
            Assert.That(result.RelicInstances[0].InstanceId, Is.Empty);
            Assert.That(result.RelicInstances[0].RelicDefinitionId, Is.Empty);
            Assert.That(result.RelicInstances[0].EquippedCharacterId, Is.Empty);
            Assert.That(
                result.RelicInstances[0].EquippedSlotIndex,
                Is.EqualTo(SaveRules.UnequippedRelicSlotIndex));
            Assert.That(result.Party.Presets[0].PresetId, Is.Empty);
            Assert.That(result.Party.Presets[0].CharacterSlotIds.All(value => value == string.Empty), Is.True);
            Assert.That(result.Party.Presets[1].PresetId, Is.Empty);
            Assert.That(result.Party.Presets[1].CharacterSlotIds[0], Is.Empty);
            Assert.That(result.Party.LastBossId, Is.Empty);
            Assert.That(result.Party.LastDifficultyId, Is.Empty);
            Assert.That(result.GachaStates[0].GachaId, Is.Empty);
            Assert.That(result.BossRecords[0].BossId, Is.Empty);
            Assert.That(result.BossRecords[0].DifficultyId, Is.Empty);
            Assert.That(result.BossRecords[0].HighestGradeId, Is.Empty);
            Assert.That(result.BossRecords[0].ClaimedFirstRewardGradeIds, Is.Empty);
            Assert.That(result.UnlockedContentIds[0], Is.Empty);
            Assert.That(result.CompletedTutorialIds[0], Is.Empty);
        }

        [Test]
        public void NormalizeCopy_CreatesDefaultPresetWhenPresetsIsNull()
        {
            var source = CreateMinimalProfile();
            source.Party.Presets = null;

            ProfileSaveData result = normalizer.NormalizeCopy(source);

            AssertDefaultPreset(result.Party);
        }

        [Test]
        public void NormalizeCopy_CreatesDefaultPresetWhenPresetsIsEmpty()
        {
            var source = CreateMinimalProfile();
            source.Party.Presets = new List<PartyPresetSaveData>();

            ProfileSaveData result = normalizer.NormalizeCopy(source);

            AssertDefaultPreset(result.Party);
        }

        [TestCase(0)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void NormalizeCopy_ProducesExactlyFivePartySlots(int originalCount)
        {
            var slots = new List<string>();

            for (int i = 0; i < originalCount; i++)
            {
                slots.Add($"character_{i}");
            }

            var source = CreateMinimalProfile();
            source.Party.Presets[0].CharacterSlotIds = slots;

            ProfileSaveData result = normalizer.NormalizeCopy(source);

            Assert.That(result.Party.Presets[0].CharacterSlotIds, Has.Count.EqualTo(SaveRules.PartySlotCount));

            int preservedCount = originalCount < SaveRules.PartySlotCount
                ? originalCount
                : SaveRules.PartySlotCount;

            for (int i = 0; i < preservedCount; i++)
            {
                Assert.That(result.Party.Presets[0].CharacterSlotIds[i], Is.EqualTo($"character_{i}"));
            }

            for (int i = preservedCount; i < SaveRules.PartySlotCount; i++)
            {
                Assert.That(result.Party.Presets[0].CharacterSlotIds[i], Is.Empty);
            }
        }

        [TestCase(-1)]
        [TestCase(1)]
        [TestCase(100)]
        public void NormalizeCopy_ResetsInvalidActivePresetIndex(int activePresetIndex)
        {
            var source = CreateMinimalProfile();
            source.Party.ActivePresetIndex = activePresetIndex;

            ProfileSaveData result = normalizer.NormalizeCopy(source);

            Assert.That(result.Party.ActivePresetIndex, Is.Zero);
        }

        [Test]
        public void NormalizeCopy_DoesNotChangeSemanticNumericValues()
        {
            var source = CreateMinimalProfile();
            source.SaveVersion = -2;
            source.CreatedAtUtcUnixSeconds = -10;
            source.LastSavedAtUtcUnixSeconds = -20;
            source.Currencies = new CurrencySaveData
            {
                GachaCurrency = -1,
                BattleRecords = -2,
                HeroTokens = -3,
                RelicTokens = -4
            };
            source.Characters.Add(new CharacterSaveData { Level = -5, Awakening = 99 });
            source.RelicInstances.Add(new RelicInstanceSaveData
            {
                Awakening = -6,
                EquippedCharacterId = null,
                EquippedSlotIndex = 77
            });
            source.GachaStates.Add(new GachaStateSaveData { PityCount = -7, TotalPullCount = -8 });
            source.BossRecords.Add(new BossRecordSaveData
            {
                HighScore = -9,
                BestDefeatTurn = -10,
                BestRemainingTurns = -11
            });

            ProfileSaveData result = normalizer.NormalizeCopy(source);

            Assert.That(result.SaveVersion, Is.EqualTo(-2));
            Assert.That(result.CreatedAtUtcUnixSeconds, Is.EqualTo(-10));
            Assert.That(result.LastSavedAtUtcUnixSeconds, Is.EqualTo(-20));
            Assert.That(result.Currencies.GachaCurrency, Is.EqualTo(-1));
            Assert.That(result.Currencies.BattleRecords, Is.EqualTo(-2));
            Assert.That(result.Currencies.HeroTokens, Is.EqualTo(-3));
            Assert.That(result.Currencies.RelicTokens, Is.EqualTo(-4));
            Assert.That(result.Characters[0].Level, Is.EqualTo(-5));
            Assert.That(result.Characters[0].Awakening, Is.EqualTo(99));
            Assert.That(result.RelicInstances[0].Awakening, Is.EqualTo(-6));
            Assert.That(result.RelicInstances[0].EquippedSlotIndex, Is.EqualTo(77));
            Assert.That(result.GachaStates[0].PityCount, Is.EqualTo(-7));
            Assert.That(result.GachaStates[0].TotalPullCount, Is.EqualTo(-8));
            Assert.That(result.BossRecords[0].HighScore, Is.EqualTo(-9));
            Assert.That(result.BossRecords[0].BestDefeatTurn, Is.EqualTo(-10));
            Assert.That(result.BossRecords[0].BestRemainingTurns, Is.EqualTo(-11));
        }

        [Test]
        public void NormalizeCopy_DoesNotChangeSource()
        {
            var source = new ProfileSaveData
            {
                ProfileId = null,
                Party = new PartySaveData
                {
                    ActivePresetIndex = 5,
                    Presets = new List<PartyPresetSaveData>
                    {
                        new PartyPresetSaveData
                        {
                            CharacterSlotIds = new List<string> { null, "a", "b", "c", "d", "e" }
                        }
                    }
                }
            };

            ProfileSaveData result = normalizer.NormalizeCopy(source);

            Assert.That(result, Is.Not.SameAs(source));
            Assert.That(source.ProfileId, Is.Null);
            Assert.That(source.Currencies, Is.Null);
            Assert.That(source.Characters, Is.Null);
            Assert.That(source.Party.ActivePresetIndex, Is.EqualTo(5));
            Assert.That(source.Party.Presets[0].PresetId, Is.Null);
            Assert.That(source.Party.Presets[0].CharacterSlotIds, Has.Count.EqualTo(6));
            Assert.That(source.Party.Presets[0].CharacterSlotIds[0], Is.Null);
        }

        [Test]
        public void NormalizeCopy_IsIdempotent()
        {
            var source = new ProfileSaveData
            {
                Characters = new List<CharacterSaveData> { null },
                Party = new PartySaveData { Presets = null },
                UnlockedContentIds = new List<string> { null }
            };
            var serializer = new NewtonsoftJsonSaveSerializer();

            ProfileSaveData first = normalizer.NormalizeCopy(source);
            ProfileSaveData second = normalizer.NormalizeCopy(first);

            Assert.That(serializer.Serialize(second), Is.EqualTo(serializer.Serialize(first)));
            Assert.That(second, Is.Not.SameAs(first));
        }

        private static ProfileSaveData CreateMinimalProfile()
        {
            return new ProfileSaveData
            {
                Currencies = new CurrencySaveData(),
                Characters = new List<CharacterSaveData>(),
                RelicInstances = new List<RelicInstanceSaveData>(),
                Party = new PartySaveData
                {
                    Presets = new List<PartyPresetSaveData>
                    {
                        new PartyPresetSaveData
                        {
                            PresetId = SaveRules.DefaultPartyPresetId,
                            CharacterSlotIds = new List<string>()
                        }
                    }
                },
                GachaStates = new List<GachaStateSaveData>(),
                BossRecords = new List<BossRecordSaveData>(),
                UnlockedContentIds = new List<string>(),
                CompletedTutorialIds = new List<string>()
            };
        }

        private static void AssertDefaultPreset(PartySaveData party)
        {
            Assert.That(party.Presets, Has.Count.EqualTo(1));
            Assert.That(party.Presets[0].PresetId, Is.EqualTo(SaveRules.DefaultPartyPresetId));
            Assert.That(party.Presets[0].CharacterSlotIds, Has.Count.EqualTo(SaveRules.PartySlotCount));
            Assert.That(party.Presets[0].CharacterSlotIds.All(value => value == string.Empty), Is.True);
        }
    }
}
