using System;

namespace ValorChronicle.Relics.Stats
{
    public sealed class RelicStatBonusSummary
    {
        public RelicStatBonusSummary(
            double maxHpIncreaseRateSum,
            double attackIncreaseRateSum)
        {
            ValidateRate(
                maxHpIncreaseRateSum,
                nameof(maxHpIncreaseRateSum));
            ValidateRate(
                attackIncreaseRateSum,
                nameof(attackIncreaseRateSum));
            MaxHpIncreaseRateSum = maxHpIncreaseRateSum;
            AttackIncreaseRateSum = attackIncreaseRateSum;
        }

        public static RelicStatBonusSummary None { get; } =
            new RelicStatBonusSummary(0d, 0d);

        public double MaxHpIncreaseRateSum { get; }
        public double AttackIncreaseRateSum { get; }

        private static void ValidateRate(double rate, string parameterName)
        {
            if (double.IsNaN(rate)
                || double.IsInfinity(rate)
                || rate < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    rate,
                    "A relic stat increase rate must be finite and "
                        + "non-negative.");
            }
        }
    }
}
