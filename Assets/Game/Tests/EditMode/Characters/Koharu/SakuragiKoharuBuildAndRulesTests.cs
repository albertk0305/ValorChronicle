using System;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Koharu;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Koharu
{
    public sealed class SakuragiKoharuBuildAndRulesTests
    {
        private SakuragiKoharuCombatConfig config;
        private CharacterDefinition definition;
        private CharacterBuildResolver buildResolver;

        [SetUp]
        public void SetUp()
        {
            config = SakuragiKoharuTestConfig.Create();
            definition = SakuragiKoharuTestConfig.Definition(config);
            buildResolver = new CharacterBuildResolver(
                new ICharacterBuildModifier[]
                {
                    new SakuragiKoharuBuildModifier()
                });
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(definition);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void DefinitionUsesConfirmedBaseStatsAndGrassElement()
        {
            Assert.That(definition.Level1Hp, Is.EqualTo(1100));
            Assert.That(definition.Level1Attack, Is.EqualTo(120));
            Assert.That(definition.Level100Hp, Is.EqualTo(4300));
            Assert.That(definition.Level100Attack, Is.EqualTo(650));
            Assert.That(definition.Element, Is.EqualTo(ElementType.Grass));
        }

        [TestCase(1, 0, 1100L, 120L)]
        [TestCase(100, 0, 4300L, 650L)]
        [TestCase(100, 1, 4515L, 650L)]
        [TestCase(100, 2, 4515L, 683L)]
        [TestCase(100, 6, 4515L, 683L)]
        public void BuildUsesConfirmedStatsAndAwakeningMultipliers(
            int level,
            int awakening,
            long expectedHp,
            long expectedAttack)
        {
            ResolvedCharacterBuild result = buildResolver.Resolve(
                definition,
                level,
                awakening);

            Assert.That(result.MaxHp, Is.EqualTo(expectedHp));
            Assert.That(result.Attack, Is.EqualTo(expectedAttack));
        }

        [TestCase(0, 0.50d, 0.80d, 1.30d, 0.12d, 6, false)]
        [TestCase(1, 0.50d, 0.80d, 1.30d, 0.12d, 6, false)]
        [TestCase(2, 0.50d, 0.80d, 1.30d, 0.12d, 6, false)]
        [TestCase(3, 0.55d, 0.90d, 1.30d, 0.12d, 6, false)]
        [TestCase(4, 0.55d, 0.90d, 1.45d, 0.15d, 6, false)]
        [TestCase(5, 0.55d, 0.90d, 1.45d, 0.15d, 5, false)]
        [TestCase(6, 0.55d, 0.90d, 1.45d, 0.15d, 5, true)]
        public void RulesResolveEveryAwakeningThreshold(
            int awakening,
            double match3,
            double match4,
            double match5,
            double match5DamageIncrease,
            int spiritCost,
            bool hasAwakening6Buff)
        {
            SakuragiKoharuResolvedCombatRules rules =
                SakuragiKoharuCombatRuleResolver.Resolve(config, awakening);

            Assert.That(rules.Match3Coefficient, Is.EqualTo(match3));
            Assert.That(rules.Match4Coefficient, Is.EqualTo(match4));
            Assert.That(rules.Match5Coefficient, Is.EqualTo(match5));
            Assert.That(rules.Match5AdditionalAndChaseDamageIncreaseRate,
                Is.EqualTo(match5DamageIncrease));
            Assert.That(rules.SpiritEnergyCostPerCooldownReduction,
                Is.EqualTo(spiritCost));
            Assert.That(
                rules.HasAwakening6AdditionalAndChaseDamageIncrease,
                Is.EqualTo(hasAwakening6Buff));
            Assert.That(
                rules.Awakening6AdditionalAndChaseDamageIncreaseRate,
                Is.EqualTo(0.10d));
            Assert.That(rules.Awakening6EffectDurationTurns, Is.EqualTo(1));
            Assert.That(new[]
            {
                rules.Match3SpiritEnergyGain,
                rules.Match4SpiritEnergyGain,
                rules.Match5SpiritEnergyGain
            }, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(rules.Match5EffectDurationTurns, Is.EqualTo(2));
            Assert.That(rules.ActiveCooldownTurns, Is.EqualTo(4));
            Assert.That(rules.ActiveBlockConversionCount, Is.EqualTo(2));
        }

        [TestCase(-1)]
        [TestCase(7)]
        public void ResolverAndBuildModifierRejectInvalidAwakening(
            int awakening)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SakuragiKoharuCombatRuleResolver.Resolve(config, awakening));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SakuragiKoharuBuildModifier()
                    .GetStatMultipliers(awakening));
        }
    }
}
