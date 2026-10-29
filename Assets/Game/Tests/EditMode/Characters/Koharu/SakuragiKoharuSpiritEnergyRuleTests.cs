using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ValorChronicle.Battle.Board;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Flow;
using ValorChronicle.Characters.Koharu;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Koharu
{
    public sealed class SakuragiKoharuSpiritEnergyRuleTests
    {
        [TestCase(BoardMatchTier.Three, 1L)]
        [TestCase(BoardMatchTier.Four, 2L)]
        [TestCase(BoardMatchTier.FiveOrMore, 3L)]
        public void GrantsSpiritOnlyAfterCompletedMatchDamage(
            BoardMatchTier tier,
            long expectedEnergy)
        {
            using (var battle = new TestBattle())
            {
                IReadOnlyList<CombatAction> actions =
                    battle.CreateMatch(tier);

                Assert.That(battle.State.SpiritEnergy, Is.Zero);
                battle.Execute(actions);

                Assert.That(battle.State.SpiritEnergy,
                    Is.EqualTo(expectedEnergy));
            }
        }

        [Test]
        public void DuplicateCompletionDoesNotGrantSpiritTwice()
        {
            using (var battle = new TestBattle())
            {
                CombatActionExecutionResult result = battle.Execute(
                    battle.CreateMatch(BoardMatchTier.Four));
                var history = new CombatActionExecutionHistory(
                    result.ActionResults);
                var context = new CombatActionTriggerContext(
                    result.ActionResults[0],
                    battle.Boss,
                    battle.Party,
                    history);

                battle.Rule.CreateDerivedActions(context);

                Assert.That(battle.State.SpiritEnergy, Is.EqualTo(2));
            }
        }

        [TestCase(ActionOrigin.Additional, AttackType.Additional)]
        [TestCase(ActionOrigin.Chase, AttackType.Chase)]
        [TestCase(ActionOrigin.Active, AttackType.Active)]
        [TestCase(ActionOrigin.DamageOverTime,
            AttackType.DamageOverTime)]
        public void IgnoresNonMatchDamage(
            ActionOrigin origin,
            AttackType attackType)
        {
            using (var battle = new TestBattle())
            {
                DamageAction action = battle.CreateDamage(
                    battle.Koharu,
                    origin,
                    attackType,
                    AttackTag.Match5Plus,
                    derived: true);

                battle.Execute(new CombatAction[] { action });

                Assert.That(battle.State.SpiritEnergy, Is.Zero);
            }
        }

        [Test]
        public void IgnoresOtherCharacterAndInvalidTierTags()
        {
            using (var battle = new TestBattle(includeAlly: true))
            {
                DamageAction allyMatch = battle.CreateDamage(
                    battle.Ally,
                    ActionOrigin.Match,
                    AttackType.Match,
                    AttackTag.Match3);
                DamageAction invalidTag = battle.CreateDamage(
                    battle.Koharu,
                    ActionOrigin.Match,
                    AttackType.Match,
                    AttackTag.Match3 | AttackTag.Match4);

                battle.Execute(new CombatAction[]
                {
                    allyMatch,
                    invalidTag
                });

                Assert.That(battle.State.SpiritEnergy, Is.Zero);
            }
        }

        [Test]
        public void CompletedZeroDamageStillGrantsSpiritLikeCommonCompletionRules()
        {
            using (var battle = new TestBattle())
            {
                DamageAction zeroDamage = battle.CreateDamage(
                    battle.Koharu,
                    ActionOrigin.Match,
                    AttackType.Match,
                    AttackTag.Match3,
                    coefficient: 0d);

                CombatActionExecutionResult result = battle.Execute(
                    new CombatAction[] { zeroDamage });

                Assert.That(result.ActionResults
                        .OfType<DamageActionResult>()
                        .Single()
                        .AppliedDamage,
                    Is.Zero);
                Assert.That(battle.State.SpiritEnergy, Is.EqualTo(1));
            }
        }

        [Test]
        public void WrongAttackElementDoesNotGrantSpirit()
        {
            using (var battle = new TestBattle())
            {
                DamageAction fireDamage = battle.CreateDamage(
                    battle.Koharu,
                    ActionOrigin.Match,
                    AttackType.Match,
                    AttackTag.Match3,
                    attackElement: ElementType.Fire);

                battle.Execute(new CombatAction[] { fireDamage });

                Assert.That(battle.State.SpiritEnergy, Is.Zero);
            }
        }

        [Test]
        public void SeparateCascadeMatchActionsEachGrantSpirit()
        {
            using (var battle = new TestBattle())
            {
                var actions = new List<CombatAction>();
                actions.AddRange(battle.CreateMatch(BoardMatchTier.Three));
                actions.AddRange(battle.CreateMatch(BoardMatchTier.Four));

                battle.Execute(actions);

                Assert.That(battle.State.SpiritEnergy, Is.EqualTo(3));
            }
        }

        [TestCase(0, 3L, 4, 0L, 3)]
        [TestCase(5, 2L, 4, 0L, 3)]
        [TestCase(6, 2L, 4, 0L, 3)]
        [TestCase(0, 10L, 4, 1L, 2)]
        [TestCase(5, 8L, 4, 1L, 2)]
        public void GainImmediatelyAppliesAwakeningCostAndKeepsRemainder(
            int awakening,
            long initialEnergy,
            int initialCooldown,
            long expectedEnergy,
            int expectedCooldown)
        {
            using (var battle = new TestBattle(
                awakening,
                useActive: true))
            {
                Assert.That(battle.RuntimeState.RemainingCooldown,
                    Is.EqualTo(initialCooldown));
                if (initialEnergy > 0)
                {
                    battle.State.GainSpiritEnergy(initialEnergy);
                }

                battle.Execute(battle.CreateMatch(BoardMatchTier.FiveOrMore));

                Assert.That(battle.State.SpiritEnergy,
                    Is.EqualTo(expectedEnergy));
                Assert.That(battle.RuntimeState.RemainingCooldown,
                    Is.EqualTo(expectedCooldown));
                Assert.That(battle.RuntimeState.UsedThisTurn, Is.True);
                Assert.That(battle.RuntimeState.CanUse, Is.False);
            }
        }

        [Test]
        public void ReadyCooldownAccumulatesSpiritWithoutConsumption()
        {
            using (var battle = new TestBattle())
            {
                battle.State.GainSpiritEnergy(5);

                battle.Execute(battle.CreateMatch(BoardMatchTier.FiveOrMore));

                Assert.That(battle.State.SpiritEnergy, Is.EqualTo(8));
                Assert.That(battle.RuntimeState.RemainingCooldown, Is.Zero);
            }
        }

        [Test]
        public void UnboundHandleThrowsBeforeMutatingSpirit()
        {
            using (var battle = new TestBattle(bindHandle: false))
            {
                Assert.Throws<InvalidOperationException>(() =>
                    battle.Execute(
                        battle.CreateMatch(BoardMatchTier.Three)));
                Assert.That(battle.State.SpiritEnergy, Is.Zero);
            }
        }

        [Test]
        public void WrongCharacterOrSlotHandleIsRejected()
        {
            using (var config = new ConfigScope())
            {
                CharacterBattleState owner = CreateKoharu(slot: 2);
                var state = new SakuragiKoharuBattleState(owner);

                Assert.Throws<ArgumentException>(() =>
                    new SakuragiKoharuSpiritEnergyRule(
                        state,
                        config.Value,
                        0,
                        new ActiveAbilityCooldownReductionHandle(
                            "other_character",
                            2)));
                Assert.Throws<ArgumentException>(() =>
                    new SakuragiKoharuSpiritEnergyRule(
                        state,
                        config.Value,
                        0,
                        new ActiveAbilityCooldownReductionHandle(
                            SakuragiKoharuRules.CharacterId,
                            3)));
                Assert.That(state.SpiritEnergy, Is.Zero);
            }
        }

        [Test]
        public void DamageThenSpiritAndCooldownThenBuffOrderIsPreserved()
        {
            using (var battle = new TestBattle(useActive: true))
            {
                battle.State.GainSpiritEnergy(3);
                long energyObservedByLaterRule = -1;
                int cooldownObservedByLaterRule = -1;
                var observer = new DelegateRule(context =>
                {
                    if (context.CompletedResult is DamageActionResult)
                    {
                        energyObservedByLaterRule =
                            battle.State.SpiritEnergy;
                        cooldownObservedByLaterRule =
                            battle.RuntimeState.RemainingCooldown;
                    }

                    return Array.Empty<CombatAction>();
                });

                CombatActionExecutionResult result = battle.Execute(
                    battle.CreateMatch(BoardMatchTier.FiveOrMore),
                    observer);

                Assert.That(energyObservedByLaterRule, Is.Zero);
                Assert.That(cooldownObservedByLaterRule, Is.EqualTo(3));
                Assert.That(result.ActionResults[0],
                    Is.TypeOf<DamageActionResult>());
                Assert.That(result.ActionResults[1],
                    Is.TypeOf<ApplyEffectActionResult>());
                Assert.That(battle.Party.Effects.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void LethalMatchUsesTerminalPolicyAndSkipsSpiritAndBuff()
        {
            using (var battle = new TestBattle(
                bossHp: 1000L,
                useActive: true))
            {
                CombatActionExecutionResult result = battle.Execute(
                    battle.CreateMatch(BoardMatchTier.FiveOrMore));

                Assert.That(result.BossDefeated, Is.True);
                Assert.That(result.ClearedRemainingActions, Is.True);
                Assert.That(result.CompletedActionCount, Is.EqualTo(1));
                Assert.That(battle.State.SpiritEnergy, Is.Zero);
                Assert.That(battle.RuntimeState.RemainingCooldown,
                    Is.EqualTo(4));
                Assert.That(battle.Party.Effects.Count, Is.Zero);
            }
        }

        [Test]
        public void SeparateBattleRulesKeepStateIndependent()
        {
            using (var first = new TestBattle())
            using (var second = new TestBattle())
            {
                first.Execute(first.CreateMatch(BoardMatchTier.FiveOrMore));

                Assert.That(first.State.SpiritEnergy, Is.EqualTo(3));
                Assert.That(second.State.SpiritEnergy, Is.Zero);
            }
        }

        private static CharacterBattleState CreateKoharu(int slot)
        {
            return new CharacterBattleState(
                SakuragiKoharuRules.CharacterId,
                slot,
                ElementType.Grass,
                1100L,
                1000d);
        }

        private sealed class TestBattle : IDisposable
        {
            private readonly SakuragiKoharuCombatConfig config;

            public TestBattle(
                int awakening = 0,
                bool useActive = false,
                bool bindHandle = true,
                bool includeAlly = false,
                long bossHp = 1000000L)
            {
                config = SakuragiKoharuTestConfig.Create();
                Koharu = CreateKoharu(slot: 2);
                Ally = new CharacterBattleState(
                    "character_test_ally",
                    4,
                    ElementType.Grass,
                    1000L,
                    1000d);
                Party = new PartyBattleState(includeAlly
                    ? new[] { Koharu, Ally }
                    : new[] { Koharu });
                Boss = new BossBattleState(
                    "training_boss",
                    ElementType.Water,
                    bossHp,
                    100d);
                ActionIds = new CombatActionIdSequence();
                Provider = new SakuragiKoharuMatchActionProvider(
                    config,
                    awakening);
                State = new SakuragiKoharuBattleState(Koharu);
                Coordinator = new BattleFlowCoordinator(
                    25,
                    new[] { config.ActiveCooldownTurns });
                Coordinator.StartBattle();
                if (useActive)
                {
                    Assert.That(
                        Coordinator.TryUseActiveAbility(0),
                        Is.True);
                }

                RuntimeState = Coordinator.Context.ActiveAbilities[0];
                Handle = new ActiveAbilityCooldownReductionHandle(
                    Koharu.CharacterId,
                    Koharu.PartySlotIndex);
                if (bindHandle)
                {
                    Bind(Handle, RuntimeState);
                }

                Rule = new SakuragiKoharuSpiritEnergyRule(
                    State,
                    config,
                    awakening,
                    Handle);
            }

            public CharacterBattleState Koharu { get; }
            public CharacterBattleState Ally { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public SakuragiKoharuMatchActionProvider Provider { get; }
            public SakuragiKoharuBattleState State { get; }
            public BattleFlowCoordinator Coordinator { get; }
            public ActiveAbilityRuntimeState RuntimeState { get; }
            public ActiveAbilityCooldownReductionHandle Handle { get; }
            public SakuragiKoharuSpiritEnergyRule Rule { get; }

            public IReadOnlyList<CombatAction> CreateMatch(
                BoardMatchTier tier)
            {
                return Provider.CreateActions(
                    Koharu,
                    Party,
                    Boss,
                    tier,
                    1,
                    ActionIds);
            }

            public DamageAction CreateDamage(
                CharacterBattleState attacker,
                ActionOrigin origin,
                AttackType attackType,
                AttackTag tags,
                bool derived = false,
                double coefficient = 0.10d,
                ElementType attackElement = ElementType.Grass)
            {
                long actionId = ActionIds.Next();
                long? root = derived ? 900L : (long?)null;
                long? source = derived ? 899L : (long?)null;
                return new DamageAction(
                    actionId,
                    origin,
                    new DamageContextBuildRequest(
                        attacker,
                        Party,
                        Boss,
                        attackElement,
                        attackType,
                        tags,
                        coefficient,
                        appliesCombo: false,
                        finalComboCount: 0,
                        canCritical: false),
                    root,
                    source);
            }

            public CombatActionExecutionResult Execute(
                IReadOnlyList<CombatAction> actions,
                params ICombatTriggerRule[] laterRules)
            {
                var rules = new ICombatTriggerRule[laterRules.Length + 1];
                rules[0] = Rule;
                for (int index = 0; index < laterRules.Length; index++)
                {
                    rules[index + 1] = laterRules[index];
                }

                var executor = new CombatActionExecutor(
                    Boss,
                    Party,
                    new DamageContextFactory(
                        new SeededRandomSource(123)),
                    new CombatTriggerResolver(rules));
                return executor.Execute(new CombatActionQueue(actions));
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(config);
            }

            private static void Bind(
                ActiveAbilityCooldownReductionHandle handle,
                ActiveAbilityRuntimeState runtimeState)
            {
                MethodInfo bindMethod = typeof(
                        ActiveAbilityCooldownReductionHandle)
                    .GetMethod(
                        "Bind",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(bindMethod, Is.Not.Null);
                bindMethod.Invoke(handle, new object[] { runtimeState });
            }
        }

        private sealed class ConfigScope : IDisposable
        {
            public ConfigScope()
            {
                Value = SakuragiKoharuTestConfig.Create();
            }

            public SakuragiKoharuCombatConfig Value { get; }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(Value);
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
