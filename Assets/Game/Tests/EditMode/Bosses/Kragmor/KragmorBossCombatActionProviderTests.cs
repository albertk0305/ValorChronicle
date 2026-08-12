using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Tests.EditMode.Battle.Combat.Integration;
using ValorChronicle.Tests.EditMode.Battle.Flow;

namespace ValorChronicle.Tests.EditMode.Bosses.Kragmor
{
    public sealed class KragmorBossCombatActionProviderTests
    {
        [Test]
        public void RuntimeStateStartsAtFirstIntentAndCyclesFourActions()
        {
            var runtime = new KragmorBattleRuntimeState();

            Assert.That(runtime.PatternIndex, Is.Zero);
            Assert.That(runtime.NextActionKind,
                Is.EqualTo(KragmorActionKind.ColossusIronFist));
            Assert.That(runtime.NextIntent.ActionKind,
                Is.EqualTo(KragmorActionKind.ColossusIronFist));

            var observed = new List<KragmorActionKind>();
            var completedStates = new[]
            {
                KragmorDefenseState.VolcanicCarapace,
                KragmorDefenseState.VolcanicCarapace,
                KragmorDefenseState.CoreCompression,
                KragmorDefenseState.CoreExposure,
                KragmorDefenseState.VolcanicCarapace
            };
            for (int actionIndex = 0; actionIndex < 5; actionIndex++)
            {
                observed.Add(runtime.NextActionKind);
                runtime.CommitAction(completedStates[actionIndex]);
                Assert.That(runtime.PatternIndex,
                    Is.InRange(0, KragmorRules.PatternCount - 1));
            }

            Assert.That(observed, Is.EqualTo(new[]
            {
                KragmorActionKind.ColossusIronFist,
                KragmorActionKind.RockshardEruption,
                KragmorActionKind.CoreCompression,
                KragmorActionKind.EarthCollapse,
                KragmorActionKind.ColossusIronFist
            }));
        }

        [Test]
        public void ForecastReturnsTenWrappedIntentsWithOneBasedTurns()
        {
            var runtime = new KragmorBattleRuntimeState();
            var provider = new KragmorBossCombatActionProvider(runtime);

            IReadOnlyList<KragmorBossIntentPreview> forecast =
                provider.GetIntentForecast(10);

            Assert.That(
                forecast.Select(item => item.Intent.ActionKind),
                Is.EqualTo(new[]
                {
                    KragmorActionKind.ColossusIronFist,
                    KragmorActionKind.RockshardEruption,
                    KragmorActionKind.CoreCompression,
                    KragmorActionKind.EarthCollapse,
                    KragmorActionKind.ColossusIronFist,
                    KragmorActionKind.RockshardEruption,
                    KragmorActionKind.CoreCompression,
                    KragmorActionKind.EarthCollapse,
                    KragmorActionKind.ColossusIronFist,
                    KragmorActionKind.RockshardEruption
                }));
            Assert.That(
                forecast.Select(item => item.TurnsUntilAction),
                Is.EqualTo(Enumerable.Range(1, 10)));
            Assert.That(forecast[0].Intent, Is.SameAs(provider.NextIntent));
        }

        [Test]
        public void ForecastQueryIsSideEffectFreeAndHandlesNonPositiveCount()
        {
            var runtime = new KragmorBattleRuntimeState();
            var provider = new KragmorBossCombatActionProvider(runtime);
            BossBattleState boss = Boss(850d);
            PartyBattleState party = Party();
            var actionIds = new CombatActionIdSequence();
            int patternBefore = runtime.PatternIndex;
            KragmorDefenseState defenseBefore =
                runtime.CurrentDefenseState;
            long bossHpBefore = boss.CurrentHp;
            long partyHpBefore = party.CurrentHp;
            long lastActionIdBefore = actionIds.LastIssuedId;

            IReadOnlyList<KragmorBossIntentPreview> first =
                provider.GetIntentForecast(10);
            IReadOnlyList<KragmorBossIntentPreview> second =
                provider.GetIntentForecast(10);

            Assert.That(provider.GetIntentForecast(0), Is.Empty);
            Assert.That(provider.GetIntentForecast(-1), Is.Empty);
            Assert.That(runtime.PatternIndex, Is.EqualTo(patternBefore));
            Assert.That(runtime.CurrentDefenseState,
                Is.EqualTo(defenseBefore));
            Assert.That(boss.CurrentHp, Is.EqualTo(bossHpBefore));
            Assert.That(party.CurrentHp, Is.EqualTo(partyHpBefore));
            Assert.That(actionIds.LastIssuedId,
                Is.EqualTo(lastActionIdBefore));
            Assert.That(
                second.Select(item => item.Intent.ActionKind),
                Is.EqualTo(first.Select(item => item.Intent.ActionKind)));
        }

        [Test]
        public void ForecastSlidesOnlyAfterCommitAndWrapsAfterFullPattern()
        {
            var runtime = new KragmorBattleRuntimeState();
            var provider = new KragmorBossCombatActionProvider(runtime);
            BossBattleState boss = Boss(850d);
            PartyBattleState party = Party();
            var actionIds = new CombatActionIdSequence();
            InitializeDefense(boss, runtime);
            CombatActionExecutor executor = Executor(boss, party);

            var expectedAfterCommit = new[]
            {
                KragmorActionKind.RockshardEruption,
                KragmorActionKind.CoreCompression,
                KragmorActionKind.EarthCollapse,
                KragmorActionKind.ColossusIronFist
            };
            for (int index = 0; index < expectedAfterCommit.Length; index++)
            {
                KragmorActionKind before = provider.NextIntent.ActionKind;
                IReadOnlyList<CombatAction> actions = provider.CreateActions(
                    boss,
                    party,
                    actionIds);
                Assert.That(provider.GetIntentForecast(1)[0].Intent.ActionKind,
                    Is.EqualTo(before));
                executor.Execute(new CombatActionQueue(actions));
                Assert.That(provider.GetIntentForecast(1)[0].Intent.ActionKind,
                    Is.EqualTo(before));

                Assert.That(provider.TryCommitCompletedAction(), Is.True);
                Assert.That(provider.GetIntentForecast(1)[0].Intent.ActionKind,
                    Is.EqualTo(expectedAfterCommit[index]));
                Assert.That(provider.GetIntentForecast(1)[0].Intent,
                    Is.SameAs(provider.NextIntent));
            }
        }

        [Test]
        public void QueryingAndPlanningDoNotAdvancePattern()
        {
            var runtime = new KragmorBattleRuntimeState();
            var provider = new KragmorBossCombatActionProvider(runtime);
            BossBattleState boss = Boss(850d);
            PartyBattleState party = Party();
            var actionIds = new CombatActionIdSequence();
            InitializeDefense(boss, runtime);

            KragmorBossIntent firstIntent = runtime.NextIntent;
            KragmorBossIntent secondIntent = runtime.NextIntent;
            IReadOnlyList<CombatAction> firstPlan = provider.CreateActions(
                boss,
                party,
                actionIds);
            IReadOnlyList<CombatAction> secondPlan = provider.CreateActions(
                boss,
                party,
                actionIds);

            Assert.That(firstIntent, Is.SameAs(secondIntent));
            Assert.That(runtime.PatternIndex, Is.Zero);
            Assert.That(DamageAction(firstPlan).ContextRequest
                .AttackCoefficient, Is.EqualTo(0.75d));
            Assert.That(DamageAction(secondPlan).ContextRequest
                .AttackCoefficient, Is.EqualTo(0.75d));
        }

        [Test]
        public void PatternCreatesExpectedDamageActionsAndHeavyTag()
        {
            var runtime = new KragmorBattleRuntimeState();
            var provider = new KragmorBossCombatActionProvider(runtime);
            BossBattleState boss = Boss(850d);
            PartyBattleState party = Party();
            var actionIds = new CombatActionIdSequence();
            InitializeDefense(boss, runtime);
            CombatActionExecutor executor = Executor(boss, party);

            IReadOnlyList<CombatAction> plan = provider.CreateActions(
                boss,
                party,
                actionIds);
            AssertDamagePlan(plan, 0.75d, AttackTag.None);
            executor.Execute(new CombatActionQueue(plan));
            Assert.That(provider.TryCommitCompletedAction(), Is.True);

            plan = provider.CreateActions(boss, party, actionIds);
            AssertDamagePlan(plan, 0.65d, AttackTag.None);
            executor.Execute(new CombatActionQueue(plan));
            Assert.That(provider.TryCommitCompletedAction(), Is.True);

            plan = provider.CreateActions(boss, party, actionIds);
            Assert.That(plan.Count, Is.EqualTo(2));
            Assert.That(plan[0], Is.TypeOf<RemoveEffectAction>());
            Assert.That(plan[1], Is.TypeOf<ApplyEffectAction>());
            executor.Execute(new CombatActionQueue(plan));
            Assert.That(provider.TryCommitCompletedAction(), Is.True);

            plan = provider.CreateActions(boss, party, actionIds);
            AssertDamagePlan(plan, 2.40d, AttackTag.Heavy);
            executor.Execute(new CombatActionQueue(plan));
            Assert.That(provider.TryCommitCompletedAction(), Is.True);
            Assert.That(runtime.PatternIndex, Is.Zero);
            Assert.That(provider.TryCommitCompletedAction(), Is.False);
        }

        [Test]
        public void OnlyRockshardPlanRequestsThreeRocksWithMaximumSix()
        {
            var runtime = new KragmorBattleRuntimeState();
            var provider = new KragmorBossCombatActionProvider(runtime);
            BossBattleState boss = Boss(850d);
            PartyBattleState party = Party();
            var actionIds = new CombatActionIdSequence();
            InitializeDefense(boss, runtime);
            CombatActionExecutor executor = Executor(boss, party);

            BossActionPlan ironFist = provider.CreatePlan(
                Context(boss, party, 1));
            Assert.That(ironFist.BoardCommand, Is.Null);
            executor.Execute(new CombatActionQueue(ironFist.CombatActions));
            Assert.That(provider.TryCommitCompletedAction(), Is.True);

            BossActionPlan rockshard = provider.CreatePlan(
                Context(boss, party, 2));
            Assert.That(rockshard.BoardCommand, Is.Not.Null);
            Assert.That(rockshard.BoardCommand.Kind,
                Is.EqualTo(BattleBoardMutationKind.CreateRock));
            Assert.That(rockshard.BoardCommand.RequestedCount,
                Is.EqualTo(KragmorRules.RockshardRockCreationCount));
            Assert.That(rockshard.BoardCommand.MaximumCount,
                Is.EqualTo(KragmorRules.MaximumRockCount));
            Assert.That(runtime.PatternIndex, Is.EqualTo(1));
            executor.Execute(new CombatActionQueue(
                rockshard.CombatActions));
            Assert.That(provider.TryCommitCompletedAction(), Is.True);

            BossActionPlan compression = provider.CreatePlan(
                Context(boss, party, 3));
            Assert.That(compression.BoardCommand, Is.Null);
            executor.Execute(new CombatActionQueue(
                compression.CombatActions));
            Assert.That(provider.TryCommitCompletedAction(), Is.True);

            BossActionPlan collapse = provider.CreatePlan(
                Context(boss, party, 4));
            Assert.That(collapse.BoardCommand, Is.Null);
        }

        [Test]
        public void IntentDependsOnRuntimePatternNotBattleTurn()
        {
            PartyBattleState party = Party();
            BossBattleState firstBoss = Boss(850d);
            var firstRuntime = new KragmorBattleRuntimeState();
            InitializeDefense(firstBoss, firstRuntime);
            var firstProvider = new KragmorBossCombatActionProvider(
                firstRuntime);
            BossBattleState laterBoss = Boss(850d);
            var laterRuntime = new KragmorBattleRuntimeState();
            InitializeDefense(laterBoss, laterRuntime);
            var laterProvider = new KragmorBossCombatActionProvider(
                laterRuntime);

            IReadOnlyList<CombatAction> first = firstProvider
                .CreateRootActions(Context(
                    firstBoss,
                    party,
                    currentTurn: 1));
            IReadOnlyList<CombatAction> later = laterProvider
                .CreateRootActions(Context(
                    laterBoss,
                    party,
                    currentTurn: 99));

            Assert.That(DamageAction(first).ContextRequest.AttackCoefficient,
                Is.EqualTo(0.75d));
            Assert.That(DamageAction(later).ContextRequest.AttackCoefficient,
                Is.EqualTo(0.75d));
        }

        [Test]
        public void BridgeExecutesFullCycleAndCommitsOnlyOncePerTurn()
        {
            BattleHarness battle = CreateBattle(850d, partyHp: 10000);
            long hpBefore = battle.Party.CurrentHp;

            BossDamageActionResult first = ResolveDamage(battle, 1);
            Assert.That(first.DamageResult.FinalDamageBeforeShield,
                Is.EqualTo(637));
            Assert.That(battle.Party.CurrentHp, Is.EqualTo(hpBefore - 637));
            Assert.That(battle.Runtime.PatternIndex, Is.EqualTo(1));
            Assert.That(battle.Bridge.ResolveBossAction(1), Is.False);
            Assert.That(battle.Runtime.PatternIndex, Is.EqualTo(1));

            BossDamageActionResult second = ResolveDamage(battle, 2);
            Assert.That(second.DamageResult.FinalDamageBeforeShield,
                Is.EqualTo(552));
            Assert.That(battle.Runtime.PatternIndex, Is.EqualTo(2));

            EnterBossActing(battle.Coordinator);
            long hpBeforeCore = battle.Party.CurrentHp;
            Assert.That(battle.Bridge.ResolveBossAction(3), Is.True);
            Assert.That(battle.Bridge.LastBossExecutionResult.ActionResults,
                Has.Count.EqualTo(2));
            Assert.That(battle.Party.CurrentHp, Is.EqualTo(hpBeforeCore));
            Assert.That(battle.Runtime.PatternIndex, Is.EqualTo(3));

            BossDamageActionResult fourth = ResolveDamage(battle, 4);
            Assert.That(fourth.DamageResult.FinalDamageBeforeShield,
                Is.EqualTo(2040));
            Assert.That(((BossDamageAction)fourth.Action)
                    .ContextRequest.AttackTags,
                Is.EqualTo(AttackTag.Heavy));
            Assert.That(battle.Runtime.PatternIndex, Is.Zero);

            BossDamageActionResult fifth = ResolveDamage(battle, 5);
            Assert.That(fifth.DamageResult.FinalDamageBeforeShield,
                Is.EqualTo(637));
            Assert.That(battle.Runtime.PatternIndex, Is.EqualTo(1));
        }

        [TestCase(500d, 375L)]
        [TestCase(1200d, 900L)]
        [TestCase(1550d, 1162L)]
        [TestCase(2100d, 1575L)]
        public void FirstActionUsesSelectedDifficultyAttack(
            double bossAttack,
            long expectedDamage)
        {
            BattleHarness battle = CreateBattle(
                bossAttack,
                partyHp: 100000);

            BossDamageActionResult result = ResolveDamage(battle, 1);

            Assert.That(result.Context.BaseAttack, Is.EqualTo(bossAttack));
            Assert.That(result.DamageResult.FinalDamageBeforeShield,
                Is.EqualTo(expectedDamage));
        }

        [Test]
        public void LethalBossActionDoesNotCommitPattern()
        {
            BattleHarness battle = CreateBattle(850d, partyHp: 100);
            EnterBossActing(battle.Coordinator);

            Assert.That(battle.Bridge.ResolveBossAction(1), Is.True);

            Assert.That(battle.Party.IsIncapacitated, Is.True);
            Assert.That(battle.Coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.Defeat));
            Assert.That(battle.Runtime.PatternIndex, Is.Zero);
        }

        private static BossDamageActionResult ResolveDamage(
            BattleHarness battle,
            int expectedTurn)
        {
            EnterBossActing(battle.Coordinator);
            Assert.That(battle.Coordinator.Context.CurrentTurn,
                Is.EqualTo(expectedTurn));
            Assert.That(battle.Bridge.ResolveBossAction(expectedTurn),
                Is.True);
            return battle.Bridge.LastBossExecutionResult.ActionResults
                .OfType<BossDamageActionResult>()
                .Single();
        }

        private static void EnterBossActing(
            BattleFlowCoordinator coordinator)
        {
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.PlayerInput));
            Assert.That(coordinator.TryBeginBoardResolution(), Is.True);
            Assert.That(coordinator.NotifyBoardActionResolved(
                cascade: BattleFlowTestSupport.CreateCascade(),
                consumesTurn: true), Is.True);
            Assert.That(coordinator.TryBeginNextMatchEvent(out _), Is.False);
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));
        }

        private static BattleHarness CreateBattle(
            double bossAttack,
            long partyHp)
        {
            PartyBattleState party = Party(partyHp);
            BossBattleState boss = Boss(bossAttack);
            var coordinator = new BattleFlowCoordinator(turnLimit: 10);
            var runtime = new KragmorBattleRuntimeState();
            var provider = new KragmorBossCombatActionProvider(runtime);
            var actionIds = new CombatActionIdSequence();
            KragmorDefenseEffectFactory.InitializeBattle(
                boss,
                runtime,
                actionIds);
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)),
                new CombatTriggerResolver(Array.Empty<ICombatTriggerRule>()));
            var bridge = new BattleFlowCombatBridge(
                coordinator,
                party,
                boss,
                executor,
                new EmptyMatchEventActionProvider(),
                provider,
                actionIds,
                new ImmediateSuccessfulBoardMutationSink());
            coordinator.StartBattle();
            return new BattleHarness(
                coordinator,
                party,
                runtime,
                bridge);
        }

        private static void AssertDamagePlan(
            IReadOnlyList<CombatAction> actions,
            double coefficient,
            AttackTag tags)
        {
            BossDamageAction action = actions.OfType<BossDamageAction>()
                .Single();
            Assert.That(action.ContextRequest.AttackCoefficient,
                Is.EqualTo(coefficient));
            Assert.That(action.ContextRequest.AttackTags, Is.EqualTo(tags));
        }

        private static BossDamageAction DamageAction(
            IReadOnlyList<CombatAction> actions)
        {
            Assert.That(actions.Count, Is.EqualTo(1));
            Assert.That(actions[0], Is.TypeOf<BossDamageAction>());
            return (BossDamageAction)actions[0];
        }

        private static CombatActionExecutor Executor(
            BossBattleState boss,
            PartyBattleState party)
        {
            return new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)));
        }

        private static void InitializeDefense(
            BossBattleState boss,
            KragmorBattleRuntimeState runtime)
        {
            boss.Effects.ApplyEffect(KragmorDefenseEffectFactory.Create(
                runtime.CurrentDefenseState,
                runtimeId: 1000));
        }

        private static BossCombatActionContext Context(
            BossBattleState boss,
            PartyBattleState party,
            int currentTurn)
        {
            return (BossCombatActionContext)Activator.CreateInstance(
                typeof(BossCombatActionContext),
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: new object[]
                {
                    boss,
                    party,
                    currentTurn,
                    new CombatActionIdSequence()
                },
                culture: null);
        }

        private static BossBattleState Boss(double attack)
        {
            return new BossBattleState(
                KragmorRules.BossId,
                ElementType.Fire,
                maxHp: 100000,
                attack);
        }

        private static PartyBattleState Party(long hp = 10000)
        {
            return new PartyBattleState(new[]
            {
                new CharacterBattleState(
                    "test_character",
                    partySlotIndex: 0,
                    ElementType.Water,
                    hp,
                    attack: 1d)
            });
        }

        private sealed class EmptyMatchEventActionProvider
            : IMatchEventActionProvider
        {
            public IReadOnlyList<CombatAction> CreateRootActions(
                MatchEventActionContext context)
            {
                return Array.Empty<CombatAction>();
            }
        }

        private sealed class BattleHarness
        {
            public BattleHarness(
                BattleFlowCoordinator coordinator,
                PartyBattleState party,
                KragmorBattleRuntimeState runtime,
                BattleFlowCombatBridge bridge)
            {
                Coordinator = coordinator;
                Party = party;
                Runtime = runtime;
                Bridge = bridge;
            }

            public BattleFlowCoordinator Coordinator { get; }
            public PartyBattleState Party { get; }
            public KragmorBattleRuntimeState Runtime { get; }
            public BattleFlowCombatBridge Bridge { get; }
        }
    }
}
