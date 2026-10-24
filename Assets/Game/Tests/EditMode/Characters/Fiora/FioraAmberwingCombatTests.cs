using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.Modifiers;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Fiora;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Party.Battle;

namespace ValorChronicle.Tests.EditMode.Characters.Fiora
{
    public sealed class FioraAmberwingCombatTests
    {
        private FioraAmberwingCombatConfig config;

        [SetUp]
        public void SetUp()
        {
            config = FioraAmberwingTestConfig.Create();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void ConfigBuildAndAwakeningsResolveCanonicalValues()
        {
            Assert.That(config.TryValidate(out _), Is.True);
            var build = new FioraAmberwingBuildModifier();
            Assert.That(build.GetStatMultipliers(0).MaxHpMultiplier, Is.EqualTo(1d));
            Assert.That(build.GetStatMultipliers(1).MaxHpMultiplier, Is.EqualTo(1.05d));
            Assert.That(5800d * build.GetStatMultipliers(6).MaxHpMultiplier, Is.EqualTo(6090d));

            FioraAmberwingResolvedCombatRules zero =
                FioraAmberwingCombatRuleResolver.Resolve(config, 0);
            FioraAmberwingResolvedCombatRules six =
                FioraAmberwingCombatRuleResolver.Resolve(config, 6);
            Assert.That(zero.Match3HealingCoefficient, Is.EqualTo(0.08d));
            Assert.That(zero.Match4HealingCoefficient, Is.EqualTo(0.14d));
            Assert.That(zero.Match5HealingCoefficient, Is.EqualTo(0.22d));
            Assert.That(zero.PassiveConversionRate, Is.EqualTo(0.50d));
            Assert.That(zero.ActiveInstantHealingCoefficient, Is.EqualTo(0.24d));
            Assert.That(zero.ActionLocalHealingIncreaseRate, Is.Zero);
            Assert.That(six.Match3HealingCoefficient, Is.EqualTo(0.09d));
            Assert.That(six.Match4HealingCoefficient, Is.EqualTo(0.15d));
            Assert.That(six.Match5HealingCoefficient, Is.EqualTo(0.22d));
            Assert.That(six.PassiveConversionRate, Is.EqualTo(0.55d));
            Assert.That(six.ActiveInstantHealingCoefficient, Is.EqualTo(0.26d));
            Assert.That(six.ActiveFollowUpHealingCoefficient, Is.EqualTo(0.07d));
            Assert.That(six.ActionLocalHealingIncreaseRate, Is.EqualTo(0.05d));
            Assert.That(six.HasAwakening6FullOverheal, Is.True);
        }

        [Test]
        public void ConfigRejectsInvalidCoefficientDurationAndCounts()
        {
            SetField("match3HealingCoefficient", double.NaN);
            Assert.That(config.TryValidate(out _), Is.False);
            SetField("match3HealingCoefficient", 0.08d);
            SetField("passiveDurationTurns", 0);
            Assert.That(config.TryValidate(out _), Is.False);
            SetField("passiveDurationTurns", 2);
            SetField("activeFollowUpHealGrantCount", 5);
            Assert.That(config.TryValidate(out _), Is.False);
        }

        [TestCase(BoardMatchTier.Three, 0.08d, 1)]
        [TestCase(BoardMatchTier.Four, 0.14d, 2)]
        [TestCase(BoardMatchTier.FiveOrMore, 0.22d, 2)]
        public void MatchCreatesHealingRootsWithoutDamage(
            BoardMatchTier tier,
            double expectedCoefficient,
            int expectedCount)
        {
            TestBattle battle = CreateBattle(awakening: 0);
            var provider = new FioraAmberwingMatchActionProvider(
                config,
                awakening: 0,
                battle.State);

            var actions = provider.CreateActions(
                battle.Fiora,
                battle.Party,
                tier,
                finalComboCount: 3,
                battle.ActionIds);

            Assert.That(actions.Count, Is.EqualTo(expectedCount));
            HealAction heal = actions[0] as HealAction;
            Assert.That(heal, Is.Not.Null);
            Assert.That(heal.Origin, Is.EqualTo(ActionOrigin.Match));
            Assert.That(heal.RootActionId, Is.EqualTo(heal.ActionId));
            Assert.That(heal.SourceActionId, Is.Null);
            Assert.That(heal.ContextRequest.HealingCoefficient, Is.EqualTo(expectedCoefficient));
            Assert.That(heal.ContextRequest.AppliesCombo, Is.True);
            Assert.That(heal.ContextRequest.FinalComboCount, Is.EqualTo(3));
            Assert.That(actions.OfType<DamageAction>(), Is.Empty);
            for (int index = 0; index < actions.Count; index++)
            {
                Assert.That(actions[index].ActionId, Is.EqualTo(index + 1));
                Assert.That(actions[index].RootActionId,
                    Is.EqualTo(actions[index].ActionId));
                Assert.That(actions[index].SourceActionId, Is.Null);
            }

            if (expectedCount == 2)
            {
                Assert.That(actions[1], Is.TypeOf<ApplyEffectAction>());
                Assert.That(actions[1].Origin, Is.EqualTo(ActionOrigin.Match));
            }
        }

        [Test]
        public void Awakening2MatchHealingUsesComboAndUpdatesPartyHp()
        {
            TestBattle battle = CreateBattle(awakening: 2);
            long damageId = battle.ActionIds.Next();
            battle.Executor.Execute(new CombatActionQueue(new CombatAction[]
            {
                new BossDamageAction(
                    damageId,
                    new BossDamageContextBuildRequest(
                        battle.Boss,
                        battle.Party,
                        attackCoefficient: 1d,
                        AttackTag.None))
            }));
            var provider = new FioraAmberwingMatchActionProvider(config, 2, battle.State);
            var actions = provider.CreateActions(
                battle.Fiora,
                battle.Party,
                BoardMatchTier.Three,
                finalComboCount: 2,
                battle.ActionIds);

            CombatActionExecutionResult result = battle.Executor.Execute(
                new CombatActionQueue(actions));
            HealActionResult heal = result.ActionResults.OfType<HealActionResult>().Single();

            Assert.That(heal.FinalHealing, Is.EqualTo(88L));
            Assert.That(heal.AppliedHealing, Is.EqualTo(88L));
            Assert.That(heal.OverhealAmount, Is.Zero);
            Assert.That(battle.Party.CurrentHp, Is.EqualTo(988L));
        }

        [Test]
        public void FourMatchExecutesHealThenPassiveThenFixedBuffAndRefreshes()
        {
            TestBattle battle = CreateBattle(awakening: 0);
            var provider = new FioraAmberwingMatchActionProvider(config, 0, battle.State);
            var first = provider.CreateActions(
                battle.Fiora, battle.Party, BoardMatchTier.Four, 1, battle.ActionIds);

            CombatActionExecutionResult result = battle.Executor.Execute(
                new CombatActionQueue(first));

            Assert.That(result.ActionResults, Has.Count.EqualTo(3));
            Assert.That(result.ActionResults[0], Is.TypeOf<HealActionResult>());
            Assert.That(result.ActionResults[1], Is.TypeOf<ApplyEffectActionResult>());
            Assert.That(((ApplyEffectAction)result.ActionResults[1].Action).Effect.EffectId,
                Is.EqualTo(FioraAmberwingRules.PassiveBlessingEffectId));
            Assert.That(((ApplyEffectAction)result.ActionResults[2].Action).Effect.EffectId,
                Is.EqualTo(FioraAmberwingRules.Match4AttackEffectId));

            new BattleTurnEndProcessor(battle.Party, battle.Boss).ProcessTurnEnd();
            var second = provider.CreateActions(
                battle.Fiora, battle.Party, BoardMatchTier.Four, 1, battle.ActionIds);
            battle.Executor.Execute(new CombatActionQueue(second));

            EffectInstance[] fixedEffects = battle.Party.Effects.GetActiveEffects()
                .Where(effect => effect.EffectId == FioraAmberwingRules.Match4AttackEffectId)
                .ToArray();
            Assert.That(fixedEffects, Has.Length.EqualTo(1));
            Assert.That(fixedEffects[0].Magnitude, Is.EqualTo(0.05d));
            Assert.That(fixedEffects[0].RemainingTurns, Is.EqualTo(2));
        }

        [Test]
        public void PartialOverhealUsesOnlyFinalExcessAmount()
        {
            TestBattle battle = CreateBattle(awakening: 0);
            long damageId = battle.ActionIds.Next();
            battle.Executor.Execute(new CombatActionQueue(new CombatAction[]
            {
                new BossDamageAction(
                    damageId,
                    new BossDamageContextBuildRequest(
                        battle.Boss,
                        battle.Party,
                        attackCoefficient: 1d,
                        AttackTag.None))
            }));
            var provider = new FioraAmberwingMatchActionProvider(config, 0, battle.State);

            CombatActionExecutionResult result = battle.Executor.Execute(
                new CombatActionQueue(provider.CreateActions(
                    battle.Fiora,
                    battle.Party,
                    BoardMatchTier.Four,
                    finalComboCount: 1,
                    battle.ActionIds)));

            HealActionResult heal = result.ActionResults.OfType<HealActionResult>().Single();
            EffectInstance blessing = battle.Party.Effects.GetActiveEffects()
                .Single(effect => effect.EffectId == FioraAmberwingRules.PassiveBlessingEffectId);
            Assert.That(heal.FinalHealing, Is.EqualTo(140L));
            Assert.That(heal.AppliedHealing, Is.EqualTo(100L));
            Assert.That(heal.OverhealAmount, Is.EqualTo(40L));
            Assert.That(blessing.Magnitude, Is.EqualTo(0.02d).Within(0.000000001d));
        }

        [Test]
        public void PassiveInstancesPreserveRawValuesAndCapOnlyTheirGroup()
        {
            TestBattle battle = CreateBattle(awakening: 0);
            var provider = new FioraAmberwingMatchActionProvider(config, 0, battle.State);
            for (int index = 0; index < 5; index++)
            {
                var actions = provider.CreateActions(
                    battle.Fiora, battle.Party, BoardMatchTier.FiveOrMore, 1, battle.ActionIds);
                battle.Executor.Execute(new CombatActionQueue(actions));
            }

            EffectInstance[] blessings = battle.Party.Effects.GetActiveEffects()
                .Where(effect => effect.EffectId == FioraAmberwingRules.PassiveBlessingEffectId)
                .ToArray();
            Assert.That(blessings, Has.Length.EqualTo(5));
            Assert.That(blessings.Sum(effect => effect.Magnitude), Is.EqualTo(0.25d).Within(0.000000001d));
            AllyDamageModifierSnapshot modifiers = CombatModifierCollector.CollectAllyDamage(
                battle.Fiora, battle.Party, battle.Boss, ElementType.Fire);
            Assert.That(modifiers.AttackIncreaseRateSum, Is.EqualTo(0.29d).Within(0.000000001d));

            var four = provider.CreateActions(
                battle.Fiora, battle.Party, BoardMatchTier.Four, 1, battle.ActionIds);
            battle.Executor.Execute(new CombatActionQueue(four));
            modifiers = CombatModifierCollector.CollectAllyDamage(
                battle.Fiora, battle.Party, battle.Boss, ElementType.Fire);
            Assert.That(modifiers.AttackIncreaseRateSum, Is.EqualTo(0.34d).Within(0.000000001d));
            Assert.That(battle.Party.Effects.GetActiveEffects().Count(effect =>
                effect.EffectId == FioraAmberwingRules.Match4AttackEffectId), Is.EqualTo(1));
            Assert.That(battle.Party.Effects.GetActiveEffects().Count(effect =>
                effect.EffectId == FioraAmberwingRules.Match5AttackEffectId), Is.EqualTo(1));
        }

        [Test]
        public void IndependentBlessingExpiryRecalculatesRemainingMagnitude()
        {
            TestBattle battle = CreateBattle(awakening: 0);
            var provider = new FioraAmberwingMatchActionProvider(config, 0, battle.State);
            battle.Executor.Execute(new CombatActionQueue(provider.CreateActions(
                battle.Fiora, battle.Party, BoardMatchTier.Three, 1, battle.ActionIds)));
            var turns = new BattleTurnEndProcessor(battle.Party, battle.Boss);
            turns.ProcessTurnEnd();
            battle.Executor.Execute(new CombatActionQueue(provider.CreateActions(
                battle.Fiora, battle.Party, BoardMatchTier.Three, 1, battle.ActionIds)));
            turns.ProcessTurnEnd();

            EffectInstance[] blessings = battle.Party.Effects.GetActiveEffects()
                .Where(effect => effect.EffectId == FioraAmberwingRules.PassiveBlessingEffectId)
                .ToArray();
            Assert.That(blessings, Has.Length.EqualTo(1));
            Assert.That(blessings[0].RemainingTurns, Is.EqualTo(1));
        }

        [Test]
        public void Awakening6UsesSeventyPercentOnlyOncePerPlayerTurn()
        {
            TestBattle battle = CreateBattle(awakening: 6, fioraHp: 100, allyHp: 900);
            var provider = new FioraAmberwingMatchActionProvider(config, 6, battle.State);
            var first = provider.CreateActions(
                battle.Fiora, battle.Party, BoardMatchTier.Three, 1, battle.ActionIds);
            CombatActionExecutionSession firstSession =
                battle.Executor.BeginExecution(first, playerTurnNumber: 3);
            while (firstSession.TryExecuteNext(out _)) { }
            HealActionResult firstHealing = firstSession.Result.ActionResults
                .OfType<HealActionResult>()
                .Single();
            Assert.That(firstHealing.FinalHealing, Is.EqualTo(9L));
            Assert.That(firstHealing.AppliedHealing, Is.Zero);
            Assert.That(firstHealing.OverhealAmount, Is.EqualTo(9L));
            Assert.That(battle.Party.MaxHp, Is.EqualTo(1000L));
            Assert.That(battle.State.LastAwakening6ConversionTurn,
                Is.EqualTo(3));

            var second = provider.CreateActions(
                battle.Fiora, battle.Party, BoardMatchTier.Three, 1, battle.ActionIds);
            CombatActionExecutionSession secondSession =
                battle.Executor.BeginExecution(second, playerTurnNumber: 3);
            while (secondSession.TryExecuteNext(out _)) { }
            HealActionResult secondHealing = secondSession.Result.ActionResults
                .OfType<HealActionResult>()
                .Single();
            Assert.That(secondHealing.FinalHealing, Is.EqualTo(9L));
            Assert.That(secondHealing.AppliedHealing, Is.Zero);
            Assert.That(secondHealing.OverhealAmount, Is.EqualTo(9L));
            Assert.That(battle.State.LastAwakening6ConversionTurn,
                Is.EqualTo(3));

            var third = provider.CreateActions(
                battle.Fiora, battle.Party, BoardMatchTier.Three, 1, battle.ActionIds);
            CombatActionExecutionSession thirdSession =
                battle.Executor.BeginExecution(third, playerTurnNumber: 4);
            while (thirdSession.TryExecuteNext(out _)) { }
            HealActionResult thirdHealing = thirdSession.Result.ActionResults
                .OfType<HealActionResult>()
                .Single();
            Assert.That(thirdHealing.FinalHealing, Is.EqualTo(9L));
            Assert.That(thirdHealing.AppliedHealing, Is.Zero);
            Assert.That(thirdHealing.OverhealAmount, Is.EqualTo(9L));
            Assert.That(battle.State.LastAwakening6ConversionTurn,
                Is.EqualTo(4));

            double[] values = battle.Party.Effects.GetActiveEffects()
                .Where(effect => effect.EffectId == FioraAmberwingRules.PassiveBlessingEffectId)
                .Select(effect => effect.Magnitude)
                .ToArray();
            double firstExpected = Math.Min(
                (double)firstHealing.OverhealAmount
                    / battle.Party.MaxHp
                    * config.Awakening6FullOverhealConversionRate,
                config.PassivePerInstanceCap);
            double secondExpected = Math.Min(
                (double)secondHealing.OverhealAmount
                    / battle.Party.MaxHp
                    * config.Awakening3PassiveConversionRate,
                config.PassivePerInstanceCap);
            double thirdExpected = Math.Min(
                (double)thirdHealing.OverhealAmount
                    / battle.Party.MaxHp
                    * config.Awakening6FullOverhealConversionRate,
                config.PassivePerInstanceCap);
            Assert.That(firstExpected,
                Is.EqualTo(0.0063d).Within(0.000000001d));
            Assert.That(secondExpected,
                Is.EqualTo(0.00495d).Within(0.000000001d));
            Assert.That(thirdExpected,
                Is.EqualTo(0.0063d).Within(0.000000001d));
            Assert.That(values, Has.Length.EqualTo(3));
            Assert.That(values[0],
                Is.EqualTo(firstExpected).Within(0.000000001d));
            Assert.That(values[1],
                Is.EqualTo(secondExpected).Within(0.000000001d));
            Assert.That(values[2],
                Is.EqualTo(thirdExpected).Within(0.000000001d));
        }

        [Test]
        public void ActiveGrantsChargesOnlyAfterExecutionAndCapsAtFour()
        {
            TestBattle battle = CreateBattle(awakening: 5);
            var provider = new FioraAmberwingActiveActionProvider(config, 5, battle.State);
            var actions = provider.CreateActions(battle.Fiora, battle.Party, battle.ActionIds);
            HealAction heal = (HealAction)actions[0];
            Assert.That(battle.State.RemainingFollowUpHeals, Is.Zero);
            Assert.That(heal.ContextRequest.HealingCoefficient, Is.EqualTo(0.26d));
            Assert.That(heal.ContextRequest.AppliesCombo, Is.False);
            Assert.That(heal.ContextRequest.ActionLocalHealingIncreaseRate, Is.EqualTo(0.05d));
            Assert.That(provider.CooldownTurns, Is.EqualTo(8));

            battle.Executor.Execute(new CombatActionQueue(actions));
            Assert.That(battle.State.RemainingFollowUpHeals, Is.EqualTo(2));
            Assert.That(battle.State.FirstFollowUpHealAvailableTurn, Is.EqualTo(1));
            battle.Executor.Execute(new CombatActionQueue(provider.CreateActions(
                battle.Fiora, battle.Party, battle.ActionIds)));
            battle.Executor.Execute(new CombatActionQueue(provider.CreateActions(
                battle.Fiora, battle.Party, battle.ActionIds)));
            Assert.That(battle.State.RemainingFollowUpHeals, Is.EqualTo(4));
        }

        [Test]
        public void ActiveHealingIgnoresComboAndUsesFinalCoefficient()
        {
            TestBattle battle = CreateBattle(awakening: 5);
            var provider = new FioraAmberwingActiveActionProvider(config, 5, battle.State);
            CombatActionExecutionResult result = battle.Executor.Execute(
                new CombatActionQueue(provider.CreateActions(
                    battle.Fiora,
                    battle.Party,
                    battle.ActionIds)));

            HealActionResult heal = result.ActionResults.OfType<HealActionResult>().Single();
            Assert.That(heal.FinalHealing, Is.EqualTo(273L));
            Assert.That(heal.HealingResult.ComboMultiplier, Is.EqualTo(1d));
        }

        [Test]
        public void UnregisteredHealingDoesNotTriggerFioraPassive()
        {
            TestBattle battle = CreateBattle(awakening: 0, allyHp: 1000);
            CharacterBattleState ally = battle.Party.Characters[1];
            long id = battle.ActionIds.Next();
            var unrelated = new HealAction(
                id,
                ActionOrigin.System,
                new ValorChronicle.Battle.Combat.Healing.HealingContextBuildRequest(
                    ally,
                    battle.Party,
                    0.08d,
                    appliesCombo: false,
                    finalComboCount: 0));

            CombatActionExecutionResult result = battle.Executor.Execute(
                new CombatActionQueue(new CombatAction[] { unrelated }));

            Assert.That(result.ActionResults, Has.Count.EqualTo(1));
            Assert.That(battle.Party.Effects.GetActiveEffects(), Is.Empty);
        }

        [Test]
        public void RegistrarWiresProvidersTriggerAndSharedStatusState()
        {
            CharacterDefinition definition = ScriptableObject.CreateInstance<CharacterDefinition>();
            try
            {
                var serialized = new SerializedObject(definition);
                serialized.FindProperty("id").stringValue = FioraAmberwingRules.CharacterId;
                serialized.FindProperty("element").enumValueIndex = (int)ElementType.Fire;
                serialized.FindProperty("level1Hp").intValue = 1450;
                serialized.FindProperty("level1Attack").intValue = 80;
                serialized.FindProperty("level100Hp").intValue = 5800;
                serialized.FindProperty("level100Attack").intValue = 400;
                serialized.FindProperty("combatConfig").objectReferenceValue = config;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var member = new BattlePartyMemberInput(
                    FioraAmberwingRules.CharacterId,
                    partySlotIndex: 0,
                    level: 100,
                    awakening: 6,
                    definition);
                TestBattle battle = CreateBattle(awakening: 6);
                var matches = new MatchEventActionProviderRegistry();
                var actives = new ActiveAbilityActionProviderRegistry();

                CharacterCombatProviderRegistrationResult registration =
                    new FioraAmberwingCombatProviderRegistrar().Register(
                        member,
                        battle.Fiora,
                        battle.Boss,
                        battle.ActionIds,
                        matches,
                        actives);

                Assert.That(matches.TryResolve(FioraAmberwingRules.CharacterId, out _), Is.True);
                Assert.That(actives.TryResolve(
                    FioraAmberwingRules.CharacterId,
                    FioraAmberwingRules.ActiveAbilityId,
                    out _), Is.True);
                Assert.That(registration.ActiveCooldownTurns, Is.EqualTo(8));
                Assert.That(registration.TriggerRules.Single(), Is.TypeOf<FioraAmberwingOverhealBlessingRule>());
                Assert.That(registration.CombatStatusSources.Single(), Is.TypeOf<FioraAmberwingBattleState>());
                Assert.That(registration.ResourceRequirements, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void BattleStateEnforcesNextTurnAndOneConsumptionPerTurn()
        {
            TestBattle battle = CreateBattle(awakening: 0);
            battle.State.GrantFollowUpHeals(2, currentTurn: 4);
            Assert.That(battle.State.TryConsumeFollowUpHeal(4), Is.False);
            Assert.That(battle.State.TryConsumeFollowUpHeal(5), Is.True);
            Assert.That(battle.State.TryConsumeFollowUpHeal(5), Is.False);
            Assert.That(battle.State.TryConsumeFollowUpHeal(6), Is.True);
            Assert.That(battle.State.RemainingFollowUpHeals, Is.Zero);
        }

        private TestBattle CreateBattle(int awakening, long fioraHp = 1000, long allyHp = 0)
        {
            var fiora = new CharacterBattleState(
                FioraAmberwingRules.CharacterId, 0, ElementType.Fire, fioraHp, 400d);
            CharacterBattleState[] characters = allyHp > 0
                ? new[]
                {
                    fiora,
                    new CharacterBattleState("ally", 1, ElementType.Water, allyHp, 100d)
                }
                : new[] { fiora };
            var party = new PartyBattleState(characters);
            var boss = new BossBattleState("boss", ElementType.Water, 1000000, 100d);
            var ids = new CombatActionIdSequence();
            FioraAmberwingResolvedCombatRules rules =
                FioraAmberwingCombatRuleResolver.Resolve(config, awakening);
            var state = new FioraAmberwingBattleState(fiora, rules.ActiveFollowUpHealMaxCount);
            var trigger = new FioraAmberwingOverhealBlessingRule(state, config, awakening, ids);
            var executor = new CombatActionExecutor(
                boss,
                party,
                new DamageContextFactory(new SeededRandomSource(1)),
                new CombatTriggerResolver(new ICombatTriggerRule[] { trigger }));
            return new TestBattle(fiora, party, boss, ids, state, executor);
        }

        private void SetField(string name, object value)
        {
            FieldInfo field = typeof(FioraAmberwingCombatConfig).GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(config, value);
        }

        private sealed class TestBattle
        {
            public TestBattle(
                CharacterBattleState fiora,
                PartyBattleState party,
                BossBattleState boss,
                CombatActionIdSequence actionIds,
                FioraAmberwingBattleState state,
                CombatActionExecutor executor)
            {
                Fiora = fiora;
                Party = party;
                Boss = boss;
                ActionIds = actionIds;
                State = state;
                Executor = executor;
            }

            public CharacterBattleState Fiora { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public FioraAmberwingBattleState State { get; }
            public CombatActionExecutor Executor { get; }
        }
    }
}
