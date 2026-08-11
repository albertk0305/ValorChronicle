using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Bosses.Kragmor;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Tests.EditMode.Battle.Combat.Integration;

namespace ValorChronicle.Tests.EditMode.Bosses.Kragmor
{
    public sealed class KragmorDefenseLifecycleTests
    {
        [Test]
        public void InitializationAppliesOneIndefiniteCarapaceAndMultiplier()
        {
            BossBattleState boss = Boss();
            var runtime = new KragmorBattleRuntimeState();
            var actionIds = new CombatActionIdSequence();

            EffectInstance effect =
                KragmorDefenseEffectFactory.InitializeBattle(
                    boss,
                    runtime,
                    actionIds);

            Assert.That(runtime.CurrentDefenseState,
                Is.EqualTo(KragmorDefenseState.VolcanicCarapace));
            Assert.That(runtime.CurrentDefenseEffectId,
                Is.EqualTo(KragmorRules.VolcanicCarapaceEffectId));
            Assert.That(effect.EffectId,
                Is.EqualTo(KragmorRules.VolcanicCarapaceEffectId));
            Assert.That(effect.RemainingTurns, Is.Null);
            AssertDefense(boss, runtime,
                KragmorDefenseState.VolcanicCarapace);
            Assert.That(PlayerDamageMultiplier(boss),
                Is.EqualTo(0.90d).Within(0.000000001d));
        }

        [TestCase(KragmorDefenseState.VolcanicCarapace, 0.90d, 1.05d)]
        [TestCase(KragmorDefenseState.CoreCompression, 0.80d, 0.95d)]
        [TestCase(KragmorDefenseState.CoreExposure, 1.30d, 1.45d)]
        public void DefenseMultiplierCombinesWithExternalEffectAdditively(
            KragmorDefenseState state,
            double expectedKragmorOnly,
            double expectedWithExternal)
        {
            BossBattleState boss = Boss();
            boss.Effects.ApplyEffect(
                KragmorDefenseEffectFactory.Create(state, runtimeId: 1));

            Assert.That(PlayerDamageMultiplier(boss),
                Is.EqualTo(expectedKragmorOnly).Within(0.000000001d));

            boss.Effects.ApplyEffect(ExternalVulnerability(runtimeId: 2));

            Assert.That(PlayerDamageMultiplier(boss),
                Is.EqualTo(expectedWithExternal).Within(0.000000001d));
        }

        [Test]
        public void FullPatternTransitionsExclusiveDefenseAndPreservesExternal()
        {
            BattleHarness battle = CreateBattle(
                partyHp: 100000,
                includeExternalEffect: true);

            AssertDefense(battle.Boss, battle.Runtime,
                KragmorDefenseState.VolcanicCarapace);
            Assert.That(PlayerDamageMultiplier(battle.Boss),
                Is.EqualTo(1.05d).Within(0.000000001d));

            ResolveBossAction(battle, expectedTurn: 1);
            AssertDefense(battle.Boss, battle.Runtime,
                KragmorDefenseState.VolcanicCarapace);

            ResolveBossAction(battle, expectedTurn: 2);
            AssertDefense(battle.Boss, battle.Runtime,
                KragmorDefenseState.VolcanicCarapace);

            EnterBossActing(battle.Coordinator);
            IReadOnlyList<CombatAction> compressionPreview =
                battle.Provider.CreateActions(
                    battle.Boss,
                    battle.Party,
                    battle.ActionIds);
            Assert.That(compressionPreview.Select(action => action.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(RemoveEffectAction),
                    typeof(ApplyEffectAction)
                }));
            Assert.That(battle.Runtime.PatternIndex, Is.EqualTo(2));
            AssertDefense(battle.Boss, battle.Runtime,
                KragmorDefenseState.VolcanicCarapace);

            Assert.That(battle.Bridge.ResolveBossAction(3), Is.True);
            Assert.That(battle.Bridge.LastBossExecutionResult.ActionResults
                    .Select(result => result.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(RemoveEffectActionResult),
                    typeof(ApplyEffectActionResult)
                }));
            AssertDefense(battle.Boss, battle.Runtime,
                KragmorDefenseState.CoreCompression);
            Assert.That(battle.Runtime.NextActionKind,
                Is.EqualTo(KragmorActionKind.EarthCollapse));
            Assert.That(PlayerDamageMultiplier(battle.Boss),
                Is.EqualTo(0.95d).Within(0.000000001d));

            ResolveBossAction(battle, expectedTurn: 4);
            Assert.That(battle.Bridge.LastBossExecutionResult.ActionResults
                    .Select(result => result.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(BossDamageActionResult),
                    typeof(RemoveEffectActionResult),
                    typeof(ApplyEffectActionResult)
                }));
            AssertDefense(battle.Boss, battle.Runtime,
                KragmorDefenseState.CoreExposure);
            Assert.That(battle.Runtime.NextActionKind,
                Is.EqualTo(KragmorActionKind.ColossusIronFist));
            Assert.That(PlayerDamageMultiplier(battle.Boss),
                Is.EqualTo(1.45d).Within(0.000000001d));
            Assert.That(battle.Observer.DefenseAtBossDamage.Last(),
                Is.EqualTo(KragmorRules.CoreCompressionEffectId));

            EnterBossActing(battle.Coordinator);
            IReadOnlyList<CombatAction> ironFistPreview =
                battle.Provider.CreateActions(
                    battle.Boss,
                    battle.Party,
                    battle.ActionIds);
            Assert.That(ironFistPreview.Select(action => action.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(RemoveEffectAction),
                    typeof(ApplyEffectAction),
                    typeof(BossDamageAction)
                }));
            AssertDefense(battle.Boss, battle.Runtime,
                KragmorDefenseState.CoreExposure);

            Assert.That(battle.Bridge.ResolveBossAction(5), Is.True);
            AssertDefense(battle.Boss, battle.Runtime,
                KragmorDefenseState.VolcanicCarapace);
            Assert.That(battle.Runtime.PatternIndex, Is.EqualTo(1));
            Assert.That(battle.Observer.DefenseAtBossDamage.Last(),
                Is.EqualTo(KragmorRules.VolcanicCarapaceEffectId));
            Assert.That(PlayerDamageMultiplier(battle.Boss),
                Is.EqualTo(1.05d).Within(0.000000001d));
            Assert.That(battle.Boss.Effects.FindByEffectId(
                ExternalEffectId), Has.Count.EqualTo(1));

            ResolveBossAction(battle, expectedTurn: 6);
            AssertDefense(battle.Boss, battle.Runtime,
                KragmorDefenseState.VolcanicCarapace);
            Assert.That(battle.Boss.Effects.FindByEffectId(
                ExternalEffectId), Has.Count.EqualTo(1));
        }

        [Test]
        public void LethalEarthCollapseStopsBeforeTransitionAndCommit()
        {
            BattleHarness battle = CreateBattle(
                partyHp: 2500,
                includeExternalEffect: false);
            ResolveBossAction(battle, expectedTurn: 1);
            ResolveBossAction(battle, expectedTurn: 2);
            ResolveBossAction(battle, expectedTurn: 3);
            AssertDefense(battle.Boss, battle.Runtime,
                KragmorDefenseState.CoreCompression);

            EnterBossActing(battle.Coordinator);
            Assert.That(battle.Bridge.ResolveBossAction(4), Is.True);

            Assert.That(battle.Party.IsIncapacitated, Is.True);
            Assert.That(battle.Coordinator.Context.Result,
                Is.EqualTo(BattleResultKind.Defeat));
            Assert.That(battle.Bridge.LastBossExecutionResult.ActionResults,
                Has.Count.EqualTo(1));
            Assert.That(battle.Bridge.LastBossExecutionResult
                .ActionResults[0], Is.TypeOf<BossDamageActionResult>());
            Assert.That(battle.Runtime.PatternIndex, Is.EqualTo(3));
            AssertDefense(battle.Boss, battle.Runtime,
                KragmorDefenseState.CoreCompression);
        }

        private const string ExternalEffectId =
            "effect_test_external_vulnerability";

        private static void ResolveBossAction(
            BattleHarness battle,
            int expectedTurn)
        {
            EnterBossActing(battle.Coordinator);
            Assert.That(battle.Coordinator.Context.CurrentTurn,
                Is.EqualTo(expectedTurn));
            Assert.That(battle.Bridge.ResolveBossAction(expectedTurn),
                Is.True);
        }

        private static void EnterBossActing(
            BattleFlowCoordinator coordinator)
        {
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.ActiveInput));
            Assert.That(coordinator.CompleteActiveInput(), Is.True);
            Assert.That(coordinator.NotifyBoardActionStarted(), Is.True);
            Assert.That(coordinator.NotifyBoardActionResolved(
                cascade: null,
                consumesTurn: true), Is.True);
            Assert.That(coordinator.TryBeginNextMatchEvent(out _), Is.False);
            Assert.That(coordinator.Context.Phase,
                Is.EqualTo(BattlePhase.BossActing));
        }

        private static BattleHarness CreateBattle(
            long partyHp,
            bool includeExternalEffect)
        {
            CharacterBattleState character = Character(partyHp);
            var party = new PartyBattleState(new[] { character });
            BossBattleState boss = Boss();
            var runtime = new KragmorBattleRuntimeState();
            var actionIds = new CombatActionIdSequence();
            KragmorDefenseEffectFactory.InitializeBattle(
                boss,
                runtime,
                actionIds);
            if (includeExternalEffect)
            {
                boss.Effects.ApplyEffect(
                    ExternalVulnerability(runtimeId: 5000));
            }

            var observer = new BossDamageDefenseObserver();
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)),
                new CombatTriggerResolver(new ICombatTriggerRule[]
                {
                    observer
                }));
            var coordinator = new BattleFlowCoordinator(turnLimit: 10);
            var provider = new KragmorBossCombatActionProvider(runtime);
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
                boss,
                runtime,
                actionIds,
                provider,
                bridge,
                observer);
        }

        private static double PlayerDamageMultiplier(BossBattleState boss)
        {
            CharacterBattleState character = Character(maxHp: 10000);
            var party = new PartyBattleState(new[] { character });
            var request = new DamageContextBuildRequest(
                character,
                party,
                boss,
                ElementType.Fire,
                AttackType.Active,
                AttackTag.None,
                skillCoefficient: 1d,
                appliesCombo: false,
                finalComboCount: 0,
                canCritical: false);
            DamageContext context = new DamageContextFactory(
                new SeededRandomSource(1)).Build(request);
            return DamageCalculator.Calculate(context)
                .TargetTakenDamageMultiplier;
        }

        private static void AssertDefense(
            BossBattleState boss,
            KragmorBattleRuntimeState runtime,
            KragmorDefenseState expectedState)
        {
            Assert.That(runtime.CurrentDefenseState,
                Is.EqualTo(expectedState));
            Assert.That(
                KragmorDefenseEffectFactory.CountActiveDefenseEffects(boss),
                Is.EqualTo(1));
            EffectInstance effect =
                KragmorDefenseEffectFactory.GetRequiredActiveEffect(
                    boss,
                    expectedState);
            Assert.That(effect.RemainingTurns, Is.Null);
        }

        private static EffectInstance ExternalVulnerability(long runtimeId)
        {
            return new EffectInstance(
                runtimeId,
                ExternalEffectId,
                "test_external_source",
                EffectCategory.Debuff,
                EffectModifierType.TargetTakenDamageIncrease,
                magnitude: 0.15d,
                remainingTurns: null,
                creationOrder: runtimeId,
                EffectStackPolicy.Unique);
        }

        private static CharacterBattleState Character(long maxHp)
        {
            return new CharacterBattleState(
                "test_character",
                partySlotIndex: 0,
                ElementType.Fire,
                maxHp,
                attack: 100d);
        }

        private static BossBattleState Boss()
        {
            return new BossBattleState(
                KragmorRules.BossId,
                ElementType.Fire,
                maxHp: 100000,
                attack: 850d);
        }

        private static string ActiveDefenseEffectId(BossBattleState boss)
        {
            return boss.Effects.GetActiveEffects()
                .Single(effect =>
                    KragmorDefenseEffectFactory.IsDefenseEffectId(
                        effect.EffectId))
                .EffectId;
        }

        private sealed class BossDamageDefenseObserver : ICombatTriggerRule
        {
            public List<string> DefenseAtBossDamage { get; } =
                new List<string>();

            public IReadOnlyList<CombatAction> CreateDerivedActions(
                CombatActionTriggerContext context)
            {
                if (context.CompletedAction is BossDamageAction)
                {
                    DefenseAtBossDamage.Add(
                        ActiveDefenseEffectId(context.Boss));
                }

                return Array.Empty<CombatAction>();
            }
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
                BossBattleState boss,
                KragmorBattleRuntimeState runtime,
                CombatActionIdSequence actionIds,
                KragmorBossCombatActionProvider provider,
                BattleFlowCombatBridge bridge,
                BossDamageDefenseObserver observer)
            {
                Coordinator = coordinator;
                Party = party;
                Boss = boss;
                Runtime = runtime;
                ActionIds = actionIds;
                Provider = provider;
                Bridge = bridge;
                Observer = observer;
            }

            public BattleFlowCoordinator Coordinator { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public KragmorBattleRuntimeState Runtime { get; }
            public CombatActionIdSequence ActionIds { get; }
            public KragmorBossCombatActionProvider Provider { get; }
            public BattleFlowCombatBridge Bridge { get; }
            public BossDamageDefenseObserver Observer { get; }
        }
    }
}
