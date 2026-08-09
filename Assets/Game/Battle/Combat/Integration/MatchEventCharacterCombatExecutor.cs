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
            if (execution == null)
            {
                throw new ArgumentNullException(nameof(execution));
            }

            var results = new List<CombatActionResult>();
            bool stoppedEarly = false;
            bool clearedRemainingActions = false;
            for (int characterIndex = 0;
                characterIndex < party.Characters.Count;
                characterIndex++)
            {
                CharacterBattleState character =
                    party.Characters[characterIndex];
                if (character.Element != execution.MatchEvent.Element)
                {
                    continue;
                }

                IReadOnlyList<CombatAction> rootActions =
                    actionFactory.CreateCharacterRootActions(
                        execution,
                        character);
                if (rootActions.Count > 0)
                {
                    CombatActionExecutionResult characterResult =
                        executor.Execute(new CombatActionQueue(rootActions));
                    for (int resultIndex = 0;
                        resultIndex < characterResult.ActionResults.Count;
                        resultIndex++)
                    {
                        results.Add(
                            characterResult.ActionResults[resultIndex]);
                    }

                    stoppedEarly |= characterResult.StoppedEarly;
                    clearedRemainingActions |=
                        characterResult.ClearedRemainingActions;
                }

                if (boss.IsDefeated || party.IsIncapacitated)
                {
                    stoppedEarly = true;
                    clearedRemainingActions |= HasMatchingCharacterAfter(
                        characterIndex,
                        execution);
                    break;
                }
            }

            if (results.Count == 0)
            {
                return null;
            }

            return new CombatActionExecutionResult(
                results,
                stoppedEarly,
                boss.IsDefeated,
                party.IsIncapacitated,
                clearedRemainingActions);
        }

        private bool HasMatchingCharacterAfter(
            int characterIndex,
            MatchEventExecution execution)
        {
            for (int index = characterIndex + 1;
                index < party.Characters.Count;
                index++)
            {
                if (party.Characters[index].Element
                    == execution.MatchEvent.Element)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
