using System.Threading.Tasks;
using UnityEngine;
using ValorChronicle.Core.Logging;
using ValorChronicle.Core.Random;
using ValorChronicle.Core.Scene;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Validation;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Core.Bootstrap
{
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [SerializeField]
        private DefinitionDatabase definitionDatabase;

        public static GameBootstrapper Instance { get; private set; }

        public SceneService SceneService { get; private set; }
        public DefinitionDatabase DefinitionDatabase => definitionDatabase;
        public IRandomSource RandomSource { get; private set; }
        /// <summary>Gets the initialized save service for persistent game-domain operations.</summary>
        public SaveService SaveService { get; private set; }
        /// <summary>Gets the most recent bootstrap result, or null while initialization is pending.</summary>
        public BootstrapInitializationResult InitializationResult { get; private set; }

        private async void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            SceneService = new SceneService();
            RandomSource = new UnityRandomSource();

            try
            {
                InitializationResult = await InitializeAsync();
                HandleInitializationResult(InitializationResult);
            }
            catch (System.Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error("[Bootstrap] Unexpected initialization failure.", this);
            }
        }

        private async Task<BootstrapInitializationResult> InitializeAsync()
        {
            GameLogger.Log("[Bootstrap] Initialization started.", this);

            var coordinator = new GameInitializationCoordinator(
                initializeContent: () =>
                {
                    if (definitionDatabase == null)
                    {
                        throw new System.InvalidOperationException(
                            "DefinitionDatabase is not assigned.");
                    }

                    definitionDatabase.Initialize();
                },
                validateContent: () =>
                {
                    ValidationReport report = DataValidator.Validate(definitionDatabase);
                    DataValidator.LogReport(report);
                    return !report.HasErrors;
                },
                createSaveService: () =>
                {
                    VerifyDevelopmentLookups();
                    return SaveSystemFactory.Create(
                        Application.persistentDataPath,
                        definitionDatabase);
                },
                profileIdProvider: new GuidProfileIdProvider(),
                loadMainScene: () => SceneService.LoadAsync(GameScene.Main));

            BootstrapInitializationResult result = await coordinator.InitializeAsync();
            SaveService = coordinator.SaveService;
            return result;
        }

        private void HandleInitializationResult(BootstrapInitializationResult result)
        {
            if (result == null)
            {
                GameLogger.Error("[Bootstrap] Initialization returned no result.", this);
                return;
            }

            if (!result.IsSuccess)
            {
                if (result.Exception != null)
                {
                    GameLogger.Exception(result.Exception, this);
                }

                string saveStatus = result.SaveLoadResult == null
                    ? "NotAttempted"
                    : result.SaveLoadResult.Status.ToString();
                GameLogger.Error(
                    $"[Bootstrap] Initialization failed. " +
                    $"Status={result.Status}, SaveStatus={saveStatus}. {result.Message}",
                    this);
                return;
            }

            switch (result.SaveLoadResult.Status)
            {
                case SaveLoadStatus.LoadedMain:
                    GameLogger.Log("[Bootstrap] Existing profile loaded.", this);
                    break;
                case SaveLoadStatus.CreatedNewProfile:
                    GameLogger.Log("[Bootstrap] New profile created.", this);
                    break;
                case SaveLoadStatus.LoadedAndRepairedMain:
                    GameLogger.Warning(
                        "[Bootstrap] Profile data was repaired and saved.",
                        this);
                    break;
                case SaveLoadStatus.RecoveredFromBackup:
                    GameLogger.Warning(
                        "[Bootstrap] Main profile was recovered from backup.",
                        this);
                    break;
            }

            GameLogger.Log("[Bootstrap] Initialization completed.", this);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void VerifyDevelopmentLookups()
        {
            if (definitionDatabase.Characters.Count > 0)
            {
                bool foundCharacter = definitionDatabase.TryGetCharacter(
                    "character_marea_bluefang",
                    out _);

                LogDevelopmentLookupResult("character_marea_bluefang", foundCharacter);
            }

            if (definitionDatabase.Bosses.Count > 0)
            {
                bool foundBoss = definitionDatabase.TryGetBoss("boss_kragmor", out _);
                LogDevelopmentLookupResult("boss_kragmor", foundBoss);
            }
        }

        private void LogDevelopmentLookupResult(string id, bool found)
        {
            if (found)
            {
                GameLogger.Log($"[Bootstrap] Development content lookup succeeded: {id}", this);
            }
            else
            {
                GameLogger.Warning($"[Bootstrap] Development content lookup failed: {id}", this);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
