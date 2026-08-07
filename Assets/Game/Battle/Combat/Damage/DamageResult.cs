namespace ValorChronicle.Battle.Combat.Damage
{
    public sealed class DamageResult
    {
        internal DamageResult(
            double finalAttack,
            double skillCoefficient,
            double comboMultiplier,
            double criticalMultiplier,
            double elementDamageMultiplier,
            double attackTypeDamageMultiplier,
            double dealtDamageMultiplier,
            double elementAffinityMultiplier,
            double targetTakenDamageMultiplier,
            double rawDamage,
            long finalDamage)
        {
            FinalAttack = finalAttack;
            SkillCoefficient = skillCoefficient;
            ComboMultiplier = comboMultiplier;
            CriticalMultiplier = criticalMultiplier;
            ElementDamageMultiplier = elementDamageMultiplier;
            AttackTypeDamageMultiplier = attackTypeDamageMultiplier;
            DealtDamageMultiplier = dealtDamageMultiplier;
            ElementAffinityMultiplier = elementAffinityMultiplier;
            TargetTakenDamageMultiplier = targetTakenDamageMultiplier;
            RawDamage = rawDamage;
            FinalDamage = finalDamage;
        }

        public double FinalAttack { get; }
        public double SkillCoefficient { get; }
        public double ComboMultiplier { get; }
        public double CriticalMultiplier { get; }
        public double ElementDamageMultiplier { get; }
        public double AttackTypeDamageMultiplier { get; }
        public double DealtDamageMultiplier { get; }
        public double ElementAffinityMultiplier { get; }
        public double TargetTakenDamageMultiplier { get; }
        public double RawDamage { get; }
        public long FinalDamage { get; }
    }
}
