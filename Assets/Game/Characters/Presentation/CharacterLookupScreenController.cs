using System;
using UnityEngine;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Logging;
using ValorChronicle.Localization;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Characters.Presentation
{
    public sealed class CharacterLookupScreenController : MonoBehaviour
    {
        private const int DefaultAwakeningStage = 1;
        private const CharacterLookupSkillType DefaultSkillType =
            CharacterLookupSkillType.Match3;

        private CharacterLookupScreenView view;
        private LocalizationService localizationService;
        private SaveService saveService;
        private CharacterLookupDescriptionResolver descriptionResolver;
        private bool viewSubscribed;
        private bool localeSubscribed;
        private bool initialized;
        private string characterId;
        private CharacterLookupMode mode = CharacterLookupMode.Awakening;
        private int selectedAwakeningStage = DefaultAwakeningStage;
        private CharacterLookupSkillType selectedSkillType = DefaultSkillType;

        public event Action ReturnRequested;
        public event Action<int> AwakeningStageSelected;
        public event Action<CharacterLookupSkillType> SkillTypeSelected;
        public event Action<CharacterLookupPresentationModel>
            PresentationChanged;

        public CharacterLookupPresentationModel CurrentModel { get; private set; }
        public string CurrentCharacterId => characterId;
        public CharacterLookupMode Mode => mode;
        public int SelectedAwakeningStage => selectedAwakeningStage;
        public CharacterLookupSkillType SelectedSkillType => selectedSkillType;

        private void Start()
        {
            if (initialized)
            {
                return;
            }

            GameBootstrapper bootstrap = GameBootstrapper.Instance;
            if (bootstrap?.LocalizationService == null
                || bootstrap.SaveService == null
                || bootstrap.DefinitionDatabase == null)
            {
                GameLogger.Error(
                    "[CharacterLookupScreenController] Initialized "
                        + "LocalizationService, SaveService, and "
                        + "DefinitionDatabase are required.",
                    this);
                return;
            }

            Initialize(
                bootstrap.LocalizationService,
                bootstrap.SaveService,
                CharacterLookupDescriptionResolverFactory.CreateDefault(
                    bootstrap.DefinitionDatabase,
                    bootstrap.LocalizationService));
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(CharacterLookupScreenView configuredView)
        {
            UnsubscribeView();
            view = configuredView
                ?? throw new ArgumentNullException(nameof(configuredView));
            if (isActiveAndEnabled)
            {
                SubscribeView();
            }
        }

        public void Initialize(
            LocalizationService initializedService,
            SaveService initializedSaveService = null,
            CharacterLookupDescriptionResolver initializedDescriptionResolver =
                null)
        {
            UnsubscribeLocale();
            localizationService = initializedService
                ?? throw new ArgumentNullException(nameof(initializedService));
            saveService = initializedSaveService;
            descriptionResolver = initializedDescriptionResolver;
            if ((saveService == null) != (descriptionResolver == null))
            {
                throw new ArgumentException(
                    "SaveService and description resolver must either both "
                        + "be supplied or both be omitted.");
            }

            initialized = true;
            if (isActiveAndEnabled)
            {
                SubscribeLocale();
            }
        }

        public bool TryOpen(
            string requestedCharacterId,
            CharacterLookupMode requestedMode)
        {
            if (!initialized
                || view == null
                || string.IsNullOrWhiteSpace(requestedCharacterId)
                || !Enum.IsDefined(
                    typeof(CharacterLookupMode),
                    requestedMode))
            {
                return false;
            }

            if (!string.Equals(
                    characterId,
                    requestedCharacterId,
                    StringComparison.Ordinal))
            {
                characterId = requestedCharacterId;
                selectedAwakeningStage = DefaultAwakeningStage;
                selectedSkillType = DefaultSkillType;
            }

            mode = requestedMode;
            PublishPresentation();
            return true;
        }

        public void SetDescription(string text)
        {
            if (CurrentModel == null)
            {
                return;
            }

            CurrentModel = CurrentModel.WithDescription(text);
            view.Render(CurrentModel);
        }

        private void Subscribe()
        {
            SubscribeView();
            SubscribeLocale();
        }

        private void Unsubscribe()
        {
            UnsubscribeView();
            UnsubscribeLocale();
        }

        private void SubscribeView()
        {
            if (viewSubscribed || view == null)
            {
                return;
            }

            view.ReturnRequested += HandleReturnRequested;
            view.ModeRequested += HandleModeRequested;
            view.AwakeningStageRequested += HandleAwakeningStageRequested;
            view.SkillTypeRequested += HandleSkillTypeRequested;
            viewSubscribed = true;
        }

        private void UnsubscribeView()
        {
            if (!viewSubscribed)
            {
                return;
            }

            view.ReturnRequested -= HandleReturnRequested;
            view.ModeRequested -= HandleModeRequested;
            view.AwakeningStageRequested -= HandleAwakeningStageRequested;
            view.SkillTypeRequested -= HandleSkillTypeRequested;
            viewSubscribed = false;
        }

        private void SubscribeLocale()
        {
            if (localeSubscribed || localizationService == null)
            {
                return;
            }

            localizationService.LocaleChanged += HandleLocaleChanged;
            localeSubscribed = true;
        }

        private void UnsubscribeLocale()
        {
            if (!localeSubscribed)
            {
                return;
            }

            localizationService.LocaleChanged -= HandleLocaleChanged;
            localeSubscribed = false;
        }

        private void HandleReturnRequested()
        {
            ReturnRequested?.Invoke();
        }

        private void HandleModeRequested(CharacterLookupMode requestedMode)
        {
            if (mode == requestedMode || CurrentModel == null)
            {
                return;
            }

            mode = requestedMode;
            PublishPresentation();
        }

        private void HandleAwakeningStageRequested(int stage)
        {
            if (CurrentModel == null || stage < 1 || stage > 6)
            {
                return;
            }

            selectedAwakeningStage = stage;
            PublishPresentation();
            AwakeningStageSelected?.Invoke(stage);
        }

        private void HandleSkillTypeRequested(
            CharacterLookupSkillType skillType)
        {
            if (CurrentModel == null
                || !Enum.IsDefined(
                    typeof(CharacterLookupSkillType),
                    skillType))
            {
                return;
            }

            selectedSkillType = skillType;
            PublishPresentation();
            SkillTypeSelected?.Invoke(skillType);
        }

        private void HandleLocaleChanged(string _)
        {
            if (CurrentModel != null)
            {
                PublishPresentation();
            }
        }

        private void PublishPresentation()
        {
            string description = ResolveDescription();
            CurrentModel = new CharacterLookupPresentationModel(
                characterId,
                mode,
                selectedAwakeningStage,
                selectedSkillType,
                description);
            view.Render(CurrentModel);
            PresentationChanged?.Invoke(CurrentModel);
        }

        private string ResolveDescription()
        {
            if (saveService == null || descriptionResolver == null)
            {
                return string.Empty;
            }

            CharacterLookupDescriptionResult result;
            try
            {
                CharacterSaveData savedCharacter = FindCharacter(
                    saveService.GetCurrentProfileSnapshot(),
                    characterId);
                if (savedCharacter == null)
                {
                    result = Failure(
                        $"Owned character '{characterId}' was not found.");
                }
                else if (mode == CharacterLookupMode.Awakening)
                {
                    result = descriptionResolver.ResolveAwakening(
                        characterId,
                        selectedAwakeningStage);
                }
                else
                {
                    result = descriptionResolver.ResolveSkill(
                        characterId,
                        savedCharacter.Awakening,
                        selectedSkillType);
                }
            }
            catch (Exception exception)
            {
                result = Failure(
                    "Authoritative character lookup state could not be read. "
                        + exception.Message);
            }

            if (!result.IsSuccess)
            {
                GameLogger.Warning(
                    "[CharacterLookupScreenController] " + result.Error,
                    this);
            }

            return result.Text;
        }

        private CharacterLookupDescriptionResult Failure(string error)
        {
            return CharacterLookupDescriptionResult.Failure(
                localizationService.GetText(
                    CharacterLookupDescriptionResolver.UnavailableKey),
                error);
        }

        private static CharacterSaveData FindCharacter(
            ProfileSaveData profile,
            string requestedCharacterId)
        {
            if (profile?.Characters == null)
            {
                return null;
            }

            for (int index = 0; index < profile.Characters.Count; index++)
            {
                CharacterSaveData savedCharacter = profile.Characters[index];
                if (savedCharacter != null
                    && string.Equals(
                        savedCharacter.CharacterId,
                        requestedCharacterId,
                        StringComparison.Ordinal))
                {
                    return savedCharacter;
                }
            }

            return null;
        }
    }
}
