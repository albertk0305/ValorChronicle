using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;

namespace ValorChronicle.Battle.Combat.Integration
{
    public interface IPreMatchQueueActionProvider
    {
        IReadOnlyList<CombatAction> CreateRootActions(
            PreMatchQueueActionContext context);
    }

    public sealed class PreMatchQueueActionContext
    {
        public PreMatchQueueActionContext(
            CharacterBattleState character,
            PartyBattleState party,
            BossBattleState boss,
            int playerTurnNumber,
            long matchQueueId,
            IReadOnlyList<MatchEvent> matchEvents,
            CombatActionIdSequence actionIds)
        {
            Character = character
                ?? throw new ArgumentNullException(nameof(character));
            Party = party
                ?? throw new ArgumentNullException(nameof(party));
            Boss = boss
                ?? throw new ArgumentNullException(nameof(boss));
            if (playerTurnNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playerTurnNumber));
            }

            if (matchQueueId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(matchQueueId));
            }

            PlayerTurnNumber = playerTurnNumber;
            MatchQueueId = matchQueueId;
            MatchEvents = matchEvents
                ?? throw new ArgumentNullException(nameof(matchEvents));
            ActionIds = actionIds
                ?? throw new ArgumentNullException(nameof(actionIds));
        }

        public CharacterBattleState Character { get; }
        public PartyBattleState Party { get; }
        public BossBattleState Boss { get; }
        public int PlayerTurnNumber { get; }
        public long MatchQueueId { get; }
        public IReadOnlyList<MatchEvent> MatchEvents { get; }
        public CombatActionIdSequence ActionIds { get; }
    }

    public sealed class PreMatchQueueActionProviderBinding
    {
        public PreMatchQueueActionProviderBinding(
            CharacterBattleState character,
            IPreMatchQueueActionProvider provider)
        {
            Character = character
                ?? throw new ArgumentNullException(nameof(character));
            Provider = provider
                ?? throw new ArgumentNullException(nameof(provider));
        }

        public CharacterBattleState Character { get; }
        public IPreMatchQueueActionProvider Provider { get; }
    }
}
