namespace ValorChronicle.Save.Migration
{
    /// <summary>
    /// Describes the outcome of a save migration attempt.
    /// </summary>
    public enum SaveMigrationStatus
    {
        /// <summary>The profile is at the current version after migration.</summary>
        Success = 0,
        /// <summary>The profile belongs to a schema newer than this application supports.</summary>
        FutureVersion = 1,
        /// <summary>No migrations are registered for the supplied older version.</summary>
        UnsupportedOlderVersion = 2,
        /// <summary>A required step in a partially registered migration chain is missing.</summary>
        MissingMigrationStep = 3,
        /// <summary>A migration step threw or returned an invalid result.</summary>
        MigrationFailed = 4
    }
}
