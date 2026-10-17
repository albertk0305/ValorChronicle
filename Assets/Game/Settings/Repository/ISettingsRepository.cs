namespace ValorChronicle.Settings.Repository
{
    public interface ISettingsRepository
    {
        bool MainExists { get; }
        bool BackupExists { get; }
        bool TempExists { get; }

        string ReadMain();
        string ReadBackup();
        string ReadTemp();
        void WriteTemp(string contents);
        void CopyMainToBackup();
        void PromoteTempToMain();
        void DeleteTempIfExists();
    }
}
