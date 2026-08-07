namespace ValorChronicle.Battle.Combat.Application
{
    public sealed class PartyDamageApplicationResult
    {
        internal PartyDamageApplicationResult(
            long requestedDamage,
            long shieldAbsorbedDamage,
            long hpDamage,
            long overkillDamage,
            long hpBefore,
            long hpAfter,
            long totalShieldBefore,
            long totalShieldAfter,
            bool wasIncapacitatedBefore,
            bool isIncapacitatedAfter)
        {
            RequestedDamage = requestedDamage;
            ShieldAbsorbedDamage = shieldAbsorbedDamage;
            HpDamage = hpDamage;
            OverkillDamage = overkillDamage;
            HpBefore = hpBefore;
            HpAfter = hpAfter;
            TotalShieldBefore = totalShieldBefore;
            TotalShieldAfter = totalShieldAfter;
            WasIncapacitatedBefore = wasIncapacitatedBefore;
            IsIncapacitatedAfter = isIncapacitatedAfter;
        }

        public long RequestedDamage { get; }
        public long ShieldAbsorbedDamage { get; }
        public long HpDamage { get; }
        public long OverkillDamage { get; }
        public long HpBefore { get; }
        public long HpAfter { get; }
        public long TotalShieldBefore { get; }
        public long TotalShieldAfter { get; }
        public bool WasIncapacitatedBefore { get; }
        public bool IsIncapacitatedAfter { get; }
        public bool BecameIncapacitated =>
            !WasIncapacitatedBefore && IsIncapacitatedAfter;
    }
}
