namespace ValorChronicle.Battle.Combat.Application
{
    public sealed class PartyHealingApplicationResult
    {
        internal PartyHealingApplicationResult(
            long requestedHealing,
            long appliedHealing,
            long overhealAmount,
            long hpBefore,
            long hpAfter,
            bool wasFullBefore,
            bool isFullAfter)
        {
            RequestedHealing = requestedHealing;
            AppliedHealing = appliedHealing;
            OverhealAmount = overhealAmount;
            HpBefore = hpBefore;
            HpAfter = hpAfter;
            WasFullBefore = wasFullBefore;
            IsFullAfter = isFullAfter;
        }

        public long RequestedHealing { get; }
        public long AppliedHealing { get; }
        public long OverhealAmount { get; }
        public long HpBefore { get; }
        public long HpAfter { get; }
        public bool WasFullBefore { get; }
        public bool IsFullAfter { get; }
    }
}
