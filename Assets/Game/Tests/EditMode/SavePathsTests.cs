using System;
using System.IO;
using NUnit.Framework;
using ValorChronicle.Save.Repository;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class SavePathsTests
    {
        [Test]
        public void Constructor_BuildsFixedPathsUnderRoot()
        {
            string root = Path.Combine(Path.GetTempPath(), "valor_chronicle_paths", Guid.NewGuid().ToString("N"));

            var paths = new SavePaths(root);

            Assert.That(paths.RootDirectory, Is.EqualTo(Path.GetFullPath(root)));
            Assert.That(paths.MainPath, Is.EqualTo(Path.Combine(paths.RootDirectory, "profile.save")));
            Assert.That(paths.BackupPath, Is.EqualTo(Path.Combine(paths.RootDirectory, "profile.backup.save")));
            Assert.That(paths.TempPath, Is.EqualTo(Path.Combine(paths.RootDirectory, "profile.save.tmp")));
            Assert.That(Path.GetDirectoryName(paths.MainPath), Is.EqualTo(paths.RootDirectory));
            Assert.That(Path.GetDirectoryName(paths.BackupPath), Is.EqualTo(paths.RootDirectory));
            Assert.That(Path.GetDirectoryName(paths.TempPath), Is.EqualTo(paths.RootDirectory));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_RejectsInvalidRoot(string root)
        {
            Assert.Throws<ArgumentException>(() => new SavePaths(root));
        }
    }
}
