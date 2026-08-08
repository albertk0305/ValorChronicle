using System;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Actions
{
    public sealed class CombatActionTriggerContext
    {
        public CombatActionTriggerContext(
            CombatActionResult completedResult,
            BossBattleState boss,
            PartyBattleState party,
            CombatActionExecutionHistory history)
        {
            CompletedResult = completedResult
                ?? throw new ArgumentNullException(nameof(completedResult));
            Boss = boss ?? throw new ArgumentNullException(nameof(boss));
            Party = party ?? throw new ArgumentNullException(nameof(party));
            History = history
                ?? throw new ArgumentNullException(nameof(history));
            if (!history.TryGetResult(
                    completedResult.Action.ActionId,
                    out CombatActionResult recordedResult)
                || !ReferenceEquals(recordedResult, completedResult))
            {
                throw new ArgumentException(
                    "Completed result must be present in execution history.",
                    nameof(history));
            }
        }

        public CombatActionResult CompletedResult { get; }
        public CombatAction CompletedAction => CompletedResult.Action;
        public BossBattleState Boss { get; }
        public PartyBattleState Party { get; }
        public CombatActionExecutionHistory History { get; }
        public long ActionId => CompletedAction.ActionId;
        public long RootActionId => CompletedAction.RootActionId;
        public long? SourceActionId => CompletedAction.SourceActionId;
        public bool CanTriggerFollowUp =>
            CompletedAction.CanTriggerFollowUp;

        public bool CanTriggerAttackFollowUp
        {
            get
            {
                if (!(CompletedResult is ConsumeResourceActionResult
                    consumptionResult))
                {
                    return CanTriggerFollowUp;
                }

                if (consumptionResult.ConsumptionRecord == null
                    || !TryGetSourceActionResult(
                        out CombatActionResult sourceResult))
                {
                    return false;
                }

                return sourceResult.Action.CanTriggerFollowUp;
            }
        }

        public bool TryGetSourceActionResult(
            out CombatActionResult sourceResult)
        {
            if (!SourceActionId.HasValue)
            {
                sourceResult = null;
                return false;
            }

            return History.TryGetResult(
                SourceActionId.Value,
                out sourceResult);
        }
    }
}
