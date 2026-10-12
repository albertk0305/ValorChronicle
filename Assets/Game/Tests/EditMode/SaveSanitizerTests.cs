using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ValorChronicle.Save.Copying;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;
using ValorChronicle.Save.Sanitization;
using ValorChronicle.Save.Serialization;
using ValorChronicle.Save.Validation;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class SaveSanitizerTests
    {
        private SaveSanitizer sanitizer;

        [SetUp]
        public void SetUp()
        {
            sanitizer = new SaveSanitizer(new SaveDataCloner(), new SaveStructuralValidator(), new SaveReferenceValidator());
        }

        [Test]
        public void SanitizeStructure_AppliesNumericAndBossAllowlistWithoutChangingSource()
        {
            ProfileSaveData profile = SaveValidationTestSupport.CreateValidProfile();
            profile.CreatedAtUtcUnixSeconds = -1;
            profile.LastSavedAtUtcUnixSeconds = -2;
            profile.Currencies.GachaCurrency = -1;
            profile.Characters.Add(new CharacterSaveData { CharacterId = "hero", Level = 101, Awakening = -1 });
            profile.GachaStates.Add(new GachaStateSaveData { GachaId = "g", PityCount = 5, TotalPullCount = 3 });
            profile.BossRecords.Add(new BossRecordSaveData { BossId = "b", DifficultyId = "d", HighScore = -1, BestDefeatTurn = 4, BestRemainingTurns = -2, ClaimedFirstRewardGradeIds = new List<string> { "", "gold", "gold" } });
            string before = new NewtonsoftJsonSaveSerializer().Serialize(profile);

            SaveSanitizationResult result = sanitizer.SanitizeStructure(profile);

            Assert.That(result.Profile.CreatedAtUtcUnixSeconds, Is.Zero);
            Assert.That(result.Profile.LastSavedAtUtcUnixSeconds, Is.Zero);
            Assert.That(result.Profile.Currencies.GachaCurrency, Is.Zero);
            Assert.That(result.Profile.Characters[0].Level, Is.EqualTo(100));
            Assert.That(result.Profile.Characters[0].Awakening, Is.Zero);
            Assert.That(result.Profile.GachaStates[0].PityCount, Is.EqualTo(3));
            Assert.That(result.Profile.BossRecords[0].HighScore, Is.Zero);
            Assert.That(result.Profile.BossRecords[0].BestDefeatTurn, Is.Zero);
            Assert.That(result.Profile.BossRecords[0].BestRemainingTurns, Is.Zero);
            Assert.That(result.Profile.BossRecords[0].HasAttempted, Is.True);
            Assert.That(result.Profile.BossRecords[0].ClaimedFirstRewardGradeIds, Is.EqualTo(new[] { "gold" }));
            Assert.That(result.Report.Issues.Where(issue => issue.CanAutoCorrect).All(issue => issue.WasAutoCorrected), Is.True);
            Assert.That(new NewtonsoftJsonSaveSerializer().Serialize(profile), Is.EqualTo(before));
        }

        [Test]
        public void SanitizeStructure_PreservesFirstRelicEquipsAndPresetIds()
        {
            ProfileSaveData profile = SaveValidationTestSupport.CreateValidProfile();
            profile.Party.Presets[0].PresetId = "";
            profile.Party.Presets[1].PresetId = "party_recovered_1";
            profile.Party.Presets[2].PresetId = "party_recovered_1";
            profile.RelicInstances.Add(new RelicInstanceSaveData
            {
                InstanceId = "one",
                RelicDefinitionId = "sword"
            });
            profile.RelicInstances.Add(new RelicInstanceSaveData
            {
                InstanceId = "two",
                RelicDefinitionId = "sword"
            });
            profile.RelicInstances.Add(new RelicInstanceSaveData
            {
                InstanceId = "three",
                RelicDefinitionId = "sword"
            });
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = "hero_a",
                Level = 1,
                EquippedRelicInstanceIds = new List<string>
                {
                    "one", "three", "missing", ""
                }
            });
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = "hero_b",
                Level = 1,
                EquippedRelicInstanceIds = new List<string>
                {
                    "", "two", "one", ""
                }
            });

            SaveSanitizationResult first = sanitizer.SanitizeStructure(profile);
            SaveSanitizationResult second = sanitizer.SanitizeStructure(first.Profile);

            Assert.That(
                first.Profile.Party.Presets.Select(item => item.PresetId),
                Is.EqualTo(new[]
                {
                    "party_recovered_2",
                    "party_recovered_1",
                    "party_recovered_3",
                    "party_4",
                    "party_5"
                }));
            Assert.That(
                first.Profile.Characters[0].EquippedRelicInstanceIds,
                Is.EqualTo(new[] { "one", "", "", "" }));
            Assert.That(
                first.Profile.Characters[1].EquippedRelicInstanceIds,
                Is.EqualTo(new[] { "", "two", "", "" }));
            Assert.That(first.Profile.RelicInstances, Has.Count.EqualTo(3));
            Assert.That(new NewtonsoftJsonSaveSerializer().Serialize(second.Profile), Is.EqualTo(new NewtonsoftJsonSaveSerializer().Serialize(first.Profile)));
            Assert.That(second.WasModified, Is.False);
        }

        [Test]
        public void SanitizeStructure_DoesNotDeleteFatalDuplicateData()
        {
            ProfileSaveData profile = SaveValidationTestSupport.CreateValidProfile();
            profile.Characters.Add(new CharacterSaveData { CharacterId = "hero", Level = 1 });
            profile.Characters.Add(new CharacterSaveData { CharacterId = "hero", Level = 1 });
            profile.RelicInstances.Add(new RelicInstanceSaveData { InstanceId = "r", RelicDefinitionId = "s", EquippedCharacterId = "", EquippedSlotIndex = -1 });
            profile.RelicInstances.Add(new RelicInstanceSaveData { InstanceId = "r", RelicDefinitionId = "s", EquippedCharacterId = "", EquippedSlotIndex = -1 });

            SaveSanitizationResult result = sanitizer.SanitizeStructure(profile);

            Assert.That(result.Profile.Characters, Has.Count.EqualTo(2));
            Assert.That(result.Profile.RelicInstances, Has.Count.EqualTo(2));
            Assert.That(result.Report.Contains(SaveValidationCode.DuplicateCharacterId), Is.True);
            Assert.That(result.Report.Issues.Single(issue => issue.Code == SaveValidationCode.DuplicateCharacterId).WasAutoCorrected, Is.False);
        }

        [Test]
        public void SanitizeReferences_ClearsOnlyBrokenConnections()
        {
            ProfileSaveData profile = SaveReferenceValidatorTests.CreateReferencedProfile();
            profile.Party.Presets[0].CharacterSlotIds[1] = "ghost";
            var catalog = new FakeSaveContentCatalog { CharacterLookup = id => id == "ghost" ? SaveContentLookupResult.Missing : SaveContentLookupResult.Exists };

            SaveSanitizationResult result = sanitizer.SanitizeReferences(profile, catalog);

            Assert.That(result.Profile.Party.Presets[0].CharacterSlotIds[1], Is.Empty);
            Assert.That(result.Report.Issues.Where(issue => issue.CanAutoCorrect).All(issue => issue.WasAutoCorrected), Is.True);
        }
    }
}
