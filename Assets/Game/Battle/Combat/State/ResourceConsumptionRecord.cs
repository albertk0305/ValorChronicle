using System;

namespace ValorChronicle.Battle.Combat.State
{
    public sealed class ResourceConsumptionRecord
    {
        public ResourceConsumptionRecord(
            ResourceConsumeResult consumptionResult,
            string consumerId,
            string targetId)
        {
            if (consumptionResult == null)
            {
                throw new ArgumentNullException(
                    nameof(consumptionResult));
            }

            ValidateId(consumerId, nameof(consumerId));
            ValidateId(targetId, nameof(targetId));
            if (consumptionResult.ConsumedAmount <= 0)
            {
                throw new ArgumentException(
                    "A consumption record requires an actual consumption.",
                    nameof(consumptionResult));
            }

            ResourceId = consumptionResult.ResourceId;
            ConsumerId = consumerId;
            TargetId = targetId;
            ConsumedAmount = consumptionResult.ConsumedAmount;
        }

        public string ResourceId { get; }
        public string ConsumerId { get; }
        public string TargetId { get; }
        public int ConsumedAmount { get; }

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
