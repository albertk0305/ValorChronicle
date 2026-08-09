using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;

namespace ValorChronicle.Battle.Combat.Integration
{
    public sealed class BattleFlowCombatBridge
    {
        private readonly BattleFlowCoordinator coordinator;
        private readonly PartyBattleState party;
        private readonly BossBattleState boss;
        private readonly CombatActionExecutor executor;
        private readonly MatchEventCombatActionFactory matchActionFactory;
        private readonly IBossCombatActionProvider bossActionProvider;
        private readonly CombatActionIdSequence actionIds;
        private readonly HashSet<long> executedActionIds =
            new HashSet<long>();

        public BattleFlowCombatBridge(
            BattleFlowCoordinator coordinator,
            PartyBattleState party,
            BossBattleState boss,
            CombatActionExecutor executor,
            IMatchEventActionProvider matchActionProvider,
            IBossCombatActionProvider bossActionProvider,
            CombatActionIdSequence actionIds)
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
            matchActionFactory = new MatchEventCombatActionFactory(
                party,
                boss,
                matchActionProvider,
                actionIds);
        }

        public BattleFlowCoordinator Coordinator => coordinator;
        public PartyBattleState Party => party;
        public BossBattleState Boss => boss;
        public CombatActionIdSequence ActionIds => actionIds;
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

            IReadOnlyList<CombatAction> rootActions =
                matchActionFactory.CreateRootActions(execution);
            LastMatchExecutionResult = Execute(rootActions);
            if (LastMatchExecutionResult != null)
            {
                ValidateExecutedActionIds(LastMatchExecutionResult);
            }

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
                || coordinator.Context.CurrentTurn != currentTurn)
            {
                return false;
            }

            var context = new BossCombatActionContext(
                boss,
                party,
                currentTurn,
                actionIds);
            long idBeforeProvider = actionIds.LastIssuedId;
            IReadOnlyList<CombatAction> rootActions =
                bossActionProvider.CreateRootActions(context);
            ValidateBossRootActions(rootActions, idBeforeProvider);
            LastBossExecutionResult = Execute(rootActions);
            if (LastBossExecutionResult != null)
            {
                ValidateExecutedActionIds(LastBossExecutionResult);
            }

            if (party.IsIncapacitated)
            {
                return coordinator.NotifyPartyIncapacitated();
            }

            if (boss.IsDefeated)
            {
                return coordinator.NotifyBossDefeated();
            }

            return coordinator.CompleteBossAction();
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
    }
}
