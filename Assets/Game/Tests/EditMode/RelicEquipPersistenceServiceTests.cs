using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Relics;
using ValorChronicle.Relics.Persistence;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;
using ValorChronicle.Save.Serialization;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class RelicEquipPersistenceServiceTests
    {
        [Test]
        public void Equip_EmptySlotPersistsCandidate()
        {
            TestContext context = CreateContext(Profile());
            int writesBefore = context.WriteCount;
            RelicEquipSessionCommand command = EquipCommand(
                context.SaveService, 2, "orb_a");

            RelicEquipPersistenceOperationResult result =
                context.Persistence.Execute(command);

            AssertChanged(result, "sword_a", "bark_a", "orb_a", "");
            Assert.That(context.WriteCount, Is.EqualTo(writesBefore + 1));
        }

        [Test]
        public void Equip_ReplacesOnlyTargetAndKeepsOldRelicInInventory()
        {
            TestContext context = CreateContext(Profile());
            RelicEquipSessionCommand command = EquipCommand(
                context.SaveService, 0, "orb_a");

            RelicEquipPersistenceOperationResult result =
                context.Persistence.Execute(command);
            ProfileSaveData saved =
                context.SaveService.GetCurrentProfileSnapshot();

            AssertChanged(result, "orb_a", "bark_a", "", "");
            Assert.That(
                saved.RelicInstances.Exists(
                    relic => relic.InstanceId == "sword_a"),
                Is.True);
            Assert.That(saved.RelicInstances, Has.Count.EqualTo(6));
        }

        [Test]
        public void Equip_SameDefinitionReplacementInTargetPersists()
        {
            TestContext context = CreateContext(Profile());
            RelicEquipSessionCommand command = EquipCommand(
                context.SaveService, 0, "sword_b");

            RelicEquipPersistenceOperationResult result =
                context.Persistence.Execute(command);

            AssertChanged(result, "sword_b", "bark_a", "", "");
        }

        [Test]
        public void Equip_DuplicateDefinitionInOtherSlotIsAtomic()
        {
            ProfileSaveData profile = Profile();
            profile.Characters[0].EquippedRelicInstanceIds[0] =
                SaveRules.EmptyId;
            TestContext context = CreateContext(profile);
            RelicEquipSessionCommand command = EquipCommand(
                context.SaveService, 2, "sword_b");
            SetLatestSlot(context.SaveService, 0, 0, "sword_a");

            AssertRejectedWithoutChange(
                context,
                command,
                RelicEquipAvailabilityReason
                    .DuplicateDefinitionInOtherSlot);
        }

        [Test]
        public void Equip_InstanceUsedByOtherCharacterIsAtomic()
        {
            TestContext context = CreateContext(Profile());
            RelicEquipSessionCommand command = EquipCommand(
                context.SaveService, 2, "orb_a");
            SetLatestSlot(context.SaveService, 1, 0, "orb_a");

            AssertRejectedWithoutChange(
                context,
                command,
                RelicEquipAvailabilityReason.EquippedByOtherCharacter);
        }

        [Test]
        public void Equip_InstanceUsedInOtherSlotIsAtomic()
        {
            TestContext context = CreateContext(Profile());
            RelicEquipSessionCommand command = EquipCommand(
                context.SaveService, 2, "orb_a");
            SetLatestSlot(context.SaveService, 0, 3, "orb_a");

            AssertRejectedWithoutChange(
                context,
                command,
                RelicEquipAvailabilityReason.EquippedInOtherSlot);
        }

        [Test]
        public void Equip_SameDefinitionOnOtherCharacterIsAllowed()
        {
            ProfileSaveData profile = Profile();
            profile.Characters[0].EquippedRelicInstanceIds[0] =
                SaveRules.EmptyId;
            profile.Characters[1].EquippedRelicInstanceIds[0] = "sword_a";
            TestContext context = CreateContext(profile);
            RelicEquipSessionCommand command = EquipCommand(
                context.SaveService, 0, "sword_b");

            RelicEquipPersistenceOperationResult result =
                context.Persistence.Execute(command);

            AssertChanged(result, "sword_b", "bark_a", "", "");
            Assert.That(
                result.ProfileSnapshot.Characters[1]
                    .EquippedRelicInstanceIds[0],
                Is.EqualTo("sword_a"));
        }

        [Test]
        public void Equip_SameInstanceIsNoChangeWithoutWrite()
        {
            TestContext context = CreateContext(Profile());
            int writesBefore = context.WriteCount;
            RelicEquipSessionCommand command = EquipCommand(
                context.SaveService, 0, "sword_a");

            RelicEquipPersistenceOperationResult result =
                context.Persistence.Execute(command);

            AssertNoChange(result);
            Assert.That(context.WriteCount, Is.EqualTo(writesBefore));
        }

        [Test]
        public void Equip_EmptyPreviewIsNoRelicSelectedWithoutWrite()
        {
            TestContext context = CreateContext(Profile());
            int writesBefore = context.WriteCount;
            var session = new RelicEquipEditSession(
                new RelicEquipEditor(),
                context.SaveService.GetCurrentProfileSnapshot(),
                "character_a",
                2);

            RelicEquipPersistenceOperationResult result =
                context.Persistence.Execute(
                    session.CreateEquipCommand());

            Assert.That(
                result.Status,
                Is.EqualTo(RelicEquipPersistenceStatus.NoRelicSelected));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.SaveTransactionResult, Is.Null);
            Assert.That(context.WriteCount, Is.EqualTo(writesBefore));
        }

        [Test]
        public void Clear_PopulatedSlotPersistsEmptyAndIgnoresPreview()
        {
            TestContext context = CreateContext(Profile());
            var session = new RelicEquipEditSession(
                new RelicEquipEditor(),
                context.SaveService.GetCurrentProfileSnapshot(),
                "character_a",
                0);
            Assert.That(
                session.TrySetPreview("orb_a").CanSelect,
                Is.True);

            RelicEquipPersistenceOperationResult result =
                context.Persistence.Execute(
                    session.CreateClearCommand());

            AssertChanged(result, "", "bark_a", "", "");
            Assert.That(
                result.ProfileSnapshot.RelicInstances.Exists(
                    relic => relic.InstanceId == "orb_a"),
                Is.True);
        }

        [Test]
        public void Clear_EmptyOriginalIsNoChangeWithoutWrite()
        {
            TestContext context = CreateContext(Profile());
            int writesBefore = context.WriteCount;
            var session = new RelicEquipEditSession(
                new RelicEquipEditor(),
                context.SaveService.GetCurrentProfileSnapshot(),
                "character_a",
                2);
            session.TrySetPreview("orb_a");

            RelicEquipPersistenceOperationResult result =
                context.Persistence.Execute(
                    session.CreateClearCommand());

            AssertNoChange(result);
            Assert.That(context.WriteCount, Is.EqualTo(writesBefore));
        }

        [Test]
        public void Equip_StaleSessionPreservesLatestSlot()
        {
            TestContext context = CreateContext(Profile());
            RelicEquipSessionCommand command = EquipCommand(
                context.SaveService, 2, "orb_a");
            SetLatestSlot(context.SaveService, 2, "crown_a");
            int writesBefore = context.WriteCount;

            RelicEquipPersistenceOperationResult result =
                context.Persistence.Execute(command);

            AssertStale(result);
            Assert.That(CurrentSlots(context)[2], Is.EqualTo("crown_a"));
            Assert.That(context.WriteCount, Is.EqualTo(writesBefore));
        }

        [Test]
        public void Clear_StaleSessionPreservesLatestSlot()
        {
            TestContext context = CreateContext(Profile());
            var session = new RelicEquipEditSession(
                new RelicEquipEditor(),
                context.SaveService.GetCurrentProfileSnapshot(),
                "character_a",
                0);
            RelicEquipSessionCommand command = session.CreateClearCommand();
            SetLatestSlot(context.SaveService, 0, "orb_a");
            int writesBefore = context.WriteCount;

            RelicEquipPersistenceOperationResult result =
                context.Persistence.Execute(command);

            AssertStale(result);
            Assert.That(CurrentSlots(context)[0], Is.EqualTo("orb_a"));
            Assert.That(context.WriteCount, Is.EqualTo(writesBefore));
        }

        [Test]
        public void Equip_MissingInstanceIsRejectedWithoutChange()
        {
            TestContext context = CreateContext(Profile());
            RelicEquipSessionCommand command = EquipCommand(
                context.SaveService, 2, "orb_a");
            SaveTransactionResult transaction =
                context.SaveService.ExecuteTransaction(profile =>
                {
                    profile.RelicInstances.RemoveAll(
                        relic => relic.InstanceId == "orb_a");
                });
            Assert.That(transaction.IsSuccess, Is.True);

            AssertRejectedWithoutChange(
                context,
                command,
                RelicEquipAvailabilityReason.RelicInstanceNotFound);
        }

        [Test]
        public void Equip_CharacterRemovedAfterSessionIsRejected()
        {
            TestContext context = CreateContext(Profile());
            RelicEquipSessionCommand command = EquipCommand(
                context.SaveService, 2, "orb_a");
            SaveTransactionResult transaction =
                context.SaveService.ExecuteTransaction(profile =>
                {
                    profile.Characters.RemoveAll(
                        character =>
                            character.CharacterId == "character_a");
                });
            Assert.That(transaction.IsSuccess, Is.True);

            AssertRejectedWithoutChange(
                context,
                command,
                RelicEquipAvailabilityReason.CharacterNotFound);
        }

        [Test]
        public void Equip_RoundTripRetainsAuthoritativeRelationship()
        {
            TestContext context = CreateContext(Profile());
            RelicEquipPersistenceOperationResult result =
                context.Persistence.Execute(
                    EquipCommand(context.SaveService, 2, "orb_a"));
            Assert.That(result.IsSuccess, Is.True);

            SaveService reloaded = SaveServiceTestFactory.Create(
                context.Repository);
            SaveLoadResult load = reloaded.LoadOrCreate("ignored");

            Assert.That(load.CanUseProfile, Is.True, load.Message);
            Assert.That(
                reloaded.GetCurrentProfileSnapshot().Characters[0]
                    .EquippedRelicInstanceIds[2],
                Is.EqualTo("orb_a"));
        }

        [Test]
        public void EquipAndClear_DoNotWriteLegacyEquipFields()
        {
            TestContext context = CreateContext(Profile());
            RelicEquipPersistenceOperationResult equipped =
                context.Persistence.Execute(
                    EquipCommand(context.SaveService, 2, "orb_a"));
            Assert.That(equipped.IsSuccess, Is.True);
            var clearSession = new RelicEquipEditSession(
                new RelicEquipEditor(),
                context.SaveService.GetCurrentProfileSnapshot(),
                "character_a",
                2);
            RelicEquipPersistenceOperationResult cleared =
                context.Persistence.Execute(
                    clearSession.CreateClearCommand());

            Assert.That(cleared.IsSuccess, Is.True);
            foreach (RelicInstanceSaveData relic in cleared
                .ProfileSnapshot.RelicInstances)
            {
                Assert.That(relic.EquippedCharacterId, Is.Empty);
                Assert.That(
                    relic.EquippedSlotIndex,
                    Is.EqualTo(SaveRules.UnequippedRelicSlotIndex));
            }
        }

        [Test]
        public void Equip_SaveFailureRollsBackMemoryAndDisk()
        {
            TestContext context = CreateContext(Profile());
            string before = SnapshotJson(context);
            string diskBefore = context.Repository.MainText;
            RelicEquipSessionCommand command = EquipCommand(
                context.SaveService, 2, "orb_a");
            context.Repository.FailWriteTemp = true;

            RelicEquipPersistenceOperationResult result =
                context.Persistence.Execute(command);

            Assert.That(
                result.Status,
                Is.EqualTo(RelicEquipPersistenceStatus.SaveFailed));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(
                result.SaveTransactionResult.Status,
                Is.EqualTo(SaveTransactionStatus.SaveFailed));
            Assert.That(SnapshotJson(context), Is.EqualTo(before));
            Assert.That(context.Repository.MainText, Is.EqualTo(diskBefore));
        }

        private static RelicEquipSessionCommand EquipCommand(
            SaveService saveService,
            int slotIndex,
            string candidateInstanceId)
        {
            var session = new RelicEquipEditSession(
                new RelicEquipEditor(),
                saveService.GetCurrentProfileSnapshot(),
                "character_a",
                slotIndex);
            RelicEquipAvailability availability =
                session.TrySetPreview(candidateInstanceId);
            Assert.That(
                availability.CanSelect,
                Is.True,
                availability.Reason.ToString());
            return session.CreateEquipCommand();
        }

        private static TestContext CreateContext(ProfileSaveData profile)
        {
            var repository = new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.Json(profile)
            };
            SaveService saveService = SaveServiceTestFactory.Create(
                repository);
            SaveLoadResult load = saveService.LoadOrCreate("ignored");
            Assert.That(load.CanUseProfile, Is.True, load.Message);
            return new TestContext(repository, saveService);
        }

        private static ProfileSaveData Profile()
        {
            ProfileSaveData profile = SaveTestDataBuilder.Valid();
            profile.RelicInstances.Add(Relic("sword_a", "relic_sword", 0));
            profile.RelicInstances.Add(Relic("sword_b", "relic_sword", 5));
            profile.RelicInstances.Add(Relic("bark_a", "relic_bark", 0));
            profile.RelicInstances.Add(Relic("bark_b", "relic_bark", 5));
            profile.RelicInstances.Add(Relic("orb_a", "relic_orb", 0));
            profile.RelicInstances.Add(Relic("crown_a", "relic_crown", 0));
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = "character_a",
                Level = 1,
                EquippedRelicInstanceIds = new List<string>
                {
                    "sword_a", "bark_a", "", ""
                }
            });
            profile.Characters.Add(new CharacterSaveData
            {
                CharacterId = "character_b",
                Level = 1
            });
            return profile;
        }

        private static RelicInstanceSaveData Relic(
            string instanceId,
            string definitionId,
            int awakening)
        {
            return new RelicInstanceSaveData
            {
                InstanceId = instanceId,
                RelicDefinitionId = definitionId,
                Awakening = awakening
            };
        }

        private static void SetLatestSlot(
            SaveService saveService,
            int slotIndex,
            string instanceId)
        {
            SetLatestSlot(
                saveService,
                0,
                slotIndex,
                instanceId);
        }

        private static void SetLatestSlot(
            SaveService saveService,
            int characterIndex,
            int slotIndex,
            string instanceId)
        {
            SaveTransactionResult transaction =
                saveService.ExecuteTransaction(profile =>
                {
                    profile.Characters[characterIndex]
                        .EquippedRelicInstanceIds[slotIndex] = instanceId;
                });
            Assert.That(transaction.IsSuccess, Is.True);
        }

        private static IReadOnlyList<string> CurrentSlots(
            TestContext context)
        {
            return context.SaveService.GetCurrentProfileSnapshot()
                .Characters[0].EquippedRelicInstanceIds;
        }

        private static string SnapshotJson(TestContext context)
        {
            return new NewtonsoftJsonSaveSerializer().Serialize(
                context.SaveService.GetCurrentProfileSnapshot());
        }

        private static void AssertChanged(
            RelicEquipPersistenceOperationResult result,
            params string[] expectedSlots)
        {
            Assert.That(
                result.Status,
                Is.EqualTo(
                    RelicEquipPersistenceStatus.ChangedAndSaved));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.WasChanged, Is.True);
            Assert.That(result.SaveTransactionResult.IsSuccess, Is.True);
            Assert.That(
                result.ProfileSnapshot.Characters[0]
                    .EquippedRelicInstanceIds,
                Is.EqualTo(expectedSlots));
        }

        private static void AssertNoChange(
            RelicEquipPersistenceOperationResult result)
        {
            Assert.That(
                result.Status,
                Is.EqualTo(RelicEquipPersistenceStatus.NoChange));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.WasChanged, Is.False);
            Assert.That(result.ProfileSnapshot, Is.Not.Null);
            Assert.That(result.SaveTransactionResult, Is.Null);
        }

        private static void AssertStale(
            RelicEquipPersistenceOperationResult result)
        {
            Assert.That(
                result.Status,
                Is.EqualTo(RelicEquipPersistenceStatus.StaleSession));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.SaveTransactionResult, Is.Null);
        }

        private static void AssertRejectedWithoutChange(
            TestContext context,
            RelicEquipSessionCommand command,
            RelicEquipAvailabilityReason expectedReason)
        {
            string before = SnapshotJson(context);
            int writesBefore = context.WriteCount;

            RelicEquipPersistenceOperationResult result =
                context.Persistence.Execute(command);

            Assert.That(
                result.Status,
                Is.EqualTo(RelicEquipPersistenceStatus.Rejected));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Availability.Reason, Is.EqualTo(expectedReason));
            Assert.That(result.SaveTransactionResult, Is.Null);
            Assert.That(SnapshotJson(context), Is.EqualTo(before));
            Assert.That(context.WriteCount, Is.EqualTo(writesBefore));
        }

        private sealed class TestContext
        {
            public TestContext(
                FakeSaveRepository repository,
                SaveService saveService)
            {
                Repository = repository;
                SaveService = saveService;
                Persistence = new RelicEquipPersistenceService(
                    saveService,
                    new RelicEquipEditor());
            }

            public FakeSaveRepository Repository { get; }
            public SaveService SaveService { get; }
            public RelicEquipPersistenceService Persistence { get; }
            public int WriteCount => Repository.Count(
                nameof(FakeSaveRepository.WriteTemp));
        }
    }
}
