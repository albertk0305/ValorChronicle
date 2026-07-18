using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ValorChronicle.Save.Repository;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class SaveRepositoryTests
    {
        private readonly List<string> roots = new List<string>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < roots.Count; i++)
            {
                if (Directory.Exists(roots[i]))
                {
                    Directory.Delete(roots[i], true);
                }
            }

            roots.Clear();
        }

        [Test]
        public void Repository_ImplementsSaveRepositoryInterface()
        {
            (SaveRepository repository, _) = CreateRepository();

            Assert.That(repository, Is.InstanceOf<ISaveRepository>());
        }

        [Test]
        public void EnsureRootDirectory_CreatesInjectedRoot()
        {
            (SaveRepository repository, SavePaths paths) = CreateRepository();

            repository.EnsureRootDirectory();

            Assert.That(Directory.Exists(paths.RootDirectory), Is.True);
        }

        [Test]
        public void WriteTemp_WritesReadableUtf8WithoutBom()
        {
            (SaveRepository repository, SavePaths paths) = CreateRepository();
            const string contents = "{\"message\":\"valor chronicle 한글\"}";

            repository.WriteTemp(contents);
            byte[] bytes = File.ReadAllBytes(paths.TempPath);

            Assert.That(repository.TempExists, Is.True);
            Assert.That(repository.ReadTemp(), Is.EqualTo(contents));
            Assert.That(bytes, Has.Length.GreaterThan(3));
            Assert.That(bytes[0], Is.Not.EqualTo(0xEF));
        }

        [Test]
        public void PromoteTempToMain_CreatesFirstMainAndRemovesTemp()
        {
            (SaveRepository repository, _) = CreateRepository();
            repository.WriteTemp("first");

            repository.PromoteTempToMain();

            Assert.That(repository.MainExists, Is.True);
            Assert.That(repository.ReadMain(), Is.EqualTo("first"));
            Assert.That(repository.TempExists, Is.False);
        }

        [Test]
        public void PromoteTempToMain_ReplacesExistingMain()
        {
            (SaveRepository repository, _) = CreateRepository();
            repository.WriteTemp("first");
            repository.PromoteTempToMain();
            repository.WriteTemp("second");

            repository.PromoteTempToMain();

            Assert.That(repository.ReadMain(), Is.EqualTo("second"));
            Assert.That(repository.TempExists, Is.False);
        }

        [Test]
        public void CopyMainToBackup_CreatesAndReplacesBackup()
        {
            (SaveRepository repository, _) = CreateRepository();
            repository.WriteTemp("first");
            repository.PromoteTempToMain();
            repository.CopyMainToBackup();

            Assert.That(repository.BackupExists, Is.True);
            Assert.That(repository.ReadBackup(), Is.EqualTo("first"));
            Assert.That(repository.TempExists, Is.False);

            repository.WriteTemp("second");
            repository.PromoteTempToMain();
            repository.CopyMainToBackup();

            Assert.That(repository.ReadBackup(), Is.EqualTo("second"));
            Assert.That(repository.TempExists, Is.False);
        }

        [Test]
        public void CopyMainToBackup_ReplacesStaleTempWithMainContent()
        {
            (SaveRepository repository, _) = CreateRepository();
            repository.WriteTemp("main-content");
            repository.PromoteTempToMain();
            repository.WriteTemp("stale-temp");

            repository.CopyMainToBackup();

            Assert.That(repository.ReadBackup(), Is.EqualTo("main-content"));
            Assert.That(repository.TempExists, Is.False);
        }

        [Test]
        public void CopyMainToBackup_AllowsNewCandidateToReuseTempPath()
        {
            (SaveRepository repository, _) = CreateRepository();
            repository.WriteTemp("main-content");
            repository.PromoteTempToMain();
            repository.CopyMainToBackup();

            repository.WriteTemp("new-candidate");

            Assert.That(repository.ReadBackup(), Is.EqualTo("main-content"));
            Assert.That(repository.ReadTemp(), Is.EqualTo("new-candidate"));
        }

        [Test]
        public void PromoteTempToMain_DoesNotChangeBackup()
        {
            (SaveRepository repository, _) = CreateRepository();
            repository.WriteTemp("known-good");
            repository.PromoteTempToMain();
            repository.CopyMainToBackup();
            repository.WriteTemp("new-main");

            repository.PromoteTempToMain();

            Assert.That(repository.ReadMain(), Is.EqualTo("new-main"));
            Assert.That(repository.ReadBackup(), Is.EqualTo("known-good"));
        }

        [Test]
        public void PromoteTempToMain_ThrowsWhenTempIsMissing()
        {
            (SaveRepository repository, _) = CreateRepository();

            Assert.Throws<FileNotFoundException>(() => repository.PromoteTempToMain());
        }

        [Test]
        public void CopyMainToBackup_ThrowsWhenMainIsMissing()
        {
            (SaveRepository repository, _) = CreateRepository();

            Assert.Throws<FileNotFoundException>(() => repository.CopyMainToBackup());
        }

        [Test]
        public void DeleteTempIfExists_IsSafeWhenTempIsMissing()
        {
            (SaveRepository repository, _) = CreateRepository();

            Assert.DoesNotThrow(() => repository.DeleteTempIfExists());
            repository.WriteTemp("temporary");
            Assert.DoesNotThrow(() => repository.DeleteTempIfExists());
            Assert.That(repository.TempExists, Is.False);
        }

        [Test]
        public void Repositories_UseIndependentInjectedRoots()
        {
            (SaveRepository first, SavePaths firstPaths) = CreateRepository();
            (SaveRepository second, SavePaths secondPaths) = CreateRepository();

            first.WriteTemp("first");
            second.WriteTemp("second");

            Assert.That(firstPaths.RootDirectory, Is.Not.EqualTo(secondPaths.RootDirectory));
            Assert.That(first.ReadTemp(), Is.EqualTo("first"));
            Assert.That(second.ReadTemp(), Is.EqualTo("second"));
        }

        private (SaveRepository Repository, SavePaths Paths) CreateRepository()
        {
            string root = Path.Combine(
                Path.GetTempPath(),
                "valor_chronicle_save_tests",
                Guid.NewGuid().ToString("N"));
            roots.Add(root);
            var paths = new SavePaths(root);
            return (new SaveRepository(paths), paths);
        }
    }
}
