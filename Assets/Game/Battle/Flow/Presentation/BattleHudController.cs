using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Data.Database;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Flow.Presentation
{
    public sealed class BattleHudController : MonoBehaviour
    {
        [SerializeField]
        private BattleFlowController battleFlowController = null;

        [SerializeField]
        private BattleSceneCombatBootstrap battleSceneCombatBootstrap = null;

        [SerializeField]
        private TMP_Text turnText = null;

        [SerializeField]
        private TMP_Text scoreText = null;

        [SerializeField]
        private GameObject resultOverlay = null;

        [SerializeField]
        private TMP_Text resultText = null;

        [SerializeField]
        [Min(0f)]
        private float resultOverlayDelaySeconds = 1f;

        [SerializeField]
        private BattleBossIntentSlotView[] bossIntentSlots =
            Array.Empty<BattleBossIntentSlotView>();

        [SerializeField]
        private Image bossImage = null;

        [SerializeField]
        private Sprite resourceFallbackIcon = null;

        [SerializeField]
        private BattleStatusIconView[] bossStatusSlots =
            Array.Empty<BattleStatusIconView>();

        [SerializeField]
        private Slider bossHpSlider = null;

        [SerializeField]
        private TMP_Text bossHpText = null;

        [SerializeField]
        private Image bossShieldImage = null;

        [SerializeField]
        private TMP_Text comboText = null;

        [SerializeField]
        private BattleMatchEventSlotView[] matchEventSlots =
            Array.Empty<BattleMatchEventSlotView>();

        [SerializeField]
        private BattleCharacterSlotView[] characterSlots =
            Array.Empty<BattleCharacterSlotView>();

        [SerializeField]
        private BoardElementSpriteSet elementSpriteSet = null;

        [SerializeField]
        private BattleStatusIconView[] partyStatusSlots =
            Array.Empty<BattleStatusIconView>();

        [SerializeField]
        private Slider partyHpSlider = null;

        [SerializeField]
        private TMP_Text partyHpText = null;

        [SerializeField]
        private Image partyShieldImage = null;

        private BattleFlowCoordinator subscribedCoordinator;
        private BattleFlowCombatBridge subscribedBridge;
        private BattleFlowController subscribedFlowController;
        private UnityAction[] activeButtonListeners =
            Array.Empty<UnityAction>();
        private bool safeDisplayInitialized;
        private bool resultHudFrozen;
        private int displayedFinalComboCount;
        private bool partyShieldLayoutCached;
        private float partyShieldFullWidth;
        private float partyShieldFullPositionX;
        private float partyShieldPivotX;
        private Sprite defaultBossSprite;
        private bool defaultBossSpriteCached;
        public BattleFlowController BattleFlowController =>
            battleFlowController;
        public BattleSceneCombatBootstrap BattleSceneCombatBootstrap =>
            battleSceneCombatBootstrap;
        public BattleFlowCoordinator Coordinator => subscribedCoordinator;
        public BattleFlowCombatBridge CombatBridge => subscribedBridge;
        public bool IsRuntimeConnected =>
            subscribedCoordinator != null && subscribedBridge != null;
        public TMP_Text TurnText => turnText;
        public TMP_Text ScoreText => scoreText;
        public GameObject ResultOverlay => resultOverlay;
        public TMP_Text ResultText => resultText;
        public float ResultOverlayDelaySeconds => resultOverlayDelaySeconds;
        public Image BossImage => bossImage;
        public Sprite ResourceFallbackIcon => resourceFallbackIcon;
        public Slider BossHpSlider => bossHpSlider;
        public TMP_Text BossHpText => bossHpText;
        public Image BossShieldImage => bossShieldImage;
        public TMP_Text ComboText => comboText;
        public Slider PartyHpSlider => partyHpSlider;
        public TMP_Text PartyHpText => partyHpText;
        public Image PartyShieldImage => partyShieldImage;
        public int BossIntentSlotCount => bossIntentSlots?.Length ?? 0;
        public int BossStatusSlotCount => bossStatusSlots?.Length ?? 0;
        public int MatchEventSlotCount => matchEventSlots?.Length ?? 0;
        public int CharacterSlotCount => characterSlots?.Length ?? 0;
        public BoardElementSpriteSet ElementSpriteSet => elementSpriteSet;
        public int PartyStatusSlotCount => partyStatusSlots?.Length ?? 0;

        private void Awake()
        {
            InitializeSafeDisplay();
        }

        private void OnEnable()
        {
            InitializeSafeDisplay();
            AttachActiveButtonListeners();
            TryConnectRuntime();
        }

        private void Start()
        {
            TryConnectRuntime();
        }

        private void OnDisable()
        {
            DetachActiveButtonListeners();
            DisconnectRuntime();
        }

        private void OnDestroy()
        {
            DetachActiveButtonListeners();
            DisconnectRuntime();
        }

        public BattleBossIntentSlotView GetBossIntentSlot(int index)
        {
            return bossIntentSlots[index];
        }

        public BattleStatusIconView GetBossStatusSlot(int index)
        {
            return bossStatusSlots[index];
        }

        public BattleMatchEventSlotView GetMatchEventSlot(int index)
        {
            return matchEventSlots[index];
        }

        public BattleCharacterSlotView GetCharacterSlot(int index)
        {
            return characterSlots[index];
        }

        public BattleStatusIconView GetPartyStatusSlot(int index)
        {
            return partyStatusSlots[index];
        }

        public bool TryResolvePlayerProjectileAnchors(
            int partySlotIndex,
            string characterId,
            out RectTransform source,
            out RectTransform target)
        {
            source = null;
            target = null;
            if (!IsRuntimeConnected
                || string.IsNullOrWhiteSpace(characterId)
                || characterSlots == null
                || partySlotIndex < 0
                || partySlotIndex >= characterSlots.Length
                || bossImage == null
                || !bossImage.enabled
                || !bossImage.gameObject.activeInHierarchy)
            {
                return false;
            }

            CharacterBattleState character = null;
            IReadOnlyList<CharacterBattleState> characters =
                subscribedBridge.Party.Characters;
            for (int index = 0; index < characters.Count; index++)
            {
                CharacterBattleState candidate = characters[index];
                if (candidate.PartySlotIndex == partySlotIndex
                    && string.Equals(
                        candidate.CharacterId,
                        characterId,
                        StringComparison.Ordinal))
                {
                    character = candidate;
                    break;
                }
            }

            BattleCharacterSlotView slot = characterSlots[partySlotIndex];
            if (character == null
                || slot == null
                || !string.Equals(
                    slot.CharacterId,
                    character.CharacterId,
                    StringComparison.Ordinal)
                || slot.Root == null
                || !slot.Root.activeInHierarchy
                || slot.CharacterImage == null
                || !slot.CharacterImage.enabled
                || !slot.CharacterImage.gameObject.activeInHierarchy)
            {
                return false;
            }

            source = slot.CharacterImage.rectTransform;
            target = bossImage.rectTransform;
            return true;
        }

        public bool TryResolveBossProjectileAnchors(
            out RectTransform source,
            out IReadOnlyList<RectTransform> targets)
        {
            source = null;
            targets = Array.Empty<RectTransform>();
            if (!IsRuntimeConnected
                || bossImage == null
                || !bossImage.enabled
                || !bossImage.gameObject.activeInHierarchy
                || characterSlots == null)
            {
                return false;
            }

            var resolvedTargets = new List<RectTransform>();
            for (int slotIndex = 0;
                slotIndex < characterSlots.Length;
                slotIndex++)
            {
                CharacterBattleState character =
                    FindPartyCharacterAtSlot(slotIndex);
                BattleCharacterSlotView slot = characterSlots[slotIndex];
                if (character == null
                    || string.IsNullOrWhiteSpace(character.CharacterId)
                    || slot == null
                    || !string.Equals(
                        slot.CharacterId,
                        character.CharacterId,
                        StringComparison.Ordinal)
                    || slot.Root == null
                    || !slot.Root.activeInHierarchy
                    || slot.CharacterImage == null
                    || !slot.CharacterImage.enabled
                    || !slot.CharacterImage.gameObject.activeInHierarchy)
                {
                    continue;
                }

                resolvedTargets.Add(slot.CharacterImage.rectTransform);
            }

            if (resolvedTargets.Count == 0
                && partyHpSlider != null
                && partyHpSlider.enabled
                && partyHpSlider.gameObject.activeInHierarchy)
            {
                resolvedTargets.Add(partyHpSlider.transform as RectTransform);
            }

            if (resolvedTargets.Count == 0
                || resolvedTargets[0] == null)
            {
                return false;
            }

            source = bossImage.rectTransform;
            targets = resolvedTargets.ToArray();
            return true;
        }

        public void RefreshInitialSnapshot()
        {
            if (!IsRuntimeConnected)
            {
                return;
            }

            resultHudFrozen = IsNormalTerminalResult(
                battleFlowController?.Context?.Result
                ?? BattleResultKind.None);
            if (resultHudFrozen)
            {
                RefreshCombatState();
                RefreshActiveAvailability();
                return;
            }

            RefreshTurnAndPhase();
            RefreshScorePlaceholder();
            RefreshCombatState();
            RefreshBossIntent();
            RefreshCombo();
            RefreshMatchQueue();
            RefreshActiveAvailability();
            RefreshStatusSnapshot();
        }

        private bool TryConnectRuntime()
        {
            BattleFlowCoordinator coordinator =
                battleFlowController?.Coordinator;
            BattleFlowCombatBridge bridge =
                battleFlowController?.CombatBridge;
            if (coordinator == null || bridge == null)
            {
                DisconnectRuntime();
                RefreshCombo();
                RefreshMatchQueue();
                RefreshActiveAvailability();
                RefreshStatusSnapshot();
                return false;
            }

            if (ReferenceEquals(coordinator, subscribedCoordinator)
                && ReferenceEquals(bridge, subscribedBridge))
            {
                return true;
            }

            DisconnectRuntime();
            coordinator.PhaseChanged += HandlePhaseChanged;
            coordinator.MatchEventExecuting += HandleMatchEventExecuting;
            coordinator.BossActionStarted += HandleBossActionStarted;
            coordinator.ResultReached += HandleResultReached;
            bridge.CombatActionsApplied += HandleCombatActionsApplied;
            bridge.CombatActionStepApplied +=
                HandleCombatActionStepApplied;
            bridge.BossCombatActionStepApplied +=
                HandleBossCombatActionStepApplied;
            bridge.ActiveCombatActionStepApplied +=
                HandleActiveCombatActionStepApplied;
            battleFlowController.PresentationMatchQueueChanged +=
                HandlePresentationMatchQueueChanged;
            battleFlowController.ActivePresentationStateChanged +=
                HandleActivePresentationStateChanged;
            subscribedCoordinator = coordinator;
            subscribedBridge = bridge;
            subscribedFlowController = battleFlowController;
            resultHudFrozen = IsNormalTerminalResult(
                battleFlowController.Context.Result);
            subscribedFlowController
                .ReconcilePresentationMatchQueueForCurrentRuntime();
            RefreshInitialSnapshot();
            return true;
        }

        private void DisconnectRuntime()
        {
            if (subscribedCoordinator != null)
            {
                subscribedCoordinator.PhaseChanged -= HandlePhaseChanged;
                subscribedCoordinator.MatchEventExecuting -=
                    HandleMatchEventExecuting;
                subscribedCoordinator.BossActionStarted -=
                    HandleBossActionStarted;
                subscribedCoordinator.ResultReached -= HandleResultReached;
            }

            if (subscribedBridge != null)
            {
                subscribedBridge.CombatActionsApplied -=
                    HandleCombatActionsApplied;
                subscribedBridge.CombatActionStepApplied -=
                    HandleCombatActionStepApplied;
                subscribedBridge.BossCombatActionStepApplied -=
                    HandleBossCombatActionStepApplied;
                subscribedBridge.ActiveCombatActionStepApplied -=
                    HandleActiveCombatActionStepApplied;
            }

            if (subscribedFlowController != null)
            {
                subscribedFlowController.PresentationMatchQueueChanged -=
                    HandlePresentationMatchQueueChanged;
                subscribedFlowController.ActivePresentationStateChanged -=
                    HandleActivePresentationStateChanged;
            }

            subscribedCoordinator = null;
            subscribedBridge = null;
            subscribedFlowController = null;
        }

        private void AttachActiveButtonListeners()
        {
            DetachActiveButtonListeners();
            int slotCount = characterSlots?.Length ?? 0;
            activeButtonListeners = new UnityAction[slotCount];
            for (int index = 0; index < slotCount; index++)
            {
                int partySlotIndex = index;
                UnityAction listener =
                    () => HandleActiveClicked(partySlotIndex);
                activeButtonListeners[index] = listener;
                characterSlots[index]?.SetActiveListener(listener);
            }
        }

        private void DetachActiveButtonListeners()
        {
            int listenerCount = activeButtonListeners?.Length ?? 0;
            int slotCount = characterSlots?.Length ?? 0;
            int count = Math.Min(listenerCount, slotCount);
            for (int index = 0; index < count; index++)
            {
                characterSlots[index]?.ClearActiveListener();
            }

            activeButtonListeners = Array.Empty<UnityAction>();
        }

        private void HandleActiveClicked(int partySlotIndex)
        {
            if (!TryResolveActiveBinding(
                    partySlotIndex,
                    out ActiveAbilityBinding binding)
                || !battleFlowController.CanUseActive(
                    binding.ActiveAbilityIndex))
            {
                return;
            }

            battleFlowController.TryUseActive(binding.ActiveAbilityIndex);
        }

        private CharacterBattleState FindPartyCharacterAtSlot(
            int partySlotIndex)
        {
            if (subscribedBridge == null)
            {
                return null;
            }

            var characters = subscribedBridge.Party.Characters;
            for (int index = 0; index < characters.Count; index++)
            {
                if (characters[index].PartySlotIndex == partySlotIndex)
                {
                    return characters[index];
                }
            }

            return null;
        }

        private bool TryResolveActiveBinding(
            int partySlotIndex,
            out ActiveAbilityBinding binding)
        {
            binding = null;
            CharacterBattleState character =
                FindPartyCharacterAtSlot(partySlotIndex);
            return character != null
                && subscribedBridge.TryGetSingleActiveBinding(
                    partySlotIndex,
                    character.CharacterId,
                    out binding);
        }

        private void InitializeSafeDisplay()
        {
            if (safeDisplayInitialized)
            {
                return;
            }

            safeDisplayInitialized = true;
            SetSlotsVisible(bossIntentSlots, false);
            SetSlotsVisible(bossStatusSlots, false);
            SetSlotsVisible(matchEventSlots, false);
            SetSlotsVisible(partyStatusSlots, false);
            ClearText(turnText);
            ClearText(scoreText);
            SetActive(resultOverlay, false);
            ClearText(resultText);
            ClearText(bossHpText);
            ClearText(comboText);
            ClearText(partyHpText);
            CacheDefaultBossSprite();
            CachePartyShieldLayout();
            if (bossShieldImage != null)
            {
                bossShieldImage.gameObject.SetActive(false);
            }

            if (partyShieldImage != null)
            {
                partyShieldImage.gameObject.SetActive(false);
            }

            if (characterSlots == null)
            {
                return;
            }

            for (int index = 0; index < characterSlots.Length; index++)
            {
                characterSlots[index]?.InitializeSafeDisplay();
            }
        }

        private void HandlePhaseChanged(BattlePhase phase)
        {
            if (phase == BattlePhase.Result
                && IsNormalTerminalResult(
                    battleFlowController?.Context?.Result
                    ?? BattleResultKind.None))
            {
                resultHudFrozen = true;
                RefreshCombatState();
                RefreshActiveAvailability();
                return;
            }

            resultHudFrozen = false;
            if (phase == BattlePhase.TurnStart)
            {
                displayedFinalComboCount = 0;
            }

            RefreshTurnAndPhase();
            RefreshCombatState();
            RefreshBossIntent();
            RefreshCombo();
            RefreshMatchQueue();
            RefreshActiveAvailability();
            RefreshStatusSnapshot();
        }

        private void HandleMatchEventExecuting(MatchEvent matchEvent)
        {
            MatchEventExecution execution =
                subscribedCoordinator?.CurrentMatchEventExecution;
            if (execution != null)
            {
                displayedFinalComboCount = execution.FinalComboCount;
            }

            RefreshCombo();
            RefreshMatchQueue();
        }

        private void HandleBossActionStarted()
        {
            RefreshBossIntent();
            RefreshActiveAvailability();
            RefreshStatusSnapshot();
        }

        private void HandleResultReached(BattleResultKind result)
        {
            if (IsNormalTerminalResult(result))
            {
                resultHudFrozen = true;
                RefreshCombatState();
                RefreshActiveAvailability();
                return;
            }

            resultHudFrozen = false;
            RefreshCombatState();
            RefreshBossIntent();
            RefreshCombo();
            RefreshMatchQueue();
            RefreshActiveAvailability();
            RefreshStatusSnapshot();
        }

        private void HandleCombatActionsApplied()
        {
            RefreshCombatState();
            RefreshActiveAvailability();
            RefreshStatusSnapshot();
        }

        private void HandleCombatActionStepApplied(
            MatchEventCharacterCombatExecutionStepResult stepResult)
        {
            battleFlowController?.CombatPresentationController?.TryPresent(
                stepResult.ActionStep.Result);
            RefreshCombatState();
            RefreshActiveAvailability();
            RefreshStatusSnapshot();
        }

        private void HandleBossCombatActionStepApplied(
            CombatActionExecutionStepResult stepResult)
        {
            battleFlowController?.CombatPresentationController?.TryPresent(
                stepResult.Result);
            RefreshCombatState();
            RefreshActiveAvailability();
            RefreshStatusSnapshot();
        }

        private void HandleActiveCombatActionStepApplied(
            ActiveCombatActionExecutionStepResult stepResult)
        {
            battleFlowController?.CombatPresentationController?.TryPresent(
                stepResult.ActionStep.Result);
            RefreshCombatState();
            RefreshActiveAvailability();
            RefreshStatusSnapshot();
        }

        private void HandleActivePresentationStateChanged()
        {
            if (resultHudFrozen)
            {
                return;
            }

            RefreshActiveAvailability();
        }

        private void HandlePresentationMatchQueueChanged()
        {
            if (resultHudFrozen)
            {
                return;
            }

            RefreshMatchQueue();
        }

        private static bool IsNormalTerminalResult(BattleResultKind result)
        {
            return result == BattleResultKind.Victory
                || result == BattleResultKind.Defeat
                || result == BattleResultKind.TurnLimitReached;
        }

        private void RefreshTurnAndPhase()
        {
            BattleContext context = battleFlowController?.Context;
            if (context == null || turnText == null)
            {
                return;
            }

            turnText.text = string.Concat(
                "Turn\n",
                context.CurrentTurn.ToString(CultureInfo.InvariantCulture),
                " / ",
                context.TurnLimit.ToString(CultureInfo.InvariantCulture));
        }

        private void RefreshCombatState()
        {
            if (subscribedBridge == null)
            {
                return;
            }

            long bossCurrentHp = subscribedBridge.Boss.CurrentHp;
            long bossMaxHp = subscribedBridge.Boss.MaxHp;
            SetNormalizedHpSlider(
                bossHpSlider,
                bossCurrentHp,
                bossMaxHp);
            SetHpText(
                bossHpText,
                bossCurrentHp,
                bossMaxHp,
                shield: 0);
            SetImageVisible(bossShieldImage, false);

            long partyCurrentHp = subscribedBridge.Party.CurrentHp;
            long partyMaxHp = subscribedBridge.Party.MaxHp;
            long partyShield = subscribedBridge.Party.Shields.TotalShield;
            SetNormalizedHpSlider(
                partyHpSlider,
                partyCurrentHp,
                partyMaxHp);
            SetHpText(
                partyHpText,
                partyCurrentHp,
                partyMaxHp,
                partyShield);
            SetShieldOverlay(
                partyShieldImage,
                partyShield,
                partyMaxHp);
        }

        private void RefreshBossIntent()
        {
            int slotCount = bossIntentSlots?.Length ?? 0;
            BattleSceneCombatComposition composition =
                battleSceneCombatBootstrap?.CombatComposition;
            IKragmorBossIntentSource source =
                battleSceneCombatBootstrap?.BossIntentSource;
            if (slotCount == 0
                || battleFlowController?.Context == null
                || battleFlowController.Context.Result
                    != BattleResultKind.None
                || composition == null
                || !ReferenceEquals(composition.Bridge, subscribedBridge)
                || source == null)
            {
                SetSlotsVisible(bossIntentSlots, false);
                return;
            }

            IReadOnlyList<KragmorBossIntentPreview> forecast =
                source.GetIntentForecast(slotCount);
            int renderedCount = Math.Min(forecast.Count, slotCount);
            for (int index = 0; index < renderedCount; index++)
            {
                KragmorBossIntentPreview preview = forecast[index];
                bossIntentSlots[index]?.Render(
                    ResolveIntentIcon(preview.Intent.SkillId),
                    preview.TurnsUntilAction);
            }

            for (int index = renderedCount; index < slotCount; index++)
            {
                bossIntentSlots[index]?.SetVisible(false);
            }
        }

        private Sprite ResolveIntentIcon(string skillId)
        {
            DefinitionDatabase database =
                battleSceneCombatBootstrap?.DefinitionDatabase;
            if (database != null
                && database.IsInitialized
                && database.TryGetSkill(
                    skillId,
                    out SkillDefinition definition))
            {
                return definition.Icon;
            }

            return null;
        }

        private void RefreshMatchQueue()
        {
            int slotCount = matchEventSlots?.Length ?? 0;
            if (slotCount == 0
                || subscribedCoordinator == null
                || subscribedCoordinator.Context.Result
                    != BattleResultKind.None)
            {
                SetSlotsVisible(matchEventSlots, false);
                return;
            }

            IReadOnlyList<BattleMatchQueuePresentationEntry> pending =
                subscribedFlowController?.MatchQueuePresentation
                    .GetSnapshot()
                ?? Array.Empty<BattleMatchQueuePresentationEntry>();
            int renderedCount = Math.Min(pending.Count, slotCount);
            for (int index = 0; index < renderedCount; index++)
            {
                BattleMatchQueuePresentationEntry entry = pending[index];
                if (entry == null
                    || !TryResolveElementSprite(
                        entry.Element,
                        out Sprite elementSprite))
                {
                    matchEventSlots[index]?.SetVisible(false);
                    continue;
                }

                matchEventSlots[index]?.Render(
                    elementSprite,
                    entry.RemovedBlockCount);
            }

            for (int index = renderedCount; index < slotCount; index++)
            {
                matchEventSlots[index]?.SetVisible(false);
            }
        }

        private void RefreshCombo()
        {
            if (comboText == null)
            {
                return;
            }

            BattleContext context = subscribedCoordinator?.Context;
            if (context == null || context.Result != BattleResultKind.None)
            {
                comboText.text = string.Empty;
                return;
            }

            MatchEventExecution execution =
                subscribedCoordinator.CurrentMatchEventExecution;
            if (execution != null)
            {
                displayedFinalComboCount = execution.FinalComboCount;
            }

            comboText.text = string.Concat(
                "Combo ",
                displayedFinalComboCount.ToString(
                    CultureInfo.InvariantCulture));
        }

        private bool TryResolveElementSprite(
            ElementType element,
            out Sprite sprite)
        {
            sprite = null;
            if (elementSpriteSet == null)
            {
                return false;
            }

            try
            {
                sprite = elementSpriteSet.GetSprite(element);
                return sprite != null;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private void RefreshScorePlaceholder()
        {
            if (scoreText != null)
            {
                scoreText.text = "Score --";
            }
        }

        private void RefreshStatusSnapshot()
        {
            if (subscribedBridge == null)
            {
                HideStatusSlots(bossStatusSlots);
                HideStatusSlots(partyStatusSlots);
                HideAllCharacterStatusSlots();
                RefreshBossImage();
                return;
            }

            RefreshBossStatuses();
            RenderEffects(
                partyStatusSlots,
                subscribedBridge.Party.Effects.GetActiveEffects());
            RefreshCharacterStatuses();
            RefreshBossImage();
        }

        private void RefreshBossStatuses()
        {
            IReadOnlyList<EffectInstance> effects =
                subscribedBridge.Boss.Effects.GetActiveEffects();
            int slotCount = bossStatusSlots?.Length ?? 0;
            int renderedCount = Math.Min(effects.Count, slotCount);
            for (int index = 0; index < renderedCount; index++)
            {
                RenderEffect(bossStatusSlots[index], effects[index]);
            }

            IReadOnlyList<ResourceState> resources =
                subscribedBridge.Boss.Resources.GetAll();
            for (int index = 0;
                index < resources.Count && renderedCount < slotCount;
                index++)
            {
                ResourceState resource = resources[index];
                if (resource.CurrentAmount <= 0)
                {
                    continue;
                }

                bossStatusSlots[renderedCount]?.Render(
                    ResolveResourceIcon(resource.ResourceId),
                    resource.CurrentAmount,
                    badgeVisible: true);
                renderedCount++;
            }

            HideStatusSlotsFrom(bossStatusSlots, renderedCount);
        }

        private void RefreshCharacterStatuses()
        {
            int slotCount = characterSlots?.Length ?? 0;
            for (int partySlotIndex = 0;
                partySlotIndex < slotCount;
                partySlotIndex++)
            {
                BattleCharacterSlotView slot =
                    characterSlots[partySlotIndex];
                CharacterBattleState character =
                    FindPartyCharacterAtSlot(partySlotIndex);
                if (slot == null || character == null)
                {
                    slot?.HideStatusSlots();
                    continue;
                }

                IReadOnlyList<EffectInstance> effects =
                    character.Effects.GetActiveEffects();
                int renderedCount = Math.Min(
                    effects.Count,
                    slot.StatusSlotCount);
                for (int index = 0; index < renderedCount; index++)
                {
                    RenderEffect(slot.GetStatusSlot(index), effects[index]);
                }

                for (int index = renderedCount;
                    index < slot.StatusSlotCount;
                    index++)
                {
                    slot.GetStatusSlot(index)?.SetVisible(false);
                }
            }
        }

        private void RenderEffects(
            BattleStatusIconView[] slots,
            IReadOnlyList<EffectInstance> effects)
        {
            int slotCount = slots?.Length ?? 0;
            int effectCount = effects?.Count ?? 0;
            int renderedCount = Math.Min(slotCount, effectCount);
            for (int index = 0; index < renderedCount; index++)
            {
                RenderEffect(slots[index], effects[index]);
            }

            HideStatusSlotsFrom(slots, renderedCount);
        }

        private void RenderEffect(
            BattleStatusIconView slot,
            EffectInstance effect)
        {
            if (slot == null || effect == null)
            {
                return;
            }

            bool badgeVisible;
            int badgeValue;
            if (effect.StackPolicy == EffectStackPolicy.StackCount)
            {
                badgeVisible = true;
                badgeValue = effect.StackCount;
            }
            else if (effect.RemainingTurns.HasValue)
            {
                badgeVisible = true;
                badgeValue = effect.RemainingTurns.Value;
            }
            else
            {
                badgeVisible = false;
                badgeValue = 0;
            }

            slot.Render(
                ResolveEffectIcon(effect.EffectId),
                badgeValue,
                badgeVisible);
        }

        private Sprite ResolveEffectIcon(string effectId)
        {
            DefinitionDatabase database =
                battleSceneCombatBootstrap?.DefinitionDatabase;
            if (database != null
                && database.IsInitialized
                && database.TryGetEffect(
                    effectId,
                    out EffectDefinition definition))
            {
                return definition.Icon;
            }

            return null;
        }

        private Sprite ResolveResourceIcon(string resourceId)
        {
            DefinitionDatabase database =
                battleSceneCombatBootstrap?.DefinitionDatabase;
            if (database != null
                && database.IsInitialized
                && database.TryGetResource(
                    resourceId,
                    out ResourceDefinition definition)
                && definition.Icon != null)
            {
                return definition.Icon;
            }

            return resourceFallbackIcon;
        }

        private void RefreshBossImage()
        {
            CacheDefaultBossSprite();
            if (bossImage == null)
            {
                return;
            }

            Sprite targetSprite = defaultBossSprite;
            BattleSceneCombatComposition composition =
                battleSceneCombatBootstrap?.CombatComposition;
            DefinitionDatabase database =
                battleSceneCombatBootstrap?.DefinitionDatabase;
            IBossVisualStateSource visualStateSource =
                battleSceneCombatBootstrap?.BossVisualStateSource;
            if (composition != null
                && ReferenceEquals(composition.Bridge, subscribedBridge)
                && visualStateSource != null
                && database != null
                && database.IsInitialized
                && database.TryGetBossPresentation(
                    subscribedBridge.Boss.BossId,
                    out BossPresentationDefinition presentation))
            {
                if (!presentation.TryGetStateSprite(
                    visualStateSource.CurrentVisualStateId,
                    out targetSprite))
                {
                    targetSprite = presentation.DefaultSprite
                        ?? defaultBossSprite;
                }
            }

            bossImage.sprite = targetSprite;
        }

        private void CacheDefaultBossSprite()
        {
            if (defaultBossSpriteCached)
            {
                return;
            }

            defaultBossSpriteCached = true;
            defaultBossSprite = bossImage != null ? bossImage.sprite : null;
        }

        private void HideAllCharacterStatusSlots()
        {
            int slotCount = characterSlots?.Length ?? 0;
            for (int index = 0; index < slotCount; index++)
            {
                characterSlots[index]?.HideStatusSlots();
            }
        }

        private static void HideStatusSlots(
            BattleStatusIconView[] slots)
        {
            HideStatusSlotsFrom(slots, 0);
        }

        private static void HideStatusSlotsFrom(
            BattleStatusIconView[] slots,
            int startIndex)
        {
            int slotCount = slots?.Length ?? 0;
            for (int index = Math.Max(0, startIndex);
                index < slotCount;
                index++)
            {
                slots[index]?.SetVisible(false);
            }
        }

        private void RefreshActiveAvailability()
        {
            if (characterSlots == null)
            {
                return;
            }

            for (int index = 0; index < characterSlots.Length; index++)
            {
                BattleCharacterSlotView slot = characterSlots[index];
                if (slot == null)
                {
                    continue;
                }

                CharacterBattleState character = IsRuntimeConnected
                    ? FindPartyCharacterAtSlot(index)
                    : null;
                if (character == null)
                {
                    slot.RenderEmpty();
                    continue;
                }

                int remainingCooldown = 0;
                bool available = false;
                if (TryResolveActiveBinding(
                        index,
                        out ActiveAbilityBinding binding))
                {
                    int activeIndex = binding.ActiveAbilityIndex;
                    BattleContext context = battleFlowController.Context;
                    if (activeIndex >= 0
                        && activeIndex < context.ActiveAbilities.Count)
                    {
                        remainingCooldown = context
                            .ActiveAbilities[activeIndex]
                            .RemainingCooldown;
                        available = battleFlowController.CanUseActive(
                            activeIndex);
                    }
                }

                Sprite elementSprite = elementSpriteSet != null
                    ? elementSpriteSet.GetSprite(character.Element)
                    : null;
                slot.RenderCharacter(
                    character.CharacterId,
                    elementSprite,
                    remainingCooldown,
                    available);
            }
        }

        private static void SetSlotsVisible(
            BattleBossIntentSlotView[] slots,
            bool visible)
        {
            if (slots == null)
            {
                return;
            }

            for (int index = 0; index < slots.Length; index++)
            {
                slots[index]?.SetVisible(visible);
            }
        }

        private static void SetSlotsVisible(
            BattleStatusIconView[] slots,
            bool visible)
        {
            if (slots == null)
            {
                return;
            }

            for (int index = 0; index < slots.Length; index++)
            {
                slots[index]?.SetVisible(visible);
            }
        }

        private static void SetSlotsVisible(
            BattleMatchEventSlotView[] slots,
            bool visible)
        {
            if (slots == null)
            {
                return;
            }

            for (int index = 0; index < slots.Length; index++)
            {
                slots[index]?.SetVisible(visible);
            }
        }

        private static void ClearText(TMP_Text text)
        {
            if (text != null)
            {
                text.text = string.Empty;
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        private static void SetNormalizedHpSlider(
            Slider slider,
            long currentHp,
            long maxHp)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.SetValueWithoutNotify(CalculateRatio(currentHp, maxHp));
        }

        private static void SetHpText(
            TMP_Text text,
            long currentHp,
            long maxHp,
            long shield)
        {
            if (text == null)
            {
                return;
            }

            string current = currentHp.ToString(
                CultureInfo.InvariantCulture);
            string maximum = maxHp.ToString(
                CultureInfo.InvariantCulture);
            text.text = shield > 0
                ? string.Concat(
                    current,
                    " (+",
                    shield.ToString(CultureInfo.InvariantCulture),
                    ") / ",
                    maximum)
                : string.Concat(current, " / ", maximum);
        }

        private void SetShieldOverlay(
            Image shieldImage,
            long shield,
            long maxHp)
        {
            if (shieldImage == null)
            {
                return;
            }

            float ratio = CalculateRatio(shield, maxHp);
            if (shieldImage.sprite != null)
            {
                RestorePartyShieldLayout(shieldImage.rectTransform);
                shieldImage.type = Image.Type.Filled;
                shieldImage.fillMethod = Image.FillMethod.Horizontal;
                shieldImage.fillOrigin =
                    (int)Image.OriginHorizontal.Left;
                shieldImage.fillClockwise = true;
                shieldImage.fillAmount = ratio;
            }
            else
            {
                shieldImage.type = Image.Type.Simple;
                SetPartyShieldWidth(shieldImage.rectTransform, ratio);
            }

            SetImageVisible(shieldImage, shield > 0 && ratio > 0f);
        }

        private void CachePartyShieldLayout()
        {
            if (partyShieldLayoutCached || partyShieldImage == null)
            {
                return;
            }

            RectTransform rectTransform = partyShieldImage.rectTransform;
            partyShieldFullWidth = rectTransform.sizeDelta.x;
            if (partyShieldFullWidth <= 0f)
            {
                partyShieldFullWidth = rectTransform.rect.width;
            }

            partyShieldFullPositionX = rectTransform.anchoredPosition.x;
            partyShieldPivotX = rectTransform.pivot.x;
            partyShieldLayoutCached = true;
        }

        private void RestorePartyShieldLayout(RectTransform rectTransform)
        {
            if (!partyShieldLayoutCached)
            {
                CachePartyShieldLayout();
            }

            Vector2 size = rectTransform.sizeDelta;
            size.x = partyShieldFullWidth;
            rectTransform.sizeDelta = size;
            Vector2 position = rectTransform.anchoredPosition;
            position.x = partyShieldFullPositionX;
            rectTransform.anchoredPosition = position;
        }

        private void SetPartyShieldWidth(
            RectTransform rectTransform,
            float ratio)
        {
            if (!partyShieldLayoutCached)
            {
                CachePartyShieldLayout();
            }

            float width = partyShieldFullWidth * ratio;
            float fullLeft = partyShieldFullPositionX
                - (partyShieldFullWidth * partyShieldPivotX);
            Vector2 size = rectTransform.sizeDelta;
            size.x = width;
            rectTransform.sizeDelta = size;
            Vector2 position = rectTransform.anchoredPosition;
            position.x = fullLeft + (width * partyShieldPivotX);
            rectTransform.anchoredPosition = position;
        }

        private static float CalculateRatio(long value, long maximum)
        {
            if (maximum <= 0)
            {
                return 0f;
            }

            double ratio = (double)value / maximum;
            ratio = Math.Max(0d, Math.Min(1d, ratio));
            return (float)ratio;
        }

        private static void SetImageVisible(Image image, bool visible)
        {
            if (image != null)
            {
                image.gameObject.SetActive(visible);
            }
        }
    }
}
