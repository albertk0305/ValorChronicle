using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ValorChronicle.Save.Copying;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;
using ValorChronicle.Save.Serialization;
using ValorChronicle.Save.Validation;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class SaveStructuralValidatorTests
    {
        private SaveStructuralValidator validator;

        [SetUp]
        public void SetUp() => validator = new SaveStructuralValidator();

        [Test]
        public void Validate_CollectsProfileTimestampAndEveryCurrencyIssue()
        {
            ProfileSaveData profile = SaveValidationTestSupport.CreateValidProfile();
            profile.SaveVersion++;
            profile.ProfileId = " ";
            profile.CreatedAtUtcUnixSeconds = -1;
            profile.LastSavedAtUtcUnixSeconds = -2;
            profile.Currencies = new CurrencySaveData { GachaCurrency = -1, BattleRecords = -1, HeroTokens = -1, RelicTokens = -1 };

            SaveValidationReport report = validator.Validate(profile);

            Assert.That(report.Contains(SaveValidationCode.UnexpectedSaveVersion), Is.True);
            Assert.That(report.Contains(SaveValidationCode.MissingProfileId), Is.True);
            Assert.That(report.Contains(SaveValidationCode.InvalidCreatedTimestamp), Is.True);
            Assert.That(report.Contains(SaveValidationCode.InvalidLastSavedTimestamp), Is.True);
            Assert.That(report.Issues.Count(issue => issue.Code == SaveValidationCode.NegativeCurrency), Is.EqualTo(4));
        }

        [Test]
        public void Validate_DetectsCharacterIdentityAndBoundsWithExactPaths()
        {
            ProfileSaveData profile = SaveValidationTestSupport.CreateValidProfile();
            profile.Characters.Add(new CharacterSaveData { CharacterId = "hero", Level = 0, Awakening = 7 });
            profile.Characters.Add(new CharacterSaveData { CharacterId = "hero", Level = 100, Awakening = 0 });

            SaveValidationReport report = validator.Validate(profile);

            Assert.That(report.Find(SaveValidationCode.CharacterLevelOutOfRange, "Characters[0].Level"), Is.Not.Empty);
            Assert.That(report.Find(SaveValidationCode.CharacterAwakeningOutOfRange, "Characters[0].Awakening"), Is.Not.Empty);
            Assert.That(report.Find(SaveValidationCode.DuplicateCharacterId, "Characters[1].CharacterId").Single().Severity, Is.EqualTo(SaveValidationSeverity.FatalError));
        }

        [Test]
        public void Validate_DetectsRelicIdentityEquipAndDeterministicCollisions()
        {
            ProfileSaveData profile = SaveValidationTestSupport.CreateValidProfile();
            profile.RelicInstances.Add(new RelicInstanceSaveData { InstanceId = "r", RelicDefinitionId = "sword", Awakening = 6, EquippedCharacterId = "hero", EquippedSlotIndex = 0 });
            profile.RelicInstances.Add(new RelicInstanceSaveData { InstanceId = "r", RelicDefinitionId = "sword", EquippedCharacterId = "hero", EquippedSlotIndex = 0 });
            profile.RelicInstances.Add(new RelicInstanceSaveData { InstanceId = "r3", RelicDefinitionId = "shield", EquippedCharacterId = "", EquippedSlotIndex = 2 });

            SaveValidationReport report = validator.Validate(profile);

            Assert.That(report.Contains(SaveValidationCode.DuplicateRelicInstanceId), Is.True);
            Assert.That(report.Contains(SaveValidationCode.RelicAwakeningOutOfRange), Is.True);
            Assert.That(report.Find(SaveValidationCode.RelicSlotCollision, "RelicInstances[1].EquippedSlotIndex"), Is.Not.Empty);
            Assert.That(report.Contains(SaveValidationCode.DuplicateRelicDefinitionEquipped), Is.True);
            Assert.That(report.Contains(SaveValidationCode.InvalidRelicEquipPair), Is.True);
        }

        [Test]
        public void Validate_DetectsPartyGachaBossAndStringListRules()
        {
            ProfileSaveData profile = SaveValidationTestSupport.CreateValidProfile();
            profile.Party.ActivePresetIndex = 2;
            profile.Party.Presets.Add(new PartyPresetSaveData { PresetId = SaveRules.DefaultPartyPresetId, CharacterSlotIds = new List<string> { "hero", "hero" } });
            profile.GachaStates.Add(new GachaStateSaveData { GachaId = "g", PityCount = 3, TotalPullCount = 2 });
            profile.GachaStates.Add(new GachaStateSaveData { GachaId = "g", PityCount = -1, TotalPullCount = -1 });
            profile.BossRecords.Add(new BossRecordSaveData { BossId = "b", DifficultyId = "d", IsCleared = true, BestDefeatTurn = 0, ClaimedFirstRewardGradeIds = new List<string> { "", "a", "a" } });
            profile.BossRecords.Add(new BossRecordSaveData { BossId = "b", DifficultyId = "d", ClaimedFirstRewardGradeIds = new List<string>() });
            profile.UnlockedContentIds.AddRange(new[] { "", "u", "u" });
            profile.CompletedTutorialIds.AddRange(new[] { "", "t", "t" });

            SaveValidationReport report = validator.Validate(profile);

            Assert.That(report.Contains(SaveValidationCode.InvalidActivePresetIndex), Is.True);
            Assert.That(report.Contains(SaveValidationCode.DuplicatePartyPresetId), Is.True);
            Assert.That(report.Contains(SaveValidationCode.InvalidPartySlotCount), Is.True);
            Assert.That(report.Contains(SaveValidationCode.DuplicatePartyCharacter), Is.True);
            Assert.That(report.Contains(SaveValidationCode.DuplicateGachaId), Is.True);
            Assert.That(report.Contains(SaveValidationCode.GachaPityExceedsTotal), Is.True);
            Assert.That(report.Contains(SaveValidationCode.DuplicateBossRecord), Is.True);
            Assert.That(report.Find(SaveValidationCode.InvalidBestDefeatTurn, "BossRecords[0].BestDefeatTurn").Single().Severity, Is.EqualTo(SaveValidationSeverity.FatalError));
            Assert.That(report.Contains(SaveValidationCode.EmptyRewardGradeId), Is.True);
            Assert.That(report.Contains(SaveValidationCode.DuplicateRewardGradeId), Is.True);
            Assert.That(report.Contains(SaveValidationCode.EmptyUnlockedContentId), Is.True);
            Assert.That(report.Contains(SaveValidationCode.DuplicateTutorialId), Is.True);
        }

        [Test]
        public void Validate_DoesNotMutateInput()
        {
            ProfileSaveData profile = SaveValidationTestSupport.CreateValidProfile();
            profile.Characters.Add(new CharacterSaveData { CharacterId = "hero", Level = -1 });
            var serializer = new NewtonsoftJsonSaveSerializer();
            string before = serializer.Serialize(profile);

            validator.Validate(profile);

            Assert.That(serializer.Serialize(profile), Is.EqualTo(before));
        }
    }
}
