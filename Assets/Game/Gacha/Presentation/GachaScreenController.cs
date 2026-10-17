using System;
using System.Collections.Generic;
using UnityEngine;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Logging;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Exchange.Persistence;
using ValorChronicle.Exchange.Presentation;
using ValorChronicle.Gacha.Persistence;
using ValorChronicle.Localization;
using ValorChronicle.Relics.Acquisition;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Gacha.Presentation
{
    public sealed class GachaBannerDisplayData
    {
        public GachaBannerDisplayData(
            string gachaId,
            int pityCount,
            int pityMaximum,
            long singlePullCost,
            long tenPullCost,
            long availableCurrency)
        {
            GachaId = gachaId;
            PityCount = pityCount;
            PityMaximum = pityMaximum;
            SinglePullCost = singlePullCost;
            TenPullCost = tenPullCost;
            CanAffordSingle = availableCurrency >= singlePullCost;
            CanAffordTen = availableCurrency >= tenPullCost;
        }

        public string GachaId { get; }
        public int PityCount { get; }
        public int PityMaximum { get; }
        public long SinglePullCost { get; }
        public long TenPullCost { get; }
        public bool CanAffordSingle { get; }
        public bool CanAffordTen { get; }
    }

    public sealed class GachaScreenDisplayData
    {
        public GachaScreenDisplayData(
            long heroTokens,
            long relicTokens,
            long gachaCurrency,
            GachaBannerDisplayData characterBanner,
            GachaBannerDisplayData relicBanner)
        {
            HeroTokens = heroTokens;
            RelicTokens = relicTokens;
            GachaCurrency = gachaCurrency;
            CharacterBanner = characterBanner;
            RelicBanner = relicBanner;
        }

        public long HeroTokens { get; }
        public long RelicTokens { get; }
        public long GachaCurrency { get; }
        public GachaBannerDisplayData CharacterBanner { get; }
        public GachaBannerDisplayData RelicBanner { get; }
    }

    public sealed class GachaScreenController : MonoBehaviour
    {
        public const string CharacterGachaId =
            "gacha_character_standard";
        public const string RelicGachaId = "gacha_relic_standard";

        private GachaScreenView view;
        private GachaInfoPanelPresenter infoPresenter;
        private GachaResultPresenter resultPresenter;
        private GachaPresentationController presentationController;
        private ExchangeScreenController exchangeController;
        private Func<SaveService, DefinitionDatabase,
            ExchangeTransactionService> exchangeTransactionServiceFactory;
        private SaveService saveService;
        private DefinitionDatabase definitionDatabase;
        private GachaTransactionService transactionService;
        private GachaScreenDisplayData currentDisplayData;
        private bool resultResourcesReady;
        private bool individualPresentationReady;
        private bool transactionInProgress;
        private bool awaitingResultCompletion;
        private bool activeUsesIndividualPresentation;
        private GachaTransactionResult activeTransactionResult;
        private bool profileChangeSubscribed;
        private bool subscribed;

        public bool IsConfigured =>
            view != null
            && view.IsConfigured
            && (infoPresenter == null || infoPresenter.IsConfigured)
            && (resultPresenter == null || resultPresenter.IsConfigured)
            && (presentationController == null
                || presentationController.IsConfigured)
            && (exchangeController == null
                || (exchangeController.IsConfigured
                    && exchangeTransactionServiceFactory != null));
        public bool IsTransactionInProgress => transactionInProgress;
        public bool IsIndividualPresentationReady =>
            individualPresentationReady;

        private void OnEnable()
        {
            Subscribe();
            SubscribeProfileChanges();
            if (saveService != null && definitionDatabase != null)
            {
                RefreshFromAuthoritativeProfile();
            }
        }

        private void OnDisable()
        {
            UnsubscribeProfileChanges();
            Unsubscribe();
            presentationController?.Cancel();
            ClearResultSession();
        }

        private void Start()
        {
            if (saveService != null && definitionDatabase != null)
            {
                RefreshFromAuthoritativeProfile();
                return;
            }

            GameBootstrapper bootstrapper = GameBootstrapper.Instance;
            if (bootstrapper == null
                || bootstrapper.InitializationResult == null
                || !bootstrapper.InitializationResult.IsSuccess
                || bootstrapper.SaveService == null
                || bootstrapper.DefinitionDatabase == null
                || bootstrapper.LocalizationService == null
                || bootstrapper.RandomSource == null)
            {
                Fail(
                    "GameBootstrapper services are not initialized.");
                return;
            }

            Initialize(
                bootstrapper.SaveService,
                bootstrapper.DefinitionDatabase,
                bootstrapper.LocalizationService,
                bootstrapper.RandomSource);
        }

        public void Configure(
            GachaScreenView configuredView,
            GachaInfoPanelPresenter configuredInfoPresenter = null,
            GachaResultPresenter configuredResultPresenter = null,
            GachaPresentationController configuredPresentationController =
                null,
            ExchangeScreenController configuredExchangeController = null,
            Func<SaveService, DefinitionDatabase,
                ExchangeTransactionService>
                configuredExchangeTransactionServiceFactory = null)
        {
            Unsubscribe();
            presentationController?.Cancel();
            ClearResultSession();
            view = configuredView
                ?? throw new ArgumentNullException(nameof(configuredView));
            infoPresenter = configuredInfoPresenter;
            resultPresenter = configuredResultPresenter;
            presentationController = configuredPresentationController;
            exchangeController = configuredExchangeController;
            exchangeTransactionServiceFactory =
                configuredExchangeTransactionServiceFactory;
            view.SetUnavailable();
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        public void Initialize(
            SaveService initializedSaveService,
            DefinitionDatabase initializedDefinitionDatabase)
        {
            ResetResultPresentationSession();
            SetSaveService(initializedSaveService);
            definitionDatabase = initializedDefinitionDatabase
                ?? throw new ArgumentNullException(
                    nameof(initializedDefinitionDatabase));
            resultPresenter?.Initialize(definitionDatabase);
            presentationController?.Initialize(definitionDatabase);
            transactionService = null;
            resultResourcesReady = false;
            individualPresentationReady = false;
            InitializeExchangeController();
            RefreshFromAuthoritativeProfile();
        }

        public void Initialize(
            SaveService initializedSaveService,
            DefinitionDatabase initializedDefinitionDatabase,
            LocalizationService initializedLocalization)
        {
            ResetResultPresentationSession();
            SetSaveService(initializedSaveService);
            definitionDatabase = initializedDefinitionDatabase
                ?? throw new ArgumentNullException(
                    nameof(initializedDefinitionDatabase));
            infoPresenter?.Initialize(
                definitionDatabase,
                initializedLocalization
                    ?? throw new ArgumentNullException(
                        nameof(initializedLocalization)));
            resultPresenter?.Initialize(definitionDatabase);
            presentationController?.Initialize(definitionDatabase);
            transactionService = null;
            resultResourcesReady = ValidateResultResources();
            individualPresentationReady = resultResourcesReady
                && ValidateIndividualPresentationResources();
            InitializeExchangeController();
            RefreshFromAuthoritativeProfile();
        }

        public void Initialize(
            SaveService initializedSaveService,
            DefinitionDatabase initializedDefinitionDatabase,
            LocalizationService initializedLocalization,
            IRandomSource randomSource)
        {
            if (randomSource == null)
            {
                throw new ArgumentNullException(nameof(randomSource));
            }

            var service = new GachaTransactionService(
                initializedSaveService,
                initializedDefinitionDatabase,
                randomSource,
                new GuidRelicInstanceIdProvider());
            Initialize(
                initializedSaveService,
                initializedDefinitionDatabase,
                initializedLocalization,
                service);
        }

        public void Initialize(
            SaveService initializedSaveService,
            DefinitionDatabase initializedDefinitionDatabase,
            LocalizationService initializedLocalization,
            GachaTransactionService initializedTransactionService)
        {
            ResetResultPresentationSession();
            SetSaveService(initializedSaveService);
            definitionDatabase = initializedDefinitionDatabase
                ?? throw new ArgumentNullException(
                    nameof(initializedDefinitionDatabase));
            transactionService = initializedTransactionService
                ?? throw new ArgumentNullException(
                    nameof(initializedTransactionService));
            infoPresenter?.Initialize(
                definitionDatabase,
                initializedLocalization
                    ?? throw new ArgumentNullException(
                        nameof(initializedLocalization)));
            resultPresenter?.Initialize(definitionDatabase);
            presentationController?.Initialize(definitionDatabase);
            resultResourcesReady = ValidateResultResources();
            individualPresentationReady = resultResourcesReady
                && ValidateIndividualPresentationResources();
            InitializeExchangeController();
            if (!resultResourcesReady)
            {
                ShowFailure("gacha.error.initialization_failed");
            }

            RefreshFromAuthoritativeProfile();
        }

        public bool ShowResult(GachaTransactionResult result)
        {
            if (resultPresenter == null)
            {
                GameLogger.Error(
                    "[GachaScreenController] Result presenter is missing.",
                    this);
                return false;
            }

            return resultPresenter.ShowResult(result);
        }

        public bool RefreshFromAuthoritativeProfile()
        {
            if (!IsConfigured)
            {
                Fail("The Gacha screen view is not configured.");
                return false;
            }

            if (saveService == null || definitionDatabase == null)
            {
                Fail("Required Gacha screen services are unavailable.");
                return false;
            }

            ProfileSaveData snapshot;
            try
            {
                snapshot = saveService.GetCurrentProfileSnapshot();
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                Fail("The current profile snapshot could not be read.");
                return false;
            }

            if (!TryCreateDisplayData(
                    definitionDatabase,
                    snapshot,
                    out GachaScreenDisplayData data,
                    out string error))
            {
                Fail(error);
                return false;
            }

            view.Bind(data);
            currentDisplayData = data;
            ApplyButtonState();
            return true;
        }

        public bool TryExecutePull(string gachaId, int pullCount)
        {
            if (!CanStartTransaction(gachaId, pullCount))
            {
                ApplyButtonState();
                return false;
            }

            transactionInProgress = true;
            ApplyButtonState();
            GachaTransactionOperationResult operation;
            try
            {
                operation = pullCount == 1
                    ? transactionService.PullSingle(gachaId)
                    : transactionService.PullTen(gachaId);
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                transactionInProgress = false;
                RefreshFromAuthoritativeProfile();
                ShowFailure("gacha.error.unexpected_failure");
                return false;
            }

            bool refreshed = RefreshFromAuthoritativeProfile();
            if (operation == null || !operation.IsSuccess)
            {
                HandleFailedOperation(operation);
                return false;
            }

            if (!refreshed)
            {
                transactionInProgress = false;
                ShowFailure("gacha.error.result_display_failed");
                GameLogger.Error(
                    "[GachaScreenController] The Gacha transaction was "
                        + "saved, but the latest profile could not be "
                        + "displayed.",
                    this);
                return false;
            }

            if (!operation.WasPersisted
                || operation.TransactionResult == null)
            {
                GameLogger.Error(
                    "[GachaScreenController] A successful transaction "
                        + "did not contain a persisted result.",
                    this);
                transactionInProgress = false;
                ShowFailure("gacha.error.unexpected_failure");
                return false;
            }

            if (!StartResultPresentation(operation.TransactionResult))
            {
                return false;
            }

            ApplyButtonState();
            return true;
        }

        private bool CanStartTransaction(string gachaId, int pullCount)
        {
            if (transactionService == null
                || !resultResourcesReady
                || transactionInProgress
                || currentDisplayData == null
                || infoPresenter?.IsOpen == true
                || resultPresenter?.IsOpen == true
                || presentationController?.IsPresenting == true
                || exchangeController?.IsOpen == true
                || (pullCount != 1
                    && pullCount != GachaCalculator.TenPullCount))
            {
                return false;
            }

            GachaBannerDisplayData banner;
            if (string.Equals(
                gachaId,
                CharacterGachaId,
                StringComparison.Ordinal))
            {
                banner = currentDisplayData.CharacterBanner;
            }
            else if (string.Equals(
                gachaId,
                RelicGachaId,
                StringComparison.Ordinal))
            {
                banner = currentDisplayData.RelicBanner;
            }
            else
            {
                return false;
            }

            return pullCount == 1
                ? banner.CanAffordSingle
                : banner.CanAffordTen;
        }

        private void HandleFailedOperation(
            GachaTransactionOperationResult operation)
        {
            transactionInProgress = false;
            if (operation == null)
            {
                GameLogger.Error(
                    "[GachaScreenController] The Gacha transaction "
                        + "returned no operation result.",
                    this);
                ShowFailure("gacha.error.unexpected_failure");
                return;
            }

            if (operation.Exception != null)
            {
                GameLogger.Exception(operation.Exception, this);
            }

            GameLogger.Error(
                $"[GachaScreenController] Gacha transaction failed. "
                    + $"Status={operation.Status}, "
                    + $"WasPersisted={operation.WasPersisted}. "
                    + operation.Message,
                this);

            if (operation.WasPersisted
                || operation.Status
                    == GachaTransactionStatus.PersistedResultMismatch)
            {
                ShowFailure("gacha.error.persisted_result_mismatch");
            }
            else if (operation.Status
                == GachaTransactionStatus.InsufficientGachaCurrency)
            {
                ShowFailure("gacha.error.insufficient_currency");
            }
            else
            {
                ShowFailure("gacha.error.transaction_failed");
            }

            ApplyButtonState();
        }

        private bool ValidateResultResources()
        {
            string error = string.Empty;
            if (definitionDatabase == null
                || resultPresenter == null
                || !definitionDatabase.TryGetGacha(
                    CharacterGachaId,
                    out GachaDefinition characterDefinition)
                || !definitionDatabase.TryGetGacha(
                    RelicGachaId,
                    out GachaDefinition relicDefinition)
                || !TryValidateTransactionDefinition(
                    characterDefinition,
                    GachaKind.Character,
                    out error)
                || !TryValidateTransactionDefinition(
                    relicDefinition,
                    GachaKind.Relic,
                    out error)
                || !resultPresenter.TryValidateResources(
                    new[] { characterDefinition, relicDefinition },
                    out error))
            {
                GameLogger.Error(
                    "[GachaScreenController] Result resource validation "
                        + "failed. " + (error ?? string.Empty),
                    this);
                return false;
            }

            return true;
        }

        private bool ValidateIndividualPresentationResources()
        {
            string error = string.Empty;
            if (definitionDatabase == null
                || presentationController == null
                || !definitionDatabase.TryGetGacha(
                    CharacterGachaId,
                    out GachaDefinition characterDefinition)
                || !definitionDatabase.TryGetGacha(
                    RelicGachaId,
                    out GachaDefinition relicDefinition)
                || !presentationController.TryValidateResources(
                    new[] { characterDefinition, relicDefinition },
                    out error))
            {
                GameLogger.Warning(
                    "[GachaScreenController] Individual presentation "
                        + "resource validation failed. Persisted results "
                        + "will use the final summary directly. "
                        + (error ?? string.Empty),
                    this);
                return false;
            }

            return true;
        }

        private static bool TryValidateTransactionDefinition(
            GachaDefinition definition,
            GachaKind expectedKind,
            out string error)
        {
            try
            {
                if (definition == null
                    || definition.GachaKind != expectedKind
                    || definition.PityPullCount <= 0
                    || definition.SinglePullCost <= 0L
                    || definition.SinglePullCost
                        > long.MaxValue / GachaCalculator.TenPullCount
                    || definition.TenPullCost
                        != definition.SinglePullCost
                            * GachaCalculator.TenPullCount)
                {
                    error = "Gacha definition cost, kind, or pity data is "
                        + "invalid.";
                    return false;
                }

                GachaRewardSelector.ValidateOrThrow(definition);
                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private void ApplyButtonState()
        {
            if (view == null || !view.IsConfigured)
            {
                return;
            }

            bool available = transactionService != null
                && resultResourcesReady
                && !transactionInProgress
                && currentDisplayData != null
                && infoPresenter?.IsOpen != true
                && resultPresenter?.IsOpen != true
                && presentationController?.IsPresenting != true
                && exchangeController?.IsOpen != true;
            infoPresenter?.SetEntryButtonsInteractable(available);
            view.SetPullButtonsInteractable(
                available
                    && currentDisplayData.CharacterBanner.CanAffordSingle,
                available
                    && currentDisplayData.CharacterBanner.CanAffordTen,
                available
                    && currentDisplayData.RelicBanner.CanAffordSingle,
                available
                    && currentDisplayData.RelicBanner.CanAffordTen,
                available
                    && exchangeController != null
                    && exchangeController.IsReady);
        }

        private void ShowFailure(string localizationKey)
        {
            if (infoPresenter == null
                || !infoPresenter.ShowLocalizedMessage(localizationKey))
            {
                GameLogger.Error(
                    "[GachaScreenController] Player-facing Gacha failure "
                        + "message could not be displayed: "
                        + localizationKey,
                    this);
            }

            ApplyButtonState();
        }

        private void Subscribe()
        {
            if (subscribed || view == null)
            {
                return;
            }

            view.CharacterSingleButton.onClick.AddListener(
                HandleCharacterSingle);
            view.CharacterTenButton.onClick.AddListener(
                HandleCharacterTen);
            view.RelicSingleButton.onClick.AddListener(HandleRelicSingle);
            view.RelicTenButton.onClick.AddListener(HandleRelicTen);
            if (infoPresenter != null)
            {
                infoPresenter.Opened += HandleOverlayOpened;
                infoPresenter.Closed += HandleInfoClosed;
            }

            if (resultPresenter != null)
            {
                resultPresenter.Opened += HandleOverlayOpened;
                resultPresenter.Closed += HandleResultClosed;
            }

            if (presentationController != null)
            {
                presentationController.Finished +=
                    HandlePresentationFinished;
                presentationController.Failed += HandlePresentationFailed;
                presentationController.Cancelled +=
                    HandlePresentationCancelled;
            }

            if (exchangeController != null)
            {
                exchangeController.View.OpenRequested +=
                    HandleExchangeOpenRequested;
                exchangeController.Opened += HandleExchangeOpened;
                exchangeController.Closed += HandleExchangeClosed;
                exchangeController.AvailabilityChanged +=
                    HandleExchangeAvailabilityChanged;
            }

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            view.CharacterSingleButton.onClick.RemoveListener(
                HandleCharacterSingle);
            view.CharacterTenButton.onClick.RemoveListener(
                HandleCharacterTen);
            view.RelicSingleButton.onClick.RemoveListener(
                HandleRelicSingle);
            view.RelicTenButton.onClick.RemoveListener(HandleRelicTen);
            if (infoPresenter != null)
            {
                infoPresenter.Opened -= HandleOverlayOpened;
                infoPresenter.Closed -= HandleInfoClosed;
            }

            if (resultPresenter != null)
            {
                resultPresenter.Opened -= HandleOverlayOpened;
                resultPresenter.Closed -= HandleResultClosed;
            }

            if (presentationController != null)
            {
                presentationController.Finished -=
                    HandlePresentationFinished;
                presentationController.Failed -= HandlePresentationFailed;
                presentationController.Cancelled -=
                    HandlePresentationCancelled;
            }

            if (exchangeController != null)
            {
                exchangeController.View.OpenRequested -=
                    HandleExchangeOpenRequested;
                exchangeController.Opened -= HandleExchangeOpened;
                exchangeController.Closed -= HandleExchangeClosed;
                exchangeController.AvailabilityChanged -=
                    HandleExchangeAvailabilityChanged;
            }

            subscribed = false;
        }

        private void SetSaveService(SaveService initializedSaveService)
        {
            UnsubscribeProfileChanges();
            saveService = initializedSaveService
                ?? throw new ArgumentNullException(
                    nameof(initializedSaveService));
            SubscribeProfileChanges();
        }

        private void SubscribeProfileChanges()
        {
            if (profileChangeSubscribed
                || saveService == null
                || !isActiveAndEnabled)
            {
                return;
            }

            saveService.CurrentProfileChanged +=
                HandleCurrentProfileChanged;
            profileChangeSubscribed = true;
        }

        private void UnsubscribeProfileChanges()
        {
            if (!profileChangeSubscribed || saveService == null)
                return;

            saveService.CurrentProfileChanged -=
                HandleCurrentProfileChanged;
            profileChangeSubscribed = false;
        }

        private void HandleCurrentProfileChanged()
        {
            if (!transactionInProgress
                && saveService != null
                && definitionDatabase != null)
            {
                RefreshFromAuthoritativeProfile();
            }
        }

        private void HandleCharacterSingle() =>
            TryExecutePull(CharacterGachaId, 1);

        private void HandleCharacterTen() =>
            TryExecutePull(CharacterGachaId, GachaCalculator.TenPullCount);

        private void HandleRelicSingle() =>
            TryExecutePull(RelicGachaId, 1);

        private void HandleRelicTen() =>
            TryExecutePull(RelicGachaId, GachaCalculator.TenPullCount);

        private void HandleOverlayOpened()
        {
            ApplyButtonState();
        }

        private void HandleExchangeOpened()
        {
            ApplyButtonState();
        }

        private void HandleExchangeOpenRequested()
        {
            if (CanOpenExchange())
                exchangeController.TryOpen();
            ApplyButtonState();
        }

        private bool CanOpenExchange()
        {
            return exchangeController != null
                && exchangeController.IsReady
                && !exchangeController.IsOpen
                && transactionService != null
                && resultResourcesReady
                && !transactionInProgress
                && currentDisplayData != null
                && infoPresenter?.IsOpen != true
                && resultPresenter?.IsOpen != true
                && presentationController?.IsPresenting != true;
        }

        private void InitializeExchangeController()
        {
            if (exchangeController == null)
                return;

            if (saveService == null
                || definitionDatabase == null
                || exchangeTransactionServiceFactory == null)
            {
                GameLogger.Error(
                    "[GachaScreenController] Exchange dependencies are "
                        + "not configured.",
                    this);
                return;
            }

            try
            {
                ExchangeTransactionService exchangeTransactionService =
                    exchangeTransactionServiceFactory(
                        saveService,
                        definitionDatabase);
                exchangeController.Initialize(
                    saveService,
                    definitionDatabase,
                    exchangeTransactionService);
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[GachaScreenController] Exchange initialization "
                        + "failed.",
                    this);
            }
        }

        private void HandleExchangeClosed()
        {
            if (saveService != null && definitionDatabase != null)
                RefreshFromAuthoritativeProfile();
            else
                ApplyButtonState();
        }

        private void HandleExchangeAvailabilityChanged()
        {
            ApplyButtonState();
        }

        private void HandleInfoClosed()
        {
            if (saveService != null && definitionDatabase != null)
            {
                RefreshFromAuthoritativeProfile();
            }
            else
            {
                ApplyButtonState();
            }
        }

        private void HandleResultClosed()
        {
            if (!awaitingResultCompletion
                || activeUsesIndividualPresentation)
            {
                return;
            }

            CompleteResultSession();
        }

        private void HandlePresentationFinished(
            GachaTransactionResult transaction)
        {
            if (!awaitingResultCompletion
                || !activeUsesIndividualPresentation
                || !ReferenceEquals(transaction, activeTransactionResult))
            {
                return;
            }

            CompleteResultSession();
        }

        private void HandlePresentationFailed(
            GachaTransactionResult transaction,
            string message)
        {
            if (!awaitingResultCompletion
                || !activeUsesIndividualPresentation
                || !ReferenceEquals(transaction, activeTransactionResult))
            {
                return;
            }

            GameLogger.Error(
                "[GachaScreenController] Persisted Gacha result "
                    + "presentation failed. " + (message ?? string.Empty),
                this);
            resultResourcesReady = false;
            ClearResultSession();
            if (saveService != null && definitionDatabase != null)
            {
                RefreshFromAuthoritativeProfile();
            }
            else
            {
                ApplyButtonState();
            }

            ShowFailure("gacha.error.result_display_failed");
        }

        private void HandlePresentationCancelled(
            GachaTransactionResult transaction,
            string message)
        {
            if (!awaitingResultCompletion
                || !activeUsesIndividualPresentation
                || !ReferenceEquals(transaction, activeTransactionResult))
            {
                return;
            }

            GameLogger.Warning(
                "[GachaScreenController] Persisted Gacha result "
                    + "presentation was cancelled. "
                    + (message ?? string.Empty),
                this);
            ClearResultSession();
            if (isActiveAndEnabled
                && gameObject.activeInHierarchy
                && saveService != null
                && definitionDatabase != null)
            {
                RefreshFromAuthoritativeProfile();
                ShowFailure("gacha.error.result_display_failed");
            }
        }

        private bool StartResultPresentation(
            GachaTransactionResult transaction)
        {
            activeTransactionResult = transaction;
            awaitingResultCompletion = true;
            activeUsesIndividualPresentation =
                individualPresentationReady
                && presentationController != null;

            bool shown = activeUsesIndividualPresentation
                ? presentationController.Play(transaction)
                : ShowResult(transaction);
            if (shown)
            {
                ApplyButtonState();
                return true;
            }

            if (awaitingResultCompletion)
            {
                GameLogger.Error(
                    "[GachaScreenController] The Gacha transaction was "
                        + "saved, but its result UI could not be shown.",
                    this);
                resultResourcesReady = false;
                ClearResultSession();
                RefreshFromAuthoritativeProfile();
                ShowFailure("gacha.error.result_display_failed");
            }

            return false;
        }

        private void CompleteResultSession()
        {
            if (!awaitingResultCompletion)
            {
                return;
            }

            ClearResultSession();
            if (saveService != null && definitionDatabase != null)
            {
                RefreshFromAuthoritativeProfile();
            }
            else
            {
                ApplyButtonState();
            }
        }

        private void ClearResultSession()
        {
            awaitingResultCompletion = false;
            activeUsesIndividualPresentation = false;
            activeTransactionResult = null;
            transactionInProgress = false;
        }

        private void ResetResultPresentationSession()
        {
            presentationController?.Cancel();
            resultPresenter?.Close(notify: false);
            ClearResultSession();
        }

        public static bool TryCreateDisplayData(
            DefinitionDatabase database,
            ProfileSaveData profile,
            out GachaScreenDisplayData data,
            out string error)
        {
            data = null;
            error = string.Empty;
            if (database == null)
            {
                error = "DefinitionDatabase is missing.";
                return false;
            }

            if (!database.IsInitialized)
            {
                error = "DefinitionDatabase is not initialized.";
                return false;
            }

            if (profile == null || profile.Currencies == null)
            {
                error = "Profile currencies are missing.";
                return false;
            }

            CurrencySaveData currencies = profile.Currencies;
            if (currencies.GachaCurrency < 0
                || currencies.HeroTokens < 0
                || currencies.RelicTokens < 0)
            {
                error = "Profile currencies contain a negative value.";
                return false;
            }

            if (!TryGetBanner(
                    database,
                    profile.GachaStates,
                    CharacterGachaId,
                    GachaKind.Character,
                    currencies.GachaCurrency,
                    out GachaBannerDisplayData characterBanner,
                    out error)
                || !TryGetBanner(
                    database,
                    profile.GachaStates,
                    RelicGachaId,
                    GachaKind.Relic,
                    currencies.GachaCurrency,
                    out GachaBannerDisplayData relicBanner,
                    out error))
            {
                return false;
            }

            data = new GachaScreenDisplayData(
                currencies.HeroTokens,
                currencies.RelicTokens,
                currencies.GachaCurrency,
                characterBanner,
                relicBanner);
            return true;
        }

        private static bool TryGetBanner(
            DefinitionDatabase database,
            IReadOnlyList<GachaStateSaveData> states,
            string gachaId,
            GachaKind expectedKind,
            long availableCurrency,
            out GachaBannerDisplayData banner,
            out string error)
        {
            banner = null;
            error = string.Empty;
            if (!database.TryGetGacha(
                    gachaId,
                    out GachaDefinition definition))
            {
                error = $"GachaDefinition '{gachaId}' was not found.";
                return false;
            }

            if (definition.GachaKind != expectedKind
                || definition.PityPullCount <= 0
                || definition.SinglePullCost <= 0
                || definition.TenPullCost <= 0)
            {
                error = $"GachaDefinition '{gachaId}' is invalid.";
                return false;
            }

            int pityCount = 0;
            GachaStateSaveData match = null;
            if (states != null)
            {
                for (int index = 0; index < states.Count; index++)
                {
                    GachaStateSaveData candidate = states[index];
                    if (candidate == null
                        || string.IsNullOrEmpty(candidate.GachaId))
                    {
                        error = "Gacha state data is invalid.";
                        return false;
                    }

                    if (!string.Equals(
                            candidate.GachaId,
                            gachaId,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (match != null)
                    {
                        error = $"Gacha state '{gachaId}' is duplicated.";
                        return false;
                    }

                    match = candidate;
                }
            }

            if (match != null)
            {
                if (match.PityCount < 0
                    || match.PityCount >= definition.PityPullCount
                    || match.TotalPullCount < 0
                    || match.PityCount > match.TotalPullCount)
                {
                    error = $"Gacha state '{gachaId}' is invalid.";
                    return false;
                }

                pityCount = match.PityCount;
            }

            banner = new GachaBannerDisplayData(
                gachaId,
                pityCount,
                definition.PityPullCount,
                definition.SinglePullCost,
                definition.TenPullCost,
                availableCurrency);
            return true;
        }

        private void Fail(string message)
        {
            currentDisplayData = null;
            view?.SetUnavailable();
            GameLogger.Error(
                "[GachaScreenController] " + message,
                this);
        }
    }
}
