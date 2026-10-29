using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Characters.Koharu;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Koharu
{
    public sealed class SakuragiKoharuMatchActionProviderTests
    {
        [TestCase(BoardMatchTier.Three, 0, 0.50d, AttackTag.Match3)]
        [TestCase(BoardMatchTier.Four, 0, 0.80d, AttackTag.Match4)]
        [TestCase(BoardMatchTier.FiveOrMore, 0, 1.30d,
            AttackTag.Match5Plus)]
        [TestCase(BoardMatchTier.Three, 3, 0.55d, AttackTag.Match3)]
        [TestCase(BoardMatchTier.Four, 3, 0.90d, AttackTag.Match4)]
        [TestCase(BoardMatchTier.FiveOrMore, 4, 1.45d,
            AttackTag.Match5Plus)]
        public void CreatesGrassMatchDamageWithResolvedCoefficient(
            BoardMatchTier tier,
            int awakening,
            double coefficient,
            AttackTag tag)
        {
            using (var battle = new TestBattle(tier, awakening))
            {
                DamageAction damage = battle.MatchDamage;

                Assert.That(damage.ActionId, Is.EqualTo(1));
                Assert.That(damage.Origin, Is.EqualTo(ActionOrigin.Match));
                Assert.That(damage.RootActionId, Is.EqualTo(1));
                Assert.That(damage.SourceActionId, Is.Null);
                Assert.That(damage.ContextRequest.AttackElement,
                    Is.EqualTo(ElementType.Grass));
                Assert.That(damage.ContextRequest.AttackType,
                    Is.EqualTo(AttackType.Match));
                Assert.That(damage.ContextRequest.AttackTags,
                    Is.EqualTo(tag));
                Assert.That(damage.ContextRequest.SkillCoefficient,
                    Is.EqualTo(coefficient));
                Assert.That(damage.ContextRequest.AppliesCombo, Is.True);
                Assert.That(damage.ContextRequest.FinalComboCount,
                    Is.EqualTo(2));
                Assert.That(damage.ContextRequest.CanCritical, Is.True);
            }
        }

        [Test]
        public void FactoryIgnoresWrongElementAndTreatsSixAsFiveOrMore()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.FiveOrMore,
                createActions: false))
            {
                var factory = new MatchEventCombatActionFactory(
                    battle.Party,
                    battle.Boss,
                    battle.Provider,
                    battle.ActionIds);

                IReadOnlyList<CombatAction> wrong =
                    factory.CreateCharacterRootActions(
                        CreateExecution(
                            ElementType.Fire,
                            BoardMatchTier.FiveOrMore,
                            blockCount: 6),
                        battle.Character);
                IReadOnlyList<CombatAction> six =
                    factory.CreateCharacterRootActions(
                        CreateExecution(
                            ElementType.Grass,
                            BoardMatchTier.FiveOrMore,
                            blockCount: 6),
                        battle.Character);

                Assert.That(wrong, Is.Empty);
                Assert.That(six.Count, Is.EqualTo(2));
                Assert.That(((DamageAction)six[0]).ContextRequest
                        .SkillCoefficient,
                    Is.EqualTo(1.30d));
                Assert.That(six.Select(action => action.ActionId),
                    Is.EqualTo(new long[] { 1, 2 }));
            }
        }

        [TestCase(0, 0.12d)]
        [TestCase(4, 0.15d)]
        public void FiveMatchCreatesPartyBuffAfterDamage(
            int awakening,
            double expectedMagnitude)
        {
            using (var battle = new TestBattle(
                BoardMatchTier.FiveOrMore,
                awakening))
            {
                Assert.That(battle.Actions.Count, Is.EqualTo(2));
                var effectAction = battle.Actions[1] as ApplyEffectAction;

                Assert.That(effectAction, Is.Not.Null);
                Assert.That(effectAction.ActionId, Is.EqualTo(2));
                Assert.That(effectAction.Origin,
                    Is.EqualTo(ActionOrigin.System));
                Assert.That(effectAction.TargetType,
                    Is.EqualTo(CombatEffectTargetType.Party));
                Assert.That(effectAction.TargetParty,
                    Is.SameAs(battle.Party));
                Assert.That(effectAction.RootActionId,
                    Is.EqualTo(battle.MatchDamage.ActionId));
                Assert.That(effectAction.SourceActionId,
                    Is.EqualTo(battle.MatchDamage.ActionId));
                Assert.That(effectAction.Effect.EffectId,
                    Is.EqualTo(SakuragiKoharuRules.Match5EffectId));
                Assert.That(effectAction.Effect.Magnitude,
                    Is.EqualTo(expectedMagnitude));
                Assert.That(effectAction.Effect.RemainingTurns,
                    Is.EqualTo(2));
                Assert.That(effectAction.Effect.StackPolicy,
                    Is.EqualTo(EffectStackPolicy.RefreshDuration));
                Assert.That(effectAction.Effect.AttackTypeMask,
                    Is.EqualTo(
                        AttackTypeMask.Additional | AttackTypeMask.Chase));
            }
        }

        [Test]
        public void ExecutionUsesCommonDamageCalculatorAndDoesNotRetroactivelyBuffMatch()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.FiveOrMore))
            {
                var expectedFactory = new DamageContextFactory(
                    new SeededRandomSource(123));
                DamageResult expected = DamageCalculator.Calculate(
                    expectedFactory.Build(
                        battle.MatchDamage.ContextRequest));

                CombatActionExecutionResult result = battle.Execute();
                DamageActionResult damage = result.ActionResults
                    .OfType<DamageActionResult>()
                    .Single();

                Assert.That(damage.DamageResult.FinalDamage,
                    Is.EqualTo(expected.FinalDamage));
                Assert.That(damage.Context.AttackTypeDamageIncreaseRateSum,
                    Is.Zero);
                Assert.That(result.ActionResults[0],
                    Is.TypeOf<DamageActionResult>());
                Assert.That(result.ActionResults[1],
                    Is.TypeOf<ApplyEffectActionResult>());
            }
        }

        [Test]
        public void BuffAppliesOnlyToSubsequentAdditionalAndChase()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.FiveOrMore))
            {
                var actions = new List<CombatAction>(battle.Actions);
                AttackType[] types =
                {
                    AttackType.Additional,
                    AttackType.Chase,
                    AttackType.Match,
                    AttackType.Active,
                    AttackType.DamageOverTime
                };
                for (int index = 0; index < types.Length; index++)
                {
                    actions.Add(battle.CreateSystemDamage(types[index]));
                }

                CombatActionExecutionResult result = battle.Execute(actions);
                double[] rates = result.ActionResults
                    .OfType<DamageActionResult>()
                    .Select(item => item.Context
                        .AttackTypeDamageIncreaseRateSum)
                    .ToArray();

                Assert.That(rates,
                    Is.EqualTo(new[] { 0d, 0.12d, 0.12d, 0d, 0d, 0d }));
            }
        }

        [Test]
        public void DistinctAdditionalAndChaseEffectsAddTogether()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.FiveOrMore,
                awakening: 4))
            {
                battle.Party.Effects.ApplyEffect(new EffectInstance(
                    9001L,
                    "effect_test_distinct_active_buff",
                    battle.Character.CharacterId,
                    EffectCategory.Buff,
                    EffectModifierType.AttackTypeDamageIncrease,
                    0.10d,
                    1,
                    9001L,
                    EffectStackPolicy.RefreshDuration,
                    attackTypeMask:
                        AttackTypeMask.Additional | AttackTypeMask.Chase));
                var actions = new List<CombatAction>(battle.Actions)
                {
                    battle.CreateSystemDamage(AttackType.Additional)
                };

                CombatActionExecutionResult result = battle.Execute(actions);
                DamageActionResult additional = result.ActionResults
                    .OfType<DamageActionResult>()
                    .Last();

                Assert.That(additional.Context
                        .AttackTypeDamageIncreaseRateSum,
                    Is.EqualTo(0.25d));
            }
        }

        [Test]
        public void ReapplicationRefreshesTwoTurnsWithoutStacking()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.FiveOrMore))
            {
                battle.Execute();
                var turns = new BattleTurnEndProcessor(
                    battle.Party,
                    battle.Boss);
                turns.ProcessTurnEnd();
                Assert.That(battle.ActiveMatch5Effect.RemainingTurns,
                    Is.EqualTo(1));

                battle.RecreateActions(BoardMatchTier.FiveOrMore);
                battle.Execute();

                Assert.That(battle.Party.Effects.Count, Is.EqualTo(1));
                Assert.That(battle.ActiveMatch5Effect.Magnitude,
                    Is.EqualTo(0.12d));
                Assert.That(battle.ActiveMatch5Effect.RemainingTurns,
                    Is.EqualTo(2));

                turns.ProcessTurnEnd();
                Assert.That(battle.ActiveMatch5Effect.RemainingTurns,
                    Is.EqualTo(1));
                turns.ProcessTurnEnd();
                Assert.That(battle.Party.Effects.Count, Is.Zero);
            }
        }

        [Test]
        public void LethalMatchClearsBuffAction()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.FiveOrMore,
                bossHp: 1000L))
            {
                CombatActionExecutionResult result = battle.Execute();

                Assert.That(result.CompletedActionCount, Is.EqualTo(1));
                Assert.That(result.BossDefeated, Is.True);
                Assert.That(result.ClearedRemainingActions, Is.True);
                Assert.That(battle.Party.Effects.Count, Is.Zero);
            }
        }

        private static MatchEventExecution CreateExecution(
            ElementType element,
            BoardMatchTier tier,
            int blockCount)
        {
            var positions = new List<BoardPosition>(blockCount);
            for (int index = 0; index < blockCount; index++)
            {
                positions.Add(new BoardPosition(index, 0));
            }

            MatchEvent matchEvent = CreateInternal<MatchEvent>(
                0,
                0,
                0,
                element,
                tier,
                positions[0],
                positions,
                positions.Count);
            return CreateInternal<MatchEventExecution>(
                1L,
                matchEvent,
                2);
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
            private readonly SakuragiKoharuCombatConfig config;

            public TestBattle(
                BoardMatchTier tier,
                int awakening = 0,
                long bossHp = 1000000L,
                bool createActions = true)
            {
                config = SakuragiKoharuTestConfig.Create();
                Character = new CharacterBattleState(
                    SakuragiKoharuRules.CharacterId,
                    2,
                    ElementType.Grass,
                    1100L,
                    1000d);
                Party = new PartyBattleState(new[] { Character });
                Boss = new BossBattleState(
                    "training_boss",
                    ElementType.Water,
                    bossHp,
                    100d);
                ActionIds = new CombatActionIdSequence();
                Provider = new SakuragiKoharuMatchActionProvider(
                    config,
                    awakening);
                Actions = createActions
                    ? Provider.CreateActions(
                        Character,
                        Party,
                        Boss,
                        tier,
                        2,
                        ActionIds)
                    : Array.Empty<CombatAction>();
            }

            public CharacterBattleState Character { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public SakuragiKoharuMatchActionProvider Provider { get; }
            public IReadOnlyList<CombatAction> Actions { get; private set; }
            public DamageAction MatchDamage => (DamageAction)Actions[0];
            public EffectInstance ActiveMatch5Effect =>
                Party.Effects.FindByEffectId(
                    SakuragiKoharuRules.Match5EffectId).Single();

            public void RecreateActions(BoardMatchTier tier)
            {
                Actions = Provider.CreateActions(
                    Character,
                    Party,
                    Boss,
                    tier,
                    2,
                    ActionIds);
            }

            public DamageAction CreateSystemDamage(AttackType attackType)
            {
                return new DamageAction(
                    ActionIds.Next(),
                    ActionOrigin.System,
                    new DamageContextBuildRequest(
                        Character,
                        Party,
                        Boss,
                        ElementType.Grass,
                        attackType,
                        AttackTag.None,
                        0.10d,
                        appliesCombo: false,
                        finalComboCount: 0,
                        canCritical: false));
            }

            public CombatActionExecutionResult Execute()
            {
                return Execute(Actions);
            }

            public CombatActionExecutionResult Execute(
                IReadOnlyList<CombatAction> actions)
            {
                var executor = new CombatActionExecutor(
                    Boss,
                    Party,
                    new DamageContextFactory(
                        new SeededRandomSource(123)));
                return executor.Execute(new CombatActionQueue(actions));
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}
