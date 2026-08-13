using System;

namespace ValorChronicle.Bosses.Kragmor
{
    public sealed class KragmorBossIntent
    {
        internal KragmorBossIntent(
            KragmorActionKind actionKind,
            string skillId,
            bool hasDirectDamage,
            bool isHeavy,
            double damageCoefficient)
        {
            if (!Enum.IsDefined(typeof(KragmorActionKind), actionKind))
            {
                throw new ArgumentOutOfRangeException(nameof(actionKind));
            }

            if (string.IsNullOrWhiteSpace(skillId))
            {
                throw new ArgumentException(
                    "Skill ID cannot be null or whitespace.",
                    nameof(skillId));
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
            SkillId = skillId;
            HasDirectDamage = hasDirectDamage;
            IsHeavy = isHeavy;
            DamageCoefficient = damageCoefficient;
        }

        public KragmorActionKind ActionKind { get; }
        public string SkillId { get; }
        public bool HasDirectDamage { get; }
        public bool IsHeavy { get; }
        public double DamageCoefficient { get; }
    }

    public sealed class KragmorBossIntentPreview
    {
        internal KragmorBossIntentPreview(
            int turnsUntilAction,
            KragmorBossIntent intent)
        {
            if (turnsUntilAction <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(turnsUntilAction),
                    turnsUntilAction,
                    "Turns until action must be positive.");
            }

            TurnsUntilAction = turnsUntilAction;
            Intent = intent
                ?? throw new ArgumentNullException(nameof(intent));
        }

        public int TurnsUntilAction { get; }
        public KragmorBossIntent Intent { get; }
    }
}
