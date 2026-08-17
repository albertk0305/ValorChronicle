using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class MatchEventCharacterCombatExecutor
    {
        private readonly PartyBattleState party;
        private readonly BossBattleState boss;
        private readonly CombatActionExecutor executor;
        private readonly MatchEventCombatActionFactory actionFactory;

        public MatchEventCharacterCombatExecutor(
            PartyBattleState party,
            BossBattleState boss,
            CombatActionExecutor executor,
            MatchEventCombatActionFactory actionFactory)
        {
            this.party = party
                ?? throw new ArgumentNullException(nameof(party));
            this.boss = boss
                ?? throw new ArgumentNullException(nameof(boss));
            this.executor = executor
                ?? throw new ArgumentNullException(nameof(executor));
            this.actionFactory = actionFactory
                ?? throw new ArgumentNullException(nameof(actionFactory));
        }

        public CombatActionExecutionResult Execute(
            MatchEventExecution execution)
        {
            MatchEventCharacterCombatExecutionSession session =
                BeginExecution(execution);
            while (session.TryGetNextAction(out _, out _))
            {
                session.TryExecuteNext(out _);
            }

            return session.Result;
        }

        public MatchEventCharacterCombatExecutionSession BeginExecution(
            MatchEventExecution execution)
        {
            if (execution == null)
            {
                throw new ArgumentNullException(nameof(execution));
            }

            return new MatchEventCharacterCombatExecutionSession(
                execution,
                party,
                boss,
                executor,
                actionFactory);
        }
    }
}
