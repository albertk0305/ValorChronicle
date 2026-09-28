using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ValorChronicle.Party;
using ValorChronicle.Party.Persistence;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class PartyPersistenceServiceTests
    {
        [Test]
        public void SetActivePreset_ChangedIndex_SavesAndUpdatesProfile()
        {
            var repository = RepositoryWithProfile(Profile());
            SaveService saveService = LoadedService(repository);
            var persistence = CreatePersistence(saveService);
            int writesBefore = WriteCount(repository);

            PartyPersistenceOperationResult result =
                persistence.SetActivePreset(2);

            Assert.That(
                result.Status,
                Is.EqualTo(PartyPersistenceStatus.ChangedAndSaved));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.WasChanged, Is.True);
            Assert.That(result.SaveTransactionResult.IsSuccess, Is.True);
            Assert.That(result.ProfileSnapshot.Party.ActivePresetIndex,
                Is.EqualTo(2));
            Assert.That(
                saveService.GetCurrentProfileSnapshot()
                    .Party.ActivePresetIndex,
                Is.EqualTo(2));
            Assert.That(WriteCount(repository), Is.EqualTo(writesBefore + 1));
        }

        [Test]
        public void SetActivePreset_CurrentIndex_ReturnsNoChangeWithoutWrite()
        {
            ProfileSaveData profile = Profile();
            profile.Party.ActivePresetIndex = 2;
            var repository = RepositoryWithProfile(profile);
            SaveService saveService = LoadedService(repository);
            var persistence = CreatePersistence(saveService);
            int writesBefore = WriteCount(repository);

            PartyPersistenceOperationResult result =
                persistence.SetActivePreset(2);

            AssertNoChange(result);
            Assert.That(result.EditResult, Is.Null);
            Assert.That(result.ProfileSnapshot.Party.ActivePresetIndex,
                Is.EqualTo(2));
            Assert.That(WriteCount(repository), Is.EqualTo(writesBefore));
        }

        [TestCase(-1)]
        [TestCase(5)]
        public void SetActivePreset_InvalidIndexThrowsWithoutWrite(
            int presetIndex)
        {
            var repository = RepositoryWithProfile(Profile());
            SaveService saveService = LoadedService(repository);
            var persistence = CreatePersistence(saveService);
            int writesBefore = WriteCount(repository);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                persistence.SetActivePreset(presetIndex));
            Assert.That(WriteCount(repository), Is.EqualTo(writesBefore));
        }

        [Test]
        public void ConfirmEdit_PlacesCharacterAndReturnsSavedSnapshot()
        {
            var repository = RepositoryWithProfile(Profile());
            SaveService saveService = LoadedService(repository);
            var persistence = CreatePersistence(saveService);

            PartyPersistenceOperationResult result =
                persistence.ConfirmEdit(0, 0, "a");

            AssertChanged(
                result,
                PartyPresetEditOperationType.Place,
                "a", "", "", "", "");
            result.ProfileSnapshot.Party.Presets[0]
                .CharacterSlotIds[0] = "detached_change";
            Assert.That(
                saveService.GetCurrentProfileSnapshot()
                    .Party.Presets[0].CharacterSlotIds,
                Is.EqualTo(new[] { "a", "", "", "", "" }));
        }

        [Test]
        public void ConfirmEdit_ReplacesCharacter()
        {
            var repository = RepositoryWithProfile(
                Profile(Slots("a", "b", "", "", "")));
            var persistence = CreatePersistence(LoadedService(repository));

            PartyPersistenceOperationResult result =
                persistence.ConfirmEdit(0, 1, "c");

            AssertChanged(
                result,
                PartyPresetEditOperationType.Replace,
                "a", "c", "", "", "");
        }

        [Test]
        public void ConfirmEdit_MovesCharacterInOneTransaction()
        {
            var repository = RepositoryWithProfile(
                Profile(Slots("a", "b", "c", "", "")));
            var persistence = CreatePersistence(LoadedService(repository));
            int writesBefore = WriteCount(repository);

            PartyPersistenceOperationResult result =
                persistence.ConfirmEdit(0, 3, "b");

            AssertChanged(
                result,
                PartyPresetEditOperationType.Move,
                "a", "", "c", "b", "");
            Assert.That(result.EditResult.SourceSlotIndex, Is.EqualTo(1));
            Assert.That(WriteCount(repository), Is.EqualTo(writesBefore + 1));
        }

        [Test]
        public void ConfirmEdit_SwapsCharactersInOneTransaction()
        {
            var repository = RepositoryWithProfile(
                Profile(Slots("a", "b", "c", "", "")));
            var persistence = CreatePersistence(LoadedService(repository));
            int writesBefore = WriteCount(repository);

            PartyPersistenceOperationResult result =
                persistence.ConfirmEdit(0, 0, "c");

            AssertChanged(
                result,
                PartyPresetEditOperationType.Swap,
                "c", "b", "a", "", "");
            Assert.That(result.EditResult.SourceSlotIndex, Is.EqualTo(2));
            Assert.That(WriteCount(repository), Is.EqualTo(writesBefore + 1));
        }

        [Test]
        public void ConfirmEdit_CurrentCharacter_ReturnsNoChangeWithoutWrite()
        {
            var repository = RepositoryWithProfile(
                Profile(Slots("a", "b", "", "", "")));
            SaveService saveService = LoadedService(repository);
            var persistence = CreatePersistence(saveService);
            int writesBefore = WriteCount(repository);
            ProfileSaveData before = saveService.GetCurrentProfileSnapshot();

            PartyPersistenceOperationResult result =
                persistence.ConfirmEdit(0, 1, "b");

            AssertNoChange(result);
            Assert.That(
                result.EditResult.OperationType,
                Is.EqualTo(PartyPresetEditOperationType.NoChange));
            Assert.That(WriteCount(repository), Is.EqualTo(writesBefore));
            Assert.That(
                saveService.GetCurrentProfileSnapshot()
                    .Party.Presets[0].CharacterSlotIds,
                Is.EqualTo(before.Party.Presets[0].CharacterSlotIds));
        }

        [Test]
        public void ClearSlot_OccupiedSavesWhileEmptyDoesNotWrite()
        {
            var repository = RepositoryWithProfile(
                Profile(Slots("a", "b", "", "", "")));
            SaveService saveService = LoadedService(repository);
            var persistence = CreatePersistence(saveService);
            int writesBefore = WriteCount(repository);

            PartyPersistenceOperationResult cleared =
                persistence.ClearSlot(0, 1);
            int writesAfterClear = WriteCount(repository);
            PartyPersistenceOperationResult empty =
                persistence.ClearSlot(0, 1);

            AssertChanged(
                cleared,
                PartyPresetEditOperationType.Clear,
                "a", "", "", "", "");
            Assert.That(writesAfterClear, Is.EqualTo(writesBefore + 1));
            AssertNoChange(empty);
            Assert.That(
                empty.EditResult.OperationType,
                Is.EqualTo(PartyPresetEditOperationType.NoChange));
            Assert.That(WriteCount(repository), Is.EqualTo(writesAfterClear));
        }

        [Test]
        public void ConfirmEdit_ChangesOnlySelectedPreset()
        {
            ProfileSaveData profile = Profile(
                Slots("a", "", "", "", ""),
                Slots("a", "b", "", "", ""));
            var repository = RepositoryWithProfile(profile);
            SaveService saveService = LoadedService(repository);
            var persistence = CreatePersistence(saveService);
            string[] otherPresetBefore = saveService
                .GetCurrentProfileSnapshot()
                .Party.Presets[1].CharacterSlotIds.ToArray();

            PartyPersistenceOperationResult result =
                persistence.ConfirmEdit(0, 0, "c");
            ProfileSaveData saved =
                saveService.GetCurrentProfileSnapshot();

            AssertChanged(
                result,
                PartyPresetEditOperationType.Replace,
                "c", "", "", "", "");
            Assert.That(
                saved.Party.Presets[1].CharacterSlotIds,
                Is.EqualTo(otherPresetBefore));
        }

        [Test]
        public void ConfirmEdit_PersistedProfileReloadsWithChange()
        {
            var repository = RepositoryWithProfile(Profile());
            SaveService firstService = LoadedService(repository);
            var persistence = CreatePersistence(firstService);

            PartyPersistenceOperationResult result =
                persistence.ConfirmEdit(0, 0, "a");
            SaveService reloadedService = LoadedService(repository);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(
                reloadedService.GetCurrentProfileSnapshot()
                    .Party.Presets[0].CharacterSlotIds,
                Is.EqualTo(new[] { "a", "", "", "", "" }));
        }

        [Test]
        public void ConfirmEdit_SaveFailureReturnsFailedAndRollsBack()
        {
            var repository = RepositoryWithProfile(
                Profile(Slots("a", "b", "", "", "")));
            SaveService saveService = LoadedService(repository);
            var persistence = CreatePersistence(saveService);
            ProfileSaveData before = saveService.GetCurrentProfileSnapshot();
            repository.FailWriteTemp = true;

            PartyPersistenceOperationResult result =
                persistence.ConfirmEdit(0, 1, "c");
            ProfileSaveData after = saveService.GetCurrentProfileSnapshot();

            Assert.That(
                result.Status,
                Is.EqualTo(PartyPersistenceStatus.Failed));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ProfileSnapshot, Is.Null);
            Assert.That(
                result.SaveTransactionResult.Status,
                Is.EqualTo(SaveTransactionStatus.SaveFailed));
            Assert.That(
                after.Party.Presets[0].CharacterSlotIds,
                Is.EqualTo(before.Party.Presets[0].CharacterSlotIds));
        }

        [Test]
        public void ConfirmEdit_UnownedCharacterFailsWithoutWrite()
        {
            var repository = RepositoryWithProfile(
                Profile(Slots("a", "b", "", "", "")));
            SaveService saveService = LoadedService(repository);
            var persistence = CreatePersistence(saveService);
            int writesBefore = WriteCount(repository);

            PartyPersistenceOperationResult result =
                persistence.ConfirmEdit(0, 1, "not_owned");

            Assert.That(
                result.Status,
                Is.EqualTo(PartyPersistenceStatus.Failed));
            Assert.That(result.SaveTransactionResult, Is.Null);
            Assert.That(WriteCount(repository), Is.EqualTo(writesBefore));
            Assert.That(
                saveService.GetCurrentProfileSnapshot()
                    .Party.Presets[0].CharacterSlotIds,
                Is.EqualTo(new[] { "a", "b", "", "", "" }));
        }

        [Test]
        public void ConfirmEdit_NullPreviewThrowsWithoutWrite()
        {
            var repository = RepositoryWithProfile(Profile());
            var persistence = CreatePersistence(LoadedService(repository));
            int writesBefore = WriteCount(repository);

            Assert.Throws<ArgumentNullException>(() =>
                persistence.ConfirmEdit(0, 0, null));
            Assert.That(WriteCount(repository), Is.EqualTo(writesBefore));
        }

        [TestCase(-1)]
        [TestCase(5)]
        public void ConfirmEdit_InvalidSlotIndexThrowsWithoutWrite(
            int slotIndex)
        {
            var repository = RepositoryWithProfile(Profile());
            var persistence = CreatePersistence(LoadedService(repository));
            int writesBefore = WriteCount(repository);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                persistence.ConfirmEdit(0, slotIndex, "a"));
            Assert.That(WriteCount(repository), Is.EqualTo(writesBefore));
        }

        [Test]
        public void OperationWithoutLoadedProfile_ReturnsFailed()
        {
            SaveService saveService = SaveServiceTestFactory.Create(
                new FakeSaveRepository());
            var persistence = CreatePersistence(saveService);

            PartyPersistenceOperationResult result =
                persistence.ConfirmEdit(0, 0, "a");

            Assert.That(
                result.Status,
                Is.EqualTo(PartyPersistenceStatus.Failed));
            Assert.That(result.SaveTransactionResult, Is.Null);
            Assert.That(result.ProfileSnapshot, Is.Null);
        }

        private static PartyPersistenceService CreatePersistence(
            SaveService saveService)
        {
            return new PartyPersistenceService(
                saveService,
                new PartyPresetEditor());
        }

        private static SaveService LoadedService(
            FakeSaveRepository repository)
        {
            SaveService service = SaveServiceTestFactory.Create(repository);
            SaveLoadResult load = service.LoadOrCreate("ignored");
            Assert.That(load.CanUseProfile, Is.True, load.Message);
            return service;
        }

        private static FakeSaveRepository RepositoryWithProfile(
            ProfileSaveData profile)
        {
            return new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.Json(profile)
            };
        }

        private static ProfileSaveData Profile(
            params IReadOnlyList<string>[] presets)
        {
            ProfileSaveData profile = SaveTestDataBuilder.Valid();
            foreach (string characterId in new[] { "a", "b", "c", "d" })
            {
                profile.Characters.Add(new CharacterSaveData
                {
                    CharacterId = characterId,
                    Level = 1
                });
            }

            for (int index = 0; index < presets.Length; index++)
            {
                profile.Party.Presets[index].CharacterSlotIds =
                    new List<string>(presets[index]);
            }

            return profile;
        }

        private static IReadOnlyList<string> Slots(
            params string[] characterIds)
        {
            return characterIds;
        }

        private static int WriteCount(FakeSaveRepository repository)
        {
            return repository.Count(nameof(FakeSaveRepository.WriteTemp));
        }

        private static void AssertChanged(
            PartyPersistenceOperationResult result,
            PartyPresetEditOperationType expectedOperation,
            params string[] expectedSlots)
        {
            Assert.That(
                result.Status,
                Is.EqualTo(PartyPersistenceStatus.ChangedAndSaved));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.WasChanged, Is.True);
            Assert.That(result.SaveTransactionResult.IsSuccess, Is.True);
            Assert.That(
                result.EditResult.OperationType,
                Is.EqualTo(expectedOperation));
            Assert.That(
                result.ProfileSnapshot.Party.Presets[0]
                    .CharacterSlotIds,
                Is.EqualTo(expectedSlots));
        }

        private static void AssertNoChange(
            PartyPersistenceOperationResult result)
        {
            Assert.That(
                result.Status,
                Is.EqualTo(PartyPersistenceStatus.NoChange));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.WasChanged, Is.False);
            Assert.That(result.SaveTransactionResult, Is.Null);
            Assert.That(result.ProfileSnapshot, Is.Not.Null);
        }
    }
}
