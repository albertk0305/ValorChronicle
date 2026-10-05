using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Marea
{
    public sealed class MareaBluefangAwakeningCombatTests
    {
        [TestCase(2, 0.15d)]
        [TestCase(3, 0.18d)]
        public void Passive_WithWater_UsesAwakeningRate(
            int awakening,
            double expectedRate)
        {
            using (var battle = new TestBattle(awakening))
            {
                battle.Water.Add(1);

                DamageAction damage = battle.CreateDamage(
                    BoardMatchTier.Three);

                Assert.That(
                    damage.ContextRequest
                        .ActionLocalDealtDamageIncreaseRate,
                    Is.EqualTo(expectedRate));
            }
        }

        [TestCase(2)]
        [TestCase(3)]
        public void Passive_WithoutWater_DoesNotApply(int awakening)
        {
            using (var battle = new TestBattle(awakening))
            {
                DamageAction damage = battle.CreateDamage(
                    BoardMatchTier.Three);

                Assert.That(
                    damage.ContextRequest
                        .ActionLocalDealtDamageIncreaseRate,
                    Is.Zero);
            }
        }

        [TestCase(3, BoardMatchTier.Three, 0, 0.90d)]
        [TestCase(4, BoardMatchTier.Three, 0, 1.00d)]
        [TestCase(3, BoardMatchTier.Four, 1, 1.90d)]
        [TestCase(4, BoardMatchTier.Four, 1, 2.10d)]
        [TestCase(4, BoardMatchTier.Four, 0, 1.65d)]
        public void Match3And4_UseAwakeningSpecificCoefficients(
            int awakening,
            BoardMatchTier tier,
            int initialWater,
            double expectedCoefficient)
        {
            using (var battle = new TestBattle(awakening))
            {
                if (initialWater > 0)
                {
                    battle.Water.Add(initialWater);
                }

                IReadOnlyList<CombatAction> actions =
                    battle.CreateActions(tier);

                Assert.That(
                    ((DamageAction)actions[0]).ContextRequest
                        .SkillCoefficient,
                    Is.EqualTo(expectedCoefficient)
                        .Within(0.000000001d));
                AddResourceAction add = actions
                    .OfType<AddResourceAction>()
                    .Single();
                Assert.That(add.Amount, Is.EqualTo(1));
            }
        }

        [Test]
        public void Awakening4_DevourStillUsesOneFortyPerWater()
        {
            using (var battle = new TestBattle(awakening: 4))
            {
                battle.Water.Add(5);

                DamageAction damage = battle.CreateDamage(
                    BoardMatchTier.FiveOrMore);

                Assert.That(
                    damage.ContextRequest.SkillCoefficient,
                    Is.EqualTo(9.40d));
            }
        }

        [TestCase(0, 2.40d)]
        [TestCase(1, 4.00d)]
        [TestCase(2, 5.60d)]
        [TestCase(3, 7.20d)]
        [TestCase(4, 8.80d)]
        [TestCase(5, 10.40d)]
        public void Awakening5_DevourUsesOneSixtyPerWater(
            int initialWater,
            double expectedCoefficient)
        {
            using (var battle = new TestBattle(awakening: 5))
            {
                if (initialWater > 0)
                {
                    battle.Water.Add(initialWater);
                }

                DamageAction damage = battle.CreateDamage(
                    BoardMatchTier.FiveOrMore);

                Assert.That(
                    damage.ContextRequest.SkillCoefficient,
                    Is.EqualTo(expectedCoefficient)
                        .Within(0.000000001d));
            }
        }

        [Test]
        public void Awakening6_ConsumingFiveGrantsLingeringSurge()
        {
            using (var battle = new TestBattle(awakening: 6))
            {
                battle.Water.Add(5);

                CombatActionExecutionResult result = battle.Execute(
                    BoardMatchTier.FiveOrMore);

                Assert.That(result.ActionResults
                        .OfType<ConsumeResourceActionResult>()
                        .Single()
                        .ConsumeResult.ConsumedAmount,
                    Is.EqualTo(5));
                Assert.That(
                    MareaBluefangLingeringSurgeState.HasCharge(
                        battle.Character),
                    Is.True);
            }
        }

        [Test]
        public void Awakening6_ConsumingFourDoesNotGrantLingeringSurge()
        {
            using (var battle = new TestBattle(awakening: 6))
            {
                battle.Water.Add(4);

                battle.Execute(BoardMatchTier.FiveOrMore);

                Assert.That(
                    MareaBluefangLingeringSurgeState.HasCharge(
                        battle.Character),
                    Is.False);
            }
        }

        [Test]
        public void Awakening6_LingeringSurgeRemainsAtOneCharge()
        {
            using (var battle = new TestBattle(awakening: 6))
            {
                battle.Water.Add(5);
                battle.Execute(BoardMatchTier.FiveOrMore);
                battle.Water.Add(5);

                battle.Execute(BoardMatchTier.FiveOrMore);

                AssertSingleLingeringEffects(battle.Character);
            }
        }

        [Test]
        public void Awakening6_LingeringSurgePersistsAcrossTurnEnd()
        {
            using (var battle = ChargedAwakeningSixBattle())
            {
                battle.Character.Effects.ProcessTurnEnd();

                Assert.That(
                    MareaBluefangLingeringSurgeState.HasCharge(
                        battle.Character),
                    Is.True);
            }
        }

        [TestCase(BoardMatchTier.Three, 1.00d)]
        [TestCase(BoardMatchTier.Four, 1.65d)]
        public void Awakening6_NextMatch3Or4UsesSurgeThenConsumesIt(
            BoardMatchTier tier,
            double expectedCoefficient)
        {
            using (var battle = ChargedAwakeningSixBattle())
            {
                CombatActionExecutionResult result = battle.Execute(tier);
                DamageActionResult damage = DamageResult(result);

                Assert.That(damage.Context.SkillCoefficient,
                    Is.EqualTo(expectedCoefficient));
                Assert.That(
                    damage.Context.AttackTypeDamageIncreaseRateSum,
                    Is.EqualTo(0.30d));
                Assert.That(
                    MareaBluefangLingeringSurgeState.HasCharge(
                        battle.Character),
                    Is.False);
            }
        }

        [Test]
        public void Awakening6_Match5NeitherUsesNorConsumesExistingSurge()
        {
            using (var battle = ChargedAwakeningSixBattle())
            {
                battle.Water.Add(5);

                CombatActionExecutionResult result = battle.Execute(
                    BoardMatchTier.FiveOrMore);
                DamageActionResult damage = DamageResult(result);

                Assert.That(
                    damage.Context.AttackTypeDamageIncreaseRateSum,
                    Is.Zero);
                Assert.That(
                    MareaBluefangLingeringSurgeState.HasCharge(
                        battle.Character),
                    Is.True);
                AssertSingleLingeringEffects(battle.Character);
            }
        }

        [Test]
        public void Awakening6_MaxDevourThenNextMatchUsesSurge()
        {
            using (var battle = new TestBattle(awakening: 6))
            {
                battle.Water.Add(5);
                battle.Execute(BoardMatchTier.FiveOrMore);

                DamageActionResult next = DamageResult(
                    battle.Execute(BoardMatchTier.Three));

                Assert.That(
                    next.Context.AttackTypeDamageIncreaseRateSum,
                    Is.EqualTo(0.30d));
                Assert.That(
                    MareaBluefangLingeringSurgeState.HasCharge(
                        battle.Character),
                    Is.False);
            }
        }

        [Test]
        public void Awakening5_MaxDevourDoesNotGrantLingeringSurge()
        {
            using (var battle = new TestBattle(awakening: 5))
            {
                battle.Water.Add(5);

                battle.Execute(BoardMatchTier.FiveOrMore);

                Assert.That(
                    MareaBluefangLingeringSurgeState.HasCharge(
                        battle.Character),
                    Is.False);
            }
        }

        [Test]
        public void Awakening6_KeepsAllEarlierAwakeningRules()
        {
            MareaBluefangCombatConfig config =
                MareaBluefangTestConfig.Create();
            try
            {
                MareaBluefangResolvedCombatRules rules =
                    MareaBluefangCombatRuleResolver.Resolve(config, 6);

                Assert.That(rules.PassiveDealtDamageIncreaseRate,
                    Is.EqualTo(0.18d));
                Assert.That(rules.Match3Coefficient,
                    Is.EqualTo(1.00d));
                Assert.That(rules.Match4BaseCoefficient,
                    Is.EqualTo(1.65d));
                Assert.That(
                    rules.Match4WaterElementBonusCoefficient,
                    Is.EqualTo(0.45d));
                Assert.That(rules.Match5BaseCoefficient,
                    Is.EqualTo(2.40d));
                Assert.That(
                    rules.Match5CoefficientPerWaterElement,
                    Is.EqualTo(1.60d));
                Assert.That(rules.HasLingeringSurge, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void LingeringSurgeUsesSeparateDamageMultiplierCategory()
        {
            using (var battle = ChargedAwakeningSixBattle())
            {
                battle.ExecuteActive();
                battle.Water.Add(1);

                DamageActionResult damage = DamageResult(
                    battle.Execute(BoardMatchTier.Three));

                Assert.That(
                    damage.Context.ElementDamageIncreaseRateSum,
                    Is.EqualTo(0.25d));
                Assert.That(
                    damage.Context.AttackTypeDamageIncreaseRateSum,
                    Is.EqualTo(0.30d));
                Assert.That(
                    damage.Context.DealtDamageIncreaseRateSum,
                    Is.EqualTo(0.18d));
                Assert.That(
                    damage.DamageResult.RawDamage,
                    Is.EqualTo(1917.5d).Within(0.000000001d));
                Assert.That(
                    damage.DamageResult.FinalDamage,
                    Is.EqualTo(1917L));
            }
        }

        private static TestBattle ChargedAwakeningSixBattle()
        {
            var battle = new TestBattle(awakening: 6);
            battle.Water.Add(5);
            battle.Execute(BoardMatchTier.FiveOrMore);
            Assert.That(
                MareaBluefangLingeringSurgeState.HasCharge(
                    battle.Character),
                Is.True);
            return battle;
        }

        private static DamageActionResult DamageResult(
            CombatActionExecutionResult result)
        {
            return result.ActionResults
                .OfType<DamageActionResult>()
                .Single();
        }

        private static void AssertSingleLingeringEffects(
            CharacterBattleState character)
        {
            Assert.That(character.Effects.FindByEffectId(
                    MareaBluefangRules.LingeringSurgeMatch3EffectId),
                Has.Count.EqualTo(1));
            Assert.That(character.Effects.FindByEffectId(
                    MareaBluefangRules.LingeringSurgeMatch4EffectId),
                Has.Count.EqualTo(1));
        }

        private sealed class TestBattle : IDisposable
        {
            private readonly MareaBluefangCombatConfig config;
            private readonly MareaBluefangMatchActionProvider matchProvider;
            private readonly MareaBluefangActiveActionProvider activeProvider;
            private readonly CombatActionIdSequence actionIds =
                new CombatActionIdSequence();

            public TestBattle(int awakening)
            {
                config = MareaBluefangTestConfig.Create();
                Character = new CharacterBattleState(
                    MareaBluefangRules.CharacterId,
                    0,
                    ElementType.Water,
                    1000,
                    1000d);
                Party = new PartyBattleState(new[] { Character });
                Boss = new BossBattleState(
                    "training_boss",
                    ElementType.Water,
                    1000000000L,
                    100d);
                Water = WaterElementResource.Register(
                    Boss.Resources,
                    config.WaterElementMaxAmount);
                matchProvider = new MareaBluefangMatchActionProvider(
                    config,
                    awakening);
                activeProvider = new MareaBluefangActiveActionProvider(
                    config);
            }

            public CharacterBattleState Character { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public ResourceState Water { get; }

            public DamageAction CreateDamage(BoardMatchTier tier)
            {
                return (DamageAction)CreateActions(tier)[0];
            }

            public IReadOnlyList<CombatAction> CreateActions(
                BoardMatchTier tier)
            {
                return matchProvider.CreateActions(
                    Character,
                    Party,
                    Boss,
                    tier,
                    finalComboCount: 1,
                    actionIds);
            }

            public CombatActionExecutionResult Execute(BoardMatchTier tier)
            {
                return Executor().Execute(
                    new CombatActionQueue(CreateActions(tier)));
            }

            public void ExecuteActive()
            {
                Executor().Execute(new CombatActionQueue(
                    activeProvider.CreateActions(Character, actionIds)));
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(config);
            }

            private CombatActionExecutor Executor()
            {
                return new CombatActionExecutor(
                    Boss,
                    Party,
                    new DamageContextFactory(
                        new SeededRandomSource(123)));
            }
        }
    }
}
