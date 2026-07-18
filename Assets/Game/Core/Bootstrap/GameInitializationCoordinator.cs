using System;
using System.Threading.Tasks;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Core.Bootstrap
{
    /// <summary>Coordinates testable game initialization phases independently of MonoBehaviour lifecycle.</summary>
    public sealed class GameInitializationCoordinator
    {
        private readonly Action initializeContent;
        private readonly Func<bool> validateContent;
        private readonly Func<SaveService> createSaveService;
        private readonly IProfileIdProvider profileIdProvider;
        private readonly Func<Task<bool>> loadMainScene;

        /// <summary>Creates an initialization coordinator with explicit phase boundaries.</summary>
        public GameInitializationCoordinator(
            Action initializeContent,
            Func<bool> validateContent,
            Func<SaveService> createSaveService,
            IProfileIdProvider profileIdProvider,
            Func<Task<bool>> loadMainScene)
        {
            this.initializeContent = initializeContent ?? throw new ArgumentNullException(nameof(initializeContent));
            this.validateContent = validateContent ?? throw new ArgumentNullException(nameof(validateContent));
            this.createSaveService = createSaveService ?? throw new ArgumentNullException(nameof(createSaveService));
            this.profileIdProvider = profileIdProvider ?? throw new ArgumentNullException(nameof(profileIdProvider));
            this.loadMainScene = loadMainScene ?? throw new ArgumentNullException(nameof(loadMainScene));
        }

        /// <summary>Gets the composed save service after its composition phase succeeds.</summary>
        public SaveService SaveService { get; private set; }

        /// <summary>Runs content, save, and Main scene initialization in the required order.</summary>
        public async Task<BootstrapInitializationResult> InitializeAsync()
        {
            try
            {
                initializeContent();
            }
            catch (Exception exception)
            {
                return Failure(
                    BootstrapInitializationStatus.ContentInitializationFailed,
                    "Content initialization failed.",
                    exception);
            }

            bool contentIsValid;
            try
            {
                contentIsValid = validateContent();
            }
            catch (Exception exception)
            {
                return Failure(
                    BootstrapInitializationStatus.ContentValidationFailed,
                    "Content validation failed.",
                    exception);
            }

            if (!contentIsValid)
            {
                return Failure(
                    BootstrapInitializationStatus.ContentValidationFailed,
                    "Content validation reported errors.");
            }

            SaveLoadResult saveLoadResult;
            try
            {
                SaveService = createSaveService();
                if (SaveService == null)
                    throw new InvalidOperationException("Save service composition returned null.");

                string newProfileId = profileIdProvider.CreateProfileId();
                if (string.IsNullOrWhiteSpace(newProfileId))
                    throw new InvalidOperationException("Profile ID provider returned an empty identifier.");

                saveLoadResult = SaveService.LoadOrCreate(newProfileId);
            }
            catch (Exception exception)
            {
                return Failure(
                    BootstrapInitializationStatus.SaveInitializationFailed,
                    "Save initialization threw an exception.",
                    exception);
            }

            if (!CanEnterMain(saveLoadResult, SaveService))
            {
                return new BootstrapInitializationResult(
                    BootstrapInitializationStatus.SaveInitializationFailed,
                    saveLoadResult,
                    saveLoadResult?.Exception,
                    "Save initialization did not produce a writable profile.");
            }

            try
            {
                if (!await loadMainScene())
                {
                    return new BootstrapInitializationResult(
                        BootstrapInitializationStatus.SceneLoadFailed,
                        saveLoadResult,
                        message: "Main scene loading did not complete.");
                }
            }
            catch (Exception exception)
            {
                return new BootstrapInitializationResult(
                    BootstrapInitializationStatus.SceneLoadFailed,
                    saveLoadResult,
                    exception,
                    "Main scene loading threw an exception.");
            }

            return new BootstrapInitializationResult(
                BootstrapInitializationStatus.Success,
                saveLoadResult);
        }

        private static bool CanEnterMain(SaveLoadResult result, SaveService saveService)
        {
            if (result == null || saveService == null || !result.IsSuccess)
                return false;

            bool acceptedStatus = result.Status == SaveLoadStatus.LoadedMain ||
                                  result.Status == SaveLoadStatus.LoadedAndRepairedMain ||
                                  result.Status == SaveLoadStatus.CreatedNewProfile ||
                                  result.Status == SaveLoadStatus.RecoveredFromBackup;

            return acceptedStatus &&
                   result.CanUseProfile &&
                   result.CanWriteProfile &&
                   saveService.HasCurrentProfile &&
                   saveService.CanWriteCurrentProfile;
        }

        private static BootstrapInitializationResult Failure(
            BootstrapInitializationStatus status,
            string message,
            Exception exception = null)
        {
            return new BootstrapInitializationResult(status, exception: exception, message: message);
        }
    }
}
