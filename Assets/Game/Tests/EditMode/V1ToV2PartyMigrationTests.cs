using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ValorChronicle.Save.Copying;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Migration;
using ValorChronicle.Save.Processing;
using ValorChronicle.Save.Rules;
using ValorChronicle.Save.Serialization;
using ValorChronicle.Save.Validation;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class V1ToV2PartyMigrationTests
    {
        private SaveDataCloner cloner;
        private SaveMigrationRunner runner;

        [SetUp]
        public void SetUp()
        {
            cloner = new SaveDataCloner();
            runner = new SaveMigrationRunner(
                cloner,
                new ISaveMigrationStep[]
                {
                    new V1ToV2PartyMigration()
                });
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void Migrate_ExpandsShortPresetListsAndPreservesOrder(
            int originalPresetCount)
        {
            ProfileSaveData source = CreateV1Profile(originalPresetCount);

            SaveMigrationResult result = runner.Migrate(source);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data.SaveVersion, Is.EqualTo(2));
            Assert.That(
                result.Data.Party.Presets,
                Has.Count.EqualTo(SaveRules.PartyPresetCount));
            for (int presetIndex = 0;
                presetIndex < originalPresetCount;
                presetIndex++)
            {
                Assert.That(
                    result.Data.Party.Presets[presetIndex].PresetId,
                    Is.EqualTo(presetIndex == 0
                        ? SaveRules.DefaultPartyPresetId
                        : $"legacy_{presetIndex + 1}"));
                Assert.That(
                    result.Data.Party.Presets[presetIndex]
                        .CharacterSlotIds[0],
                    Is.EqualTo($"character_{presetIndex}"));
            }

            for (int presetIndex = originalPresetCount;
                presetIndex < SaveRules.PartyPresetCount;
                presetIndex++)
            {
                AssertEmptyDefaultPreset(
                    result.Data.Party.Presets[presetIndex],
                    presetIndex);
            }

            Assert.That(source.SaveVersion, Is.EqualTo(1));
            Assert.That(
                source.Party.Presets,
                Has.Count.EqualTo(originalPresetCount));
        }

        [Test]
        public void Migrate_PreservesExactlyFivePresets()
        {
            ProfileSaveData source = CreateV1Profile(
                SaveRules.PartyPresetCount);

            SaveMigrationResult result = runner.Migrate(source);

            Assert.That(
                result.Data.Party.Presets.Select(item => item.PresetId),
                Is.EqualTo(source.Party.Presets.Select(
                    item => item.PresetId)));
        }

        [Test]
        public void Migrate_TruncatesPresetsAfterTheFirstFive()
        {
            ProfileSaveData source = CreateV1Profile(7);

            SaveMigrationResult result = runner.Migrate(source);

            Assert.That(
                result.Data.Party.Presets,
                Has.Count.EqualTo(SaveRules.PartyPresetCount));
            Assert.That(
                result.Data.Party.Presets.Select(item => item.PresetId),
                Is.EqualTo(source.Party.Presets.Take(
                    SaveRules.PartyPresetCount).Select(
                    item => item.PresetId)));
        }

        [Test]
        public void Migrate_NormalizesShortLongAndNullSlots()
        {
            ProfileSaveData source = CreateV1Profile(3);
            source.Party.Presets[0].CharacterSlotIds =
                new List<string> { "a", null };
            source.Party.Presets[1].CharacterSlotIds =
                new List<string> { "a", "b", "c", "d", "e", "f" };
            source.Party.Presets[2].CharacterSlotIds = null;

            SaveMigrationResult result = runner.Migrate(source);

            Assert.That(
                result.Data.Party.Presets[0].CharacterSlotIds,
                Is.EqualTo(new[] { "a", "", "", "", "" }));
            Assert.That(
                result.Data.Party.Presets[1].CharacterSlotIds,
                Is.EqualTo(new[] { "a", "b", "c", "d", "e" }));
            Assert.That(
                result.Data.Party.Presets[2].CharacterSlotIds,
                Is.EqualTo(new[] { "", "", "", "", "" }));
        }

        [TestCase(0, 0)]
        [TestCase(2, 2)]
        [TestCase(4, 4)]
        [TestCase(-1, 0)]
        [TestCase(5, 0)]
        [TestCase(99, 0)]
        public void Migrate_PreservesOrRepairsActivePresetIndex(
            int sourceIndex,
            int expectedIndex)
        {
            ProfileSaveData source = CreateV1Profile(5);
            source.Party.ActivePresetIndex = sourceIndex;

            SaveMigrationResult result = runner.Migrate(source);

            Assert.That(
                result.Data.Party.ActivePresetIndex,
                Is.EqualTo(expectedIndex));
        }

        [Test]
        public void ValidationPipeline_RepairsDuplicatesAndBrokenReferences()
        {
            ProfileSaveData source = CreateV1Profile(2);
            source.Characters = new List<CharacterSaveData>
            {
                new CharacterSaveData
                {
                    CharacterId = "owned",
                    Level = 1
                }
            };
            source.Party.Presets[0].CharacterSlotIds =
                new List<string>
                {
                    "owned",
                    "",
                    "owned",
                    "missing",
                    "unowned"
                };
            source.Party.Presets[1].CharacterSlotIds =
                new List<string> { "owned", "", "", "", "" };
            var catalog = new FakeSaveContentCatalog
            {
                CharacterLookup = id => id == "missing"
                    ? SaveContentLookupResult.Missing
                    : SaveContentLookupResult.Exists
            };

            SaveMigrationResult migration = runner.Migrate(source);
            SaveValidationProcessResult validation =
                new SaveValidationProcessor()
                    .ValidateAndRepairCurrentVersion(
                        migration.Data,
                        catalog);

            Assert.That(validation.CanUseProfile, Is.True);
            Assert.That(
                validation.UsableProfile.Party.Presets[0]
                    .CharacterSlotIds,
                Is.EqualTo(new[] { "owned", "", "", "", "" }));
            Assert.That(
                validation.UsableProfile.Party.Presets[1]
                    .CharacterSlotIds[0],
                Is.EqualTo("owned"));
        }

        [Test]
        public void MigratedProfile_IsStableAcrossRepeatedValidation()
        {
            ProfileSaveData source = CreateV1Profile(1);
            source.Party.Presets[0].CharacterSlotIds =
                new List<string> { null, "", "", "", "", "extra" };
            SaveMigrationResult migration = runner.Migrate(source);
            var processor = new SaveValidationProcessor();
            var catalog = new FakeSaveContentCatalog();
            var serializer = new NewtonsoftJsonSaveSerializer();

            SaveValidationProcessResult first =
                processor.ValidateAndRepairCurrentVersion(
                    migration.Data,
                    catalog);
            SaveValidationProcessResult second =
                processor.ValidateAndRepairCurrentVersion(
                    first.UsableProfile,
                    catalog);

            Assert.That(first.CanUseProfile, Is.True);
            Assert.That(second.CanUseProfile, Is.True);
            Assert.That(second.WasModified, Is.False);
            Assert.That(
                serializer.Serialize(second.UsableProfile),
                Is.EqualTo(serializer.Serialize(first.UsableProfile)));
        }

        private static ProfileSaveData CreateV1Profile(int presetCount)
        {
            var presets = new List<PartyPresetSaveData>(presetCount);
            for (int presetIndex = 0;
                presetIndex < presetCount;
                presetIndex++)
            {
                presets.Add(new PartyPresetSaveData
                {
                    PresetId = presetIndex == 0
                        ? SaveRules.DefaultPartyPresetId
                        : $"legacy_{presetIndex + 1}",
                    CharacterSlotIds = new List<string>
                    {
                        $"character_{presetIndex}",
                        "",
                        "",
                        "",
                        ""
                    }
                });
            }

            return new ProfileSaveData
            {
                SaveVersion = 1,
                ProfileId = "profile_v1",
                CreatedAtUtcUnixSeconds = 1,
                LastSavedAtUtcUnixSeconds = 1,
                Currencies = new CurrencySaveData(),
                Characters = new List<CharacterSaveData>(),
                RelicInstances = new List<RelicInstanceSaveData>(),
                Party = new PartySaveData
                {
                    ActivePresetIndex = 0,
                    Presets = presets,
                    LastBossId = "",
                    LastDifficultyId = ""
                },
                GachaStates = new List<GachaStateSaveData>(),
                BossRecords = new List<BossRecordSaveData>(),
                UnlockedContentIds = new List<string>(),
                CompletedTutorialIds = new List<string>()
            };
        }

        private static void AssertEmptyDefaultPreset(
            PartyPresetSaveData preset,
            int presetIndex)
        {
            Assert.That(
                preset.PresetId,
                Is.EqualTo(SaveRules.GetDefaultPartyPresetId(
                    presetIndex)));
            Assert.That(
                preset.CharacterSlotIds,
                Has.Count.EqualTo(SaveRules.PartySlotCount));
            Assert.That(
                preset.CharacterSlotIds.All(
                    value => value == string.Empty),
                Is.True);
        }
    }
}
