using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Actions
{
    internal sealed class CombatTriggerTestBattle
    {
        public CombatTriggerTestBattle(
            long partyHp = 1000,
            long bossHp = 10000,
            double characterAttack = 100d,
            double bossAttack = 100d)
        {
            Character = new CharacterBattleState(
                "hero",
                0,
                ElementType.Fire,
                partyHp,
                characterAttack);
            Party = new PartyBattleState(new[] { Character });
            Boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                bossHp,
                bossAttack);
        }

        public CharacterBattleState Character { get; }
        public PartyBattleState Party { get; }
        public BossBattleState Boss { get; }

        public CombatActionExecutor Executor(
            params ICombatTriggerRule[] rules)
        {
            return new CombatActionExecutor(
                Boss,
                Party,
                new DamageContextFactory(new SeededRandomSource(1)),
                new CombatTriggerResolver(rules));
        }

        public DamageAction Damage(
            long actionId,
            ActionOrigin origin,
            double coefficient = 1d,
            long? rootActionId = null,
            long? sourceActionId = null)
        {
            return new DamageAction(
                actionId,
                origin,
                new DamageContextBuildRequest(
                    Character,
                    Party,
                    Boss,
                    ElementType.Fire,
                    ToAttackType(origin),
                    AttackTag.None,
                    coefficient,
                    false,
                    0,
                    false),
                rootActionId,
                sourceActionId);
        }

        private static AttackType ToAttackType(ActionOrigin origin)
        {
            switch (origin)
            {
                case ActionOrigin.Match:
                    return AttackType.Match;
                case ActionOrigin.Additional:
                    return AttackType.Additional;
                case ActionOrigin.Chase:
                    return AttackType.Chase;
                case ActionOrigin.DamageOverTime:
                    return AttackType.DamageOverTime;
                default:
                    return AttackType.Active;
            }
        }
    }

    internal sealed class DelegateCombatTriggerRule : ICombatTriggerRule
    {
        private readonly Func<
            CombatActionTriggerContext,
            IReadOnlyList<CombatAction>> createActions;

        public DelegateCombatTriggerRule(
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
}
