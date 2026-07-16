using System.Threading.Tasks;
using UnityEngine;
using ValorChronicle.Core.Logging;
using ValorChronicle.Core.Random;
using ValorChronicle.Core.Scene;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Validation;

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

            try
            {
                bool initialized = await InitializeAsync();

                if (!initialized)
                {
                    GameLogger.Error("[Bootstrap] Initialization failed.", this);
                }
            }
            catch (System.Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error("[Bootstrap] Initialization failed.", this);

                // 향후 여기서 오류 팝업 또는 복구 화면을 띄울 수 있다.
            }
        }

        private async Task<bool> InitializeAsync()
        {
            GameLogger.Log("[Bootstrap] Initialization started.", this);

            if (definitionDatabase == null)
            {
                GameLogger.Error("[Bootstrap] DefinitionDatabase is not assigned.", this);
                return false;
            }

            ValidationReport report = DataValidator.Validate(definitionDatabase);
            DataValidator.LogReport(report);

            if (report.HasErrors)
            {
                return false;
            }

            definitionDatabase.Initialize();
            VerifyDevelopmentLookups();

            RandomSource = new UnityRandomSource();

            GameLogger.Log("[Bootstrap] Initialization completed.", this);

            return await SceneService.LoadAsync(GameScene.Main);
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
