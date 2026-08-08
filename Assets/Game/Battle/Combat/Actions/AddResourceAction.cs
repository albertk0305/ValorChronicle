using System;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Actions
{
    public sealed class AddResourceAction : CombatAction
    {
        public AddResourceAction(
            long actionId,
            ActionOrigin origin,
            BossBattleState targetBoss,
            string resourceId,
            int amount,
            long? rootActionId = null,
            long? sourceActionId = null)
            : base(actionId, origin, rootActionId, sourceActionId)
        {
            TargetBoss = targetBoss
                ?? throw new ArgumentNullException(nameof(targetBoss));
            ValidateId(resourceId, nameof(resourceId));
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            ResourceId = resourceId;
            Amount = amount;
        }

        public BossBattleState TargetBoss { get; }
        public string ResourceId { get; }
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
