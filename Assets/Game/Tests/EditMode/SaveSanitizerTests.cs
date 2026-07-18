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
        public void SanitizeStructure_PreservesFirstRelicsAndCreatesDeterministicPresetIds()
        {
            ProfileSaveData profile = SaveValidationTestSupport.CreateValidProfile();
            profile.Party.Presets[0].PresetId = "";
            profile.Party.Presets.Add(new PartyPresetSaveData { PresetId = "party_recovered_1", CharacterSlotIds = EmptySlots() });
            profile.Party.Presets.Add(new PartyPresetSaveData { PresetId = "party_recovered_1", CharacterSlotIds = EmptySlots() });
            profile.RelicInstances.Add(new RelicInstanceSaveData { InstanceId = "one", RelicDefinitionId = "sword", EquippedCharacterId = "hero", EquippedSlotIndex = 0 });
            profile.RelicInstances.Add(new RelicInstanceSaveData { InstanceId = "two", RelicDefinitionId = "shield", EquippedCharacterId = "hero", EquippedSlotIndex = 0 });
            profile.RelicInstances.Add(new RelicInstanceSaveData { InstanceId = "three", RelicDefinitionId = "sword", EquippedCharacterId = "hero", EquippedSlotIndex = 1 });

            SaveSanitizationResult first = sanitizer.SanitizeStructure(profile);
            SaveSanitizationResult second = sanitizer.SanitizeStructure(first.Profile);

            Assert.That(first.Profile.Party.Presets.Select(item => item.PresetId), Is.EqualTo(new[] { "party_recovered_2", "party_recovered_1", "party_recovered_3" }));
            Assert.That(first.Profile.RelicInstances[0].EquippedCharacterId, Is.EqualTo("hero"));
            Assert.That(first.Profile.RelicInstances[1].EquippedCharacterId, Is.Empty);
            Assert.That(first.Profile.RelicInstances[2].EquippedCharacterId, Is.Empty);
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
            profile.RelicInstances[0].EquippedCharacterId = "ghost";
            var catalog = new FakeSaveContentCatalog { CharacterLookup = id => id == "ghost" ? SaveContentLookupResult.Missing : SaveContentLookupResult.Exists };

            SaveSanitizationResult result = sanitizer.SanitizeReferences(profile, catalog);

            Assert.That(result.Profile.Party.Presets[0].CharacterSlotIds[1], Is.Empty);
            Assert.That(result.Profile.RelicInstances[0].EquippedCharacterId, Is.Empty);
            Assert.That(result.Profile.RelicInstances[0].EquippedSlotIndex, Is.EqualTo(SaveRules.UnequippedRelicSlotIndex));
            Assert.That(result.Report.Issues.Where(issue => issue.CanAutoCorrect).All(issue => issue.WasAutoCorrected), Is.True);
        }

        private static List<string> EmptySlots() => new List<string> { "", "", "", "", "" };
    }
}
