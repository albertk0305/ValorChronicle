using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Logging;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Localization;
using ValorChronicle.Relics.Inventory;
using ValorChronicle.Relics.Persistence;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;
using ValorChronicle.Save.Services;

namespace ValorChronicle.Relics.Presentation
{
    public sealed class CharacterRelicScreenController : MonoBehaviour
    {
        private CharacterRelicSlotsView slotsView;
        private RelicSelectionScreenView selectionView;
        private VirtualizedRelicGridView inventoryGrid;
        private CharacterUpgradeScreenController upgradeController;
        private SaveService saveService;
        private DefinitionDatabase definitionDatabase;
        private LocalizationService localizationService;
        private RelicEquipEditor equipEditor;
        private RelicEquipPersistenceService persistenceService;
        private RelicInventoryQuery inventoryQuery;
        private RelicEquipEditSession editSession;
        private ProfileSaveData editProfileSnapshot;
        private string presentedCharacterId;
        private bool subscribed;
        private bool localizationSubscribed;
        private bool initialized;

        public event Action SelectionOpened;
        public event Action SelectionClosed;
        public event Action AuthoritativeProfileRefreshRequested;

        public RelicEquipEditSession EditSession => editSession;
        public RelicEquipPersistenceOperationResult LastPersistenceResult
        {
            get;
            private set;
        }
        public string PresentedCharacterId => presentedCharacterId;
        public bool IsSelectionOpen => editSession != null;

        private void Start()
        {
            if (initialized)
            {
                return;
            }

            GameBootstrapper bootstrap = GameBootstrapper.Instance;
            if (bootstrap?.SaveService == null
                || bootstrap.DefinitionDatabase == null
                || bootstrap.LocalizationService == null)
            {
                GameLogger.Error(
                    "[CharacterRelicScreenController] Initialized "
                        + "SaveService, DefinitionDatabase, and "
                        + "LocalizationService are required.",
                    this);
                slotsView?.Clear();
                selectionView?.SetVisible(false);
                return;
            }

            try
            {
                Initialize(
                    bootstrap.SaveService,
                    bootstrap.DefinitionDatabase,
                    bootstrap.LocalizationService);
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[CharacterRelicScreenController] Initialization failed.",
                    this);
                slotsView?.Clear();
                selectionView?.SetVisible(false);
            }
        }

        private void OnEnable()
        {
            Subscribe();
            SubscribeLocalization();
        }

        private void OnDisable()
        {
            Unsubscribe();
            UnsubscribeLocalization();
            DiscardSession();
        }

        public void Configure(
            CharacterRelicSlotsView configuredSlotsView,
            RelicSelectionScreenView configuredSelectionView,
            VirtualizedRelicGridView configuredInventoryGrid,
            CharacterUpgradeScreenController configuredUpgradeController)
        {
            Unsubscribe();
            slotsView = configuredSlotsView
                ?? throw new ArgumentNullException(
                    nameof(configuredSlotsView));
            selectionView = configuredSelectionView
                ?? throw new ArgumentNullException(
                    nameof(configuredSelectionView));
            inventoryGrid = configuredInventoryGrid
                ?? throw new ArgumentNullException(
                    nameof(configuredInventoryGrid));
            upgradeController = configuredUpgradeController
                ?? throw new ArgumentNullException(
                    nameof(configuredUpgradeController));
            selectionView.SetVisible(false);
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        public void Initialize(
            SaveService initializedSaveService,
            DefinitionDatabase initializedDefinitionDatabase,
            LocalizationService initializedLocalizationService,
            RelicEquipEditor initializedEquipEditor = null,
            RelicEquipPersistenceService initializedPersistenceService = null)
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

            localizationService = initializedLocalizationService
                ?? throw new ArgumentNullException(
                    nameof(initializedLocalizationService));
            if (slotsView == null
                || selectionView == null
                || inventoryGrid == null
                || upgradeController == null)
            {
                throw new InvalidOperationException(
                    "Character relic screen dependencies are incomplete.");
            }

            equipEditor = initializedEquipEditor ?? new RelicEquipEditor();
            persistenceService = initializedPersistenceService
                ?? new RelicEquipPersistenceService(
                    saveService,
                    equipEditor);
            inventoryQuery = new RelicInventoryQuery(
                definitionDatabase,
                localizationService,
                equipEditor);
            initialized = true;
            if (isActiveAndEnabled)
            {
                SubscribeLocalization();
            }
        }

        public bool TryPresent(string characterId)
        {
            if (!initialized
                || string.IsNullOrEmpty(characterId)
                || !TryGetCurrentProfile(out ProfileSaveData profile)
                || FindCharacter(profile, characterId) == null)
            {
                presentedCharacterId = null;
                slotsView?.Clear();
                return false;
            }

            presentedCharacterId = characterId;
            RenderSlots(profile, characterId);
            return true;
        }

        public bool Open(string characterId, int slotIndex)
        {
            if (!initialized
                || !TryGetCurrentProfile(out ProfileSaveData profile))
            {
                return false;
            }

            try
            {
                editSession = new RelicEquipEditSession(
                    equipEditor,
                    profile,
                    characterId,
                    slotIndex);
                editProfileSnapshot = profile;
                presentedCharacterId = characterId;
                LastPersistenceResult = null;
                selectionView.SetVisible(true);
                RenderPreview(editSession.PreviewInstanceId);
                RefreshInventory(resetScroll: true);
                SelectionOpened?.Invoke();
                return true;
            }
            catch (Exception exception)
                when (exception is ArgumentException
                    || exception is InvalidOperationException)
            {
                DiscardSession();
                GameLogger.Warning(
                    "[CharacterRelicScreenController] Could not open relic "
                        + $"slot {slotIndex} for '{characterId}'.",
                    this);
                return false;
            }
        }

        public void Close()
        {
            bool wasOpen = editSession != null;
            DiscardSession();
            selectionView?.Clear();
            inventoryGrid?.Clear();
            selectionView?.SetVisible(false);
            if (wasOpen)
            {
                SelectionClosed?.Invoke();
            }
        }

        public RelicEquipAvailability TrySetPreview(string instanceId)
        {
            if (editSession == null)
            {
                return null;
            }

            RelicEquipAvailability availability =
                editSession.TrySetPreview(instanceId);
            if (availability.CanSelect)
            {
                RenderPreview(editSession.PreviewInstanceId);
            }

            return availability;
        }

        public void RenderPreview(string instanceId)
        {
            selectionView.RenderPreview(CreatePresentation(
                editProfileSnapshot,
                instanceId,
                includeDescription: true));
        }

        public void ConfirmEquip()
        {
            Execute(editSession?.CreateEquipCommand());
        }

        public void ClearTargetSlot()
        {
            Execute(editSession?.CreateClearCommand());
        }

        private void Execute(RelicEquipSessionCommand command)
        {
            if (command == null)
            {
                return;
            }

            LastPersistenceResult = null;
            try
            {
                LastPersistenceResult = persistenceService.Execute(command);
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[CharacterRelicScreenController] Relic equip request "
                        + "failed unexpectedly.",
                    this);
                return;
            }

            if (!LastPersistenceResult.IsSuccess)
            {
                if (LastPersistenceResult.Status
                    != RelicEquipPersistenceStatus.NoRelicSelected)
                {
                    GameLogger.Warning(
                        "[CharacterRelicScreenController] Relic equip was "
                            + "not saved. Status="
                            + $"{LastPersistenceResult.Status}.",
                        this);
                }

                return;
            }

            string characterId = command.CharacterId;
            Close();
            TryPresent(characterId);
            upgradeController.TryPresent(characterId);
            AuthoritativeProfileRefreshRequested?.Invoke();
        }

        private void RenderSlots(
            ProfileSaveData profile,
            string characterId)
        {
            CharacterSaveData character = FindCharacter(
                profile,
                characterId);
            var models = new RelicPresentationModel[
                SaveRules.RelicSlotCountPerCharacter];
            for (int index = 0; index < models.Length; index++)
            {
                string instanceId =
                    character.EquippedRelicInstanceIds[index];
                models[index] = CreatePresentation(
                    profile,
                    instanceId,
                    includeDescription: false);
            }

            slotsView.Render(models);
        }

        private RelicPresentationModel CreatePresentation(
            ProfileSaveData profile,
            string instanceId,
            bool includeDescription)
        {
            RelicInstanceSaveData instance = FindRelic(
                profile,
                instanceId);
            if (instance == null
                || !definitionDatabase.TryGetRelic(
                    instance.RelicDefinitionId,
                    out RelicDefinition definition))
            {
                return RelicPresentationModel.Empty();
            }

            definitionDatabase.TryGetRelicPresentation(
                instance.RelicDefinitionId,
                out RelicPresentationDefinition presentation);
            string description = includeDescription
                ? localizationService.Format(
                    definition.DescriptionKey,
                    new Dictionary<string, object>
                    {
                        ["rate"] = FormatPercent(
                            definition.GetEffectRate(instance.Awakening))
                    })
                : string.Empty;
            return RelicPresentationModel.Equipped(
                instance.InstanceId,
                presentation?.Icon,
                instance.Awakening,
                description);
        }

        private void HandleSlotRequested(int slotIndex)
        {
            if (!string.IsNullOrEmpty(presentedCharacterId))
            {
                Open(presentedCharacterId, slotIndex);
            }
        }

        private void HandleLocaleChanged(string _)
        {
            if (editSession != null)
            {
                RefreshInventory(resetScroll: false);
                RenderPreview(editSession.PreviewInstanceId);
            }
        }

        private void HandleInventoryRelicRequested(string instanceId)
        {
            TrySetPreview(instanceId);
        }

        private void RefreshInventory(bool resetScroll)
        {
            if (editSession == null || editProfileSnapshot == null)
            {
                inventoryGrid.Clear();
                return;
            }

            IReadOnlyList<RelicInventoryEntry> entries =
                inventoryQuery.BuildEntries(
                    editProfileSnapshot,
                    editSession.CharacterId,
                    editSession.TargetSlotIndex,
                    localizationService.CurrentLocale);
            inventoryGrid.SetItems(
                entries,
                ResolveRelicIcon,
                resetScroll);
        }

        private Sprite ResolveRelicIcon(string relicDefinitionId)
        {
            return definitionDatabase.TryGetRelicPresentation(
                relicDefinitionId,
                out RelicPresentationDefinition presentation)
                ? presentation.Icon
                : null;
        }

        private void Subscribe()
        {
            if (subscribed || slotsView == null || selectionView == null)
            {
                return;
            }

            slotsView.SlotRequested += HandleSlotRequested;
            selectionView.EquipRequested += ConfirmEquip;
            selectionView.RemoveRequested += ClearTargetSlot;
            inventoryGrid.RelicRequested +=
                HandleInventoryRelicRequested;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            slotsView.SlotRequested -= HandleSlotRequested;
            selectionView.EquipRequested -= ConfirmEquip;
            selectionView.RemoveRequested -= ClearTargetSlot;
            inventoryGrid.RelicRequested -=
                HandleInventoryRelicRequested;
            subscribed = false;
        }

        private void SubscribeLocalization()
        {
            if (localizationSubscribed || localizationService == null)
            {
                return;
            }

            localizationService.LocaleChanged += HandleLocaleChanged;
            localizationSubscribed = true;
        }

        private void UnsubscribeLocalization()
        {
            if (!localizationSubscribed)
            {
                return;
            }

            localizationService.LocaleChanged -= HandleLocaleChanged;
            localizationSubscribed = false;
        }

        private void DiscardSession()
        {
            editSession = null;
            editProfileSnapshot = null;
        }

        private bool TryGetCurrentProfile(out ProfileSaveData profile)
        {
            profile = null;
            if (saveService?.HasCurrentProfile != true)
            {
                return false;
            }

            try
            {
                profile = saveService.GetCurrentProfileSnapshot();
                return true;
            }
            catch (InvalidOperationException)
            {
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

        private static RelicInstanceSaveData FindRelic(
            ProfileSaveData profile,
            string instanceId)
        {
            if (profile?.RelicInstances == null
                || string.IsNullOrEmpty(instanceId))
            {
                return null;
            }

            for (int index = 0;
                index < profile.RelicInstances.Count;
                index++)
            {
                RelicInstanceSaveData relic = profile.RelicInstances[index];
                if (relic != null
                    && string.Equals(
                        relic.InstanceId,
                        instanceId,
                        StringComparison.Ordinal))
                {
                    return relic;
                }
            }

            return null;
        }

        private static string FormatPercent(double rate)
        {
            return (rate * 100d).ToString(
                "0.##",
                CultureInfo.InvariantCulture);
        }
    }
}
