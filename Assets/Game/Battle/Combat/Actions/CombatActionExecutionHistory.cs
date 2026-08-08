using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Combat.Actions
{
    public sealed class CombatActionExecutionHistory
    {
        private readonly ReadOnlyCollection<CombatActionResult> results;
        private readonly Dictionary<long, CombatActionResult> resultsByActionId;

        public CombatActionExecutionHistory(
            IReadOnlyList<CombatActionResult> actionResults)
        {
            if (actionResults == null)
            {
                throw new ArgumentNullException(nameof(actionResults));
            }

            var copiedResults = new CombatActionResult[actionResults.Count];
            resultsByActionId =
                new Dictionary<long, CombatActionResult>(actionResults.Count);
            for (int index = 0; index < actionResults.Count; index++)
            {
                CombatActionResult result = actionResults[index];
                if (result == null || result.Action == null)
                {
                    throw new ArgumentException(
                        "Execution history cannot contain a null result.",
                        nameof(actionResults));
                }

                if (resultsByActionId.ContainsKey(result.Action.ActionId))
                {
                    throw new ArgumentException(
                        $"Duplicate executed action ID: "
                            + $"{result.Action.ActionId}.",
                        nameof(actionResults));
                }

                copiedResults[index] = result;
                resultsByActionId.Add(result.Action.ActionId, result);
            }

            results = Array.AsReadOnly(copiedResults);
        }

        public IReadOnlyList<CombatActionResult> Results => results;
        public int Count => results.Count;

        public bool TryGetResult(
            long actionId,
            out CombatActionResult result)
        {
            if (actionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(actionId));
            }

            return resultsByActionId.TryGetValue(actionId, out result);
        }
    }
}
