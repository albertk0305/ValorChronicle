using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Brina;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Brina
{
    public sealed class BrinaAshwoodCombatActionTests
    {
        [TestCase(BoardMatchTier.Three, 0, 1.30d, AttackTag.Match3)]
        [TestCase(BoardMatchTier.Three, 4, 1.35d, AttackTag.Match3)]
        [TestCase(BoardMatchTier.Four, 0, 2.40d, AttackTag.Match4)]
        [TestCase(BoardMatchTier.Four, 4, 2.50d, AttackTag.Match4)]
        [TestCase(BoardMatchTier.FiveOrMore, 0, 4.00d,
            AttackTag.Match5Plus)]
        [TestCase(BoardMatchTier.FiveOrMore, 6, 4.00d,
            AttackTag.Match5Plus)]
        public void ProviderCreatesOneGrassMatchWithResolvedCoefficient(
            BoardMatchTier tier,
            int awakening,
            double coefficient,
            AttackTag tag)
        {
            using (var battle = new TestBattle(tier, awakening))
            {
                Assert.That(battle.Actions.Count, Is.EqualTo(1));
                DamageAction action = battle.MatchDamage;
                Assert.That(action.Origin, Is.EqualTo(ActionOrigin.Match));
                Assert.That(action.RootActionId,
                    Is.EqualTo(action.ActionId));
                Assert.That(action.SourceActionId, Is.Null);
                Assert.That(action.ContextRequest.AttackElement,
                    Is.EqualTo(ElementType.Grass));
                Assert.That(action.ContextRequest.AttackType,
                    Is.EqualTo(AttackType.Match));
                Assert.That(action.ContextRequest.AttackTags,
                    Is.EqualTo(tag));
                Assert.That(action.ContextRequest.SkillCoefficient,
                    Is.EqualTo(coefficient));
                Assert.That(action.ContextRequest.AppliesCombo, Is.True);
                Assert.That(action.ContextRequest.CanCritical, Is.True);
                Assert.That(battle.State.WoodcutCount, Is.Zero);
            }
        }

        [Test]
        public void MatchDamageUsesComboAndGainsWoodcutOnlyAfterExecution()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Three,
                finalComboCount: 3))
            {
                Assert.That(battle.State.WoodcutCount, Is.Zero);

                CombatActionExecutionResult execution = battle.Execute();
                DamageActionResult damage = execution.ActionResults
                    .OfType<DamageActionResult>()
                    .Single();

                Assert.That(damage.Context.FinalComboCount, Is.EqualTo(3));
                Assert.That(damage.DamageResult.ComboMultiplier,
                    Is.EqualTo(1.12d));
                Assert.That(damage.DamageResult.FinalDamage,
                    Is.EqualTo(1456L));
                Assert.That(battle.State.WoodcutCount, Is.EqualTo(1));
            }
        }

        [TestCase(BoardMatchTier.Three, 1)]
        [TestCase(BoardMatchTier.Four, 2)]
        [TestCase(BoardMatchTier.FiveOrMore, 3)]
        public void CompletedMatchGainsTierWoodcut(
            BoardMatchTier tier,
            int expectedGain)
        {
            using (var battle = new TestBattle(tier))
            {
                battle.Execute(playerTurnNumber: 4);

                Assert.That(battle.State.WoodcutCount,
                    Is.EqualTo(expectedGain));
                Assert.That(battle.State.LastWoodcutGainTurn,
                    Is.EqualTo(4));
            }
        }

        [TestCase(BoardMatchTier.Three, 2)]
        [TestCase(BoardMatchTier.Four, 4)]
        [TestCase(BoardMatchTier.FiveOrMore, 6)]
        public void ActiveDoublesOnlyBaseMatchWoodcut(
            BoardMatchTier tier,
            int expectedGain)
        {
            using (var battle = new TestBattle(tier))
            {
                battle.ApplyActive();

                battle.ExecuteMatch(tier, playerTurnNumber: 1);

                Assert.That(battle.State.WoodcutCount,
                    Is.EqualTo(expectedGain));
            }
        }

        [Test]
        public void ActiveAppliesToEveryMatchInTheSameTurn()
        {
            using (var battle = new TestBattle(BoardMatchTier.Three))
            {
                battle.ApplyActive();

                battle.ExecuteMatch(
                    BoardMatchTier.Three,
                    playerTurnNumber: 4);
                battle.ExecuteMatch(
                    BoardMatchTier.Four,
                    playerTurnNumber: 4);
                CombatActionExecutionResult last = battle.ExecuteMatch(
                    BoardMatchTier.FiveOrMore,
                    playerTurnNumber: 4);

                Assert.That(battle.State.WoodcutCount, Is.EqualTo(2));
                Assert.That(last.ActionResults.Count, Is.EqualTo(2));
                Assert.That(last.ActionResults[1].Action.Origin,
                    Is.EqualTo(ActionOrigin.Additional));
            }
        }

        [Test]
        public void TurnEndExpiresActiveAndRestoresBaseGain()
        {
            using (var battle = new TestBattle(BoardMatchTier.Three))
            {
                battle.ApplyActive();
                battle.ExecuteMatch(
                    BoardMatchTier.Three,
                    playerTurnNumber: 1);
                Assert.That(battle.State.WoodcutCount, Is.EqualTo(2));

                battle.TurnEnd.ProcessTurnEnd();
                battle.ExecuteMatch(
                    BoardMatchTier.Three,
                    playerTurnNumber: 2);

                Assert.That(battle.Character.Effects.Count, Is.Zero);
                Assert.That(battle.State.WoodcutCount, Is.EqualTo(3));
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void AwakeningBelowSixDoesNotGrantFirstGainBonus(
            int awakening)
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Three,
                awakening))
            {
                battle.Execute(playerTurnNumber: 2);

                Assert.That(battle.State.WoodcutCount, Is.EqualTo(1));
            }
        }

        [TestCase(BoardMatchTier.Three, 2)]
        [TestCase(BoardMatchTier.Four, 3)]
        [TestCase(BoardMatchTier.FiveOrMore, 4)]
        public void AwakeningSixAddsOneToFirstGain(
            BoardMatchTier tier,
            int expectedGain)
        {
            using (var battle = new TestBattle(tier, awakening: 6))
            {
                battle.Execute(playerTurnNumber: 3);

                Assert.That(battle.State.WoodcutCount,
                    Is.EqualTo(expectedGain));
            }
        }

        [Test]
        public void AwakeningSixBonusAppliesOnceAcrossSameTurnMatches()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Three,
                awakening: 6))
            {
                battle.ExecuteMatch(
                    BoardMatchTier.Three,
                    playerTurnNumber: 7);
                battle.ExecuteMatch(
                    BoardMatchTier.Three,
                    playerTurnNumber: 7);

                Assert.That(battle.State.WoodcutCount, Is.EqualTo(3));
            }
        }

        [Test]
        public void AwakeningSixBonusIsAvailableAgainNextTurn()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Three,
                awakening: 6))
            {
                battle.ExecuteMatch(
                    BoardMatchTier.Three,
                    playerTurnNumber: 1);
                battle.ExecuteMatch(
                    BoardMatchTier.Three,
                    playerTurnNumber: 2);

                Assert.That(battle.State.WoodcutCount, Is.EqualTo(4));
            }
        }

        [Test]
        public void TurnWithoutWoodcutDoesNotConsumeAwakeningSixBonus()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Three,
                awakening: 6,
                createActions: false))
            {
                battle.TurnEnd.ProcessTurnEnd();
                Assert.That(battle.State.LastWoodcutGainTurn, Is.Null);

                battle.ExecuteMatch(
                    BoardMatchTier.Three,
                    playerTurnNumber: 2);

                Assert.That(battle.State.WoodcutCount, Is.EqualTo(2));
                Assert.That(battle.State.LastWoodcutGainTurn,
                    Is.EqualTo(2));
            }
        }

        [TestCase(BoardMatchTier.Three, 3)]
        [TestCase(BoardMatchTier.Four, 5)]
        [TestCase(BoardMatchTier.FiveOrMore, 7)]
        public void ActiveMultiplierAndAwakeningSixBonusAreIndependent(
            BoardMatchTier tier,
            int expectedGain)
        {
            using (var battle = new TestBattle(tier, awakening: 6))
            {
                battle.ApplyActive();

                battle.ExecuteMatch(tier, playerTurnNumber: 5);

                Assert.That(battle.State.WoodcutCount,
                    Is.EqualTo(expectedGain));
            }
        }

        [Test]
        public void AwakeningSixBonusCanReachThresholdAndTriggerAdditional()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Three,
                awakening: 6))
            {
                battle.State.GainWoodcut(8);

                CombatActionExecutionResult execution = battle.Execute(
                    playerTurnNumber: 1);

                Assert.That(battle.State.WoodcutCount, Is.Zero);
                Assert.That(execution.ActionResults.Count, Is.EqualTo(2));
                Assert.That(execution.ActionResults[1].Action.Origin,
                    Is.EqualTo(ActionOrigin.Additional));
                Assert.That(execution.ActionResults[1].Action.RootActionId,
                    Is.EqualTo(battle.MatchDamage.ActionId));
                Assert.That(execution.ActionResults[1].Action.SourceActionId,
                    Is.EqualTo(battle.MatchDamage.ActionId));
            }
        }

        [TestCase(0, 5.00d)]
        [TestCase(2, 5.00d)]
        [TestCase(3, 5.25d)]
        public void ThresholdCreatesNonComboCriticalAdditional(
            int awakening,
            double expectedCoefficient)
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Three,
                awakening))
            {
                battle.State.GainWoodcut(9);

                CombatActionExecutionResult execution = battle.Execute();
                DamageAction additional = (DamageAction)execution
                    .ActionResults[1].Action;

                Assert.That(additional.Origin,
                    Is.EqualTo(ActionOrigin.Additional));
                Assert.That(additional.ContextRequest.AttackType,
                    Is.EqualTo(AttackType.Additional));
                Assert.That(additional.ContextRequest.AttackElement,
                    Is.EqualTo(ElementType.Grass));
                Assert.That(additional.ContextRequest.SkillCoefficient,
                    Is.EqualTo(expectedCoefficient));
                Assert.That(additional.ContextRequest.AppliesCombo,
                    Is.False);
                Assert.That(additional.ContextRequest.CanCritical, Is.True);
                Assert.That(additional.RootActionId,
                    Is.EqualTo(battle.MatchDamage.ActionId));
                Assert.That(additional.SourceActionId,
                    Is.EqualTo(battle.MatchDamage.ActionId));
                Assert.That(battle.State.WoodcutCount, Is.Zero);
            }
        }

        [Test]
        public void AdditionalOnlyUsesAdditionalAttackTypeIncrease()
        {
            using (var battle = new TestBattle(BoardMatchTier.Three))
            {
                battle.Character.Effects.ApplyEffect(
                    AttackTypeIncreaseEffect(
                        1L,
                        "effect_test_additional_damage",
                        0.25d,
                        AttackTypeMask.Additional));
                battle.Character.Effects.ApplyEffect(
                    AttackTypeIncreaseEffect(
                        2L,
                        "effect_test_match_damage",
                        0.40d,
                        AttackTypeMask.Match));
                battle.State.GainWoodcut(9);

                DamageActionResult[] damages = battle.Execute()
                    .ActionResults.OfType<DamageActionResult>().ToArray();

                Assert.That(damages[0].Context
                    .AttackTypeDamageIncreaseRateSum, Is.EqualTo(0.40d));
                Assert.That(damages[1].Context
                    .AttackTypeDamageIncreaseRateSum,
                    Is.EqualTo(0.25d));
                Assert.That(damages[1].DamageResult.FinalDamage,
                    Is.EqualTo(6250L));
            }
        }

        [Test]
        public void AdditionalCompletionDoesNotGainWoodcutOrChain()
        {
            using (var battle = new TestBattle(BoardMatchTier.Three))
            {
                battle.State.GainWoodcut(9);

                CombatActionExecutionResult execution = battle.Execute();

                Assert.That(execution.CompletedActionCount, Is.EqualTo(2));
                Assert.That(battle.State.WoodcutCount, Is.Zero);
            }
        }

        [Test]
        public void OtherCharacterMatchDoesNotGainWoodcut()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Three,
                createActions: false))
            {
                var other = new CharacterBattleState(
                    "character_other",
                    1,
                    ElementType.Grass,
                    1000L,
                    1000d);
                battle.ReplaceParty(new PartyBattleState(
                    new[] { battle.Character, other }));
                DamageAction otherMatch = CreateDamage(
                    battle.ActionIds.Next(),
                    other,
                    battle.Party,
                    battle.Boss,
                    ActionOrigin.Match,
                    AttackType.Match,
                    AttackTag.Match3,
                    1d);

                battle.Execute(new CombatAction[] { otherMatch });

                Assert.That(battle.State.WoodcutCount, Is.Zero);
            }
        }

        [Test]
        public void MultipleDerivedActionsRunInOrderBeforeQueuedAction()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Three,
                match3WoodcutGain: 20))
            {
                var queued = new DamageAction(
                    battle.ActionIds.Next(),
                    ActionOrigin.System,
                    new DamageContextBuildRequest(
                        battle.Character,
                        battle.Party,
                        battle.Boss,
                        ElementType.Grass,
                        AttackType.Match,
                        AttackTag.None,
                        0d,
                        appliesCombo: false,
                        finalComboCount: 0,
                        canCritical: false));
                CombatAction[] actions = battle.Actions
                    .Concat(new CombatAction[] { queued }).ToArray();

                CombatActionExecutionResult execution =
                    battle.Execute(actions);

                Assert.That(execution.CompletedActionCount, Is.EqualTo(4));
                Assert.That(execution.ActionResults[0].Action,
                    Is.SameAs(battle.MatchDamage));
                Assert.That(execution.ActionResults[1].Action.Origin,
                    Is.EqualTo(ActionOrigin.Additional));
                Assert.That(execution.ActionResults[2].Action.Origin,
                    Is.EqualTo(ActionOrigin.Additional));
                Assert.That(execution.ActionResults[3].Action,
                    Is.SameAs(queued));
                Assert.That(execution.ActionResults.Select(
                    result => result.Action.ActionId).Distinct().Count(),
                    Is.EqualTo(4));
            }
        }

        [Test]
        public void LethalMatchDoesNotGrantWoodcutOrCreateAdditional()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Three,
                bossHp: 1000L))
            {
                battle.State.GainWoodcut(9);

                CombatActionExecutionResult execution = battle.Execute();

                Assert.That(execution.CompletedActionCount, Is.EqualTo(1));
                Assert.That(execution.BossDefeated, Is.True);
                Assert.That(battle.State.WoodcutCount, Is.EqualTo(9));
            }
        }

        [Test]
        public void LethalFirstAdditionalClearsRemainingAdditional()
        {
            using (var battle = new TestBattle(
                BoardMatchTier.Three,
                bossHp: 6000L,
                match3WoodcutGain: 20))
            {
                CombatActionExecutionResult execution = battle.Execute();

                Assert.That(execution.CompletedActionCount, Is.EqualTo(2));
                Assert.That(execution.BossDefeated, Is.True);
                Assert.That(execution.ClearedRemainingActions, Is.True);
            }
        }

        private static EffectInstance AttackTypeIncreaseEffect(
            long runtimeId,
            string effectId,
            double magnitude,
            AttackTypeMask attackTypeMask)
        {
            return new EffectInstance(
                runtimeId,
                effectId,
                "test",
                EffectCategory.Buff,
                EffectModifierType.AttackTypeDamageIncrease,
                magnitude,
                2,
                runtimeId,
                attackTypeMask: attackTypeMask);
        }

        private static DamageAction CreateDamage(
            long actionId,
            CharacterBattleState attacker,
            PartyBattleState party,
            BossBattleState boss,
            ActionOrigin origin,
            AttackType attackType,
            AttackTag tag,
            double coefficient)
        {
            return new DamageAction(
                actionId,
                origin,
                new DamageContextBuildRequest(
                    attacker,
                    party,
                    boss,
                    ElementType.Grass,
                    attackType,
                    tag,
                    coefficient,
                    appliesCombo: false,
                    finalComboCount: 0,
                    canCritical: false));
        }

        private sealed class TestBattle : IDisposable
        {
            private readonly BrinaAshwoodCombatConfig config;
            private PartyBattleState party;

            public TestBattle(
                BoardMatchTier tier,
                int awakening = 0,
                int finalComboCount = 1,
                long bossHp = 1000000L,
                int? match3WoodcutGain = null,
                bool createActions = true)
            {
                config = BrinaAshwoodTestConfig.Create();
                if (match3WoodcutGain.HasValue)
                {
                    BrinaAshwoodTestConfig.SetInt(
                        config,
                        "match3WoodcutGainAmount",
                        match3WoodcutGain.Value);
                }

                Character = new CharacterBattleState(
                    BrinaAshwoodRules.CharacterId,
                    0,
                    ElementType.Grass,
                    1000L,
                    1000d);
                party = new PartyBattleState(new[] { Character });
                Boss = new BossBattleState(
                    "training_boss",
                    ElementType.Grass,
                    bossHp,
                    0d);
                ActionIds = new CombatActionIdSequence();
                State = new BrinaAshwoodBattleState(
                    Character,
                    config.WoodcutTriggerThreshold);
                Rule = new BrinaAshwoodMatchCompletionRule(
                    State,
                    config,
                    awakening,
                    ActionIds);
                Provider = new BrinaAshwoodMatchActionProvider(
                    config,
                    awakening);
                ActiveProvider = new BrinaAshwoodActiveActionProvider(
                    config,
                    awakening);
                TurnEnd = new BattleTurnEndProcessor(party, Boss);
                Actions = createActions
                    ? Provider.CreateActions(
                        Character,
                        party,
                        Boss,
                        tier,
                        finalComboCount,
                        ActionIds)
                    : Array.Empty<CombatAction>();
            }

            public CharacterBattleState Character { get; }
            public PartyBattleState Party => party;
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public BrinaAshwoodBattleState State { get; }
            public BrinaAshwoodMatchCompletionRule Rule { get; }
            public BrinaAshwoodMatchActionProvider Provider { get; }
            public BrinaAshwoodActiveActionProvider ActiveProvider { get; }
            public BattleTurnEndProcessor TurnEnd { get; }
            public IReadOnlyList<CombatAction> Actions { get; }
            public DamageAction MatchDamage => (DamageAction)Actions[0];

            public void ReplaceParty(PartyBattleState replacement)
            {
                party = replacement;
            }

            public CombatActionExecutionResult ApplyActive()
            {
                return Execute(ActiveProvider.CreateActions(
                    Character,
                    ActionIds));
            }

            public CombatActionExecutionResult ExecuteMatch(
                BoardMatchTier tier,
                int playerTurnNumber)
            {
                IReadOnlyList<CombatAction> actions =
                    Provider.CreateActions(
                        Character,
                        party,
                        Boss,
                        tier,
                        finalComboCount: 1,
                        actionIds: ActionIds);
                return Execute(actions, playerTurnNumber);
            }

            public CombatActionExecutionResult Execute(
                int playerTurnNumber = 0)
            {
                return Execute(Actions, playerTurnNumber);
            }

            public CombatActionExecutionResult Execute(
                IReadOnlyList<CombatAction> actions,
                int playerTurnNumber = 0)
            {
                var executor = new CombatActionExecutor(
                    Boss,
                    party,
                    new DamageContextFactory(
                        new SeededRandomSource(123)),
                    new CombatTriggerResolver(
                        new ICombatTriggerRule[] { Rule }));
                CombatActionExecutionSession session =
                    executor.BeginExecution(actions, playerTurnNumber);
                while (session.TryExecuteNext(out _))
                {
                }

                return session.Result;
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}
