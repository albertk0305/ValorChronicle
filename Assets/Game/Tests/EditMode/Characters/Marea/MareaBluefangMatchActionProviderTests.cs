using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Marea
{
    public sealed class MareaBluefangMatchActionProviderTests
    {
        [Test]
        public void ThreeMatchWithoutWaterCreatesDamageThenAdd()
        {
            TestBattle battle = CreateBattle(BoardMatchTier.Three, 0);

            Assert.That(battle.Actions.Count, Is.EqualTo(2));
            DamageAction damage = AssertDamage(
                battle,
                0.90d,
                0d,
                AttackTag.Match3);
            AssertFollowUpLineage<AddResourceAction>(battle, damage);

            CombatActionExecutionResult result = battle.Execute();

            Assert.That(result.ActionResults[0],
                Is.TypeOf<DamageActionResult>());
            Assert.That(result.ActionResults[1],
                Is.TypeOf<AddResourceActionResult>());
            Assert.That(DamageResult(result).DamageResult.FinalDamage,
                Is.EqualTo(900L));
            Assert.That(battle.Water.CurrentAmount, Is.EqualTo(1));
        }

        [TestCase(1, 2)]
        [TestCase(5, 5)]
        public void ThreeMatchWithWaterUsesPassiveAndClampsAdd(
            int initialWater,
            int expectedWater)
        {
            TestBattle battle = CreateBattle(
                BoardMatchTier.Three,
                initialWater);
            DamageAction damage = AssertDamage(
                battle,
                0.90d,
                0.15d,
                AttackTag.Match3);
            AssertFollowUpLineage<AddResourceAction>(battle, damage);

            CombatActionExecutionResult result = battle.Execute();

            Assert.That(DamageResult(result).DamageResult.FinalDamage,
                Is.EqualTo(1035L));
            Assert.That(battle.Water.CurrentAmount,
                Is.EqualTo(expectedWater));
        }

        [TestCase(0, 1.50d, 0d, 1500L)]
        [TestCase(1, 1.90d, 0.15d, 2185L)]
        public void FourMatchUsesOneConditionalDamageThenAdd(
            int initialWater,
            double expectedCoefficient,
            double expectedPassive,
            long expectedDamage)
        {
            TestBattle battle = CreateBattle(
                BoardMatchTier.Four,
                initialWater);
            DamageAction damage = AssertDamage(
                battle,
                expectedCoefficient,
                expectedPassive,
                AttackTag.Match4);

            Assert.That(battle.Actions.Count, Is.EqualTo(2));
            Assert.That(battle.Actions.OfType<DamageAction>().Count(),
                Is.EqualTo(1));
            Assert.That(battle.Actions.Any(action =>
                    action.Origin == ActionOrigin.Additional
                    || action.Origin == ActionOrigin.Chase),
                Is.False);
            AssertFollowUpLineage<AddResourceAction>(battle, damage);

            CombatActionExecutionResult result = battle.Execute();

            Assert.That(DamageResult(result).DamageResult.FinalDamage,
                Is.EqualTo(expectedDamage));
            Assert.That(battle.Water.CurrentAmount,
                Is.EqualTo(Math.Min(5, initialWater + 1)));
        }

        [TestCase(0, 2.40d, 2400d, 2400L)]
        [TestCase(1, 3.80d, 4370d, 4370L)]
        [TestCase(2, 5.20d, 5980d, 5980L)]
        [TestCase(3, 6.60d, 7590d, 7590L)]
        [TestCase(4, 8.00d, 9200d, 9200L)]
        [TestCase(5, 9.40d, 10810d, 10810L)]
        public void FiveMatchUsesOneDamageAndSnapshotConsumption(
            int initialWater,
            double expectedCoefficient,
            double expectedRawDamage,
            long expectedFlooredDamage)
        {
            TestBattle battle = CreateBattle(
                BoardMatchTier.FiveOrMore,
                initialWater);
            double expectedPassive = initialWater > 0 ? 0.15d : 0d;
            DamageAction damage = AssertDamage(
                battle,
                expectedCoefficient,
                expectedPassive,
                AttackTag.Match5Plus);

            Assert.That(battle.Actions.OfType<DamageAction>().Count(),
                Is.EqualTo(1));
            Assert.That(battle.Actions.Any(action =>
                    action.Origin == ActionOrigin.Additional
                    || action.Origin == ActionOrigin.Chase),
                Is.False);

            if (initialWater == 0)
            {
                Assert.That(battle.Actions.Count, Is.EqualTo(1));
                Assert.That(battle.Actions.OfType<ConsumeResourceAction>(),
                    Is.Empty);
            }
            else
            {
                Assert.That(battle.Actions.Count, Is.EqualTo(2));
                ConsumeResourceAction consume =
                    battle.Actions[1] as ConsumeResourceAction;
                Assert.That(consume, Is.Not.Null);
                Assert.That(consume.Mode,
                    Is.EqualTo(ResourceConsumptionMode.Amount));
                Assert.That(consume.Amount, Is.EqualTo(initialWater));
                Assert.That(consume.ResourceId,
                    Is.EqualTo(WaterElementResource.Id));
                Assert.That(consume.ConsumerId,
                    Is.EqualTo(MareaBluefangRules.CharacterId));
                AssertFollowUpLineage<ConsumeResourceAction>(battle, damage);
            }

            CombatActionExecutionResult result = battle.Execute();
            DamageActionResult damageResult = DamageResult(result);

            Assert.That(damageResult.DamageResult.RawDamage,
                Is.EqualTo(expectedRawDamage).Within(0.000000001d));
            Assert.That(damageResult.DamageResult.FinalDamage,
                Is.EqualTo(expectedFlooredDamage));
            Assert.That(battle.Water.CurrentAmount, Is.Zero);

            ConsumeResourceActionResult[] consumptionResults =
                result.ActionResults
                    .OfType<ConsumeResourceActionResult>()
                    .ToArray();
            if (initialWater == 0)
            {
                Assert.That(consumptionResults, Is.Empty);
            }
            else
            {
                Assert.That(consumptionResults, Has.Length.EqualTo(1));
                AssertConsumption(
                    consumptionResults[0],
                    battle,
                    initialWater);
            }
        }

        [Test]
        public void ThreeMatchAppliesFinalComboCount()
        {
            TestBattle battle = CreateBattle(
                BoardMatchTier.Three,
                0,
                finalComboCount: 3);

            CombatActionExecutionResult result = battle.Execute();
            DamageActionResult damage = DamageResult(result);

            Assert.That(damage.Context.AppliesCombo, Is.True);
            Assert.That(damage.Context.FinalComboCount, Is.EqualTo(3));
            Assert.That(damage.DamageResult.ComboMultiplier,
                Is.EqualTo(1.12d));
            Assert.That(damage.DamageResult.FinalDamage, Is.EqualTo(1008L));
        }

        [Test]
        public void FiveMatchSnapshotIgnoresWaterAddedAfterDamage()
        {
            TestBattle battle = CreateBattle(
                BoardMatchTier.FiveOrMore,
                3);
            DamageAction damage = (DamageAction)battle.Actions[0];
            var addAfterDamage = new AddWaterAfterDamageRule(
                battle.Boss,
                damage.ActionId);

            CombatActionExecutionResult result = battle.Execute(
                addAfterDamage);

            Assert.That(damage.ContextRequest.SkillCoefficient,
                Is.EqualTo(6.60d).Within(0.000000001d));
            Assert.That(
                damage.ContextRequest.ActionLocalDealtDamageIncreaseRate,
                Is.EqualTo(0.15d));
            Assert.That(result.ActionResults[0],
                Is.TypeOf<DamageActionResult>());
            Assert.That(result.ActionResults[1],
                Is.TypeOf<AddResourceActionResult>());
            Assert.That(result.ActionResults[2],
                Is.TypeOf<ConsumeResourceActionResult>());
            ConsumeResourceActionResult consume =
                (ConsumeResourceActionResult)result.ActionResults[2];
            Assert.That(consume.ConsumeResult.AmountBefore, Is.EqualTo(4));
            Assert.That(consume.ConsumeResult.RequestedAmount, Is.EqualTo(3));
            Assert.That(consume.ConsumeResult.ConsumedAmount, Is.EqualTo(3));
            Assert.That(battle.Water.CurrentAmount, Is.EqualTo(1));
        }

        [Test]
        public void ConsumptionIsObservedOnceWithDamageLineage()
        {
            TestBattle battle = CreateBattle(
                BoardMatchTier.FiveOrMore,
                3);
            DamageAction damage = (DamageAction)battle.Actions[0];
            var observer = new ConsumptionObserverRule();

            CombatActionExecutionResult result = battle.Execute(observer);

            Assert.That(observer.ObservationCount, Is.EqualTo(1));
            Assert.That(observer.Context, Is.Not.Null);
            Assert.That(observer.Context.RootActionId,
                Is.EqualTo(damage.ActionId));
            Assert.That(observer.Context.SourceActionId,
                Is.EqualTo(damage.ActionId));
            Assert.That(observer.Context.CanTriggerAttackFollowUp, Is.True);
            AssertConsumption(observer.Result, battle, 3);
            Assert.That(result.ActionResults
                    .OfType<ConsumeResourceActionResult>()
                    .Count(),
                Is.EqualTo(1));
        }

        [Test]
        public void LethalDamagePreservesTerminalQueueContract()
        {
            TestBattle battle = CreateBattle(
                BoardMatchTier.FiveOrMore,
                3,
                bossHp: 1000);
            var observer = new ConsumptionObserverRule();

            CombatActionExecutionResult result = battle.Execute(observer);

            Assert.That(result.CompletedActionCount, Is.EqualTo(1));
            Assert.That(result.StoppedEarly, Is.True);
            Assert.That(result.BossDefeated, Is.True);
            Assert.That(result.ClearedRemainingActions, Is.True);
            Assert.That(battle.Water.CurrentAmount, Is.EqualTo(3));
            Assert.That(observer.ObservationCount, Is.Zero);
        }

        private static TestBattle CreateBattle(
            BoardMatchTier tier,
            int initialWater,
            int finalComboCount = 1,
            long bossHp = 1000000)
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
                ElementType.Water,
                bossHp,
                100d);
            ResourceState water = WaterElementResource.Register(
                boss.Resources);
            if (initialWater > 0)
            {
                water.Add(initialWater);
            }

            var provider = new MareaBluefangMatchActionProvider();
            IReadOnlyList<CombatAction> actions = provider.CreateActions(
                character,
                party,
                boss,
                tier,
                finalComboCount,
                new CombatActionIdSequence());
            return new TestBattle(
                character,
                party,
                boss,
                water,
                actions);
        }

        private static DamageAction AssertDamage(
            TestBattle battle,
            double expectedCoefficient,
            double expectedPassive,
            AttackTag expectedTag)
        {
            DamageAction damage = battle.Actions[0] as DamageAction;
            Assert.That(damage, Is.Not.Null);
            Assert.That(damage.Origin, Is.EqualTo(ActionOrigin.Match));
            Assert.That(damage.RootActionId, Is.EqualTo(damage.ActionId));
            Assert.That(damage.SourceActionId, Is.Null);
            Assert.That(damage.ContextRequest.Attacker,
                Is.SameAs(battle.Character));
            Assert.That(damage.ContextRequest.Party,
                Is.SameAs(battle.Party));
            Assert.That(damage.ContextRequest.TargetBoss,
                Is.SameAs(battle.Boss));
            Assert.That(damage.ContextRequest.AttackElement,
                Is.EqualTo(ElementType.Water));
            Assert.That(damage.ContextRequest.AttackType,
                Is.EqualTo(AttackType.Match));
            Assert.That(damage.ContextRequest.AttackTags,
                Is.EqualTo(expectedTag));
            Assert.That(damage.ContextRequest.SkillCoefficient,
                Is.EqualTo(expectedCoefficient).Within(0.000000001d));
            Assert.That(damage.ContextRequest.AppliesCombo, Is.True);
            Assert.That(damage.ContextRequest.CanCritical, Is.True);
            Assert.That(
                damage.ContextRequest.ActionLocalDealtDamageIncreaseRate,
                Is.EqualTo(expectedPassive));
            return damage;
        }

        private static void AssertFollowUpLineage<TAction>(
            TestBattle battle,
            DamageAction damage)
            where TAction : CombatAction
        {
            Assert.That(battle.Actions[1], Is.TypeOf<TAction>());
            CombatAction followUp = battle.Actions[1];
            Assert.That(followUp.RootActionId,
                Is.EqualTo(damage.ActionId));
            Assert.That(followUp.SourceActionId,
                Is.EqualTo(damage.ActionId));
        }

        private static DamageActionResult DamageResult(
            CombatActionExecutionResult result)
        {
            return (DamageActionResult)result.ActionResults[0];
        }

        private static void AssertConsumption(
            ConsumeResourceActionResult result,
            TestBattle battle,
            int expectedAmount)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ConsumptionRecord, Is.Not.Null);
            Assert.That(result.ConsumeResult.RequestedAmount,
                Is.EqualTo(expectedAmount));
            Assert.That(result.ConsumeResult.ConsumedAmount,
                Is.EqualTo(expectedAmount));
            Assert.That(result.ConsumptionRecord.ResourceId,
                Is.EqualTo(WaterElementResource.Id));
            Assert.That(result.ConsumptionRecord.ConsumerId,
                Is.EqualTo(battle.Character.CharacterId));
            Assert.That(result.ConsumptionRecord.TargetId,
                Is.EqualTo(battle.Boss.BossId));
            Assert.That(result.ConsumptionRecord.ConsumedAmount,
                Is.EqualTo(expectedAmount));
        }

        private sealed class TestBattle
        {
            public TestBattle(
                CharacterBattleState character,
                PartyBattleState party,
                BossBattleState boss,
                ResourceState water,
                IReadOnlyList<CombatAction> actions)
            {
                Character = character;
                Party = party;
                Boss = boss;
                Water = water;
                Actions = actions;
            }

            public CharacterBattleState Character { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public ResourceState Water { get; }
            public IReadOnlyList<CombatAction> Actions { get; }

            public CombatActionExecutionResult Execute(
                params ICombatTriggerRule[] rules)
            {
                var executor = new CombatActionExecutor(
                    Boss,
                    Party,
                    new DamageContextFactory(
                        new SeededRandomSource(123)),
                    new CombatTriggerResolver(rules));
                return executor.Execute(new CombatActionQueue(Actions));
            }
        }

        private sealed class ConsumptionObserverRule : ICombatTriggerRule
        {
            public int ObservationCount { get; private set; }
            public CombatActionTriggerContext Context { get; private set; }
            public ConsumeResourceActionResult Result { get; private set; }

            public IReadOnlyList<CombatAction> CreateDerivedActions(
                CombatActionTriggerContext context)
            {
                if (context.CompletedResult is
                    ConsumeResourceActionResult result)
                {
                    ObservationCount++;
                    Context = context;
                    Result = result;
                }

                return Array.Empty<CombatAction>();
            }
        }

        private sealed class AddWaterAfterDamageRule : ICombatTriggerRule
        {
            private readonly BossBattleState boss;
            private readonly long damageActionId;

            public AddWaterAfterDamageRule(
                BossBattleState boss,
                long damageActionId)
            {
                this.boss = boss;
                this.damageActionId = damageActionId;
            }

            public IReadOnlyList<CombatAction> CreateDerivedActions(
                CombatActionTriggerContext context)
            {
                if (context.ActionId != damageActionId)
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
