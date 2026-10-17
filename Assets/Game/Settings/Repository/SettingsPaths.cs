using System;
using System.IO;

namespace ValorChronicle.Settings.Repository
{
    public sealed class SettingsPaths
    {
        private const string MainFileName = "settings.json";
        private const string BackupFileName = "settings.backup.json";
        private const string TempFileName = "settings.json.tmp";

        public SettingsPaths(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException(
                    "Settings root directory cannot be empty or whitespace.",
                    nameof(rootDirectory));
            }

            RootDirectory = Path.GetFullPath(rootDirectory);
            MainPath = Path.Combine(RootDirectory, MainFileName);
            BackupPath = Path.Combine(RootDirectory, BackupFileName);
            TempPath = Path.Combine(RootDirectory, TempFileName);
        }

        public string RootDirectory { get; }
        public string MainPath { get; }
        public string BackupPath { get; }
        public string TempPath { get; }
    }
}
