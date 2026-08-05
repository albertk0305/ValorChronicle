using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValorChronicle.Battle.Flow
{
    public sealed class BattleContext
    {
        private readonly ReadOnlyCollection<ActiveAbilityRuntimeState>
            activeAbilities;

        public BattleContext(int turnLimit)
            : this(turnLimit, Array.Empty<int>())
        {
        }

        public BattleContext(
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

            var states = new ActiveAbilityRuntimeState[
                activeAbilityCooldowns.Count];
            for (int index = 0; index < states.Length; index++)
            {
                states[index] = new ActiveAbilityRuntimeState(
                    activeAbilityCooldowns[index]);
            }

            TurnLimit = turnLimit;
            CurrentTurn = 0;
            Phase = BattlePhase.NotStarted;
            Result = BattleResultKind.None;
            activeAbilities = Array.AsReadOnly(states);
        }

        public int TurnLimit { get; }
        public int CurrentTurn { get; internal set; }
        public BattlePhase Phase { get; internal set; }
        public BattleResultKind Result { get; internal set; }
        public IReadOnlyList<ActiveAbilityRuntimeState> ActiveAbilities =>
            activeAbilities;
    }
}
