using System;
using System.Collections.Generic;
using System.Reflection;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Integration
{
    internal sealed class DelegateMatchEventActionProvider
        : IMatchEventActionProvider
    {
        private readonly Func<
            MatchEventActionContext,
            IReadOnlyList<CombatAction>> createActions;

        public DelegateMatchEventActionProvider(
            Func<MatchEventActionContext,
                IReadOnlyList<CombatAction>> createActions)
        {
            this.createActions = createActions
                ?? throw new ArgumentNullException(nameof(createActions));
        }

        public IReadOnlyList<CombatAction> CreateRootActions(
            MatchEventActionContext context)
        {
            return createActions(context);
        }
    }

    internal sealed class DelegateBossCombatActionProvider
        : IBossCombatActionProvider
    {
        private readonly Func<
            BossCombatActionContext,
            IReadOnlyList<CombatAction>> createActions;

        public DelegateBossCombatActionProvider(
            Func<BossCombatActionContext,
                IReadOnlyList<CombatAction>> createActions)
        {
            this.createActions = createActions
                ?? throw new ArgumentNullException(nameof(createActions));
        }

        public IReadOnlyList<CombatAction> CreateRootActions(
            BossCombatActionContext context)
        {
            return createActions(context);
        }
    }

    internal sealed class DelegateIntegrationTriggerRule
        : ICombatTriggerRule
    {
        private readonly Func<
            CombatActionTriggerContext,
            IReadOnlyList<CombatAction>> createActions;

        public DelegateIntegrationTriggerRule(
            Func<CombatActionTriggerContext,
                IReadOnlyList<CombatAction>> createActions)
        {
            this.createActions = createActions
                ?? throw new ArgumentNullException(nameof(createActions));
        }

        public IReadOnlyList<CombatAction> CreateDerivedActions(
            CombatActionTriggerContext context)
        {
            return createActions(context);
        }
    }

    internal sealed class RecordingBoardMutationSink
        : IBattleBoardMutationSink
    {
        private Action<BattleBoardMutationCompletion> completion;
        private Action<BattleBoardMutationCompletion> lastCompletion;

        public bool AcceptRequests { get; set; } = true;
        public Exception RequestException { get; set; }
        public int RequestCount { get; private set; }
        public BattleBoardMutationCommand LastCommand { get; private set; }

        public bool TryExecuteMutation(
            BattleBoardMutationCommand command,
            Action<BattleBoardMutationCompletion> callback)
        {
            RequestCount++;
            LastCommand = command;
            if (RequestException != null)
            {
                throw RequestException;
            }

            if (!AcceptRequests)
            {
                return false;
            }

            completion = callback;
            lastCompletion = callback;
            return true;
        }

        public void Complete(BattleBoardMutationCompletion result)
        {
            Action<BattleBoardMutationCompletion> callback = completion;
            completion = null;
            callback(result);
        }

        public void RepeatCompletion(BattleBoardMutationCompletion result)
        {
            lastCompletion(result);
        }
    }

    internal sealed class ImmediateSuccessfulBoardMutationSink
        : IBattleBoardMutationSink
    {
        public int RequestCount { get; private set; }
        public BattleBoardMutationCommand LastCommand { get; private set; }

        public bool TryExecuteMutation(
            BattleBoardMutationCommand command,
            Action<BattleBoardMutationCompletion> completion)
        {
            RequestCount++;
            LastCommand = command;
            completion(CombatIntegrationTestSupport
                .CreateSuccessfulBoardMutation(command));
            return true;
        }
    }

    internal static class CombatIntegrationTestSupport
    {
        public static BattleBoardMutationCompletion
            CreateSuccessfulBoardMutation(
                BattleBoardMutationCommand command)
        {
            var result = (BoardRockMutationResult)Activator.CreateInstance(
                typeof(BoardRockMutationResult),
                BindingFlags.Instance
                    | BindingFlags.Public
                    | BindingFlags.NonPublic,
                binder: null,
                args: new object[]
                {
                    new BoardState(),
                    command.RequestedCount,
                    Array.Empty<BoardRockPlacement>(),
                    0,
                    null,
                    BoardRockMutationFailure.None
                },
                culture: null);
            return new BattleBoardMutationCompletion(
                command,
                result,
                BattleBoardMutationCompletionStatus.Completed);
        }

        public static DamageAction MatchDamage(
            MatchEventActionContext context,
            double coefficient = 1d)
        {
            return new DamageAction(
                context.ActionIds.Next(),
                ActionOrigin.Match,
                new DamageContextBuildRequest(
                    context.Character,
                    context.Party,
                    context.Boss,
                    context.Character.Element,
                    AttackType.Match,
                    context.MatchAttackTag,
                    coefficient,
                    true,
                    context.FinalComboCount,
                    false));
        }

        public static BossDamageAction BossDamage(
            BossCombatActionContext context,
            double coefficient = 1d)
        {
            return new BossDamageAction(
                context.ActionIds.Next(),
                new BossDamageContextBuildRequest(
                    context.Boss,
                    context.Party,
                    coefficient,
                    AttackTag.None));
        }
    }
}
