using System;
using System.Collections.Generic;
using ValorChronicle.Save.Copying;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Save.Migration
{
    /// <summary>
    /// Applies registered one-version migration steps to deep copies of save data.
    /// </summary>
    public sealed class SaveMigrationRunner
    {
        private readonly SaveDataCloner cloner;
        private readonly Dictionary<int, ISaveMigrationStep> stepsByFromVersion;

        /// <summary>
        /// Creates a migration runner and validates the registered step chain shape.
        /// </summary>
        /// <param name="cloner">The cloner used to protect caller-owned data.</param>
        /// <param name="steps">Optional one-version migration steps.</param>
        /// <exception cref="ArgumentNullException">Thrown when the cloner or step collection is null.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when a step is null, duplicates a FromVersion, or does not advance exactly one version.
        /// </exception>
        public SaveMigrationRunner(
            SaveDataCloner cloner,
            IEnumerable<ISaveMigrationStep> steps)
        {
            this.cloner = cloner ?? throw new ArgumentNullException(nameof(cloner));

            if (steps == null)
            {
                throw new ArgumentNullException(nameof(steps));
            }

            stepsByFromVersion = new Dictionary<int, ISaveMigrationStep>();

            foreach (ISaveMigrationStep step in steps)
            {
                Register(step);
            }
        }

        /// <summary>
        /// Migrates a profile copy to the current schema version without changing the source.
        /// </summary>
        /// <param name="source">The profile to inspect and migrate.</param>
        /// <returns>A typed migration result. Failed results never expose partially migrated data.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public SaveMigrationResult Migrate(ProfileSaveData source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            int sourceVersion = source.SaveVersion;

            if (sourceVersion > SaveRules.CurrentSaveVersion)
            {
                return Failure(
                    SaveMigrationStatus.FutureVersion,
                    sourceVersion,
                    "The save was created by a newer schema version.");
            }

            if (sourceVersion == SaveRules.CurrentSaveVersion)
            {
                return Success(sourceVersion, cloner.Clone(source));
            }

            if (stepsByFromVersion.Count == 0)
            {
                return Failure(
                    SaveMigrationStatus.UnsupportedOlderVersion,
                    sourceVersion,
                    "No older save versions are currently supported.");
            }

            ProfileSaveData working = cloner.Clone(source);
            int currentVersion = sourceVersion;

            while (currentVersion < SaveRules.CurrentSaveVersion)
            {
                if (!stepsByFromVersion.TryGetValue(currentVersion, out ISaveMigrationStep step))
                {
                    return Failure(
                        SaveMigrationStatus.MissingMigrationStep,
                        sourceVersion,
                        $"No migration step is registered from version {currentVersion}.");
                }

                try
                {
                    ProfileSaveData stepInput = cloner.Clone(working);
                    ProfileSaveData migrated = step.Migrate(stepInput);

                    if (migrated == null)
                    {
                        return Failure(
                            SaveMigrationStatus.MigrationFailed,
                            sourceVersion,
                            $"Migration step {step.FromVersion}->{step.ToVersion} returned null.");
                    }

                    if (migrated.SaveVersion != step.ToVersion)
                    {
                        return Failure(
                            SaveMigrationStatus.MigrationFailed,
                            sourceVersion,
                            $"Migration step {step.FromVersion}->{step.ToVersion} " +
                            $"returned save version {migrated.SaveVersion}.");
                    }

                    working = cloner.Clone(migrated);
                    currentVersion = step.ToVersion;
                }
                catch (Exception exception)
                {
                    return new SaveMigrationResult(
                        SaveMigrationStatus.MigrationFailed,
                        sourceVersion,
                        SaveRules.CurrentSaveVersion,
                        null,
                        $"Migration step {step.FromVersion}->{step.ToVersion} failed.",
                        exception);
                }
            }

            return Success(sourceVersion, working);
        }

        private void Register(ISaveMigrationStep step)
        {
            if (step == null)
            {
                throw new ArgumentException("Migration steps cannot contain null.", "steps");
            }

            if (step.ToVersion != step.FromVersion + 1)
            {
                throw new ArgumentException(
                    $"Migration step {step.FromVersion}->{step.ToVersion} must advance one version.",
                    "steps");
            }

            if (stepsByFromVersion.ContainsKey(step.FromVersion))
            {
                throw new ArgumentException(
                    $"A migration step from version {step.FromVersion} is already registered.",
                    "steps");
            }

            stepsByFromVersion.Add(step.FromVersion, step);
        }

        private static SaveMigrationResult Success(int sourceVersion, ProfileSaveData data)
        {
            return new SaveMigrationResult(
                SaveMigrationStatus.Success,
                sourceVersion,
                SaveRules.CurrentSaveVersion,
                data,
                string.Empty,
                null);
        }

        private static SaveMigrationResult Failure(
            SaveMigrationStatus status,
            int sourceVersion,
            string message)
        {
            return new SaveMigrationResult(
                status,
                sourceVersion,
                SaveRules.CurrentSaveVersion,
                null,
                message,
                null);
        }
    }
}
