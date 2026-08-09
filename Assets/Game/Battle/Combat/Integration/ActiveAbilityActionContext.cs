using System;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class ActiveAbilityActionContext
    {
        internal ActiveAbilityActionContext(
            ActiveAbilityBinding binding,
            CharacterBattleState character,
            PartyBattleState party,
            BossBattleState boss,
            CombatActionIdSequence actionIds)
        {
            Binding = binding
                ?? throw new ArgumentNullException(nameof(binding));
            Character = character
                ?? throw new ArgumentNullException(nameof(character));
            Party = party ?? throw new ArgumentNullException(nameof(party));
            Boss = boss ?? throw new ArgumentNullException(nameof(boss));
            ActionIds = actionIds
                ?? throw new ArgumentNullException(nameof(actionIds));
        }

        public ActiveAbilityBinding Binding { get; }
        public CharacterBattleState Character { get; }
        public PartyBattleState Party { get; }
        public BossBattleState Boss { get; }
        public CombatActionIdSequence ActionIds { get; }
    }
}
