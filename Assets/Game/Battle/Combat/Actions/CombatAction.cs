using System;

namespace ValorChronicle.Battle.Combat.Actions
{
    public abstract class CombatAction
    {
        protected CombatAction(
            long actionId,
            ActionOrigin origin,
            long? rootActionId = null,
            long? sourceActionId = null)
        {
            if (actionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(actionId));
            }

            if (!Enum.IsDefined(typeof(ActionOrigin), origin))
            {
                throw new ArgumentOutOfRangeException(nameof(origin));
            }

            long resolvedRootActionId = rootActionId ?? actionId;
            if (resolvedRootActionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rootActionId));
            }

            if (sourceActionId.HasValue && sourceActionId.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceActionId));
            }

            if (sourceActionId == actionId)
            {
                throw new ArgumentException(
                    "An action cannot be its own source.",
                    nameof(sourceActionId));
            }

            bool isFollowUpOrigin = origin == ActionOrigin.Additional
                || origin == ActionOrigin.Chase;
            if (isFollowUpOrigin
                && (!sourceActionId.HasValue
                    || resolvedRootActionId == actionId))
            {
                throw new ArgumentException(
                    "Additional and chase actions require source and root "
                        + "action tracking.",
                    nameof(sourceActionId));
            }

            if (resolvedRootActionId != actionId
                && !sourceActionId.HasValue)
            {
                throw new ArgumentException(
                    "A derived action requires a source action ID.",
                    nameof(sourceActionId));
            }

            ActionId = actionId;
            Origin = origin;
            RootActionId = resolvedRootActionId;
            SourceActionId = sourceActionId;
        }

        public long ActionId { get; }
        public ActionOrigin Origin { get; }
        public long RootActionId { get; }
        public long? SourceActionId { get; }
        public virtual bool CanTriggerFollowUp => false;
    }
}
