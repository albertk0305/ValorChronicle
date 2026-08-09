using System;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class BattleTurnEndProcessor
    {
        private readonly PartyBattleState party;
        private readonly BossBattleState boss;

        public BattleTurnEndProcessor(
            PartyBattleState party,
            BossBattleState boss)
        {
            this.party = party
                ?? throw new ArgumentNullException(nameof(party));
            this.boss = boss
                ?? throw new ArgumentNullException(nameof(boss));
        }

        public int ProcessedTurnCount { get; private set; }

        public void ProcessTurnEnd()
        {
            for (int index = 0;
                index < party.Characters.Count;
                index++)
            {
                party.Characters[index].Effects.ProcessTurnEnd();
            }

            party.Effects.ProcessTurnEnd();
            boss.Effects.ProcessTurnEnd();
            party.Shields.ProcessTurnEnd();
            ProcessedTurnCount = checked(ProcessedTurnCount + 1);
        }
    }
}
