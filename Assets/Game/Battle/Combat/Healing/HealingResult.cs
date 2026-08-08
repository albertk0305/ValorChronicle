namespace ValorChronicle.Battle.Combat.Healing
{
    public sealed class HealingResult
    {
        internal HealingResult(
            long sourceMaxHp,
            double healingCoefficient,
            double comboMultiplier,
            double healingIncreaseMultiplier,
            double rawHealing,
            long finalHealing)
        {
            SourceMaxHp = sourceMaxHp;
            HealingCoefficient = healingCoefficient;
            ComboMultiplier = comboMultiplier;
            HealingIncreaseMultiplier = healingIncreaseMultiplier;
            RawHealing = rawHealing;
            FinalHealing = finalHealing;
        }

        public long SourceMaxHp { get; }
        public double HealingCoefficient { get; }
        public double ComboMultiplier { get; }
        public double HealingIncreaseMultiplier { get; }
        public double RawHealing { get; }
        public long FinalHealing { get; }
    }
}
