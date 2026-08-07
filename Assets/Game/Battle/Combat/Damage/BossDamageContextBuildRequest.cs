using System;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Damage
{
    public sealed class BossDamageContextBuildRequest
    {
        public BossDamageContextBuildRequest(
            BossBattleState boss,
            PartyBattleState party,
            double attackCoefficient,
            AttackTag attackTags)
        {
            Boss = boss ?? throw new ArgumentNullException(nameof(boss));
            Party = party ?? throw new ArgumentNullException(nameof(party));
            if (double.IsNaN(attackCoefficient)
                || double.IsInfinity(attackCoefficient)
                || attackCoefficient < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(attackCoefficient),
                    attackCoefficient,
                    "Attack coefficient must be finite and non-negative.");
            }

            ValidateAttackTags(attackTags);
            AttackCoefficient = attackCoefficient;
            AttackTags = attackTags;
        }

        public BossBattleState Boss { get; }
        public PartyBattleState Party { get; }
        public double AttackCoefficient { get; }
        public AttackTag AttackTags { get; }

        private static void ValidateAttackTags(AttackTag attackTags)
        {
            const AttackTag allAttackTags =
                AttackTag.Match3
                | AttackTag.Match4
                | AttackTag.Match5Plus
                | AttackTag.Heavy
                | AttackTag.MultiHit
                | AttackTag.Elemental
                | AttackTag.FixedDamage
                | AttackTag.MaxHpRatio;
            if ((attackTags & ~allAttackTags) != 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(attackTags),
                    attackTags,
                    "Attack tags contain undefined flags.");
            }
        }
    }
}
