using System;
using ValorChronicle.Battle.Combat.Healing;

namespace ValorChronicle.Battle.Combat.Actions
{
    public sealed class HealAction : CombatAction
    {
        public HealAction(
            long actionId,
            ActionOrigin origin,
            HealingContextBuildRequest contextRequest,
            long? rootActionId = null,
            long? sourceActionId = null)
            : base(actionId, origin, rootActionId, sourceActionId)
        {
            ContextRequest = contextRequest
                ?? throw new ArgumentNullException(nameof(contextRequest));
        }

        public HealingContextBuildRequest ContextRequest { get; }
    }
}
