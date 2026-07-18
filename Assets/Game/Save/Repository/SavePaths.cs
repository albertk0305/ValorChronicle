using System;
using System.IO;

namespace ValorChronicle.Save.Repository
{
    public sealed class SavePaths
    {
        private const string MainFileName = "profile.save";
        private const string BackupFileName = "profile.backup.save";
        private const string TempFileName = "profile.save.tmp";

        public SavePaths(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException("Save root directory cannot be empty or whitespace.", nameof(rootDirectory));
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
