using System;
using UnityEngine;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Logging;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

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
        private BattleResultBalanceDefinition
            fallbackResultBalanceDefinition = null;

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

            if (DefinitionDatabase == null)
            {
                GameLogger.Error(
                    "[BattleSceneCombatBootstrap] An initialized "
                        + "DefinitionDatabase is required.",
                    this);
                return;
            }

            SaveService saveService =
                GameBootstrapper.Instance?.SaveService;
            if (saveService == null || !saveService.HasCurrentProfile)
            {
                GameLogger.Error(
                    "[BattleSceneCombatBootstrap] SaveService with a "
                        + "current profile is required.",
                    this);
                return;
            }

            ProfileSaveData profileSnapshot;
            try
            {
                profileSnapshot = saveService.GetCurrentProfileSnapshot();
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[BattleSceneCombatBootstrap] Current profile "
                        + "snapshot could not be read.",
                    this);
                return;
            }

            var partyResolver = new BattlePartyResolver(
                DefinitionDatabase);
            BattlePartyResolutionResult partyResolution =
                partyResolver.Resolve(profileSnapshot);
            if (!partyResolution.IsSuccess)
            {
                GameLogger.Error(
                    "[BattleSceneCombatBootstrap] Battle party "
                        + $"resolution failed. Status="
                        + $"{partyResolution.Status}. "
                        + partyResolution.ErrorMessage,
                    this);
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

            BattleSceneCombatComposition composition = null;
            try
            {
                IRandomSource randomSource =
                    GameBootstrapper.Instance?.RandomSource
                    ?? new UnityRandomSource();
                var characterRegistrars =
                    new CharacterCombatProviderRegistrarCatalog(
                        new ICharacterCombatProviderRegistrar[]
                        {
                            new MareaBluefangCombatProviderRegistrar()
                        });
                composition = new BattleSceneCombatComposition(
                    partyResolution.Members,
                    characterRegistrars,
                    selectedBoss,
                    selectedKragmorConfig,
                    selectedDifficulty,
                    randomSource,
                    selectedResultBalance.CreateBalance(),
                    saveService);
                battleFlowController.Initialize(
                    composition.FlowSetup,
                    coordinator => composition.CreateBridge(
                        coordinator,
                        battleFlowController.BoardMutationSink));
                if (composition.ResultPersistenceCoordinator != null
                    && resultPresentationController != null)
                {
                    resultPresentationController.Initialize(
                        composition.ResultPersistenceCoordinator);
                }

                CombatComposition = composition;
                HasInitializedFlow = true;
            }
            catch (Exception exception)
            {
                composition?.Dispose();
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
