namespace ValorChronicle.Battle.Combat.Modifiers
{
    public sealed class BossAttackModifierSnapshot
    {
        internal BossAttackModifierSnapshot(
            double attackIncreaseRateSum,
            double attackReductionRateSum,
            double dealtDamageIncreaseRateSum,
            double dealtDamageReductionRateSum,
            double partyTakenDamageIncreaseRateSum,
            double partyDamageReductionRateSum)
        {
            AttackIncreaseRateSum = attackIncreaseRateSum;
            AttackReductionRateSum = attackReductionRateSum;
            DealtDamageIncreaseRateSum = dealtDamageIncreaseRateSum;
            DealtDamageReductionRateSum = dealtDamageReductionRateSum;
            PartyTakenDamageIncreaseRateSum =
                partyTakenDamageIncreaseRateSum;
            PartyDamageReductionRateSum = partyDamageReductionRateSum;
        }

        public double AttackIncreaseRateSum { get; }
        public double AttackReductionRateSum { get; }
        public double DealtDamageIncreaseRateSum { get; }
        public double DealtDamageReductionRateSum { get; }
        public double PartyTakenDamageIncreaseRateSum { get; }
        public double PartyDamageReductionRateSum { get; }
    }
}
