using System;
using System.Collections.Generic;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Bosses.Kragmor
{
    public sealed class KragmorBossCombatActionProvider
        : IBossCombatActionProvider,
            IBossCombatActionPlanProvider,
            IBossCombatActionCompletionHandler,
            IKragmorBossIntentSource
    {
        private readonly KragmorBattleRuntimeState runtimeState;
        private bool hasPendingAction;
        private int pendingPatternIndex;
        private KragmorDefenseState pendingDefenseState;
        private BossBattleState pendingBoss;

        public KragmorBossCombatActionProvider(
            KragmorBattleRuntimeState runtimeState)
        {
            this.runtimeState = runtimeState
                ?? throw new ArgumentNullException(nameof(runtimeState));
        }

        public KragmorBattleRuntimeState RuntimeState => runtimeState;
        public KragmorBossIntent NextIntent => runtimeState.NextIntent;

        public IReadOnlyList<CombatAction> CreateRootActions(
            BossCombatActionContext context)
        {
            return CreatePlan(context).CombatActions;
        }

        public BossActionPlan CreatePlan(
            BossCombatActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            KragmorActionKind actionKind = runtimeState.NextActionKind;
            IReadOnlyList<CombatAction> actions = CreateActions(
                context.Boss,
                context.Party,
                context.ActionIds);
            BattleBoardMutationCommand command =
                actionKind == KragmorActionKind.RockshardEruption
                    ? new BattleBoardMutationCommand(
                        BattleBoardMutationKind.CreateRock,
                        KragmorRules.RockshardRockCreationCount,
                        KragmorRules.MaximumRockCount)
                    : null;
            return new BossActionPlan(actions, command);
        }

        public IReadOnlyList<CombatAction> CreateActions(
            BossBattleState boss,
            PartyBattleState party,
            CombatActionIdSequence actionIds)
        {
            ValidateInputs(boss, party, actionIds);
            int patternIndex = runtimeState.PatternIndex;
            if (hasPendingAction
                && pendingPatternIndex != patternIndex)
            {
                throw new InvalidOperationException(
                    "The pending Kragmor action no longer matches the "
                        + "runtime pattern state.");
            }

            EffectInstance currentDefenseEffect =
                KragmorDefenseEffectFactory.GetRequiredActiveEffect(
                    boss,
                    runtimeState.CurrentDefenseState);
            var actions = new List<CombatAction>();
            KragmorDefenseState completedDefenseState =
                runtimeState.CurrentDefenseState;
            switch (runtimeState.NextActionKind)
            {
                case KragmorActionKind.ColossusIronFist:
                    if (runtimeState.CurrentDefenseState
                        == KragmorDefenseState.CoreExposure)
                    {
                        AddDefenseTransition(
                            actions,
                            boss,
                            currentDefenseEffect,
                            KragmorDefenseState.VolcanicCarapace,
                            actionIds);
                    }
                    else if (runtimeState.CurrentDefenseState
                        != KragmorDefenseState.VolcanicCarapace)
                    {
                        throw InvalidDefenseState();
                    }

                    completedDefenseState =
                        KragmorDefenseState.VolcanicCarapace;
                    AddDamageAction(actions, boss, party, actionIds);
                    break;
                case KragmorActionKind.RockshardEruption:
                    RequireDefenseState(
                        KragmorDefenseState.VolcanicCarapace);
                    AddDamageAction(actions, boss, party, actionIds);
                    break;
                case KragmorActionKind.CoreCompression:
                    RequireDefenseState(
                        KragmorDefenseState.VolcanicCarapace);
                    AddDefenseTransition(
                        actions,
                        boss,
                        currentDefenseEffect,
                        KragmorDefenseState.CoreCompression,
                        actionIds);
                    completedDefenseState =
                        KragmorDefenseState.CoreCompression;
                    break;
                case KragmorActionKind.EarthCollapse:
                    RequireDefenseState(
                        KragmorDefenseState.CoreCompression);
                    AddDamageAction(actions, boss, party, actionIds);
                    AddDefenseTransition(
                        actions,
                        boss,
                        currentDefenseEffect,
                        KragmorDefenseState.CoreExposure,
                        actionIds);
                    completedDefenseState =
                        KragmorDefenseState.CoreExposure;
                    break;
                default:
                    throw new InvalidOperationException(
                        "Kragmor pattern contains an unsupported action.");
            }

            hasPendingAction = true;
            pendingPatternIndex = patternIndex;
            pendingDefenseState = completedDefenseState;
            pendingBoss = boss;
            return actions.AsReadOnly();
        }

        public bool TryCommitCompletedAction()
        {
            if (!hasPendingAction
                || runtimeState.PatternIndex != pendingPatternIndex
                || pendingBoss == null)
            {
                return false;
            }

            KragmorDefenseEffectFactory.GetRequiredActiveEffect(
                pendingBoss,
                pendingDefenseState);
            hasPendingAction = false;
            pendingBoss = null;
            runtimeState.CommitAction(pendingDefenseState);
            return true;
        }

        private void AddDamageAction(
            ICollection<CombatAction> actions,
            BossBattleState boss,
            PartyBattleState party,
            CombatActionIdSequence actionIds)
        {
            KragmorBossIntent intent = runtimeState.NextIntent;
            AttackTag attackTags = intent.IsHeavy
                ? AttackTag.Heavy
                : AttackTag.None;
            actions.Add(new BossDamageAction(
                actionIds.Next(),
                new BossDamageContextBuildRequest(
                    boss,
                    party,
                    intent.DamageCoefficient,
                    attackTags)));
        }

        private static void AddDefenseTransition(
            ICollection<CombatAction> actions,
            BossBattleState boss,
            EffectInstance currentEffect,
            KragmorDefenseState nextState,
            CombatActionIdSequence actionIds)
        {
            actions.Add(new RemoveEffectAction(
                actionIds.Next(),
                ActionOrigin.System,
                boss,
                currentEffect.RuntimeId));
            long applyActionId = actionIds.Next();
            actions.Add(new ApplyEffectAction(
                applyActionId,
                ActionOrigin.System,
                boss,
                KragmorDefenseEffectFactory.Create(
                    nextState,
                    applyActionId)));
        }

        private void RequireDefenseState(KragmorDefenseState expectedState)
        {
            if (runtimeState.CurrentDefenseState != expectedState)
            {
                throw InvalidDefenseState();
            }
        }

        private InvalidOperationException InvalidDefenseState()
        {
            return new InvalidOperationException(
                $"Action {runtimeState.NextActionKind} cannot run while "
                    + $"Kragmor defense state is "
                    + $"{runtimeState.CurrentDefenseState}.");
        }

        private static void ValidateInputs(
            BossBattleState boss,
            PartyBattleState party,
            CombatActionIdSequence actionIds)
        {
            if (boss == null)
            {
                throw new ArgumentNullException(nameof(boss));
            }

            if (!string.Equals(
                    boss.BossId,
                    KragmorRules.BossId,
                    StringComparison.Ordinal)
                || boss.Element != ElementType.Fire)
            {
                throw new ArgumentException(
                    "Kragmor's Fire BossBattleState is required.",
                    nameof(boss));
            }

            if (party == null)
            {
                throw new ArgumentNullException(nameof(party));
            }

            if (actionIds == null)
            {
                throw new ArgumentNullException(nameof(actionIds));
            }
        }
    }
}
