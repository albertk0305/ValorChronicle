using System;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Logging;
using ValorChronicle.Party.Persistence;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Party.Presentation
{
    public sealed class PartyScreenController : MonoBehaviour
    {
        public event Action<PartyEditSession, ProfileSaveData>
            CharacterSelectOpened;
        public event Action CharacterSelectClosed;
        public event Action CharacterSelectSessionChanged;
        public event Action<bool> CharacterSelectInteractionChanged;

        [SerializeField]
        private PartyPresetView[] presetViews =
            Array.Empty<PartyPresetView>();

        [SerializeField]
        private CanvasGroup partyInputGroup = null;

        [SerializeField]
        private GameObject characterSelect = null;

        [SerializeField]
        private Button exitButton = null;

        private SaveService saveService;
        private PartyPresetEditor presetEditor;
        private PartyPersistenceService persistenceService;
        private ProfileSaveData currentSnapshot;
        private bool operationInProgress;
        private bool started;

        public PartyEditSession CurrentEditSession { get; private set; }
        public bool IsCharacterSelectOpen =>
            CurrentEditSession != null
            && characterSelect != null
            && characterSelect.activeSelf;
        public bool IsOperationInProgress => operationInProgress;
        public bool IsConfigured =>
            presetViews != null
            && presetViews.Length == SaveRules.PartyPresetCount
            && Array.TrueForAll(presetViews, view => view != null)
            && partyInputGroup != null
            && characterSelect != null
            && exitButton != null;

        private void Awake()
        {
            if (characterSelect != null)
            {
                characterSelect.SetActive(false);
            }
        }

        private void OnEnable()
        {
            SubscribeToViews();
            if (exitButton != null)
            {
                exitButton.onClick.AddListener(HandleExitClick);
            }

            if (started && saveService != null)
            {
                RefreshFromAuthoritativeProfile();
            }
        }

        private void Start()
        {
            started = true;
            if (saveService != null)
            {
                RefreshFromAuthoritativeProfile();
                return;
            }

            SaveService bootstrapSaveService =
                GameBootstrapper.Instance?.SaveService;
            if (bootstrapSaveService == null)
            {
                GameLogger.Error(
                    "[PartyScreenController] SaveService is not initialized.",
                    this);
                SetPartyInputEnabled(false);
                return;
            }

            Initialize(bootstrapSaveService);
        }

        private void OnDisable()
        {
            UnsubscribeFromViews();
            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(HandleExitClick);
            }

            ResetTransientState();
        }

        public void Initialize(SaveService initializedSaveService)
        {
            saveService = initializedSaveService
                ?? throw new ArgumentNullException(
                    nameof(initializedSaveService));
            presetEditor = new PartyPresetEditor();
            persistenceService = new PartyPersistenceService(
                saveService,
                presetEditor);
            RefreshFromAuthoritativeProfile();
        }

        public void OpenCharacterSelect(
            int presetIndex,
            int slotIndex)
        {
            if (operationInProgress
                || CurrentEditSession != null
                || currentSnapshot == null)
            {
                return;
            }

            ValidatePresetIndex(presetIndex);
            ValidateSlotIndex(slotIndex);
            ProfileSaveData latestProfile;
            try
            {
                latestProfile = saveService.GetCurrentProfileSnapshot();
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[PartyScreenController] CharacterSelect could not read "
                        + "the current profile.",
                    this);
                return;
            }

            if (!TryValidateProfile(latestProfile, out string error))
            {
                GameLogger.Error(
                    "[PartyScreenController] CharacterSelect cannot open. "
                        + error,
                    this);
                return;
            }

            Render(latestProfile);
            PartyPresetSaveData preset =
                latestProfile.Party.Presets[presetIndex];
            CurrentEditSession = new PartyEditSession(
                presetEditor,
                presetIndex,
                slotIndex,
                preset.CharacterSlotIds);
            characterSelect.SetActive(true);
            SetPartyInputEnabled(false);
            CharacterSelectOpened?.Invoke(
                CurrentEditSession,
                latestProfile);
        }

        public void CloseCharacterSelectWithoutSaving()
        {
            CurrentEditSession = null;
            if (characterSelect != null)
            {
                characterSelect.SetActive(false);
            }

            CharacterSelectClosed?.Invoke();
            SetPartyInputEnabled(!operationInProgress);
        }

        public void ConfirmCharacterSelection()
        {
            if (operationInProgress
                || CurrentEditSession == null
                || persistenceService == null
                || string.IsNullOrEmpty(
                    CurrentEditSession.PreviewCharacterId))
            {
                return;
            }

            ExecuteCharacterSelectPersistence(
                () => persistenceService.ConfirmEdit(
                    CurrentEditSession.PresetIndex,
                    CurrentEditSession.SlotIndex,
                    CurrentEditSession.PreviewCharacterId),
                renderNoChangeSnapshot: false,
                "Confirm");
        }

        public void PerformCharacterSelectSecondaryAction()
        {
            if (operationInProgress || CurrentEditSession == null)
            {
                return;
            }

            switch (CurrentEditSession.SecondaryAction)
            {
                case PartyEditSecondaryAction.Revert:
                    CurrentEditSession.Revert();
                    CharacterSelectSessionChanged?.Invoke();
                    return;

                case PartyEditSecondaryAction.Clear:
                    if (persistenceService == null)
                    {
                        return;
                    }

                    ExecuteCharacterSelectPersistence(
                        () => persistenceService.ClearSlot(
                            CurrentEditSession.PresetIndex,
                            CurrentEditSession.SlotIndex),
                        renderNoChangeSnapshot: true,
                        "Clear");
                    return;

                case PartyEditSecondaryAction.CloseWithoutChange:
                    CloseCharacterSelectWithoutSaving();
                    return;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void HandleSelectRequested(int presetIndex)
        {
            if (operationInProgress
                || IsCharacterSelectOpen
                || persistenceService == null)
            {
                return;
            }

            operationInProgress = true;
            SetPartyInputEnabled(false);
            try
            {
                PartyPersistenceOperationResult result =
                    persistenceService.SetActivePreset(presetIndex);
                if (result.Status
                    == PartyPersistenceStatus.ChangedAndSaved)
                {
                    Render(result.ProfileSnapshot);
                }
                else if (result.Status == PartyPersistenceStatus.Failed)
                {
                    string transactionStatus =
                        result.SaveTransactionResult == null
                            ? "NotStarted"
                            : result.SaveTransactionResult.Status.ToString();
                    GameLogger.Error(
                        "[PartyScreenController] Active preset save failed. "
                            + $"TransactionStatus={transactionStatus}.",
                        this);
                }
            }
            finally
            {
                operationInProgress = false;
                SetPartyInputEnabled(true);
            }
        }

        private void ExecuteCharacterSelectPersistence(
            Func<PartyPersistenceOperationResult> operation,
            bool renderNoChangeSnapshot,
            string operationName)
        {
            operationInProgress = true;
            CharacterSelectInteractionChanged?.Invoke(false);
            bool closeCharacterSelect = false;
            try
            {
                PartyPersistenceOperationResult result = operation();
                switch (result.Status)
                {
                    case PartyPersistenceStatus.ChangedAndSaved:
                        Render(result.ProfileSnapshot);
                        closeCharacterSelect = true;
                        break;

                    case PartyPersistenceStatus.NoChange:
                        if (renderNoChangeSnapshot)
                        {
                            Render(result.ProfileSnapshot);
                        }

                        closeCharacterSelect = true;
                        break;

                    case PartyPersistenceStatus.Failed:
                        LogCharacterSelectPersistenceFailure(
                            operationName,
                            result);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    $"[PartyScreenController] {operationName} failed.",
                    this);
            }
            finally
            {
                operationInProgress = false;
                if (closeCharacterSelect)
                {
                    CloseCharacterSelectWithoutSaving();
                }
                else if (CurrentEditSession != null)
                {
                    CharacterSelectInteractionChanged?.Invoke(true);
                }
            }
        }

        private void LogCharacterSelectPersistenceFailure(
            string operationName,
            PartyPersistenceOperationResult result)
        {
            string transactionStatus = result.SaveTransactionResult == null
                ? "NotStarted"
                : result.SaveTransactionResult.Status.ToString();
            GameLogger.Error(
                $"[PartyScreenController] {operationName} save failed. "
                    + $"TransactionStatus={transactionStatus}.",
                this);
        }

        private void HandleSlotRequested(
            int presetIndex,
            int slotIndex)
        {
            OpenCharacterSelect(presetIndex, slotIndex);
        }

        private void HandleExitClick()
        {
            CurrentEditSession = null;
            if (characterSelect != null)
            {
                characterSelect.SetActive(false);
            }

            CharacterSelectClosed?.Invoke();
            SetPartyInputEnabled(!operationInProgress);
        }

        private void RefreshFromAuthoritativeProfile()
        {
            if (saveService == null || !saveService.HasCurrentProfile)
            {
                GameLogger.Error(
                    "[PartyScreenController] No current profile is available.",
                    this);
                SetPartyInputEnabled(false);
                return;
            }

            try
            {
                Render(saveService.GetCurrentProfileSnapshot());
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[PartyScreenController] Party screen refresh failed.",
                    this);
                SetPartyInputEnabled(false);
            }
        }

        private void Render(ProfileSaveData profile)
        {
            if (!TryValidateProfile(profile, out string error))
            {
                GameLogger.Error(
                    $"[PartyScreenController] Cannot render party. {error}",
                    this);
                SetPartyInputEnabled(false);
                return;
            }

            currentSnapshot = profile;
            bool inputEnabled = !operationInProgress
                && CurrentEditSession == null;
            for (int presetIndex = 0;
                presetIndex < SaveRules.PartyPresetCount;
                presetIndex++)
            {
                PartyPresetSaveData preset =
                    profile.Party.Presets[presetIndex];
                presetViews[presetIndex].Bind(
                    presetIndex,
                    preset.CharacterSlotIds,
                    profile.Party.ActivePresetIndex == presetIndex,
                    inputEnabled);
            }

            SetPartyInputEnabled(inputEnabled);
        }

        private void SetPartyInputEnabled(bool inputEnabled)
        {
            if (partyInputGroup != null)
            {
                partyInputGroup.interactable = inputEnabled;
                partyInputGroup.blocksRaycasts = true;
            }

            if (presetViews != null)
            {
                for (int index = 0; index < presetViews.Length; index++)
                {
                    presetViews[index]?.SetInputEnabled(inputEnabled);
                }
            }
        }

        private void ResetTransientState()
        {
            operationInProgress = false;
            CurrentEditSession = null;
            if (characterSelect != null)
            {
                characterSelect.SetActive(false);
            }

            CharacterSelectClosed?.Invoke();
            SetPartyInputEnabled(true);
        }

        private void SubscribeToViews()
        {
            if (presetViews == null)
            {
                return;
            }

            for (int index = 0; index < presetViews.Length; index++)
            {
                PartyPresetView view = presetViews[index];
                if (view == null)
                {
                    continue;
                }

                view.SelectRequested += HandleSelectRequested;
                view.SlotRequested += HandleSlotRequested;
            }
        }

        private void UnsubscribeFromViews()
        {
            if (presetViews == null)
            {
                return;
            }

            for (int index = 0; index < presetViews.Length; index++)
            {
                PartyPresetView view = presetViews[index];
                if (view == null)
                {
                    continue;
                }

                view.SelectRequested -= HandleSelectRequested;
                view.SlotRequested -= HandleSlotRequested;
            }
        }

        private bool TryValidateProfile(
            ProfileSaveData profile,
            out string error)
        {
            if (!IsConfigured)
            {
                error = "Inspector references are incomplete.";
                return false;
            }

            if (profile?.Party?.Presets == null
                || profile.Party.Presets.Count
                    != SaveRules.PartyPresetCount)
            {
                error = $"Expected {SaveRules.PartyPresetCount} presets.";
                return false;
            }

            if (profile.Party.ActivePresetIndex < 0
                || profile.Party.ActivePresetIndex
                    >= SaveRules.PartyPresetCount)
            {
                error = "ActivePresetIndex is outside the valid range.";
                return false;
            }

            for (int index = 0;
                index < SaveRules.PartyPresetCount;
                index++)
            {
                PartyPresetSaveData preset = profile.Party.Presets[index];
                if (preset?.CharacterSlotIds == null
                    || preset.CharacterSlotIds.Count
                        != SaveRules.PartySlotCount)
                {
                    error = $"Preset {index} does not contain "
                        + $"{SaveRules.PartySlotCount} slots.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private static void ValidatePresetIndex(int presetIndex)
        {
            if (presetIndex < 0
                || presetIndex >= SaveRules.PartyPresetCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(presetIndex));
            }
        }

        private static void ValidateSlotIndex(int slotIndex)
        {
            if (slotIndex < 0
                || slotIndex >= SaveRules.PartySlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            }
        }

        private void OnValidate()
        {
            if (!IsConfigured)
            {
                GameLogger.Warning(
                    "[PartyScreenController] Requires exactly five preset "
                        + "views, Party CanvasGroup, CharSelect, and ExitButton.",
                    this);
            }
        }
    }
}
