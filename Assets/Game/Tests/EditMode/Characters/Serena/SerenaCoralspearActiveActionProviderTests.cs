using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Characters.Serena;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Tests.EditMode;
using ValorChronicle.Tests.EditMode.Battle.Flow;

namespace ValorChronicle.Tests.EditMode.Characters.Serena
{
    public sealed class SerenaCoralspearActiveActionProviderTests
    {
        [TestCase(0, 0.35d)]
        [TestCase(5, 0.45d)]
        public void CreateActionsCreatesFilteredSelfEffect(
            int awakening,
            double expectedMagnitude)
        {
            using (var battle = new TestBattle(awakening))
            {
                IReadOnlyList<CombatAction> actions =
                    battle.ActiveProvider.CreateActions(
                        battle.Serena,
                        battle.ActionIds);

                Assert.That(actions.Count, Is.EqualTo(1));
                Assert.That(actions.OfType<DamageAction>(), Is.Empty);
                var action = actions[0] as ApplyEffectAction;
                Assert.That(action, Is.Not.Null);
                Assert.That(action.Origin, Is.EqualTo(ActionOrigin.Active));
                Assert.That(action.RootActionId,
                    Is.EqualTo(action.ActionId));
                Assert.That(action.SourceActionId, Is.Null);
                Assert.That(action.TargetType,
                    Is.EqualTo(CombatEffectTargetType.Character));
                Assert.That(action.TargetCharacter,
                    Is.SameAs(battle.Serena));
                AssertEffect(action.Effect, expectedMagnitude);
                Assert.That(action.Effect.RuntimeId,
                    Is.EqualTo(action.ActionId));
                Assert.That(action.Effect.CreationOrder,
                    Is.EqualTo(action.ActionId));
                Assert.That(battle.ActiveProvider.CooldownTurns,
                    Is.EqualTo(8));
            }
        }

        [Test]
        public void ExecuteActiveAppliesEffectWithoutDamage()
        {
            using (var battle = new TestBattle())
            {
                CombatActionExecutionResult result = battle.ApplyActive();

                Assert.That(result.CompletedActionCount, Is.EqualTo(1));
                Assert.That(result.ActionResults[0],
                    Is.TypeOf<ApplyEffectActionResult>());
                Assert.That(result.ActionResults.OfType<DamageActionResult>(),
                    Is.Empty);
                Assert.That(battle.Serena.Effects.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void EffectAppliesToPrecreatedAdditionalAtExecutionTime()
        {
            using (var battle = new TestBattle())
            {
                battle.Water.Add(1);
                IReadOnlyList<CombatAction> matchActions =
                    battle.MatchProvider.CreateActions(
                        battle.Serena,
                        battle.Party,
                        battle.Boss,
                        BoardMatchTier.Four,
                        1,
                        battle.ActionIds);

                battle.ApplyActive();
                CombatActionExecutionResult result = battle.Execute(
                    matchActions);
                DamageActionResult[] damage = DamageResults(result);

                Assert.That(damage.Length, Is.EqualTo(2));
                Assert.That(damage[0].Context
                        .AttackTypeDamageIncreaseRateSum,
                    Is.Zero);
                Assert.That(damage[0].DamageResult.FinalDamage,
                    Is.EqualTo(1350L));
                Assert.That(damage[1].Context
                        .AttackTypeDamageIncreaseRateSum,
                    Is.EqualTo(0.35d));
                Assert.That(damage[1].DamageResult.FinalDamage,
                    Is.EqualTo(1215L));
            }
        }

        [TestCase(0, 0.35d, 1215L)]
        [TestCase(2, 0.45d, 1305L)]
        [TestCase(5, 0.55d, 1550L)]
        public void AdditionalCombinesAwakeningAndActiveInOneCategory(
            int awakening,
            double expectedIncrease,
            long expectedDamage)
        {
            using (var battle = new TestBattle(awakening))
            {
                battle.ApplyActive();
                battle.Water.Add(1);
                CombatActionExecutionResult result = battle.Execute(
                    battle.MatchProvider.CreateActions(
                        battle.Serena,
                        battle.Party,
                        battle.Boss,
                        BoardMatchTier.Four,
                        1,
                        battle.ActionIds));
                DamageActionResult additional = DamageResults(result)[1];

                Assert.That(additional.Context
                        .AttackTypeDamageIncreaseRateSum,
                    Is.EqualTo(expectedIncrease).Within(0.000000001d));
                Assert.That(additional.Context.DealtDamageIncreaseRateSum,
                    Is.Zero);
                Assert.That(additional.DamageResult.FinalDamage,
                    Is.EqualTo(expectedDamage));
            }
        }

        [TestCase(0, 0, 0.35d, 1012L)]
        [TestCase(2, 0, 0.45d, 1087L)]
        [TestCase(2, 1, 1.25d, 1687L)]
        [TestCase(5, 1, 1.35d, 1997L)]
        public void ChaseCombinesAllDamageIncreasesAdditively(
            int awakening,
            int initialCharges,
            double expectedIncrease,
            long expectedDamage)
        {
            using (var battle = new TestBattle(
                awakening,
                initialCharges))
            {
                battle.ApplyActive();
                CombatActionExecutionResult result = battle.Execute(
                    battle.CreateMareaFiveMatch(1));
                DamageActionResult chase = result.ActionResults
                    .OfType<DamageActionResult>()
                    .Single(item =>
                        item.Action.Origin == ActionOrigin.Chase);

                Assert.That(chase.Context
                        .AttackTypeDamageIncreaseRateSum,
                    Is.EqualTo(expectedIncrease).Within(0.000000001d));
                Assert.That(chase.Context.DealtDamageIncreaseRateSum,
                    Is.Zero);
                Assert.That(chase.DamageResult.FinalDamage,
                    Is.EqualTo(expectedDamage));
                Assert.That(battle.State.EmpoweredChaseCharges, Is.Zero);
            }
        }

        [Test]
        public void EffectDoesNotApplyToMatchOrOtherCharacter()
        {
            using (var battle = new TestBattle())
            {
                battle.ApplyActive();
                DamageAction serenaMatch = battle.CreateDamage(
                    battle.Serena,
                    ActionOrigin.Match,
                    AttackType.Match,
                    1d);
                DamageAction mareaAdditional = battle.CreateDamage(
                    battle.Marea,
                    ActionOrigin.Additional,
                    AttackType.Additional,
                    1d,
                    rootActionId: serenaMatch.ActionId,
                    sourceActionId: serenaMatch.ActionId);
                DamageAction mareaChase = battle.CreateDamage(
                    battle.Marea,
                    ActionOrigin.Chase,
                    AttackType.Chase,
                    1d,
                    rootActionId: serenaMatch.ActionId,
                    sourceActionId: serenaMatch.ActionId);

                DamageActionResult[] damage = DamageResults(battle.Execute(
                    new CombatAction[]
                    {
                        serenaMatch,
                        mareaAdditional,
                        mareaChase
                    }));

                Assert.That(damage[0].Context
                        .AttackTypeDamageIncreaseRateSum,
                    Is.Zero);
                Assert.That(damage[1].Context
                        .AttackTypeDamageIncreaseRateSum,
                    Is.Zero);
                Assert.That(damage[2].Context
                        .AttackTypeDamageIncreaseRateSum,
                    Is.Zero);
                Assert.That(damage[0].DamageResult.FinalDamage,
                    Is.EqualTo(1000L));
                Assert.That(damage[1].DamageResult.FinalDamage,
                    Is.EqualTo(1000L));
                Assert.That(damage[2].DamageResult.FinalDamage,
                    Is.EqualTo(1000L));
            }
        }

        [Test]
        public void NewBattleStartsWithoutPreviousActiveEffect()
        {
            using (var firstBattle = new TestBattle())
            {
                firstBattle.ApplyActive();
                Assert.That(firstBattle.Serena.Effects.Count,
                    Is.EqualTo(1));
            }

            using (var nextBattle = new TestBattle())
            {
                Assert.That(nextBattle.Serena.Effects.Count, Is.Zero);
                Assert.That(nextBattle.ExecuteSerenaAdditional()
                        .DamageResult.FinalDamage,
                    Is.EqualTo(1000L));
            }
        }

        [Test]
        public void ReapplyRefreshesDurationWithoutStacking()
        {
            using (var battle = new TestBattle())
            {
                battle.ApplyActive();
                EffectInstance original = battle.Serena.Effects
                    .GetActiveEffects()[0];
                battle.TurnEnd.ProcessTurnEnd();
                battle.TurnEnd.ProcessTurnEnd();
                Assert.That(original.RemainingTurns, Is.EqualTo(1));

                battle.ApplyActive();

                Assert.That(battle.Serena.Effects.Count, Is.EqualTo(1));
                Assert.That(battle.Serena.Effects.GetActiveEffects()[0],
                    Is.SameAs(original));
                Assert.That(original.Magnitude, Is.EqualTo(0.35d));
                Assert.That(original.RemainingTurns, Is.EqualTo(3));
            }
        }

        [Test]
        public void DurationIncludesUseTurnAndExpiresAtThirdTurnEnd()
        {
            using (var battle = new TestBattle())
            {
                battle.ApplyActive();
                for (int turn = 1; turn <= 3; turn++)
                {
                    Assert.That(battle.ExecuteSerenaAdditional()
                            .DamageResult.FinalDamage,
                        Is.EqualTo(1350L),
                        $"Turn {turn}");
                    battle.TurnEnd.ProcessTurnEnd();
                    Assert.That(battle.Serena.Effects.Count,
                        Is.EqualTo(turn < 3 ? 1 : 0),
                        $"Turn {turn} end");
                }

                Assert.That(battle.ExecuteSerenaAdditional()
                        .DamageResult.FinalDamage,
                    Is.EqualTo(1000L));
            }
        }

        [Test]
        public void ActiveExecutorUsesEightTurnCooldownContract()
        {
            using (var battle = new TestBattle())
            {
                var coordinator = new BattleFlowCoordinator(
                    25,
                    new[] { battle.ActiveProvider.CooldownTurns });
                var registry = new ActiveAbilityActionProviderRegistry();
                registry.Register(
                    SerenaCoralspearRules.CharacterId,
                    SerenaCoralspearRules.ActiveAbilityId,
                    battle.ActiveProvider);
                var binding = new ActiveAbilityBinding(
                    0,
                    battle.Serena.PartySlotIndex,
                    SerenaCoralspearRules.CharacterId,
                    SerenaCoralspearRules.ActiveAbilityId);
                var activeExecutor = new ActiveAbilityCombatExecutor(
                    coordinator,
                    battle.Party,
                    battle.Boss,
                    battle.Executor,
                    battle.ActionIds,
                    new[] { binding },
                    registry);
                coordinator.StartBattle();

                Assert.That(activeExecutor.CanExecute(0), Is.True);
                Assert.That(activeExecutor.TryExecute(0, out _), Is.True);
                ActiveAbilityRuntimeState state =
                    coordinator.Context.ActiveAbilities[0];
                Assert.That(state.RemainingCooldown, Is.EqualTo(8));
                Assert.That(activeExecutor.CanExecute(0), Is.False);
                Assert.That(activeExecutor.TryExecute(0, out _), Is.False);

                for (int turn = 0; turn < 8; turn++)
                {
                    CompleteTurnWithoutMatchEvents(coordinator);
                }

                Assert.That(state.RemainingCooldown, Is.Zero);
                Assert.That(activeExecutor.CanExecute(0), Is.True);
                Assert.That(activeExecutor.TryExecute(0, out _), Is.True);
            }
        }

        [Test]
        public void ConstructorAndCreateActionsRejectInvalidInputs()
        {
            using (var battle = new TestBattle())
            {
                Assert.Throws<ArgumentNullException>(() =>
                    new SerenaCoralspearActiveActionProvider(null));
                Assert.Throws<ArgumentException>(() =>
                    battle.ActiveProvider.CreateActions(
                        battle.Marea,
                        battle.ActionIds));
                Assert.Throws<ArgumentNullException>(() =>
                    battle.ActiveProvider.CreateActions(
                        battle.Serena,
                        null));
            }
        }

        private static void CompleteTurnWithoutMatchEvents(
            BattleFlowCoordinator coordinator)
        {
            Assert.That(coordinator.TryBeginBoardResolution(), Is.True);
            Assert.That(coordinator.NotifyBoardActionResolved(
                    BattleFlowTestSupport.CreateCascade(),
                    true),
                Is.True);
            coordinator.ExecuteRemainingMatchEvents();
            Assert.That(coordinator.CompleteBossAction(), Is.True);
        }

        private static void AssertEffect(
            EffectInstance effect,
            double magnitude)
        {
            Assert.That(effect.EffectId,
                Is.EqualTo(SerenaCoralspearRules.ActiveEffectId));
            Assert.That(effect.SourceId,
                Is.EqualTo(SerenaCoralspearRules.CharacterId));
            Assert.That(effect.Category, Is.EqualTo(EffectCategory.Buff));
            Assert.That(effect.ModifierType,
                Is.EqualTo(EffectModifierType.AttackTypeDamageIncrease));
            Assert.That(effect.Magnitude, Is.EqualTo(magnitude));
            Assert.That(effect.RemainingTurns, Is.EqualTo(3));
            Assert.That(effect.StackPolicy,
                Is.EqualTo(EffectStackPolicy.RefreshDuration));
            Assert.That(effect.AttackTypeMask,
                Is.EqualTo(
                    AttackTypeMask.Additional | AttackTypeMask.Chase));
            Assert.That(effect.RequiredAttackTags,
                Is.EqualTo(AttackTag.None));
        }

        private static DamageActionResult[] DamageResults(
            CombatActionExecutionResult result)
        {
            return result.ActionResults
                .OfType<DamageActionResult>()
                .ToArray();
        }

        private sealed class TestBattle : IDisposable
        {
            private readonly MareaBluefangCombatConfig mareaConfig;

            public TestBattle(
                int awakening = 0,
                int initialCharges = 0)
            {
                SerenaConfig = SerenaCoralspearTestConfig.Create();
                mareaConfig = MareaBluefangTestConfig.Create();
                Marea = new CharacterBattleState(
                    MareaBluefangRules.CharacterId,
                    0,
                    ElementType.Water,
                    1000L,
                    1000d);
                Serena = new CharacterBattleState(
                    SerenaCoralspearRules.CharacterId,
                    1,
                    ElementType.Water,
                    1000L,
                    1000d);
                Party = new PartyBattleState(new[] { Marea, Serena });
                Boss = new BossBattleState(
                    "training_boss",
                    ElementType.Water,
                    1000000L,
                    100d);
                Water = WaterElementResource.Register(Boss.Resources, 5);
                ActionIds = new CombatActionIdSequence();
                State = new SerenaCoralspearBattleState(
                    Serena,
                    SerenaConfig.EmpoweredChaseMaxCharges,
                    initialCharges);
                PassiveRule = new SerenaCoralspearPassiveChaseRule(
                    State,
                    SerenaConfig,
                    awakening,
                    ActionIds);
                ActiveProvider =
                    new SerenaCoralspearActiveActionProvider(
                        SerenaConfig,
                        awakening);
                MatchProvider = new SerenaCoralspearMatchActionProvider(
                    SerenaConfig,
                    awakening);
                Executor = new CombatActionExecutor(
                    Boss,
                    Party,
                    new DamageContextFactory(
                        new SeededRandomSource(123)),
                    new CombatTriggerResolver(
                        new ICombatTriggerRule[] { PassiveRule }));
                TurnEnd = new BattleTurnEndProcessor(Party, Boss);
            }

            public SerenaCoralspearCombatConfig SerenaConfig { get; }
            public CharacterBattleState Marea { get; }
            public CharacterBattleState Serena { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public ResourceState Water { get; }
            public CombatActionIdSequence ActionIds { get; }
            public SerenaCoralspearBattleState State { get; }
            public SerenaCoralspearPassiveChaseRule PassiveRule { get; }
            public SerenaCoralspearActiveActionProvider ActiveProvider
            {
                get;
            }
            public SerenaCoralspearMatchActionProvider MatchProvider
            {
                get;
            }
            public CombatActionExecutor Executor { get; }
            public BattleTurnEndProcessor TurnEnd { get; }

            public CombatActionExecutionResult ApplyActive()
            {
                return Execute(ActiveProvider.CreateActions(
                    Serena,
                    ActionIds));
            }

            public IReadOnlyList<CombatAction> CreateMareaFiveMatch(
                int waterAmount)
            {
                Water.Add(waterAmount);
                return new MareaBluefangMatchActionProvider(mareaConfig)
                    .CreateActions(
                        Marea,
                        Party,
                        Boss,
                        BoardMatchTier.FiveOrMore,
                        1,
                        ActionIds);
            }

            public DamageActionResult ExecuteSerenaAdditional()
            {
                DamageAction root = CreateDamage(
                    Serena,
                    ActionOrigin.Match,
                    AttackType.Match,
                    0d);
                DamageAction additional = CreateDamage(
                    Serena,
                    ActionOrigin.Additional,
                    AttackType.Additional,
                    1d,
                    rootActionId: root.ActionId,
                    sourceActionId: root.ActionId);
                return DamageResults(Execute(
                    new CombatAction[] { root, additional }))[1];
            }

            public DamageAction CreateDamage(
                CharacterBattleState attacker,
                ActionOrigin origin,
                AttackType attackType,
                double coefficient,
                long? rootActionId = null,
                long? sourceActionId = null)
            {
                return new DamageAction(
                    ActionIds.Next(),
                    origin,
                    new DamageContextBuildRequest(
                        attacker,
                        Party,
                        Boss,
                        ElementType.Water,
                        attackType,
                        AttackTag.None,
                        coefficient,
                        appliesCombo: false,
                        finalComboCount: 0,
                        canCritical: false),
                    rootActionId,
                    sourceActionId);
            }

            public CombatActionExecutionResult Execute(
                IReadOnlyList<CombatAction> actions)
            {
                return Executor.Execute(new CombatActionQueue(actions));
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(SerenaConfig);
                UnityEngine.Object.DestroyImmediate(mareaConfig);
            }
        }
    }
}
