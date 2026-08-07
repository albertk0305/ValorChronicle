using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Combat.State
{
    public sealed class ShieldAbsorptionResult
    {
        private readonly ReadOnlyCollection<long> depletedShieldRuntimeIds;

        internal ShieldAbsorptionResult(
            long incomingDamage,
            long absorbedDamage,
            long remainingDamage,
            long totalShieldBefore,
            long totalShieldAfter,
            IReadOnlyList<long> depletedShieldRuntimeIds)
        {
            var copiedIds = new long[depletedShieldRuntimeIds.Count];
            for (int index = 0; index < copiedIds.Length; index++)
            {
                copiedIds[index] = depletedShieldRuntimeIds[index];
            }

            IncomingDamage = incomingDamage;
            AbsorbedDamage = absorbedDamage;
            RemainingDamage = remainingDamage;
            TotalShieldBefore = totalShieldBefore;
            TotalShieldAfter = totalShieldAfter;
            this.depletedShieldRuntimeIds = Array.AsReadOnly(copiedIds);
        }

        public long IncomingDamage { get; }
        public long AbsorbedDamage { get; }
        public long RemainingDamage { get; }
        public long TotalShieldBefore { get; }
        public long TotalShieldAfter { get; }
        public IReadOnlyList<long> DepletedShieldRuntimeIds =>
            depletedShieldRuntimeIds;
    }
}
