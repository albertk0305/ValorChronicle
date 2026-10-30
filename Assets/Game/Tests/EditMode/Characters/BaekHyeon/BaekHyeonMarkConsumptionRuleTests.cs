using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.BaekHyeon;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.BaekHyeon
{
    public sealed class BaekHyeonMarkConsumptionRuleTests
    {
        [TestCase(AttackType.Additional, 1)]
        [TestCase(AttackType.Additional, 5)]
        [TestCase(AttackType.Additional, 10)]
        [TestCase(AttackType.Chase, 1)]
        [TestCase(AttackType.Chase, 5)]
        [TestCase(AttackType.Chase, 10)]
        public void QualifyingDamageRemovesAllMarkStacks(
            AttackType attackType,
            int markCount)
        {
            using (var battle = new MarkConsumptionBattle())
            {
                battle.AddMarks(markCount);

                CombatActionExecutionResult execution = battle.Execute(
                    new[] { battle.Damage(attackType, 1d) },
                    battle.Rule);

                DamageActionResult damage = execution.ActionResults
                    .OfType<DamageActionResult>().Single();
                RemoveEffectActionResult removal = execution.ActionResults
                    .OfType<RemoveEffectActionResult>().Single();
                Assert.That(damage.AppliedDamage, Is.GreaterThanOrEqualTo(1L));
                Assert.That(damage.Context.TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.03d * markCount).Within(0.000000001d));
                Assert.That(removal.WasRemoved, Is.True);
                Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(battle.Boss),
                    Is.Zero);
            }
        }

        [TestCase(AttackType.Match)]
        [TestCase(AttackType.Active)]
        [TestCase(AttackType.DamageOverTime)]
        public void OtherAttackTypesDoNotConsumeMarks(AttackType attackType)
        {
            using (var battle = new MarkConsumptionBattle())
            {
                battle.AddMarks(5);

                CombatActionExecutionResult execution = battle.Execute(
                    new[] { battle.Damage(attackType, 1d) },
                    battle.Rule);

                Assert.That(execution.ActionResults
                    .OfType<RemoveEffectActionResult>(), Is.Empty);
                Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(battle.Boss),
                    Is.EqualTo(5));
            }
        }

        [Test]
        public void ZeroAppliedDamageDoesNotConsumeMarks()
        {
            using (var battle = new MarkConsumptionBattle())
            {
                battle.AddMarks(5);

                CombatActionExecutionResult execution = battle.Execute(
                    new[] { battle.Damage(AttackType.Additional, 0d) },
                    battle.Rule);

                Assert.That(execution.ActionResults
                    .OfType<DamageActionResult>().Single().AppliedDamage,
                    Is.Zero);
                Assert.That(execution.ActionResults
                    .OfType<RemoveEffectActionResult>(), Is.Empty);
                Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(battle.Boss),
                    Is.EqualTo(5));
            }
        }

        [Test]
        public void OneAppliedDamageConsumesMarks()
        {
            using (var battle = new MarkConsumptionBattle(
                attackerAttack: 1d))
            {
                battle.AddMarks(5);

                CombatActionExecutionResult execution = battle.Execute(
                    new[] { battle.Damage(AttackType.Chase, 1d) },
                    battle.Rule);

                Assert.That(execution.ActionResults
                    .OfType<DamageActionResult>().Single().AppliedDamage,
                    Is.GreaterThanOrEqualTo(1L));
                Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(battle.Boss),
                    Is.Zero);
            }
        }

        [Test]
        public void NoMarksCreatesNoRemovalAction()
        {
            using (var battle = new MarkConsumptionBattle())
            {
                CombatActionExecutionResult execution = battle.Execute(
                    new[] { battle.Damage(AttackType.Additional, 1d) },
                    battle.Rule);

                Assert.That(execution.ActionResults.Count, Is.EqualTo(1));
                Assert.That(execution.ActionResults
                    .OfType<RemoveEffectActionResult>(), Is.Empty);
            }
        }

        [Test]
        public void RemovalPreservesOtherBossEffects()
        {
            using (var battle = new MarkConsumptionBattle())
            {
                battle.AddMarks(5);
                var other = new EffectInstance(
                    8000L,
                    "effect_test_other_boss_debuff",
                    "test_source",
                    EffectCategory.Debuff,
                    EffectModifierType.TargetTakenDamageIncrease,
                    0.10d,
                    remainingTurns: null,
                    creationOrder: 8000L);
                battle.Boss.Effects.ApplyEffect(other);

                battle.Execute(
                    new[] { battle.Damage(AttackType.Additional, 1d) },
                    battle.Rule);

                Assert.That(battle.Boss.Effects.Count, Is.EqualTo(1));
                Assert.That(battle.Boss.Effects.GetActiveEffects().Single(),
                    Is.SameAs(other));
            }
        }

        [Test]
        public void DuplicateRegistrationOfSameRuleRemovesOnlyOnce()
        {
            using (var battle = new MarkConsumptionBattle())
            {
                battle.AddMarks(5);

                CombatActionExecutionResult execution = battle.Execute(
                    new[] { battle.Damage(AttackType.Additional, 1d) },
                    battle.Rule,
                    battle.Rule);

                Assert.That(execution.ActionResults
                    .OfType<RemoveEffectActionResult>().Count(), Is.EqualTo(1));
                Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(battle.Boss),
                    Is.Zero);
            }
        }

        [Test]
        public void ConstructorRejectsInvalidInputs()
        {
            using (var config = new ConfigScope())
            {
                Assert.That(
                    () => new BaekHyeonMarkConsumptionRule(
                        null,
                        0,
                        new CombatActionIdSequence()),
                    Throws.TypeOf<ArgumentNullException>());
                Assert.That(
                    () => new BaekHyeonMarkConsumptionRule(
                        config.Config,
                        0,
                        null),
                    Throws.TypeOf<ArgumentNullException>());
            }
        }

        internal sealed class MarkConsumptionBattle : IDisposable
        {
            private readonly BaekHyeonCombatConfig config;
            private readonly BaekHyeonResolvedCombatRules rules;
            private long nextEffectId = 1000L;

            public MarkConsumptionBattle(
                double attackerAttack = 100d,
                long bossHp = 100000L)
            {
                config = BaekHyeonTestConfig.Create();
                rules = BaekHyeonCombatRuleResolver.Resolve(config, 0);
                Attacker = new CharacterBattleState(
                    "test_follow_up_attacker",
                    0,
                    ElementType.Grass,
                    1000L,
                    attackerAttack);
                BaekHyeon = new CharacterBattleState(
                    BaekHyeonRules.CharacterId,
                    1,
                    ElementType.Grass,
                    1000L,
                    100d);
                Party = new PartyBattleState(new[] { Attacker, BaekHyeon });
                Boss = new BossBattleState(
                    "training_boss",
                    ElementType.Light,
                    bossHp,
                    100d);
                ActionIds = new CombatActionIdSequence();
                Rule = new BaekHyeonMarkConsumptionRule(
                    config,
                    0,
                    ActionIds);
            }

            public CharacterBattleState Attacker { get; }
            public CharacterBattleState BaekHyeon { get; }
            public PartyBattleState Party { get; }
            public BossBattleState Boss { get; }
            public CombatActionIdSequence ActionIds { get; }
            public BaekHyeonMarkConsumptionRule Rule { get; }

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

            public DamageAction Damage(
                AttackType attackType,
                double coefficient,
                CharacterBattleState attacker = null)
            {
                ActionOrigin origin;
                switch (attackType)
                {
                    case AttackType.Match:
                        origin = ActionOrigin.Match;
                        break;
                    case AttackType.Active:
                        origin = ActionOrigin.Active;
                        break;
                    case AttackType.Additional:
                        origin = ActionOrigin.Additional;
                        break;
                    case AttackType.Chase:
                        origin = ActionOrigin.Chase;
                        break;
                    case AttackType.DamageOverTime:
                        origin = ActionOrigin.DamageOverTime;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(attackType));
                }

                long actionId = ActionIds.Next();
                bool isFollowUp = attackType == AttackType.Additional
                    || attackType == AttackType.Chase;
                return new DamageAction(
                    actionId,
                    origin,
                    new DamageContextBuildRequest(
                        attacker ?? Attacker,
                        Party,
                        Boss,
                        ElementType.Grass,
                        attackType,
                        AttackTag.None,
                        coefficient,
                        appliesCombo: false,
                        finalComboCount: 0,
                        canCritical: false),
                    isFollowUp ? 9000L : (long?)null,
                    isFollowUp ? 9000L : (long?)null);
            }

            public CombatActionExecutionResult Execute(
                IReadOnlyList<CombatAction> actions,
                params ICombatTriggerRule[] triggerRules)
            {
                var executor = new CombatActionExecutor(
                    Boss,
                    Party,
                    new DamageContextFactory(new SeededRandomSource(123)),
                    new CombatTriggerResolver(triggerRules));
                return executor.Execute(new CombatActionQueue(actions));
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        private sealed class ConfigScope : IDisposable
        {
            public ConfigScope()
            {
                Config = BaekHyeonTestConfig.Create();
            }

            public BaekHyeonCombatConfig Config { get; }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(Config);
            }
        }
    }
}
