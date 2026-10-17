using System;
using System.Threading.Tasks;
using UnityEngine;
using ValorChronicle.Audio.Core;
using ValorChronicle.Audio.Runtime;
using ValorChronicle.Battle.Entry;
using ValorChronicle.Core.Logging;
using ValorChronicle.Core.Pause;
using ValorChronicle.Core.Random;
using ValorChronicle.Core.Scene;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Validation;
using ValorChronicle.Localization;
using ValorChronicle.Save.Services;
using ValorChronicle.Settings.Integration;
using ValorChronicle.Settings.Services;
using GameAudioConfiguration =
    ValorChronicle.Audio.Configuration.AudioConfiguration;

namespace ValorChronicle.Core.Bootstrap
{
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [SerializeField]
        private DefinitionDatabase definitionDatabase;
        [SerializeField]
        private LocalizationCatalog localizationCatalog;
        [SerializeField]
        private GameAudioConfiguration audioConfiguration;

        public static GameBootstrapper Instance { get; private set; }

        public event Action<BootstrapInitializationResult>
            InitializationCompleted;

        public SceneService SceneService { get; private set; }
        public BattleEntryService BattleEntryService { get; private set; }
        public DefinitionDatabase DefinitionDatabase => definitionDatabase;
        public LocalizationService LocalizationService { get; private set; }
        public IRandomSource RandomSource { get; private set; }
        /// <summary>Gets the initialized save service for persistent game-domain operations.</summary>
        public SaveService SaveService { get; private set; }
        public SettingsService SettingsService { get; private set; }
        public SettingsLoadResult SettingsLoadResult { get; private set; }
        public AudioService AudioService { get; private set; }
        public GamePauseService GamePauseService { get; private set; }
        /// <summary>Gets the most recent bootstrap result, or null while initialization is pending.</summary>
        public BootstrapInitializationResult InitializationResult { get; private set; }

        private SettingsLocaleBinding settingsLocaleBinding;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeRuntimeServices();
        }

        private async void Start()
        {
            if (Instance != this)
                return;

            // Audio initialization reaches AudioMixer.SetFloat, which Unity only
            // guarantees from Start onward. Keep load, volume apply, and first
            // playback in this lifecycle phase so playback cannot precede it.
            BootstrapInitializationResult result;
            try
            {
                result = await InitializeAsync();
            }
            catch (Exception exception)
            {
                result = new BootstrapInitializationResult(
                    BootstrapInitializationStatus.UnexpectedFailure,
                    exception: exception,
                    message: "Unexpected initialization failure.");
            }

            CompleteInitialization(result);
        }

        private void InitializeRuntimeServices()
        {
            if (SceneService == null)
            {
                SceneService = new SceneService();
            }

            if (BattleEntryService == null)
            {
                BattleEntryService = new BattleEntryService();
            }

            if (RandomSource == null)
            {
                RandomSource = new UnityRandomSource();
            }

            if (GamePauseService == null)
            {
                GamePauseService = new GamePauseService();
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

                    if (localizationCatalog == null)
                    {
                        throw new System.InvalidOperationException(
                            "LocalizationCatalog is not assigned.");
                    }

                    LocalizationService = localizationCatalog.CreateService();
                    InitializeSettingsAndLocalization();
                    InitializeAudio();
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
                profileIdProvider: new GuidProfileIdProvider());

            BootstrapInitializationResult result = await coordinator.InitializeAsync();
            SaveService = coordinator.SaveService;
            return result;
        }

        private void CompleteInitialization(
            BootstrapInitializationResult result)
        {
            InitializationResult = result;
            HandleInitializationResult(result);
            InitializationCompleted?.Invoke(result);
        }

        private void InitializeSettingsAndLocalization()
        {
            try
            {
                SettingsService = SettingsSystemFactory.Create(
                    Application.persistentDataPath);
                SettingsLoadResult = SettingsService.LoadOrCreate();
                settingsLocaleBinding = new SettingsLocaleBinding(
                    SettingsService,
                    LocalizationService);
            }
            catch (Exception exception)
            {
                GameLogger.Error(
                    "[Settings] Persistent settings initialization failed; "
                        + "using in-memory defaults for this session.",
                    this);

                settingsLocaleBinding?.Dispose();
                SettingsService = ValorChronicle.Settings.Services.SettingsService
                    .CreateInMemoryDefaults();
                SettingsLoadResult = new SettingsLoadResult(
                    SettingsLoadStatus.InMemoryFallback,
                    message: "Persistent settings initialization failed.",
                    exception: exception);
                settingsLocaleBinding = new SettingsLocaleBinding(
                    SettingsService,
                    LocalizationService);
            }

            LogSettingsLoadResult(SettingsLoadResult);
            if (SettingsLoadResult.Status != SettingsLoadStatus.InMemoryFallback)
            {
                gameObject.AddComponent<SettingsPersistenceDriver>()
                    .Initialize(SettingsService);
            }
        }

        private void LogSettingsLoadResult(SettingsLoadResult result)
        {
            if (result == null)
            {
                GameLogger.Error("[Settings] Load returned no result.", this);
                return;
            }

            switch (result.Status)
            {
                case SettingsLoadStatus.LoadedMain:
                case SettingsLoadStatus.CreatedDefaults:
                    GameLogger.Log(
                        $"[Settings] Initialization completed. Status={result.Status}.",
                        this);
                    break;
                case SettingsLoadStatus.LoadedAndRepairedMain:
                case SettingsLoadStatus.RecoveredFromBackup:
                case SettingsLoadStatus.RecoveredDefaults:
                    GameLogger.Warning(
                        $"[Settings] Settings were recovered or normalized. "
                            + $"Status={result.Status}.",
                        this);
                    break;
                default:
                    if (result.Exception != null)
                        GameLogger.Exception(result.Exception, this);

                    GameLogger.Error(
                        $"[Settings] Persistence is degraded. "
                            + $"Status={result.Status}. {result.Message}",
                        this);
                    break;
            }
        }

        private void InitializeAudio()
        {
            try
            {
                AudioRuntime runtime = GetComponent<AudioRuntime>();
                if (runtime == null)
                    runtime = gameObject.AddComponent<AudioRuntime>();

                AudioService = runtime.Initialize(
                    SettingsService,
                    audioConfiguration);
            }
            catch (Exception exception)
            {
                AudioService = null;
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[Audio] Initialization failed. "
                        + "Game initialization will continue without audio.",
                    this);
            }
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
                bool foundBoss = definitionDatabase.TryGetBoss("kragmor", out _);
                LogDevelopmentLookupResult("kragmor", foundBoss);
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
            settingsLocaleBinding?.Dispose();
            settingsLocaleBinding = null;

            if (Instance == this)
            {
                GamePauseService?.Dispose();
                GamePauseService = null;
                Instance = null;
            }
        }
    }
}
