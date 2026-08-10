using System;

namespace ValorChronicle.Bosses.Kragmor
{
    public sealed class KragmorBossIntent
    {
        internal KragmorBossIntent(
            KragmorActionKind actionKind,
            bool hasDirectDamage,
            bool isHeavy,
            double damageCoefficient)
        {
            if (!Enum.IsDefined(typeof(KragmorActionKind), actionKind))
            {
                throw new ArgumentOutOfRangeException(nameof(actionKind));
            }

            if (double.IsNaN(damageCoefficient)
                || double.IsInfinity(damageCoefficient)
                || damageCoefficient < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(damageCoefficient));
            }

            if (!hasDirectDamage && damageCoefficient != 0d)
            {
                throw new ArgumentException(
                    "A non-damaging intent must have a zero coefficient.",
                    nameof(damageCoefficient));
            }

            ActionKind = actionKind;
            HasDirectDamage = hasDirectDamage;
            IsHeavy = isHeavy;
            DamageCoefficient = damageCoefficient;
        }

        public KragmorActionKind ActionKind { get; }
        public bool HasDirectDamage { get; }
        public bool IsHeavy { get; }
        public double DamageCoefficient { get; }
    }
}
