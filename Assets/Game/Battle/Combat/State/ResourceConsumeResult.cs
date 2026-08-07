namespace ValorChronicle.Battle.Combat.State
{
    public sealed class ResourceConsumeResult
    {
        internal ResourceConsumeResult(
            string resourceId,
            int requestedAmount,
            int consumedAmount,
            int amountBefore,
            int amountAfter)
        {
            ResourceId = resourceId;
            RequestedAmount = requestedAmount;
            ConsumedAmount = consumedAmount;
            AmountBefore = amountBefore;
            AmountAfter = amountAfter;
        }

        public string ResourceId { get; }
        public int RequestedAmount { get; }
        public int ConsumedAmount { get; }
        public int AmountBefore { get; }
        public int AmountAfter { get; }
    }
}
