using System;
using UnityEngine;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Entry;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Characters.Runtime;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Logging;
using ValorChronicle.Core.Pause;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Battle.Flow.Presentation
{
    public sealed class BattleSceneCombatBootstrap : MonoBehaviour
    {
        [SerializeField]
        private BattleFlowController battleFlowController = null;

        [SerializeField]
        private BattleResultPresentationController
            resultPresentationController = null;

        private bool initializationAttempted;
        private bool resultAudioSubscribed;

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
            UnsubscribeResultAudio();
            CombatComposition?.Dispose();
        }

        private void InitializeFlowOnce()
        {
            if (initializationAttempted)
            {
                return;
            }

            initializationAttempted = true;
            GameBootstrapper bootstrapper = GameBootstrapper.Instance;
            if (bootstrapper == null)
            {
                FailInitialization(
                    "GameBootstrapper is not available.");
                return;
            }

            BattleEntryService entryService =
                bootstrapper.BattleEntryService;
            if (entryService == null)
            {
                FailInitialization(
                    "BattleEntryService is not available.");
                return;
            }

            if (!entryService.TryGetCurrent(
                out BattleEntryRequest entryRequest))
            {
                FailInitialization(
                    "A BattleEntryRequest is required.");
                return;
            }

            if (bootstrapper.InitializationResult == null
                || !bootstrapper.InitializationResult.IsSuccess)
            {
                FailInitialization(
                    "GameBootstrapper initialization is not complete.",
                    entryService,
                    entryRequest);
                return;
            }

            if (battleFlowController == null)
            {
                FailInitialization(
                    "BattleFlowController is not assigned.",
                    entryService,
                    entryRequest);
                return;
            }

            if (battleFlowController.Context != null)
            {
                FailInitialization(
                    "BattleFlowController is already initialized.",
                    entryService,
                    entryRequest);
                return;
            }

            DefinitionDatabase = bootstrapper.DefinitionDatabase;
            if (DefinitionDatabase == null)
            {
                FailInitialization(
                    "An initialized DefinitionDatabase is required.",
                    entryService,
                    entryRequest);
                return;
            }

            if (!BattleEncounterFactory.TryCreate(
                DefinitionDatabase,
                entryRequest,
                out BattleEncounterDefinition encounter,
                out string encounterError))
            {
                FailInitialization(
                    encounterError,
                    entryService,
                    entryRequest);
                return;
            }

            SaveService saveService = bootstrapper.SaveService;
            if (saveService == null || !saveService.HasCurrentProfile)
            {
                FailInitialization(
                    "SaveService with a current profile is required.",
                    entryService,
                    entryRequest);
                return;
            }

            ProfileSaveData profileSnapshot;
            try
            {
                profileSnapshot = saveService.GetCurrentProfileSnapshot();
            }
            catch (Exception exception)
            {
                FailInitialization(
                    "Current profile snapshot could not be read.",
                    entryService,
                    entryRequest,
                    exception);
                return;
            }

            var partyResolver = new BattlePartyResolver(
                DefinitionDatabase);
            BattlePartyResolutionResult partyResolution =
                partyResolver.Resolve(profileSnapshot);
            if (!partyResolution.IsSuccess)
            {
                FailInitialization(
                    "Battle party resolution failed. "
                        + $"Status={partyResolution.Status}. "
                        + partyResolution.ErrorMessage,
                    entryService,
                    entryRequest);
                return;
            }

            BattleResultBalanceDefinition selectedResultBalance =
                DefinitionDatabase.BattleResultBalance;
            if (selectedResultBalance == null)
            {
                FailInitialization(
                    "No BattleResultBalanceDefinition is available.",
                    entryService,
                    entryRequest);
                return;
            }

            if (!selectedResultBalance.TryValidate(
                out string resultBalanceError))
            {
                FailInitialization(
                    "Battle result balance is invalid. "
                        + resultBalanceError,
                    entryService,
                    entryRequest);
                return;
            }

            if (bootstrapper.RandomSource == null)
            {
                FailInitialization(
                    "IRandomSource is not available.",
                    entryService,
                    entryRequest);
                return;
            }

            BattleSceneCombatComposition composition = null;
            try
            {
                GamePauseService pauseService =
                    bootstrapper.GamePauseService;
                if (pauseService == null)
                {
                    throw new InvalidOperationException(
                        "GamePauseService is not available.");
                }

                battleFlowController.InitializePauseService(pauseService);
                CharacterCombatProviderRegistrarCatalog characterRegistrars =
                    CharacterRuntimeRegistrationCatalog
                        .CreateDefault()
                        .CreateCombatRegistrarCatalog();
                composition = new BattleSceneCombatComposition(
                    partyResolution.Members,
                    characterRegistrars,
                    encounter.Boss,
                    encounter.KragmorConfig,
                    encounter.Difficulty,
                    bootstrapper.RandomSource,
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
                    SubscribeResultAudio();
                    resultPresentationController.Initialize(
                        composition.ResultPersistenceCoordinator);
                }

                if (!entryService.TryConsume(entryRequest, out _))
                {
                    throw new InvalidOperationException(
                        "The BattleEntryRequest could not be consumed.");
                }

                CombatComposition = composition;
                HasInitializedFlow = true;
            }
            catch (Exception exception)
            {
                UnsubscribeResultAudio();
                composition?.Dispose();
                if (battleFlowController.Context != null)
                {
                    battleFlowController.enabled = false;
                }

                FailInitialization(
                    "Battle Flow initialization failed.",
                    entryService,
                    entryRequest,
                    exception);
            }
        }

        private void SubscribeResultAudio()
        {
            if (resultAudioSubscribed || resultPresentationController == null)
                return;

            resultPresentationController.ResultPresented +=
                HandleResultPresented;
            resultAudioSubscribed = true;
        }

        private void UnsubscribeResultAudio()
        {
            if (!resultAudioSubscribed || resultPresentationController == null)
                return;

            resultPresentationController.ResultPresented -=
                HandleResultPresented;
            resultAudioSubscribed = false;
        }

        private static void HandleResultPresented(BattleResultKind result)
        {
            if (result != BattleResultKind.Victory
                && result != BattleResultKind.Defeat
                && result != BattleResultKind.TurnLimitReached)
            {
                return;
            }

            GameBootstrapper bootstrapper = GameBootstrapper.Instance;
            if (bootstrapper?.AudioService?.IsInitialized == true)
                bootstrapper.AudioService.FadeOutBattleResultBgm();
        }

        private void FailInitialization(
            string message,
            BattleEntryService entryService = null,
            BattleEntryRequest entryRequest = null,
            Exception exception = null)
        {
            if (exception != null)
            {
                GameLogger.Exception(exception, this);
            }

            GameLogger.Error(
                "[BattleSceneCombatBootstrap] " + message,
                this);
            if (entryService == null || entryRequest == null)
            {
                return;
            }

            entryService.TryCancel(entryRequest);
        }
    }
}
