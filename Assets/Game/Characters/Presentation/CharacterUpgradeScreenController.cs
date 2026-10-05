using System;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Progression;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Logging;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Presentation;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Characters.Presentation
{
    public sealed class CharacterUpgradeScreenController : UnityEngine.MonoBehaviour
    {
        private CharacterUpgradeScreenView view;
        private CharacterPresentationCatalog presentationCatalog;
        private ElementIconSet elementIconSet;
        private SaveService saveService;
        private DefinitionDatabase definitionDatabase;
        private CharacterBuildResolver buildResolver;
        private CharacterLevelUpPersistenceService persistenceService;
        private CharacterLevelUpPreviewSession previewSession;
        private CharacterDefinition previewDefinition;
        private bool subscribed;
        private bool initialized;
        private bool operationInProgress;

        public event Action ReturnRequested;
        public event Action AuthoritativeProfileRefreshRequested;

        public CharacterUpgradePresentationModel CurrentModel { get; private set; }
        public CharacterLevelUpPreviewSession PreviewSession => previewSession;
        public CharacterLevelUpPersistenceOperationResult LastPersistenceResult
        {
            get;
            private set;
        }
        public bool IsOperationInProgress => operationInProgress;

        private void Start()
        {
            if (initialized)
            {
                return;
            }

            GameBootstrapper bootstrap = GameBootstrapper.Instance;
            if (bootstrap?.SaveService == null
                || bootstrap.DefinitionDatabase == null)
            {
                GameLogger.Error(
                    "[CharacterUpgradeScreenController] Initialized "
                        + "SaveService and DefinitionDatabase are required.",
                    this);
                view?.Clear();
                return;
            }

            try
            {
                Initialize(bootstrap.SaveService, bootstrap.DefinitionDatabase);
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[CharacterUpgradeScreenController] Initialization failed.",
                    this);
                view?.Clear();
            }
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            CancelPreviewAndRestoreAuthoritative(renderView: false);
            Unsubscribe();
        }

        public void Configure(
            CharacterUpgradeScreenView configuredView,
            CharacterPresentationCatalog configuredPresentationCatalog,
            ElementIconSet configuredElementIconSet)
        {
            Unsubscribe();
            view = configuredView
                ?? throw new ArgumentNullException(nameof(configuredView));
            presentationCatalog = configuredPresentationCatalog
                ?? throw new ArgumentNullException(
                    nameof(configuredPresentationCatalog));
            elementIconSet = configuredElementIconSet
                ?? throw new ArgumentNullException(
                    nameof(configuredElementIconSet));
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        public void Initialize(
            SaveService initializedSaveService,
            DefinitionDatabase initializedDefinitionDatabase,
            CharacterBuildResolver initializedBuildResolver = null,
            CharacterLevelUpPersistenceService initializedPersistenceService =
                null)
        {
            saveService = initializedSaveService
                ?? throw new ArgumentNullException(
                    nameof(initializedSaveService));
            definitionDatabase = initializedDefinitionDatabase
                ?? throw new ArgumentNullException(
                    nameof(initializedDefinitionDatabase));
            if (!definitionDatabase.IsInitialized)
            {
                throw new InvalidOperationException(
                    "DefinitionDatabase must be initialized.");
            }

            if (view == null
                || presentationCatalog == null
                || elementIconSet == null)
            {
                throw new InvalidOperationException(
                    "Character upgrade screen dependencies are incomplete.");
            }

            if (!presentationCatalog.IsInitialized)
            {
                presentationCatalog.Initialize();
            }

            buildResolver = initializedBuildResolver
                ?? CharacterBuildResolverFactory.CreateDefault();
            persistenceService = initializedPersistenceService
                ?? new CharacterLevelUpPersistenceService(saveService);
            initialized = true;
        }

        public bool TryPresent(string characterId)
        {
            return TryPresent(characterId, renderView: true);
        }

        private bool TryPresent(string characterId, bool renderView)
        {
            CurrentModel = null;
            if (renderView)
            {
                view?.Clear();
            }

            if (!initialized
                || string.IsNullOrEmpty(characterId)
                || !saveService.HasCurrentProfile)
            {
                return false;
            }

            try
            {
                ProfileSaveData profile =
                    saveService.GetCurrentProfileSnapshot();
                CharacterSaveData savedCharacter = FindCharacter(
                    profile,
                    characterId);
                if (savedCharacter == null
                    || !definitionDatabase.TryGetCharacter(
                        characterId,
                        out CharacterDefinition definition))
                {
                    return false;
                }

                long battleRecords = profile.Currencies?.BattleRecords
                    ?? throw new InvalidOperationException(
                        "The current profile has no currencies.");
                presentationCatalog.TryGet(
                    characterId,
                    out CharacterPresentationDefinition presentation);

                CurrentModel = CreatePresentationModel(
                    definition,
                    presentation,
                    savedCharacter.Level,
                    savedCharacter.Awakening,
                    battleRecords);
                if (renderView)
                {
                    view.Render(CurrentModel);
                }

                return true;
            }
            catch (Exception exception)
            {
                CurrentModel = null;
                if (renderView)
                {
                    view?.Clear();
                }

                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[CharacterUpgradeScreenController] Failed to build "
                        + $"detail presentation for '{characterId}'.",
                    this);
                return false;
            }
        }

        private static CharacterSaveData FindCharacter(
            ProfileSaveData profile,
            string characterId)
        {
            if (profile?.Characters == null)
            {
                return null;
            }

            for (int index = 0; index < profile.Characters.Count; index++)
            {
                CharacterSaveData character = profile.Characters[index];
                if (character != null
                    && string.Equals(
                        character.CharacterId,
                        characterId,
                        StringComparison.Ordinal))
                {
                    return character;
                }
            }

            return null;
        }

        public void CancelPreview()
        {
            HandleLevelUpCancelRequested();
        }

        private CharacterUpgradePresentationModel CreatePresentationModel(
            CharacterDefinition definition,
            CharacterPresentationDefinition presentation,
            int level,
            int awakening,
            long battleRecords)
        {
            ResolvedCharacterBuild build = buildResolver.Resolve(
                definition,
                level,
                awakening);
            bool isMaximumLevel =
                CharacterLevelUpCostCalculator.IsMaximumLevel(level);
            long nextLevelCost = isMaximumLevel
                ? 0L
                : CharacterLevelUpCostCalculator.GetNextLevelCost(level);
            return new CharacterUpgradePresentationModel(
                definition.Id,
                string.IsNullOrEmpty(definition.DisplayNameKey)
                    ? definition.Id
                    : definition.DisplayNameKey,
                presentation?.PreviewSprite,
                elementIconSet.GetIcon(definition.Element),
                level,
                awakening,
                build.MaxHp,
                build.Attack,
                battleRecords,
                nextLevelCost,
                !isMaximumLevel,
                !isMaximumLevel && battleRecords >= nextLevelCost);
        }

        private void Subscribe()
        {
            if (subscribed || view == null)
            {
                return;
            }

            view.ReturnRequested += HandleReturnRequested;
            view.LevelUpPressStarted += HandleLevelUpPressStarted;
            view.LevelUpRepeatRequested += HandleLevelUpRepeatRequested;
            view.LevelUpCommitRequested += HandleLevelUpCommitRequested;
            view.LevelUpCancelRequested += HandleLevelUpCancelRequested;
            view.Disabled += HandleViewDisabled;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            view.ReturnRequested -= HandleReturnRequested;
            view.LevelUpPressStarted -= HandleLevelUpPressStarted;
            view.LevelUpRepeatRequested -= HandleLevelUpRepeatRequested;
            view.LevelUpCommitRequested -= HandleLevelUpCommitRequested;
            view.LevelUpCancelRequested -= HandleLevelUpCancelRequested;
            view.Disabled -= HandleViewDisabled;
            subscribed = false;
        }

        private void HandleReturnRequested()
        {
            if (!operationInProgress && previewSession == null)
            {
                ReturnRequested?.Invoke();
            }
        }

        private void HandleLevelUpPressStarted()
        {
            if (operationInProgress
                || previewSession != null
                || CurrentModel?.CanLevelUp != true)
            {
                view.CancelLevelUpPress(notify: false);
                return;
            }

            string characterId = CurrentModel.CharacterId;
            if (!TryPresent(characterId)
                || CurrentModel?.CanLevelUp != true
                || !definitionDatabase.TryGetCharacter(
                    characterId,
                    out previewDefinition))
            {
                view.CancelLevelUpPress(notify: false);
                return;
            }

            previewSession = new CharacterLevelUpPreviewSession(
                characterId,
                CurrentModel.Level,
                CurrentModel.BattleRecords,
                CurrentModel.Awakening);
            view.SetPreviewActive(true);
            if (!TryAdvancePreview())
            {
                HandleLevelUpCancelRequested();
            }
        }

        private void HandleLevelUpRepeatRequested()
        {
            if (!TryAdvancePreview())
            {
                view.StopLevelUpRepeating();
            }
        }

        private bool TryAdvancePreview()
        {
            if (operationInProgress
                || previewSession == null
                || previewDefinition == null
                || !previewSession.TryAdvance())
            {
                return false;
            }

            presentationCatalog.TryGet(
                previewSession.CharacterId,
                out CharacterPresentationDefinition presentation);
            CurrentModel = CreatePresentationModel(
                previewDefinition,
                presentation,
                previewSession.PreviewTargetLevel,
                previewSession.Awakening,
                previewSession.PreviewRemainingBattleRecords);
            view.Render(CurrentModel);
            if (!CurrentModel.CanLevelUp)
            {
                view.StopLevelUpRepeating();
            }

            return true;
        }

        private void HandleLevelUpCommitRequested()
        {
            if (operationInProgress || previewSession == null)
            {
                return;
            }

            CharacterLevelUpPreviewSession committedPreview = previewSession;
            previewSession = null;
            previewDefinition = null;
            view.SetPreviewActive(false);
            if (!committedPreview.HasLevelChange)
            {
                TryPresent(committedPreview.CharacterId);
                return;
            }

            operationInProgress = true;
            view.SetBusy(true);
            LastPersistenceResult = null;
            try
            {
                LastPersistenceResult = persistenceService.LevelUp(
                    committedPreview.CharacterId,
                    committedPreview.PreviewTargetLevel);
                if (!LastPersistenceResult.IsSuccess)
                {
                    GameLogger.Warning(
                        "[CharacterUpgradeScreenController] Level-up was "
                            + "not saved. Status="
                            + $"{LastPersistenceResult.Status}.",
                        this);
                }
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[CharacterUpgradeScreenController] Level-up request "
                        + "failed unexpectedly.",
                    this);
            }
            finally
            {
                TryPresent(committedPreview.CharacterId);
                AuthoritativeProfileRefreshRequested?.Invoke();
                operationInProgress = false;
                view.SetBusy(false);
            }
        }

        private void HandleLevelUpCancelRequested()
        {
            CancelPreviewAndRestoreAuthoritative(
                renderView: view != null && view.isActiveAndEnabled);
        }

        private void HandleViewDisabled()
        {
            CancelPreviewAndRestoreAuthoritative(renderView: false);
        }

        private void CancelPreviewAndRestoreAuthoritative(bool renderView)
        {
            if (previewSession == null)
            {
                return;
            }

            string characterId = previewSession.CharacterId;
            previewSession = null;
            previewDefinition = null;
            view?.CancelLevelUpPress(notify: false);
            view?.SetPreviewActive(false);
            TryPresent(characterId, renderView);
        }
    }
}
