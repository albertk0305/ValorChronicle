using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Actions
{
    public sealed class CombatActionExecutionSession
    {
        private readonly CombatActionExecutor executor;
        private readonly CombatActionQueue queue;
        private readonly BossBattleState boss;
        private readonly PartyBattleState party;
        private readonly CombatTriggerResolver triggerResolver;
        private readonly List<CombatActionResult> results =
            new List<CombatActionResult>();

        internal CombatActionExecutionSession(
            CombatActionExecutor executor,
            CombatActionQueue queue,
            BossBattleState boss,
            PartyBattleState party,
            CombatTriggerResolver triggerResolver)
        {
            this.executor = executor
                ?? throw new ArgumentNullException(nameof(executor));
            this.queue = queue
                ?? throw new ArgumentNullException(nameof(queue));
            this.boss = boss
                ?? throw new ArgumentNullException(nameof(boss));
            this.party = party
                ?? throw new ArgumentNullException(nameof(party));
            this.triggerResolver = triggerResolver
                ?? throw new ArgumentNullException(nameof(triggerResolver));

            if (IsBattleTerminal)
            {
                bool cleared = queue.Count > 0;
                queue.Clear();
                Complete(true, cleared);
            }
            else if (queue.Count == 0)
            {
                Complete(false, false);
            }
        }

        public bool IsCompleted { get; private set; }
        public int CompletedActionCount => results.Count;
        public CombatActionExecutionResult Result { get; private set; }

        public CombatAction NextAction
        {
            get
            {
                return !IsCompleted && queue.TryPeek(out CombatAction action)
                    ? action
                    : null;
            }
        }

        public bool TryExecuteNext(
            out CombatActionExecutionStepResult stepResult)
        {
            stepResult = null;
            if (IsCompleted)
            {
                return false;
            }

            if (!queue.TryDequeue(out CombatAction action))
            {
                Complete(false, false);
                return false;
            }

            CombatActionResult completedResult = executor.ExecuteAction(
                action,
                results.Count + 1);
            results.Add(completedResult);

            bool wasTerminalAfterStep = IsBattleTerminal;
            if (wasTerminalAfterStep)
            {
                bool cleared = queue.Count > 0;
                queue.Clear();
                Complete(true, cleared);
            }
            else
            {
                var history = new CombatActionExecutionHistory(results);
                var triggerContext = new CombatActionTriggerContext(
                    completedResult,
                    boss,
                    party,
                    history);
                IReadOnlyList<CombatAction> derivedActions =
                    triggerResolver.Resolve(triggerContext);
                queue.EnqueueNextRange(derivedActions);

                if (queue.Count == 0)
                {
                    Complete(false, false);
                }
            }

            stepResult = new CombatActionExecutionStepResult(
                action,
                completedResult,
                wasTerminalAfterStep,
                IsCompleted);
            return true;
        }

        private bool IsBattleTerminal =>
            boss.IsDefeated || party.IsIncapacitated;

        private void Complete(
            bool stoppedEarly,
            bool clearedRemainingActions)
        {
            IsCompleted = true;
            Result = executor.BuildResult(
                results,
                stoppedEarly,
                clearedRemainingActions);
        }
    }

    public sealed class CombatActionExecutionStepResult
    {
        internal CombatActionExecutionStepResult(
            CombatAction action,
            CombatActionResult result,
            bool wasTerminalAfterStep,
            bool isCompleted)
        {
            Action = action ?? throw new ArgumentNullException(nameof(action));
            Result = result ?? throw new ArgumentNullException(nameof(result));
            WasTerminalAfterStep = wasTerminalAfterStep;
            IsCompleted = isCompleted;
        }

        public CombatAction Action { get; }
        public CombatActionResult Result { get; }
        public bool WasTerminalAfterStep { get; }
        public bool IsCompleted { get; }
    }
}
