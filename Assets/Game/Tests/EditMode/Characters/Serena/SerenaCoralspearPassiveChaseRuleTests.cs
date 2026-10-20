using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Characters.Serena;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Tests.EditMode;

namespace ValorChronicle.Tests.EditMode.Characters.Serena
{
    public sealed class SerenaCoralspearPassiveChaseRuleTests
    {
        [TestCase(1, 0.75d, 750L)]
        [TestCase(2, 1.50d, 1500L)]
        [TestCase(4, 3.00d, 3000L)]
        [TestCase(5, 3.75d, 3750L)]
        public void OtherAllyWaterConsumptionCreatesOneChase(
            int consumedAmount,
            double expectedCoefficient,
            long expectedDamage)
        {
            using (var battle = new TestBattle())
            {
                IReadOnlyList<CombatAction> actions =
                    battle.CreateMareaFiveMatch(consumedAmount);

                CombatActionExecutionResult result = battle.Execute(actions);
                DamageActionResult chase = ChaseResults(result).Single();

                Assert.That(ChaseResults(result).Length, Is.EqualTo(1));
                Assert.That(chase.Action.ActionId, Is.EqualTo(3L));
                Assert.That(chase.Action.RootActionId,
                    Is.EqualTo(actions[0].ActionId));
                Assert.That(chase.Action.SourceActionId,
                    Is.EqualTo(actions[1].ActionId));
                Assert.That(chase.Action.CanTriggerFollowUp, Is.False);
                var chaseAction = (DamageAction)chase.Action;
                Assert.That(chaseAction.Origin,
                    Is.EqualTo(ActionOrigin.Chase));
                Assert.That(chaseAction.ContextRequest.AttackType,
                    Is.EqualTo(AttackType.Chase));
                Assert.That(chaseAction.ContextRequest.Attacker,
                    Is.SameAs(battle.Serena));
                Assert.That(chaseAction.ContextRequest.Party,
                    Is.SameAs(battle.Party));
                Assert.That(chaseAction.ContextRequest.TargetBoss,
                    Is.SameAs(battle.Boss));
                Assert.That(chaseAction.ContextRequest.AttackElement,
                    Is.EqualTo(ElementType.Water));
                Assert.That(chaseAction.ContextRequest.AppliesCombo,
                    Is.False);
                Assert.That(chaseAction.ContextRequest.CanCritical,
                    Is.True);
                Assert.That(chase.Context.SkillCoefficient,
                    Is.EqualTo(expectedCoefficient));
                Assert.That(chase.DamageResult.FinalDamage,
                    Is.EqualTo(expectedDamage));
                Assert.That(chase.AppliedDamage, Is.EqualTo(expectedDamage));
                Assert.That(battle.Water.CurrentAmount, Is.Zero);
            }
        }

        [Test]
        public void Awakening3UsesEightyFivePercentPerConsumedWater()
        {
            using (var battle = new TestBattle(awakening: 3))
            {
                CombatActionExecutionResult result = battle.Execute(
                    battle.CreateMareaFiveMatch(2));
                DamageActionResult chase = ChaseResults(result).Single();

                Assert.That(chase.Context.SkillCoefficient,
                    Is.EqualTo(1.70d));
                Assert.That(chase.DamageResult.FinalDamage,
                    Is.EqualTo(1870L));
            }
        }

        [TestCase(0, 0.10d, 1650L, 0)]
        [TestCase(1, 0.90d, 2850L, 0)]
        public void Awakening2AndEmpoweredIncreaseShareDamageRateCategory(
            int initialCharges,
            double expectedRate,
            long expectedDamage,
            int expectedRemainingCharges)
        {
            using (var battle = new TestBattle(
                awakening: 2,
                initialCharges: initialCharges))
            {
                CombatActionExecutionResult result = battle.Execute(
                    battle.CreateMareaFiveMatch(2));
                DamageActionResult chase = ChaseResults(result).Single();

                Assert.That(chase.Context.AttackTypeDamageIncreaseRateSum,
                    Is.EqualTo(expectedRate));
                Assert.That(chase.DamageResult.FinalDamage,
                    Is.EqualTo(expectedDamage));
                Assert.That(battle.State.EmpoweredChaseCharges,
                    Is.EqualTo(expectedRemainingCharges));
            }
        }

        [Test]
        public void ThreeChargesEmpowerThreeSeparateChasesWithoutStacking()
        {
            using (var battle = new TestBattle(initialCharges: 3))
            {
                var rates = new List<double>();
                for (int index = 0; index < 4; index++)
                {
                    CombatActionExecutionResult result = battle.Execute(
                        battle.CreateMareaFiveMatch(1));
                    rates.Add(ChaseResults(result).Single().Context
                        .AttackTypeDamageIncreaseRateSum);
                }

                Assert.That(rates, Is.EqualTo(new[]
                {
                    0.80d,
                    0.80d,
                    0.80d,
                    0d
                }));
                Assert.That(battle.State.EmpoweredChaseCharges, Is.Zero);
            }
        }

        [Test]
        public void FourConsumptionEventsCreateFourOrderedUniqueChases()
        {
            using (var battle = new TestBattle(initialCharges: 3))
            {
                battle.Water.Add(4);
                var actions = new List<CombatAction>();
                for (int index = 0; index < 4; index++)
                {
                    DamageAction source = battle.CreateDamage(
                        battle.Marea,
                        ActionOrigin.Match,
                        0d);
                    actions.Add(source);
                    actions.Add(battle.CreateConsume(
                        battle.Marea,
                        WaterElementResource.Id,
                        1,
                        source.RootActionId,
                        source.ActionId));
                }

                CombatActionExecutionResult result = battle.Execute(actions);
                DamageActionResult[] chases = ChaseResults(result);

                Assert.That(chases.Length, Is.EqualTo(4));
                Assert.That(chases.Select(item => item.Action.ActionId),
                    Is.EqualTo(new long[] { 9, 10, 11, 12 }));
                Assert.That(chases.Select(item => item.Action.RootActionId),
                    Is.EqualTo(new long[] { 1, 3, 5, 7 }));
                Assert.That(chases.Select(item =>
                        item.Action.SourceActionId.Value),
                    Is.EqualTo(new long[] { 2, 4, 6, 8 }));
                Assert.That(chases.Select(item => item.Context
                        .AttackTypeDamageIncreaseRateSum),
                    Is.EqualTo(new[] { 0.80d, 0.80d, 0.80d, 0d }));
                Assert.That(battle.State.EmpoweredChaseCharges, Is.Zero);
            }
        }

        [Test]
        public void SameConsumptionContextCannotCreateDuplicateChase()
        {
            using (var battle = new TestBattle())
            {
                CombatActionTriggerContext consumptionContext = null;
                var observer = new DelegateRule(context =>
                {
                    if (context.CompletedResult is
                        ConsumeResourceActionResult)
                    {
                        consumptionContext = context;
                    }

                    return Array.Empty<CombatAction>();
                });

                CombatActionExecutionResult result = battle.Execute(
                    battle.CreateMareaFiveMatch(2),
                    observer,
                    battle.PassiveRule);
                long lastIssuedId = battle.ActionIds.LastIssuedId;
                IReadOnlyList<CombatAction> duplicate =
                    battle.PassiveRule.CreateDerivedActions(
                        consumptionContext);

                Assert.That(ChaseResults(result).Length, Is.EqualTo(1));
                Assert.That(duplicate.Count, Is.Zero);
                Assert.That(battle.ActionIds.LastIssuedId,
                    Is.EqualTo(lastIssuedId));
            }
        }

        [Test]
        public void SerenaOwnConsumptionDoesNotCreateChase()
        {
            using (var battle = new TestBattle(initialCharges: 1))
            {
                battle.Water.Add(1);
                DamageAction source = battle.CreateDamage(
                    battle.Serena,
                    ActionOrigin.Match,
                    0d);
                var actions = new CombatAction[]
                {
                    source,
                    battle.CreateConsume(
                        battle.Serena,
                        WaterElementResource.Id,
                        1,
                        source.RootActionId,
                        source.ActionId)
                };

                CombatActionExecutionResult result = battle.Execute(actions);

                Assert.That(ChaseResults(result).Length, Is.Zero);
                Assert.That(battle.State.EmpoweredChaseCharges,
                    Is.EqualTo(1));
            }
        }

        [Test]
        public void ZeroConsumptionOtherResourceAndMissingSourceAreIgnored()
        {
            using (var zero = new TestBattle())
            using (var otherResource = new TestBattle())
            using (var missingSource = new TestBattle())
            {
                DamageAction zeroSource = zero.CreateDamage(
                    zero.Marea,
                    ActionOrigin.Match,
                    0d);
                CombatActionExecutionResult zeroResult = zero.Execute(
                    new CombatAction[]
                    {
                        zeroSource,
                        zero.CreateConsume(
                            zero.Marea,
                            WaterElementResource.Id,
                            1,
                            zeroSource.RootActionId,
                            zeroSource.ActionId)
                    });

                otherResource.Boss.Resources.Register("fire_resource", 5);
                otherResource.Boss.Resources.Add("fire_resource", 1);
                DamageAction otherSource = otherResource.CreateDamage(
                    otherResource.Marea,
                    ActionOrigin.Match,
                    0d);
                CombatActionExecutionResult otherResult =
                    otherResource.Execute(new CombatAction[]
                    {
                        otherSource,
                        otherResource.CreateConsume(
                            otherResource.Marea,
                            "fire_resource",
                            1,
                            otherSource.RootActionId,
                            otherSource.ActionId)
                    });

                missingSource.Water.Add(1);
                long consumeId = missingSource.ActionIds.Next();
                var missingAction = new ConsumeResourceAction(
                    consumeId,
                    ActionOrigin.System,
                    missingSource.Boss,
                    WaterElementResource.Id,
                    missingSource.Marea.CharacterId,
                    ResourceConsumptionMode.Amount,
                    1,
                    rootActionId: 999,
                    sourceActionId: 999);
                CombatActionExecutionResult missingResult =
                    missingSource.Execute(
                        new CombatAction[] { missingAction });

                Assert.That(ChaseResults(zeroResult).Length, Is.Zero);
                Assert.That(ChaseResults(otherResult).Length, Is.Zero);
                Assert.That(ChaseResults(missingResult).Length, Is.Zero);
            }
        }

        [Test]
        public void ChaseSourceCannotCreateRecursiveChase()
        {
            using (var battle = new TestBattle())
            {
                battle.Water.Add(1);
                DamageAction source = battle.CreateDamage(
                    battle.Marea,
                    ActionOrigin.Chase,
                    0d,
                    rootActionId: 100,
                    sourceActionId: 99);
                var actions = new CombatAction[]
                {
                    source,
                    battle.CreateConsume(
                        battle.Marea,
                        WaterElementResource.Id,
                        1,
                        source.RootActionId,
                        source.ActionId)
                };

                CombatActionExecutionResult result = battle.Execute(actions);

                Assert.That(ChaseResults(result).Length, Is.EqualTo(1));
                Assert.That(result.CompletedActionCount, Is.EqualTo(2));
            }
        }

        [Test]
        public void LethalSourceStopsBeforeConsumptionAndChase()
        {
            using (var battle = new TestBattle(bossHp: 1000L))
            {
                battle.Water.Add(1);
                DamageAction source = battle.CreateDamage(
                    battle.Marea,
                    ActionOrigin.Match,
                    2d);
                var actions = new CombatAction[]
                {
                    source,
                    battle.CreateConsume(
                        battle.Marea,
                        WaterElementResource.Id,
                        1,
                        source.RootActionId,
                        source.ActionId)
                };

                CombatActionExecutionResult result = battle.Execute(actions);

                Assert.That(result.CompletedActionCount, Is.EqualTo(1));
                Assert.That(result.BossDefeated, Is.True);
                Assert.That(result.ClearedRemainingActions, Is.True);
                Assert.That(battle.Water.CurrentAmount, Is.EqualTo(1));
                Assert.That(ChaseResults(result).Length, Is.Zero);
            }
        }

        [Test]
        public void EmpoweredChargeIsNotConsumedWhenChaseIsCleared()
        {
            using (var battle = new TestBattle(
                initialCharges: 1,
                bossHp: 10000L))
            {
                IReadOnlyList<CombatAction> actions =
                    battle.CreateMareaFiveMatch(1);
                var killerRule = new DelegateRule(context =>
                {
                    if (!(context.CompletedResult is
                        ConsumeResourceActionResult))
                    {
                        return Array.Empty<CombatAction>();
                    }

                    return new CombatAction[]
                    {
                        battle.CreateDamage(
                            battle.Marea,
                            ActionOrigin.Chase,
                            20d,
                            context.RootActionId,
                            context.ActionId)
                    };
                });

                CombatActionExecutionResult result = battle.Execute(
                    actions,
                    killerRule,
                    battle.PassiveRule);

                Assert.That(result.BossDefeated, Is.True);
                Assert.That(result.ClearedRemainingActions, Is.True);
                Assert.That(ChaseResults(result).Length, Is.EqualTo(1));
                Assert.That(((DamageAction)ChaseResults(result)[0].Action)
                        .ContextRequest.Attacker,
                    Is.SameAs(battle.Marea));
                Assert.That(battle.State.EmpoweredChaseCharges,
                    Is.EqualTo(1));
            }
        }

        [Test]
        public void Match5GrantAndPassiveChaseUseSameBattleState()
        {
            using (var battle = new TestBattle())
            {
                var completionRule =
                    new SerenaCoralspearMatch5CompletionRule(battle.State);
                var provider = new SerenaCoralspearMatchActionProvider(
                    battle.SerenaConfig);
                IReadOnlyList<CombatAction> matchActions =
                    provider.CreateActions(
                        battle.Serena,
                        battle.Party,
                        battle.Boss,
                        BoardMatchTier.FiveOrMore,
                        1,
                        battle.ActionIds);

                battle.Execute(
                    matchActions,
                    completionRule,
                    battle.PassiveRule);
                Assert.That(battle.State.EmpoweredChaseCharges,
                    Is.EqualTo(1));

                CombatActionExecutionResult chaseResult = battle.Execute(
                    battle.CreateMareaFiveMatch(1),
                    completionRule,
                    battle.PassiveRule);
                DamageActionResult chase = ChaseResults(chaseResult).Single();

                Assert.That(chase.Context.AttackTypeDamageIncreaseRateSum,
                    Is.EqualTo(0.80d));
                Assert.That(battle.State.EmpoweredChaseCharges, Is.Zero);
            }
        }

        [Test]
        public void FourMatchAdditionalDoesNotConsumeEmpoweredCharge()
        {
            using (var battle = new TestBattle(initialCharges: 1))
            {
                battle.Water.Add(1);
                var provider = new SerenaCoralspearMatchActionProvider(
                    battle.SerenaConfig);
                IReadOnlyList<CombatAction> actions = provider.CreateActions(
                    battle.Serena,
                    battle.Party,
                    battle.Boss,
                    BoardMatchTier.Four,
                    1,
                    battle.ActionIds);

                CombatActionExecutionResult result = battle.Execute(actions);

                Assert.That(result.ActionResults
                        .OfType<DamageActionResult>()
                        .Count(),
                    Is.EqualTo(2));
                Assert.That(ChaseResults(result).Length, Is.Zero);
                Assert.That(battle.State.EmpoweredChaseCharges,
                    Is.EqualTo(1));
            }
        }

        [Test]
        public void ChaseConsumptionDoesNotAffectAnotherBattleState()
        {
            using (var first = new TestBattle(initialCharges: 1))
            using (var second = new TestBattle(initialCharges: 1))
            {
                first.Execute(first.CreateMareaFiveMatch(1));

                Assert.That(first.State.EmpoweredChaseCharges, Is.Zero);
                Assert.That(second.State.EmpoweredChaseCharges,
                    Is.EqualTo(1));
            }
        }

        private static DamageActionResult[] ChaseResults(
            CombatActionExecutionResult result)
        {
            return result.ActionResults
                .OfType<DamageActionResult>()
                .Where(item => item.Action.Origin == ActionOrigin.Chase)
                .ToArray();
        }

        private sealed class TestBattle : IDisposable
        {
            private readonly MareaBluefangCombatConfig mareaConfig;

            public TestBattle(
                int awakening = 0,
                int initialCharges = 0,
                long bossHp = 1000000L)
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
                    bossHp,
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

            public DamageAction CreateDamage(
                CharacterBattleState attacker,
                ActionOrigin origin,
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
                        origin == ActionOrigin.Chase
                            ? AttackType.Chase
                            : AttackType.Match,
                        AttackTag.None,
                        coefficient,
                        appliesCombo: false,
                        finalComboCount: 0,
                        canCritical: false),
                    rootActionId,
                    sourceActionId);
            }

            public ConsumeResourceAction CreateConsume(
                CharacterBattleState consumer,
                string resourceId,
                int amount,
                long rootActionId,
                long sourceActionId)
            {
                return new ConsumeResourceAction(
                    ActionIds.Next(),
                    ActionOrigin.System,
                    Boss,
                    resourceId,
                    consumer.CharacterId,
                    ResourceConsumptionMode.Amount,
                    amount,
                    rootActionId,
                    sourceActionId);
            }

            public CombatActionExecutionResult Execute(
                IReadOnlyList<CombatAction> actions,
                params ICombatTriggerRule[] rules)
            {
                IReadOnlyList<ICombatTriggerRule> resolvedRules =
                    rules.Length == 0
                        ? new ICombatTriggerRule[] { PassiveRule }
                        : rules;
                var executor = new CombatActionExecutor(
                    Boss,
                    Party,
                    new DamageContextFactory(new SeededRandomSource(123)),
                    new CombatTriggerResolver(resolvedRules));
                return executor.Execute(new CombatActionQueue(actions));
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(SerenaConfig);
                UnityEngine.Object.DestroyImmediate(mareaConfig);
            }
        }

        private sealed class DelegateRule : ICombatTriggerRule
        {
            private readonly Func<
                CombatActionTriggerContext,
                IReadOnlyList<CombatAction>> createActions;

            public DelegateRule(
                Func<CombatActionTriggerContext,
                    IReadOnlyList<CombatAction>> createActions)
            {
                this.createActions = createActions;
            }

            public IReadOnlyList<CombatAction> CreateDerivedActions(
                CombatActionTriggerContext context)
            {
                return createActions(context);
            }
        }
    }
}
