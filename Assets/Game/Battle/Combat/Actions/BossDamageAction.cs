using System;
using ValorChronicle.Battle.Combat.Damage;

namespace ValorChronicle.Battle.Combat.Actions
{
    public sealed class BossDamageAction : CombatAction
    {
        public BossDamageAction(
            long actionId,
            BossDamageContextBuildRequest contextRequest,
            long? rootActionId = null,
            long? sourceActionId = null)
            : base(
                actionId,
                ActionOrigin.System,
                rootActionId,
                sourceActionId)
        {
            ContextRequest = contextRequest
                ?? throw new ArgumentNullException(nameof(contextRequest));
        }

        public BossDamageContextBuildRequest ContextRequest { get; }
    }
}
