using ValorChronicle.Save.DTO;

namespace ValorChronicle.Save.Migration
{
    /// <summary>
    /// Migrates one save schema version to its immediately following version.
    /// </summary>
    public interface ISaveMigrationStep
    {
        /// <summary>Gets the schema version accepted by this step.</summary>
        int FromVersion { get; }
        /// <summary>Gets the schema version produced by this step.</summary>
        int ToVersion { get; }

        /// <summary>
        /// Migrates the supplied working copy to <see cref="ToVersion"/>.
        /// </summary>
        /// <param name="source">A working copy owned by the migration runner.</param>
        /// <returns>The migrated profile.</returns>
        ProfileSaveData Migrate(ProfileSaveData source);
    }
}
