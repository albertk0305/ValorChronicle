using System;

namespace ValorChronicle.Battle.Combat.Attacks
{
    [Flags]
    public enum AttackTag
    {
        None = 0,
        Match3 = 1 << 0,
        Match4 = 1 << 1,
        Match5Plus = 1 << 2,
        Heavy = 1 << 3,
        MultiHit = 1 << 4,
        Elemental = 1 << 5,
        FixedDamage = 1 << 6,
        MaxHpRatio = 1 << 7
    }
}
