using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Core.Logging;

namespace ValorChronicle.Battle.Flow.Presentation
{
    public sealed class BattleFlowController : MonoBehaviour
    {
        [SerializeField]
        private BattleBoardController boardController = null;

        [SerializeField]
        private BattleCombatPresentationController
            combatPresentationController = null;

        [SerializeField]
        private GameObject notUsersTurnPanel = null;

        [SerializeField]
        private bool requireCombatBridge;

        [SerializeField]
        [Min(0f)]
        private float preAttackDelaySeconds = 0.20f;

        [SerializeField]
        [Min(0f)]
        private float emptyQueueItemDelaySeconds = 0.12f;

        [SerializeField]
        [Min(0f)]
        private float preBossActionDelaySeconds = 0.65f;

        private bool connectionEnabled;
        private bool boardEventsSubscribed;
        private bool coordinatorEventsSubscribed;
        private bool isCleaningUp;
        private bool isAdvancingFlow;
        private bool isExecutingFlowStep;
        private bool isResolvingActivePresentation;
        private bool isInitialBoardPresentationPending;
        private long waitingActionId;
        private long lastAcceptedActionId;
        private int flowVersion;
        private Coroutine flowCoroutine;
        private Coroutine activePresentationCoroutine;
        private BattleFlowSetup setup;
        private BattleFlowCoordinator coordinator;
        private BattleFlowCombatBridge combatBridge;
        private IMatchDamageProjectilePresenter projectilePresenterOverride =
            null;
        private IActiveDamageProjectilePresenter
            activeProjectilePresenterOverride = null;
        private IBossDamageProjectilePresenter
            bossProjectilePresenterOverride = null;
        private readonly BattleMatchQueuePresentationState
            matchQueuePresentation =
                new BattleMatchQueuePresentationState();

        public event Action PresentationMatchQueueChanged;
        public event Action ActivePresentationStateChanged;

        public BattleFlowSetup Setup => setup;
        public BattleFlowCoordinator Coordinator => coordinator;
        public BattleContext Context => coordinator?.Context;
        public BattleFlowCombatBridge CombatBridge => combatBridge;
        public BattleCombatPresentationController CombatPresentationController =>
            combatPresentationController;
        public GameObject NotUsersTurnPanel => notUsersTurnPanel;
        public IBattleBoardMutationSink BoardMutationSink => boardController;
        public bool RequiresCombatBridge => requireCombatBridge;
        public BattleMatchQueuePresentationState MatchQueuePresentation =>
            matchQueuePresentation;
        public bool IsResolvingActivePresentation =>
            isResolvingActivePresentation;

        private void OnEnable()
        {
            connectionEnabled = true;
            isCleaningUp = false;
            UpdateInitialBoardPresentationState();
            SetBoardInputGate(false);
            SubscribeBoardEvents();
            SubscribeCoordinatorEvents();
            TryStartBattleFromReadyBoard();
        }

        private void OnDisable()
        {
            isCleaningUp = true;
            connectionEnabled = false;
            SetBoardInputGate(false);
            UnsubscribeBoardEvents();
            StopFlowProgression();
            ClearPresentationMatchQueue();

            if (coordinator != null
                && coordinator.Context.Result == BattleResultKind.None)
            {
                coordinator.AbortBattle();
            }

            UnsubscribeCoordinatorEvents();
            waitingActionId = 0;
        }

        private void OnDestroy()
        {
            ClearPresentationMatchQueue();
        }

        public void Initialize(BattleFlowSetup battleSetup)
        {
            Initialize(battleSetup, null);
        }

        public void Initialize(
            BattleFlowSetup battleSetup,
            Func<BattleFlowCoordinator, BattleFlowCombatBridge>
                combatBridgeFactory)
        {
            if (battleSetup == null)
            {
                throw new ArgumentNullException(nameof(battleSetup));
            }

            if (coordinator != null)
            {
                throw new InvalidOperationException(
                    "BattleFlowController is already initialized.");
            }

            if (boardController == null)
            {
                throw new InvalidOperationException(
                    "A BattleBoardController must be assigned.");
            }

            if (requireCombatBridge && combatBridgeFactory == null)
            {
                throw new InvalidOperationException(
                    "This BattleFlowController requires a combat bridge.");
            }

            var createdCoordinator = new BattleFlowCoordinator(
                battleSetup.TurnLimit,
                battleSetup.ActiveAbilityCooldowns);
            BattleFlowCombatBridge createdBridge = null;
            if (combatBridgeFactory != null)
            {
                createdBridge = combatBridgeFactory(createdCoordinator)
                    ?? throw new InvalidOperationException(
                        "Combat bridge factory returned null.");
                if (!ReferenceEquals(
                        createdBridge.Coordinator,
                        createdCoordinator))
                {
                    throw new InvalidOperationException(
                        "Combat bridge factory must use the supplied "
                            + "coordinator.");
                }
            }

            setup = battleSetup;
            coordinator = createdCoordinator;
            combatBridge = createdBridge;
            UpdateInitialBoardPresentationState();
            SetBoardInputGate(false);
            if (connectionEnabled)
            {
                SubscribeBoardEvents();
                SubscribeCoordinatorEvents();
                TryStartBattleFromReadyBoard();
            }
        }

        public void AttachCombatBridge(BattleFlowCombatBridge bridge)
        {
            if (bridge == null)
            {
                throw new ArgumentNullException(nameof(bridge));
            }

            if (coordinator == null)
            {
                throw new InvalidOperationException(
                    "BattleFlowController must be initialized first.");
            }

            if (!ReferenceEquals(bridge.Coordinator, coordinator))
            {
                throw new InvalidOperationException(
                    "Combat bridge must use this controller's coordinator.");
            }

            if (combatBridge != null || isAdvancingFlow)
            {
                throw new InvalidOperationException(
                    "Combat bridge cannot be replaced during battle flow.");
            }

            combatBridge = bridge;
        }

        public bool TryUseActive(int activeIndex)
        {
            if (coordinator == null
                || waitingActionId != 0
                || isResolvingActivePresentation)
            {
                return false;
            }

            if (combatBridge == null)
            {
                return coordinator.TryUseActiveAbility(activeIndex);
            }

            if (!combatBridge.TryBeginActiveCombat(activeIndex))
            {
                return false;
            }

            SetActivePresentationResolving(true);
            int version = flowVersion;
            Coroutine started = StartCoroutine(
                AdvanceActiveCombat(version));
            if (isResolvingActivePresentation
                && flowVersion == version)
            {
                activePresentationCoroutine = started;
            }

            return true;
        }

        public bool CanUseActive(int activeIndex)
        {
            if (coordinator == null
                || waitingActionId != 0
                || isResolvingActivePresentation)
            {
                return false;
            }

            return combatBridge != null
                ? combatBridge.CanUseActive(activeIndex)
                : coordinator.CanUseActiveAbility(activeIndex);
        }

        private IEnumerator AdvanceActiveCombat(int version)
        {
            try
            {
                while (IsActivePresentationCurrent(version)
                    && combatBridge.TryGetNextActiveCombatAction(
                        out CharacterBattleState actingCharacter,
                        out CombatAction nextAction))
                {
                    if (nextAction is DamageAction damageAction)
                    {
                        var request = new ActiveDamageProjectileRequest(
                            actingCharacter.PartySlotIndex,
                            actingCharacter.CharacterId,
                            damageAction.ContextRequest.AttackElement);
                        bool presentationCompleted = false;
                        ActiveDamageProjectileCompletion completion =
                            ActiveDamageProjectileCompletion.Cancelled;
                        IActiveDamageProjectilePresenter presenter =
                            activeProjectilePresenterOverride
                            ?? combatPresentationController;
                        bool presentationStarted = false;
                        if (presenter != null)
                        {
                            try
                            {
                                presentationStarted = presenter.TryPresent(
                                    request,
                                    result =>
                                    {
                                        completion = result;
                                        presentationCompleted = true;
                                    });
                            }
                            catch (Exception exception)
                            {
                                GameLogger.Exception(exception, this);
                                GameLogger.Warning(
                                    "[BattleFlow] Active damage projectile "
                                        + "failed to start; applying "
                                        + "immediately.",
                                    this);
                            }
                        }
                        else
                        {
                            GameLogger.Warning(
                                "[BattleFlow] Active damage projectile "
                                    + "presenter is unavailable; applying "
                                    + "immediately.",
                                this);
                        }

                        if (presentationStarted)
                        {
                            while (!presentationCompleted)
                            {
                                if (!IsActivePresentationCurrent(version))
                                {
                                    presenter.CancelActive();
                                    yield break;
                                }

                                yield return null;
                            }

                            if (!IsActivePresentationCurrent(version))
                            {
                                yield break;
                            }

                            if (completion
                                != ActiveDamageProjectileCompletion.Arrived)
                            {
                                GameLogger.Error(
                                    "[BattleFlow] Active damage projectile "
                                        + "was interrupted before combat "
                                        + "application.",
                                    this);
                                AbortBattle();
                                yield break;
                            }
                        }
                    }

                    bool actionApplied;
                    isExecutingFlowStep = true;
                    try
                    {
                        actionApplied = combatBridge
                            .TryApplyNextActiveCombatAction(out _);
                    }
                    finally
                    {
                        isExecutingFlowStep = false;
                    }

                    if (!actionApplied)
                    {
                        GameLogger.Error(
                            "[BattleFlow] Active combat action could not "
                                + "be applied.",
                            this);
                        AbortBattle();
                        yield break;
                    }
                }

                if (!IsActivePresentationCurrent(version))
                {
                    yield break;
                }

                bool completed;
                isExecutingFlowStep = true;
                try
                {
                    completed = combatBridge.TryCompleteActiveCombat();
                }
                finally
                {
                    isExecutingFlowStep = false;
                }

                if (!completed)
                {
                    GameLogger.Error(
                        "[BattleFlow] Active combat could not be "
                            + "completed.",
                        this);
                    AbortBattle();
                }
            }
            finally
            {
                if (flowVersion == version)
                {
                    activePresentationCoroutine = null;
                    SetActivePresentationResolving(false);
                }
            }
        }

        public bool NotifyBossDefeated()
        {
            return EndBattle(coordinator?.NotifyBossDefeated() ?? false);
        }

        public bool NotifyPartyDefeated()
        {
            return EndBattle(
                coordinator?.NotifyPartyIncapacitated() ?? false);
        }

        public bool AbortBattle()
        {
            return EndBattle(coordinator?.AbortBattle() ?? false);
        }

        private void SubscribeBoardEvents()
        {
            if (!connectionEnabled
                || boardEventsSubscribed
                || boardController == null)
            {
                return;
            }

            boardController.InitialBoardReady += HandleInitialBoardReady;
            boardController.BoardActionStarted += HandleBoardActionStarted;
            boardController.CascadeStepPresented +=
                HandleCascadeStepPresented;
            boardController.BoardActionFinished += HandleBoardActionFinished;
            boardEventsSubscribed = true;
        }

        private void UnsubscribeBoardEvents()
        {
            if (!boardEventsSubscribed || boardController == null)
            {
                return;
            }

            boardController.InitialBoardReady -= HandleInitialBoardReady;
            boardController.BoardActionStarted -= HandleBoardActionStarted;
            boardController.CascadeStepPresented -=
                HandleCascadeStepPresented;
            boardController.BoardActionFinished -= HandleBoardActionFinished;
            boardEventsSubscribed = false;
        }

        private void SubscribeCoordinatorEvents()
        {
            if (!connectionEnabled
                || coordinatorEventsSubscribed
                || coordinator == null)
            {
                return;
            }

            coordinator.PhaseChanged += HandlePhaseChanged;
            coordinator.MatchEventExecuting += HandleMatchEventExecuting;
            coordinator.BossActionStarted += HandleBossActionStarted;
            coordinator.ResultReached += HandleResultReached;
            coordinatorEventsSubscribed = true;
        }

        private void UnsubscribeCoordinatorEvents()
        {
            if (!coordinatorEventsSubscribed || coordinator == null)
            {
                return;
            }

            coordinator.PhaseChanged -= HandlePhaseChanged;
            coordinator.MatchEventExecuting -= HandleMatchEventExecuting;
            coordinator.BossActionStarted -= HandleBossActionStarted;
            coordinator.ResultReached -= HandleResultReached;
            coordinatorEventsSubscribed = false;
        }

        private void HandleInitialBoardReady()
        {
            UpdateInitialBoardPresentationState();
            TryStartBattleFromReadyBoard();
        }

        private void TryStartBattleFromReadyBoard()
        {
            UpdateInitialBoardPresentationState();
            if (requireCombatBridge
                && coordinator != null
                && combatBridge == null)
            {
                GameLogger.Error(
                    "[BattleFlow] Required combat bridge is not attached.",
                    this);
                SetBoardInputGate(false);
                return;
            }

            if (!connectionEnabled
                || coordinator == null
                || boardController == null
                || !boardController.HasInitialBoardReady
                || coordinator.Context.Phase != BattlePhase.NotStarted
                || coordinator.Context.Result != BattleResultKind.None)
            {
                UpdateBoardInputGate();
                return;
            }

            if (coordinator.StartBattle())
            {
                GameLogger.Log(
                    $"[BattleFlow] Battle started. " +
                    $"TurnLimit={coordinator.Context.TurnLimit}.",
                    this);
            }

            UpdateBoardInputGate();
        }

        private void HandleBoardActionStarted(BoardActionExecution execution)
        {
            if (!CanHandleBoardCallback() || execution == null)
            {
                return;
            }

            if (execution.ActionId <= lastAcceptedActionId
                || waitingActionId != 0)
            {
                GameLogger.Warning(
                    $"[BattleFlow] Ignored duplicate or stale board action. " +
                    $"ActionId={execution.ActionId}; " +
                    $"WaitingActionId={waitingActionId}.",
                    this);
                return;
            }

            if (coordinator.Context.Phase != BattlePhase.PlayerInput)
            {
                GameLogger.Error(
                    $"[BattleFlow] Board action started in an invalid phase. " +
                    $"ActionId={execution.ActionId}; " +
                    $"Phase={coordinator.Context.Phase}.",
                    this);
                UpdateBoardInputGate();
                return;
            }

            waitingActionId = execution.ActionId;
            lastAcceptedActionId = execution.ActionId;
            if (!execution.Result.ConsumesTurn)
            {
                UpdateBoardInputGate();
                return;
            }

            BeginPresentationMatchQueue(execution.ActionId);

            if (!coordinator.TryBeginBoardResolution())
            {
                GameLogger.Error(
                    $"[BattleFlow] Coordinator rejected board action start. " +
                    $"ActionId={execution.ActionId}.",
                    this);
                ClearPresentationMatchQueue();
                UpdateBoardInputGate();
                return;
            }

            UpdateBoardInputGate();
        }

        private void HandleCascadeStepPresented(
            BoardCascadeStepPresentation presentation)
        {
            if (!CanHandleBoardCallback() || presentation == null)
            {
                return;
            }

            if (!matchQueuePresentation.TryAppend(presentation))
            {
                GameLogger.Warning(
                    "[BattleFlow] Ignored stale, duplicate, or out-of-order "
                        + "cascade presentation. "
                        + $"ActionId={presentation.ActionId}; "
                        + $"StepIndex={presentation.CascadeStepIndex}.",
                    this);
                return;
            }

            PublishPresentationMatchQueueChanged();
        }

        private void HandleBoardActionFinished(
            BoardActionCompletion completion)
        {
            if (!CanHandleBoardCallback() || completion == null)
            {
                return;
            }

            if (waitingActionId == 0
                || completion.ActionId != waitingActionId)
            {
                GameLogger.Warning(
                    $"[BattleFlow] Ignored unexpected board completion. " +
                    $"ActionId={completion.ActionId}; " +
                    $"WaitingActionId={waitingActionId}.",
                    this);
                return;
            }

            waitingActionId = 0;
            switch (completion.CompletionStatus)
            {
                case BoardActionCompletionStatus.Completed:
                    HandleCompletedBoardAction(completion);
                    break;

                case BoardActionCompletionStatus.Failed:
                    ClearPresentationMatchQueue();
                    HandleFailedBoardAction(completion);
                    break;

                case BoardActionCompletionStatus.Interrupted:
                    ClearPresentationMatchQueue();
                    AbortForInterruptedBoardAction(completion.ActionId);
                    break;

                default:
                    GameLogger.Error(
                        $"[BattleFlow] Unknown board completion status. " +
                        $"ActionId={completion.ActionId}; " +
                        $"Status={completion.CompletionStatus}.",
                        this);
                    AbortBattle();
                    break;
            }
        }

        private void HandleCompletedBoardAction(
            BoardActionCompletion completion)
        {
            BoardSwapActionResult result = completion.Result;
            if (result == null)
            {
                GameLogger.Error(
                    $"[BattleFlow] Completed board action has no result. " +
                    $"ActionId={completion.ActionId}.",
                    this);
                AbortBattle();
                return;
            }

            if (!result.ConsumesTurn)
            {
                UpdateBoardInputGate();
                return;
            }

            BoardCascadeResult cascade = result.Cascade;
            if (cascade == null)
            {
                GameLogger.Error(
                    $"[BattleFlow] Consuming board action has no cascade. " +
                    $"ActionId={completion.ActionId}.",
                    this);
                AbortBattle();
                return;
            }

            bool resolved;
            try
            {
                resolved = coordinator.NotifyBoardActionResolved(
                    cascade,
                    consumesTurn: true);
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    $"[BattleFlow] Board action result was rejected. " +
                    $"ActionId={completion.ActionId}.",
                    this);
                AbortBattle();
                return;
            }

            if (!resolved)
            {
                GameLogger.Error(
                    $"[BattleFlow] Board action completed in an invalid " +
                    $"phase. ActionId={completion.ActionId}; " +
                    $"Phase={coordinator.Context.Phase}.",
                    this);
                AbortBattle();
                return;
            }

            UpdateBoardInputGate();
            StartFlowProgression();
        }

        private void HandleFailedBoardAction(
            BoardActionCompletion completion)
        {
            if (completion.Failure != null)
            {
                GameLogger.Exception(completion.Failure, this);
            }

            GameLogger.Error(
                $"[BattleFlow] Board presentation failed. " +
                $"ActionId={completion.ActionId}.",
                this);
            AbortBattle();
        }

        private void AbortForInterruptedBoardAction(long actionId)
        {
            if (isCleaningUp
                || coordinator == null
                || coordinator.Context.Result != BattleResultKind.None)
            {
                return;
            }

            GameLogger.Warning(
                $"[BattleFlow] Board presentation was interrupted. " +
                $"ActionId={actionId}.",
                this);
            AbortBattle();
        }

        private void StartFlowProgression()
        {
            if (isAdvancingFlow
                || !connectionEnabled
                || coordinator == null
                || coordinator.Context.Result != BattleResultKind.None
                || coordinator.Context.Phase
                    != BattlePhase.MatchEventResolving)
            {
                return;
            }

            int version = ++flowVersion;
            isAdvancingFlow = true;
            Coroutine startedCoroutine = StartCoroutine(AdvanceFlow(version));
            if (isAdvancingFlow && IsFlowProgressionCurrent(version))
            {
                flowCoroutine = startedCoroutine;
            }
        }

        private IEnumerator AdvanceFlow(int version)
        {
            try
            {
                if (preAttackDelaySeconds > 0f)
                {
                    yield return new WaitForSeconds(
                        preAttackDelaySeconds);
                }

                if (!IsFlowProgressionCurrent(
                    version,
                    BattlePhase.MatchEventResolving))
                {
                    yield break;
                }

                while (IsFlowProgressionCurrent(
                    version,
                    BattlePhase.MatchEventResolving))
                {
                    MatchEventExecution execution;
                    isExecutingFlowStep = true;
                    try
                    {
                        coordinator.TryBeginNextMatchEvent(out execution);
                    }
                    finally
                    {
                        isExecutingFlowStep = false;
                    }

                    UpdateBoardInputGate();
                    if (!IsFlowProgressionCurrent(version))
                    {
                        yield break;
                    }

                    if (execution != null)
                    {
                        bool matchCompleted;
                        if (combatBridge != null)
                        {
                            bool combatBegan;
                            bool hasMatchingCharacter;
                            isExecutingFlowStep = true;
                            try
                            {
                                combatBegan = combatBridge
                                    .TryBeginMatchEventCombat(
                                        execution,
                                        out hasMatchingCharacter);
                            }
                            finally
                            {
                                isExecutingFlowStep = false;
                            }

                            if (!combatBegan)
                            {
                                LogMatchCompletionFailure(execution);
                                yield break;
                            }

                            if (!hasMatchingCharacter
                                && emptyQueueItemDelaySeconds > 0f)
                            {
                                yield return new WaitForSeconds(
                                    emptyQueueItemDelaySeconds);
                                if (!IsFlowProgressionCurrent(
                                    version,
                                    BattlePhase.MatchEventResolving))
                                {
                                    yield break;
                                }
                            }

                            while (combatBridge.TryGetNextMatchCombatAction(
                                out CharacterBattleState actingCharacter,
                                out CombatAction nextAction))
                            {
                                if (nextAction is DamageAction damageAction)
                                {
                                    var request =
                                        new MatchDamageProjectileRequest(
                                            actingCharacter.PartySlotIndex,
                                            actingCharacter.CharacterId,
                                            damageAction.ContextRequest
                                                .AttackElement);
                                    bool presentationCompleted = false;
                                    MatchDamageProjectileCompletion completion =
                                        MatchDamageProjectileCompletion
                                            .Cancelled;
                                    IMatchDamageProjectilePresenter presenter =
                                        projectilePresenterOverride
                                        ?? combatPresentationController;
                                    bool presentationStarted = false;
                                    if (presenter != null)
                                    {
                                        try
                                        {
                                            presentationStarted =
                                                presenter.TryPresent(
                                                    request,
                                                    result =>
                                                    {
                                                        completion = result;
                                                        presentationCompleted =
                                                            true;
                                                    });
                                        }
                                        catch (Exception exception)
                                        {
                                            GameLogger.Exception(
                                                exception,
                                                this);
                                            GameLogger.Warning(
                                                "[BattleFlow] Player damage "
                                                    + "projectile failed to "
                                                    + "start; applying "
                                                    + "immediately.",
                                                this);
                                        }
                                    }
                                    else
                                    {
                                        GameLogger.Warning(
                                            "[BattleFlow] Player damage "
                                                + "projectile presenter is "
                                                + "unavailable; applying "
                                                + "immediately.",
                                            this);
                                    }

                                    if (presentationStarted)
                                    {
                                        while (!presentationCompleted)
                                        {
                                            if (!IsFlowProgressionCurrent(
                                                version,
                                                BattlePhase
                                                    .MatchEventResolving))
                                            {
                                                presenter.CancelActive();
                                                yield break;
                                            }

                                            yield return null;
                                        }

                                        if (!IsFlowProgressionCurrent(
                                            version,
                                            BattlePhase.MatchEventResolving))
                                        {
                                            yield break;
                                        }

                                        if (completion !=
                                            MatchDamageProjectileCompletion
                                                .Arrived)
                                        {
                                            GameLogger.Error(
                                                "[BattleFlow] Player damage "
                                                    + "projectile was "
                                                    + "interrupted before "
                                                    + "combat application.",
                                                this);
                                            AbortBattle();
                                            yield break;
                                        }
                                    }
                                }

                                bool actionApplied;
                                isExecutingFlowStep = true;
                                try
                                {
                                    actionApplied = combatBridge
                                        .TryApplyNextMatchCombatAction(
                                            out _);
                                }
                                finally
                                {
                                    isExecutingFlowStep = false;
                                }

                                if (!actionApplied)
                                {
                                    LogMatchCompletionFailure(execution);
                                    yield break;
                                }

                                if (!IsFlowProgressionCurrent(
                                    version,
                                    BattlePhase.MatchEventResolving))
                                {
                                    yield break;
                                }
                            }

                            if (ShouldDelayBeforeBossTransition())
                            {
                                yield return new WaitForSeconds(
                                    preBossActionDelaySeconds);
                                if (!IsFlowProgressionCurrent(
                                    version,
                                    BattlePhase.MatchEventResolving))
                                {
                                    yield break;
                                }
                            }

                            isExecutingFlowStep = true;
                            try
                            {
                                matchCompleted = combatBridge
                                    .TryCompleteMatchEventCombat(execution);
                            }
                            finally
                            {
                                isExecutingFlowStep = false;
                            }
                        }
                        else
                        {
                            yield return null;
                            if (!IsFlowProgressionCurrent(version))
                            {
                                yield break;
                            }

                            if (ShouldDelayBeforeBossTransition())
                            {
                                yield return new WaitForSeconds(
                                    preBossActionDelaySeconds);
                                if (!IsFlowProgressionCurrent(
                                    version,
                                    BattlePhase.MatchEventResolving))
                                {
                                    yield break;
                                }
                            }

                            isExecutingFlowStep = true;
                            try
                            {
                                matchCompleted = coordinator
                                    .CompleteCurrentMatchEvent(
                                    execution.ExecutionId);
                            }
                            finally
                            {
                                isExecutingFlowStep = false;
                            }
                        }

                        if (!matchCompleted)
                        {
                            LogMatchCompletionFailure(execution);
                            yield break;
                        }

                        UpdateBoardInputGate();
                        if (!IsFlowProgressionCurrent(version))
                        {
                            yield break;
                        }
                    }

                    if (coordinator.Context.Phase == BattlePhase.BossActing)
                    {
                        break;
                    }

                    if (execution == null)
                    {
                        yield break;
                    }
                }

                if (!IsFlowProgressionCurrent(
                    version,
                    BattlePhase.BossActing))
                {
                    yield break;
                }

                yield return null;

                if (!IsFlowProgressionCurrent(
                    version,
                    BattlePhase.BossActing))
                {
                    yield break;
                }

                bool completed;
                if (combatBridge == null)
                {
                    isExecutingFlowStep = true;
                    try
                    {
                        completed = coordinator.CompleteBossAction();
                    }
                    finally
                    {
                        isExecutingFlowStep = false;
                    }
                }
                else
                {
                    bool bossCombatBegan;
                    isExecutingFlowStep = true;
                    try
                    {
                        bossCombatBegan = combatBridge
                            .TryBeginBossActionCombat(
                                coordinator.Context.CurrentTurn);
                    }
                    finally
                    {
                        isExecutingFlowStep = false;
                    }

                    completed = bossCombatBegan;
                    while (completed
                        && combatBridge.TryGetNextBossCombatAction(
                            out CombatAction nextBossAction))
                    {
                        if (nextBossAction is BossDamageAction)
                        {
                            var request = new BossDamageProjectileRequest(
                                coordinator.Context.CurrentTurn,
                                nextBossAction.ActionId);
                            bool presentationCompleted = false;
                            BossDamageProjectileCompletion completion =
                                BossDamageProjectileCompletion.Cancelled;
                            IBossDamageProjectilePresenter presenter =
                                bossProjectilePresenterOverride
                                ?? combatPresentationController;
                            bool presentationStarted = false;
                            if (presenter != null)
                            {
                                try
                                {
                                    presentationStarted = presenter.TryPresent(
                                        request,
                                        result =>
                                        {
                                            completion = result;
                                            presentationCompleted = true;
                                        });
                                }
                                catch (Exception exception)
                                {
                                    GameLogger.Exception(exception, this);
                                    GameLogger.Warning(
                                        "[BattleFlow] Boss damage "
                                            + "projectile failed to start; "
                                            + "applying immediately.",
                                        this);
                                }
                            }
                            else
                            {
                                GameLogger.Warning(
                                    "[BattleFlow] Boss damage projectile "
                                        + "presenter is unavailable; "
                                        + "applying immediately.",
                                    this);
                            }

                            if (presentationStarted)
                            {
                                while (!presentationCompleted)
                                {
                                    if (!IsFlowProgressionCurrent(
                                        version,
                                        BattlePhase.BossActing))
                                    {
                                        presenter.CancelActive();
                                        yield break;
                                    }

                                    yield return null;
                                }

                                if (!IsFlowProgressionCurrent(
                                    version,
                                    BattlePhase.BossActing))
                                {
                                    yield break;
                                }

                                if (completion !=
                                    BossDamageProjectileCompletion.Arrived)
                                {
                                    GameLogger.Error(
                                        "[BattleFlow] Boss damage "
                                            + "projectile was interrupted "
                                            + "before combat application.",
                                        this);
                                    AbortBattle();
                                    yield break;
                                }
                            }
                        }

                        isExecutingFlowStep = true;
                        try
                        {
                            completed = combatBridge
                                .TryApplyNextBossCombatAction(out _);
                        }
                        finally
                        {
                            isExecutingFlowStep = false;
                        }
                    }

                    if (completed)
                    {
                        isExecutingFlowStep = true;
                        try
                        {
                            completed = combatBridge
                                .TryCompleteBossActionCombat();
                        }
                        finally
                        {
                            isExecutingFlowStep = false;
                        }
                    }
                }

                if (!completed)
                {
                    GameLogger.Error(
                        $"[BattleFlow] Boss action could not " +
                        $"be completed. " +
                        $"Turn={coordinator.Context.CurrentTurn}; " +
                        $"Phase={coordinator.Context.Phase}.",
                        this);
                }

                UpdateBoardInputGate();
            }
            finally
            {
                isExecutingFlowStep = false;
                if (flowVersion == version)
                {
                    isAdvancingFlow = false;
                    flowCoroutine = null;
                }
            }
        }

        private bool IsFlowProgressionCurrent(int version)
        {
            return connectionEnabled
                && flowVersion == version
                && coordinator != null
                && coordinator.Context.Result == BattleResultKind.None;
        }

        private bool IsFlowProgressionCurrent(
            int version,
            BattlePhase phase)
        {
            return IsFlowProgressionCurrent(version)
                && coordinator.Context.Phase == phase;
        }

        private bool IsActivePresentationCurrent(int version)
        {
            return connectionEnabled
                && flowVersion == version
                && isResolvingActivePresentation
                && coordinator != null
                && coordinator.Context.Result == BattleResultKind.None
                && coordinator.Context.Phase == BattlePhase.PlayerInput;
        }

        private void StopFlowProgression()
        {
            flowVersion++;
            IMatchDamageProjectilePresenter matchPresenter =
                projectilePresenterOverride ?? combatPresentationController;
            IActiveDamageProjectilePresenter activePresenter =
                activeProjectilePresenterOverride
                ?? combatPresentationController;
            IBossDamageProjectilePresenter bossPresenter =
                bossProjectilePresenterOverride ?? combatPresentationController;
            matchPresenter?.CancelActive();
            if (!ReferenceEquals(matchPresenter, activePresenter))
            {
                activePresenter?.CancelActive();
            }

            if (!ReferenceEquals(matchPresenter, bossPresenter)
                && !ReferenceEquals(activePresenter, bossPresenter))
            {
                bossPresenter?.CancelActive();
            }

            combatBridge?.CancelMatchEventCombat();
            combatBridge?.CancelActiveCombat();
            combatBridge?.CancelBossActionCombat();
            Coroutine activeFlowCoroutine = flowCoroutine;
            Coroutine activeCombatCoroutine =
                activePresentationCoroutine;
            flowCoroutine = null;
            activePresentationCoroutine = null;
            isAdvancingFlow = false;
            SetActivePresentationResolving(false);
            if (activeFlowCoroutine != null && !isExecutingFlowStep)
            {
                StopCoroutine(activeFlowCoroutine);
            }

            if (activeCombatCoroutine != null && !isExecutingFlowStep)
            {
                StopCoroutine(activeCombatCoroutine);
            }
        }

        private void LogMatchCompletionFailure(
            MatchEventExecution execution)
        {
            GameLogger.Error(
                $"[BattleFlow] MatchEvent could not be completed. " +
                $"ExecutionId={execution.ExecutionId}; " +
                $"Turn={coordinator.Context.CurrentTurn}; " +
                $"Phase={coordinator.Context.Phase}.",
                this);
        }

        private bool EndBattle(bool ended)
        {
            if (!ended)
            {
                return false;
            }

            waitingActionId = 0;
            StopFlowProgression();
            UpdateBoardInputGate();
            return true;
        }

        private bool CanHandleBoardCallback()
        {
            return connectionEnabled
                && !isCleaningUp
                && coordinator != null
                && coordinator.Context.Result == BattleResultKind.None;
        }

        private void HandlePhaseChanged(BattlePhase phase)
        {
            if (phase == BattlePhase.MatchEventResolving)
            {
                ReconcilePresentationMatchQueue(
                    "MatchEventResolving");
            }

            UpdateBoardInputGate();
            GameLogger.Log(
                $"[BattleFlow] Phase changed. " +
                $"Turn={coordinator.Context.CurrentTurn}; Phase={phase}.",
                this);
        }

        private void HandleMatchEventExecuting(MatchEvent matchEvent)
        {
            if (!matchQueuePresentation.TryPop(matchEvent))
            {
                GameLogger.Error(
                    "[BattleFlow] Presentation MatchEvent did not match "
                        + "the authoritative execution. "
                        + $"SequenceIndex={matchEvent.SequenceIndex}; "
                        + $"CascadeStepIndex={matchEvent.CascadeStepIndex}; "
                        + $"Element={matchEvent.Element}; "
                        + $"RemovedBlockCount={matchEvent.RemovedBlockCount}.",
                    this);
                ReconcilePresentationMatchQueue(
                    "MatchEventExecutingMismatch");
            }
            else
            {
                PublishPresentationMatchQueueChanged();
            }

            GameLogger.Log(
                $"[BattleFlow] MatchEvent executing. " +
                $"Turn={coordinator.Context.CurrentTurn}; " +
                $"SequenceIndex={matchEvent.SequenceIndex}; " +
                $"CascadeStepIndex={matchEvent.CascadeStepIndex}; " +
                $"MatchIndex={matchEvent.MatchIndex}; " +
                $"Element={matchEvent.Element}; Tier={matchEvent.Tier}; " +
                $"RemovedBlockCount={matchEvent.RemovedBlockCount}.",
                this);
        }

        private void HandleBossActionStarted()
        {
            GameLogger.Log(
                $"[BattleFlow] Boss action started. " +
                $"Turn={coordinator.Context.CurrentTurn}.",
                this);
        }

        private void HandleResultReached(BattleResultKind result)
        {
            waitingActionId = 0;
            StopFlowProgression();
            ClearPresentationMatchQueue();
            SetBoardInputGate(false);
            GameLogger.Log(
                $"[BattleFlow] Result reached. " +
                $"Turn={coordinator.Context.CurrentTurn}; Result={result}.",
                this);
        }

        internal void ReconcilePresentationMatchQueueForCurrentRuntime()
        {
            if (coordinator == null
                || coordinator.Context.Result != BattleResultKind.None)
            {
                ClearPresentationMatchQueue();
                return;
            }

            if (coordinator.Context.Phase == BattlePhase.MatchEventResolving)
            {
                ReconcilePresentationMatchQueue("RuntimeConnection");
            }
        }

        private void BeginPresentationMatchQueue(long actionId)
        {
            if (matchQueuePresentation.BeginAction(actionId))
            {
                PublishPresentationMatchQueueChanged();
            }
        }

        private void ReconcilePresentationMatchQueue(string context)
        {
            IReadOnlyList<MatchEvent> authoritative =
                coordinator.GetPendingMatchEvents();
            if (!matchQueuePresentation.Reconcile(authoritative))
            {
                return;
            }

            GameLogger.Warning(
                "[BattleFlow] Rebuilt presentation MatchEvent queue from "
                    + $"the authoritative snapshot. Context={context}; "
                    + $"Count={authoritative.Count}.",
                this);
            PublishPresentationMatchQueueChanged();
        }

        private void ClearPresentationMatchQueue()
        {
            if (matchQueuePresentation.Clear())
            {
                PublishPresentationMatchQueueChanged();
            }
        }

        private void PublishPresentationMatchQueueChanged()
        {
            Action observers = PresentationMatchQueueChanged;
            if (observers == null)
            {
                return;
            }

            foreach (Action observer in observers.GetInvocationList())
            {
                try
                {
                    observer();
                }
                catch (Exception exception)
                {
                    GameLogger.Exception(exception, this);
                    GameLogger.Error(
                        "[BattleFlow] Presentation queue observer failed.",
                        this);
                }
            }
        }

        private void UpdateBoardInputGate()
        {
            bool enabled = connectionEnabled
                && coordinator != null
                && coordinator.Context.Result == BattleResultKind.None
                && coordinator.Context.Phase == BattlePhase.PlayerInput
                && !isResolvingActivePresentation;
            SetBoardInputGate(enabled);
        }

        private void SetActivePresentationResolving(bool resolving)
        {
            if (isResolvingActivePresentation == resolving)
            {
                return;
            }

            isResolvingActivePresentation = resolving;
            UpdateBoardInputGate();
            ActivePresentationStateChanged?.Invoke();
        }

        private void SetBoardInputGate(bool enabled)
        {
            if (boardController != null)
            {
                boardController.IsExternalInputEnabled = enabled;
            }

            if (notUsersTurnPanel != null)
            {
                bool inputAvailable = enabled && waitingActionId == 0;
                bool shouldShow = !inputAvailable
                    && !isInitialBoardPresentationPending;
                notUsersTurnPanel.SetActive(shouldShow);
            }
        }

        private bool ShouldDelayBeforeBossTransition()
        {
            if (preBossActionDelaySeconds <= 0f
                || coordinator == null
                || coordinator.Context.Result != BattleResultKind.None
                || coordinator.Context.Phase
                    != BattlePhase.MatchEventResolving
                || coordinator.PendingMatchEventCount != 0)
            {
                return false;
            }

            return combatBridge == null
                || (!combatBridge.Boss.IsDefeated
                    && !combatBridge.Party.IsIncapacitated);
        }

        private void UpdateInitialBoardPresentationState()
        {
            isInitialBoardPresentationPending = boardController != null
                && !boardController.HasInitialBoardReady;
        }

    }
}
