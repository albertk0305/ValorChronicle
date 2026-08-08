using System;
using System.Collections;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Board.Presentation;
using ValorChronicle.Core.Logging;

namespace ValorChronicle.Battle.Flow.Presentation
{
    public sealed class BattleFlowController : MonoBehaviour
    {
        [SerializeField]
        private BattleBoardController boardController = null;

        private bool connectionEnabled;
        private bool boardEventsSubscribed;
        private bool coordinatorEventsSubscribed;
        private bool isCleaningUp;
        private bool isAdvancingFlow;
        private bool isExecutingFlowStep;
        private long waitingActionId;
        private long lastAcceptedActionId;
        private int flowVersion;
        private Coroutine flowCoroutine;
        private BattleFlowSetup setup;
        private BattleFlowCoordinator coordinator;

        public BattleFlowSetup Setup => setup;
        public BattleFlowCoordinator Coordinator => coordinator;
        public BattleContext Context => coordinator?.Context;

        private void OnEnable()
        {
            connectionEnabled = true;
            isCleaningUp = false;
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

            if (coordinator != null
                && coordinator.Context.Result == BattleResultKind.None)
            {
                coordinator.AbortBattle();
            }

            UnsubscribeCoordinatorEvents();
            waitingActionId = 0;
        }

        public void Initialize(BattleFlowSetup battleSetup)
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

            setup = battleSetup;
            coordinator = new BattleFlowCoordinator(
                battleSetup.TurnLimit,
                battleSetup.ActiveAbilityCooldowns);
            SetBoardInputGate(false);
            if (connectionEnabled)
            {
                SubscribeBoardEvents();
                SubscribeCoordinatorEvents();
                TryStartBattleFromReadyBoard();
            }
        }

        public bool CompleteActiveInput()
        {
            if (coordinator == null)
            {
                return false;
            }

            bool completed = coordinator.CompleteActiveInput();
            UpdateBoardInputGate();
            return completed;
        }

        public bool TryUseActive(int activeIndex)
        {
            return coordinator != null
                && coordinator.TryUseActiveAbility(activeIndex);
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
            TryStartBattleFromReadyBoard();
        }

        private void TryStartBattleFromReadyBoard()
        {
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

            if (coordinator.Context.Phase != BattlePhase.PuzzleInput)
            {
                GameLogger.Error(
                    $"[BattleFlow] Board action started in an invalid phase. " +
                    $"ActionId={execution.ActionId}; " +
                    $"Phase={coordinator.Context.Phase}.",
                    this);
                UpdateBoardInputGate();
                return;
            }

            if (!coordinator.NotifyBoardActionStarted())
            {
                GameLogger.Error(
                    $"[BattleFlow] Coordinator rejected board action start. " +
                    $"ActionId={execution.ActionId}.",
                    this);
                UpdateBoardInputGate();
                return;
            }

            waitingActionId = execution.ActionId;
            lastAcceptedActionId = execution.ActionId;
            UpdateBoardInputGate();
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
                    HandleFailedBoardAction(completion);
                    break;

                case BoardActionCompletionStatus.Interrupted:
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

            BoardCascadeResult cascade = result.ConsumesTurn
                ? result.Cascade
                : null;
            if (result.ConsumesTurn && cascade == null)
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
                    result.ConsumesTurn);
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
            if (result.ConsumesTurn)
            {
                StartFlowProgression();
            }
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
                        yield return null;

                        if (!IsFlowProgressionCurrent(version))
                        {
                            yield break;
                        }

                        bool matchCompleted;
                        isExecutingFlowStep = true;
                        try
                        {
                            matchCompleted =
                                coordinator.CompleteCurrentMatchEvent(
                                    execution.ExecutionId);
                        }
                        finally
                        {
                            isExecutingFlowStep = false;
                        }

                        if (!matchCompleted)
                        {
                            GameLogger.Error(
                                $"[BattleFlow] Placeholder MatchEvent could " +
                                $"not be completed. " +
                                $"ExecutionId={execution.ExecutionId}; " +
                                $"Turn={coordinator.Context.CurrentTurn}; " +
                                $"Phase={coordinator.Context.Phase}.",
                                this);
                            yield break;
                        }

                        UpdateBoardInputGate();
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
                isExecutingFlowStep = true;
                try
                {
                    completed = coordinator.CompleteBossAction();
                }
                finally
                {
                    isExecutingFlowStep = false;
                }

                if (!completed)
                {
                    GameLogger.Error(
                        $"[BattleFlow] Placeholder boss action could not " +
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

        private void StopFlowProgression()
        {
            flowVersion++;
            Coroutine activeFlowCoroutine = flowCoroutine;
            flowCoroutine = null;
            isAdvancingFlow = false;
            if (activeFlowCoroutine != null && !isExecutingFlowStep)
            {
                StopCoroutine(activeFlowCoroutine);
            }
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
            UpdateBoardInputGate();
            GameLogger.Log(
                $"[BattleFlow] Phase changed. " +
                $"Turn={coordinator.Context.CurrentTurn}; Phase={phase}.",
                this);
        }

        private void HandleMatchEventExecuting(MatchEvent matchEvent)
        {
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
            SetBoardInputGate(false);
            GameLogger.Log(
                $"[BattleFlow] Result reached. " +
                $"Turn={coordinator.Context.CurrentTurn}; Result={result}.",
                this);
        }

        private void UpdateBoardInputGate()
        {
            bool enabled = connectionEnabled
                && coordinator != null
                && coordinator.Context.Result == BattleResultKind.None
                && coordinator.Context.Phase == BattlePhase.PuzzleInput;
            SetBoardInputGate(enabled);
        }

        private void SetBoardInputGate(bool enabled)
        {
            if (boardController != null)
            {
                boardController.IsExternalInputEnabled = enabled;
            }
        }

    }
}
