namespace ValorChronicle.Battle.Combat.Shields
{
    public sealed class ShieldGenerationResult
    {
        internal ShieldGenerationResult(
            long sourceMaxHp,
            double shieldCoefficient,
            double comboMultiplier,
            double shieldAmountIncreaseMultiplier,
            double rawShieldAmount,
            long finalShieldAmount)
        {
            SourceMaxHp = sourceMaxHp;
            ShieldCoefficient = shieldCoefficient;
            ComboMultiplier = comboMultiplier;
            ShieldAmountIncreaseMultiplier =
                shieldAmountIncreaseMultiplier;
            RawShieldAmount = rawShieldAmount;
            FinalShieldAmount = finalShieldAmount;
        }

        public long SourceMaxHp { get; }
        public double ShieldCoefficient { get; }
        public double ComboMultiplier { get; }
        public double ShieldAmountIncreaseMultiplier { get; }
        public double RawShieldAmount { get; }
        public long FinalShieldAmount { get; }
    }
}
