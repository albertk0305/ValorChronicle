using System;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Combat.State
{
    public sealed class BossBattleState
    {
        public BossBattleState(
            string bossId,
            ElementType element,
            long maxHp,
            double attack)
        {
            if (string.IsNullOrWhiteSpace(bossId))
            {
                throw new ArgumentException(
                    "Boss ID cannot be null or whitespace.",
                    nameof(bossId));
            }

            if (!Enum.IsDefined(typeof(ElementType), element))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(element),
                    element,
                    "Element must be a defined value.");
            }

            if (maxHp <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxHp),
                    maxHp,
                    "Maximum HP must be positive.");
            }

            if (double.IsNaN(attack)
                || double.IsInfinity(attack)
                || attack < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(attack),
                    attack,
                    "Attack must be finite and non-negative.");
            }

            BossId = bossId;
            Element = element;
            MaxHp = maxHp;
            CurrentHp = maxHp;
            Attack = attack;
            Effects = new EffectCollection();
            Resources = new ResourceCollection();
            Marks = new MarkCollection();
        }

        public string BossId { get; }
        public ElementType Element { get; }
        public long MaxHp { get; }
        public long CurrentHp { get; private set; }
        public double Attack { get; }
        public EffectCollection Effects { get; }
        public ResourceCollection Resources { get; }
        public MarkCollection Marks { get; }
        public bool IsDefeated => CurrentHp == 0;

        internal long ApplyDamage(long damage)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage));
            }

            long appliedDamage = Math.Min(damage, CurrentHp);
            CurrentHp -= appliedDamage;
            return appliedDamage;
        }
    }
}
