using System;
using System.Collections.Generic;

namespace ValorChronicle.Battle.Combat.Actions
{
    public sealed class CombatActionQueue
    {
        private readonly LinkedList<CombatAction> actions =
            new LinkedList<CombatAction>();
        private readonly HashSet<long> queuedActionIds =
            new HashSet<long>();

        public CombatActionQueue()
        {
        }

        public CombatActionQueue(IReadOnlyList<CombatAction> initialActions)
        {
            EnqueueRange(initialActions);
        }

        public int Count => actions.Count;

        public void Enqueue(CombatAction action)
        {
            ValidateActionForInsertion(action, nameof(action));
            actions.AddLast(action);
            queuedActionIds.Add(action.ActionId);
        }

        public void EnqueueRange(IReadOnlyList<CombatAction> newActions)
        {
            if (newActions == null)
            {
                throw new ArgumentNullException(nameof(newActions));
            }

            ValidateRangeForInsertion(newActions, nameof(newActions));

            for (int index = 0; index < newActions.Count; index++)
            {
                CombatAction action = newActions[index];
                actions.AddLast(action);
                queuedActionIds.Add(action.ActionId);
            }
        }

        public void EnqueueNext(CombatAction action)
        {
            ValidateActionForInsertion(action, nameof(action));
            actions.AddFirst(action);
            queuedActionIds.Add(action.ActionId);
        }

        public void EnqueueNextRange(IReadOnlyList<CombatAction> newActions)
        {
            if (newActions == null)
            {
                throw new ArgumentNullException(nameof(newActions));
            }

            ValidateRangeForInsertion(newActions, nameof(newActions));
            for (int index = newActions.Count - 1; index >= 0; index--)
            {
                CombatAction action = newActions[index];
                actions.AddFirst(action);
                queuedActionIds.Add(action.ActionId);
            }
        }

        public bool TryDequeue(out CombatAction action)
        {
            if (actions.Count == 0)
            {
                action = null;
                return false;
            }

            action = actions.First.Value;
            actions.RemoveFirst();
            queuedActionIds.Remove(action.ActionId);
            return true;
        }

        public void Clear()
        {
            actions.Clear();
            queuedActionIds.Clear();
        }

        private void ValidateRangeForInsertion(
            IReadOnlyList<CombatAction> newActions,
            string parameterName)
        {
            var batchActionIds = new HashSet<long>();
            for (int index = 0; index < newActions.Count; index++)
            {
                CombatAction action = newActions[index];
                if (action == null)
                {
                    throw new ArgumentException(
                        "Action collection cannot contain null.",
                        parameterName);
                }

                if (queuedActionIds.Contains(action.ActionId)
                    || !batchActionIds.Add(action.ActionId))
                {
                    throw new ArgumentException(
                        $"Duplicate queued action ID: {action.ActionId}.",
                        parameterName);
                }
            }
        }

        private void ValidateActionForInsertion(
            CombatAction action,
            string parameterName)
        {
            if (action == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (queuedActionIds.Contains(action.ActionId))
            {
                throw new ArgumentException(
                    $"Duplicate queued action ID: {action.ActionId}.",
                    parameterName);
            }
        }
    }
}
