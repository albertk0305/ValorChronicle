using System;
using System.Collections.Generic;
using UnityEngine;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Logging;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Presentation;
using ValorChronicle.Party.Roster;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Characters.Presentation
{
    public sealed class CharacterRosterScreenController : MonoBehaviour
    {
        private CharacterRosterScreenView view;
        private CharacterPresentationCatalog presentationCatalog;
        private ElementIconSet elementIconSet;
        private SaveService saveService;
        private PartyCharacterRosterBuilder rosterBuilder;
        private IReadOnlyList<CharacterRosterEntry> fullRoster =
            Array.Empty<CharacterRosterEntry>();
        private IReadOnlyList<CharacterRosterEntry> visibleRoster =
            Array.Empty<CharacterRosterEntry>();
        private readonly HashSet<string> ownedCharacterIds =
            new HashSet<string>(StringComparer.Ordinal);
        private bool subscribed;
        private bool initialized;

        public event Action<string> SelectedCharacterChanged;

        public string SelectedCharacterId { get; private set; } =
            string.Empty;
        public ElementType? SelectedElement { get; private set; }
        public CharacterRosterSortMode SortMode { get; private set; } =
            CharacterRosterSortMode.Level;
        public IReadOnlyList<CharacterRosterEntry> FullRoster => fullRoster;
        public IReadOnlyList<CharacterRosterEntry> VisibleRoster =>
            visibleRoster;

        private void OnEnable()
        {
            Subscribe();
        }

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
                    "[CharacterRosterScreenController] Initialized "
                        + "SaveService and DefinitionDatabase are required.",
                    this);
                view?.Clear();
                return;
            }

            try
            {
                Initialize(
                    bootstrap.SaveService,
                    bootstrap.DefinitionDatabase);
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[CharacterRosterScreenController] Initialization "
                        + "failed.",
                    this);
                view?.Clear();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(
            CharacterRosterScreenView configuredView,
            CharacterPresentationCatalog configuredPresentationCatalog,
            ElementIconSet configuredElementIconSet)
        {
            Unsubscribe();
            view = configuredView;
            presentationCatalog = configuredPresentationCatalog;
            elementIconSet = configuredElementIconSet;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        public void Initialize(
            SaveService initializedSaveService,
            DefinitionDatabase definitionDatabase)
        {
            saveService = initializedSaveService
                ?? throw new ArgumentNullException(
                    nameof(initializedSaveService));
            if (definitionDatabase == null)
            {
                throw new ArgumentNullException(nameof(definitionDatabase));
            }

            if (view == null
                || presentationCatalog == null
                || elementIconSet == null)
            {
                throw new InvalidOperationException(
                    "Character roster screen dependencies are incomplete.");
            }

            if (!definitionDatabase.IsInitialized)
            {
                throw new InvalidOperationException(
                    "DefinitionDatabase must be initialized.");
            }

            presentationCatalog.Initialize();
            rosterBuilder = new PartyCharacterRosterBuilder(
                definitionDatabase,
                message => GameLogger.Warning(
                    "[CharacterRosterScreenController] " + message,
                    this));
            initialized = true;
            RefreshFromAuthoritativeProfile(resetControls: true);
        }

        public void RefreshFromAuthoritativeProfile(bool resetControls)
        {
            if (!initialized
                || saveService == null
                || !saveService.HasCurrentProfile)
            {
                view?.Clear();
                return;
            }

            ProfileSaveData profile =
                saveService.GetCurrentProfileSnapshot();
            if (profile?.Characters == null || profile.Currencies == null)
            {
                throw new InvalidOperationException(
                    "The current profile has no roster or currencies.");
            }

            if (resetControls)
            {
                SelectedElement = null;
                SortMode = CharacterRosterSortMode.Level;
            }

            fullRoster = rosterBuilder.Build(profile.Characters);
            ownedCharacterIds.Clear();
            for (int index = 0; index < fullRoster.Count; index++)
            {
                ownedCharacterIds.Add(fullRoster[index].CharacterId);
            }

            if (!ownedCharacterIds.Contains(SelectedCharacterId))
            {
                SelectedCharacterId = string.Empty;
            }

            view.BindCurrencies(
                profile.Currencies.GachaCurrency,
                profile.Currencies.BattleRecords);
            RefreshVisibleRoster(resetScroll: resetControls);
        }

        private void HandleCharacterRequested(string characterId)
        {
            if (!ownedCharacterIds.Contains(characterId))
            {
                return;
            }

            SelectedCharacterId = characterId;
            view.RefreshSelection(characterId);
            SelectedCharacterChanged?.Invoke(characterId);
        }

        private void HandleElementFilterRequested(ElementType element)
        {
            SelectedElement = SelectedElement.HasValue
                && SelectedElement.Value == element
                    ? null
                    : element;
            RefreshVisibleRoster(resetScroll: true);
        }

        private void HandleSortRequested(CharacterRosterSortMode sortMode)
        {
            if (SortMode == sortMode)
            {
                return;
            }

            SortMode = sortMode;
            RefreshVisibleRoster(resetScroll: true);
        }

        private void RefreshVisibleRoster(bool resetScroll)
        {
            visibleRoster = CharacterRosterQuery.Apply(
                fullRoster,
                SelectedElement,
                SortMode);
            view.BindControls(SelectedElement, SortMode);
            view.BindRoster(
                visibleRoster,
                SelectedCharacterId,
                ResolveFaceSprite,
                elementIconSet.GetIcon,
                resetScroll);
        }

        private Sprite ResolveFaceSprite(string characterId)
        {
            return presentationCatalog.TryGet(
                characterId,
                out CharacterPresentationDefinition presentation)
                    ? presentation.FaceSprite
                    : null;
        }

        private void Subscribe()
        {
            if (subscribed || view == null)
            {
                return;
            }

            view.CharacterRequested += HandleCharacterRequested;
            view.ElementFilterRequested += HandleElementFilterRequested;
            view.SortRequested += HandleSortRequested;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            view.CharacterRequested -= HandleCharacterRequested;
            view.ElementFilterRequested -= HandleElementFilterRequested;
            view.SortRequested -= HandleSortRequested;
            subscribed = false;
        }
    }
}
