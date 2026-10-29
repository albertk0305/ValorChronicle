using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Application;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.Shields;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Naira;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Relics.Stats;
using ValorChronicle.Tests.EditMode.Battle.Flow;

namespace ValorChronicle.Tests.EditMode.Characters.Naira
{
    public sealed class NairaWindhornActiveActionProviderTests
    {
        [TestCase(0, 0.35d)]
        [TestCase(4, 0.35d)]
        [TestCase(5, 0.40d)]
        [TestCase(6, 0.40d)]
        public void CreatesShieldThenExpectedUniqueGrace(
            int awakening,
            double expectedReduction)
        {
            using (var battle = new TestBattle(awakening: awakening))
            {
                Assert.That(battle.Provider.CooldownTurns, Is.EqualTo(12));
                Assert.That(battle.Actions.Count, Is.EqualTo(2));
                Assert.That(battle.Actions[0], Is.TypeOf<ShieldAction>());
                Assert.That(battle.Actions[1], Is.TypeOf<ApplyEffectAction>());
                Assert.That(battle.Actions.OfType<DamageAction>().Any(),
                    Is.False);
                Assert.That(battle.Actions[0].ActionId, Is.EqualTo(1L));
                Assert.That(battle.Actions[1].ActionId, Is.EqualTo(2L));
                for (int index = 0; index < battle.Actions.Count; index++)
                {
                    CombatAction action = battle.Actions[index];
                    Assert.That(action.Origin, Is.EqualTo(ActionOrigin.Active));
                    Assert.That(action.RootActionId,
                        Is.EqualTo(action.ActionId));
                    Assert.That(action.SourceActionId, Is.Null);
                }

                CombatActionExecutionResult execution = battle.Execute();
                ShieldActionResult shield = ShieldResult(execution);
                ApplyEffectActionResult effect = EffectResult(execution);

                Assert.That(shield.ApplicationResult.GrantedShieldAmount,
                    Is.EqualTo(350L));
                Assert.That(effect.AppliedEffect.EffectId,
                    Is.EqualTo(NairaWindhornRules.WhiteStagGraceEffectId));
                Assert.That(effect.AppliedEffect.ModifierType,
                    Is.EqualTo(
                        EffectModifierType.NextBossHitDamageReduction));
                Assert.That(effect.AppliedEffect.Magnitude,
                    Is.EqualTo(expectedReduction));
                Assert.That(effect.AppliedEffect.StackPolicy,
                    Is.EqualTo(EffectStackPolicy.Unique));
                Assert.That(effect.AppliedEffect.RemainingTurns, Is.Null);
                Assert.That(effect.AppliedEffect.MaxStackCount,
                    Is.EqualTo(1));
                Assert.That(battle.Party.Effects.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void ActiveShieldUsesFinalMaxHpWithoutComboAndLastsThreeTurns()
        {
            using (var battle = new TestBattle(
                sourceMaxHp: 6200L,
                playerTurnNumber: 7))
            {
                CombatActionExecutionResult execution = battle.Execute();
                ShieldActionResult shield = ShieldResult(execution);
                ShieldInstance instance = battle.Party.Shields.ActiveShields[0];

                Assert.That(shield.Context.SourceMaxHp, Is.EqualTo(6200L));
                Assert.That(shield.Context.ShieldCoefficient,
                    Is.EqualTo(0.35d));
                Assert.That(shield.Context.AppliesCombo, Is.False);
                Assert.That(shield.Context.FinalComboCount, Is.Zero);
                Assert.That(shield.GenerationResult.ComboMultiplier,
                    Is.EqualTo(1d));
                Assert.That(shield.GenerationResult.FinalShieldAmount,
                    Is.EqualTo(2170L));
                Assert.That(instance.SourceId,
                    Is.EqualTo(NairaWindhornRules.ActiveAbilityId));
                Assert.That(instance.CreatedTurn, Is.EqualTo(7));
                Assert.That(instance.RemainingTurns, Is.EqualTo(3));
            }
        }

        [Test]
        public void AwakeningAndRelicFinalMaxHpDriveActiveShield()
        {
            CharacterDefinition definition =
                AssetDatabase.LoadAssetAtPath<CharacterDefinition>(
                    "Assets/Data/Characters/naira_windhorn.asset");
            var resolver = new CharacterBuildResolver(
                new ICharacterBuildModifier[]
                {
                    new NairaWindhornBuildModifier()
                });
            ResolvedCharacterBuild awakeningBuild = resolver.Resolve(
                definition,
                level: 100,
                awakening: 5);
            ResolvedCharacterBuild relicBuild = resolver.Resolve(
                definition,
                level: 100,
                awakening: 5,
                new RelicStatBonusSummary(0.10d, 0d));

            Assert.That(awakeningBuild.MaxHp, Is.EqualTo(6510L));
            Assert.That(relicBuild.MaxHp, Is.EqualTo(7161L));
            using (var awakeningBattle = new TestBattle(
                awakening: 5,
                sourceMaxHp: awakeningBuild.MaxHp))
            using (var relicBattle = new TestBattle(
                awakening: 5,
                sourceMaxHp: relicBuild.MaxHp))
            {
                Assert.That(ShieldResult(awakeningBattle.Execute())
                        .GenerationResult.FinalShieldAmount,
                    Is.EqualTo(2278L));
                Assert.That(ShieldResult(relicBattle.Execute())
                        .GenerationResult.FinalShieldAmount,
                    Is.EqualTo(2506L));
            }
        }

        [Test]
        public void ShieldIncreaseModifierAppliesOnceAndCreatesIndependentShield()
        {
            using (var battle = new TestBattle(initialShield: 100L))
            {
                battle.Character.Effects.ApplyEffect(new EffectInstance(
                    9001L,
                    "active_shield_increase",
                    "test_source",
                    EffectCategory.Buff,
                    EffectModifierType.ShieldAmountIncrease,
                    0.20d,
                    remainingTurns: null,
                    creationOrder: 9001L,
                    stackPolicy: EffectStackPolicy.Unique));

                ShieldActionResult shield = ShieldResult(battle.Execute());

                Assert.That(shield.GenerationResult
                        .ShieldAmountIncreaseMultiplier,
                    Is.EqualTo(1.20d));
                Assert.That(shield.GenerationResult.FinalShieldAmount,
                    Is.EqualTo(420L));
                Assert.That(battle.Party.Shields.TotalShield,
                    Is.EqualTo(520L));
                Assert.That(battle.Party.Shields.ActiveShields.Count,
                    Is.EqualTo(2));
                Assert.That(battle.Party.Shields.ActiveShields[0].RuntimeId,
                    Is.Not.EqualTo(
                        battle.Party.Shields.ActiveShields[1].RuntimeId));
            }
        }

        [TestCase(800L, 200L, 1000L)]
        [TestCase(1000L, 0L, 1000L)]
        public void ShieldCapDoesNotPreventGrace(
            long initialShield,
            long expectedGrant,
            long expectedTotal)
        {
            using (var battle = new TestBattle(initialShield: initialShield))
            {
                CombatActionExecutionResult execution = battle.Execute();
                ShieldActionResult shield = ShieldResult(execution);

                Assert.That(shield.ApplicationResult.RequestedShieldAmount,
                    Is.EqualTo(350L));
                Assert.That(shield.ApplicationResult.GrantedShieldAmount,
                    Is.EqualTo(expectedGrant));
                Assert.That(shield.ApplicationResult.TotalShieldAfter,
                    Is.EqualTo(expectedTotal));
                Assert.That(battle.Party.Effects.FindByEffectId(
                        NairaWindhornRules.WhiteStagGraceEffectId).Count,
                    Is.EqualTo(1));
                if (expectedGrant == 0L)
                {
                    Assert.That(shield.ApplicationResult
                        .CreatedShieldRuntimeId, Is.Null);
                }
            }
        }

        [Test]
        public void ReacquiringGraceKeepsSingleExistingEffect()
        {
            using (var battle = new TestBattle())
            {
                CombatActionExecutionResult first = battle.Execute();
                EffectInstance existing = EffectResult(first).AppliedEffect;
                battle.RecreateActions();

                CombatActionExecutionResult second = battle.Execute();

                Assert.That(battle.Party.Effects.Count, Is.EqualTo(1));
                Assert.That(EffectResult(second).AppliedEffect,
                    Is.SameAs(existing));
                Assert.That(existing.Magnitude, Is.EqualTo(0.35d));
                Assert.That(existing.RemainingTurns, Is.Null);
            }
        }

        [TestCase(0, 650L)]
        [TestCase(5, 600L)]
        public void DirectBossHitAppliesAndConsumesGrace(
            int awakening,
            long expectedDamage)
        {
            using (var battle = new TestBattle(
                awakening: awakening,
                sourceMaxHp: 10000L))
            {
                battle.Execute();

                BossDamageActionResult hit = battle.ExecuteHit(
                    BossDamageHitType.Direct);

                Assert.That(hit.DamageResult.FinalDamageBeforeShield,
                    Is.EqualTo(expectedDamage));
                Assert.That(hit.ApplicationResult.ShieldAbsorbedDamage,
                    Is.EqualTo(expectedDamage));
                Assert.That(hit.ApplicationResult.HpDamage, Is.Zero);
                Assert.That(battle.Party.Effects.Count, Is.Zero);
            }
        }

        [TestCase(BossDamageHitType.DamageOverTime)]
        [TestCase(BossDamageHitType.Reflected)]
        [TestCase(BossDamageHitType.SelfInflicted)]
        public void NonDirectDamageDoesNotApplyOrConsumeGrace(
            BossDamageHitType hitType)
        {
            using (var battle = new TestBattle(sourceMaxHp: 10000L))
            {
                battle.Execute();

                BossDamageActionResult hit = battle.ExecuteHit(hitType);

                Assert.That(hit.DamageResult.FinalDamageBeforeShield,
                    Is.EqualTo(1000L));
                Assert.That(battle.Party.Effects.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void NonDamageActionDoesNotConsumeGrace()
        {
            using (var battle = new TestBattle())
            {
                battle.Execute();
                battle.Boss.Resources.Register("marker", 1);

                battle.Executor.Execute(new CombatActionQueue(
                    new CombatAction[]
                    {
                        new AddResourceAction(
                            100L,
                            ActionOrigin.System,
                            battle.Boss,
                            "marker",
                            1)
                    }));

                Assert.That(battle.Party.Effects.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void GraceCombinesWithGeneralReductionAndUsesSeventyPercentCap()
        {
            using (var battle = new TestBattle(
                awakening: 5,
                sourceMaxHp: 10000L))
            {
                battle.Party.Effects.ApplyEffect(GeneralReduction(
                    9000L,
                    0.50d));
                battle.Execute();

                BossDamageActionResult hit = battle.ExecuteHit(
                    BossDamageHitType.Direct);

                Assert.That(hit.Context.PartyDamageReductionRateSum,
                    Is.EqualTo(0.90d));
                Assert.That(hit.DamageResult
                        .EffectivePartyDamageReductionRate,
                    Is.EqualTo(0.70d));
                Assert.That(hit.DamageResult.FinalDamageBeforeShield,
                    Is.EqualTo(300L));
                Assert.That(battle.Party.Effects.Count, Is.EqualTo(1));
                Assert.That(battle.Party.Effects.GetActiveEffects()[0]
                        .ModifierType,
                    Is.EqualTo(EffectModifierType.PartyDamageReduction));
            }
        }

        [Test]
        public void ThreeHitsConsumeGraceThenAncientGuardianThenNone()
        {
            using (var battle = new TestBattle(
                awakening: 5,
                sourceMaxHp: 10000L))
            {
                battle.Party.Effects.ApplyEffect(OneTimeReduction(
                    9000L,
                    "effect_test_ancient_guardian",
                    0.15d));
                battle.Execute();
                var hits = new CombatAction[]
                {
                    battle.Hit(
                        100L,
                        BossDamageHitType.Direct,
                        AttackTag.MultiHit),
                    battle.Hit(
                        101L,
                        BossDamageHitType.Direct,
                        AttackTag.MultiHit),
                    battle.Hit(
                        102L,
                        BossDamageHitType.Direct,
                        AttackTag.MultiHit)
                };

                CombatActionExecutionResult execution = battle.Executor
                    .Execute(new CombatActionQueue(hits));
                long[] damages = execution.ActionResults
                    .Cast<BossDamageActionResult>()
                    .Select(result => result.DamageResult
                        .FinalDamageBeforeShield)
                    .ToArray();

                Assert.That(damages,
                    Is.EqualTo(new long[] { 600L, 850L, 1000L }));
                Assert.That(battle.Party.Effects.Count, Is.Zero);
            }
        }

        [Test]
        public void TerminalBattleSkipsBothActiveActions()
        {
            using (var battle = new TestBattle(bossHp: 1L))
            {
                var lethal = new DamageAction(
                    100L,
                    ActionOrigin.System,
                    new DamageContextBuildRequest(
                        battle.Character,
                        battle.Party,
                        battle.Boss,
                        ElementType.Grass,
                        AttackType.Match,
                        AttackTag.None,
                        skillCoefficient: 1d,
                        appliesCombo: false,
                        finalComboCount: 0,
                        canCritical: false));
                CombatAction[] queue = new[] { lethal }
                    .Concat(battle.Actions)
                    .ToArray();
                CombatActionExecutionResult execution = battle.Executor
                    .Execute(new CombatActionQueue(queue));

                Assert.That(execution.CompletedActionCount, Is.EqualTo(1));
                Assert.That(execution.BossDefeated, Is.True);
                Assert.That(execution.ClearedRemainingActions, Is.True);
                Assert.That(battle.Party.Shields.TotalShield, Is.Zero);
                Assert.That(battle.Party.Effects.Count, Is.Zero);
            }
        }

        [Test]
        public void IntegratedCooldownAllowsUseOnTurnsOneThirteenAndTwentyFive()
        {
            using (var battle = new TestBattle(createActions: false))
            {
                var coordinator = new BattleFlowCoordinator(
                    25,
                    new[] { battle.Provider.CooldownTurns });
                var registry = new ActiveAbilityActionProviderRegistry();
                registry.Register(
                    NairaWindhornRules.CharacterId,
                    NairaWindhornRules.ActiveAbilityId,
                    battle.Provider);
                var activeExecutor = new ActiveAbilityCombatExecutor(
                    coordinator,
                    battle.Party,
                    battle.Boss,
                    battle.Executor,
                    battle.ActionIds,
                    new[]
                    {
                        new ActiveAbilityBinding(
                            0,
                            battle.Character.PartySlotIndex,
                            NairaWindhornRules.CharacterId,
                            NairaWindhornRules.ActiveAbilityId)
                    },
                    registry);
                Assert.That(coordinator.StartBattle(), Is.True);

                for (int turn = 1; turn <= 25; turn++)
                {
                    bool expectedUsable = turn == 1
                        || turn == 13
                        || turn == 25;
                    Assert.That(activeExecutor.CanExecute(0),
                        Is.EqualTo(expectedUsable), $"Turn {turn}");
                    if (expectedUsable)
                    {
                        Assert.That(activeExecutor.TryExecute(0, out _),
                            Is.True, $"Turn {turn}");
                        Assert.That(coordinator.Context.ActiveAbilities[0]
                                .RemainingCooldown,
                            Is.EqualTo(12), $"Turn {turn}");
                        Assert.That(battle.Party.Shields.ActiveShields
                                .Last().CreatedTurn,
                            Is.EqualTo(turn));
                        Assert.That(activeExecutor.TryExecute(0, out _),
                            Is.False, $"Turn {turn} second use");
                    }

                    if (turn < 25)
                    {
                        BattleFlowTestSupport.CompleteTurnWithSingleMatch(
                            coordinator);
                    }
                }
            }
        }

        [Test]
        public void ConstructorAndCreateActionsRejectInvalidInputs()
        {
            Assert.That(
                () => new NairaWindhornActiveActionProvider(null),
                Throws.TypeOf<ArgumentNullException>());
            using (var battle = new TestBattle())
            {
                Assert.That(
                    () => battle.Provider.CreateActions(
                        battle.Character,
                        battle.Party,
                        0,
                        battle.ActionIds),
                    Throws.TypeOf<ArgumentOutOfRangeException>());
            }
        }

        private static ShieldActionResult ShieldResult(
            CombatActionExecutionResult execution)
        {
            return execution.ActionResults
                .OfType<ShieldActionResult>()
                .Single();
        }

        private static ApplyEffectActionResult EffectResult(
            CombatActionExecutionResult execution)
        {
            return execution.ActionResults
                .OfType<ApplyEffectActionResult>()
                .Single();
        }

        private static EffectInstance GeneralReduction(
            long runtimeId,
            double magnitude)
        {
            return new EffectInstance(
                runtimeId,
                $"general_reduction_{runtimeId}",
                "test_source",
                EffectCategory.Buff,
                EffectModifierType.PartyDamageReduction,
                magnitude,
                remainingTurns: null,
                creationOrder: runtimeId,
                stackPolicy: EffectStackPolicy.Unique);
        }

        private static EffectInstance OneTimeReduction(
            long runtimeId,
            string effectId,
            double magnitude)
        {
            return new EffectInstance(
                runtimeId,
                effectId,
                "test_source",
                EffectCategory.Buff,
                EffectModifierType.NextBossHitDamageReduction,
                magnitude,
                remainingTurns: null,
                creationOrder: runtimeId,
                stackPolicy: EffectStackPolicy.Unique);
        }

        private sealed class TestBattle : IDisposable
        {
            private readonly NairaWindhornCombatConfig config;

            public TestBattle(
                int awakening = 0,
                long sourceMaxHp = 1000L,
                long initialShield = 0L,
                long bossHp = 100000L,
                int playerTurnNumber = 4,
                bool createActions = true)
            {
                config = NairaWindhornTestConfig.Create();
                Character = new CharacterBattleState(
                    NairaWindhornRules.CharacterId,
                    0,
                    ElementType.Grass,
                    sourceMaxHp,
                    100d);
                Party = new PartyBattleState(new[] { Character });
                if (initialShield > 0L)
                {
                    Party.Shields.Add(new ShieldInstance(
                        9000L,
                        "existing_shield",
                        initialShield,
                        createdTurn: 1,
                        remainingTurns: 5,
                        creationOrder: 9000L));
                }

                Boss = new BossBattleState(
                    "training_boss",
                    ElementType.Fire,
                    bossHp,
                    1000d);
                Provider = new NairaWindhornActiveActionProvider(
                    config,
                    awakening);
                ActionIds = new CombatActionIdSequence();
                PlayerTurnNumber = playerTurnNumber;
                Executor = new CombatActionExecutor(
                    Boss,
                    Party,
                    new DamageContextFactory(new SeededRandomSource(123)));
                Actions = createActions
                    ? Provider.CreateActions(
                        Character,
                        Party,
                        playerTurnNumber,
                        ActionIds)
                    : Array.Empty<CombatAction>();
            }

            public CharacterBattleState Character { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public NairaWindhornActiveActionProvider Provider { get; }
            public CombatActionIdSequence ActionIds { get; }
            public CombatActionExecutor Executor { get; }
            public int PlayerTurnNumber { get; }
            public IReadOnlyList<CombatAction> Actions { get; private set; }

            public void RecreateActions()
            {
                Actions = Provider.CreateActions(
                    Character,
                    Party,
                    PlayerTurnNumber,
                    ActionIds);
            }

            public CombatActionExecutionResult Execute()
            {
                return Executor.Execute(new CombatActionQueue(Actions));
            }

            public BossDamageActionResult ExecuteHit(
                BossDamageHitType hitType)
            {
                CombatActionExecutionResult execution = Executor.Execute(
                    new CombatActionQueue(new CombatAction[]
                    {
                        Hit(100L, hitType)
                    }));
                return (BossDamageActionResult)execution.ActionResults[0];
            }

            public BossDamageAction Hit(
                long actionId,
                BossDamageHitType hitType,
                AttackTag attackTags = AttackTag.None)
            {
                return new BossDamageAction(
                    actionId,
                    new BossDamageContextBuildRequest(
                        Boss,
                        Party,
                        attackCoefficient: 1d,
                        attackTags,
                        hitType));
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}
