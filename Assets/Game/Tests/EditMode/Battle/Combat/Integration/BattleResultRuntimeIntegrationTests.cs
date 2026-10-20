using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Results;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Tests.EditMode.Battle.Flow;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Integration
{
    public sealed class BattleResultRuntimeIntegrationTests
    {
        [Test]
        public void MatchAndAdditionalDamageAreCollectedFromOneExecution()
        {
            RuntimeBattle battle = CreateBattle(
                bossHp: 1000L,
                matchProvider: new DelegateMatchEventActionProvider(context =>
                    new CombatAction[] { Damage(context, 1d) }),
                triggerRuleFactory: actionIds =>
                    new ICombatTriggerRule[]
                    {
                        new DelegateIntegrationTriggerRule(context =>
                        {
                            if (!(context.CompletedAction
                                    is DamageAction source)
                                || source.Origin != ActionOrigin.Match)
                            {
                                return Array.Empty<CombatAction>();
                            }

                            return new CombatAction[]
                            {
                                FollowUpDamage(context, actionIds, source)
                            };
                        })
                    });

            BeginSingleMatch(battle.Coordinator);
            battle.Coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution);
            Assert.That(battle.Bridge.ResolveMatchEvent(execution), Is.True);

            Assert.That(battle.Bridge.DamageScore, Is.EqualTo(300L));
            Assert.That(
                battle.Bridge.LastMatchExecutionResult.ActionResults,
                Has.Count.EqualTo(2));
        }

        [Test]
        public void ProviderAdditionalUsesSeparateModifiersAndSharedCalculator()
        {
            RuntimeBattle battle = CreateBattle(
                bossHp: 10000L,
                matchProvider: new DelegateMatchEventActionProvider(context =>
                {
                    DamageAction match =
                        CombatIntegrationTestSupport.MatchDamage(
                            context,
                            1.013d);
                    return new CombatAction[]
                    {
                        match,
                        CombatIntegrationTestSupport.AdditionalMatchDamage(
                            context,
                            match,
                            1.013d)
                    };
                }));
            CharacterBattleState attacker =
                battle.Bridge.Party.Characters[0];
            attacker.Effects.ApplyEffect(AttackTypeEffect(
                1,
                "match_damage",
                0.10d,
                AttackTypeMask.Match));
            attacker.Effects.ApplyEffect(AttackTypeEffect(
                2,
                "additional_damage",
                0.20d,
                AttackTypeMask.Additional));
            attacker.Effects.ApplyEffect(AttackTypeEffect(
                3,
                "chase_damage",
                0.40d,
                AttackTypeMask.Chase));
            attacker.Effects.ApplyEffect(ElementEffect(
                4,
                "fire_damage",
                0.30d,
                ElementType.Fire));
            attacker.Effects.ApplyEffect(Effect(
                5,
                "dealt_damage",
                EffectModifierType.DealtDamageIncrease,
                0.05d));
            long hpBefore = battle.Bridge.Boss.CurrentHp;

            BeginSingleMatch(battle.Coordinator);
            battle.Coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution);
            Assert.That(battle.Bridge.ResolveMatchEvent(execution), Is.True);

            var match = (DamageActionResult)battle.Bridge
                .LastMatchExecutionResult.ActionResults[0];
            var additional = (DamageActionResult)battle.Bridge
                .LastMatchExecutionResult.ActionResults[1];
            Assert.That(match.Context.AttackTypeDamageIncreaseRateSum,
                Is.EqualTo(0.10d));
            Assert.That(additional.Context.AttackTypeDamageIncreaseRateSum,
                Is.EqualTo(0.20d));
            Assert.That(match.Context.ElementDamageIncreaseRateSum,
                Is.EqualTo(0.30d));
            Assert.That(additional.Context.ElementDamageIncreaseRateSum,
                Is.EqualTo(0.30d));
            Assert.That(match.Context.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.05d));
            Assert.That(additional.Context.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.05d));
            Assert.That(match.DamageResult.FinalDamage,
                Is.EqualTo((long)Math.Floor(match.DamageResult.RawDamage)));
            Assert.That(additional.DamageResult.FinalDamage,
                Is.EqualTo((long)Math.Floor(
                    additional.DamageResult.RawDamage)));
            long appliedDamage = match.AppliedDamage
                + additional.AppliedDamage;
            Assert.That(battle.Bridge.Boss.CurrentHp,
                Is.EqualTo(hpBefore - appliedDamage));
            Assert.That(battle.Bridge.DamageScore,
                Is.EqualTo(appliedDamage));
        }

        [Test]
        public void LethalMatchStopsQueuedProviderAdditional()
        {
            RuntimeBattle battle = CreateBattle(
                bossHp: 50L,
                matchProvider: new DelegateMatchEventActionProvider(context =>
                {
                    DamageAction match = Damage(context, 1d);
                    return new CombatAction[]
                    {
                        match,
                        CombatIntegrationTestSupport.AdditionalMatchDamage(
                            context,
                            match,
                            1d)
                    };
                }));

            BeginSingleMatch(battle.Coordinator);
            battle.Coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution);
            Assert.That(battle.Bridge.ResolveMatchEvent(execution), Is.True);

            Assert.That(
                battle.Bridge.LastMatchExecutionResult.CompletedActionCount,
                Is.EqualTo(1));
            Assert.That(
                battle.Bridge.LastMatchExecutionResult.ClearedRemainingActions,
                Is.True);
            Assert.That(battle.Bridge.DamageScore, Is.EqualTo(50L));
        }

        [Test]
        public void LethalProviderAdditionalStopsLaterProviderAction()
        {
            RuntimeBattle battle = CreateBattle(
                bossHp: 150L,
                matchProvider: new DelegateMatchEventActionProvider(context =>
                {
                    DamageAction match = Damage(context, 1d);
                    return new CombatAction[]
                    {
                        match,
                        CombatIntegrationTestSupport.AdditionalMatchDamage(
                            context,
                            match,
                            1d),
                        Damage(context, 1d)
                    };
                }));

            BeginSingleMatch(battle.Coordinator);
            battle.Coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution);
            Assert.That(battle.Bridge.ResolveMatchEvent(execution), Is.True);

            Assert.That(
                battle.Bridge.LastMatchExecutionResult.CompletedActionCount,
                Is.EqualTo(2));
            Assert.That(
                battle.Bridge.LastMatchExecutionResult.ActionResults[1]
                    .Action.Origin,
                Is.EqualTo(ActionOrigin.Additional));
            Assert.That(
                battle.Bridge.LastMatchExecutionResult.ClearedRemainingActions,
                Is.True);
            Assert.That(battle.Bridge.DamageScore, Is.EqualTo(150L));
        }

        [Test]
        public void TriggerDerivedActionRunsBeforeQueuedProviderAdditional()
        {
            RuntimeBattle battle = CreateBattle(
                bossHp: 1000L,
                matchProvider: new DelegateMatchEventActionProvider(context =>
                {
                    DamageAction match = Damage(context, 1d);
                    return new CombatAction[]
                    {
                        match,
                        CombatIntegrationTestSupport.AdditionalMatchDamage(
                            context,
                            match,
                            1d)
                    };
                }),
                triggerRuleFactory: actionIds =>
                    new ICombatTriggerRule[]
                    {
                        new DelegateIntegrationTriggerRule(context =>
                        {
                            if (!(context.CompletedAction
                                    is DamageAction damage)
                                || damage.Origin != ActionOrigin.Match)
                            {
                                return Array.Empty<CombatAction>();
                            }

                            return new CombatAction[]
                            {
                                new AddResourceAction(
                                    actionIds.Next(),
                                    ActionOrigin.System,
                                    context.Boss,
                                    "trigger_marker",
                                    1,
                                    context.RootActionId,
                                    context.ActionId)
                            };
                        })
                    });
            battle.Bridge.Boss.Resources.Register("trigger_marker", 1);

            BeginSingleMatch(battle.Coordinator);
            battle.Coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution);
            Assert.That(battle.Bridge.ResolveMatchEvent(execution), Is.True);

            IReadOnlyList<CombatActionResult> results = battle.Bridge
                .LastMatchExecutionResult.ActionResults;
            Assert.That(results, Has.Count.EqualTo(3));
            Assert.That(results[0].Action, Is.TypeOf<DamageAction>());
            Assert.That(results[1].Action, Is.TypeOf<AddResourceAction>());
            Assert.That(results[2].Action, Is.TypeOf<DamageAction>());
            var match = (DamageAction)results[0].Action;
            var triggerAction = (AddResourceAction)results[1].Action;
            var additional = (DamageAction)results[2].Action;

            Assert.That(results[0].ExecutionOrder, Is.EqualTo(1));
            Assert.That(match.ActionId, Is.EqualTo(1));
            Assert.That(match.RootActionId, Is.EqualTo(1));
            Assert.That(match.SourceActionId, Is.Null);
            Assert.That(match.Origin, Is.EqualTo(ActionOrigin.Match));
            Assert.That(match.ContextRequest.AttackType,
                Is.EqualTo(AttackType.Match));

            Assert.That(results[1].ExecutionOrder, Is.EqualTo(2));
            Assert.That(triggerAction.ActionId, Is.EqualTo(3));
            Assert.That(triggerAction.RootActionId, Is.EqualTo(1));
            Assert.That(triggerAction.SourceActionId, Is.EqualTo(1));
            Assert.That(triggerAction.Origin,
                Is.EqualTo(ActionOrigin.System));
            Assert.That(triggerAction.ResourceId,
                Is.EqualTo("trigger_marker"));

            Assert.That(results[2].ExecutionOrder, Is.EqualTo(3));
            Assert.That(additional.ActionId, Is.EqualTo(2));
            Assert.That(additional.RootActionId, Is.EqualTo(1));
            Assert.That(additional.SourceActionId, Is.EqualTo(1));
            Assert.That(additional.Origin,
                Is.EqualTo(ActionOrigin.Additional));
            Assert.That(additional.ContextRequest.AttackType,
                Is.EqualTo(AttackType.Additional));
            Assert.That(
                battle.Bridge.Boss.Resources.GetAmount("trigger_marker"),
                Is.EqualTo(1));
        }

        [Test]
        public void ActiveDamageIsCollected()
        {
            const string characterId = "active_character";
            const string activeId = "active_test";
            var activeProviders = new ActiveAbilityActionProviderRegistry();
            activeProviders.Register(
                characterId,
                activeId,
                new DamageActiveProvider());
            RuntimeBattle battle = CreateBattle(
                bossHp: 1000L,
                characterId: characterId,
                activeBindings: new[]
                {
                    new ActiveAbilityBinding(0, 0, characterId, activeId)
                },
                activeProviders: activeProviders,
                activeCooldowns: new[] { 0 });

            Assert.That(battle.Coordinator.StartBattle(), Is.True);
            Assert.That(battle.Bridge.TryUseActive(0), Is.True);

            Assert.That(battle.Bridge.DamageScore, Is.EqualTo(100L));
            Assert.That(
                battle.Bridge.LastActiveExecutionResult.ActionResults[0],
                Is.TypeOf<DamageActionResult>());
        }

        [Test]
        public void LethalOverkillFinalizesOnceAndSkipsRemainingAction()
        {
            RuntimeBattle battle = CreateBattle(
                bossHp: 50L,
                matchProvider: new DelegateMatchEventActionProvider(context =>
                    new CombatAction[]
                    {
                        Damage(context, 10d),
                        Damage(context, 10d)
                    }));
            int finalizedCount = 0;
            battle.Bridge.ResultFinalized += _ => finalizedCount++;

            BeginSingleMatch(battle.Coordinator);
            battle.Coordinator.TryBeginNextMatchEvent(
                out MatchEventExecution execution);
            Assert.That(battle.Bridge.ResolveMatchEvent(execution), Is.True);

            Assert.That(battle.Bridge.DamageScore, Is.EqualTo(50L));
            Assert.That(
                battle.Bridge.LastMatchExecutionResult.CompletedActionCount,
                Is.EqualTo(1));
            Assert.That(
                battle.Bridge.LastMatchExecutionResult.ClearedRemainingActions,
                Is.True);
            Assert.That(finalizedCount, Is.EqualTo(1));
            Assert.That(battle.Bridge.LastFinalResult, Is.Not.Null);
            Assert.That(
                battle.Bridge.LastFinalResult.EndReason,
                Is.EqualTo(BattleResultKind.Victory));
            Assert.That(
                battle.Coordinator.NotifyBossDefeated(),
                Is.False);
            Assert.That(finalizedCount, Is.EqualTo(1));
        }

        [Test]
        public void DefeatAndTurnLimitCreateFinalResultsWithoutBonus()
        {
            RuntimeBattle defeat = CreateBattle(
                bossHp: 1000L,
                partyHp: 50L,
                bossProvider: new DelegateBossCombatActionProvider(context =>
                    new CombatAction[]
                    {
                        CombatIntegrationTestSupport.BossDamage(context, 1d)
                    }));
            BeginBossAction(defeat.Coordinator);
            Assert.That(defeat.Bridge.ResolveBossAction(1), Is.True);

            Assert.That(
                defeat.Bridge.LastFinalResult.EndReason,
                Is.EqualTo(BattleResultKind.Defeat));
            Assert.That(defeat.Bridge.LastFinalResult.RemainingTurns, Is.Zero);
            Assert.That(
                defeat.Bridge.LastFinalResult.RemainingTurnBonus,
                Is.Zero);

            RuntimeBattle turnLimit = CreateBattle(
                bossHp: 1000L,
                turnLimit: 1);
            BeginBossAction(turnLimit.Coordinator);
            Assert.That(turnLimit.Bridge.ResolveBossAction(1), Is.True);

            Assert.That(
                turnLimit.Bridge.LastFinalResult.EndReason,
                Is.EqualTo(BattleResultKind.TurnLimitReached));
            Assert.That(
                turnLimit.Bridge.LastFinalResult.FinishedTurn,
                Is.EqualTo(1));
            Assert.That(
                turnLimit.Bridge.LastFinalResult.RemainingTurnBonus,
                Is.Zero);
        }

        [Test]
        public void AbortedDoesNotFinalizeAndNewBattleStartsEmpty()
        {
            RuntimeBattle aborted = CreateBattle(bossHp: 1000L);
            Assert.That(aborted.Coordinator.StartBattle(), Is.True);
            Assert.That(aborted.Coordinator.AbortBattle(), Is.True);
            Assert.That(aborted.Bridge.LastFinalResult, Is.Null);

            RuntimeBattle next = CreateBattle(bossHp: 1000L);
            Assert.That(next.Bridge.DamageScore, Is.Zero);
            Assert.That(next.Bridge.LastFinalResult, Is.Null);
            Assert.That(next.Coordinator.StartBattle(), Is.True);
            Assert.That(next.Bridge.DamageScore, Is.Zero);
            Assert.That(next.Bridge.LastFinalResult, Is.Null);
        }

        private static RuntimeBattle CreateBattle(
            long bossHp,
            long partyHp = 1000L,
            int turnLimit = 25,
            string characterId = "character",
            IMatchEventActionProvider matchProvider = null,
            IBossCombatActionProvider bossProvider = null,
            IReadOnlyList<ICombatTriggerRule> triggerRules = null,
            Func<CombatActionIdSequence, IReadOnlyList<ICombatTriggerRule>>
                triggerRuleFactory = null,
            IReadOnlyList<ActiveAbilityBinding> activeBindings = null,
            ActiveAbilityActionProviderRegistry activeProviders = null,
            IReadOnlyList<int> activeCooldowns = null)
        {
            var character = new CharacterBattleState(
                characterId,
                0,
                ElementType.Fire,
                partyHp,
                100d);
            var party = new PartyBattleState(new[] { character });
            var boss = new BossBattleState(
                "boss_test",
                ElementType.Fire,
                bossHp,
                100d);
            var coordinator = new BattleFlowCoordinator(
                turnLimit,
                activeCooldowns ?? Array.Empty<int>());
            var actionIds = new CombatActionIdSequence();
            IReadOnlyList<ICombatTriggerRule> resolvedTriggerRules =
                triggerRuleFactory != null
                    ? triggerRuleFactory(actionIds)
                    : triggerRules ?? Array.Empty<ICombatTriggerRule>();
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)),
                new CombatTriggerResolver(resolvedTriggerRules));
            var bridge = new BattleFlowCombatBridge(
                coordinator,
                party,
                boss,
                executor,
                matchProvider ?? new DelegateMatchEventActionProvider(
                    _ => Array.Empty<CombatAction>()),
                bossProvider ?? new DelegateBossCombatActionProvider(
                    _ => Array.Empty<CombatAction>()),
                actionIds,
                activeBindings ?? Array.Empty<ActiveAbilityBinding>(),
                activeProviders ?? new ActiveAbilityActionProviderRegistry(),
                boardMutationSink: null,
                difficultyId: BattleDifficultyIds.Challenge,
                resultBalance: BattleResultBalanceDefaults.Create());
            return new RuntimeBattle(coordinator, bridge);
        }

        private static void BeginSingleMatch(BattleFlowCoordinator coordinator)
        {
            Assert.That(coordinator.StartBattle(), Is.True);
            Assert.That(coordinator.TryBeginBoardResolution(), Is.True);
            Assert.That(
                coordinator.NotifyBoardActionResolved(
                    BattleFlowTestSupport.CreateCascade(new[]
                    {
                        BattleFlowTestSupport.Match(
                            ElementType.Fire,
                            new BoardPosition(0, 0),
                            new BoardPosition(1, 0),
                            new BoardPosition(2, 0))
                    }),
                    true),
                Is.True);
        }

        private static void BeginBossAction(BattleFlowCoordinator coordinator)
        {
            Assert.That(coordinator.StartBattle(), Is.True);
            Assert.That(coordinator.TryBeginBoardResolution(), Is.True);
            Assert.That(
                coordinator.NotifyBoardActionResolved(
                    BattleFlowTestSupport.CreateCascade(),
                    true),
                Is.True);
            Assert.That(coordinator.TryBeginNextMatchEvent(out _), Is.False);
            Assert.That(
                coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));
        }

        private static DamageAction Damage(
            MatchEventActionContext context,
            double coefficient)
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

        private static DamageAction FollowUpDamage(
            CombatActionTriggerContext context,
            CombatActionIdSequence battleActionIds,
            DamageAction source)
        {
            DamageContextBuildRequest request = source.ContextRequest;
            return new DamageAction(
                battleActionIds.Next(),
                ActionOrigin.Additional,
                new DamageContextBuildRequest(
                    request.Attacker,
                    request.Party,
                    request.TargetBoss,
                    request.AttackElement,
                    AttackType.Additional,
                    request.AttackTags,
                    2d,
                    false,
                    0,
                    false),
                context.RootActionId,
                context.ActionId);
        }

        private static EffectInstance AttackTypeEffect(
            long id,
            string effectId,
            double magnitude,
            AttackTypeMask attackTypeMask)
        {
            return new EffectInstance(
                id,
                effectId,
                "test",
                EffectCategory.Buff,
                EffectModifierType.AttackTypeDamageIncrease,
                magnitude,
                2,
                id,
                attackTypeMask: attackTypeMask);
        }

        private static EffectInstance ElementEffect(
            long id,
            string effectId,
            double magnitude,
            ElementType element)
        {
            return new EffectInstance(
                id,
                effectId,
                "test",
                EffectCategory.Buff,
                EffectModifierType.ElementDamageIncrease,
                magnitude,
                2,
                id,
                elementFilter: element);
        }

        private static EffectInstance Effect(
            long id,
            string effectId,
            EffectModifierType modifierType,
            double magnitude)
        {
            return new EffectInstance(
                id,
                effectId,
                "test",
                EffectCategory.Buff,
                modifierType,
                magnitude,
                2,
                id);
        }

        private sealed class DamageActiveProvider
            : IActiveAbilityActionProvider
        {
            public IReadOnlyList<CombatAction> CreateRootActions(
                ActiveAbilityActionContext context)
            {
                return new CombatAction[]
                {
                    new DamageAction(
                        context.ActionIds.Next(),
                        ActionOrigin.Active,
                        new DamageContextBuildRequest(
                            context.Character,
                            context.Party,
                            context.Boss,
                            context.Character.Element,
                            AttackType.Active,
                            AttackTag.None,
                            1d,
                            false,
                            0,
                            false))
                };
            }
        }

        private sealed class RuntimeBattle
        {
            public RuntimeBattle(
                BattleFlowCoordinator coordinator,
                BattleFlowCombatBridge bridge)
            {
                Coordinator = coordinator;
                Bridge = bridge;
            }

            public BattleFlowCoordinator Coordinator { get; }
            public BattleFlowCombatBridge Bridge { get; }
        }
    }
}
