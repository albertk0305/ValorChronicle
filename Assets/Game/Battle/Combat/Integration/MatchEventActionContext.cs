using System;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class MatchEventActionContext
    {
        internal MatchEventActionContext(
            CharacterBattleState character,
            MatchEventExecution execution,
            AttackTag matchAttackTag,
            PartyBattleState party,
            BossBattleState boss,
            CombatActionIdSequence actionIds)
        {
            Character = character
                ?? throw new ArgumentNullException(nameof(character));
            Execution = execution
                ?? throw new ArgumentNullException(nameof(execution));
            Party = party ?? throw new ArgumentNullException(nameof(party));
            Boss = boss ?? throw new ArgumentNullException(nameof(boss));
            ActionIds = actionIds
                ?? throw new ArgumentNullException(nameof(actionIds));
            MatchAttackTag = matchAttackTag;
        }

        public CharacterBattleState Character { get; }
        public MatchEventExecution Execution { get; }
        public MatchEvent MatchEvent => Execution.MatchEvent;
        public int FinalComboCount => Execution.FinalComboCount;
        public AttackTag MatchAttackTag { get; }
        public PartyBattleState Party { get; }
        public BossBattleState Boss { get; }
        public CombatActionIdSequence ActionIds { get; }
    }
}
