using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Processing;
using ValorChronicle.Save.Rules;
using ValorChronicle.Save.Serialization;
using ValorChronicle.Save.Validation;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class SaveValidationProcessorTests
    {
        private SaveValidationProcessor processor;
        private NewtonsoftJsonSaveSerializer serializer;

        [SetUp]
        public void SetUp()
        {
            processor = new SaveValidationProcessor();
            serializer = new NewtonsoftJsonSaveSerializer();
        }

        [Test]
        public void ValidateAndRepair_ValidProfileReturnsIndependentUsableCopy()
        {
            ProfileSaveData source = SaveValidationTestSupport.CreateValidProfile();

            SaveValidationProcessResult result = processor.ValidateAndRepairCurrentVersion(source, new FakeSaveContentCatalog());

            Assert.That(result.CanUseProfile, Is.True);
            Assert.That(result.HasFatalErrors, Is.False);
            Assert.That(result.FinalReport.HasRecoverableErrors, Is.False);
            Assert.That(result.UsableProfile, Is.Not.SameAs(source));
            Assert.That(result.UsableProfile.Party, Is.Not.SameAs(source.Party));
        }

        [Test]
        public void ValidateAndRepair_ReportsNormalizerOnlyChanges()
        {
            ProfileSaveData source = SaveValidationTestSupport.CreateValidProfile();
            source.Party.Presets[0].CharacterSlotIds.RemoveAt(4);

            SaveValidationProcessResult result = processor.ValidateAndRepairCurrentVersion(source, new FakeSaveContentCatalog());

            Assert.That(result.CanUseProfile, Is.True);
            Assert.That(result.WasModified, Is.True);
            Assert.That(result.UsableProfile.Party.Presets[0].CharacterSlotIds, Has.Count.EqualTo(SaveRules.PartySlotCount));
        }

        [Test]
        public void ValidateAndRepair_RepairsStructureAndReferencesWithoutMutatingInput()
        {
            ProfileSaveData source = SaveReferenceValidatorTests.CreateReferencedProfile();
            source.Currencies.GachaCurrency = -10;
            source.Characters[0].Level = 200;
            source.Party.Presets[0].CharacterSlotIds[1] = "ghost";
            string before = serializer.Serialize(source);
            var catalog = new FakeSaveContentCatalog { CharacterLookup = id => id == "ghost" ? SaveContentLookupResult.Missing : SaveContentLookupResult.Exists };

            SaveValidationProcessResult result = processor.ValidateAndRepairCurrentVersion(source, catalog);

            Assert.That(result.CanUseProfile, Is.True);
            Assert.That(result.WasModified, Is.True);
            Assert.That(result.UsableProfile.Currencies.GachaCurrency, Is.Zero);
            Assert.That(result.UsableProfile.Characters[0].Level, Is.EqualTo(100));
            Assert.That(result.UsableProfile.Party.Presets[0].CharacterSlotIds[1], Is.Empty);
            Assert.That(result.FinalReport.HasRecoverableErrors, Is.False);
            Assert.That(serializer.Serialize(source), Is.EqualTo(before));
        }

        [TestCase("character")]
        [TestCase("relic")]
        public void ValidateAndRepair_FatalDuplicatesDoNotExposePartialProfile(string kind)
        {
            ProfileSaveData source = SaveValidationTestSupport.CreateValidProfile();
            if (kind == "character")
            {
                source.Characters.Add(new CharacterSaveData { CharacterId = "hero", Level = 1 });
                source.Characters.Add(new CharacterSaveData { CharacterId = "hero", Level = 1 });
            }
            else
            {
                source.RelicInstances.Add(new RelicInstanceSaveData { InstanceId = "r", RelicDefinitionId = "s", EquippedCharacterId = "", EquippedSlotIndex = -1 });
                source.RelicInstances.Add(new RelicInstanceSaveData { InstanceId = "r", RelicDefinitionId = "s", EquippedCharacterId = "", EquippedSlotIndex = -1 });
            }

            SaveValidationProcessResult result = processor.ValidateAndRepairCurrentVersion(source, new FakeSaveContentCatalog());

            Assert.That(result.CanUseProfile, Is.False);
            Assert.That(result.UsableProfile, Is.Null);
            Assert.That(result.HasFatalErrors, Is.True);
        }

        [Test]
        public void ValidateAndRepair_MissingOwnedContentIsFatalButUnavailableIsUsable()
        {
            ProfileSaveData source = SaveValidationTestSupport.CreateValidProfile();
            source.Characters.Add(new CharacterSaveData { CharacterId = "hero", Level = 1 });

            SaveValidationProcessResult missing = processor.ValidateAndRepairCurrentVersion(source, new FakeSaveContentCatalog { CharacterResult = SaveContentLookupResult.Missing });
            SaveValidationProcessResult unavailable = processor.ValidateAndRepairCurrentVersion(source, new FakeSaveContentCatalog { CharacterResult = SaveContentLookupResult.Unavailable });

            Assert.That(missing.CanUseProfile, Is.False);
            Assert.That(missing.FinalReport.Contains(SaveValidationCode.CharacterNotFound), Is.True);
            Assert.That(unavailable.CanUseProfile, Is.True);
            Assert.That(unavailable.FinalReport.Contains(SaveValidationCode.ReferenceCatalogUnavailable), Is.True);
        }

        [Test]
        public void ValidateAndRepair_FutureVersionIsRejectedBeforeNormalization()
        {
            var source = new ProfileSaveData { SaveVersion = SaveRules.CurrentSaveVersion + 1, ProfileId = null, Party = null };

            SaveValidationProcessResult result = processor.ValidateAndRepairCurrentVersion(source, new FakeSaveContentCatalog());

            Assert.That(result.CanUseProfile, Is.False);
            Assert.That(result.WasModified, Is.False);
            Assert.That(result.FinalReport.Issues, Has.Count.EqualTo(1));
            Assert.That(result.FinalReport.Contains(SaveValidationCode.UnexpectedSaveVersion), Is.True);
            Assert.That(source.ProfileId, Is.Null);
            Assert.That(source.Party, Is.Null);
        }

        [Test]
        public void ValidateAndRepair_IsIdempotent()
        {
            ProfileSaveData source = SaveValidationTestSupport.CreateValidProfile();
            source.Currencies.HeroTokens = -1;
            source.UnlockedContentIds = new List<string> { "", "mode", "mode" };

            SaveValidationProcessResult first = processor.ValidateAndRepairCurrentVersion(source, new FakeSaveContentCatalog());
            SaveValidationProcessResult second = processor.ValidateAndRepairCurrentVersion(first.UsableProfile, new FakeSaveContentCatalog());

            Assert.That(serializer.Serialize(second.UsableProfile), Is.EqualTo(serializer.Serialize(first.UsableProfile)));
            Assert.That(second.WasModified, Is.False);
            Assert.That(second.FinalReport.HasRecoverableErrors, Is.False);
        }
    }
}
