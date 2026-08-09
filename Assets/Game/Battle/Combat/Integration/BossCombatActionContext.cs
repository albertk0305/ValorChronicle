using System;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class BossCombatActionContext
    {
        internal BossCombatActionContext(
            BossBattleState boss,
            PartyBattleState party,
            int currentTurn,
            CombatActionIdSequence actionIds)
        {
            Boss = boss ?? throw new ArgumentNullException(nameof(boss));
            Party = party ?? throw new ArgumentNullException(nameof(party));
            if (currentTurn <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentTurn));
            }

            ActionIds = actionIds
                ?? throw new ArgumentNullException(nameof(actionIds));
            CurrentTurn = currentTurn;
        }

        public BossBattleState Boss { get; }
        public PartyBattleState Party { get; }
        public int CurrentTurn { get; }
        public CombatActionIdSequence ActionIds { get; }
    }
}
