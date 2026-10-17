using System;
using System.IO;
using System.Text;

namespace ValorChronicle.Settings.Repository
{
    public sealed class SettingsRepository : ISettingsRepository
    {
        private static readonly Encoding Utf8WithoutBom =
            new UTF8Encoding(false);
        private readonly SettingsPaths paths;

        public SettingsRepository(SettingsPaths paths)
        {
            this.paths = paths
                ?? throw new ArgumentNullException(nameof(paths));
        }

        public bool MainExists => File.Exists(paths.MainPath);
        public bool BackupExists => File.Exists(paths.BackupPath);
        public bool TempExists => File.Exists(paths.TempPath);

        public string ReadMain() =>
            File.ReadAllText(paths.MainPath, Utf8WithoutBom);
        public string ReadBackup() =>
            File.ReadAllText(paths.BackupPath, Utf8WithoutBom);
        public string ReadTemp() =>
            File.ReadAllText(paths.TempPath, Utf8WithoutBom);

        public void WriteTemp(string contents)
        {
            if (contents == null)
                throw new ArgumentNullException(nameof(contents));

            EnsureRootDirectory();
            using var stream = new FileStream(
                paths.TempPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);
            using var writer = new StreamWriter(stream, Utf8WithoutBom);
            writer.Write(contents);
            writer.Flush();
            stream.Flush(true);
        }

        public void CopyMainToBackup()
        {
            if (!MainExists)
            {
                throw new FileNotFoundException(
                    "Cannot back up a missing settings file.",
                    paths.MainPath);
            }

            EnsureRootDirectory();
            DeleteTempIfExists();
            File.Copy(paths.MainPath, paths.TempPath);
            if (BackupExists)
                File.Replace(paths.TempPath, paths.BackupPath, null);
            else
                File.Move(paths.TempPath, paths.BackupPath);
        }

        public void PromoteTempToMain()
        {
            if (!TempExists)
            {
                throw new FileNotFoundException(
                    "Cannot promote a missing temporary settings file.",
                    paths.TempPath);
            }

            EnsureRootDirectory();
            if (MainExists)
                File.Replace(paths.TempPath, paths.MainPath, null);
            else
                File.Move(paths.TempPath, paths.MainPath);
        }

        public void DeleteTempIfExists()
        {
            if (TempExists)
                File.Delete(paths.TempPath);
        }

        private void EnsureRootDirectory()
        {
            Directory.CreateDirectory(paths.RootDirectory);
        }
    }
}
