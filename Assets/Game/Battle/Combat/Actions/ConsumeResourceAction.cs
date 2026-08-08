using System;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Actions
{
    public enum ResourceConsumptionMode
    {
        Amount,
        All
    }

    public sealed class ConsumeResourceAction : CombatAction
    {
        public ConsumeResourceAction(
            long actionId,
            ActionOrigin origin,
            BossBattleState targetBoss,
            string resourceId,
            string consumerId,
            ResourceConsumptionMode mode,
            int amount = 0,
            long? rootActionId = null,
            long? sourceActionId = null)
            : base(actionId, origin, rootActionId, sourceActionId)
        {
            TargetBoss = targetBoss
                ?? throw new ArgumentNullException(nameof(targetBoss));
            ValidateId(resourceId, nameof(resourceId));
            ValidateId(consumerId, nameof(consumerId));
            if (!Enum.IsDefined(typeof(ResourceConsumptionMode), mode))
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            if ((mode == ResourceConsumptionMode.Amount && amount <= 0)
                || (mode == ResourceConsumptionMode.All && amount != 0))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    amount,
                    "Amount mode requires a positive amount; all mode uses zero.");
            }

            ResourceId = resourceId;
            ConsumerId = consumerId;
            Mode = mode;
            Amount = amount;
        }

        public BossBattleState TargetBoss { get; }
        public string ResourceId { get; }
        public string ConsumerId { get; }
        public ResourceConsumptionMode Mode { get; }
        public int Amount { get; }

        private static void ValidateId(string id, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(
                    "ID cannot be null or whitespace.",
                    parameterName);
            }
        }
    }
}
