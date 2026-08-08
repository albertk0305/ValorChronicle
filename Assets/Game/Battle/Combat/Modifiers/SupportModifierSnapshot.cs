namespace ValorChronicle.Battle.Combat.Modifiers
{
    public sealed class SupportModifierSnapshot
    {
        internal SupportModifierSnapshot(
            double healingIncreaseRateSum,
            double shieldAmountIncreaseRateSum)
        {
            HealingIncreaseRateSum = healingIncreaseRateSum;
            ShieldAmountIncreaseRateSum = shieldAmountIncreaseRateSum;
        }

        public double HealingIncreaseRateSum { get; }
        public double ShieldAmountIncreaseRateSum { get; }
    }
}
