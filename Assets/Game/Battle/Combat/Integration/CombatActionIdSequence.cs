using System;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class CombatActionIdSequence
    {
        private long lastIssuedId;

        public long LastIssuedId => lastIssuedId;

        public long Next()
        {
            return checked(++lastIssuedId);
        }

        public bool HasIssued(long actionId)
        {
            return actionId > 0 && actionId <= lastIssuedId;
        }
    }
}
