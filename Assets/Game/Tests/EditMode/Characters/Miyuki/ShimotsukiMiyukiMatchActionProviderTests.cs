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
using ValorChronicle.Characters.Miyuki;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Miyuki
{
    public sealed class ShimotsukiMiyukiMatchActionProviderTests
    {
        [TestCase(BoardMatchTier.Three, 0.55d, 1, AttackTag.Match3, 2)]
        [TestCase(BoardMatchTier.Four, 0.90d, 2, AttackTag.Match4, 2)]
        [TestCase(BoardMatchTier.FiveOrMore, 1.45d, 3,
            AttackTag.Match5Plus, 3)]
        public void MatchCreatesOrderedDamageResourceAndOptionalBuff(
            BoardMatchTier tier,
            double coefficient,
            int waterAmount,
            AttackTag tag,
            int actionCount)
        {
            using (var battle = new TestBattle())
            {
                IReadOnlyList<CombatAction> actions = battle.Create(tier);

                Assert.That(actions, Has.Count.EqualTo(actionCount));
                DamageAction damage = actions[0] as DamageAction;
                AddResourceAction add = actions[1] as AddResourceAction;
                Assert.That(damage, Is.Not.Null);
                Assert.That(add, Is.Not.Null);
                Assert.That(damage.ContextRequest.AttackElement,
                    Is.EqualTo(ElementType.Water));
                Assert.That(damage.ContextRequest.AttackType,
                    Is.EqualTo(AttackType.Match));
                Assert.That(damage.ContextRequest.AttackTags,
                    Is.EqualTo(tag));
                Assert.That(damage.ContextRequest.SkillCoefficient,
                    Is.EqualTo(coefficient));
                Assert.That(damage.ContextRequest.AppliesCombo, Is.True);
                Assert.That(add.Amount, Is.EqualTo(waterAmount));
                Assert.That(add.ResourceId,
                    Is.EqualTo(WaterElementResource.Id));
                AssertLineage(add, damage.ActionId);
                if (tier == BoardMatchTier.FiveOrMore)
                {
                    ApplyEffectAction effect =
                        actions[2] as ApplyEffectAction;
                    Assert.That(effect, Is.Not.Null);
                    AssertLineage(effect, damage.ActionId);
                    Assert.That(effect.TargetParty,
                        Is.SameAs(battle.Party));
                    Assert.That(effect.Effect.EffectId,
                        Is.EqualTo(ShimotsukiMiyukiRules.Match5EffectId));
                    Assert.That(effect.Effect.Magnitude, Is.EqualTo(0.12d));
                    Assert.That(effect.Effect.RemainingTurns, Is.EqualTo(2));
                    Assert.That(effect.Effect.StackPolicy,
                        Is.EqualTo(EffectStackPolicy.RefreshDuration));
                    Assert.That(effect.Effect.ElementFilter,
                        Is.EqualTo(ElementType.Water));
                }
            }
        }

        [Test]
        public void WaterResourceClampsAtSharedMaximum()
        {
            using (var battle = new TestBattle(initialWater: 4))
            {
                CombatActionExecutionResult result = battle.Execute(
                    BoardMatchTier.FiveOrMore);

                Assert.That(battle.Water.CurrentAmount, Is.EqualTo(5));
                AddResourceActionResult add = result.ActionResults
                    .OfType<AddResourceActionResult>().Single();
                Assert.That(add.AddResult.AddedAmount, Is.EqualTo(1));
            }
        }

        [Test]
        public void FiveMatchDamageDoesNotReceiveItsNewBuff()
        {
            using (var battle = new TestBattle(withPassive: true))
            {
                CombatActionExecutionResult result = battle.Execute(
                    BoardMatchTier.FiveOrMore);
                DamageActionResult damage = Damage(result);

                Assert.That(damage.Context.ElementDamageIncreaseRateSum,
                    Is.EqualTo(0.08d));
                Assert.That(battle.Party.Effects.FindByEffectId(
                        ShimotsukiMiyukiRules.Match5EffectId),
                    Has.Count.EqualTo(1));
            }
        }

        [Test]
        public void LaterWaterAttackReceivesPassiveAndMatch5Buff()
        {
            using (var battle = new TestBattle(withPassive: true))
            {
                battle.Execute(BoardMatchTier.FiveOrMore);

                DamageActionResult later = Damage(
                    battle.Execute(BoardMatchTier.Three));

                Assert.That(later.Context.ElementDamageIncreaseRateSum,
                    Is.EqualTo(0.20d).Within(0.000000001d));
            }
        }

        [Test]
        public void Match5BuffRefreshesOneInstanceToTwoTurns()
        {
            using (var battle = new TestBattle())
            {
                battle.Execute(BoardMatchTier.FiveOrMore);
                EffectInstance first = battle.Party.Effects.FindByEffectId(
                    ShimotsukiMiyukiRules.Match5EffectId).Single();
                battle.Party.Effects.ProcessTurnEnd();
                Assert.That(first.RemainingTurns, Is.EqualTo(1));

                battle.Execute(BoardMatchTier.FiveOrMore);
                IReadOnlyList<EffectInstance> effects =
                    battle.Party.Effects.FindByEffectId(
                        ShimotsukiMiyukiRules.Match5EffectId);

                Assert.That(effects, Has.Count.EqualTo(1));
                Assert.That(effects[0], Is.SameAs(first));
                Assert.That(first.RemainingTurns, Is.EqualTo(2));
            }
        }

        [Test]
        public void Match5BuffExpiresAfterTwoTurnEnds()
        {
            using (var battle = new TestBattle())
            {
                battle.Execute(BoardMatchTier.FiveOrMore);

                battle.Party.Effects.ProcessTurnEnd();
                Assert.That(battle.Party.Effects.FindByEffectId(
                    ShimotsukiMiyukiRules.Match5EffectId), Has.Count.EqualTo(1));
                battle.Party.Effects.ProcessTurnEnd();
                Assert.That(battle.Party.Effects.FindByEffectId(
                    ShimotsukiMiyukiRules.Match5EffectId), Is.Empty);
            }
        }

        [TestCase(3, 0.55d)]
        [TestCase(4, 0.60d)]
        public void AwakeningFourChangesMatch3Coefficient(
            int awakening,
            double expected)
        {
            using (var battle = new TestBattle(awakening: awakening))
            {
                DamageAction damage = (DamageAction)battle.Create(
                    BoardMatchTier.Three)[0];

                Assert.That(damage.ContextRequest.SkillCoefficient,
                    Is.EqualTo(expected));
            }
        }

        [Test]
        public void AwakeningFourKeepsMatch5CoefficientAndAttachment()
        {
            using (var battle = new TestBattle(awakening: 4))
            {
                IReadOnlyList<CombatAction> actions = battle.Create(
                    BoardMatchTier.FiveOrMore);

                Assert.That(((DamageAction)actions[0]).ContextRequest
                    .SkillCoefficient, Is.EqualTo(1.45d));
                Assert.That(((AddResourceAction)actions[1]).Amount,
                    Is.EqualTo(3));
            }
        }

        [Test]
        public void ActionIdsAndLineageAreDeterministic()
        {
            using (var battle = new TestBattle())
            {
                IReadOnlyList<CombatAction> actions = battle.Create(
                    BoardMatchTier.FiveOrMore);

                Assert.That(actions.Select(action => action.ActionId),
                    Is.EqualTo(new long[] { 1, 2, 3 }));
                Assert.That(actions[0].RootActionId, Is.EqualTo(1));
                Assert.That(actions[0].SourceActionId, Is.Null);
                AssertLineage(actions[1], 1);
                AssertLineage(actions[2], 1);
            }
        }

        [TestCase(BoardMatchTier.Three, 2)]
        [TestCase(BoardMatchTier.Four, 3)]
        [TestCase(BoardMatchTier.FiveOrMore, 4)]
        public void FrostEchoEnhancesAttachmentAndConsumesAfterAddExecutes(
            BoardMatchTier tier,
            int expectedAmount)
        {
            using (var battle = new TestBattle(
                awakening: 6,
                withFrostEcho: true))
            {
                IReadOnlyList<CombatAction> actions = battle.Create(tier);
                Assert.That(((AddResourceAction)actions[1]).Amount,
                    Is.EqualTo(expectedAmount));
                Assert.That(battle.FrostEchoState.HasFrostEcho, Is.True);

                battle.Execute(actions);

                Assert.That(battle.FrostEchoState.HasFrostEcho, Is.False);
            }
        }

        [Test]
        public void FrostEchoConsumesEvenWhenWaterIsAlreadyCapped()
        {
            using (var battle = new TestBattle(
                initialWater: 5,
                awakening: 6,
                withFrostEcho: true))
            {
                CombatActionExecutionResult result = battle.Execute(
                    BoardMatchTier.Three);

                Assert.That(result.ActionResults
                    .OfType<AddResourceActionResult>().Single()
                    .AddResult.AddedAmount, Is.Zero);
                Assert.That(battle.Water.CurrentAmount, Is.EqualTo(5));
                Assert.That(battle.FrostEchoState.HasFrostEcho, Is.False);
            }
        }

        [Test]
        public void LethalDamageBeforeAttachmentPreservesFrostEcho()
        {
            using (var battle = new TestBattle(
                awakening: 6,
                withFrostEcho: true,
                bossHp: 1))
            {
                CombatActionExecutionResult result = battle.Execute(
                    BoardMatchTier.Three);

                Assert.That(result.StoppedEarly, Is.True);
                Assert.That(result.ActionResults, Has.Count.EqualTo(1));
                Assert.That(battle.FrostEchoState.HasFrostEcho, Is.True);
            }
        }

        [Test]
        public void AnotherWaterCharactersMatchDoesNotConsumeFrostEcho()
        {
            using (var battle = new TestBattle(
                awakening: 6,
                withFrostEcho: true,
                includeOtherWaterCharacter: true))
            {
                CombatActionExecutionResult result =
                    battle.ExecuteOtherWaterMatch();

                Assert.That(result.ActionResults, Has.Count.EqualTo(2));
                Assert.That(battle.FrostEchoState.HasFrostEcho, Is.True);
            }
        }

        [Test]
        public void SecondMatchAfterConsumptionUsesBaseAttachment()
        {
            using (var battle = new TestBattle(
                awakening: 6,
                withFrostEcho: true))
            {
                battle.Execute(BoardMatchTier.Three);

                IReadOnlyList<CombatAction> second = battle.Create(
                    BoardMatchTier.Three);

                Assert.That(((AddResourceAction)second[1]).Amount,
                    Is.EqualTo(1));
            }
        }

        private static void AssertLineage(CombatAction action, long root)
        {
            Assert.That(action.RootActionId, Is.EqualTo(root));
            Assert.That(action.SourceActionId, Is.EqualTo(root));
        }

        private static DamageActionResult Damage(
            CombatActionExecutionResult result)
        {
            return result.ActionResults.OfType<DamageActionResult>().Single();
        }

        private sealed class TestBattle : IDisposable
        {
            private readonly ShimotsukiMiyukiCombatConfig config;
            private readonly ShimotsukiMiyukiMatchActionProvider provider;
            private readonly CombatActionIdSequence ids =
                new CombatActionIdSequence();
            private readonly CombatActionExecutor executor;

            public TestBattle(
                int initialWater = 0,
                int awakening = 0,
                bool withPassive = false,
                bool withFrostEcho = false,
                long bossHp = 1000000000L,
                bool includeOtherWaterCharacter = false)
            {
                config = ShimotsukiMiyukiTestConfig.Create();
                Character = new CharacterBattleState(
                    ShimotsukiMiyukiRules.CharacterId,
                    0,
                    ElementType.Water,
                    1000,
                    1000d);
                OtherWaterCharacter = includeOtherWaterCharacter
                    ? new CharacterBattleState(
                        "other_water_character",
                        1,
                        ElementType.Water,
                        1000,
                        1000d)
                    : null;
                Party = new PartyBattleState(
                    OtherWaterCharacter == null
                        ? new[] { Character }
                        : new[] { Character, OtherWaterCharacter });
                Boss = new BossBattleState(
                    "miyuki_test_boss",
                    ElementType.Water,
                    bossHp,
                    100d);
                Water = WaterElementResource.Register(
                    Boss.Resources,
                    WaterElementResource.MaxAmount);
                if (initialWater > 0)
                {
                    Water.Add(initialWater);
                }

                if (withPassive)
                {
                    Party.Effects.ApplyEffect(new EffectInstance(
                        1000,
                        ShimotsukiMiyukiRules.PassiveEffectId,
                        ShimotsukiMiyukiRules.CharacterId,
                        EffectCategory.Buff,
                        EffectModifierType.ElementDamageIncrease,
                        0.08d,
                        null,
                        1000,
                        EffectStackPolicy.Unique,
                        elementFilter: ElementType.Water));
                }

                FrostEchoState = new ShimotsukiMiyukiBattleState(
                    Character,
                    awakening >= 6);
                if (withFrostEcho)
                {
                    Assert.That(FrostEchoState.TryGrantFrostEcho(), Is.True);
                }

                provider = new ShimotsukiMiyukiMatchActionProvider(
                    config,
                    awakening,
                    FrostEchoState);
                executor = new CombatActionExecutor(
                    Boss,
                    Party,
                    new DamageContextFactory(new SeededRandomSource(123)),
                    new CombatTriggerResolver(new ICombatTriggerRule[]
                    {
                        new ShimotsukiMiyukiFrostEchoConsumptionRule(
                            FrostEchoState,
                            config,
                            awakening)
                    }));
            }

            public CharacterBattleState Character { get; }
            public CharacterBattleState OtherWaterCharacter { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public ResourceState Water { get; }
            public ShimotsukiMiyukiBattleState FrostEchoState { get; }

            public IReadOnlyList<CombatAction> Create(BoardMatchTier tier)
            {
                return provider.CreateActions(
                    Character,
                    Party,
                    Boss,
                    tier,
                    finalComboCount: 1,
                    ids);
            }

            public CombatActionExecutionResult Execute(BoardMatchTier tier)
            {
                return Execute(Create(tier));
            }

            public CombatActionExecutionResult Execute(
                IReadOnlyList<CombatAction> actions)
            {
                return executor.Execute(new CombatActionQueue(actions));
            }

            public CombatActionExecutionResult ExecuteOtherWaterMatch()
            {
                if (OtherWaterCharacter == null)
                {
                    throw new InvalidOperationException(
                        "The test battle has no other Water character.");
                }

                long damageActionId = ids.Next();
                return Execute(new CombatAction[]
                {
                    new DamageAction(
                        damageActionId,
                        ActionOrigin.Match,
                        new DamageContextBuildRequest(
                            OtherWaterCharacter,
                            Party,
                            Boss,
                            ElementType.Water,
                            AttackType.Match,
                            AttackTag.Match3,
                            0.5d,
                            appliesCombo: true,
                            finalComboCount: 1,
                            canCritical: false)),
                    new AddResourceAction(
                        ids.Next(),
                        ActionOrigin.System,
                        Boss,
                        WaterElementResource.Id,
                        2,
                        damageActionId,
                        damageActionId)
                });
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}
