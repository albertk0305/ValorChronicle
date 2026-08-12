using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;

namespace ValorChronicle.Battle.Flow
{
    public sealed class BattleFlowCoordinator
    {
        private readonly MatchEventQueue matchEventQueue;
        private long nextMatchEventExecutionId;
        private MatchEventExecution currentMatchEventExecution;
        private int resolvedMatchEventCount;

        public BattleFlowCoordinator(int turnLimit)
            : this(turnLimit, Array.Empty<int>())
        {
        }

        public BattleFlowCoordinator(
            int turnLimit,
            IReadOnlyList<int> activeAbilityCooldowns)
        {
            Context = new BattleContext(
                turnLimit,
                activeAbilityCooldowns);
            matchEventQueue = new MatchEventQueue();
        }

        public event Action<BattlePhase> PhaseChanged;
        /// <summary>
        /// Notifies listeners that a MatchEvent has begun. Returning from an
        /// event handler does not complete the MatchEvent.
        /// </summary>
        public event Action<MatchEvent> MatchEventExecuting;
        public event Action BossActionStarted;
        public event Action<BattleResultKind> ResultReached;

        public BattleContext Context { get; }
        public int PendingMatchEventCount => matchEventQueue.Count;
        public bool HasMatchEventInFlight =>
            currentMatchEventExecution != null;
        public MatchEventExecution CurrentMatchEventExecution =>
            currentMatchEventExecution;

        public IReadOnlyList<MatchEvent> GetPendingMatchEvents()
        {
            return matchEventQueue.GetPendingSnapshot();
        }

        public bool CanUseActiveAbility(int activeAbilityIndex)
        {
            return Context.Result == BattleResultKind.None
                && Context.Phase == BattlePhase.PlayerInput
                && activeAbilityIndex >= 0
                && activeAbilityIndex < Context.ActiveAbilities.Count
                && Context.ActiveAbilities[activeAbilityIndex].CanUse;
        }

        public bool StartBattle()
        {
            if (Context.Phase != BattlePhase.NotStarted
                || Context.Result != BattleResultKind.None)
            {
                return false;
            }

            matchEventQueue.Clear();
            resolvedMatchEventCount = 0;
            BeginNextTurn();
            return true;
        }

        public bool TryUseActiveAbility(int activeAbilityIndex)
        {
            if (Context.Phase != BattlePhase.PlayerInput
                || Context.Result != BattleResultKind.None)
            {
                return false;
            }

            if (activeAbilityIndex < 0
                || activeAbilityIndex >= Context.ActiveAbilities.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(activeAbilityIndex),
                    activeAbilityIndex,
                    "Active ability index is outside the battle context.");
            }

            if (!CanUseActiveAbility(activeAbilityIndex))
            {
                return false;
            }

            return Context.ActiveAbilities[activeAbilityIndex].TryUse();
        }

        public bool TryBeginBoardResolution()
        {
            if (!CanExecuteInPhase(BattlePhase.PlayerInput))
            {
                return false;
            }

            TransitionTo(BattlePhase.BoardResolving);
            return true;
        }

        public bool NotifyBoardActionResolved(
            BoardCascadeResult cascade,
            bool consumesTurn)
        {
            if (!CanExecuteInPhase(BattlePhase.BoardResolving))
            {
                return false;
            }

            if (!consumesTurn)
            {
                throw new ArgumentException(
                    "Only a confirmed consuming board action may resolve "
                        + "the PlayerInput phase.",
                    nameof(consumesTurn));
            }

            if (cascade == null)
            {
                throw new ArgumentNullException(
                    nameof(cascade),
                    "A consuming board action must provide its resolved "
                        + "cascade.");
            }

            matchEventQueue.Clear();
            resolvedMatchEventCount = 0;
            IReadOnlyList<MatchEvent> matchEvents =
                MatchEventFactory.Create(cascade);
            matchEventQueue.EnqueueRange(matchEvents);
            resolvedMatchEventCount = matchEvents.Count;

            TransitionTo(BattlePhase.MatchEventResolving);
            return true;
        }

        public bool TryBeginNextMatchEvent(
            out MatchEventExecution execution)
        {
            execution = null;
            if (!CanExecuteInPhase(BattlePhase.MatchEventResolving))
            {
                return false;
            }

            if (currentMatchEventExecution != null)
            {
                return false;
            }

            if (!matchEventQueue.TryDequeue(out MatchEvent matchEvent))
            {
                BeginBossAction();
                return false;
            }

            execution = new MatchEventExecution(
                checked(++nextMatchEventExecutionId),
                matchEvent,
                resolvedMatchEventCount);
            currentMatchEventExecution = execution;
            MatchEventExecuting?.Invoke(matchEvent);
            return true;
        }

        public bool TryExecuteNextMatchEvent(out MatchEvent matchEvent)
        {
            bool began = TryBeginNextMatchEvent(
                out MatchEventExecution execution);
            matchEvent = execution?.MatchEvent;
            return began;
        }

        public bool CompleteCurrentMatchEvent(long executionId)
        {
            if (!CanExecuteInPhase(BattlePhase.MatchEventResolving)
                || currentMatchEventExecution == null
                || currentMatchEventExecution.ExecutionId != executionId)
            {
                return false;
            }

            currentMatchEventExecution = null;
            if (matchEventQueue.Count == 0)
            {
                resolvedMatchEventCount = 0;
                BeginBossAction();
            }

            return true;
        }

        public int ExecuteRemainingMatchEvents()
        {
            if (!CanExecuteInPhase(BattlePhase.MatchEventResolving))
            {
                return 0;
            }

            int executedCount = 0;
            while (Context.Phase == BattlePhase.MatchEventResolving
                && Context.Result == BattleResultKind.None)
            {
                if (TryBeginNextMatchEvent(
                    out MatchEventExecution execution))
                {
                    executedCount++;
                    CompleteCurrentMatchEvent(execution.ExecutionId);
                    continue;
                }

                break;
            }

            return executedCount;
        }

        public bool CompleteBossAction()
        {
            if (!CanExecuteInPhase(BattlePhase.BossActing))
            {
                return false;
            }

            TransitionTo(BattlePhase.TurnEnd);
            if (Context.Result != BattleResultKind.None)
            {
                return true;
            }

            TransitionTo(BattlePhase.ResultCheck);
            if (Context.Result != BattleResultKind.None)
            {
                return true;
            }

            if (Context.CurrentTurn >= Context.TurnLimit)
            {
                EndBattle(BattleResultKind.TurnLimitReached);
            }
            else
            {
                BeginNextTurn();
            }

            return true;
        }

        public bool NotifyBossDefeated()
        {
            return EndBattle(BattleResultKind.Victory);
        }

        public bool NotifyPartyIncapacitated()
        {
            return EndBattle(BattleResultKind.Defeat);
        }

        public bool AbortBattle()
        {
            return EndBattle(BattleResultKind.Aborted);
        }

        private bool CanExecuteInPhase(BattlePhase phase)
        {
            return Context.Result == BattleResultKind.None
                && Context.Phase == phase;
        }

        private void BeginNextTurn()
        {
            if (Context.Result != BattleResultKind.None
                || Context.CurrentTurn >= Context.TurnLimit)
            {
                return;
            }

            Context.CurrentTurn++;
            for (int index = 0;
                index < Context.ActiveAbilities.Count;
                index++)
            {
                Context.ActiveAbilities[index].BeginTurn();
            }

            TransitionTo(BattlePhase.TurnStart);
            if (Context.Result == BattleResultKind.None)
            {
                TransitionTo(BattlePhase.PlayerInput);
            }
        }

        private void BeginBossAction()
        {
            if (!CanExecuteInPhase(BattlePhase.MatchEventResolving))
            {
                return;
            }

            TransitionTo(BattlePhase.BossActing);
            if (Context.Result == BattleResultKind.None)
            {
                BossActionStarted?.Invoke();
            }
        }

        private bool EndBattle(BattleResultKind result)
        {
            if (result == BattleResultKind.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(result),
                    result,
                    "A terminal battle result is required.");
            }

            if (Context.Result != BattleResultKind.None)
            {
                return false;
            }

            matchEventQueue.Clear();
            currentMatchEventExecution = null;
            resolvedMatchEventCount = 0;
            Context.Result = result;
            TransitionTo(BattlePhase.Result);
            ResultReached?.Invoke(result);
            return true;
        }

        private void TransitionTo(BattlePhase phase)
        {
            Context.Phase = phase;
            PhaseChanged?.Invoke(phase);
        }
    }
}
