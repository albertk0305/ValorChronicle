using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Flow.Presentation;
using ValorChronicle.Characters.Brina;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Tests.EditMode.Battle.Flow;

namespace ValorChronicle.Tests.EditMode.Characters.Brina
{
    public sealed class BrinaAshwoodActiveActionProviderTests
    {
        [TestCase(0, 8)]
        [TestCase(4, 8)]
        [TestCase(5, 7)]
        [TestCase(6, 7)]
        public void ProviderUsesResolvedCooldown(
            int awakening,
            int expectedCooldown)
        {
            using (var battle = new ActiveBattle(awakening))
            {
                Assert.That(battle.Provider.CooldownTurns,
                    Is.EqualTo(expectedCooldown));
            }
        }

        [Test]
        public void CreatesOnlyOneRootActiveMarkerAction()
        {
            using (var battle = new ActiveBattle())
            {
                IReadOnlyList<CombatAction> actions = battle.CreateActive();

                Assert.That(actions.Count, Is.EqualTo(1));
                Assert.That(actions.OfType<DamageAction>(), Is.Empty);
                Assert.That(actions.OfType<AddResourceAction>(), Is.Empty);
                ApplyEffectAction action = actions[0]
                    as ApplyEffectAction;
                Assert.That(action, Is.Not.Null);
                Assert.That(action.Origin, Is.EqualTo(ActionOrigin.Active));
                Assert.That(action.RootActionId,
                    Is.EqualTo(action.ActionId));
                Assert.That(action.SourceActionId, Is.Null);
                Assert.That(action.TargetCharacter,
                    Is.SameAs(battle.Character));
                AssertMarker(action.Effect, action.ActionId);
            }
        }

        [Test]
        public void ExecutionAppliesMarkerWithoutChangingWoodcut()
        {
            using (var battle = new ActiveBattle())
            {
                battle.State.GainWoodcut(6);

                CombatActionExecutionResult result = battle.ApplyActive();

                Assert.That(result.CompletedActionCount, Is.EqualTo(1));
                Assert.That(result.ActionResults[0],
                    Is.TypeOf<ApplyEffectActionResult>());
                Assert.That(battle.Character.Effects.Count, Is.EqualTo(1));
                Assert.That(battle.State.WoodcutCount, Is.EqualTo(6));
            }
        }

        [Test]
        public void MarkerDoesNotMultiplyDirectWoodcutGrants()
        {
            using (var battle = new ActiveBattle())
            {
                battle.ApplyActive();

                battle.State.GainWoodcut(2);

                Assert.That(battle.State.WoodcutCount, Is.EqualTo(2));
            }
        }

        [Test]
        public void ZeroMagnitudeMarkerDoesNotChangeDamageCalculation()
        {
            using (var battle = new ActiveBattle())
            {
                DamageActionResult before = battle.ExecuteSimpleDamage();
                battle.ApplyActive();
                DamageActionResult after = battle.ExecuteSimpleDamage();

                Assert.That(before.DamageResult.FinalDamage,
                    Is.EqualTo(1000L));
                Assert.That(after.Context.AttackIncreaseRateSum, Is.Zero);
                Assert.That(after.DamageResult.FinalDamage,
                    Is.EqualTo(1000L));
            }
        }

        [Test]
        public void ExistingHudProjectionIncludesZeroMagnitudeMarker()
        {
            using (var battle = new ActiveBattle())
            {
                battle.ApplyActive();

                IReadOnlyList<BattleHudEffectProjection> projections =
                    BattleHudEffectProjector.CreateDefault().Project(
                        battle.Character.Effects.GetActiveEffects());

                Assert.That(projections.Count, Is.EqualTo(1));
                Assert.That(projections[0].EffectId,
                    Is.EqualTo(BrinaAshwoodRules.ActiveEffectId));
                Assert.That(projections[0].BadgeText, Is.EqualTo("1"));
                Assert.That(projections[0].BadgeVisible, Is.True);
            }
        }

        [Test]
        public void MarkerRemainsUntilTurnEndThenExpires()
        {
            using (var battle = new ActiveBattle())
            {
                battle.ApplyActive();
                EffectInstance marker = battle.Character.Effects
                    .GetActiveEffects().Single();

                Assert.That(marker.RemainingTurns, Is.EqualTo(1));
                Assert.That(battle.Character.Effects.Count, Is.EqualTo(1));

                battle.TurnEnd.ProcessTurnEnd();

                Assert.That(battle.Character.Effects.Count, Is.Zero);
            }
        }

        [Test]
        public void ActiveExecutorAppliesMarkerAndCommitsCooldown()
        {
            using (var battle = new ActiveBattle())
            {
                var coordinator = new BattleFlowCoordinator(
                    25,
                    new[] { battle.Provider.CooldownTurns });
                var registry = new ActiveAbilityActionProviderRegistry();
                registry.Register(
                    BrinaAshwoodRules.CharacterId,
                    BrinaAshwoodRules.ActiveAbilityId,
                    battle.Provider);
                var binding = new ActiveAbilityBinding(
                    0,
                    battle.Character.PartySlotIndex,
                    BrinaAshwoodRules.CharacterId,
                    BrinaAshwoodRules.ActiveAbilityId);
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
                Assert.That(
                    activeExecutor.TryExecute(0, out var result),
                    Is.True);

                ActiveAbilityRuntimeState runtime =
                    coordinator.Context.ActiveAbilities[0];
                Assert.That(result.CompletedActionCount, Is.EqualTo(1));
                Assert.That(battle.Character.Effects.Count, Is.EqualTo(1));
                Assert.That(runtime.RemainingCooldown, Is.EqualTo(8));
                Assert.That(runtime.UsedThisTurn, Is.True);
                Assert.That(activeExecutor.CanExecute(0), Is.False);
                Assert.That(activeExecutor.TryExecute(0, out _), Is.False);
            }
        }

        [Test]
        public void ReapplyRefreshesMarkerWithoutDuplicatingIt()
        {
            using (var battle = new ActiveBattle())
            {
                battle.ApplyActive();
                EffectInstance original = battle.Character.Effects
                    .GetActiveEffects().Single();

                battle.ApplyActive();

                Assert.That(battle.Character.Effects.Count, Is.EqualTo(1));
                Assert.That(battle.Character.Effects.GetActiveEffects()[0],
                    Is.SameAs(original));
                Assert.That(original.RemainingTurns, Is.EqualTo(1));
            }
        }

        [TestCase(8, "1,9,17,25")]
        [TestCase(7, "1,8,15,22")]
        public void TwentyFiveTurnRuntimeUsesEngineCooldownTiming(
            int cooldown,
            string expectedUseTurns)
        {
            var coordinator = new BattleFlowCoordinator(
                25,
                new[] { cooldown });
            Assert.That(coordinator.StartBattle(), Is.True);
            var actualUseTurns = new List<int>();

            for (int turn = 1; turn <= 25; turn++)
            {
                if (coordinator.CanUseActiveAbility(0))
                {
                    Assert.That(
                        coordinator.TryUseActiveAbility(0),
                        Is.True);
                    actualUseTurns.Add(turn);
                    Assert.That(
                        coordinator.TryUseActiveAbility(0),
                        Is.False,
                        "The Active cannot be used twice in one turn.");
                }

                if (turn < 25)
                {
                    CompleteTurnWithoutMatches(coordinator);
                }
            }

            Assert.That(string.Join(",", actualUseTurns),
                Is.EqualTo(expectedUseTurns));
        }

        [Test]
        public void NonConsumingBoardAttemptDoesNotAdvanceActiveState()
        {
            using (var battle = new ActiveBattle())
            {
                var coordinator = new BattleFlowCoordinator(
                    25,
                    new[] { battle.Provider.CooldownTurns });
                coordinator.StartBattle();
                Assert.That(coordinator.TryUseActiveAbility(0), Is.True);
                battle.ApplyActive();
                ActiveAbilityRuntimeState runtime =
                    coordinator.Context.ActiveAbilities[0];

                // Invalid swaps do not enter BoardResolving or turn end.
                Assert.That(coordinator.Context.Phase,
                    Is.EqualTo(BattlePhase.PlayerInput));
                Assert.That(runtime.RemainingCooldown, Is.EqualTo(8));
                Assert.That(runtime.UsedThisTurn, Is.True);
                Assert.That(battle.Character.Effects.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void SeparateBattleDoesNotInheritActiveMarker()
        {
            using (var first = new ActiveBattle())
            using (var second = new ActiveBattle())
            {
                first.ApplyActive();

                Assert.That(first.Character.Effects.Count, Is.EqualTo(1));
                Assert.That(second.Character.Effects.Count, Is.Zero);
                Assert.That(second.State.WoodcutCount, Is.Zero);
                Assert.That(second.State.LastWoodcutGainTurn, Is.Null);
            }
        }

        private static void CompleteTurnWithoutMatches(
            BattleFlowCoordinator coordinator)
        {
            Assert.That(coordinator.TryBeginBoardResolution(), Is.True);
            Assert.That(
                coordinator.NotifyBoardActionResolved(
                    BattleFlowTestSupport.CreateCascade(),
                    consumesTurn: true),
                Is.True);
            coordinator.ExecuteRemainingMatchEvents();
            Assert.That(coordinator.CompleteBossAction(), Is.True);
        }

        private static void AssertMarker(
            EffectInstance effect,
            long actionId)
        {
            Assert.That(effect.RuntimeId, Is.EqualTo(actionId));
            Assert.That(effect.CreationOrder, Is.EqualTo(actionId));
            Assert.That(effect.EffectId,
                Is.EqualTo(BrinaAshwoodRules.ActiveEffectId));
            Assert.That(effect.SourceId,
                Is.EqualTo(BrinaAshwoodRules.CharacterId));
            Assert.That(effect.Category, Is.EqualTo(EffectCategory.Buff));
            Assert.That(effect.ModifierType,
                Is.EqualTo(EffectModifierType.AttackIncrease));
            Assert.That(effect.Magnitude, Is.Zero);
            Assert.That(effect.RemainingTurns, Is.EqualTo(1));
            Assert.That(effect.StackPolicy,
                Is.EqualTo(EffectStackPolicy.RefreshDuration));
        }

        private sealed class ActiveBattle : IDisposable
        {
            private readonly BrinaAshwoodCombatConfig config;

            public ActiveBattle(int awakening = 0)
            {
                config = BrinaAshwoodTestConfig.Create();
                Character = new CharacterBattleState(
                    BrinaAshwoodRules.CharacterId,
                    0,
                    ElementType.Grass,
                    1000L,
                    1000d);
                Party = new PartyBattleState(new[] { Character });
                Boss = new BossBattleState(
                    "training_boss",
                    ElementType.Grass,
                    1000000L,
                    0d);
                ActionIds = new CombatActionIdSequence();
                Provider = new BrinaAshwoodActiveActionProvider(
                    config,
                    awakening);
                State = new BrinaAshwoodBattleState(
                    Character,
                    config.WoodcutTriggerThreshold);
                Executor = new CombatActionExecutor(
                    Boss,
                    Party,
                    new DamageContextFactory(
                        new SeededRandomSource(123)));
                TurnEnd = new BattleTurnEndProcessor(Party, Boss);
            }

            public CharacterBattleState Character { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public BrinaAshwoodActiveActionProvider Provider { get; }
            public BrinaAshwoodBattleState State { get; }
            public BattleTurnEndProcessor TurnEnd { get; }
            public CombatActionExecutor Executor { get; }

            public IReadOnlyList<CombatAction> CreateActive()
            {
                return Provider.CreateActions(Character, ActionIds);
            }

            public CombatActionExecutionResult ApplyActive()
            {
                return Executor.Execute(
                    new CombatActionQueue(CreateActive()));
            }

            public DamageActionResult ExecuteSimpleDamage()
            {
                var action = new DamageAction(
                    ActionIds.Next(),
                    ActionOrigin.Match,
                    new DamageContextBuildRequest(
                        Character,
                        Party,
                        Boss,
                        ElementType.Grass,
                        AttackType.Match,
                        AttackTag.Match3,
                        1d,
                        appliesCombo: false,
                        finalComboCount: 0,
                        canCritical: false));
                return (DamageActionResult)Executor.Execute(
                    new CombatActionQueue(
                        new CombatAction[] { action }))
                    .ActionResults[0];
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}
