using System;

namespace ValorChronicle.Battle.Combat.Damage
{
    public sealed class BossDamageContext
    {
        public BossDamageContext(
            double baseAttack,
            double attackIncreaseRateSum,
            double attackReductionRateSum,
            double attackCoefficient,
            double dealtDamageIncreaseRateSum,
            double dealtDamageReductionRateSum,
            double partyTakenDamageIncreaseRateSum,
            double partyDamageReductionRateSum)
        {
            ValidateNonNegative(baseAttack, nameof(baseAttack));
            ValidateNonNegative(attackIncreaseRateSum, nameof(attackIncreaseRateSum));
            ValidateNonNegative(attackReductionRateSum, nameof(attackReductionRateSum));
            ValidateNonNegative(attackCoefficient, nameof(attackCoefficient));
            ValidateNonNegative(dealtDamageIncreaseRateSum, nameof(dealtDamageIncreaseRateSum));
            ValidateNonNegative(dealtDamageReductionRateSum, nameof(dealtDamageReductionRateSum));
            ValidateNonNegative(partyTakenDamageIncreaseRateSum, nameof(partyTakenDamageIncreaseRateSum));
            ValidateNonNegative(partyDamageReductionRateSum, nameof(partyDamageReductionRateSum));

            ValidateMultiplier(
                1d + (attackIncreaseRateSum - attackReductionRateSum),
                nameof(attackReductionRateSum));
            ValidateMultiplier(
                1d
                    + (dealtDamageIncreaseRateSum
                        - dealtDamageReductionRateSum),
                nameof(dealtDamageReductionRateSum));

            BaseAttack = baseAttack;
            AttackIncreaseRateSum = attackIncreaseRateSum;
            AttackReductionRateSum = attackReductionRateSum;
            AttackCoefficient = attackCoefficient;
            DealtDamageIncreaseRateSum = dealtDamageIncreaseRateSum;
            DealtDamageReductionRateSum = dealtDamageReductionRateSum;
            PartyTakenDamageIncreaseRateSum = partyTakenDamageIncreaseRateSum;
            PartyDamageReductionRateSum = partyDamageReductionRateSum;
        }

        public double BaseAttack { get; }
        public double AttackIncreaseRateSum { get; }
        public double AttackReductionRateSum { get; }
        public double AttackCoefficient { get; }
        public double DealtDamageIncreaseRateSum { get; }
        public double DealtDamageReductionRateSum { get; }
        public double PartyTakenDamageIncreaseRateSum { get; }
        public double PartyDamageReductionRateSum { get; }

        private static void ValidateNonNegative(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Value must be finite and non-negative.");
            }
        }

        private static void ValidateMultiplier(double multiplier, string parameterName)
        {
            if (double.IsNaN(multiplier)
                || double.IsInfinity(multiplier)
                || multiplier < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    multiplier,
                    "The resulting multiplier must be finite and non-negative.");
            }
        }
    }
}
