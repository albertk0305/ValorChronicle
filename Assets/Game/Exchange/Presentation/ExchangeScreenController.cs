using System;
using UnityEngine;
using ValorChronicle.Core.Logging;
using ValorChronicle.Data.Database;
using ValorChronicle.Exchange.Persistence;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Exchange.Presentation
{
    public sealed class ExchangeScreenController : MonoBehaviour
    {
        private ExchangeScreenView view;
        private VirtualizedExchangeGridView grid;
        private ExchangeConfirmView confirmView;
        private Func<DefinitionDatabase, ExchangeSpriteResolver>
            spriteResolverFactory;
        private SaveService saveService;
        private DefinitionDatabase database;
        private ExchangeSpriteResolver spriteResolver;
        private IExchangeTransactionExecutor transactionService;
        private Action<string> showFeedback;
        private ExchangeCatalogData currentCatalog;
        private ExchangeConfirmDisplayData selectedDisplayData;
        private ExchangeContentKind selectedContentKind;
        private string selectedContentId = string.Empty;
        private bool ready;
        private bool subscribed;
        private bool hasSelectedTab;
        private bool transactionInProgress;
        private bool retryBlocked;
        private ExchangeTab selectedTab;

        public event Action Opened;
        public event Action Closed;
        public event Action AvailabilityChanged;
        public event Action<ExchangeContentKind, string>
            SelectionRequested;

        public ExchangeScreenView View => view;
        public VirtualizedExchangeGridView Grid => grid;
        public ExchangeConfirmView ConfirmView => confirmView;
        public bool IsConfigured => view != null
            && view.IsConfigured
            && grid != null
            && grid.IsConfigured
            && confirmView != null
            && confirmView.IsConfigured
            && spriteResolverFactory != null
            && showFeedback != null;
        public bool IsReady => ready;
        public bool IsOpen => view != null && view.IsOpen;
        public bool IsConfirmOpen => confirmView != null
            && confirmView.IsOpen;
        public bool IsTransactionInProgress => transactionInProgress;
        public bool HasSelection => selectedDisplayData != null;
        public ExchangeContentKind SelectedContentKind =>
            selectedContentKind;
        public string SelectedContentId => selectedContentId;
        public bool HasSelectedTab => hasSelectedTab;
        public ExchangeTab SelectedTab => selectedTab;
        public ExchangeCatalogData CurrentCatalog => currentCatalog;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            Close(notify: false, force: true);
        }

        public void Configure(
            ExchangeScreenView configuredView,
            VirtualizedExchangeGridView configuredGrid,
            ExchangeConfirmView configuredConfirmView,
            Func<DefinitionDatabase, ExchangeSpriteResolver>
                configuredSpriteResolverFactory,
            Action<string> configuredShowFeedback)
        {
            Unsubscribe();
            view = configuredView
                ?? throw new ArgumentNullException(nameof(configuredView));
            grid = configuredGrid
                ?? throw new ArgumentNullException(nameof(configuredGrid));
            confirmView = configuredConfirmView
                ?? throw new ArgumentNullException(
                    nameof(configuredConfirmView));
            spriteResolverFactory = configuredSpriteResolverFactory
                ?? throw new ArgumentNullException(
                    nameof(configuredSpriteResolverFactory));
            showFeedback = configuredShowFeedback
                ?? throw new ArgumentNullException(
                    nameof(configuredShowFeedback));
            ResetScreen();
            ready = false;
            if (isActiveAndEnabled)
                Subscribe();
        }

        public bool Initialize(
            SaveService initializedSaveService,
            DefinitionDatabase initializedDatabase,
            IExchangeTransactionExecutor initializedTransactionService)
        {
            saveService = initializedSaveService
                ?? throw new ArgumentNullException(
                    nameof(initializedSaveService));
            database = initializedDatabase
                ?? throw new ArgumentNullException(
                    nameof(initializedDatabase));
            transactionService = initializedTransactionService
                ?? throw new ArgumentNullException(
                    nameof(initializedTransactionService));
            ResetScreen();
            retryBlocked = false;
            try
            {
                if (!database.IsInitialized)
                {
                    throw new InvalidOperationException(
                        "Definition database must be initialized.");
                }
                if (database.ExchangeCatalog == null)
                {
                    throw new InvalidOperationException(
                        "Exchange catalog is not registered.");
                }

                spriteResolver = spriteResolverFactory(database)
                    ?? throw new InvalidOperationException(
                        "Exchange sprite resolver could not be created.");
                ProfileSaveData snapshot =
                    saveService.GetCurrentProfileSnapshot();
                ExchangeCatalogBuilder.Build(
                    database,
                    database.ExchangeCatalog,
                    snapshot);
                SetReady(true);
                return true;
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[ExchangeScreenController] Exchange initialization "
                        + "failed.",
                    this);
                spriteResolver = null;
                SetReady(false);
                return false;
            }
        }

        public bool TryOpen()
        {
            if (!ready || retryBlocked || !IsConfigured || IsOpen)
                return false;

            return TryRefreshAndBind(ExchangeTab.Hero, true, true);
        }

        public void Close() => Close(notify: true, force: false);

        public bool SelectTab(ExchangeTab tab)
        {
            if (!IsOpen
                || IsConfirmOpen
                || transactionInProgress
                || retryBlocked)
                return false;
            if (hasSelectedTab && selectedTab == tab)
                return true;

            return TryRefreshAndBind(tab, true, false);
        }

        public bool RefreshCurrent(bool resetScroll = false)
        {
            if (!IsOpen || !hasSelectedTab || transactionInProgress)
                return false;

            return TryRefreshAndBind(selectedTab, resetScroll, false);
        }

        public bool TrySelect(
            ExchangeContentKind kind,
            string contentId)
        {
            if (!IsOpen
                || IsConfirmOpen
                || transactionInProgress
                || retryBlocked
                || string.IsNullOrWhiteSpace(contentId)
                || !KindMatchesSelectedTab(kind))
            {
                return false;
            }

            if (!TryRefreshAndBind(selectedTab, false, false))
                return false;

            ExchangeListItem item = FindItem(
                currentCatalog,
                kind,
                contentId);
            if (item == null
                || item.UnavailableReasons.HasFlag(
                    ExchangeUnavailableReason.MaximumAwakening))
            {
                return false;
            }

            ExchangeConfirmDisplayData display =
                item.CreateConfirmDisplayData();
            try
            {
                confirmView.Open(
                    display,
                    spriteResolver.ResolveContent(item),
                    spriteResolver.ResolveCurrency(item.CurrencyKind));
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                ShowFeedback(
                    "The selected Exchange item could not be displayed.");
                return false;
            }

            selectedContentKind = kind;
            selectedContentId = contentId;
            selectedDisplayData = display;
            SetConfirmOutsideInputEnabled(false);
            SelectionRequested?.Invoke(kind, contentId);
            return true;
        }

        public void CancelConfirm()
        {
            if (transactionInProgress || !IsConfirmOpen)
                return;

            ClearSelectionAndCloseConfirm();
            SetConfirmOutsideInputEnabled(true);
        }

        public bool TryExecuteSelected()
        {
            if (!IsOpen
                || !IsConfirmOpen
                || selectedDisplayData == null
                || !selectedDisplayData.CanConfirm
                || transactionInProgress
                || retryBlocked
                || transactionService == null)
            {
                return false;
            }

            transactionInProgress = true;
            confirmView.SetProcessing(true);
            SetConfirmOutsideInputEnabled(false);
            ExchangeTransactionOperationResult operation;
            try
            {
                operation = transactionService.Execute(
                    new ExchangeTransactionRequest(
                        selectedContentKind,
                        selectedContentId));
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                transactionInProgress = false;
                confirmView.SetProcessing(false);
                ShowFeedback(
                    "An unexpected Exchange error occurred. No automatic "
                        + "retry was attempted.");
                return false;
            }

            transactionInProgress = false;
            if (operation != null && operation.IsSuccess)
                return HandleSuccessfulTransaction();

            HandleFailedTransaction(operation);
            return false;
        }

        private bool HandleSuccessfulTransaction()
        {
            ClearSelectionAndCloseConfirm();
            bool refreshed = TryRefreshAndBind(
                selectedTab,
                resetScroll: false,
                openAfterBinding: false);
            if (refreshed)
            {
                SetConfirmOutsideInputEnabled(true);
                return true;
            }

            GameLogger.Error(
                "[ExchangeScreenController] The Exchange was saved, but "
                    + "the refreshed UI could not be displayed.",
                this);
            ShowFeedback(
                "The Exchange was saved, but the screen could not be "
                    + "refreshed. Do not retry automatically.");
            return false;
        }

        private void HandleFailedTransaction(
            ExchangeTransactionOperationResult operation)
        {
            if (operation?.Exception != null)
                GameLogger.Exception(operation.Exception, this);

            ExchangeTransactionStatus status = operation?.Status
                ?? ExchangeTransactionStatus.InvalidRequest;
            GameLogger.Error(
                "[ExchangeScreenController] Exchange transaction failed. "
                    + $"Status={status}, WasPersisted="
                    + $"{operation?.WasPersisted == true}. "
                    + (operation?.Message ?? string.Empty),
                this);

            if (status == ExchangeTransactionStatus.PersistedStateMismatch
                || operation?.WasPersisted == true)
            {
                ClearSelectionAndCloseConfirm();
                TryRefreshAndBind(
                    selectedTab,
                    resetScroll: false,
                    openAfterBinding: false);
                retryBlocked = true;
                SetReady(false);
                grid.SetInteractionEnabled(false);
                view.SetNavigationInteractable(
                    tabsInteractable: false,
                    exitInteractable: true);
                ShowFeedback(
                    "The saved Exchange result could not be verified. "
                        + "Additional exchanges are blocked; reopen the "
                        + "scene after checking the profile.");
                return;
            }

            if (!TryRefreshAndBind(
                    selectedTab,
                    resetScroll: false,
                    openAfterBinding: false))
            {
                ShowFeedback(GetFailureMessage(operation));
                return;
            }

            ExchangeListItem latest = FindItem(
                currentCatalog,
                selectedContentKind,
                selectedContentId);
            if (latest == null
                || latest.UnavailableReasons.HasFlag(
                    ExchangeUnavailableReason.MaximumAwakening))
            {
                ClearSelectionAndCloseConfirm();
                SetConfirmOutsideInputEnabled(true);
            }
            else
            {
                selectedDisplayData = latest.CreateConfirmDisplayData();
                confirmView.Open(
                    selectedDisplayData,
                    spriteResolver.ResolveContent(latest),
                    spriteResolver.ResolveCurrency(latest.CurrencyKind));
                SetConfirmOutsideInputEnabled(false);
            }

            ShowFeedback(GetFailureMessage(operation));
        }

        private bool TryRefreshAndBind(
            ExchangeTab tab,
            bool resetScroll,
            bool openAfterBinding)
        {
            try
            {
                ProfileSaveData snapshot =
                    saveService.GetCurrentProfileSnapshot();
                ExchangeCatalogData catalog = ExchangeCatalogBuilder.Build(
                    database,
                    database.ExchangeCatalog,
                    snapshot);
                view.BindCurrencies(
                    snapshot.Currencies.HeroTokens,
                    snapshot.Currencies.RelicTokens,
                    snapshot.Currencies.GachaCurrency);
                view.SetSelectedTab(tab);
                if (tab == ExchangeTab.Hero)
                    grid.BindCharacters(catalog, spriteResolver, resetScroll);
                else
                    grid.BindRelics(catalog, spriteResolver, resetScroll);

                currentCatalog = catalog;
                selectedTab = tab;
                hasSelectedTab = true;
                if (openAfterBinding)
                {
                    view.Open();
                    grid.SetInteractionEnabled(true);
                    Opened?.Invoke();
                }

                return true;
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[ExchangeScreenController] Exchange data could not "
                        + "be displayed.",
                    this);
                ResetScreen();
                SetReady(false);
                return false;
            }
        }

        private void Close(bool notify, bool force)
        {
            if (!force && (transactionInProgress || IsConfirmOpen))
                return;

            bool wasOpen = IsOpen;
            ResetScreen();
            if (notify && wasOpen)
                Closed?.Invoke();
        }

        private void ResetScreen()
        {
            currentCatalog = null;
            hasSelectedTab = false;
            transactionInProgress = false;
            retryBlocked = false;
            ClearSelectionAndCloseConfirm();
            if (grid != null && grid.IsConfigured)
            {
                grid.SetInteractionEnabled(true);
                grid.Clear();
            }
            if (view != null)
            {
                view.SetNavigationInteractable(false, false);
                view.Close();
                view.ClearCurrencies();
            }
        }

        private void ClearSelectionAndCloseConfirm()
        {
            selectedDisplayData = null;
            selectedContentId = string.Empty;
            confirmView?.Close();
        }

        private void SetConfirmOutsideInputEnabled(bool enabled)
        {
            grid.SetInteractionEnabled(enabled);
            view.SetNavigationInteractable(enabled, enabled);
        }

        private bool KindMatchesSelectedTab(ExchangeContentKind kind)
        {
            return hasSelectedTab
                && ((selectedTab == ExchangeTab.Hero
                        && kind == ExchangeContentKind.Character)
                    || (selectedTab == ExchangeTab.Artifact
                        && kind == ExchangeContentKind.Relic));
        }

        private static ExchangeListItem FindItem(
            ExchangeCatalogData catalog,
            ExchangeContentKind kind,
            string contentId)
        {
            if (catalog == null)
                return null;

            if (kind == ExchangeContentKind.Character)
            {
                for (int index = 0;
                    index < catalog.Characters.Count;
                    index++)
                {
                    CharacterExchangeListItem item =
                        catalog.Characters[index];
                    if (string.Equals(
                            item.CharacterId,
                            contentId,
                            StringComparison.Ordinal))
                    {
                        return item;
                    }
                }

                return null;
            }

            for (int index = 0; index < catalog.Relics.Count; index++)
            {
                RelicExchangeListItem item = catalog.Relics[index];
                if (string.Equals(
                        item.RelicId,
                        contentId,
                        StringComparison.Ordinal))
                {
                    return item;
                }
            }

            return null;
        }

        private static string GetFailureMessage(
            ExchangeTransactionOperationResult operation)
        {
            if (operation == null)
                return "The Exchange request did not return a result.";
            if (!string.IsNullOrWhiteSpace(operation.Message))
                return operation.Message;

            return "The Exchange could not be completed ("
                + operation.Status + ").";
        }

        private void ShowFeedback(string message)
        {
            try
            {
                showFeedback(message);
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[ExchangeScreenController] Player feedback could "
                        + "not be displayed: " + message,
                    this);
            }
        }

        private void SetReady(bool value)
        {
            if (ready == value)
                return;

            ready = value;
            AvailabilityChanged?.Invoke();
        }

        private void Subscribe()
        {
            if (subscribed
                || view == null
                || grid == null
                || confirmView == null)
                return;

            view.CloseRequested += HandleCloseRequested;
            view.HeroTabRequested += HandleHeroTabRequested;
            view.ArtifactTabRequested += HandleArtifactTabRequested;
            grid.SelectionRequested += HandleSelectionRequested;
            confirmView.ConfirmRequested += HandleConfirmRequested;
            confirmView.CancelRequested += HandleCancelRequested;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
                return;

            view.CloseRequested -= HandleCloseRequested;
            view.HeroTabRequested -= HandleHeroTabRequested;
            view.ArtifactTabRequested -= HandleArtifactTabRequested;
            grid.SelectionRequested -= HandleSelectionRequested;
            confirmView.ConfirmRequested -= HandleConfirmRequested;
            confirmView.CancelRequested -= HandleCancelRequested;
            subscribed = false;
        }

        private void HandleCloseRequested() => Close();
        private void HandleHeroTabRequested() =>
            SelectTab(ExchangeTab.Hero);
        private void HandleArtifactTabRequested() =>
            SelectTab(ExchangeTab.Artifact);
        private void HandleConfirmRequested() => TryExecuteSelected();
        private void HandleCancelRequested() => CancelConfirm();

        private void HandleSelectionRequested(
            ExchangeContentKind kind,
            string contentId)
        {
            TrySelect(kind, contentId);
        }
    }
}
