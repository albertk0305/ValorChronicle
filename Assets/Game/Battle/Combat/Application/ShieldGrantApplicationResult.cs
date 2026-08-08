namespace ValorChronicle.Battle.Combat.Application
{
    public sealed class ShieldGrantApplicationResult
    {
        internal ShieldGrantApplicationResult(
            long requestedShieldAmount,
            long grantedShieldAmount,
            long totalShieldBefore,
            long totalShieldAfter,
            long? createdShieldRuntimeId)
        {
            RequestedShieldAmount = requestedShieldAmount;
            GrantedShieldAmount = grantedShieldAmount;
            TotalShieldBefore = totalShieldBefore;
            TotalShieldAfter = totalShieldAfter;
            CreatedShieldRuntimeId = createdShieldRuntimeId;
        }

        public long RequestedShieldAmount { get; }
        public long GrantedShieldAmount { get; }
        public long TotalShieldBefore { get; }
        public long TotalShieldAfter { get; }
        public long? CreatedShieldRuntimeId { get; }
    }
}
