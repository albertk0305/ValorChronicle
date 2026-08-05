using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;

namespace ValorChronicle.Battle.Flow
{
    public sealed class BattleFlowCoordinator
    {
        private readonly MatchEventQueue matchEventQueue;

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
        public event Action<MatchEvent> MatchEventExecuting;
        public event Action BossActionStarted;
        public event Action<BattleResultKind> ResultReached;

        public BattleContext Context { get; }
        public int PendingMatchEventCount => matchEventQueue.Count;

        public bool StartBattle()
        {
            if (Context.Phase != BattlePhase.NotStarted
                || Context.Result != BattleResultKind.None)
            {
                return false;
            }

            matchEventQueue.Clear();
            BeginNextTurn();
            return true;
        }

        public bool TryUseActiveAbility(int activeAbilityIndex)
        {
            if (Context.Phase != BattlePhase.ActiveInput
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

            return Context.ActiveAbilities[activeAbilityIndex].TryUse();
        }

        public bool CompleteActiveInput()
        {
            if (!CanExecuteInPhase(BattlePhase.ActiveInput))
            {
                return false;
            }

            TransitionTo(BattlePhase.PuzzleInput);
            return true;
        }

        public bool NotifyBoardActionStarted()
        {
            if (!CanExecuteInPhase(BattlePhase.PuzzleInput))
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
                if (cascade != null)
                {
                    throw new ArgumentException(
                        "A non-consuming board action cannot contain a " +
                        "cascade result.",
                        nameof(cascade));
                }

                TransitionTo(BattlePhase.PuzzleInput);
                return true;
            }

            matchEventQueue.Clear();
            if (cascade != null)
            {
                matchEventQueue.EnqueueRange(
                    MatchEventFactory.Create(cascade));
            }

            TransitionTo(BattlePhase.MatchEventResolving);
            return true;
        }

        public bool TryExecuteNextMatchEvent(out MatchEvent matchEvent)
        {
            matchEvent = null;
            if (!CanExecuteInPhase(BattlePhase.MatchEventResolving))
            {
                return false;
            }

            if (!matchEventQueue.TryDequeue(out matchEvent))
            {
                BeginBossAction();
                return false;
            }

            MatchEventExecuting?.Invoke(matchEvent);
            if (Context.Result != BattleResultKind.None)
            {
                return true;
            }

            if (matchEventQueue.Count == 0)
            {
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
                if (TryExecuteNextMatchEvent(out MatchEvent matchEvent))
                {
                    if (matchEvent != null)
                    {
                        executedCount++;
                    }

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
                TransitionTo(BattlePhase.ActiveInput);
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
