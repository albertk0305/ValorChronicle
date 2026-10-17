using System;
using System.IO;
using NUnit.Framework;
using ValorChronicle.Settings.DTO;
using ValorChronicle.Settings.Repository;
using ValorChronicle.Settings.Serialization;
using ValorChronicle.Settings.Services;
using ValorChronicle.Settings.Validation;

namespace ValorChronicle.Tests.EditMode.Settings
{
    public sealed class SettingsServiceTests
    {
        private string rootDirectory;
        private SettingsPaths paths;
        private NewtonsoftJsonSettingsSerializer serializer;

        [SetUp]
        public void SetUp()
        {
            rootDirectory = Path.Combine(
                Path.GetTempPath(),
                "ValorChronicleSettingsTests",
                Guid.NewGuid().ToString("N"));
            paths = new SettingsPaths(rootDirectory);
            serializer = new NewtonsoftJsonSettingsSerializer();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(rootDirectory))
                Directory.Delete(rootDirectory, true);
        }

        [Test]
        public void FirstLoad_CreatesSeparateDefaultSettingsFile()
        {
            SettingsService service = CreateService();

            SettingsLoadResult result = service.LoadOrCreate();

            Assert.That(result.Status,
                Is.EqualTo(SettingsLoadStatus.CreatedDefaults));
            Assert.That(File.Exists(paths.MainPath), Is.True);
            Assert.That(File.Exists(paths.BackupPath), Is.False);
            Assert.That(File.Exists(paths.TempPath), Is.False);
            Assert.That(service.IsDirty, Is.False);
        }

        [Test]
        public void Paths_UseDedicatedSettingsFileNamesUnderInjectedRoot()
        {
            Assert.That(paths.MainPath,
                Is.EqualTo(Path.Combine(rootDirectory, "settings.json")));
            Assert.That(paths.BackupPath,
                Is.EqualTo(Path.Combine(rootDirectory, "settings.backup.json")));
            Assert.That(paths.TempPath,
                Is.EqualTo(Path.Combine(rootDirectory, "settings.json.tmp")));
            Assert.That(paths.MainPath, Does.Not.Contain("profile"));
        }

        [Test]
        public void Setter_NormalizesEmitsSnapshotAndMarksDirty()
        {
            SettingsService service = CreateService();
            service.LoadOrCreate();
            SettingsChangedEvent observed = null;
            int saveRequests = 0;
            service.SettingsChanged += changed => observed = changed;
            service.SaveRequested += () => saveRequests++;

            bool changedValue = service.SetMasterVolume(-5);

            Assert.That(changedValue, Is.True);
            Assert.That(service.Current.MasterVolume, Is.Zero);
            Assert.That(service.IsDirty, Is.True);
            Assert.That(observed.ChangeKind,
                Is.EqualTo(SettingsChangeKind.MasterVolume));
            Assert.That(observed.Settings.MasterVolume, Is.Zero);
            Assert.That(saveRequests, Is.EqualTo(1));

            GameSettingsData detached = service.Current;
            detached.MasterVolume = 99;
            Assert.That(service.Current.MasterVolume, Is.Zero);
            Assert.That(service.SetMasterVolume(0), Is.False);
            Assert.That(saveRequests, Is.EqualTo(1));
        }

        [Test]
        public void Flush_VerifiesTempRotatesValidMainAndPersistsChange()
        {
            SettingsService service = CreateService();
            service.LoadOrCreate();
            service.SetMasterVolume(42);

            SettingsWriteResult result = service.Flush();

            Assert.That(result.Status, Is.EqualTo(SettingsWriteStatus.Success));
            Assert.That(service.IsDirty, Is.False);
            Assert.That(serializer.Deserialize(File.ReadAllText(paths.MainPath))
                .MasterVolume, Is.EqualTo(42));
            Assert.That(serializer.Deserialize(File.ReadAllText(paths.BackupPath))
                .MasterVolume, Is.EqualTo(100));
            Assert.That(File.Exists(paths.TempPath), Is.False);

            SettingsService restarted = CreateService();
            SettingsLoadResult reload = restarted.LoadOrCreate();
            Assert.That(reload.Status, Is.EqualTo(SettingsLoadStatus.LoadedMain));
            Assert.That(restarted.Current.MasterVolume, Is.EqualTo(42));
        }

        [Test]
        public void CorruptMain_RecoversBackupWithoutOverwritingBackup()
        {
            Directory.CreateDirectory(rootDirectory);
            var backup = new GameSettingsData { MasterVolume = 23 };
            File.WriteAllText(paths.MainPath, "not-json");
            string backupJson = serializer.Serialize(backup);
            File.WriteAllText(paths.BackupPath, backupJson);

            SettingsService service = CreateService();
            SettingsLoadResult result = service.LoadOrCreate();

            Assert.That(result.Status,
                Is.EqualTo(SettingsLoadStatus.RecoveredFromBackup));
            Assert.That(service.Current.MasterVolume, Is.EqualTo(23));
            Assert.That(service.IsDirty, Is.False);
            Assert.That(File.ReadAllText(paths.BackupPath),
                Is.EqualTo(backupJson));
            Assert.That(serializer.Deserialize(File.ReadAllText(paths.MainPath))
                .MasterVolume, Is.EqualTo(23));
        }

        [Test]
        public void InvalidMainAndBackup_UseDefaultsAndPreserveBackup()
        {
            Directory.CreateDirectory(rootDirectory);
            File.WriteAllText(paths.MainPath, "broken-main");
            File.WriteAllText(paths.BackupPath, "broken-backup");

            SettingsLoadResult result = CreateService().LoadOrCreate();

            Assert.That(result.Status,
                Is.EqualTo(SettingsLoadStatus.RecoveredDefaults));
            Assert.That(File.ReadAllText(paths.BackupPath),
                Is.EqualTo("broken-backup"));
            Assert.That(serializer.Deserialize(File.ReadAllText(paths.MainPath))
                .MasterVolume, Is.EqualTo(100));
        }

        [Test]
        public void FailedFlush_KeepsDirtyStateForLaterRetry()
        {
            var repository = new ThrowingWriteRepository();
            var service = new SettingsService(
                repository,
                serializer,
                new SettingsValidator());
            service.SetSfxVolume(12);

            SettingsWriteResult result = service.Flush();

            Assert.That(result.Status,
                Is.EqualTo(SettingsWriteStatus.TempWriteFailed));
            Assert.That(service.IsDirty, Is.True);
        }

        [Test]
        public void FailedWrite_PreservesValidMainAndCanBeRetried()
        {
            SettingsService initial = CreateService();
            initial.LoadOrCreate();
            string originalMain = File.ReadAllText(paths.MainPath);
            var repository = new FailOnceWriteRepository(
                new SettingsRepository(paths));
            var service = new SettingsService(
                repository,
                serializer,
                new SettingsValidator());
            service.LoadOrCreate();
            service.SetBgmVolume(37);

            SettingsWriteResult failed = service.Flush();

            Assert.That(failed.Status,
                Is.EqualTo(SettingsWriteStatus.TempWriteFailed));
            Assert.That(File.ReadAllText(paths.MainPath),
                Is.EqualTo(originalMain));
            Assert.That(service.IsDirty, Is.True);

            SettingsWriteResult retried = service.Flush();
            Assert.That(retried.Status, Is.EqualTo(SettingsWriteStatus.Success));
            Assert.That(service.IsDirty, Is.False);
            Assert.That(serializer.Deserialize(File.ReadAllText(paths.MainPath))
                .BgmVolume, Is.EqualTo(37));
        }

        private SettingsService CreateService()
        {
            return new SettingsService(
                new SettingsRepository(paths),
                serializer,
                new SettingsValidator());
        }

        private sealed class ThrowingWriteRepository : ISettingsRepository
        {
            public bool MainExists => false;
            public bool BackupExists => false;
            public bool TempExists => false;
            public string ReadMain() => throw new FileNotFoundException();
            public string ReadBackup() => throw new FileNotFoundException();
            public string ReadTemp() => throw new FileNotFoundException();
            public void WriteTemp(string contents) =>
                throw new IOException("Expected test failure.");
            public void CopyMainToBackup() =>
                throw new InvalidOperationException();
            public void PromoteTempToMain() =>
                throw new InvalidOperationException();
            public void DeleteTempIfExists() { }
        }

        private sealed class FailOnceWriteRepository : ISettingsRepository
        {
            private readonly ISettingsRepository inner;
            private bool shouldFail = true;

            public FailOnceWriteRepository(ISettingsRepository inner)
            {
                this.inner = inner;
            }

            public bool MainExists => inner.MainExists;
            public bool BackupExists => inner.BackupExists;
            public bool TempExists => inner.TempExists;
            public string ReadMain() => inner.ReadMain();
            public string ReadBackup() => inner.ReadBackup();
            public string ReadTemp() => inner.ReadTemp();

            public void WriteTemp(string contents)
            {
                if (shouldFail)
                {
                    shouldFail = false;
                    throw new IOException("Expected first-write failure.");
                }

                inner.WriteTemp(contents);
            }

            public void CopyMainToBackup() => inner.CopyMainToBackup();
            public void PromoteTempToMain() => inner.PromoteTempToMain();
            public void DeleteTempIfExists() => inner.DeleteTempIfExists();
        }
    }
}
