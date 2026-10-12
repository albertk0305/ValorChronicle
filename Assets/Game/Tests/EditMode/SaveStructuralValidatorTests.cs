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
        public void Validate_DetectsRelicIdentityAndCharacterEquipIssues()
        {
            ProfileSaveData profile = SaveValidationTestSupport.CreateValidProfile();
            profile.RelicInstances.Add(new RelicInstanceSaveData
            {
                InstanceId = "r1",
                RelicDefinitionId = "sword",
                Awakening = 6
            });
            profile.RelicInstances.Add(new RelicInstanceSaveData
            {
                InstanceId = "r2",
                RelicDefinitionId = "sword"
            });
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = "hero_a",
                Level = 1,
                EquippedRelicInstanceIds = new List<string>
                {
                    "r1", "r2", null, "missing"
                }
            });
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = "hero_b",
                Level = 1,
                EquippedRelicInstanceIds = new List<string>
                {
                    "", "r1", "", ""
                }
            });

            SaveValidationReport report = validator.Validate(profile);

            Assert.That(report.Contains(SaveValidationCode.RelicAwakeningOutOfRange), Is.True);
            Assert.That(report.Contains(SaveValidationCode.DuplicateRelicDefinitionEquipped), Is.True);
            Assert.That(report.Contains(SaveValidationCode.NullEquippedRelicInstanceId), Is.True);
            Assert.That(report.Contains(SaveValidationCode.EquippedRelicInstanceNotFound), Is.True);
            Assert.That(report.Contains(SaveValidationCode.DuplicateRelicInstanceEquipped), Is.True);
        }

        [Test]
        public void Validate_DetectsInvalidCharacterRelicSlotCount()
        {
            ProfileSaveData profile =
                SaveValidationTestSupport.CreateValidProfile();
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = "hero",
                Level = 1,
                EquippedRelicInstanceIds = new List<string> { "", "" }
            });

            SaveValidationReport report = validator.Validate(profile);

            Assert.That(
                report.Find(
                    SaveValidationCode.InvalidCharacterRelicSlotCount,
                    "Characters[0].EquippedRelicInstanceIds"),
                Is.Not.Empty);
        }

        [Test]
        public void Validate_AllowsSameDefinitionOnDifferentCharacters()
        {
            ProfileSaveData profile =
                SaveValidationTestSupport.CreateValidProfile();
            profile.RelicInstances.Add(new RelicInstanceSaveData
            {
                InstanceId = "sword_a",
                RelicDefinitionId = "sword"
            });
            profile.RelicInstances.Add(new RelicInstanceSaveData
            {
                InstanceId = "sword_b",
                RelicDefinitionId = "sword"
            });
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = "hero_a",
                Level = 1,
                EquippedRelicInstanceIds = new List<string>
                {
                    "sword_a", "", "", ""
                }
            });
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = "hero_b",
                Level = 1,
                EquippedRelicInstanceIds = new List<string>
                {
                    "sword_b", "", "", ""
                }
            });

            SaveValidationReport report = validator.Validate(profile);

            Assert.That(
                report.Contains(
                    SaveValidationCode.DuplicateRelicDefinitionEquipped),
                Is.False);
            Assert.That(
                report.Contains(
                    SaveValidationCode.DuplicateRelicInstanceEquipped),
                Is.False);
        }

        [Test]
        public void Validate_DetectsPartyGachaBossAndStringListRules()
        {
            ProfileSaveData profile = SaveValidationTestSupport.CreateValidProfile();
            profile.Party.ActivePresetIndex = SaveRules.PartyPresetCount;
            profile.Party.Presets.Add(new PartyPresetSaveData { PresetId = SaveRules.DefaultPartyPresetId, CharacterSlotIds = new List<string> { "hero", "hero" } });
            profile.GachaStates.Add(new GachaStateSaveData { GachaId = "g", PityCount = 3, TotalPullCount = 2 });
            profile.GachaStates.Add(new GachaStateSaveData { GachaId = "g", PityCount = -1, TotalPullCount = -1 });
            profile.BossRecords.Add(new BossRecordSaveData { BossId = "b", DifficultyId = "d", IsCleared = true, BestDefeatTurn = 0, ClaimedFirstRewardGradeIds = new List<string> { "", "a", "a" } });
            profile.BossRecords.Add(new BossRecordSaveData { BossId = "b", DifficultyId = "d", ClaimedFirstRewardGradeIds = new List<string>() });
            profile.UnlockedContentIds.AddRange(new[] { "", "u", "u" });
            profile.CompletedTutorialIds.AddRange(new[] { "", "t", "t" });

            SaveValidationReport report = validator.Validate(profile);

            Assert.That(report.Contains(SaveValidationCode.InvalidActivePresetIndex), Is.True);
            Assert.That(report.Contains(SaveValidationCode.InvalidPartyPresetCount), Is.True);
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
