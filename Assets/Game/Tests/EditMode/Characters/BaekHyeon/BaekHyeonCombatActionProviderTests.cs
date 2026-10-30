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
using ValorChronicle.Characters.BaekHyeon;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.BaekHyeon
{
    public sealed class BaekHyeonCombatActionProviderTests
    {
        [TestCase(BoardMatchTier.Three, 1, AttackTag.Match3, 0.70d)]
        [TestCase(BoardMatchTier.Four, 2, AttackTag.Match4, 1.25d)]
        [TestCase(BoardMatchTier.FiveOrMore, 3, AttackTag.Match5Plus, 2.00d)]
        public void MatchCreatesDamageThenExpectedMarks(
            BoardMatchTier tier,
            int markGain,
            AttackTag tag,
            double coefficient)
        {
            using (var battle = new TestBattle())
            {
                IReadOnlyList<CombatAction> actions = battle.Match(tier);

                Assert.That(actions.Count, Is.EqualTo(1 + markGain));
                var damage = (DamageAction)actions[0];
                Assert.That(damage.Origin, Is.EqualTo(ActionOrigin.Match));
                Assert.That(damage.ContextRequest.AttackElement,
                    Is.EqualTo(ElementType.Grass));
                Assert.That(damage.ContextRequest.AttackType,
                    Is.EqualTo(AttackType.Match));
                Assert.That(damage.ContextRequest.AttackTags, Is.EqualTo(tag));
                Assert.That(damage.ContextRequest.AppliesCombo, Is.True);
                Assert.That(damage.ContextRequest.CanCritical, Is.True);

                foreach (ApplyEffectAction mark in actions.Skip(1))
                {
                    Assert.That(mark.Origin, Is.EqualTo(ActionOrigin.System));
                    Assert.That(mark.TargetType,
                        Is.EqualTo(CombatEffectTargetType.Boss));
                    Assert.That(mark.RootActionId,
                        Is.EqualTo(damage.ActionId));
                    Assert.That(mark.SourceActionId,
                        Is.EqualTo(damage.ActionId));
                }

                CombatActionExecutionResult execution = battle.Execute(actions);
                DamageActionResult result = execution.ActionResults
                    .OfType<DamageActionResult>().Single();
                Assert.That(result.Context.SkillCoefficient,
                    Is.EqualTo(coefficient).Within(0.000000001d));
                Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(battle.Boss),
                    Is.EqualTo(markGain));
            }
        }

        [TestCase(BoardMatchTier.Three, 5, 0.85d)]
        [TestCase(BoardMatchTier.Four, 5, 1.50d)]
        [TestCase(BoardMatchTier.FiveOrMore, 5, 2.40d)]
        [TestCase(BoardMatchTier.Three, 10, 1.00d)]
        [TestCase(BoardMatchTier.Four, 10, 1.75d)]
        [TestCase(BoardMatchTier.FiveOrMore, 10, 2.80d)]
        public void MatchCoefficientUsesMarksAtExecution(
            BoardMatchTier tier,
            int initialMarks,
            double expectedCoefficient)
        {
            using (var battle = new TestBattle())
            {
                IReadOnlyList<CombatAction> actions = battle.Match(tier);
                battle.AddMarks(initialMarks);

                DamageActionResult result = battle.Execute(actions)
                    .ActionResults.OfType<DamageActionResult>().Single();

                Assert.That(result.Context.SkillCoefficient,
                    Is.EqualTo(expectedCoefficient).Within(0.000000001d));
                Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(battle.Boss),
                    Is.EqualTo(Math.Min(10, initialMarks + MarkGain(tier))));
            }
        }

        [TestCase(BoardMatchTier.Three, 0.75d)]
        [TestCase(BoardMatchTier.Four, 1.35d)]
        [TestCase(BoardMatchTier.FiveOrMore, 2.00d)]
        public void AwakeningFourChangesOnlyConfiguredMatchBases(
            BoardMatchTier tier,
            double expectedCoefficient)
        {
            using (var battle = new TestBattle(awakening: 4))
            {
                DamageActionResult result = battle.Execute(battle.Match(tier))
                    .ActionResults.OfType<DamageActionResult>().Single();
                Assert.That(result.Context.SkillCoefficient,
                    Is.EqualTo(expectedCoefficient));
            }
        }

        [Test]
        public void ConsecutiveMatchesUseMarksAppliedByEarlierMatch()
        {
            using (var battle = new TestBattle())
            {
                battle.Execute(battle.Match(BoardMatchTier.Three));
                DamageActionResult second = battle.Execute(
                        battle.Match(BoardMatchTier.Four))
                    .ActionResults.OfType<DamageActionResult>().Single();

                Assert.That(second.Context.SkillCoefficient,
                    Is.EqualTo(1.30d).Within(0.000000001d));
                Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(battle.Boss),
                    Is.EqualTo(3));
            }
        }

        [Test]
        public void LethalMatchClearsPendingMarkActions()
        {
            using (var battle = new TestBattle(bossHp: 1L))
            {
                CombatActionExecutionResult execution = battle.Execute(
                    battle.Match(BoardMatchTier.FiveOrMore));

                Assert.That(execution.CompletedActionCount, Is.EqualTo(1));
                Assert.That(execution.BossDefeated, Is.True);
                Assert.That(execution.ClearedRemainingActions, Is.True);
                Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(battle.Boss),
                    Is.Zero);
            }
        }

        [Test]
        public void DamageTriggerRunsBeforePendingMarkApplication()
        {
            using (var battle = new TestBattle())
            {
                battle.Boss.Resources.Register("trigger_marker", 10);
                IReadOnlyList<CombatAction> actions = battle.Match(
                    BoardMatchTier.Three);
                var executor = new CombatActionExecutor(
                    battle.Boss,
                    battle.Party,
                    new DamageContextFactory(new SeededRandomSource(123)),
                    new CombatTriggerResolver(new ICombatTriggerRule[]
                    {
                        new MatchDamageMarkerTrigger()
                    }));

                CombatActionExecutionResult execution = executor.Execute(
                    new CombatActionQueue(actions));

                Assert.That(execution.ActionResults[0],
                    Is.TypeOf<DamageActionResult>());
                Assert.That(execution.ActionResults[1],
                    Is.TypeOf<AddResourceActionResult>());
                Assert.That(execution.ActionResults[2],
                    Is.TypeOf<ApplyEffectActionResult>());
                Assert.That(battle.Boss.Resources.GetAmount("trigger_marker"),
                    Is.EqualTo(1));
                Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(battle.Boss),
                    Is.EqualTo(1));
            }
        }

        [Test]
        public void ProviderRejectsNonBaekHyeonGrassAttacker()
        {
            using (var battle = new TestBattle())
            {
                var other = new CharacterBattleState(
                    "other_grass_character",
                    1,
                    ElementType.Grass,
                    1000L,
                    1000d);

                Assert.That(
                    () => battle.MatchProvider.CreateActions(
                        other,
                        battle.Party,
                        battle.Boss,
                        BoardMatchTier.Three,
                        1,
                        battle.ActionIds),
                    Throws.TypeOf<ArgumentException>());
            }
        }

        [TestCase(0, 0, 2.50d)]
        [TestCase(0, 5, 3.50d)]
        [TestCase(0, 10, 4.50d)]
        [TestCase(5, 0, 2.80d)]
        [TestCase(5, 5, 3.90d)]
        [TestCase(5, 10, 5.00d)]
        public void ActiveUsesExecutionTimeMarksWithoutChangingThem(
            int awakening,
            int marks,
            double expectedCoefficient)
        {
            using (var battle = new TestBattle(awakening))
            {
                IReadOnlyList<CombatAction> actions = battle.Active();
                battle.AddMarks(marks);
                var damage = (DamageAction)actions.Single();

                Assert.That(battle.ActiveProvider.CooldownTurns, Is.EqualTo(8));
                Assert.That(damage.Origin, Is.EqualTo(ActionOrigin.Active));
                Assert.That(damage.ContextRequest.AttackType,
                    Is.EqualTo(AttackType.Active));
                Assert.That(damage.ContextRequest.AttackElement,
                    Is.EqualTo(ElementType.Grass));
                Assert.That(damage.ContextRequest.AppliesCombo, Is.False);

                DamageActionResult result = battle.Execute(actions)
                    .ActionResults.OfType<DamageActionResult>().Single();
                Assert.That(result.Context.SkillCoefficient,
                    Is.EqualTo(expectedCoefficient).Within(0.000000001d));
                Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(battle.Boss),
                    Is.EqualTo(marks));
            }
        }

        private static int MarkGain(BoardMatchTier tier)
        {
            return tier == BoardMatchTier.Three
                ? 1
                : tier == BoardMatchTier.Four ? 2 : 3;
        }

        private sealed class MatchDamageMarkerTrigger : ICombatTriggerRule
        {
            public IReadOnlyList<CombatAction> CreateDerivedActions(
                CombatActionTriggerContext context)
            {
                if (!(context.CompletedResult is DamageActionResult)
                    || context.CompletedAction.Origin != ActionOrigin.Match)
                {
                    return Array.Empty<CombatAction>();
                }

                return new CombatAction[]
                {
                    new AddResourceAction(
                        9000L,
                        ActionOrigin.System,
                        context.Boss,
                        "trigger_marker",
                        1,
                        context.RootActionId,
                        context.ActionId)
                };
            }
        }

        private sealed class TestBattle : IDisposable
        {
            private readonly BaekHyeonCombatConfig config;
            private readonly BaekHyeonResolvedCombatRules rules;
            private long nextEffectId = 1000L;

            public TestBattle(int awakening = 0, long bossHp = 1000000L)
            {
                config = BaekHyeonTestConfig.Create();
                rules = BaekHyeonCombatRuleResolver.Resolve(config, awakening);
                Character = new CharacterBattleState(
                    BaekHyeonRules.CharacterId,
                    0,
                    ElementType.Grass,
                    1000L,
                    1000d);
                Party = new PartyBattleState(new[] { Character });
                Boss = new BossBattleState(
                    "training_boss",
                    ElementType.Light,
                    bossHp,
                    100d);
                ActionIds = new CombatActionIdSequence();
                MatchProvider = new BaekHyeonMatchActionProvider(
                    config,
                    awakening);
                ActiveProvider = new BaekHyeonActiveActionProvider(
                    config,
                    awakening);
                Executor = new CombatActionExecutor(
                    Boss,
                    Party,
                    new DamageContextFactory(new SeededRandomSource(123)));
            }

            public CharacterBattleState Character { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public BaekHyeonMatchActionProvider MatchProvider { get; }
            public BaekHyeonActiveActionProvider ActiveProvider { get; }
            public CombatActionExecutor Executor { get; }

            public IReadOnlyList<CombatAction> Match(BoardMatchTier tier)
            {
                return MatchProvider.CreateActions(
                    Character,
                    Party,
                    Boss,
                    tier,
                    finalComboCount: 1,
                    ActionIds);
            }

            public IReadOnlyList<CombatAction> Active()
            {
                return ActiveProvider.CreateActions(
                    Character,
                    Party,
                    Boss,
                    ActionIds);
            }

            public void AddMarks(int count)
            {
                for (int index = 0; index < count; index++)
                {
                    Boss.Effects.ApplyEffect(
                        BaekHyeonMarkEffectFactory.Create(
                            nextEffectId++,
                            rules));
                }
            }

            public CombatActionExecutionResult Execute(
                IReadOnlyList<CombatAction> actions)
            {
                return Executor.Execute(new CombatActionQueue(actions));
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}
