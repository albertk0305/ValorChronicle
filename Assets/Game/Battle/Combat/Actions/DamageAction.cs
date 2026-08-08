using System;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;

namespace ValorChronicle.Battle.Combat.Actions
{
    public sealed class DamageAction : CombatAction
    {
        public DamageAction(
            long actionId,
            ActionOrigin origin,
            DamageContextBuildRequest contextRequest,
            long? rootActionId = null,
            long? sourceActionId = null)
            : base(actionId, origin, rootActionId, sourceActionId)
        {
            ContextRequest = contextRequest
                ?? throw new ArgumentNullException(nameof(contextRequest));
            ValidateOriginMatchesAttackType(
                origin,
                contextRequest.AttackType);
        }

        public DamageContextBuildRequest ContextRequest { get; }
        public override bool CanTriggerFollowUp =>
            Origin == ActionOrigin.Match || Origin == ActionOrigin.Active;

        private static void ValidateOriginMatchesAttackType(
            ActionOrigin origin,
            AttackType attackType)
        {
            bool matches = origin == ActionOrigin.System
                || (origin == ActionOrigin.Match
                    && attackType == AttackType.Match)
                || (origin == ActionOrigin.Active
                    && attackType == AttackType.Active)
                || (origin == ActionOrigin.Additional
                    && attackType == AttackType.Additional)
                || (origin == ActionOrigin.Chase
                    && attackType == AttackType.Chase)
                || (origin == ActionOrigin.DamageOverTime
                    && attackType == AttackType.DamageOverTime);
            if (!matches)
            {
                throw new ArgumentException(
                    "Action origin must match the damage attack type.",
                    nameof(origin));
            }
        }
    }
}
