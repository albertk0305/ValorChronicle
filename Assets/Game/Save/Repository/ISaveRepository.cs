namespace ValorChronicle.Save.Repository
{
    /// <summary>
    /// Defines profile save file operations without serialization or save policy decisions.
    /// </summary>
    public interface ISaveRepository
    {
        /// <summary>Gets whether the main profile file exists.</summary>
        bool MainExists { get; }
        /// <summary>Gets whether the backup profile file exists.</summary>
        bool BackupExists { get; }
        /// <summary>Gets whether the temporary profile file exists.</summary>
        bool TempExists { get; }

        /// <summary>Creates the save root directory when it does not exist.</summary>
        void EnsureRootDirectory();
        /// <summary>Reads all text from the main profile file.</summary>
        string ReadMain();
        /// <summary>Reads all text from the backup profile file.</summary>
        string ReadBackup();
        /// <summary>Reads all text from the temporary profile file.</summary>
        string ReadTemp();
        /// <summary>Writes UTF-8 text to the temporary profile file.</summary>
        void WriteTemp(string contents);
        /// <summary>Copies the caller-validated main file into the backup location via temp.</summary>
        void CopyMainToBackup();
        /// <summary>Moves or replaces the temporary profile file into the main location.</summary>
        void PromoteTempToMain();
        /// <summary>Deletes the temporary profile file when present.</summary>
        void DeleteTempIfExists();
    }
}
