using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class BossActionPlan
    {
        private readonly ReadOnlyCollection<CombatAction> combatActions;

        public BossActionPlan(
            IReadOnlyList<CombatAction> actions,
            BattleBoardMutationCommand boardCommand = null)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            var copy = new CombatAction[actions.Count];
            for (int index = 0; index < actions.Count; index++)
            {
                copy[index] = actions[index]
                    ?? throw new ArgumentException(
                        "Boss action plans cannot contain null actions.",
                        nameof(actions));
            }

            combatActions = Array.AsReadOnly(copy);
            BoardCommand = boardCommand;
        }

        public IReadOnlyList<CombatAction> CombatActions => combatActions;
        public BattleBoardMutationCommand BoardCommand { get; }
    }
}
