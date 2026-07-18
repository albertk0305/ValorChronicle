using System;
using System.Linq;
using NUnit.Framework;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Serialization;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class SaveServiceTests
    {
        [Test]
        public void LoadOrCreate_NoFiles_PersistsBeforeExposingNewProfile()
        {
            var repository = new FakeSaveRepository();
            var time = new FixedUnixTimeProvider(123);
            SaveService service = SaveServiceTestFactory.Create(repository, time);

            SaveLoadResult result = service.LoadOrCreate("profile_new");

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.CreatedNewProfile));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(service.HasCurrentProfile, Is.True);
            Assert.That(repository.MainExists, Is.True);
            Assert.That(result.ProfileSnapshot.ProfileId, Is.EqualTo("profile_new"));
            Assert.That(result.ProfileSnapshot.CreatedAtUtcUnixSeconds, Is.EqualTo(123));
            Assert.That(result.ProfileSnapshot.LastSavedAtUtcUnixSeconds, Is.EqualTo(123));
        }

        [Test]
        public void LoadOrCreate_FirstWriteFails_DoesNotExposeCurrentProfile()
        {
            var repository = new FakeSaveRepository { FailWriteTemp = true };
            SaveService service = SaveServiceTestFactory.Create(repository);

            SaveLoadResult result = service.LoadOrCreate("profile_new");

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.WriteFailed));
            Assert.That(service.HasCurrentProfile, Is.False);
            Assert.Throws<InvalidOperationException>(() => service.GetCurrentProfileSnapshot());
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void LoadOrCreate_InvalidNewProfileId_IsRejected(string profileId)
        {
            SaveService service = SaveServiceTestFactory.Create(new FakeSaveRepository());

            SaveLoadResult result = service.LoadOrCreate(profileId);

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.NoUsableSave));
            Assert.That(result.Exception, Is.InstanceOf<ArgumentException>());
            Assert.That(service.HasCurrentProfile, Is.False);
        }

        [Test]
        public void LoadOrCreate_ValidMain_DoesNotReadBackupAndIgnoresNewId()
        {
            var repository = new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.ValidJson("existing"),
                BackupText = SaveTestDataBuilder.ValidJson("backup"),
                TempText = "stale"
            };
            SaveService service = SaveServiceTestFactory.Create(repository);

            SaveLoadResult result = service.LoadOrCreate("ignored");

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.LoadedMain));
            Assert.That(result.ProfileSnapshot.ProfileId, Is.EqualTo("existing"));
            Assert.That(repository.Count(nameof(FakeSaveRepository.ReadBackup)), Is.Zero);
            Assert.That(repository.Count(nameof(FakeSaveRepository.DeleteTempIfExists)), Is.EqualTo(1));
            Assert.That(repository.TempExists, Is.False);
        }

        [Test]
        public void LoadOrCreate_StaleTempDeleteFails_MainStillLoads()
        {
            var repository = new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.ValidJson(),
                TempText = "stale",
                FailDeleteTemp = true
            };

            SaveLoadResult result = SaveServiceTestFactory.Create(repository).LoadOrCreate("ignored");

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.LoadedMain));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(repository.TempText, Is.EqualTo("stale"));
        }

        [Test]
        public void Snapshots_AreDeepCopiesOfInternalProfile()
        {
            SaveService service = LoadedService(new FakeSaveRepository { MainText = SaveTestDataBuilder.ValidJson() });

            ProfileSaveData first = service.GetCurrentProfileSnapshot();
            first.ProfileId = "changed";
            first.Currencies.GachaCurrency = 999;
            ProfileSaveData second = service.GetCurrentProfileSnapshot();

            Assert.That(second.ProfileId, Is.EqualTo("profile_test"));
            Assert.That(second.Currencies.GachaCurrency, Is.Zero);
        }

        [Test]
        public void LoadOrCreate_RepairableMain_SavesRepairWithoutRotatingBackup()
        {
            string backup = SaveTestDataBuilder.ValidJson("backup");
            var repository = new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.Json(SaveTestDataBuilder.RepairableNegativeCurrency()),
                BackupText = backup
            };
            SaveService service = SaveServiceTestFactory.Create(repository, new FixedUnixTimeProvider(200));

            SaveLoadResult result = service.LoadOrCreate("ignored");

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.LoadedAndRepairedMain));
            Assert.That(result.WasRepaired, Is.True);
            Assert.That(result.ProfileSnapshot.Currencies.GachaCurrency, Is.Zero);
            Assert.That(result.ProfileSnapshot.LastSavedAtUtcUnixSeconds, Is.EqualTo(200));
            Assert.That(repository.BackupText, Is.EqualTo(backup));
            Assert.That(repository.Count(nameof(FakeSaveRepository.CopyMainToBackup)), Is.Zero);
        }

        [Test]
        public void LoadOrCreate_RepairWriteFails_DoesNotExposeRepairedMain()
        {
            var repository = new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.Json(SaveTestDataBuilder.RepairableNegativeCurrency()),
                FailWriteTemp = true
            };
            SaveService service = SaveServiceTestFactory.Create(repository);

            SaveLoadResult result = service.LoadOrCreate("ignored");

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.WriteFailed));
            Assert.That(result.CanUseProfile, Is.False);
            Assert.That(service.HasCurrentProfile, Is.False);
        }

        [Test]
        public void LoadOrCreate_FutureMain_DoesNotInspectBackupOrModifyFiles()
        {
            var repository = new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.FutureJson(),
                BackupText = SaveTestDataBuilder.ValidJson(),
                TempText = "preserve"
            };
            string originalMain = repository.MainText;

            SaveLoadResult result = SaveServiceTestFactory.Create(repository).LoadOrCreate("new");

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.FutureVersion));
            Assert.That(repository.Count(nameof(FakeSaveRepository.ReadBackup)), Is.Zero);
            Assert.That(repository.MainText, Is.EqualTo(originalMain));
            Assert.That(repository.TempText, Is.EqualTo("preserve"));
            Assert.That(repository.Calls, Does.Not.Contain(nameof(FakeSaveRepository.WriteTemp)));
        }

        [Test]
        public void LoadOrCreate_UnsupportedOlderMain_DoesNotFallback()
        {
            var repository = new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.UnsupportedOlderJson(),
                BackupText = SaveTestDataBuilder.ValidJson(),
                TempText = "preserve"
            };

            SaveLoadResult result = SaveServiceTestFactory.Create(repository).LoadOrCreate("new");

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.UnsupportedOlderVersion));
            Assert.That(repository.Count(nameof(FakeSaveRepository.ReadBackup)), Is.Zero);
            Assert.That(repository.Calls, Does.Not.Contain(nameof(FakeSaveRepository.WriteTemp)));
            Assert.That(repository.TempText, Is.EqualTo("preserve"));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void LoadOrCreate_InvalidMain_RecoversBackupWithoutRotatingIt(bool fatalMain)
        {
            string backup = SaveTestDataBuilder.ValidJson("backup_profile", 25);
            var repository = new FakeSaveRepository
            {
                MainText = fatalMain
                    ? SaveTestDataBuilder.Json(SaveTestDataBuilder.FatalDuplicateCharacter())
                    : SaveTestDataBuilder.CorruptJson(),
                BackupText = backup
            };
            SaveService service = SaveServiceTestFactory.Create(repository);

            SaveLoadResult result = service.LoadOrCreate("ignored");

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.RecoveredFromBackup));
            Assert.That(result.WasRecoveredFromBackup, Is.True);
            Assert.That(result.CanWriteProfile, Is.True);
            Assert.That(result.ProfileSnapshot.ProfileId, Is.EqualTo("backup_profile"));
            Assert.That(repository.BackupText, Is.EqualTo(backup));
            Assert.That(repository.Count(nameof(FakeSaveRepository.CopyMainToBackup)), Is.Zero);
            Assert.That(new NewtonsoftJsonSaveSerializer().Deserialize(repository.MainText).ProfileId, Is.EqualTo("backup_profile"));
        }

        [Test]
        public void LoadOrCreate_BackupRestorePromoteFails_ExposesReadOnlyProfile()
        {
            var repository = new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.CorruptJson(),
                BackupText = SaveTestDataBuilder.ValidJson("backup"),
                FailPromoteTempToMain = true
            };
            SaveService service = SaveServiceTestFactory.Create(repository);

            SaveLoadResult result = service.LoadOrCreate("ignored");
            SaveTransactionResult transaction = service.ExecuteTransaction(profile => profile.Currencies.GachaCurrency++);

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.RecoveredFromBackupButMainRepairFailed));
            Assert.That(result.CanUseProfile, Is.True);
            Assert.That(result.CanWriteProfile, Is.False);
            Assert.That(service.CanWriteCurrentProfile, Is.False);
            Assert.That(transaction.Status, Is.EqualTo(SaveTransactionStatus.ReadOnlyProfile));
        }

        [Test]
        public void LoadOrCreate_MainAndBackupInvalid_PreservesAllFilesAndTemp()
        {
            var repository = new FakeSaveRepository
            {
                MainText = "bad-main",
                BackupText = "bad-backup",
                TempText = "possible-forensics"
            };

            SaveLoadResult result = SaveServiceTestFactory.Create(repository).LoadOrCreate("new");

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.MainAndBackupInvalid));
            Assert.That(result.MainFailure, Is.Not.Null);
            Assert.That(result.BackupFailure, Is.Not.Null);
            Assert.That(repository.MainText, Is.EqualTo("bad-main"));
            Assert.That(repository.BackupText, Is.EqualTo("bad-backup"));
            Assert.That(repository.TempText, Is.EqualTo("possible-forensics"));
            Assert.That(repository.Calls, Does.Not.Contain(nameof(FakeSaveRepository.WriteTemp)));
        }

        [Test]
        public void LoadOrCreate_InvalidMainAndFutureBackup_DoesNotModifyFiles()
        {
            var repository = new FakeSaveRepository
            {
                MainText = "bad-main",
                BackupText = SaveTestDataBuilder.FutureJson(),
                TempText = "preserve"
            };

            SaveLoadResult result = SaveServiceTestFactory.Create(repository).LoadOrCreate("new");

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.MainAndBackupInvalid));
            Assert.That(result.BackupFailure.Status, Is.EqualTo(SaveCandidateStatus.FutureVersion));
            Assert.That(repository.Calls, Does.Not.Contain(nameof(FakeSaveRepository.WriteTemp)));
            Assert.That(repository.TempText, Is.EqualTo("preserve"));
        }

        [Test]
        public void SaveCurrentProfile_ValidMain_RotatesThenVerifiesAndPromotes()
        {
            var repository = new FakeSaveRepository { MainText = SaveTestDataBuilder.ValidJson(timestamp: 10) };
            SaveService service = LoadedService(repository, new FixedUnixTimeProvider(50));
            string previousMain = repository.MainText;
            repository.Calls.Clear();

            SaveWriteResult result = service.SaveCurrentProfile();

            Assert.That(result.Status, Is.EqualTo(SaveWriteStatus.Success));
            Assert.That(repository.BackupText, Is.EqualTo(previousMain));
            Assert.That(repository.TempExists, Is.False);
            CollectionAssert.IsOrdered(new[]
            {
                repository.Calls.IndexOf(nameof(FakeSaveRepository.ReadMain)),
                repository.Calls.IndexOf(nameof(FakeSaveRepository.CopyMainToBackup)),
                repository.Calls.IndexOf(nameof(FakeSaveRepository.WriteTemp)),
                repository.Calls.IndexOf(nameof(FakeSaveRepository.ReadTemp)),
                repository.Calls.IndexOf(nameof(FakeSaveRepository.PromoteTempToMain))
            });
            Assert.That(service.GetCurrentProfileSnapshot().LastSavedAtUtcUnixSeconds, Is.EqualTo(50));
        }

        [TestCase(SaveWriteStatus.ExistingMainFutureVersion, true)]
        [TestCase(SaveWriteStatus.ExistingMainUnsupportedVersion, false)]
        public void SaveCurrentProfile_IncompatibleExistingMain_StopsBeforeWrite(SaveWriteStatus expected, bool future)
        {
            var repository = new FakeSaveRepository { MainText = SaveTestDataBuilder.ValidJson() };
            SaveService service = LoadedService(repository);
            repository.MainText = future ? SaveTestDataBuilder.FutureJson() : SaveTestDataBuilder.UnsupportedOlderJson();
            repository.Calls.Clear();

            SaveWriteResult result = service.SaveCurrentProfile();

            Assert.That(result.Status, Is.EqualTo(expected));
            Assert.That(repository.Calls, Does.Not.Contain(nameof(FakeSaveRepository.CopyMainToBackup)));
            Assert.That(repository.Calls, Does.Not.Contain(nameof(FakeSaveRepository.WriteTemp)));
        }

        [Test]
        public void SaveCurrentProfile_CorruptExistingMain_DoesNotOverwriteBackup()
        {
            var repository = new FakeSaveRepository
            {
                MainText = SaveTestDataBuilder.ValidJson(),
                BackupText = SaveTestDataBuilder.ValidJson("known_backup")
            };
            SaveService service = LoadedService(repository);
            repository.MainText = "corrupt";
            string backup = repository.BackupText;
            repository.Calls.Clear();

            SaveWriteResult result = service.SaveCurrentProfile();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(repository.Count(nameof(FakeSaveRepository.CopyMainToBackup)), Is.Zero);
            Assert.That(repository.BackupText, Is.EqualTo(backup));
        }

        [Test]
        public void SaveCurrentProfile_BackupRotationFailure_StopsBeforeTempWrite()
        {
            var repository = new FakeSaveRepository { MainText = SaveTestDataBuilder.ValidJson(), FailCopyMainToBackup = true };
            SaveService service = LoadedService(repository);
            ProfileSaveData before = service.GetCurrentProfileSnapshot();
            repository.Calls.Clear();

            SaveWriteResult result = service.SaveCurrentProfile();

            Assert.That(result.Status, Is.EqualTo(SaveWriteStatus.BackupRotationFailed));
            Assert.That(repository.Calls, Does.Not.Contain(nameof(FakeSaveRepository.WriteTemp)));
            AssertUnchanged(before, service.GetCurrentProfileSnapshot());
        }

        [TestCase("write", SaveWriteStatus.TempWriteFailed)]
        [TestCase("read", SaveWriteStatus.TempReadFailed)]
        [TestCase("promote", SaveWriteStatus.PromoteFailed)]
        public void SaveCurrentProfile_IoFailure_PreservesInternalProfile(string failure, SaveWriteStatus expected)
        {
            var repository = new FakeSaveRepository { MainText = SaveTestDataBuilder.ValidJson() };
            SaveService service = LoadedService(repository, new FixedUnixTimeProvider(999));
            ProfileSaveData before = service.GetCurrentProfileSnapshot();
            repository.FailWriteTemp = failure == "write";
            repository.FailReadTemp = failure == "read";
            repository.FailPromoteTempToMain = failure == "promote";

            SaveWriteResult result = service.SaveCurrentProfile();

            Assert.That(result.Status, Is.EqualTo(expected));
            AssertUnchanged(before, service.GetCurrentProfileSnapshot());
        }

        [TestCase("corrupt")]
        [TestCase("fatal")]
        [TestCase("mismatch")]
        public void SaveCurrentProfile_InvalidTemp_FailsVerificationAndPreservesInternal(string kind)
        {
            var repository = new FakeSaveRepository { MainText = SaveTestDataBuilder.ValidJson() };
            SaveService service = LoadedService(repository, new FixedUnixTimeProvider(999));
            ProfileSaveData before = service.GetCurrentProfileSnapshot();
            repository.TempReadOverride = kind == "corrupt"
                ? "bad"
                : kind == "fatal"
                    ? SaveTestDataBuilder.Json(SaveTestDataBuilder.FatalDuplicateCharacter())
                    : SaveTestDataBuilder.ValidJson("different", 999);

            SaveWriteResult result = service.SaveCurrentProfile();

            Assert.That(result.Status, Is.EqualTo(SaveWriteStatus.TempVerificationFailed));
            Assert.That(repository.Calls, Does.Not.Contain(nameof(FakeSaveRepository.PromoteTempToMain)));
            AssertUnchanged(before, service.GetCurrentProfileSnapshot());
        }

        [Test]
        public void ExecuteTransaction_SuccessUpdatesCopyAndTimestampOnlyAfterSave()
        {
            var repository = new FakeSaveRepository { MainText = SaveTestDataBuilder.ValidJson(timestamp: 10) };
            SaveService service = LoadedService(repository, new FixedUnixTimeProvider(75));

            SaveTransactionResult result = service.ExecuteTransaction(profile => profile.Currencies.GachaCurrency += 5);
            ProfileSaveData current = service.GetCurrentProfileSnapshot();

            Assert.That(result.Status, Is.EqualTo(SaveTransactionStatus.Success));
            Assert.That(current.Currencies.GachaCurrency, Is.EqualTo(5));
            Assert.That(current.CreatedAtUtcUnixSeconds, Is.EqualTo(10));
            Assert.That(current.LastSavedAtUtcUnixSeconds, Is.EqualTo(75));
            Assert.That(new NewtonsoftJsonSaveSerializer().Deserialize(repository.MainText).Currencies.GachaCurrency, Is.EqualTo(5));
        }

        [Test]
        public void ExecuteTransaction_CanChangePartyOnDetachedCopy()
        {
            var repository = new FakeSaveRepository { MainText = SaveTestDataBuilder.ValidJson() };
            SaveService service = LoadedService(repository);

            SaveTransactionResult result = service.ExecuteTransaction(profile =>
            {
                profile.Characters.Add(new CharacterSaveData { CharacterId = "hero_a", Level = 1 });
                profile.Party.Presets[0].CharacterSlotIds[0] = "hero_a";
            });

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(service.GetCurrentProfileSnapshot().Party.Presets[0].CharacterSlotIds[0], Is.EqualTo("hero_a"));
        }

        [Test]
        public void ExecuteTransaction_MutationThrows_PreservesInternalProfile()
        {
            SaveService service = LoadedService(new FakeSaveRepository { MainText = SaveTestDataBuilder.ValidJson() });
            ProfileSaveData before = service.GetCurrentProfileSnapshot();

            SaveTransactionResult result = service.ExecuteTransaction(_ => throw new InvalidOperationException("boom"));

            Assert.That(result.Status, Is.EqualTo(SaveTransactionStatus.MutationThrewException));
            Assert.That(result.Exception, Is.TypeOf<InvalidOperationException>());
            AssertUnchanged(before, service.GetCurrentProfileSnapshot());
        }

        [Test]
        public void ExecuteTransaction_FatalDuplicateCharacter_FailsValidation()
        {
            SaveService service = LoadedService(new FakeSaveRepository { MainText = SaveTestDataBuilder.ValidJson() });

            SaveTransactionResult result = service.ExecuteTransaction(profile =>
            {
                profile.Characters.Add(new CharacterSaveData { CharacterId = "hero_a", Level = 1 });
                profile.Characters.Add(new CharacterSaveData { CharacterId = "hero_a", Level = 1 });
            });

            Assert.That(result.Status, Is.EqualTo(SaveTransactionStatus.ValidationFailed));
            Assert.That(service.GetCurrentProfileSnapshot().Characters, Is.Empty);
        }

        [Test]
        public void ExecuteTransaction_RepairableNegativeValue_IsSanitizedAndSaved()
        {
            SaveService service = LoadedService(new FakeSaveRepository { MainText = SaveTestDataBuilder.ValidJson() });

            SaveTransactionResult result = service.ExecuteTransaction(profile => profile.Currencies.GachaCurrency = -50);

            Assert.That(result.Status, Is.EqualTo(SaveTransactionStatus.Success));
            Assert.That(result.WasSanitized, Is.True);
            Assert.That(service.GetCurrentProfileSnapshot().Currencies.GachaCurrency, Is.Zero);
        }

        [Test]
        public void ExecuteTransaction_SaveFailure_PreservesInternalProfile()
        {
            var repository = new FakeSaveRepository { MainText = SaveTestDataBuilder.ValidJson() };
            SaveService service = LoadedService(repository);
            repository.FailWriteTemp = true;

            SaveTransactionResult result = service.ExecuteTransaction(profile => profile.Currencies.GachaCurrency = 50);

            Assert.That(result.Status, Is.EqualTo(SaveTransactionStatus.SaveFailed));
            Assert.That(service.GetCurrentProfileSnapshot().Currencies.GachaCurrency, Is.Zero);
        }

        [Test]
        public void ExecuteTransaction_WithoutProfile_IsRejected()
        {
            SaveService service = SaveServiceTestFactory.Create(new FakeSaveRepository());

            Assert.That(service.ExecuteTransaction(_ => { }).Status, Is.EqualTo(SaveTransactionStatus.NoCurrentProfile));
            Assert.That(service.SaveCurrentProfile().Status, Is.EqualTo(SaveWriteStatus.NoCurrentProfile));
        }

        [Test]
        public void ExecuteTransaction_NestedTransactionAndSaveAreRejectedImmediately()
        {
            SaveService service = LoadedService(new FakeSaveRepository { MainText = SaveTestDataBuilder.ValidJson() });
            SaveTransactionResult nestedTransaction = null;
            SaveWriteResult nestedSave = null;

            SaveTransactionResult outer = service.ExecuteTransaction(profile =>
            {
                nestedTransaction = service.ExecuteTransaction(_ => { });
                nestedSave = service.SaveCurrentProfile();
                profile.Currencies.GachaCurrency++;
            });

            Assert.That(outer.IsSuccess, Is.True);
            Assert.That(nestedTransaction.Status, Is.EqualTo(SaveTransactionStatus.TransactionAlreadyActive));
            Assert.That(nestedSave.Status, Is.EqualTo(SaveWriteStatus.SaveAlreadyActive));
        }

        [Test]
        public void ExecuteTransaction_RetainedWorkingCopyCannotMutateCommittedProfile()
        {
            SaveService service = LoadedService(new FakeSaveRepository { MainText = SaveTestDataBuilder.ValidJson() });
            ProfileSaveData retained = null;

            SaveTransactionResult result = service.ExecuteTransaction(profile =>
            {
                retained = profile;
                profile.Currencies.GachaCurrency = 5;
            });
            retained.Currencies.GachaCurrency = 999;
            retained.ProfileId = "leaked";

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(service.GetCurrentProfileSnapshot().Currencies.GachaCurrency, Is.EqualTo(5));
            Assert.That(service.GetCurrentProfileSnapshot().ProfileId, Is.EqualTo("profile_test"));
        }

        private static SaveService LoadedService(FakeSaveRepository repository, FixedUnixTimeProvider time = null)
        {
            SaveService service = SaveServiceTestFactory.Create(repository, time ?? new FixedUnixTimeProvider(100));
            SaveLoadResult load = service.LoadOrCreate("ignored");
            Assert.That(load.CanUseProfile, Is.True, load.Message);
            return service;
        }

        private static void AssertUnchanged(ProfileSaveData expected, ProfileSaveData actual)
        {
            Assert.That(actual.ProfileId, Is.EqualTo(expected.ProfileId));
            Assert.That(actual.CreatedAtUtcUnixSeconds, Is.EqualTo(expected.CreatedAtUtcUnixSeconds));
            Assert.That(actual.LastSavedAtUtcUnixSeconds, Is.EqualTo(expected.LastSavedAtUtcUnixSeconds));
            Assert.That(actual.Currencies.GachaCurrency, Is.EqualTo(expected.Currencies.GachaCurrency));
            Assert.That(actual.Characters.Select(value => value.CharacterId),
                Is.EqualTo(expected.Characters.Select(value => value.CharacterId)));
        }
    }
}
