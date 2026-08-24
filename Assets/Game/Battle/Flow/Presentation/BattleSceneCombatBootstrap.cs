using System;
using UnityEngine;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Logging;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Flow.Presentation
{
    public sealed class BattleSceneCombatBootstrap : MonoBehaviour
    {
        private const string DevelopmentBossId = "kragmor";
        private const string DefaultDevelopmentDifficultyId =
            "difficulty_normal";

        [SerializeField]
        private BattleFlowController battleFlowController = null;

        [SerializeField]
        private BattleResultPresentationController
            resultPresentationController = null;

        [SerializeField]
        private BossDefinition fallbackBossDefinition = null;

        [SerializeField]
        private CharacterDefinition fallbackMareaDefinition = null;

        [SerializeField]
        private BattleResultBalanceDefinition
            fallbackResultBalanceDefinition = null;

        [SerializeField, Min(1)]
        private int developmentMareaLevel = 1;

        [SerializeField]
        private string developmentDifficultyId =
            DefaultDevelopmentDifficultyId;

        private bool initializationAttempted;

        public bool HasInitializedFlow { get; private set; }
        public bool HasInitializedCombat => CombatComposition?.Bridge != null;
        public IKragmorBossIntentSource BossIntentSource =>
            CombatComposition?.BossIntentSource;
        public IBossVisualStateSource BossVisualStateSource =>
            CombatComposition?.BossVisualStateSource;
        public DefinitionDatabase DefinitionDatabase { get; private set; }
        public BattleSceneCombatComposition CombatComposition
        {
            get;
            private set;
        }

        private void Awake()
        {
            InitializeFlowOnce();
        }

        private void OnDestroy()
        {
            CombatComposition?.Dispose();
        }

        private void InitializeFlowOnce()
        {
            if (initializationAttempted)
            {
                return;
            }

            initializationAttempted = true;
            DefinitionDatabase = ResolveInitializedDefinitionDatabase();
            if (battleFlowController == null)
            {
                GameLogger.Error(
                    "[BattleSceneCombatBootstrap] BattleFlowController "
                        + "is not assigned.",
                    this);
                return;
            }

            if (battleFlowController.Context != null)
            {
                HasInitializedFlow = true;
                return;
            }

            BossDefinition selectedBoss = ResolveBossDefinition();
            if (selectedBoss == null)
            {
                GameLogger.Error(
                    "[BattleSceneCombatBootstrap] No BossDefinition is "
                        + "available.",
                    this);
                return;
            }

            if (!selectedBoss.TryGetDifficultyStats(
                developmentDifficultyId,
                out BossDifficultyStats selectedDifficulty))
            {
                GameLogger.Error(
                    $"[BattleSceneCombatBootstrap] Boss difficulty is not "
                        + $"available. BossId={selectedBoss.Id}; "
                        + $"DifficultyId={developmentDifficultyId}.",
                    this);
                return;
            }

            if (!(selectedBoss.CombatConfig is
                KragmorCombatConfig selectedKragmorConfig))
            {
                GameLogger.Error(
                    "[BattleSceneCombatBootstrap] Kragmor requires a "
                        + "KragmorCombatConfig.",
                    this);
                return;
            }

            if (!selectedKragmorConfig.TryValidate(
                out string kragmorConfigError))
            {
                GameLogger.Error(
                    "[BattleSceneCombatBootstrap] Kragmor combat config "
                        + $"is invalid. {kragmorConfigError}",
                    this);
                return;
            }

            CharacterDefinition selectedMarea = ResolveMareaDefinition();
            if (selectedMarea == null)
            {
                GameLogger.Error(
                    "[BattleSceneCombatBootstrap] No Marea "
                        + "CharacterDefinition is available.",
                    this);
                return;
            }

            if (!(selectedMarea.CombatConfig is
                MareaBluefangCombatConfig selectedMareaConfig))
            {
                GameLogger.Error(
                    "[BattleSceneCombatBootstrap] Marea requires a "
                        + "MareaBluefangCombatConfig.",
                    this);
                return;
            }

            if (!selectedMareaConfig.TryValidate(
                out string configError))
            {
                GameLogger.Error(
                    "[BattleSceneCombatBootstrap] Marea combat config "
                        + $"is invalid. {configError}",
                    this);
                return;
            }

            BattleResultBalanceDefinition selectedResultBalance =
                ResolveResultBalanceDefinition();
            if (selectedResultBalance == null)
            {
                GameLogger.Error(
                    "[BattleSceneCombatBootstrap] No "
                        + "BattleResultBalanceDefinition is available.",
                    this);
                return;
            }

            if (!selectedResultBalance.TryValidate(
                out string resultBalanceError))
            {
                GameLogger.Error(
                    "[BattleSceneCombatBootstrap] Battle result balance "
                        + $"is invalid. {resultBalanceError}",
                    this);
                return;
            }

            try
            {
                IRandomSource randomSource =
                    GameBootstrapper.Instance?.RandomSource
                    ?? new UnityRandomSource();
                var saveService = GameBootstrapper.Instance?.SaveService;
                CombatComposition = new BattleSceneCombatComposition(
                    selectedMarea,
                    selectedMareaConfig,
                    developmentMareaLevel,
                    selectedBoss,
                    selectedKragmorConfig,
                    selectedDifficulty,
                    randomSource,
                    selectedResultBalance.CreateBalance(),
                    saveService);
                battleFlowController.Initialize(
                    CombatComposition.FlowSetup,
                    coordinator => CombatComposition.CreateBridge(
                        coordinator,
                        battleFlowController.BoardMutationSink));
                if (CombatComposition.ResultPersistenceCoordinator != null
                    && resultPresentationController != null)
                {
                    resultPresentationController.Initialize(
                        CombatComposition.ResultPersistenceCoordinator);
                }

                HasInitializedFlow = true;
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[BattleSceneCombatBootstrap] Battle Flow "
                        + "initialization failed.",
                    this);
            }
        }

        private BossDefinition ResolveBossDefinition()
        {
            DefinitionDatabase database = DefinitionDatabase;
            if (database != null
                && database.IsInitialized
                && database.TryGetBoss(
                    DevelopmentBossId,
                    out BossDefinition databaseBoss))
            {
                return databaseBoss;
            }

            return fallbackBossDefinition;
        }

        private CharacterDefinition ResolveMareaDefinition()
        {
            DefinitionDatabase database = DefinitionDatabase;
            if (database != null
                && database.IsInitialized
                && database.TryGetCharacter(
                    MareaBluefangRules.CharacterId,
                    out CharacterDefinition databaseMarea))
            {
                return databaseMarea;
            }

            return fallbackMareaDefinition;
        }

        private BattleResultBalanceDefinition
            ResolveResultBalanceDefinition()
        {
            DefinitionDatabase database = DefinitionDatabase;
            if (database != null
                && database.IsInitialized
                && database.BattleResultBalance != null)
            {
                return database.BattleResultBalance;
            }

            return fallbackResultBalanceDefinition;
        }

        private static DefinitionDatabase
            ResolveInitializedDefinitionDatabase()
        {
            DefinitionDatabase database =
                GameBootstrapper.Instance?.DefinitionDatabase;
            return database != null && database.IsInitialized
                ? database
                : null;
        }
    }
}
