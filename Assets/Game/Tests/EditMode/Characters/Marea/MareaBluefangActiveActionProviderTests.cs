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
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Marea
{
    public sealed class MareaBluefangActiveActionProviderTests
    {
        [Test]
        public void CreateActions_ReturnsOneRootActiveApplyEffectAction()
        {
            ActiveBattle battle = CreateBattle();

            IReadOnlyList<CombatAction> actions = battle.CreateActive();

            Assert.That(actions.Count, Is.EqualTo(1));
            Assert.That(actions.OfType<DamageAction>(), Is.Empty);
            Assert.That(actions.OfType<AddResourceAction>(), Is.Empty);
            Assert.That(actions.OfType<ConsumeResourceAction>(), Is.Empty);
            ApplyEffectAction action = actions[0] as ApplyEffectAction;
            Assert.That(action, Is.Not.Null);
            Assert.That(action.Origin, Is.EqualTo(ActionOrigin.Active));
            Assert.That(action.RootActionId, Is.EqualTo(action.ActionId));
            Assert.That(action.SourceActionId, Is.Null);
            Assert.That(action.TargetType,
                Is.EqualTo(CombatEffectTargetType.Character));
            Assert.That(action.TargetCharacter,
                Is.SameAs(battle.Character));
            AssertEffectDefinition(action.Effect);
            Assert.That(action.Effect.RuntimeId,
                Is.EqualTo(action.ActionId));
            Assert.That(action.Effect.CreationOrder,
                Is.EqualTo(action.ActionId));
        }

        [Test]
        public void Executor_AppliesChargeOrderToMarea()
        {
            ActiveBattle battle = CreateBattle();
            IReadOnlyList<CombatAction> actions = battle.CreateActive();

            Assert.That(battle.Character.Effects.Count, Is.Zero);
            CombatActionExecutionResult result = battle.Execute(actions);

            Assert.That(result.CompletedActionCount, Is.EqualTo(1));
            ApplyEffectActionResult applied = result.ActionResults[0]
                as ApplyEffectActionResult;
            Assert.That(applied, Is.Not.Null);
            Assert.That(applied.AppliedEffect,
                Is.SameAs(((ApplyEffectAction)actions[0]).Effect));
            Assert.That(battle.Character.Effects.Count, Is.EqualTo(1));
            AssertEffectDefinition(applied.AppliedEffect);
        }

        [Test]
        public void AppliedEffect_IncreasesOnlyWaterDamage()
        {
            ActiveBattle battle = CreateBattle();
            DamageActionResult before = battle.ExecuteDamage(
                ElementType.Water);

            battle.ApplyActive();
            DamageActionResult water = battle.ExecuteDamage(
                ElementType.Water);
            DamageActionResult fire = battle.ExecuteDamage(
                ElementType.Fire);

            Assert.That(before.DamageResult.FinalDamage, Is.EqualTo(1000L));
            Assert.That(water.Context.ElementDamageIncreaseRateSum,
                Is.EqualTo(0.25d));
            Assert.That(water.DamageResult.FinalDamage, Is.EqualTo(1250L));
            Assert.That(fire.Context.ElementDamageIncreaseRateSum, Is.Zero);
            Assert.That(fire.DamageResult.FinalDamage, Is.EqualTo(1000L));
        }

        [Test]
        public void Reapply_RefreshesDurationWithoutStackingMagnitude()
        {
            ActiveBattle battle = CreateBattle();
            battle.ApplyActive();
            EffectInstance original = battle.Character.Effects
                .GetActiveEffects()[0];
            battle.TurnEnd.ProcessTurnEnd();
            battle.TurnEnd.ProcessTurnEnd();
            Assert.That(original.RemainingTurns, Is.EqualTo(1));

            CombatActionExecutionResult result = battle.ApplyActive();
            ApplyEffectActionResult reapplied =
                (ApplyEffectActionResult)result.ActionResults[0];

            Assert.That(battle.Character.Effects.Count, Is.EqualTo(1));
            Assert.That(reapplied.AppliedEffect, Is.SameAs(original));
            Assert.That(original.Magnitude, Is.EqualTo(0.25d));
            Assert.That(original.RemainingTurns, Is.EqualTo(3));
        }

        [Test]
        public void OtherWaterBuff_AddsInSameModifierCategory()
        {
            ActiveBattle battle = CreateBattle();
            battle.ApplyActive();
            battle.ApplyOtherWaterBuff(0.10d);

            DamageActionResult damage = battle.ExecuteDamage(
                ElementType.Water);

            Assert.That(damage.Context.ElementDamageIncreaseRateSum,
                Is.EqualTo(0.35d).Within(0.000000001d));
            Assert.That(damage.DamageResult.ElementDamageMultiplier,
                Is.EqualTo(1.35d).Within(0.000000001d));
            Assert.That(damage.DamageResult.FinalDamage, Is.EqualTo(1350L));
        }

        [Test]
        public void ThreeMatchCombinesActiveAndPassiveThenAddsWater()
        {
            ActiveBattle battle = CreateBattle();
            ResourceState water = WaterElementResource.Register(
                battle.Boss.Resources);
            water.Add(1);
            battle.ApplyActive();
            var matchProvider = new MareaBluefangMatchActionProvider();
            IReadOnlyList<CombatAction> actions = matchProvider.CreateActions(
                battle.Character,
                battle.Party,
                battle.Boss,
                BoardMatchTier.Three,
                1,
                battle.ActionIds);

            CombatActionExecutionResult result = battle.Execute(actions);
            DamageActionResult damage =
                (DamageActionResult)result.ActionResults[0];

            Assert.That(damage.Context.ElementDamageIncreaseRateSum,
                Is.EqualTo(0.25d));
            Assert.That(damage.Context.DealtDamageIncreaseRateSum,
                Is.EqualTo(0.15d));
            Assert.That(damage.DamageResult.ElementDamageMultiplier,
                Is.EqualTo(1.25d));
            Assert.That(damage.DamageResult.DealtDamageMultiplier,
                Is.EqualTo(1.15d));
            Assert.That(damage.DamageResult.RawDamage,
                Is.EqualTo(1293.75d).Within(0.000000001d));
            Assert.That(damage.DamageResult.FinalDamage, Is.EqualTo(1293L));
            Assert.That(result.ActionResults[1],
                Is.TypeOf<AddResourceActionResult>());
            Assert.That(water.CurrentAmount, Is.EqualTo(2));
        }

        [Test]
        public void FiveMatchAppliesActiveOnceThenConsumesFiveWater()
        {
            ActiveBattle battle = CreateBattle();
            ResourceState water = WaterElementResource.Register(
                battle.Boss.Resources);
            water.Add(5);
            battle.ApplyActive();
            var matchProvider = new MareaBluefangMatchActionProvider();
            IReadOnlyList<CombatAction> actions = matchProvider.CreateActions(
                battle.Character,
                battle.Party,
                battle.Boss,
                BoardMatchTier.FiveOrMore,
                1,
                battle.ActionIds);

            CombatActionExecutionResult result = battle.Execute(actions);
            DamageActionResult[] damageResults = result.ActionResults
                .OfType<DamageActionResult>()
                .ToArray();
            ConsumeResourceActionResult[] consumptionResults =
                result.ActionResults
                    .OfType<ConsumeResourceActionResult>()
                    .ToArray();

            Assert.That(damageResults.Length, Is.EqualTo(1));
            Assert.That(damageResults[0].Context.SkillCoefficient,
                Is.EqualTo(9.40d));
            Assert.That(damageResults[0].DamageResult.RawDamage,
                Is.EqualTo(13512.5d).Within(0.000000001d));
            Assert.That(damageResults[0].DamageResult.FinalDamage,
                Is.EqualTo(13512L));
            Assert.That(consumptionResults.Length, Is.EqualTo(1));
            Assert.That(consumptionResults[0].ConsumeResult.ConsumedAmount,
                Is.EqualTo(5));
            Assert.That(water.CurrentAmount, Is.Zero);
        }

        [Test]
        public void Duration_AppliesForThreeTurnsThenExpires()
        {
            ActiveBattle battle = CreateBattle();
            battle.ApplyActive();

            for (int turn = 1; turn <= 3; turn++)
            {
                DamageActionResult damage = battle.ExecuteDamage(
                    ElementType.Water);
                Assert.That(damage.DamageResult.FinalDamage,
                    Is.EqualTo(1250L), $"Turn {turn}");
                battle.TurnEnd.ProcessTurnEnd();
                Assert.That(battle.Character.Effects.Count,
                    Is.EqualTo(turn < 3 ? 1 : 0),
                    $"Turn {turn} end");
            }

            Assert.That(battle.ExecuteDamage(ElementType.Water)
                    .DamageResult.FinalDamage,
                Is.EqualTo(1000L));
        }

        [Test]
        public void Cooldown_UsesExistingEightTurnRuntimeContract()
        {
            var coordinator = new BattleFlowCoordinator(
                25,
                new[] { MareaBluefangRules.ActiveCooldownTurns });
            coordinator.StartBattle();
            ActiveAbilityRuntimeState state =
                coordinator.Context.ActiveAbilities[0];

            Assert.That(state.RemainingCooldown, Is.Zero);
            Assert.That(state.CanUse, Is.True);
            Assert.That(coordinator.TryUseActiveAbility(0), Is.True);
            Assert.That(state.RemainingCooldown, Is.EqualTo(8));
            Assert.That(state.UsedThisTurn, Is.True);
            Assert.That(coordinator.TryUseActiveAbility(0), Is.False);

            CompleteTurnWithoutMatchEvents(coordinator);

            Assert.That(state.RemainingCooldown, Is.EqualTo(7));
            Assert.That(state.UsedThisTurn, Is.False);
            Assert.That(state.CanUse, Is.False);
        }

        [Test]
        public void CreateActions_RejectsNonMareaAndNullSequence()
        {
            var provider = new MareaBluefangActiveActionProvider();
            var other = new CharacterBattleState(
                "other",
                0,
                ElementType.Water,
                1000,
                1000d);

            Assert.Throws<ArgumentException>(() => provider.CreateActions(
                other,
                new CombatActionIdSequence()));
            Assert.Throws<ArgumentNullException>(() =>
                provider.CreateActions(CreateBattle().Character, null));
        }

        private static ActiveBattle CreateBattle()
        {
            var character = new CharacterBattleState(
                MareaBluefangRules.CharacterId,
                0,
                ElementType.Water,
                1000,
                1000d);
            var party = new PartyBattleState(new[] { character });
            var boss = new BossBattleState(
                "training_boss",
                ElementType.Light,
                1000000,
                100d);
            return new ActiveBattle(character, party, boss);
        }

        private static void CompleteTurnWithoutMatchEvents(
            BattleFlowCoordinator coordinator)
        {
            Assert.That(coordinator.CompleteActiveInput(), Is.True);
            Assert.That(coordinator.NotifyBoardActionStarted(), Is.True);
            Assert.That(
                coordinator.NotifyBoardActionResolved(null, true),
                Is.True);
            coordinator.ExecuteRemainingMatchEvents();
            Assert.That(coordinator.CompleteBossAction(), Is.True);
        }

        private static void AssertEffectDefinition(EffectInstance effect)
        {
            Assert.That(effect.EffectId,
                Is.EqualTo(MareaBluefangRules.ActiveEffectId));
            Assert.That(effect.SourceId,
                Is.EqualTo(MareaBluefangRules.CharacterId));
            Assert.That(effect.Category, Is.EqualTo(EffectCategory.Buff));
            Assert.That(effect.ModifierType,
                Is.EqualTo(EffectModifierType.ElementDamageIncrease));
            Assert.That(effect.ElementFilter,
                Is.EqualTo(ElementType.Water));
            Assert.That(effect.Magnitude, Is.EqualTo(0.25d));
            Assert.That(effect.RemainingTurns, Is.EqualTo(3));
            Assert.That(effect.StackPolicy,
                Is.EqualTo(EffectStackPolicy.RefreshDuration));
            Assert.That(effect.StackCount, Is.EqualTo(1));
            Assert.That(effect.MaxStackCount, Is.EqualTo(1));
        }

        private sealed class ActiveBattle
        {
            private readonly MareaBluefangActiveActionProvider provider =
                new MareaBluefangActiveActionProvider();
            private readonly CombatActionExecutor executor;

            public ActiveBattle(
                CharacterBattleState character,
                PartyBattleState party,
                BossBattleState boss)
            {
                Character = character;
                Party = party;
                Boss = boss;
                ActionIds = new CombatActionIdSequence();
                executor = new CombatActionExecutor(
                    boss,
                    party,
                    new DamageContextFactory(new SeededRandomSource(123)));
                TurnEnd = new BattleTurnEndProcessor(party, boss);
            }

            public CharacterBattleState Character { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public BattleTurnEndProcessor TurnEnd { get; }

            public IReadOnlyList<CombatAction> CreateActive()
            {
                return provider.CreateActions(Character, ActionIds);
            }

            public CombatActionExecutionResult ApplyActive()
            {
                return Execute(CreateActive());
            }

            public void ApplyOtherWaterBuff(double magnitude)
            {
                long actionId = ActionIds.Next();
                var effect = new EffectInstance(
                    actionId,
                    "effect_other_water_damage",
                    "other_source",
                    EffectCategory.Buff,
                    EffectModifierType.ElementDamageIncrease,
                    magnitude,
                    3,
                    actionId,
                    EffectStackPolicy.RefreshDuration,
                    elementFilter: ElementType.Water);
                Execute(new CombatAction[]
                {
                    new ApplyEffectAction(
                        actionId,
                        ActionOrigin.System,
                        Character,
                        effect)
                });
            }

            public DamageActionResult ExecuteDamage(ElementType element)
            {
                long actionId = ActionIds.Next();
                var damage = new DamageAction(
                    actionId,
                    ActionOrigin.Match,
                    new DamageContextBuildRequest(
                        Character,
                        Party,
                        Boss,
                        element,
                        AttackType.Match,
                        AttackTag.None,
                        1d,
                        false,
                        1,
                        false));
                return (DamageActionResult)Execute(
                    new CombatAction[] { damage }).ActionResults[0];
            }

            public CombatActionExecutionResult Execute(
                IReadOnlyList<CombatAction> actions)
            {
                return executor.Execute(new CombatActionQueue(actions));
            }
        }
    }
}
