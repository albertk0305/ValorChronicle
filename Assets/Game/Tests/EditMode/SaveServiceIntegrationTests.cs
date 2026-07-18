using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Repository;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class SaveServiceIntegrationTests
    {
        private readonly List<string> roots = new List<string>();

        [TearDown]
        public void TearDown()
        {
            foreach (string root in roots)
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
            roots.Clear();
        }

        [Test]
        public void RealRepository_CreateRotateAndReload_PreservesPreviousMainAsBackup()
        {
            (SaveRepository repository, SavePaths paths) = CreateRepository();
            SaveService first = SaveServiceTestFactory.Create(repository, new FixedUnixTimeProvider(10, 20));

            SaveLoadResult created = first.LoadOrCreate("profile_real");
            string firstMain = File.ReadAllText(paths.MainPath);
            SaveTransactionResult saved = first.ExecuteTransaction(profile => profile.Currencies.GachaCurrency = 7);

            Assert.That(created.Status, Is.EqualTo(SaveLoadStatus.CreatedNewProfile));
            Assert.That(saved.Status, Is.EqualTo(SaveTransactionStatus.Success));
            Assert.That(File.Exists(paths.MainPath), Is.True);
            Assert.That(File.Exists(paths.BackupPath), Is.True);
            Assert.That(File.ReadAllText(paths.BackupPath), Is.EqualTo(firstMain));

            SaveService second = SaveServiceTestFactory.Create(repository, new FixedUnixTimeProvider(30));
            SaveLoadResult reloaded = second.LoadOrCreate("ignored");
            Assert.That(reloaded.Status, Is.EqualTo(SaveLoadStatus.LoadedMain));
            Assert.That(reloaded.ProfileSnapshot.Currencies.GachaCurrency, Is.EqualTo(7));
        }

        [Test]
        public void RealRepository_CorruptMain_RecoversKnownGoodBackup()
        {
            (SaveRepository repository, SavePaths paths) = CreateRepository();
            SaveService first = SaveServiceTestFactory.Create(repository, new FixedUnixTimeProvider(10, 20));
            Assert.That(first.LoadOrCreate("profile_real").IsSuccess, Is.True);
            Assert.That(first.ExecuteTransaction(profile => profile.Currencies.GachaCurrency = 7).IsSuccess, Is.True);
            File.WriteAllText(paths.MainPath, "corrupt-main");

            SaveService second = SaveServiceTestFactory.Create(repository, new FixedUnixTimeProvider(30));
            SaveLoadResult recovered = second.LoadOrCreate("ignored");

            Assert.That(recovered.Status, Is.EqualTo(SaveLoadStatus.RecoveredFromBackup));
            Assert.That(recovered.ProfileSnapshot.Currencies.GachaCurrency, Is.Zero);
            Assert.That(second.CanWriteCurrentProfile, Is.True);
            Assert.That(File.Exists(paths.TempPath), Is.False);
        }

        [Test]
        public void RealRepository_PartyMutation_PersistsAcrossServiceInstances()
        {
            (SaveRepository repository, _) = CreateRepository();
            SaveService first = SaveServiceTestFactory.Create(
                repository,
                new FixedUnixTimeProvider(10, 20));
            Assert.That(first.LoadOrCreate("profile_party").IsSuccess, Is.True);

            SaveTransactionResult transaction = first.ExecuteTransaction(profile =>
            {
                profile.Characters.Add(new CharacterSaveData
                {
                    CharacterId = "character_party_test",
                    Level = 1
                });
                profile.Party.Presets[0].CharacterSlotIds[0] =
                    "character_party_test";
            });

            Assert.That(transaction.IsSuccess, Is.True);
            SaveService second = SaveServiceTestFactory.Create(
                repository,
                new FixedUnixTimeProvider(30));
            Assert.That(second.LoadOrCreate("ignored").Status,
                Is.EqualTo(SaveLoadStatus.LoadedMain));
            Assert.That(
                second.GetCurrentProfileSnapshot().Party.Presets[0].CharacterSlotIds[0],
                Is.EqualTo("character_party_test"));
        }

        private (SaveRepository Repository, SavePaths Paths) CreateRepository()
        {
            string root = Path.Combine(Path.GetTempPath(), "valor_chronicle_service_tests", Guid.NewGuid().ToString("N"));
            roots.Add(root);
            var paths = new SavePaths(root);
            return (new SaveRepository(paths), paths);
        }
    }
}
