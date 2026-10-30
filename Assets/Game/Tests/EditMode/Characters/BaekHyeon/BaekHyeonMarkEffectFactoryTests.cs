using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Combat.Actions;
using ValorChronicle.Battle.Combat.Attacks;
using ValorChronicle.Battle.Combat.Damage;
using ValorChronicle.Battle.Combat.Effects;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.BaekHyeon;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.BaekHyeon
{
    public sealed class BaekHyeonMarkEffectFactoryTests
    {
        [TestCase(0, 0.03d)]
        [TestCase(3, 0.035d)]
        public void CreateUsesResolvedIndefiniteStackDefinition(
            int awakening,
            double expectedMagnitude)
        {
            using (var scope = new ConfigScope(awakening))
            {
                EffectInstance effect = BaekHyeonMarkEffectFactory.Create(
                    1L,
                    scope.Rules);

                Assert.That(effect.EffectId,
                    Is.EqualTo(BaekHyeonRules.MarkEffectId));
                Assert.That(effect.SourceId,
                    Is.EqualTo(BaekHyeonRules.CharacterId));
                Assert.That(effect.Category, Is.EqualTo(EffectCategory.Debuff));
                Assert.That(effect.ModifierType,
                    Is.EqualTo(EffectModifierType.TargetTakenDamageIncrease));
                Assert.That(effect.Magnitude, Is.EqualTo(expectedMagnitude));
                Assert.That(effect.RemainingTurns, Is.Null);
                Assert.That(effect.StackPolicy,
                    Is.EqualTo(EffectStackPolicy.StackCount));
                Assert.That(effect.StackCount, Is.EqualTo(1));
                Assert.That(effect.MaxStackCount, Is.EqualTo(10));
                Assert.That(effect.AttackTypeMask,
                    Is.EqualTo(AttackTypeMask.Additional | AttackTypeMask.Chase));
            }
        }

        [Test]
        public void ReapplicationUsesOneInstanceAndCapsAtTen()
        {
            using (var scope = new ConfigScope())
            {
                var boss = new BossBattleState(
                    "training_boss",
                    ElementType.Light,
                    100000L,
                    100d);
                for (long id = 1L; id <= 12L; id++)
                {
                    boss.Effects.ApplyEffect(
                        BaekHyeonMarkEffectFactory.Create(id, scope.Rules));
                }

                Assert.That(boss.Effects.FindByEffectId(
                    BaekHyeonRules.MarkEffectId).Count, Is.EqualTo(1));
                Assert.That(BaekHyeonMarkEffectFactory.GetMarkCount(boss),
                    Is.EqualTo(10));
            }
        }

        [TestCase(AttackType.Additional, 0.15d)]
        [TestCase(AttackType.Chase, 0.15d)]
        [TestCase(AttackType.Match, 0d)]
        [TestCase(AttackType.Active, 0d)]
        [TestCase(AttackType.DamageOverTime, 0d)]
        public void ActualDamageExecutionFiltersMarkByAttackType(
            AttackType attackType,
            double expectedIncrease)
        {
            using (var scope = new ConfigScope())
            {
                var character = new CharacterBattleState(
                    BaekHyeonRules.CharacterId,
                    0,
                    ElementType.Grass,
                    1000L,
                    1000d);
                var party = new PartyBattleState(new[] { character });
                var boss = new BossBattleState(
                    "training_boss",
                    ElementType.Light,
                    100000L,
                    100d);
                for (long id = 1L; id <= 5L; id++)
                {
                    boss.Effects.ApplyEffect(
                        BaekHyeonMarkEffectFactory.Create(id, scope.Rules));
                }

                var action = new DamageAction(
                    100L,
                    ActionOrigin.System,
                    new DamageContextBuildRequest(
                        character,
                        party,
                        boss,
                        ElementType.Grass,
                        attackType,
                        AttackTag.None,
                        1d,
                        appliesCombo: false,
                        finalComboCount: 1,
                        canCritical: false));
                var executor = new CombatActionExecutor(
                    boss,
                    party,
                    new DamageContextFactory(new SeededRandomSource(123)));

                DamageActionResult result = executor.Execute(
                        new CombatActionQueue(new CombatAction[] { action }))
                    .ActionResults.OfType<DamageActionResult>().Single();

                Assert.That(result.Context.TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(expectedIncrease).Within(0.000000001d));
            }
        }

        [Test]
        public void MarkStacksWithExistingUnfilteredBossModifier()
        {
            using (var scope = new ConfigScope())
            {
                var character = new CharacterBattleState(
                    BaekHyeonRules.CharacterId,
                    0,
                    ElementType.Grass,
                    1000L,
                    1000d);
                var party = new PartyBattleState(new[] { character });
                var boss = new BossBattleState(
                    "training_boss",
                    ElementType.Light,
                    100000L,
                    100d);
                for (long id = 1L; id <= 5L; id++)
                {
                    boss.Effects.ApplyEffect(
                        BaekHyeonMarkEffectFactory.Create(id, scope.Rules));
                }

                boss.Effects.ApplyEffect(new EffectInstance(
                    100L,
                    "effect_test_unfiltered_taken_damage",
                    "test_source",
                    EffectCategory.Debuff,
                    EffectModifierType.TargetTakenDamageIncrease,
                    0.10d,
                    remainingTurns: null,
                    creationOrder: 100L));
                var action = new DamageAction(
                    101L,
                    ActionOrigin.System,
                    new DamageContextBuildRequest(
                        character,
                        party,
                        boss,
                        ElementType.Grass,
                        AttackType.Additional,
                        AttackTag.None,
                        1d,
                        appliesCombo: false,
                        finalComboCount: 1,
                        canCritical: false));
                var executor = new CombatActionExecutor(
                    boss,
                    party,
                    new DamageContextFactory(new SeededRandomSource(123)));

                DamageActionResult result = executor.Execute(
                        new CombatActionQueue(new CombatAction[] { action }))
                    .ActionResults.OfType<DamageActionResult>().Single();

                Assert.That(result.Context.TargetTakenDamageIncreaseRateSum,
                    Is.EqualTo(0.25d).Within(0.000000001d));
            }
        }

        private sealed class ConfigScope : IDisposable
        {
            private readonly BaekHyeonCombatConfig config;

            public ConfigScope(int awakening = 0)
            {
                config = BaekHyeonTestConfig.Create();
                Rules = BaekHyeonCombatRuleResolver.Resolve(config, awakening);
            }

            public BaekHyeonResolvedCombatRules Rules { get; }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}
