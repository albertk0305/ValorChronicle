using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class MatchEventCharacterCombatExecutionSession
    {
        private readonly PartyBattleState party;
        private readonly BossBattleState boss;
        private readonly CombatActionExecutor executor;
        private readonly MatchEventCombatActionFactory actionFactory;
        private readonly List<CombatActionResult> results =
            new List<CombatActionResult>();
        private int nextCharacterIndex;
        private CharacterBattleState currentCharacter;
        private CombatActionExecutionSession currentActionSession;
        private bool stoppedEarly;
        private bool clearedRemainingActions;

        internal MatchEventCharacterCombatExecutionSession(
            MatchEventExecution execution,
            PartyBattleState party,
            BossBattleState boss,
            CombatActionExecutor executor,
            MatchEventCombatActionFactory actionFactory)
        {
            Execution = execution
                ?? throw new ArgumentNullException(nameof(execution));
            this.party = party
                ?? throw new ArgumentNullException(nameof(party));
            this.boss = boss
                ?? throw new ArgumentNullException(nameof(boss));
            this.executor = executor
                ?? throw new ArgumentNullException(nameof(executor));
            this.actionFactory = actionFactory
                ?? throw new ArgumentNullException(nameof(actionFactory));
            HasMatchingCharacter = HasMatchingCharacterFrom(0);
            if (!HasMatchingCharacter)
            {
                IsCompleted = true;
            }
        }

        public MatchEventExecution Execution { get; }
        public bool HasMatchingCharacter { get; }
        public bool IsCompleted { get; private set; }
        public CombatActionExecutionResult Result { get; private set; }

        public bool TryGetNextAction(
            out CharacterBattleState character,
            out CombatAction action)
        {
            character = null;
            action = null;
            if (!EnsureCurrentActionSession())
            {
                return false;
            }

            character = currentCharacter;
            action = currentActionSession.NextAction;
            return action != null;
        }

        public bool TryExecuteNext(
            out MatchEventCharacterCombatExecutionStepResult stepResult)
        {
            stepResult = null;
            if (!EnsureCurrentActionSession())
            {
                return false;
            }

            CharacterBattleState actingCharacter = currentCharacter;
            if (!currentActionSession.TryExecuteNext(
                    out CombatActionExecutionStepResult actionStep))
            {
                return false;
            }

            stepResult =
                new MatchEventCharacterCombatExecutionStepResult(
                    Execution,
                    actingCharacter,
                    actionStep);

            if (currentActionSession.IsCompleted)
            {
                CompleteCurrentCharacter();
            }

            return true;
        }

        private bool EnsureCurrentActionSession()
        {
            if (IsCompleted)
            {
                return false;
            }

            if (currentActionSession != null)
            {
                return true;
            }

            while (TryTakeNextMatchingCharacter(
                out CharacterBattleState character))
            {
                IReadOnlyList<CombatAction> rootActions =
                    actionFactory.CreateCharacterRootActions(
                        Execution,
                        character);
                currentCharacter = character;
                currentActionSession =
                    executor.BeginExecution(rootActions);
                if (!currentActionSession.IsCompleted)
                {
                    return true;
                }

                CompleteCurrentCharacter();
                if (IsCompleted)
                {
                    return false;
                }
            }

            Complete();
            return false;
        }

        private void CompleteCurrentCharacter()
        {
            CombatActionExecutionResult characterResult =
                currentActionSession.Result;
            for (int index = 0;
                index < characterResult.ActionResults.Count;
                index++)
            {
                results.Add(characterResult.ActionResults[index]);
            }

            stoppedEarly |= characterResult.StoppedEarly;
            clearedRemainingActions |=
                characterResult.ClearedRemainingActions;
            currentActionSession = null;
            currentCharacter = null;

            if (boss.IsDefeated || party.IsIncapacitated)
            {
                stoppedEarly = true;
                clearedRemainingActions |=
                    HasMatchingCharacterFrom(nextCharacterIndex);
                Complete();
                return;
            }

            if (!HasMatchingCharacterFrom(nextCharacterIndex))
            {
                Complete();
            }
        }

        private bool TryTakeNextMatchingCharacter(
            out CharacterBattleState character)
        {
            while (nextCharacterIndex < party.Characters.Count)
            {
                CharacterBattleState candidate =
                    party.Characters[nextCharacterIndex++];
                if (candidate.Element == Execution.MatchEvent.Element)
                {
                    character = candidate;
                    return true;
                }
            }

            character = null;
            return false;
        }

        private bool HasMatchingCharacterFrom(int startingIndex)
        {
            for (int index = startingIndex;
                index < party.Characters.Count;
                index++)
            {
                if (party.Characters[index].Element
                    == Execution.MatchEvent.Element)
                {
                    return true;
                }
            }

            return false;
        }

        private void Complete()
        {
            if (IsCompleted)
            {
                return;
            }

            IsCompleted = true;
            if (results.Count == 0)
            {
                return;
            }

            Result = new CombatActionExecutionResult(
                results,
                stoppedEarly,
                boss.IsDefeated,
                party.IsIncapacitated,
                clearedRemainingActions);
        }
    }

    public sealed class MatchEventCharacterCombatExecutionStepResult
    {
        internal MatchEventCharacterCombatExecutionStepResult(
            MatchEventExecution execution,
            CharacterBattleState character,
            CombatActionExecutionStepResult actionStep)
        {
            Execution = execution
                ?? throw new ArgumentNullException(nameof(execution));
            Character = character
                ?? throw new ArgumentNullException(nameof(character));
            ActionStep = actionStep
                ?? throw new ArgumentNullException(nameof(actionStep));
        }

        public MatchEventExecution Execution { get; }
        public CharacterBattleState Character { get; }
        public int PartySlotIndex => Character.PartySlotIndex;
        public string CharacterId => Character.CharacterId;
        public CombatActionExecutionStepResult ActionStep { get; }
    }
}
