using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Battle.Entry;
using ValorChronicle.Battle.Results;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Logging;
using ValorChronicle.Core.Scene;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;
using ValorChronicle.Save.DTO;

namespace ValorChronicle.Main.Presentation
{
    public sealed class MainScreenController : MonoBehaviour
    {
        [SerializeField] private Button battleButton = null;
        [SerializeField] private GameObject battlePanel = null;
        [SerializeField] private BossSelectionListView bossSelectionList = null;
        [SerializeField] private GameObject levelSelect = null;
        [SerializeField] private LevelSelectView levelSelectView = null;
        [SerializeField] private GameObject unvalidParty = null;
        [SerializeField] private UnvalidPartyView unvalidPartyView = null;
        [SerializeField] private CanvasGroup inputCanvasGroup = null;
        [SerializeField] private TMP_Text goldText = null;

        private bool battleEntryInProgress;
        private SceneService subscribedSceneService;

        public event Action<string> BossSelected;

        public string SelectedBossId { get; private set; }
        public Button BattleButton => battleButton;
        public GameObject BattlePanel => battlePanel;
        public BossSelectionListView BossSelectionList => bossSelectionList;
        public GameObject LevelSelect => levelSelect;
        public LevelSelectView LevelSelectView => levelSelectView;
        public GameObject BossScrollPanel =>
            bossSelectionList?.ScrollRect?.gameObject;
        public GameObject UnvalidParty => unvalidParty;
        public UnvalidPartyView UnvalidPartyView => unvalidPartyView;
        public CanvasGroup InputCanvasGroup => inputCanvasGroup;
        public TMP_Text GoldText => goldText;
        public bool IsBattleEntryInProgress => battleEntryInProgress;
        public bool IsConfigured =>
            battleButton != null
            && battlePanel != null
            && bossSelectionList != null
            && levelSelect != null
            && levelSelectView != null
            && unvalidParty != null
            && unvalidPartyView != null
            && inputCanvasGroup != null
            && goldText != null;

        private void Awake()
        {
            battleButton.onClick.AddListener(HandleBattleButtonClicked);
            bossSelectionList.SelectionRequested += HandleBossSelected;
            levelSelectView.ReturnRequested += HandleReturnRequested;
            levelSelectView.DifficultySelected += HandleDifficultySelected;
            InitializeMainEntryState();
        }

        private void OnEnable()
        {
            RefreshGoldDisplay();
            BindSceneLoadingState();
        }

        private void OnDisable()
        {
            UnbindSceneLoadingState();
        }

        private void InitializeMainEntryState()
        {
            SelectedBossId = null;
            battleEntryInProgress = false;
            levelSelectView.ResetPresentation();
            SetInputLocked(false);
            SetBossListVisible(true);
            battlePanel.SetActive(false);
            levelSelect.SetActive(false);
            unvalidPartyView.Hide();
        }

        public void RefreshGoldDisplay()
        {
            if (goldText == null)
            {
                GameLogger.Error(
                    "[MainScreenController] GoldText is not configured.",
                    this);
                return;
            }

            goldText.text = "-";
            GameBootstrapper bootstrapper = GameBootstrapper.Instance;
            if (bootstrapper == null)
            {
                GameLogger.Error(
                    "[MainScreenController] GameBootstrapper is missing "
                        + "while refreshing Gold.",
                    this);
                return;
            }

            if (bootstrapper.SaveService == null)
            {
                GameLogger.Error(
                    "[MainScreenController] SaveService is unavailable "
                        + "while refreshing Gold.",
                    this);
                return;
            }

            if (!bootstrapper.SaveService.HasCurrentProfile)
            {
                GameLogger.Error(
                    "[MainScreenController] No current profile is available "
                        + "while refreshing Gold.",
                    this);
                return;
            }

            ProfileSaveData profile = GetCurrentProfileSnapshot(
                bootstrapper);
            if (profile?.Currencies == null)
            {
                GameLogger.Error(
                    "[MainScreenController] The current profile has no "
                        + "currency data.",
                    this);
                return;
            }

            goldText.text = FormatGold(profile.Currencies.GachaCurrency);
        }

        public static string FormatGold(long value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }

        private void OnDestroy()
        {
            UnbindSceneLoadingState();
            if (battleButton != null)
            {
                battleButton.onClick.RemoveListener(
                    HandleBattleButtonClicked);
            }

            if (bossSelectionList != null)
            {
                bossSelectionList.SelectionRequested -= HandleBossSelected;
            }

            if (levelSelectView != null)
            {
                levelSelectView.ReturnRequested -= HandleReturnRequested;
                levelSelectView.DifficultySelected -=
                    HandleDifficultySelected;
            }
        }

        private void HandleBattleButtonClicked()
        {
            SelectedBossId = null;
            levelSelectView.ResetPresentation();
            levelSelect.SetActive(false);
            unvalidPartyView.Hide();
            battlePanel.SetActive(true);
            SetBossListVisible(true);

            GameBootstrapper bootstrapper = GameBootstrapper.Instance;
            if (bootstrapper?.DefinitionDatabase == null
                || bootstrapper.LocalizationService == null)
            {
                GameLogger.Error(
                    "[MainScreenController] Initialized content services "
                        + "are required to show bosses.",
                    this);
                bossSelectionList.Show(Array.Empty<BossSelectionItem>());
                return;
            }

            ProfileSaveData profile = GetCurrentProfileSnapshot(
                bootstrapper);

            bossSelectionList.Populate(
                bootstrapper.DefinitionDatabase,
                bootstrapper.LocalizationService,
                profile);
        }

        private void HandleBossSelected(string bossId)
        {
            if (string.IsNullOrWhiteSpace(bossId))
            {
                GameLogger.Error(
                    "[MainScreenController] A valid boss ID is required.",
                    this);
                return;
            }

            GameBootstrapper bootstrapper = GameBootstrapper.Instance;
            if (bootstrapper?.DefinitionDatabase == null
                || bootstrapper.LocalizationService == null)
            {
                GameLogger.Error(
                    "[MainScreenController] Initialized content services "
                        + "are required to select a boss.",
                    this);
                return;
            }

            ProfileSaveData profile = GetCurrentProfileSnapshot(
                bootstrapper);
            IReadOnlyList<BossSelectionItem> items =
                bossSelectionList.BuildItems(
                    bootstrapper.DefinitionDatabase,
                    bootstrapper.LocalizationService,
                    profile);
            BossSelectionItem selectedItem = null;
            for (int index = 0; index < items.Count; index++)
            {
                if (string.Equals(
                        items[index].BossId,
                        bossId,
                        StringComparison.Ordinal))
                {
                    selectedItem = items[index];
                    break;
                }
            }

            if (selectedItem == null)
            {
                GameLogger.Error(
                    $"[MainScreenController] Boss '{bossId}' is not "
                        + "available for selection.",
                    this);
                return;
            }

            SelectedBossId = bossId;
            levelSelectView.Bind(selectedItem, profile);
            unvalidPartyView.Hide();
            SetBossListVisible(false);
            levelSelect.SetActive(true);
            BossSelected?.Invoke(bossId);
        }

        private void HandleReturnRequested()
        {
            SelectedBossId = null;
            unvalidPartyView.Hide();
            levelSelectView.ResetPresentation();
            levelSelect.SetActive(false);
            SetBossListVisible(true);
        }

        private async void HandleDifficultySelected(string difficultyId)
        {
            if (battleEntryInProgress)
            {
                return;
            }

            GameBootstrapper bootstrapper = GameBootstrapper.Instance;
            if (!TryValidateBattleSelection(
                    bootstrapper,
                    difficultyId,
                    out string selectionError))
            {
                GameLogger.Error(
                    "[MainScreenController] Battle entry rejected. "
                        + selectionError,
                    this);
                return;
            }

            SceneService sceneService = bootstrapper.SceneService;
            if (sceneService.IsLoading)
            {
                GameLogger.Warning(
                    "[MainScreenController] Battle entry ignored because "
                        + "a scene load is already active.",
                    this);
                return;
            }

            ProfileSaveData profile = GetCurrentProfileSnapshot(
                bootstrapper);
            BattlePartyResolutionResult partyResolution =
                new BattlePartyResolver(
                    bootstrapper.DefinitionDatabase).Resolve(profile);
            if (partyResolution.Status
                == BattlePartyResolutionStatus.EmptyParty)
            {
                unvalidPartyView.ShowEmptyParty(
                    bootstrapper.LocalizationService);
                return;
            }

            if (!partyResolution.IsSuccess)
            {
                GameLogger.Error(
                    "[MainScreenController] Battle entry rejected because "
                        + "the active party is invalid. "
                        + partyResolution.ErrorMessage,
                    this);
                return;
            }

            var request = new BattleEntryRequest(
                SelectedBossId,
                difficultyId);
            BattleEntryService entryService =
                bootstrapper.BattleEntryService;
            if (!entryService.TryRegister(request))
            {
                GameLogger.Error(
                    "[MainScreenController] Battle entry request could not "
                        + "be registered because another request is pending.",
                    this);
                return;
            }

            battleEntryInProgress = true;
            unvalidPartyView.Hide();
            SetInputLocked(true);

            bool loadSucceeded = false;
            try
            {
                loadSucceeded = await sceneService.LoadAsync(
                    GameScene.Battle);
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
            }

            if (loadSucceeded)
            {
                return;
            }

            if (!entryService.TryCancel(request))
            {
                GameLogger.Error(
                    "[MainScreenController] Failed to cancel the battle "
                        + "entry request after scene loading failed.",
                    this);
            }

            if (this == null)
            {
                return;
            }

            battleEntryInProgress = false;
            SetInputLocked(false);
            GameLogger.Error(
                "[MainScreenController] Battle scene loading failed.",
                this);
        }

        private bool TryValidateBattleSelection(
            GameBootstrapper bootstrapper,
            string difficultyId,
            out string error)
        {
            if (bootstrapper == null
                || bootstrapper.InitializationResult == null
                || !bootstrapper.InitializationResult.IsSuccess)
            {
                error = "Game initialization is not complete.";
                return false;
            }

            if (bootstrapper.DefinitionDatabase == null
                || bootstrapper.SaveService == null
                || !bootstrapper.SaveService.HasCurrentProfile
                || bootstrapper.BattleEntryService == null
                || bootstrapper.SceneService == null)
            {
                error = "Required battle entry services are unavailable.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(SelectedBossId)
                || !string.Equals(
                    SelectedBossId,
                    levelSelectView.BossId,
                    StringComparison.Ordinal))
            {
                error = "The selected boss is missing or stale.";
                return false;
            }

            if (!IsKnownDifficulty(difficultyId))
            {
                error = $"DifficultyId '{difficultyId}' is not recognized.";
                return false;
            }

            if (!bootstrapper.DefinitionDatabase.TryGetBoss(
                    SelectedBossId,
                    out BossDefinition boss))
            {
                error = $"BossDefinition '{SelectedBossId}' was not found.";
                return false;
            }

            if (!BattleEncounterFactory.IsSupportedBoss(
                    boss,
                    out string supportError))
            {
                error = supportError;
                return false;
            }

            if (!boss.TryGetDifficultyStats(difficultyId, out _))
            {
                error = $"Boss '{SelectedBossId}' does not support "
                    + $"difficulty '{difficultyId}'.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool IsKnownDifficulty(string difficultyId)
        {
            for (int index = 0;
                index < BattleDifficultyIds.RequiredIds.Count;
                index++)
            {
                if (string.Equals(
                        BattleDifficultyIds.RequiredIds[index],
                        difficultyId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void SetInputLocked(bool locked)
        {
            inputCanvasGroup.interactable = !locked;
            inputCanvasGroup.blocksRaycasts = true;
        }

        private void BindSceneLoadingState()
        {
            SceneService sceneService =
                GameBootstrapper.Instance?.SceneService;
            if (ReferenceEquals(subscribedSceneService, sceneService))
            {
                if (sceneService != null)
                {
                    HandleSceneLoadingStateChanged(sceneService.IsLoading);
                }

                return;
            }

            UnbindSceneLoadingState();
            subscribedSceneService = sceneService;
            if (subscribedSceneService == null)
            {
                return;
            }

            subscribedSceneService.LoadingStateChanged +=
                HandleSceneLoadingStateChanged;
            HandleSceneLoadingStateChanged(
                subscribedSceneService.IsLoading);
        }

        private void UnbindSceneLoadingState()
        {
            if (subscribedSceneService == null)
            {
                return;
            }

            subscribedSceneService.LoadingStateChanged -=
                HandleSceneLoadingStateChanged;
            subscribedSceneService = null;
        }

        private void HandleSceneLoadingStateChanged(bool isLoading)
        {
            if (inputCanvasGroup == null
                || (!isLoading && battleEntryInProgress))
            {
                return;
            }

            SetInputLocked(isLoading);
        }

        private void SetBossListVisible(bool visible)
        {
            GameObject scrollPanel = BossScrollPanel;
            if (scrollPanel != null)
            {
                scrollPanel.SetActive(visible);
            }
        }

        private ProfileSaveData GetCurrentProfileSnapshot(
            GameBootstrapper bootstrapper)
        {
            try
            {
                if (bootstrapper.SaveService?.HasCurrentProfile == true)
                {
                    return bootstrapper.SaveService
                        .GetCurrentProfileSnapshot();
                }
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
            }

            return null;
        }
    }
}
