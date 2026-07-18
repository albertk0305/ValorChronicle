using System;
using ValorChronicle.Data.Database;
using ValorChronicle.Save.Copying;
using ValorChronicle.Save.Migration;
using ValorChronicle.Save.Normalization;
using ValorChronicle.Save.Processing;
using ValorChronicle.Save.Repository;
using ValorChronicle.Save.Sanitization;
using ValorChronicle.Save.Serialization;
using ValorChronicle.Save.Validation;

namespace ValorChronicle.Save.Services
{
    /// <summary>Builds the production save dependency graph without performing load or save operations.</summary>
    public static class SaveSystemFactory
    {
        /// <summary>Creates a save service rooted at the supplied persistent data directory.</summary>
        public static SaveService Create(
            string persistentDataPath,
            DefinitionDatabase definitionDatabase)
        {
            if (string.IsNullOrWhiteSpace(persistentDataPath))
                throw new ArgumentException("Persistent data path cannot be empty or whitespace.", nameof(persistentDataPath));
            if (definitionDatabase == null)
                throw new ArgumentNullException(nameof(definitionDatabase));

            var cloner = new SaveDataCloner();
            var structuralValidator = new SaveStructuralValidator();
            var referenceValidator = new SaveReferenceValidator();
            var normalizer = new SaveNormalizer(cloner);
            var sanitizer = new SaveSanitizer(cloner, structuralValidator, referenceValidator);
            var validationProcessor = new SaveValidationProcessor(
                normalizer,
                structuralValidator,
                referenceValidator,
                sanitizer);

            return new SaveService(
                new SaveRepository(new SavePaths(persistentDataPath)),
                new NewtonsoftJsonSaveSerializer(),
                new NewProfileFactory(),
                cloner,
                new SaveMigrationRunner(cloner, Array.Empty<ISaveMigrationStep>()),
                validationProcessor,
                new DefinitionDatabaseSaveContentCatalog(definitionDatabase),
                new SystemUnixTimeProvider());
        }
    }
}
