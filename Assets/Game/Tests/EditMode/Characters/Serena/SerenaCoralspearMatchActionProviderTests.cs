using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Battle.Results;
using ValorChronicle.Characters.Serena;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Serena
{
    public sealed class SerenaCoralspearMatchActionProviderTests
    {
        [TestCase(0, 0.75d, 750L)]
        [TestCase(4, 0.85d, 850L)]
        public void ThreeMatchUsesAwakeningCoefficientThenAddsWater(
            int awakening,
            double expectedCoefficient,
            long expectedDamage)
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Three,
                awakening))
            {
                DamageAction match = battle.MatchDamage;

                AssertMatch(
                    match,
                    expectedCoefficient,
                    AttackTag.Match3,
                    finalComboCount: 1);
                Assert.That(battle.Actions.Count, Is.EqualTo(2));
                AssertLineage<AddResourceAction>(
                    battle.Actions[1],
                    match);
                Assert.That(battle.Water.CurrentAmount, Is.Zero);

                CombatActionExecutionResult result = battle.Execute();

                Assert.That(result.ActionResults[0],
                    Is.TypeOf<DamageActionResult>());
                Assert.That(result.ActionResults[1],
                    Is.TypeOf<AddResourceActionResult>());
                Assert.That(DamageResults(result).Single().DamageResult.FinalDamage,
                    Is.EqualTo(expectedDamage));
                Assert.That(battle.Water.CurrentAmount, Is.EqualTo(1));
            }
        }

        [Test]
        public void ThreeMatchClampsWaterAtSharedMaximum()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Three,
                initialWater: 5))
            {
                CombatActionExecutionResult result = battle.Execute();
                AddResourceActionResult add = result.ActionResults
                    .OfType<AddResourceActionResult>()
                    .Single();

                Assert.That(add.AddResult.AddedAmount, Is.Zero);
                Assert.That(battle.Water.CurrentAmount, Is.EqualTo(5));
            }
        }

        [Test]
        public void ThreeMatchAppliesComboAndContributesAppliedDamageScore()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Three,
                finalComboCount: 3))
            {
                CombatActionExecutionResult result = battle.Execute();
                DamageActionResult damage = DamageResults(result).Single();
                var collector = new BattleDamageScoreCollector(
                    new BattleDamageScoreAccumulator());

                Assert.That(damage.Context.FinalComboCount, Is.EqualTo(3));
                Assert.That(damage.DamageResult.ComboMultiplier,
                    Is.EqualTo(1.12d));
                Assert.That(damage.DamageResult.FinalDamage, Is.EqualTo(840L));
                Assert.That(collector.Collect(result), Is.EqualTo(840L));
            }
        }

        [Test]
        public void FourMatchWithoutWaterCreatesOnlyMatchDamage()
        {
            using (var battle = new TestBattle(BoardMatchTier.Four))
            {
                Assert.That(battle.Actions.Count, Is.EqualTo(1));
                AssertMatch(
                    battle.MatchDamage,
                    1.35d,
                    AttackTag.Match4,
                    finalComboCount: 1);

                CombatActionExecutionResult result = battle.Execute();

                Assert.That(DamageResults(result), Has.Length.EqualTo(1));
                Assert.That(battle.Water.CurrentAmount, Is.Zero);
            }
        }

        [TestCase(0, 1.35d, 0.90d)]
        [TestCase(4, 1.50d, 1.00d)]
        public void FourMatchWithWaterCreatesSeparateAdditionalDamage(
            int awakening,
            double expectedMatchCoefficient,
            double expectedAdditionalCoefficient)
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Four,
                awakening,
                initialWater: 1))
            {
                Assert.That(battle.Actions.Count, Is.EqualTo(2));
                DamageAction match = battle.MatchDamage;
                DamageAction additional = battle.Actions[1] as DamageAction;

                AssertMatch(
                    match,
                    expectedMatchCoefficient,
                    AttackTag.Match4,
                    finalComboCount: 1);
                Assert.That(additional, Is.Not.Null);
                Assert.That(additional.Origin,
                    Is.EqualTo(ActionOrigin.Additional));
                Assert.That(additional.ContextRequest.AttackType,
                    Is.EqualTo(AttackType.Additional));
                Assert.That(additional.ContextRequest.SkillCoefficient,
                    Is.EqualTo(expectedAdditionalCoefficient));
                Assert.That(additional.ContextRequest.AppliesCombo, Is.True);
                Assert.That(additional.ContextRequest.AttackTags,
                    Is.EqualTo(AttackTag.Match4));
                AssertLineage<DamageAction>(additional, match);

                battle.Execute();

                Assert.That(battle.Water.CurrentAmount, Is.EqualTo(1));
            }
        }

        [Test]
        public void Awakening2IncreasesOnlyAdditionalDamageByTenPercent()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Four,
                awakening: 2,
                initialWater: 1))
            {
                DamageAction match = battle.MatchDamage;
                var additional = (DamageAction)battle.Actions[1];

                Assert.That(match.ContextRequest
                        .ActionLocalAttackTypeDamageIncreaseRate,
                    Is.Zero);
                Assert.That(additional.ContextRequest
                        .ActionLocalAttackTypeDamageIncreaseRate,
                    Is.EqualTo(0.10d));
                Assert.That(additional.ContextRequest
                        .ActionLocalDealtDamageIncreaseRate,
                    Is.Zero);
                Assert.That(additional.ContextRequest.SkillCoefficient,
                    Is.EqualTo(0.90d));

                CombatActionExecutionResult result = battle.Execute();
                DamageActionResult[] damage = DamageResults(result);
                var collector = new BattleDamageScoreCollector(
                    new BattleDamageScoreAccumulator());

                Assert.That(damage[0].Context
                        .AttackTypeDamageIncreaseRateSum,
                    Is.Zero);
                Assert.That(damage[0].DamageResult.FinalDamage, Is.EqualTo(1350L));
                Assert.That(damage[1].Context
                        .AttackTypeDamageIncreaseRateSum,
                    Is.EqualTo(0.10d));
                Assert.That(damage[1].DamageResult.FinalDamage, Is.EqualTo(990L));
                Assert.That(collector.Collect(result), Is.EqualTo(2340L));
            }
        }

        [Test]
        public void FourMatchConditionUsesCreationTimeWaterSnapshot()
        {
            using (var withWater = new TestBattle(
                BoardMatchTier.Four,
                initialWater: 1))
            using (var withoutWater = new TestBattle(
                BoardMatchTier.Four))
            {
                withWater.Water.ConsumeAll();
                withoutWater.Water.Add(1);

                Assert.That(withWater.Actions.Count, Is.EqualTo(2));
                Assert.That(withoutWater.Actions.Count, Is.EqualTo(1));
                Assert.That(DamageResults(withWater.Execute()),
                    Has.Length.EqualTo(2));
                Assert.That(DamageResults(withoutWater.Execute()),
                    Has.Length.EqualTo(1));
            }
        }

        [Test]
        public void MatchFactoryAcceptsFourMatchAdditionalLineage()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Four,
                initialWater: 1,
                createActions: false))
            {
                var factory = new MatchEventCombatActionFactory(
                    battle.Party,
                    battle.Boss,
                    battle.Provider,
                    battle.ActionIds);

                IReadOnlyList<CombatAction> actions =
                    factory.CreateCharacterRootActions(
                        CreateExecution(BoardMatchTier.Four, 2),
                        battle.Character);

                Assert.That(actions.Count, Is.EqualTo(2));
                Assert.That(actions[0].ActionId, Is.EqualTo(1));
                Assert.That(actions[1].ActionId, Is.EqualTo(2));
                AssertLineage<DamageAction>(
                    actions[1],
                    (DamageAction)actions[0]);
                Assert.That(((DamageAction)actions[1]).ContextRequest
                        .FinalComboCount,
                    Is.EqualTo(2));
            }
        }

        [Test]
        public void FiveMatchGrantsChargeOnlyAfterDamageExecution()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.FiveOrMore))
            {
                AssertMatch(
                    battle.MatchDamage,
                    2.20d,
                    AttackTag.Match5Plus,
                    finalComboCount: 1);
                Assert.That(battle.State.EmpoweredChaseCharges, Is.Zero);
                Assert.That(battle.Actions.Count, Is.EqualTo(1));

                CombatActionExecutionResult result = battle.Execute();

                Assert.That(result.CompletedActionCount, Is.EqualTo(1));
                Assert.That(battle.State.EmpoweredChaseCharges,
                    Is.EqualTo(1));
                Assert.That(battle.Water.CurrentAmount, Is.Zero);
            }
        }

        [Test]
        public void RepeatedFiveMatchesClampChargeAtThree()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.FiveOrMore))
            {
                for (int index = 0; index < 5; index++)
                {
                    battle.RecreateActions(BoardMatchTier.FiveOrMore);
                    battle.Execute();
                }

                Assert.That(battle.State.EmpoweredChaseCharges,
                    Is.EqualTo(3));
            }
        }

        [Test]
        public void Awakening6AddsWaterAfterFiveMatchWithoutDuplicateCharge()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.FiveOrMore,
                awakening: 6))
            {
                Assert.That(battle.Actions.Count, Is.EqualTo(2));
                AssertLineage<AddResourceAction>(
                    battle.Actions[1],
                    battle.MatchDamage);

                CombatActionExecutionResult result = battle.Execute();

                Assert.That(result.ActionResults[0],
                    Is.TypeOf<DamageActionResult>());
                Assert.That(result.ActionResults[1],
                    Is.TypeOf<AddResourceActionResult>());
                Assert.That(battle.State.EmpoweredChaseCharges,
                    Is.EqualTo(1));
                Assert.That(battle.Water.CurrentAmount, Is.EqualTo(1));
            }
        }

        [Test]
        public void LethalFiveMatchSkipsChargeAndAwakening6Water()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.FiveOrMore,
                awakening: 6,
                bossHp: 1000L))
            {
                CombatActionExecutionResult result = battle.Execute();

                Assert.That(result.CompletedActionCount, Is.EqualTo(1));
                Assert.That(result.BossDefeated, Is.True);
                Assert.That(result.ClearedRemainingActions, Is.True);
                Assert.That(battle.State.EmpoweredChaseCharges, Is.Zero);
                Assert.That(battle.Water.CurrentAmount, Is.Zero);
            }
        }

        [Test]
        public void FiveMatchChargeIsIsolatedBetweenBattleInstances()
        {
            using (var first = new TestBattle(
                BoardMatchTier.FiveOrMore))
            using (var second = new TestBattle(
                BoardMatchTier.FiveOrMore))
            {
                first.Execute();

                Assert.That(first.State.EmpoweredChaseCharges,
                    Is.EqualTo(1));
                Assert.That(second.State.EmpoweredChaseCharges,
                    Is.Zero);
            }
        }

        [Test]
        public void StoredEmpoweredChaseDoesNotAffectAdditionalDamage()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Four,
                initialWater: 1))
            {
                battle.State.TryGrantEmpoweredChase();

                DamageActionResult[] damage = DamageResults(
                    battle.Execute());

                Assert.That(damage[1].Context
                        .AttackTypeDamageIncreaseRateSum,
                    Is.Zero);
                Assert.That(damage[1].Context.DealtDamageIncreaseRateSum,
                    Is.Zero);
                Assert.That(damage[1].DamageResult.FinalDamage, Is.EqualTo(900L));
                Assert.That(battle.State.EmpoweredChaseCharges,
                    Is.EqualTo(1));
            }
        }

        [Test]
        public void LethalMatchClearsAdditionalBeforeExecution()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Four,
                initialWater: 1,
                bossHp: 1000L))
            {
                CombatActionExecutionResult result = battle.Execute();

                Assert.That(result.CompletedActionCount, Is.EqualTo(1));
                Assert.That(result.BossDefeated, Is.True);
                Assert.That(result.ClearedRemainingActions, Is.True);
                Assert.That(DamageResults(result), Has.Length.EqualTo(1));
            }
        }

        [Test]
        public void LethalAdditionalClearsLaterAction()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Four,
                initialWater: 1,
                bossHp: 2000L))
            {
                battle.Actions = battle.Actions.Concat(
                    new CombatAction[]
                    {
                        new AddResourceAction(
                            100,
                            ActionOrigin.System,
                            battle.Boss,
                            WaterElementResource.Id,
                            1)
                    }).ToArray();

                CombatActionExecutionResult result = battle.Execute();

                Assert.That(result.CompletedActionCount, Is.EqualTo(2));
                Assert.That(result.BossDefeated, Is.True);
                Assert.That(result.ClearedRemainingActions, Is.True);
                Assert.That(battle.Water.CurrentAmount, Is.EqualTo(1));
            }
        }

        [Test]
        public void FiveMatchNotExecutedAfterEarlierLethalActionGrantsNothing()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.FiveOrMore,
                bossHp: 1000L))
            {
                DamageAction lethal = CreateSystemDamage(
                    100,
                    battle,
                    coefficient: 2d);
                battle.Actions = new CombatAction[] { lethal }
                    .Concat(battle.Actions)
                    .ToArray();

                CombatActionExecutionResult result = battle.Execute();

                Assert.That(result.CompletedActionCount, Is.EqualTo(1));
                Assert.That(battle.State.EmpoweredChaseCharges, Is.Zero);
            }
        }

        [Test]
        public void TriggerDerivedActionRunsBeforeProviderAdditional()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Four,
                initialWater: 1))
            {
                var rule = new AddWaterAfterMatchRule(
                    battle.Boss,
                    battle.MatchDamage.ActionId);

                CombatActionExecutionResult result = battle.Execute(rule);

                Assert.That(result.ActionResults[0],
                    Is.TypeOf<DamageActionResult>());
                Assert.That(result.ActionResults[1],
                    Is.TypeOf<AddResourceActionResult>());
                Assert.That(result.ActionResults[2],
                    Is.TypeOf<DamageActionResult>());
            }
        }

        private static void AssertMatch(
            DamageAction action,
            double coefficient,
            AttackTag tag,
            int finalComboCount)
        {
            Assert.That(action.Origin, Is.EqualTo(ActionOrigin.Match));
            Assert.That(action.RootActionId, Is.EqualTo(action.ActionId));
            Assert.That(action.SourceActionId, Is.Null);
            Assert.That(action.ContextRequest.AttackElement,
                Is.EqualTo(ElementType.Water));
            Assert.That(action.ContextRequest.AttackType,
                Is.EqualTo(AttackType.Match));
            Assert.That(action.ContextRequest.AttackTags, Is.EqualTo(tag));
            Assert.That(action.ContextRequest.SkillCoefficient,
                Is.EqualTo(coefficient));
            Assert.That(action.ContextRequest.AppliesCombo, Is.True);
            Assert.That(action.ContextRequest.FinalComboCount,
                Is.EqualTo(finalComboCount));
            Assert.That(action.ContextRequest.CanCritical, Is.True);
        }

        private static void AssertLineage<TAction>(
            CombatAction action,
            DamageAction match)
            where TAction : CombatAction
        {
            Assert.That(action, Is.TypeOf<TAction>());
            Assert.That(action.RootActionId, Is.EqualTo(match.ActionId));
            Assert.That(action.SourceActionId,
                Is.EqualTo(match.ActionId));
        }

        private static DamageActionResult[] DamageResults(
            CombatActionExecutionResult result)
        {
            return result.ActionResults
                .OfType<DamageActionResult>()
                .ToArray();
        }

        private static DamageAction CreateSystemDamage(
            long actionId,
            TestBattle battle,
            double coefficient)
        {
            return new DamageAction(
                actionId,
                ActionOrigin.System,
                new DamageContextBuildRequest(
                    battle.Character,
                    battle.Party,
                    battle.Boss,
                    ElementType.Water,
                    AttackType.Match,
                    AttackTag.None,
                    coefficient,
                    appliesCombo: false,
                    finalComboCount: 0,
                    canCritical: false));
        }

        private static MatchEventExecution CreateExecution(
            BoardMatchTier tier,
            int finalComboCount)
        {
            int count = tier == BoardMatchTier.Three
                ? 3
                : tier == BoardMatchTier.Four ? 4 : 5;
            var positions = new List<BoardPosition>(count);
            for (int index = 0; index < count; index++)
            {
                positions.Add(new BoardPosition(index, 0));
            }

            MatchEvent matchEvent = CreateInternal<MatchEvent>(
                0,
                0,
                0,
                ElementType.Water,
                tier,
                positions[0],
                positions,
                positions.Count);
            return CreateInternal<MatchEventExecution>(
                1L,
                matchEvent,
                finalComboCount);
        }

        private static T CreateInternal<T>(params object[] arguments)
        {
            return (T)Activator.CreateInstance(
                typeof(T),
                BindingFlags.Instance
                    | BindingFlags.Public
                    | BindingFlags.NonPublic,
                null,
                arguments,
                null);
        }

        private sealed class TestBattle : IDisposable
        {
            private readonly SerenaCoralspearCombatConfig config;
            private readonly SerenaCoralspearMatch5CompletionRule
                completionRule;

            public TestBattle(
                BoardMatchTier tier,
                int awakening = 0,
                int initialWater = 0,
                int finalComboCount = 1,
                long bossHp = 1000000L,
                bool createActions = true)
            {
                config = SerenaCoralspearTestConfig.Create();
                Character = new CharacterBattleState(
                    SerenaCoralspearRules.CharacterId,
                    0,
                    ElementType.Water,
                    1000L,
                    1000d);
                Party = new PartyBattleState(new[] { Character });
                Boss = new BossBattleState(
                    "training_boss",
                    ElementType.Water,
                    bossHp,
                    100d);
                Water = WaterElementResource.Register(
                    Boss.Resources,
                    config.WaterElementMaxAmount);
                if (initialWater > 0)
                {
                    Water.Add(initialWater);
                }

                State = new SerenaCoralspearBattleState(
                    Character,
                    config.EmpoweredChaseMaxCharges);
                completionRule =
                    new SerenaCoralspearMatch5CompletionRule(State);
                Provider = new SerenaCoralspearMatchActionProvider(
                    config,
                    awakening);
                ActionIds = new CombatActionIdSequence();
                FinalComboCount = finalComboCount;
                if (createActions)
                {
                    RecreateActions(tier);
                }
                else
                {
                    Actions = Array.Empty<CombatAction>();
                }
            }

            public CharacterBattleState Character { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public ResourceState Water { get; }
            public SerenaCoralspearBattleState State { get; }
            public SerenaCoralspearMatchActionProvider Provider { get; }
            public CombatActionIdSequence ActionIds { get; }
            public int FinalComboCount { get; }
            public IReadOnlyList<CombatAction> Actions { get; set; }
            public DamageAction MatchDamage => (DamageAction)Actions[0];

            public void RecreateActions(BoardMatchTier tier)
            {
                Actions = Provider.CreateActions(
                    Character,
                    Party,
                    Boss,
                    tier,
                    FinalComboCount,
                    ActionIds);
            }

            public CombatActionExecutionResult Execute(
                params ICombatTriggerRule[] additionalRules)
            {
                var rules = new ICombatTriggerRule[
                    additionalRules.Length + 1];
                rules[0] = completionRule;
                for (int index = 0;
                    index < additionalRules.Length;
                    index++)
                {
                    rules[index + 1] = additionalRules[index];
                }

                var executor = new CombatActionExecutor(
                    Boss,
                    Party,
                    new DamageContextFactory(
                        new SeededRandomSource(123)),
                    new CombatTriggerResolver(rules));
                return executor.Execute(new CombatActionQueue(Actions));
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        private sealed class AddWaterAfterMatchRule
            : ICombatTriggerRule
        {
            private readonly BossBattleState boss;
            private readonly long matchActionId;

            public AddWaterAfterMatchRule(
                BossBattleState boss,
                long matchActionId)
            {
                this.boss = boss;
                this.matchActionId = matchActionId;
            }

            public IReadOnlyList<CombatAction> CreateDerivedActions(
                CombatActionTriggerContext context)
            {
                if (context.ActionId != matchActionId)
                {
                    return Array.Empty<CombatAction>();
                }

                return new CombatAction[]
                {
                    new AddResourceAction(
                        100,
                        ActionOrigin.System,
                        boss,
                        WaterElementResource.Id,
                        1,
                        context.RootActionId,
                        context.ActionId)
                };
            }
        }
    }
}
