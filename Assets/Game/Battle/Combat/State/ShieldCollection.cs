using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Combat.State
{
    public sealed class ShieldCollection
    {
        private readonly List<ShieldInstance> activeShields =
            new List<ShieldInstance>();
        private readonly ReadOnlyCollection<ShieldInstance>
            readOnlyActiveShields;

        public ShieldCollection()
        {
            readOnlyActiveShields = activeShields.AsReadOnly();
        }

        public IReadOnlyList<ShieldInstance> ActiveShields =>
            readOnlyActiveShields;

        public long TotalShield
        {
            get
            {
                long total = 0;
                for (int index = 0; index < activeShields.Count; index++)
                {
                    total = checked(
                        total + activeShields[index].CurrentAmount);
                }

                return total;
            }
        }

        public void Add(ShieldInstance shield)
        {
            if (shield == null)
            {
                throw new ArgumentNullException(nameof(shield));
            }

            if (shield.IsDepleted || shield.IsExpired)
            {
                throw new ArgumentException(
                    "Only an active shield can be added.",
                    nameof(shield));
            }

            for (int index = 0; index < activeShields.Count; index++)
            {
                ShieldInstance existing = activeShields[index];
                if (existing.RuntimeId == shield.RuntimeId)
                {
                    throw new ArgumentException(
                        $"Duplicate shield runtime ID: {shield.RuntimeId}.",
                        nameof(shield));
                }

                if (existing.CreationOrder == shield.CreationOrder)
                {
                    throw new ArgumentException(
                        $"Duplicate shield creation order: "
                            + $"{shield.CreationOrder}.",
                        nameof(shield));
                }
            }

            checked
            {
                _ = TotalShield + shield.CurrentAmount;
            }

            shield.RegisterWithCollection();
            activeShields.Add(shield);
        }

        public ShieldAbsorptionResult Absorb(long incomingDamage)
        {
            if (incomingDamage < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(incomingDamage),
                    incomingDamage,
                    "Incoming damage cannot be negative.");
            }

            long totalShieldBefore = TotalShield;
            long remainingDamage = incomingDamage;
            var depletedRuntimeIds = new List<long>();
            var consumptionOrder =
                new List<ShieldInstance>(activeShields);
            consumptionOrder.Sort(CompareConsumptionOrder);

            for (int index = 0;
                index < consumptionOrder.Count && remainingDamage > 0;
                index++)
            {
                ShieldInstance shield = consumptionOrder[index];
                long absorbed = shield.Absorb(remainingDamage);
                remainingDamage -= absorbed;
                if (shield.IsDepleted)
                {
                    depletedRuntimeIds.Add(shield.RuntimeId);
                }
            }

            activeShields.RemoveAll(shield => shield.IsDepleted);
            long absorbedDamage = incomingDamage - remainingDamage;
            return new ShieldAbsorptionResult(
                incomingDamage,
                absorbedDamage,
                remainingDamage,
                totalShieldBefore,
                TotalShield,
                depletedRuntimeIds);
        }

        /// <summary>
        /// Decrements finite shield durations and removes expired shields.
        /// Flow should call this after the boss action at player turn end.
        /// </summary>
        public void ProcessTurnEnd()
        {
            for (int index = 0; index < activeShields.Count; index++)
            {
                activeShields[index].ProcessTurnEnd();
            }

            activeShields.RemoveAll(shield => shield.IsExpired);
        }

        private static int CompareConsumptionOrder(
            ShieldInstance left,
            ShieldInstance right)
        {
            if (left.RemainingTurns.HasValue
                && right.RemainingTurns.HasValue)
            {
                int durationComparison = left.RemainingTurns.Value.CompareTo(
                    right.RemainingTurns.Value);
                if (durationComparison != 0)
                {
                    return durationComparison;
                }
            }
            else if (left.RemainingTurns.HasValue)
            {
                return -1;
            }
            else if (right.RemainingTurns.HasValue)
            {
                return 1;
            }

            return left.CreationOrder.CompareTo(right.CreationOrder);
        }
    }
}
