namespace ValorChronicle.Battle.Combat.Damage
{
    public sealed class BossDamageResult
    {
        internal BossDamageResult(
            double finalAttack,
            double attackCoefficient,
            double bossDealtDamageMultiplier,
            double effectivePartyDamageReductionRate,
            double partyTakenDamageMultiplier,
            double rawDamage,
            long finalDamageBeforeShield)
        {
            FinalAttack = finalAttack;
            AttackCoefficient = attackCoefficient;
            BossDealtDamageMultiplier = bossDealtDamageMultiplier;
            EffectivePartyDamageReductionRate = effectivePartyDamageReductionRate;
            PartyTakenDamageMultiplier = partyTakenDamageMultiplier;
            RawDamage = rawDamage;
            FinalDamageBeforeShield = finalDamageBeforeShield;
        }

        public double FinalAttack { get; }
        public double AttackCoefficient { get; }
        public double BossDealtDamageMultiplier { get; }
        public double EffectivePartyDamageReductionRate { get; }
        public double PartyTakenDamageMultiplier { get; }
        public double RawDamage { get; }
        public long FinalDamageBeforeShield { get; }
    }
}
