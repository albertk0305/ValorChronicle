using System;

namespace ValorChronicle.Battle.Results
{
    public sealed class BattleDamageScoreAccumulator
    {
        public long DamageScore { get; private set; }

        public void Add(long appliedDamage)
        {
            if (appliedDamage < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(appliedDamage),
                    appliedDamage,
                    "Applied damage cannot be negative.");
            }

            DamageScore = checked(DamageScore + appliedDamage);
        }
    }
}
