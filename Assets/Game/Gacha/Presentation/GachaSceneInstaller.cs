using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ValorChronicle.Core.Logging;
using ValorChronicle.Data.Database;
using ValorChronicle.Exchange.Persistence;
using ValorChronicle.Exchange.Presentation;
using ValorChronicle.Party.Presentation;
using ValorChronicle.Relics.Acquisition;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Gacha.Presentation
{
    public sealed class GachaSceneInstaller : MonoBehaviour
    {
        [Header("Currency Display")]
        [SerializeField]
        private TMP_Text heroTokensText;
        [SerializeField]
        private TMP_Text relicTokensText;
        [SerializeField]
        private TMP_Text gachaCurrencyText;
        [SerializeField]
        private CharacterPresentationCatalog characterPresentationCatalog;
        [SerializeField]
        private Sprite heroTokenSprite;
        [SerializeField]
        private Sprite relicTokenSprite;
        [SerializeField]
        private Sprite battleRecordsSprite;
        [SerializeField]
        private Sprite[] awakeningSprites = new Sprite[6];
        [Header("Individual Result Presentation")]
        [SerializeField, Min(0f)]
        private float openingDuration = 0.7f;
        [SerializeField, Min(0f)]
        private float resultWaitDuration = 1f;
        [SerializeField, Min(0f)]
        private float transitionDuration = 0.15f;
        [SerializeField, Min(0f)]
        private float curtainClosedDelay = 0.3f;
        [SerializeField, Min(0f)]
        private float skipHideDelay = 3f;

        public GachaScreenView View { get; private set; }
        public GachaScreenController Controller { get; private set; }
        public GachaInfoPanelPresenter InfoPresenter { get; private set; }
        public GachaResultPresenter ResultPresenter { get; private set; }
        public GachaPresentationController PresentationController
        {
            get;
            private set;
        }
        public VirtualizedExchangeGridView ExchangeGridView
        {
            get;
            private set;
        }
        public ExchangeScreenView ExchangeView { get; private set; }
        public ExchangeConfirmView ExchangeConfirmView
        {
            get;
            private set;
        }
        public ExchangeScreenController ExchangeController
        {
            get;
            private set;
        }
        public ExchangeTransactionService ExchangeTransactionService
        {
            get;
            private set;
        }
        private SaveService exchangeTransactionSaveService;
        private DefinitionDatabase exchangeTransactionDatabase;
        public bool IsConfigured =>
            View != null
            && View.IsConfigured
            && InfoPresenter != null
            && InfoPresenter.IsConfigured
            && ResultPresenter != null
            && ResultPresenter.IsConfigured
            && PresentationController != null
            && PresentationController.IsConfigured
            && ExchangeGridView != null
            && ExchangeGridView.IsConfigured
            && ExchangeView != null
            && ExchangeView.IsConfigured
            && ExchangeConfirmView != null
            && ExchangeConfirmView.IsConfigured
            && ExchangeController != null
            && ExchangeController.IsConfigured
            && Controller != null
            && Controller.IsConfigured;

        private void Awake()
        {
            try
            {
                Install();
            }
            catch (Exception exception)
            {
                DisableKnownActions();
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[GachaSceneInstaller] Required UI could not be "
                        + "connected.",
                    this);
            }
        }

        public void Install()
        {
            Transform characterSingle = RequireTransform(
                transform,
                "HeroesPickup/1RunButton");
            Transform characterTen = RequireTransform(
                transform,
                "HeroesPickup/10RunButton");
            Transform relicSingle = RequireTransform(
                transform,
                "ArtifactPickup/1RunButton");
            Transform relicTen = RequireTransform(
                transform,
                "ArtifactPickup/10RunButton");

            View = GetOrAdd<GachaScreenView>(gameObject);
            View.Configure(
                RequireReference(heroTokensText, nameof(heroTokensText)),
                RequireReference(relicTokensText, nameof(relicTokensText)),
                RequireReference(
                    gachaCurrencyText,
                    nameof(gachaCurrencyText)),
                RequireComponent<TMP_Text>(RequireTransform(
                    transform,
                    "HeroesPickup/UpperBound/UpperBoundText")),
                RequireComponent<TMP_Text>(RequireTransform(
                    transform,
                    "ArtifactPickup/UpperBound/UpperBoundText")),
                RequireComponent<TMP_Text>(RequireTransform(
                    characterSingle,
                    "PriceText")),
                RequireComponent<TMP_Text>(RequireTransform(
                    characterTen,
                    "PriceText")),
                RequireComponent<TMP_Text>(RequireTransform(
                    relicSingle,
                    "PriceText")),
                RequireComponent<TMP_Text>(RequireTransform(
                    relicTen,
                    "PriceText")),
                RequireComponent<Button>(characterSingle),
                RequireComponent<Button>(characterTen),
                RequireComponent<Button>(relicSingle),
                RequireComponent<Button>(relicTen),
                RequireComponent<Button>(RequireTransform(
                    transform,
                    "SettingButton")),
                RequireComponent<Button>(RequireTransform(
                    transform,
                    "ExchangeButton")),
                RequireTransform(transform, "InfoPanel").gameObject,
                RequireTransform(
                    transform,
                    "GachaPresentation").gameObject);

            InfoPresenter = InstallInfoPanel();
            ResultPresenter = InstallResultPresentation();
            PresentationController = InstallIndividualPresentation(
                ResultPresenter);
            ExchangeGridView = ExchangeGridRuntimeInstaller.Install(
                RequireTransform(transform, "Exchange"));
            InstallExchangeScreen();
            Controller = GetOrAdd<GachaScreenController>(gameObject);
            Controller.Configure(
                View,
                InfoPresenter,
                ResultPresenter,
                PresentationController,
                ExchangeController,
                GetOrCreateExchangeTransactionService);
        }

        public ExchangeSpriteResolver CreateExchangeSpriteResolver(
            DefinitionDatabase database)
        {
            return new ExchangeSpriteResolver(
                RequireReference(
                    characterPresentationCatalog,
                    nameof(characterPresentationCatalog)),
                database,
                heroTokenSprite,
                relicTokenSprite);
        }

        private void InstallExchangeScreen()
        {
            RectTransform exchangeRoot = RequireTransform(
                transform,
                "Exchange");
            RequireComponent<Image>(RequireTransform(
                exchangeRoot,
                "Background")).raycastTarget = true;
            ExchangeView = GetOrAdd<ExchangeScreenView>(gameObject);
            ExchangeView.Configure(
                View.ExchangeButton,
                RequireComponent<Button>(RequireTransform(
                    exchangeRoot,
                    "ExitButton")),
                RequireComponent<Button>(RequireTransform(
                    exchangeRoot,
                    "HeroButton")),
                RequireComponent<Button>(RequireTransform(
                    exchangeRoot,
                    "ArtifactButton")),
                exchangeRoot.gameObject,
                RequireTransform(exchangeRoot, "Confirm").gameObject,
                RequireComponent<TMP_Text>(RequireTransform(
                    exchangeRoot,
                    "Medal/MedalText")),
                RequireComponent<TMP_Text>(RequireTransform(
                    exchangeRoot,
                    "Artifact/ArtifactText")),
                RequireComponent<TMP_Text>(RequireTransform(
                    exchangeRoot,
                    "Gold/GoldText")),
                GetOrAdd<CanvasGroup>(exchangeRoot.gameObject));

            RectTransform confirmRoot = RequireTransform(
                exchangeRoot,
                "Confirm");
            RequireComponent<Image>(RequireTransform(
                confirmRoot,
                "BackgroundPanel")).raycastTarget = true;
            RequireComponent<Image>(RequireTransform(
                confirmRoot,
                "Background")).raycastTarget = true;
            ExchangeConfirmView = GetOrAdd<ExchangeConfirmView>(gameObject);
            ExchangeConfirmView.Configure(
                confirmRoot.gameObject,
                RequireComponent<Image>(RequireTransform(
                    confirmRoot,
                    "Image")),
                RequireTransform(
                    confirmRoot,
                    "Awakening Icon").gameObject,
                RequireComponent<TMP_Text>(RequireTransform(
                    confirmRoot,
                    "AwakeningText")),
                RequireComponent<Image>(RequireTransform(
                    confirmRoot,
                    "PriceIcon")),
                RequireComponent<TMP_Text>(RequireTransform(
                    confirmRoot,
                    "PriceText")),
                RequireComponent<Button>(RequireTransform(
                    confirmRoot,
                    "OButton")),
                RequireComponent<Button>(RequireTransform(
                    confirmRoot,
                    "XButton")),
                GetOrAdd<CanvasGroup>(confirmRoot.gameObject));

            ExchangeController = GetOrAdd<ExchangeScreenController>(
                gameObject);
            ExchangeController.Configure(
                ExchangeView,
                ExchangeGridView,
                ExchangeConfirmView,
                CreateExchangeSpriteResolver,
                ShowExchangeFeedback);
        }

        private ExchangeTransactionService
            GetOrCreateExchangeTransactionService(
                SaveService saveService,
                DefinitionDatabase database)
        {
            if (ExchangeTransactionService != null
                && ReferenceEquals(
                    exchangeTransactionSaveService,
                    saveService)
                && ReferenceEquals(exchangeTransactionDatabase, database))
            {
                return ExchangeTransactionService;
            }

            ExchangeTransactionService = new ExchangeTransactionService(
                saveService,
                database,
                new GuidRelicInstanceIdProvider());
            exchangeTransactionSaveService = saveService;
            exchangeTransactionDatabase = database;
            return ExchangeTransactionService;
        }

        private void ShowExchangeFeedback(string message)
        {
            if (InfoPresenter == null
                || !InfoPresenter.ShowMessage(message))
            {
                GameLogger.Error(
                    "[GachaSceneInstaller] Exchange feedback could not "
                        + "be displayed: " + message,
                    this);
            }
        }

        private GachaPresentationController InstallIndividualPresentation(
            GachaResultPresenter resultPresenter)
        {
            RectTransform presentationRoot = RequireTransform(
                transform,
                "GachaPresentation");
            RectTransform character = RequireTransform(
                presentationRoot,
                "Character");
            RectTransform characterOverlay = RequireTransform(
                character,
                "WhenAlreadyAcquired");
            RectTransform artifactOrItem = RequireTransform(
                presentationRoot,
                "ArtifactOrItem");
            RectTransform artifact = RequireTransform(
                artifactOrItem,
                "UseWhenArtifact");
            RectTransform item = RequireTransform(
                artifactOrItem,
                "UseWhenItem");
            RectTransform artifactOverlay = RequireTransform(
                artifactOrItem,
                "WhenAlreadyAcquired");
            RectTransform curtain = RequireTransform(
                presentationRoot,
                "Curtain");
            Button touchButton = RequireComponent<Button>(RequireTransform(
                presentationRoot,
                "TouchPanel"));
            Button skipButton = RequireComponent<Button>(RequireTransform(
                presentationRoot,
                "SkipButton"));

            var presentationView = GetOrAdd<GachaPresentationView>(
                gameObject);
            presentationView.Configure(
                presentationRoot,
                character.gameObject,
                RequireComponent<Image>(RequireTransform(
                    character,
                    "CharacterImage")),
                characterOverlay.gameObject,
                RequireComponent<Image>(RequireTransform(
                    characterOverlay,
                    "IconImage")),
                RequireComponent<TMP_Text>(RequireTransform(
                    characterOverlay,
                    "Text")),
                artifactOrItem.gameObject,
                artifact.gameObject,
                RequireComponent<Image>(RequireTransform(
                    artifact,
                    "ArtifactImage")),
                item.gameObject,
                RequireComponent<Image>(RequireTransform(
                    item,
                    "ItemImage")),
                RequireComponent<TMP_Text>(RequireTransform(
                    item,
                    "ItemText")),
                artifactOverlay.gameObject,
                RequireComponent<Image>(RequireTransform(
                    artifactOverlay,
                    "IconImage")),
                RequireComponent<TMP_Text>(RequireTransform(
                    artifactOverlay,
                    "Text")),
                RequireTransform(
                    presentationRoot,
                    "GachaResult").gameObject,
                curtain.gameObject,
                RequireTransform(curtain, "LeftCurtain"),
                RequireTransform(curtain, "RightCurtain"),
                touchButton,
                skipButton);

            var controller = GetOrAdd<GachaPresentationController>(
                gameObject);
            controller.Configure(
                presentationView,
                resultPresenter,
                openingDuration,
                resultWaitDuration,
                transitionDuration,
                curtainClosedDelay,
                skipHideDelay);
            return controller;
        }

        private GachaInfoPanelPresenter InstallInfoPanel()
        {
            GameObject panel = RequireTransform(
                transform,
                "InfoPanel").gameObject;
            var infoView = GetOrAdd<GachaInfoPanelView>(gameObject);
            infoView.Configure(
                RequireComponent<Button>(RequireTransform(
                    transform,
                    "HeroesPickup/InfoButton")),
                RequireComponent<Button>(RequireTransform(
                    transform,
                    "ArtifactPickup/InfoButton")),
                RequireComponent<Button>(RequireTransform(
                    transform,
                    "InfoPanel/ExitButton")),
                panel,
                RequireComponent<TMP_Text>(RequireTransform(
                    transform,
                    "InfoPanel/InfoText")));

            var presenter = GetOrAdd<GachaInfoPanelPresenter>(gameObject);
            presenter.Configure(infoView);
            return presenter;
        }

        private GachaResultPresenter InstallResultPresentation()
        {
            GameObject presentationRoot = RequireTransform(
                transform,
                "GachaPresentation").gameObject;
            GameObject resultRoot = RequireTransform(
                transform,
                "GachaPresentation/GachaResult").gameObject;
            GameObject touchRoot = RequireTransform(
                transform,
                "GachaPresentation/TouchPanel").gameObject;
            Image touchImage = RequireComponent<Image>(touchRoot.transform);
            touchImage.raycastTarget = true;
            Button touchButton = GetOrAdd<Button>(touchRoot);
            touchButton.targetGraphic = touchImage;
            touchButton.transition = Selectable.Transition.None;
            touchButton.interactable = true;

            var slots = new GachaResultSlotView[
                GachaResultView.MaximumResultCount];
            for (int index = 0; index < slots.Length; index++)
            {
                string slotName = index == 0
                    ? "Slot"
                    : $"Slot ({index})";
                RectTransform slotRoot = RequireTransform(
                    transform,
                    $"GachaPresentation/GachaResult/Slots/{slotName}");
                var slotView = GetOrAdd<GachaResultSlotView>(
                    slotRoot.gameObject);
                slotView.Configure(
                    RequireTransform(
                        slotRoot,
                        "UseWhenArtifactOrCharacter").gameObject,
                    RequireTransform(
                        slotRoot,
                        "UseWhenItem").gameObject,
                    RequireComponent<Image>(RequireTransform(
                        slotRoot,
                        "UseWhenArtifactOrCharacter/"
                            + "ArtifactOrCharacterImage")),
                    RequireComponent<Image>(RequireTransform(
                        slotRoot,
                        "UseWhenItem/ItemImage")),
                    RequireComponent<TMP_Text>(RequireTransform(
                        slotRoot,
                        "UseWhenItem/ItemText")),
                    RequireComponent<Image>(RequireTransform(
                        slotRoot,
                        "AwakeningIcon")));
                slots[index] = slotView;
            }

            var resultView = GetOrAdd<GachaResultView>(gameObject);
            resultView.Configure(
                presentationRoot,
                RequireTransform(
                    transform,
                    "GachaPresentation/Character").gameObject,
                RequireTransform(
                    transform,
                    "GachaPresentation/ArtifactOrItem").gameObject,
                resultRoot,
                RequireTransform(
                    transform,
                    "GachaPresentation/Curtain").gameObject,
                RequireTransform(
                    transform,
                    "GachaPresentation/SkipButton").gameObject,
                touchButton,
                slots);

            var presenter = GetOrAdd<GachaResultPresenter>(gameObject);
            presenter.Configure(
                resultView,
                characterPresentationCatalog,
                heroTokenSprite,
                relicTokenSprite,
                battleRecordsSprite,
                awakeningSprites);
            return presenter;
        }

        private void DisableKnownActions()
        {
            string[] paths =
            {
                "HeroesPickup/1RunButton",
                "HeroesPickup/10RunButton",
                "ArtifactPickup/1RunButton",
                "ArtifactPickup/10RunButton",
                "SettingButton",
                "ExchangeButton"
            };
            for (int index = 0; index < paths.Length; index++)
            {
                Transform target = transform.Find(paths[index]);
                Button button = target != null
                    ? target.GetComponent<Button>()
                    : null;
                if (button != null)
                {
                    button.interactable = false;
                }
            }
        }

        private static RectTransform RequireTransform(
            Transform root,
            string path)
        {
            Transform found = root.Find(path);
            return found as RectTransform
                ?? throw new InvalidOperationException(
                    $"Required Gacha scene object is missing: {path}.");
        }

        private static T RequireComponent<T>(Component target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null
                ? component
                : throw new InvalidOperationException(
                    $"'{target.name}' requires {typeof(T).Name}.");
        }

        private static T RequireReference<T>(T reference, string fieldName)
            where T : UnityEngine.Object
        {
            return reference != null
                ? reference
                : throw new InvalidOperationException(
                    $"GachaSceneInstaller requires '{fieldName}'.");
        }

        private static T GetOrAdd<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}
