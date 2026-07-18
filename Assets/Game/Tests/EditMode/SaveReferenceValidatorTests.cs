using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Serialization;
using ValorChronicle.Save.Validation;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class SaveReferenceValidatorTests
    {
        [Test]
        public void Validate_ClassifiesOwnedContentMissingAsFatal()
        {
            ProfileSaveData profile = CreateReferencedProfile();
            var catalog = new FakeSaveContentCatalog
            {
                CharacterResult = SaveContentLookupResult.Missing,
                RelicResult = SaveContentLookupResult.Missing,
                GachaResult = SaveContentLookupResult.Missing,
                BossResult = SaveContentLookupResult.Missing,
                DifficultyResult = SaveContentLookupResult.Missing,
                RewardGradeResult = SaveContentLookupResult.Missing
            };

            SaveValidationReport report = new SaveReferenceValidator().Validate(profile, catalog);

            AssertFatal(report, SaveValidationCode.CharacterNotFound);
            AssertFatal(report, SaveValidationCode.RelicDefinitionNotFound);
            AssertFatal(report, SaveValidationCode.GachaNotFound);
            AssertFatal(report, SaveValidationCode.BossNotFound);
            AssertFatal(report, SaveValidationCode.BossDifficultyNotFound);
            AssertFatal(report, SaveValidationCode.RewardGradeNotFound);
        }

        [Test]
        public void Validate_ClassifiesBrokenConnectionsAsRecoverable()
        {
            ProfileSaveData profile = CreateReferencedProfile();
            profile.Party.Presets[0].CharacterSlotIds[1] = "ghost";
            profile.RelicInstances[0].EquippedCharacterId = "ghost";
            var catalog = new FakeSaveContentCatalog { CharacterLookup = id => id == "ghost" ? SaveContentLookupResult.Missing : SaveContentLookupResult.Exists };

            SaveValidationReport report = new SaveReferenceValidator().Validate(profile, catalog);

            AssertRecoverable(report, SaveValidationCode.PartyCharacterNotOwned);
            AssertRecoverable(report, SaveValidationCode.PartyCharacterNotFound);
            AssertRecoverable(report, SaveValidationCode.RelicEquippedToUnownedCharacter);
            AssertRecoverable(report, SaveValidationCode.RelicEquippedToUnknownCharacter);
        }

        [Test]
        public void Validate_UnavailableIsWarningOncePerCatalogKindAndDoesNotMutate()
        {
            ProfileSaveData profile = CreateReferencedProfile();
            profile.Characters.Add(new CharacterSaveData { CharacterId = "hero_2", Level = 1 });
            var catalog = new FakeSaveContentCatalog
            {
                CharacterResult = SaveContentLookupResult.Unavailable,
                RelicResult = SaveContentLookupResult.Unavailable,
                GachaResult = SaveContentLookupResult.Unavailable,
                BossResult = SaveContentLookupResult.Unavailable,
                DifficultyResult = SaveContentLookupResult.Unavailable,
                RewardGradeResult = SaveContentLookupResult.Unavailable
            };
            var serializer = new NewtonsoftJsonSaveSerializer();
            string before = serializer.Serialize(profile);

            SaveValidationReport report = new SaveReferenceValidator().Validate(profile, catalog);

            Assert.That(report.HasFatalErrors, Is.False);
            Assert.That(report.Issues.Count(issue => issue.Code == SaveValidationCode.ReferenceCatalogUnavailable), Is.EqualTo(6));
            Assert.That(report.Find(SaveValidationCode.ReferenceCatalogUnavailable, "Catalog.Character").Count(), Is.EqualTo(1));
            Assert.That(serializer.Serialize(profile), Is.EqualTo(before));
        }

        internal static ProfileSaveData CreateReferencedProfile()
        {
            ProfileSaveData profile = SaveValidationTestSupport.CreateValidProfile();
            profile.Characters.Add(new CharacterSaveData { CharacterId = "hero", Level = 1 });
            profile.Party.Presets[0].CharacterSlotIds[0] = "hero";
            profile.RelicInstances.Add(new RelicInstanceSaveData { InstanceId = "relic_1", RelicDefinitionId = "sword", EquippedCharacterId = "hero", EquippedSlotIndex = 0 });
            profile.GachaStates.Add(new GachaStateSaveData { GachaId = "standard" });
            profile.BossRecords.Add(new BossRecordSaveData { BossId = "boss", DifficultyId = "normal", HasAttempted = true, ClaimedFirstRewardGradeIds = new List<string> { "bronze" } });
            return profile;
        }

        private static void AssertFatal(SaveValidationReport report, SaveValidationCode code)
        {
            Assert.That(report.Issues.Any(issue => issue.Code == code && issue.Severity == SaveValidationSeverity.FatalError), Is.True);
        }

        private static void AssertRecoverable(SaveValidationReport report, SaveValidationCode code)
        {
            Assert.That(report.Issues.Any(issue => issue.Code == code && issue.Severity == SaveValidationSeverity.RecoverableError && issue.CanAutoCorrect), Is.True);
        }
    }
}
