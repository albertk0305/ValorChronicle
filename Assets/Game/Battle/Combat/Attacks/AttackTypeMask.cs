using System;

namespace ValorChronicle.Battle.Combat.Attacks
{
    [Flags]
    public enum AttackTypeMask
    {
        None = 0,
        Match = 1 << 0,
        Active = 1 << 1,
        Additional = 1 << 2,
        Chase = 1 << 3,
        DamageOverTime = 1 << 4,
        All = Match
            | Active
            | Additional
            | Chase
            | DamageOverTime
    }
}
