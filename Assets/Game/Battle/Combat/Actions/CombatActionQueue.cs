using System;
using System.Collections.Generic;

namespace ValorChronicle.Battle.Combat.Actions
{
    public sealed class CombatActionQueue
    {
        private readonly Queue<CombatAction> actions =
            new Queue<CombatAction>();

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
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            actions.Enqueue(action);
        }

        public void EnqueueRange(IReadOnlyList<CombatAction> newActions)
        {
            if (newActions == null)
            {
                throw new ArgumentNullException(nameof(newActions));
            }

            for (int index = 0; index < newActions.Count; index++)
            {
                if (newActions[index] == null)
                {
                    throw new ArgumentException(
                        "Action collection cannot contain null.",
                        nameof(newActions));
                }
            }

            for (int index = 0; index < newActions.Count; index++)
            {
                actions.Enqueue(newActions[index]);
            }
        }

        public bool TryDequeue(out CombatAction action)
        {
            if (actions.Count == 0)
            {
                action = null;
                return false;
            }

            action = actions.Dequeue();
            return true;
        }

        public void Clear()
        {
            actions.Clear();
        }
    }
}
