using System;

namespace ValorChronicle.Battle.Combat.Damage
{
    public static class DamageCalculator
    {
        public static DamageResult Calculate(DamageContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            double finalAttack = context.BaseAttack
                * (1d + context.AttackIncreaseRateSum);
            double comboMultiplier = context.AppliesCombo
                ? ComboMultiplierResolver.Resolve(context.FinalComboCount)
                : 1d;
            double criticalMultiplier =
                context.CanCritical && context.IsCritical
                    ? context.CriticalDamageMultiplier
                    : 1d;
            double elementDamageMultiplier =
                1d + context.ElementDamageIncreaseRateSum;
            double attackTypeDamageMultiplier =
                1d + context.AttackTypeDamageIncreaseRateSum;
            double dealtDamageMultiplier =
                1d + context.DealtDamageIncreaseRateSum;
            double elementAffinityMultiplier =
                ElementAffinityResolver.Resolve(
                    context.AttackElement,
                    context.TargetElement);
            double targetTakenDamageMultiplier =
                1d
                + (context.TargetTakenDamageIncreaseRateSum
                    - context.TargetTakenDamageReductionRateSum);

            double rawDamage = finalAttack
                * context.SkillCoefficient
                * comboMultiplier
                * criticalMultiplier
                * elementDamageMultiplier
                * attackTypeDamageMultiplier
                * dealtDamageMultiplier
                * elementAffinityMultiplier
                * targetTakenDamageMultiplier;
            long finalDamage = FloorToLong(rawDamage);

            return new DamageResult(
                finalAttack,
                context.SkillCoefficient,
                comboMultiplier,
                criticalMultiplier,
                elementDamageMultiplier,
                attackTypeDamageMultiplier,
                dealtDamageMultiplier,
                elementAffinityMultiplier,
                targetTakenDamageMultiplier,
                rawDamage,
                finalDamage);
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
