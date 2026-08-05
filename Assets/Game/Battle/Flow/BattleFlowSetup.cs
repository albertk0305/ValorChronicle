using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Flow
{
    public sealed class BattleFlowSetup
    {
        private readonly ReadOnlyCollection<int> activeAbilityCooldowns;

        public BattleFlowSetup(int turnLimit)
            : this(turnLimit, Array.Empty<int>())
        {
        }

        public BattleFlowSetup(
            int turnLimit,
            IReadOnlyList<int> activeAbilityCooldowns)
        {
            if (turnLimit <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(turnLimit),
                    turnLimit,
                    "Turn limit must be positive.");
            }

            if (activeAbilityCooldowns == null)
            {
                throw new ArgumentNullException(
                    nameof(activeAbilityCooldowns));
            }

            var cooldowns = new int[activeAbilityCooldowns.Count];
            for (int index = 0; index < cooldowns.Length; index++)
            {
                int cooldown = activeAbilityCooldowns[index];
                if (cooldown < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(activeAbilityCooldowns),
                        cooldown,
                        "Active ability cooldowns cannot be negative.");
                }

                cooldowns[index] = cooldown;
            }

            TurnLimit = turnLimit;
            this.activeAbilityCooldowns = Array.AsReadOnly(cooldowns);
        }

        public int TurnLimit { get; }
        public IReadOnlyList<int> ActiveAbilityCooldowns =>
            activeAbilityCooldowns;
    }
}
