namespace ValorChronicle.Battle.Combat.State
{
    public sealed class ResourceAddResult
    {
        internal ResourceAddResult(
            string resourceId,
            int requestedAmount,
            int addedAmount,
            int overflowAmount,
            int amountBefore,
            int amountAfter)
        {
            ResourceId = resourceId;
            RequestedAmount = requestedAmount;
            AddedAmount = addedAmount;
            OverflowAmount = overflowAmount;
            AmountBefore = amountBefore;
            AmountAfter = amountAfter;
        }

        public string ResourceId { get; }
        public int RequestedAmount { get; }
        public int AddedAmount { get; }
        public int OverflowAmount { get; }
        public int AmountBefore { get; }
        public int AmountAfter { get; }
    }
}
