using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Core.Logging;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class BattleFlowCombatBridge
    {
        private readonly BattleFlowCoordinator coordinator;
        private readonly PartyBattleState party;
        private readonly BossBattleState boss;
        private readonly CombatActionExecutor executor;
        private readonly MatchEventCharacterCombatExecutor
            matchCharacterExecutor;
        private readonly IBossCombatActionProvider bossActionProvider;
        private readonly CombatActionIdSequence actionIds;
        private readonly BattleTurnEndProcessor turnEndProcessor;
        private readonly ActiveAbilityCombatExecutor activeExecutor;
        private readonly IBattleBoardMutationSink boardMutationSink;
        private readonly HashSet<long> executedActionIds =
            new HashSet<long>();
        private bool hasPendingBossBoardMutation;
        private int pendingBossTurn;
        private BattleBoardMutationCommand pendingBoardCommand;

        public event Action CombatActionsApplied;

        public BattleFlowCombatBridge(
            BattleFlowCoordinator coordinator,
            PartyBattleState party,
            BossBattleState boss,
            CombatActionExecutor executor,
            IMatchEventActionProvider matchActionProvider,
            IBossCombatActionProvider bossActionProvider,
            CombatActionIdSequence actionIds)
            : this(
                coordinator,
                party,
                boss,
                executor,
                matchActionProvider,
                bossActionProvider,
                actionIds,
                Array.Empty<ActiveAbilityBinding>(),
                new ActiveAbilityActionProviderRegistry(),
                boardMutationSink: null)
        {
        }

        public BattleFlowCombatBridge(
            BattleFlowCoordinator coordinator,
            PartyBattleState party,
            BossBattleState boss,
            CombatActionExecutor executor,
            IMatchEventActionProvider matchActionProvider,
            IBossCombatActionProvider bossActionProvider,
            CombatActionIdSequence actionIds,
            IBattleBoardMutationSink boardMutationSink)
            : this(
                coordinator,
                party,
                boss,
                executor,
                matchActionProvider,
                bossActionProvider,
                actionIds,
                Array.Empty<ActiveAbilityBinding>(),
                new ActiveAbilityActionProviderRegistry(),
                boardMutationSink)
        {
        }

        public BattleFlowCombatBridge(
            BattleFlowCoordinator coordinator,
            PartyBattleState party,
            BossBattleState boss,
            CombatActionExecutor executor,
            IMatchEventActionProvider matchActionProvider,
            IBossCombatActionProvider bossActionProvider,
            CombatActionIdSequence actionIds,
            IReadOnlyList<ActiveAbilityBinding> activeBindings,
            ActiveAbilityActionProviderRegistry activeProviders)
            : this(
                coordinator,
                party,
                boss,
                executor,
                matchActionProvider,
                bossActionProvider,
                actionIds,
                activeBindings,
                activeProviders,
                boardMutationSink: null)
        {
        }

        public BattleFlowCombatBridge(
            BattleFlowCoordinator coordinator,
            PartyBattleState party,
            BossBattleState boss,
            CombatActionExecutor executor,
            IMatchEventActionProvider matchActionProvider,
            IBossCombatActionProvider bossActionProvider,
            CombatActionIdSequence actionIds,
            IReadOnlyList<ActiveAbilityBinding> activeBindings,
            ActiveAbilityActionProviderRegistry activeProviders,
            IBattleBoardMutationSink boardMutationSink)
        {
            this.coordinator = coordinator
                ?? throw new ArgumentNullException(nameof(coordinator));
            this.party = party
                ?? throw new ArgumentNullException(nameof(party));
            this.boss = boss
                ?? throw new ArgumentNullException(nameof(boss));
            this.executor = executor
                ?? throw new ArgumentNullException(nameof(executor));
            this.bossActionProvider = bossActionProvider
                ?? throw new ArgumentNullException(nameof(bossActionProvider));
            this.actionIds = actionIds
                ?? throw new ArgumentNullException(nameof(actionIds));
            this.boardMutationSink = boardMutationSink;
            var matchActionFactory = new MatchEventCombatActionFactory(
                party,
                boss,
                matchActionProvider,
                actionIds);
            matchCharacterExecutor = new MatchEventCharacterCombatExecutor(
                party,
                boss,
                executor,
                matchActionFactory);
            activeExecutor = new ActiveAbilityCombatExecutor(
                coordinator,
                party,
                boss,
                executor,
                actionIds,
                activeBindings,
                activeProviders);
            turnEndProcessor = new BattleTurnEndProcessor(party, boss);
        }

        public BattleFlowCoordinator Coordinator => coordinator;
        public PartyBattleState Party => party;
        public BossBattleState Boss => boss;
        public CombatActionIdSequence ActionIds => actionIds;
        public int ProcessedTurnEndCount =>
            turnEndProcessor.ProcessedTurnCount;
        public bool HasPendingBossBoardMutation =>
            hasPendingBossBoardMutation;
        public BattleBoardMutationCompletion
            LastBossBoardMutationCompletion { get; private set; }
        public CombatActionExecutionResult LastMatchExecutionResult
        {
            get;
            private set;
        }
        public CombatActionExecutionResult LastBossExecutionResult
        {
            get;
            private set;
        }
        public CombatActionExecutionResult LastActiveExecutionResult
        {
            get;
            private set;
        }

        public bool CanUseActive(int activeAbilityIndex)
        {
            return activeExecutor.CanExecute(activeAbilityIndex);
        }

        public bool TryGetSingleActiveBinding(
            int partySlotIndex,
            string characterId,
            out ActiveAbilityBinding binding)
        {
            return activeExecutor.TryGetSingleBinding(
                partySlotIndex,
                characterId,
                out binding);
        }

        public bool TryUseActive(int activeAbilityIndex)
        {
            if (!activeExecutor.TryExecute(
                activeAbilityIndex,
                out CombatActionExecutionResult result))
            {
                return false;
            }

            LastActiveExecutionResult = result;
            ValidateExecutedActionIds(LastActiveExecutionResult);
            NotifyCombatActionsApplied(LastActiveExecutionResult);
            if (boss.IsDefeated)
            {
                return coordinator.NotifyBossDefeated();
            }

            if (party.IsIncapacitated)
            {
                return coordinator.NotifyPartyIncapacitated();
            }

            return true;
        }

        public bool ResolveMatchEvent(MatchEventExecution execution)
        {
            if (execution == null)
            {
                throw new ArgumentNullException(nameof(execution));
            }

            if (coordinator.Context.Result != BattleResultKind.None
                || coordinator.Context.Phase
                    != BattlePhase.MatchEventResolving
                || !ReferenceEquals(
                    coordinator.CurrentMatchEventExecution,
                    execution))
            {
                return false;
            }

            LastMatchExecutionResult =
                matchCharacterExecutor.Execute(execution);
            if (LastMatchExecutionResult != null)
            {
                ValidateExecutedActionIds(LastMatchExecutionResult);
            }

            NotifyCombatActionsApplied(LastMatchExecutionResult);

            if (boss.IsDefeated)
            {
                return coordinator.NotifyBossDefeated();
            }

            if (party.IsIncapacitated)
            {
                return coordinator.NotifyPartyIncapacitated();
            }

            return coordinator.CompleteCurrentMatchEvent(
                execution.ExecutionId);
        }

        public bool ResolveBossAction(int currentTurn)
        {
            if (coordinator.Context.Result != BattleResultKind.None
                || coordinator.Context.Phase != BattlePhase.BossActing
                || coordinator.Context.CurrentTurn != currentTurn
                || hasPendingBossBoardMutation)
            {
                return false;
            }

            var context = new BossCombatActionContext(
                boss,
                party,
                currentTurn,
                actionIds);
            long idBeforeProvider = actionIds.LastIssuedId;
            BossActionPlan plan = CreateBossActionPlan(context);
            IReadOnlyList<CombatAction> rootActions = plan.CombatActions;
            ValidateBossRootActions(rootActions, idBeforeProvider);
            LastBossExecutionResult = Execute(rootActions);
            if (LastBossExecutionResult != null)
            {
                ValidateExecutedActionIds(LastBossExecutionResult);
            }

            NotifyCombatActionsApplied(LastBossExecutionResult);

            if (party.IsIncapacitated)
            {
                return coordinator.NotifyPartyIncapacitated();
            }

            if (boss.IsDefeated)
            {
                return coordinator.NotifyBossDefeated();
            }

            if (plan.BoardCommand != null)
            {
                return BeginBossBoardMutation(
                    currentTurn,
                    plan.BoardCommand);
            }

            return CompleteBossAction();
        }

        private BossActionPlan CreateBossActionPlan(
            BossCombatActionContext context)
        {
            if (bossActionProvider
                is IBossCombatActionPlanProvider planProvider)
            {
                return planProvider.CreatePlan(context)
                    ?? throw new InvalidOperationException(
                        "Boss action plan providers cannot return null.");
            }

            return new BossActionPlan(
                bossActionProvider.CreateRootActions(context));
        }

        private bool BeginBossBoardMutation(
            int currentTurn,
            BattleBoardMutationCommand command)
        {
            if (boardMutationSink == null)
            {
                GameLogger.Error(
                    "[BattleFlowCombatBridge] A boss Board command has no "
                        + "mutation sink.");
                coordinator.AbortBattle();
                return false;
            }

            hasPendingBossBoardMutation = true;
            pendingBossTurn = currentTurn;
            pendingBoardCommand = command;
            bool accepted;
            try
            {
                accepted = boardMutationSink.TryExecuteMutation(
                    command,
                    HandleBossBoardMutationCompleted);
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception);
                GameLogger.Error(
                    "[BattleFlowCombatBridge] Boss Board mutation request "
                        + "threw an exception.");
                ClearPendingBossBoardMutation();
                coordinator.AbortBattle();
                return false;
            }

            if (!accepted)
            {
                GameLogger.Error(
                    "[BattleFlowCombatBridge] Boss Board mutation request "
                        + "was rejected.");
                ClearPendingBossBoardMutation();
                coordinator.AbortBattle();
                return false;
            }

            return coordinator.Context.Result != BattleResultKind.Aborted;
        }

        private void HandleBossBoardMutationCompleted(
            BattleBoardMutationCompletion completion)
        {
            if (!hasPendingBossBoardMutation
                || completion == null
                || !ReferenceEquals(
                    completion.Command,
                    pendingBoardCommand))
            {
                GameLogger.Warning(
                    "[BattleFlowCombatBridge] Ignored duplicate or stale "
                        + "boss Board mutation completion.");
                return;
            }

            LastBossBoardMutationCompletion = completion;
            int completedTurn = pendingBossTurn;
            ClearPendingBossBoardMutation();
            if (coordinator.Context.Result != BattleResultKind.None
                || coordinator.Context.Phase != BattlePhase.BossActing
                || coordinator.Context.CurrentTurn != completedTurn)
            {
                GameLogger.Warning(
                    "[BattleFlowCombatBridge] Ignored boss Board mutation "
                        + "completion outside its originating boss action.");
                return;
            }

            if (!completion.Succeeded)
            {
                if (completion.Failure != null)
                {
                    GameLogger.Exception(completion.Failure);
                }

                GameLogger.Error(
                    "[BattleFlowCombatBridge] Boss Board mutation failed; "
                        + "the battle will be aborted without committing "
                        + "the boss pattern.");
                coordinator.AbortBattle();
                return;
            }

            try
            {
                if (!CompleteBossAction())
                {
                    throw new InvalidOperationException(
                        "The completed boss Board mutation could not finish "
                            + "the boss action.");
                }
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception);
                GameLogger.Error(
                    "[BattleFlowCombatBridge] Boss action completion failed "
                        + "after Board mutation.");
                if (coordinator.Context.Result == BattleResultKind.None)
                {
                    coordinator.AbortBattle();
                }
            }
        }

        private void ClearPendingBossBoardMutation()
        {
            hasPendingBossBoardMutation = false;
            pendingBossTurn = 0;
            pendingBoardCommand = null;
        }

        private bool CompleteBossAction()
        {
            turnEndProcessor.ProcessTurnEnd();
            CommitBossActionProvider();
            return coordinator.CompleteBossAction();
        }

        private void CommitBossActionProvider()
        {
            if (bossActionProvider
                    is IBossCombatActionCompletionHandler completionHandler
                && !completionHandler.TryCommitCompletedAction())
            {
                throw new InvalidOperationException(
                    "The boss action provider could not commit its "
                        + "completed action.");
            }
        }

        private CombatActionExecutionResult Execute(
            IReadOnlyList<CombatAction> rootActions)
        {
            if (rootActions.Count == 0)
            {
                return null;
            }

            return executor.Execute(new CombatActionQueue(rootActions));
        }

        private void ValidateBossRootActions(
            IReadOnlyList<CombatAction> actions,
            long idBeforeProvider)
        {
            if (actions == null)
            {
                throw new InvalidOperationException(
                    "Boss action providers must return an action collection.");
            }

            long allocatedCount = actionIds.LastIssuedId - idBeforeProvider;
            if (allocatedCount != actions.Count)
            {
                throw new InvalidOperationException(
                    "Boss action providers must allocate exactly one "
                        + "battle-scoped ID per returned action.");
            }

            for (int actionIndex = 0;
                actionIndex < actions.Count;
                actionIndex++)
            {
                CombatAction action = actions[actionIndex]
                    ?? throw new InvalidOperationException(
                        "Boss action providers cannot return null actions.");
                long expectedActionId = checked(
                    idBeforeProvider + actionIndex + 1);
                if (action.ActionId != expectedActionId
                    || action.RootActionId != action.ActionId
                    || action.SourceActionId.HasValue
                    || action is DamageAction)
                {
                    throw new InvalidOperationException(
                        "Boss root actions must preserve provider order, use "
                            + "new battle-scoped IDs, have no source, and not "
                            + "use player-to-boss DamageAction.");
                }
            }
        }

        private void ValidateExecutedActionIds(
            CombatActionExecutionResult result)
        {
            for (int index = 0;
                index < result.ActionResults.Count;
                index++)
            {
                long actionId = result.ActionResults[index].Action.ActionId;
                if (!actionIds.HasIssued(actionId)
                    || !executedActionIds.Add(actionId))
                {
                    throw new InvalidOperationException(
                        $"Combat action ID is not unique in this battle: "
                            + $"{actionId}.");
                }
            }
        }

        private void NotifyCombatActionsApplied(
            CombatActionExecutionResult result)
        {
            if (result != null && result.CompletedActionCount > 0)
            {
                CombatActionsApplied?.Invoke();
            }
        }
    }
}
