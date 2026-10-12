using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Save.Copying;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Migration;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class V2ToV3RelicEquipMigrationTests
    {
        private V2ToV3RelicEquipMigration migration;

        [SetUp]
        public void SetUp()
        {
            migration = new V2ToV3RelicEquipMigration();
        }

        [Test]
        public void Migrate_MovesLegacyEquipsAndClearsLegacyFields()
        {
            ProfileSaveData source = CreateProfile();
            source.Characters[0].EquippedRelicInstanceIds =
                new List<string> { "stale", "stale", "stale", "stale" };
            source.RelicInstances.Add(CreateRelic(
                "relic_a", "sword", "character_1", 0));
            source.RelicInstances.Add(CreateRelic(
                "relic_b", "shield", "character_1", 2));
            source.RelicInstances.Add(CreateRelic(
                "relic_c", "sword", "character_2", 1));
            source.RelicInstances.Add(CreateRelic(
                "relic_d", "shield", "", -1));

            ProfileSaveData result = migration.Migrate(source);

            Assert.That(result.SaveVersion, Is.EqualTo(3));
            Assert.That(
                result.Characters[0].EquippedRelicInstanceIds,
                Is.EqualTo(new[] { "relic_a", "", "relic_b", "" }));
            Assert.That(
                result.Characters[1].EquippedRelicInstanceIds,
                Is.EqualTo(new[] { "", "relic_c", "", "" }));
            Assert.That(result.RelicInstances, Has.Count.EqualTo(4));
            foreach (RelicInstanceSaveData relic in result.RelicInstances)
            {
                Assert.That(relic.EquippedCharacterId, Is.Empty);
                Assert.That(
                    relic.EquippedSlotIndex,
                    Is.EqualTo(SaveRules.UnequippedRelicSlotIndex));
            }
        }

        [Test]
        public void Migrate_IgnoresUnknownCharactersAndInvalidSlots()
        {
            ProfileSaveData source = CreateProfile();
            source.RelicInstances.Add(CreateRelic(
                "relic_unknown", "sword", "missing", 0));
            source.RelicInstances.Add(CreateRelic(
                "relic_low", "shield", "character_1", -2));
            source.RelicInstances.Add(CreateRelic(
                "relic_high", "ring", "character_1", 4));

            ProfileSaveData result = migration.Migrate(source);

            Assert.That(
                result.Characters[0].EquippedRelicInstanceIds,
                Is.EqualTo(new[] { "", "", "", "" }));
            Assert.That(result.RelicInstances, Has.Count.EqualTo(3));
        }

        [Test]
        public void Migrate_PreservesFirstSlotAndDefinitionConflicts()
        {
            ProfileSaveData source = CreateProfile();
            source.RelicInstances.Add(CreateRelic(
                "first_sword", "sword", "character_1", 0));
            source.RelicInstances.Add(CreateRelic(
                "slot_conflict", "shield", "character_1", 0));
            source.RelicInstances.Add(CreateRelic(
                "definition_conflict", "sword", "character_1", 1));
            source.RelicInstances.Add(CreateRelic(
                "other_character_sword", "sword", "character_2", 1));

            ProfileSaveData result = migration.Migrate(source);

            Assert.That(
                result.Characters[0].EquippedRelicInstanceIds,
                Is.EqualTo(new[] { "first_sword", "", "", "" }));
            Assert.That(
                result.Characters[1].EquippedRelicInstanceIds,
                Is.EqualTo(new[]
                {
                    "", "other_character_sword", "", ""
                }));
        }

        [Test]
        public void Migrate_DoesNotEquipDuplicateInstanceIdTwice()
        {
            ProfileSaveData source = CreateProfile();
            source.RelicInstances.Add(CreateRelic(
                "duplicate", "sword", "character_1", 0));
            source.RelicInstances.Add(CreateRelic(
                "duplicate", "shield", "character_2", 2));

            ProfileSaveData result = migration.Migrate(source);

            Assert.That(
                result.Characters[0].EquippedRelicInstanceIds[0],
                Is.EqualTo("duplicate"));
            Assert.That(
                result.Characters[1].EquippedRelicInstanceIds[2],
                Is.Empty);
        }

        [Test]
        public void MigrationRunner_ChainsVersionOneThroughVersionThree()
        {
            ProfileSaveData source = CreateProfile();
            source.SaveVersion = 1;
            var runner = new SaveMigrationRunner(
                new SaveDataCloner(),
                new ISaveMigrationStep[]
                {
                    new V1ToV2PartyMigration(),
                    new V2ToV3RelicEquipMigration()
                });

            SaveMigrationResult result = runner.Migrate(source);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.SourceVersion, Is.EqualTo(1));
            Assert.That(result.TargetVersion, Is.EqualTo(3));
            Assert.That(result.Data.SaveVersion, Is.EqualTo(3));
        }

        private static ProfileSaveData CreateProfile()
        {
            ProfileSaveData profile =
                SaveValidationTestSupport.CreateValidProfile();
            profile.SaveVersion = 2;
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = "character_1",
                Level = 1
            });
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = "character_2",
                Level = 1
            });
            return profile;
        }

        private static RelicInstanceSaveData CreateRelic(
            string instanceId,
            string definitionId,
            string characterId,
            int slotIndex)
        {
            return new RelicInstanceSaveData
            {
                InstanceId = instanceId,
                RelicDefinitionId = definitionId,
                EquippedCharacterId = characterId,
                EquippedSlotIndex = slotIndex
            };
        }
    }
}
