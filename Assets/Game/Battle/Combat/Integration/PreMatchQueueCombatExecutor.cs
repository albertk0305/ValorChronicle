using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class PreMatchQueueCombatExecutor
    {
        private readonly PartyBattleState party;
        private readonly BossBattleState boss;
        private readonly CombatActionExecutor executor;
        private readonly CombatActionIdSequence actionIds;
        private readonly IReadOnlyList<PreMatchQueueActionProviderBinding>
            bindings;
        private readonly HashSet<long> createdActionIds =
            new HashSet<long>();

        public PreMatchQueueCombatExecutor(
            PartyBattleState party,
            BossBattleState boss,
            CombatActionExecutor executor,
            CombatActionIdSequence actionIds,
            IReadOnlyList<PreMatchQueueActionProviderBinding> bindings)
        {
            this.party = party
                ?? throw new ArgumentNullException(nameof(party));
            this.boss = boss
                ?? throw new ArgumentNullException(nameof(boss));
            this.executor = executor
                ?? throw new ArgumentNullException(nameof(executor));
            this.actionIds = actionIds
                ?? throw new ArgumentNullException(nameof(actionIds));
            this.bindings = CopyAndValidateBindings(bindings);
        }

        public PreMatchQueueCombatExecutionSession BeginExecution(
            int playerTurnNumber,
            long matchQueueId,
            IReadOnlyList<MatchEvent> matchEvents)
        {
            if (matchEvents == null)
            {
                throw new ArgumentNullException(nameof(matchEvents));
            }

            var rootActions = new List<CombatAction>();
            var charactersByRootId =
                new Dictionary<long, CharacterBattleState>();
            for (int index = 0; index < bindings.Count; index++)
            {
                PreMatchQueueActionProviderBinding binding = bindings[index];
                var context = new PreMatchQueueActionContext(
                    binding.Character,
                    party,
                    boss,
                    playerTurnNumber,
                    matchQueueId,
                    matchEvents,
                    actionIds);
                long idBeforeProvider = actionIds.LastIssuedId;
                IReadOnlyList<CombatAction> providerActions =
                    binding.Provider.CreateRootActions(context);
                ValidateProviderActions(providerActions, idBeforeProvider);
                for (int actionIndex = 0;
                    actionIndex < providerActions.Count;
                    actionIndex++)
                {
                    CombatAction action = providerActions[actionIndex];
                    rootActions.Add(action);
                    charactersByRootId.Add(
                        action.ActionId,
                        binding.Character);
                }
            }

            return new PreMatchQueueCombatExecutionSession(
                matchQueueId,
                executor.BeginExecution(rootActions, playerTurnNumber),
                charactersByRootId);
        }

        private IReadOnlyList<PreMatchQueueActionProviderBinding>
            CopyAndValidateBindings(
                IReadOnlyList<PreMatchQueueActionProviderBinding> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var copy = new PreMatchQueueActionProviderBinding[source.Count];
            int previousSlot = -1;
            for (int index = 0; index < source.Count; index++)
            {
                PreMatchQueueActionProviderBinding binding = source[index]
                    ?? throw new ArgumentException(
                        $"Pre-match provider binding {index} is null.",
                        nameof(source));
                if (binding.Character.PartySlotIndex < previousSlot)
                {
                    throw new ArgumentException(
                        "Pre-match provider bindings must follow party slot "
                            + "order.",
                        nameof(source));
                }

                ValidatePartyCharacter(binding.Character);
                previousSlot = binding.Character.PartySlotIndex;
                copy[index] = binding;
            }

            return Array.AsReadOnly(copy);
        }

        private void ValidatePartyCharacter(CharacterBattleState character)
        {
            for (int index = 0; index < party.Characters.Count; index++)
            {
                if (ReferenceEquals(party.Characters[index], character))
                {
                    return;
                }
            }

            throw new ArgumentException(
                "Pre-match provider character must belong to the battle "
                    + "party.");
        }

        private void ValidateProviderActions(
            IReadOnlyList<CombatAction> actions,
            long idBeforeProvider)
        {
            if (actions == null)
            {
                throw new InvalidOperationException(
                    "Pre-match action providers must return an action "
                        + "collection.");
            }

            long allocatedCount = actionIds.LastIssuedId - idBeforeProvider;
            if (allocatedCount != actions.Count)
            {
                throw new InvalidOperationException(
                    "Pre-match action providers must allocate exactly one "
                        + "battle-scoped ID per returned action.");
            }

            for (int index = 0; index < actions.Count; index++)
            {
                CombatAction action = actions[index]
                    ?? throw new InvalidOperationException(
                        "Pre-match action providers cannot return null "
                            + "actions.");
                long expectedActionId = checked(
                    idBeforeProvider + index + 1);
                if (action.ActionId != expectedActionId
                    || action.RootActionId != action.ActionId
                    || action.SourceActionId.HasValue
                    || action.Origin != ActionOrigin.System
                    || !createdActionIds.Add(action.ActionId))
                {
                    throw new InvalidOperationException(
                        "Pre-match actions must be System roots in provider "
                            + "order and use new battle-scoped IDs.");
                }
            }
        }
    }

    public sealed class PreMatchQueueCombatExecutionSession
    {
        private readonly CombatActionExecutionSession actionSession;
        private readonly IReadOnlyDictionary<long, CharacterBattleState>
            charactersByRootId;

        internal PreMatchQueueCombatExecutionSession(
            long matchQueueId,
            CombatActionExecutionSession actionSession,
            IReadOnlyDictionary<long, CharacterBattleState>
                charactersByRootId)
        {
            MatchQueueId = matchQueueId;
            this.actionSession = actionSession
                ?? throw new ArgumentNullException(nameof(actionSession));
            this.charactersByRootId = charactersByRootId
                ?? throw new ArgumentNullException(nameof(charactersByRootId));
        }

        public long MatchQueueId { get; }
        public bool IsCompleted => actionSession.IsCompleted;
        public CombatActionExecutionResult Result => actionSession.Result;

        public bool TryGetNextAction(
            out CharacterBattleState character,
            out CombatAction action)
        {
            action = actionSession.NextAction;
            return TryResolveCharacter(action, out character);
        }

        public bool TryExecuteNext(
            out PreMatchQueueCombatExecutionStepResult stepResult)
        {
            stepResult = null;
            CombatAction nextAction = actionSession.NextAction;
            if (!TryResolveCharacter(
                    nextAction,
                    out CharacterBattleState character)
                || !actionSession.TryExecuteNext(
                    out CombatActionExecutionStepResult actionStep))
            {
                return false;
            }

            stepResult = new PreMatchQueueCombatExecutionStepResult(
                MatchQueueId,
                character,
                actionStep);
            return true;
        }

        private bool TryResolveCharacter(
            CombatAction action,
            out CharacterBattleState character)
        {
            character = null;
            return action != null
                && charactersByRootId.TryGetValue(
                    action.RootActionId,
                    out character);
        }
    }

    public sealed class PreMatchQueueCombatExecutionStepResult
    {
        internal PreMatchQueueCombatExecutionStepResult(
            long matchQueueId,
            CharacterBattleState character,
            CombatActionExecutionStepResult actionStep)
        {
            MatchQueueId = matchQueueId;
            Character = character
                ?? throw new ArgumentNullException(nameof(character));
            ActionStep = actionStep
                ?? throw new ArgumentNullException(nameof(actionStep));
        }

        public long MatchQueueId { get; }
        public CharacterBattleState Character { get; }
        public int PartySlotIndex => Character.PartySlotIndex;
        public string CharacterId => Character.CharacterId;
        public CombatActionExecutionStepResult ActionStep { get; }
    }
}
