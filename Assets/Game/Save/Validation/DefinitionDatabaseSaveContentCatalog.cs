using System;
using ValorChronicle.Data.Database;

namespace ValorChronicle.Save.Validation
{
    /// <summary>Resolves save references through an initialized production definition database.</summary>
    public sealed class DefinitionDatabaseSaveContentCatalog : ISaveContentCatalog
    {
        private readonly DefinitionDatabase database;

        /// <summary>Creates an adapter over an initialized definition database.</summary>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="database"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the database is not initialized.</exception>
        public DefinitionDatabaseSaveContentCatalog(DefinitionDatabase database)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
            if (!database.IsInitialized)
            {
                throw new InvalidOperationException(
                    "DefinitionDatabase must be initialized before creating the save content catalog.");
            }
        }

        /// <inheritdoc />
        public SaveContentLookupResult LookupCharacter(string characterId) =>
            database.TryGetCharacter(characterId, out _)
                ? SaveContentLookupResult.Exists
                : SaveContentLookupResult.Missing;

        /// <inheritdoc />
        public SaveContentLookupResult LookupRelic(string relicDefinitionId) =>
            database.TryGetRelic(relicDefinitionId, out _)
                ? SaveContentLookupResult.Exists
                : SaveContentLookupResult.Missing;

        /// <inheritdoc />
        public SaveContentLookupResult LookupBoss(string bossId) =>
            database.TryGetBoss(bossId, out _)
                ? SaveContentLookupResult.Exists
                : SaveContentLookupResult.Missing;

        /// <inheritdoc />
        public SaveContentLookupResult LookupBossDifficulty(string bossId, string difficultyId) =>
            SaveContentLookupResult.Unavailable;

        /// <inheritdoc />
        public SaveContentLookupResult LookupGacha(string gachaId) =>
            SaveContentLookupResult.Unavailable;

        /// <inheritdoc />
        public SaveContentLookupResult LookupRewardGrade(string bossId, string difficultyId, string gradeId) =>
            SaveContentLookupResult.Unavailable;
    }
}
