using System;

namespace ValorChronicle.Battle.Combat.Damage
{
    public static class BossDamageCalculator
    {
        public const double MaximumPartyDamageReductionRate = 0.70d;
        public const double MinimumPartyTakenDamageMultiplier = 0.30d;

        public static BossDamageResult Calculate(BossDamageContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            double finalAttack = context.BaseAttack
                * (1d
                    + (context.AttackIncreaseRateSum
                        - context.AttackReductionRateSum));
            double bossDealtDamageMultiplier =
                1d
                + (context.DealtDamageIncreaseRateSum
                    - context.DealtDamageReductionRateSum);
            double effectivePartyDamageReductionRate = Math.Min(
                context.PartyDamageReductionRateSum,
                MaximumPartyDamageReductionRate);
            double partyTakenDamageMultiplier = Math.Max(
                MinimumPartyTakenDamageMultiplier,
                1d
                    + (context.PartyTakenDamageIncreaseRateSum
                        - effectivePartyDamageReductionRate));

            double rawDamage = finalAttack
                * context.AttackCoefficient
                * bossDealtDamageMultiplier
                * partyTakenDamageMultiplier;
            long finalDamageBeforeShield = FloorToLong(rawDamage);

            return new BossDamageResult(
                finalAttack,
                context.AttackCoefficient,
                bossDealtDamageMultiplier,
                effectivePartyDamageReductionRate,
                partyTakenDamageMultiplier,
                rawDamage,
                finalDamageBeforeShield);
        }

        private static long FloorToLong(double rawDamage)
        {
            if (double.IsNaN(rawDamage)
                || double.IsInfinity(rawDamage)
                || rawDamage < 0d)
            {
                throw new OverflowException(
                    "Calculated damage must be finite and non-negative.");
            }

            return checked((long)Math.Floor(rawDamage));
        }
    }
}
