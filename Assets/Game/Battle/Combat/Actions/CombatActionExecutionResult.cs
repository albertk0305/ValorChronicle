using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Combat.Actions
{
    public sealed class CombatActionExecutionResult
    {
        private readonly ReadOnlyCollection<CombatActionResult> actionResults;

        internal CombatActionExecutionResult(
            IReadOnlyList<CombatActionResult> actionResults,
            bool stoppedEarly,
            bool bossDefeated,
            bool partyIncapacitated,
            bool clearedRemainingActions)
        {
            if (actionResults == null)
            {
                throw new ArgumentNullException(nameof(actionResults));
            }

            var copiedResults = new CombatActionResult[actionResults.Count];
            for (int index = 0; index < actionResults.Count; index++)
            {
                copiedResults[index] = actionResults[index];
            }

            this.actionResults = Array.AsReadOnly(copiedResults);
            StoppedEarly = stoppedEarly;
            BossDefeated = bossDefeated;
            PartyIncapacitated = partyIncapacitated;
            ClearedRemainingActions = clearedRemainingActions;
        }

        public IReadOnlyList<CombatActionResult> ActionResults =>
            actionResults;
        public int CompletedActionCount => actionResults.Count;
        public bool StoppedEarly { get; }
        public bool BossDefeated { get; }
        public bool PartyIncapacitated { get; }
        public bool ClearedRemainingActions { get; }
    }
}
