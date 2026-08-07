namespace ValorChronicle.Battle.Combat.Modifiers
{
    public sealed class AllyDamageModifierSnapshot
    {
        internal AllyDamageModifierSnapshot(
            double attackIncreaseRateSum,
            double attackReductionRateSum,
            double elementDamageIncreaseRateSum,
            double attackTypeDamageIncreaseRateSum,
            double dealtDamageIncreaseRateSum,
            double dealtDamageReductionRateSum,
            double targetTakenDamageIncreaseRateSum,
            double targetTakenDamageReductionRateSum,
            double criticalChanceIncreaseRateSum,
            double criticalDamageIncreaseRateSum)
        {
            AttackIncreaseRateSum = attackIncreaseRateSum;
            AttackReductionRateSum = attackReductionRateSum;
            ElementDamageIncreaseRateSum =
                elementDamageIncreaseRateSum;
            AttackTypeDamageIncreaseRateSum =
                attackTypeDamageIncreaseRateSum;
            DealtDamageIncreaseRateSum = dealtDamageIncreaseRateSum;
            DealtDamageReductionRateSum = dealtDamageReductionRateSum;
            TargetTakenDamageIncreaseRateSum =
                targetTakenDamageIncreaseRateSum;
            TargetTakenDamageReductionRateSum =
                targetTakenDamageReductionRateSum;
            CriticalChanceIncreaseRateSum =
                criticalChanceIncreaseRateSum;
            CriticalDamageIncreaseRateSum =
                criticalDamageIncreaseRateSum;
        }

        public double AttackIncreaseRateSum { get; }
        public double AttackReductionRateSum { get; }
        public double ElementDamageIncreaseRateSum { get; }
        public double AttackTypeDamageIncreaseRateSum { get; }
        public double DealtDamageIncreaseRateSum { get; }
        public double DealtDamageReductionRateSum { get; }
        public double TargetTakenDamageIncreaseRateSum { get; }
        public double TargetTakenDamageReductionRateSum { get; }
        public double CriticalChanceIncreaseRateSum { get; }
        public double CriticalDamageIncreaseRateSum { get; }
    }
}
