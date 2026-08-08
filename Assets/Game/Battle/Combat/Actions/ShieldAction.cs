using System;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Shields;

namespace ValorChronicle.Battle.Combat.Actions
{
    public sealed class ShieldAction : CombatAction
    {
        public ShieldAction(
            long actionId,
            ActionOrigin origin,
            ShieldGenerationContextBuildRequest contextRequest,
            ShieldGrantRequest grantRequest,
            long? rootActionId = null,
            long? sourceActionId = null)
            : base(actionId, origin, rootActionId, sourceActionId)
        {
            ContextRequest = contextRequest
                ?? throw new ArgumentNullException(nameof(contextRequest));
            GrantRequest = grantRequest
                ?? throw new ArgumentNullException(nameof(grantRequest));
        }

        public ShieldGenerationContextBuildRequest ContextRequest { get; }
        public ShieldGrantRequest GrantRequest { get; }
    }
}
