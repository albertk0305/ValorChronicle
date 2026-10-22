using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Core.Logging;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class ActiveAbilityCombatExecutor
    {
        private readonly BattleFlowCoordinator coordinator;
        private readonly PartyBattleState party;
        private readonly BossBattleState boss;
        private readonly CombatActionExecutor executor;
        private readonly CombatActionIdSequence actionIds;
        private readonly ActiveAbilityBinding[] bindingsByIndex;
        private readonly ActiveAbilityActionProviderRegistry providers;
        private readonly IBattleBoardElementConversionSink
            boardElementConversionSink;

        public ActiveAbilityCombatExecutor(
            BattleFlowCoordinator coordinator,
            PartyBattleState party,
            BossBattleState boss,
            CombatActionExecutor executor,
            CombatActionIdSequence actionIds,
            IReadOnlyList<ActiveAbilityBinding> bindings,
            ActiveAbilityActionProviderRegistry providers,
            IBattleBoardElementConversionSink boardElementConversionSink = null)
        {
            this.coordinator = coordinator
                ?? throw new ArgumentNullException(nameof(coordinator));
            this.party = party
                ?? throw new ArgumentNullException(nameof(party));
            this.boss = boss
                ?? throw new ArgumentNullException(nameof(boss));
            this.executor = executor
                ?? throw new ArgumentNullException(nameof(executor));
            this.actionIds = actionIds
                ?? throw new ArgumentNullException(nameof(actionIds));
            this.providers = providers
                ?? throw new ArgumentNullException(nameof(providers));
            this.boardElementConversionSink = boardElementConversionSink;
            bindingsByIndex = CreateBindings(bindings);
        }

        public bool TryExecute(
            int activeAbilityIndex,
            out CombatActionExecutionResult result)
        {
            result = null;
            if (!TryBeginExecution(
                activeAbilityIndex,
                out ActiveAbilityCombatExecutionSession session))
            {
                return false;
            }

            while (session.TryExecuteNext(out _))
            {
            }

            result = session.Result;
            return true;
        }

        public bool TryBeginExecution(
            int activeAbilityIndex,
            out ActiveAbilityCombatExecutionSession session)
        {
            session = null;
            if (!TryResolveAvailableActive(
                activeAbilityIndex,
                out ActiveAbilityBinding binding,
                out CharacterBattleState character,
                out IActiveAbilityActionProvider provider,
                out IActiveAbilityBoardPlanProvider boardProvider))
            {
                return false;
            }

            if (boardProvider != null)
            {
                return TryBeginBoardExecution(
                    activeAbilityIndex,
                    binding,
                    character,
                    boardProvider,
                    out session);
            }

            var context = new ActiveAbilityActionContext(
                binding,
                character,
                party,
                boss,
                actionIds);
            long idBeforeProvider = actionIds.LastIssuedId;
            IReadOnlyList<CombatAction> rootActions =
                provider.CreateRootActions(context);
            ValidateRootActions(rootActions, idBeforeProvider);
            CombatActionExecutionSession actionSession =
                executor.BeginExecution(rootActions);
            if (!coordinator.TryUseActiveAbility(activeAbilityIndex))
            {
                throw new InvalidOperationException(
                    "Active runtime changed after prevalidation.");
            }

            session = new ActiveAbilityCombatExecutionSession(
                binding,
                character,
                actionSession);
            return true;
        }

        public bool CanExecute(int activeAbilityIndex)
        {
            return TryResolveAvailableActive(
                activeAbilityIndex,
                out _,
                out _,
                out _,
                out _);
        }

        public bool TryGetSingleBinding(
            int partySlotIndex,
            string characterId,
            out ActiveAbilityBinding binding)
        {
            binding = null;
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return false;
            }

            for (int index = 0; index < bindingsByIndex.Length; index++)
            {
                ActiveAbilityBinding candidate = bindingsByIndex[index];
                if (candidate == null
                    || candidate.PartySlotIndex != partySlotIndex
                    || !string.Equals(
                        candidate.CharacterId,
                        characterId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (binding != null)
                {
                    binding = null;
                    return false;
                }

                binding = candidate;
            }

            return binding != null;
        }

        private bool TryResolveAvailableActive(
            int activeAbilityIndex,
            out ActiveAbilityBinding binding,
            out CharacterBattleState character,
            out IActiveAbilityActionProvider provider,
            out IActiveAbilityBoardPlanProvider boardProvider)
        {
            binding = null;
            character = null;
            provider = null;
            boardProvider = null;
            if (!coordinator.CanUseActiveAbility(activeAbilityIndex))
            {
                return false;
            }

            binding = bindingsByIndex[activeAbilityIndex];
            character = binding != null
                ? FindBoundCharacter(binding)
                : null;
            if (character == null)
            {
                return false;
            }

            if (providers.TryResolve(
                binding.CharacterId,
                binding.ActiveAbilityId,
                out provider))
            {
                return true;
            }

            return providers.TryResolveBoard(
                    binding.CharacterId,
                    binding.ActiveAbilityId,
                    out boardProvider)
                && boardElementConversionSink != null
                && boardElementConversionSink.CanAcceptElementConversion;
        }

        private bool TryBeginBoardExecution(
            int activeAbilityIndex,
            ActiveAbilityBinding binding,
            CharacterBattleState character,
            IActiveAbilityBoardPlanProvider provider,
            out ActiveAbilityCombatExecutionSession session)
        {
            session = null;
            var planContext = new ActiveAbilityBoardPlanContext(
                binding,
                character,
                party,
                boss);
            ActiveAbilityBoardElementConversionPlan plan =
                provider.CreateBoardPlan(planContext)
                ?? throw new InvalidOperationException(
                    "Board Active providers must return a Board plan.");

            CombatActionExecutionSession actionSession =
                executor.BeginExecution(Array.Empty<CombatAction>());
            if (!coordinator.TryUseActiveAbility(activeAbilityIndex))
            {
                throw new InvalidOperationException(
                    "Active runtime changed after prevalidation.");
            }

            BoardElementConversionResult conversionResult;
            bool converted;
            try
            {
                converted = boardElementConversionSink.TryConvertElements(
                    plan.TargetElement,
                    plan.RequestedCount,
                    out conversionResult);
            }
            catch
            {
                RollbackCommittedActive(activeAbilityIndex);
                throw;
            }

            if (!converted
                || conversionResult == null
                || !conversionResult.Succeeded
                || conversionResult.RequestedCount != plan.RequestedCount
                || !HasExpectedTargetElement(
                    conversionResult,
                    plan.TargetElement))
            {
                RollbackCommittedActive(activeAbilityIndex);
                return false;
            }

            if (provider is IActiveAbilityBoardSuccessCommitter committer)
            {
                try
                {
                    committer.CommitBoardSuccess(
                        new ActiveAbilityBoardSuccessContext(
                            planContext,
                            plan,
                            conversionResult));
                }
                catch (Exception exception)
                {
                    // The Board and Active runtime are already committed.
                    // A listener failure cannot safely turn this use into a
                    // reported failure or roll the Board back.
                    GameLogger.Exception(exception);
                }
            }

            session = new ActiveAbilityCombatExecutionSession(
                binding,
                character,
                actionSession);
            return true;
        }

        private void RollbackCommittedActive(int activeAbilityIndex)
        {
            if (!coordinator.TryRollbackActiveAbilityUse(activeAbilityIndex))
            {
                throw new InvalidOperationException(
                    "A failed Board Active could not restore its runtime state.");
            }
        }

        private static bool HasExpectedTargetElement(
            BoardElementConversionResult result,
            ElementType targetElement)
        {
            for (int index = 0; index < result.Entries.Count; index++)
            {
                if (result.Entries[index].NewElement != targetElement)
                {
                    return false;
                }
            }

            return result.ConvertedCount > 0;
        }

        private ActiveAbilityBinding[] CreateBindings(
            IReadOnlyList<ActiveAbilityBinding> bindings)
        {
            if (bindings == null)
            {
                throw new ArgumentNullException(nameof(bindings));
            }

            var result = new ActiveAbilityBinding[
                coordinator.Context.ActiveAbilities.Count];
            for (int index = 0; index < bindings.Count; index++)
            {
                ActiveAbilityBinding binding = bindings[index]
                    ?? throw new ArgumentException(
                        "Active bindings cannot contain null.",
                        nameof(bindings));
                if (binding.ActiveAbilityIndex >= result.Length)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(bindings),
                        binding.ActiveAbilityIndex,
                        "Active binding index is outside runtime state.");
                }

                if (result[binding.ActiveAbilityIndex] != null)
                {
                    throw new ArgumentException(
                        $"Duplicate active binding index: "
                            + $"{binding.ActiveAbilityIndex}.",
                        nameof(bindings));
                }

                if (FindBoundCharacter(binding) == null)
                {
                    throw new InvalidOperationException(
                        "Active binding must match its party slot and "
                            + "character ID.");
                }

                result[binding.ActiveAbilityIndex] = binding;
            }

            return result;
        }

        private CharacterBattleState FindBoundCharacter(
            ActiveAbilityBinding binding)
        {
            for (int index = 0; index < party.Characters.Count; index++)
            {
                CharacterBattleState character = party.Characters[index];
                if (character.PartySlotIndex == binding.PartySlotIndex)
                {
                    return string.Equals(
                        character.CharacterId,
                        binding.CharacterId,
                        StringComparison.Ordinal)
                            ? character
                            : null;
                }
            }

            return null;
        }

        private void ValidateRootActions(
            IReadOnlyList<CombatAction> actions,
            long idBeforeProvider)
        {
            if (actions == null || actions.Count == 0)
            {
                throw new InvalidOperationException(
                    "Active providers must return at least one root action.");
            }

            long allocatedCount = actionIds.LastIssuedId - idBeforeProvider;
            if (allocatedCount != actions.Count)
            {
                throw new InvalidOperationException(
                    "Active providers must allocate exactly one "
                        + "battle-scoped ID per returned action.");
            }

            for (int actionIndex = 0;
                actionIndex < actions.Count;
                actionIndex++)
            {
                CombatAction action = actions[actionIndex]
                    ?? throw new InvalidOperationException(
                        "Active providers cannot return null actions.");
                long expectedActionId = checked(
                    idBeforeProvider + actionIndex + 1);
                if (action.ActionId != expectedActionId
                    || action.RootActionId != action.ActionId
                    || action.SourceActionId.HasValue
                    || action.Origin != ActionOrigin.Active)
                {
                    throw new InvalidOperationException(
                        "Active root actions must preserve provider order, "
                            + "use new battle-scoped IDs, have no source, "
                            + "and use Active origin.");
                }
            }
        }
    }

    public sealed class ActiveAbilityCombatExecutionSession
    {
        private readonly CombatActionExecutionSession actionSession;

        internal ActiveAbilityCombatExecutionSession(
            ActiveAbilityBinding binding,
            CharacterBattleState character,
            CombatActionExecutionSession actionSession)
        {
            Binding = binding
                ?? throw new ArgumentNullException(nameof(binding));
            Character = character
                ?? throw new ArgumentNullException(nameof(character));
            this.actionSession = actionSession
                ?? throw new ArgumentNullException(nameof(actionSession));
        }

        public ActiveAbilityBinding Binding { get; }
        public CharacterBattleState Character { get; }
        public int PartySlotIndex => Character.PartySlotIndex;
        public string CharacterId => Character.CharacterId;
        public bool IsCompleted => actionSession.IsCompleted;
        public CombatAction NextAction => actionSession.NextAction;
        public CombatActionExecutionResult Result => actionSession.Result;

        public bool TryExecuteNext(
            out ActiveCombatActionExecutionStepResult stepResult)
        {
            stepResult = null;
            if (!actionSession.TryExecuteNext(
                out CombatActionExecutionStepResult actionStep))
            {
                return false;
            }

            stepResult = new ActiveCombatActionExecutionStepResult(
                Character,
                actionStep);
            return true;
        }
    }

    public sealed class ActiveCombatActionExecutionStepResult
    {
        internal ActiveCombatActionExecutionStepResult(
            CharacterBattleState character,
            CombatActionExecutionStepResult actionStep)
        {
            Character = character
                ?? throw new ArgumentNullException(nameof(character));
            ActionStep = actionStep
                ?? throw new ArgumentNullException(nameof(actionStep));
        }

        public CharacterBattleState Character { get; }
        public int PartySlotIndex => Character.PartySlotIndex;
        public string CharacterId => Character.CharacterId;
        public CombatActionExecutionStepResult ActionStep { get; }
    }
}
