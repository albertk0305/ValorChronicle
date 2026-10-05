using System;
using System.Collections.Generic;
using UnityEngine;
using ValorChronicle.Core.Logging;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Roster;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Party.Presentation
{
    public sealed class CharacterSelectController : MonoBehaviour
    {
        [SerializeField] private PartyScreenController partyScreen = null;
        [SerializeField] private CharacterSelectView view = null;
        [SerializeField] private DefinitionDatabase definitionDatabase = null;
        [SerializeField]
        private CharacterPresentationCatalog presentationCatalog = null;
        [SerializeField] private ElementIconSet elementIconSet = null;

        private PartyCharacterRosterBuilder rosterBuilder;
        private IReadOnlyList<CharacterRosterEntry> fullRoster =
            Array.Empty<CharacterRosterEntry>();
        private IReadOnlyList<CharacterRosterEntry> visibleRoster =
            Array.Empty<CharacterRosterEntry>();
        private readonly Dictionary<string, CharacterRosterEntry> entriesById =
            new Dictionary<string, CharacterRosterEntry>(
                StringComparer.Ordinal);
        private readonly HashSet<string> missingPresentationWarnings =
            new HashSet<string>(StringComparer.Ordinal);
        private PartyEditSession session;
        private bool dependenciesReady;

        public ElementType? SelectedElement { get; private set; }
        public CharacterRosterSortMode SortMode { get; private set; } =
            CharacterRosterSortMode.Level;
        public IReadOnlyList<CharacterRosterEntry> FullRoster => fullRoster;
        public IReadOnlyList<CharacterRosterEntry> VisibleRoster =>
            visibleRoster;
        public PartyEditSession CurrentSession => session;
        public bool IsConfigured =>
            partyScreen != null
            && view != null
            && definitionDatabase != null
            && presentationCatalog != null
            && elementIconSet != null;

        private void OnEnable()
        {
            if (partyScreen != null)
            {
                partyScreen.CharacterSelectOpened += HandleOpened;
                partyScreen.CharacterSelectClosed += HandleClosed;
                partyScreen.CharacterSelectSessionChanged +=
                    HandleSessionChanged;
                partyScreen.CharacterSelectInteractionChanged +=
                    HandleInteractionChanged;
            }

            if (view != null)
            {
                view.CharacterRequested += HandleCharacterRequested;
                view.ElementFilterRequested += HandleElementFilterRequested;
                view.SortRequested += HandleSortRequested;
                view.ConfirmRequested += HandleConfirmRequested;
                view.SecondaryActionRequested +=
                    HandleSecondaryActionRequested;
            }
        }

        private void Start()
        {
            PrepareDependencies();
        }

        private void OnDisable()
        {
            if (partyScreen != null)
            {
                partyScreen.CharacterSelectOpened -= HandleOpened;
                partyScreen.CharacterSelectClosed -= HandleClosed;
                partyScreen.CharacterSelectSessionChanged -=
                    HandleSessionChanged;
                partyScreen.CharacterSelectInteractionChanged -=
                    HandleInteractionChanged;
            }

            if (view != null)
            {
                view.CharacterRequested -= HandleCharacterRequested;
                view.ElementFilterRequested -= HandleElementFilterRequested;
                view.SortRequested -= HandleSortRequested;
                view.ConfirmRequested -= HandleConfirmRequested;
                view.SecondaryActionRequested -=
                    HandleSecondaryActionRequested;
            }

            ResetTransientState(clearView: false);
        }

        public void PrepareDependencies()
        {
            dependenciesReady = false;
            if (!IsConfigured)
            {
                GameLogger.Error(
                    "[CharacterSelectController] Inspector references are "
                        + "incomplete.",
                    this);
                return;
            }

            if (!definitionDatabase.IsInitialized)
            {
                GameLogger.Error(
                    "[CharacterSelectController] DefinitionDatabase is not "
                        + "initialized.",
                    this);
                return;
            }

            try
            {
                presentationCatalog.Initialize();
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[CharacterSelectController] Presentation catalog "
                        + "initialization failed.",
                    this);
                return;
            }

            rosterBuilder = new PartyCharacterRosterBuilder(
                definitionDatabase,
                message => GameLogger.Warning(
                    "[CharacterSelectController] " + message,
                    this));
            dependenciesReady = true;
            partyScreen.ConfigureSlotPresentationResolvers(
                ResolveFaceSprite,
                ResolveElementIcon);
        }

        public void Open(
            PartyEditSession editSession,
            ProfileSaveData authoritativeProfile)
        {
            if (editSession == null)
            {
                throw new ArgumentNullException(nameof(editSession));
            }

            if (authoritativeProfile?.Characters == null)
            {
                throw new ArgumentException(
                    "An authoritative profile with characters is required.",
                    nameof(authoritativeProfile));
            }

            if (!dependenciesReady)
            {
                PrepareDependencies();
            }

            if (!dependenciesReady)
            {
                view?.ClearAll();
                return;
            }

            session = editSession;
            SelectedElement = null;
            SortMode = CharacterRosterSortMode.Level;
            view.SetInteractionEnabled(true);
            fullRoster = rosterBuilder.Build(authoritativeProfile.Characters);
            entriesById.Clear();
            for (int index = 0; index < fullRoster.Count; index++)
            {
                entriesById[fullRoster[index].CharacterId] =
                    fullRoster[index];
            }

            RefreshVisibleRoster(resetScroll: true);
            RefreshPreview();
        }

        private void HandleOpened(
            PartyEditSession editSession,
            ProfileSaveData authoritativeProfile)
        {
            try
            {
                Open(editSession, authoritativeProfile);
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[CharacterSelectController] CharacterSelect open failed.",
                    this);
                view?.ClearAll();
            }
        }

        private void HandleClosed()
        {
            ResetTransientState(clearView: true);
        }

        private void HandleSessionChanged()
        {
            if (session == null)
            {
                return;
            }

            view.RefreshCellSelection(session.PreviewCharacterId);
            RefreshPreview();
        }

        private void HandleInteractionChanged(bool enabled)
        {
            view?.SetInteractionEnabled(enabled);
        }

        private void HandleCharacterRequested(string characterId)
        {
            if (session == null || !entriesById.ContainsKey(characterId))
            {
                return;
            }

            session.SetPreviewCharacterId(characterId);
            view.RefreshCellSelection(session.PreviewCharacterId);
            RefreshPreview();
        }

        private void HandleElementFilterRequested(ElementType element)
        {
            if (session == null)
            {
                return;
            }

            SelectedElement = SelectedElement.HasValue
                && SelectedElement.Value == element
                    ? null
                    : element;
            RefreshVisibleRoster(resetScroll: true);
        }

        private void HandleSortRequested(CharacterRosterSortMode sortMode)
        {
            if (session == null || SortMode == sortMode)
            {
                return;
            }

            SortMode = sortMode;
            RefreshVisibleRoster(resetScroll: true);
        }

        private void HandleConfirmRequested()
        {
            if (session != null)
            {
                partyScreen.ConfirmCharacterSelection();
            }
        }

        private void HandleSecondaryActionRequested()
        {
            if (session != null)
            {
                partyScreen.PerformCharacterSelectSecondaryAction();
            }
        }

        private void RefreshVisibleRoster(bool resetScroll)
        {
            visibleRoster = CharacterRosterQuery.Apply(
                fullRoster,
                SelectedElement,
                SortMode);
            view.BindControls(SelectedElement, SortMode);
            view.BindCells(
                visibleRoster,
                session?.PreviewCharacterId ?? string.Empty,
                ResolveFaceSprite,
                elementIconSet.GetIcon,
                resetScroll);
        }

        private void RefreshPreview()
        {
            if (session == null
                || string.IsNullOrEmpty(session.PreviewCharacterId)
                || !entriesById.TryGetValue(
                    session.PreviewCharacterId,
                    out CharacterRosterEntry entry))
            {
                view.ClearPreview();
                return;
            }

            view.BindPreview(
                entry,
                ResolveFaceSprite(entry.CharacterId),
                elementIconSet.GetIcon(entry.Element));
        }

        private Sprite ResolveFaceSprite(string characterId)
        {
            return TryGetPresentation(characterId, out var presentation)
                ? presentation.FaceSprite
                : null;
        }

        private Sprite ResolveElementIcon(string characterId)
        {
            return definitionDatabase.TryGetCharacter(
                characterId,
                out CharacterDefinition definition)
                    ? elementIconSet.GetIcon(definition.Element)
                    : null;
        }

        private bool TryGetPresentation(
            string characterId,
            out CharacterPresentationDefinition presentation)
        {
            if (presentationCatalog.TryGet(characterId, out presentation))
            {
                return true;
            }

            if (missingPresentationWarnings.Add(characterId))
            {
                GameLogger.Warning(
                    "[CharacterSelectController] Character presentation is "
                        + "missing: " + characterId,
                    this);
            }

            return false;
        }

        private void ResetTransientState(bool clearView)
        {
            session = null;
            SelectedElement = null;
            SortMode = CharacterRosterSortMode.Level;
            fullRoster = Array.Empty<CharacterRosterEntry>();
            visibleRoster = Array.Empty<CharacterRosterEntry>();
            entriesById.Clear();
            missingPresentationWarnings.Clear();
            if (clearView && view != null)
            {
                view.ClearAll();
            }
        }

        private void OnValidate()
        {
            if (!IsConfigured)
            {
                GameLogger.Warning(
                    "[CharacterSelectController] Inspector references are "
                        + "incomplete.",
                    this);
            }
        }
    }
}
