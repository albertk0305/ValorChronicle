namespace ValorChronicle.Battle.Combat.Application
{
    public sealed class BossDamageApplicationResult
    {
        internal BossDamageApplicationResult(
            long requestedDamage,
            long appliedDamage,
            long overkillDamage,
            long hpBefore,
            long hpAfter,
            bool wasDefeatedBefore,
            bool isDefeatedAfter)
        {
            RequestedDamage = requestedDamage;
            AppliedDamage = appliedDamage;
            OverkillDamage = overkillDamage;
            HpBefore = hpBefore;
            HpAfter = hpAfter;
            WasDefeatedBefore = wasDefeatedBefore;
            IsDefeatedAfter = isDefeatedAfter;
        }

        public long RequestedDamage { get; }
        public long AppliedDamage { get; }
        public long OverkillDamage { get; }
        public long HpBefore { get; }
        public long HpAfter { get; }
        public bool WasDefeatedBefore { get; }
        public bool IsDefeatedAfter { get; }
        public bool BecameDefeated =>
            !WasDefeatedBefore && IsDefeatedAfter;
    }
}
