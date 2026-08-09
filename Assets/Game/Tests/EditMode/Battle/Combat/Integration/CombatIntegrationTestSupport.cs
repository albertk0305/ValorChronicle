using System;
using System.Collections.Generic;
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

    internal static class CombatIntegrationTestSupport
    {
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
