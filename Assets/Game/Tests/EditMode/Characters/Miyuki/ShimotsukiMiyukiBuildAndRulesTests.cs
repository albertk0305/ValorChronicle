using System;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Miyuki;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Miyuki
{
    public sealed class ShimotsukiMiyukiBuildAndRulesTests
    {
        private ShimotsukiMiyukiCombatConfig config;
        private CharacterDefinition definition;
        private CharacterBuildResolver buildResolver;

        [SetUp]
        public void SetUp()
        {
            config = ShimotsukiMiyukiTestConfig.Create();
            definition = ShimotsukiMiyukiTestConfig.Definition(config);
            buildResolver = new CharacterBuildResolver(
                new ICharacterBuildModifier[]
                {
                    new ShimotsukiMiyukiBuildModifier()
                });
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(definition);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [TestCase(1, 0, 1100L, 120L)]
        [TestCase(50, 0, 2684L, 382L)]
        [TestCase(100, 0, 4300L, 650L)]
        [TestCase(100, 1, 4515L, 650L)]
        [TestCase(100, 2, 4515L, 683L)]
        [TestCase(100, 6, 4515L, 683L)]
        public void BuildUsesBaseStatsAndAwakeningMultipliers(
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

        [TestCase(-1)]
        [TestCase(7)]
        public void RulesAndModifierRejectInvalidAwakening(int awakening)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ShimotsukiMiyukiCombatRuleResolver.Resolve(config, awakening));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ShimotsukiMiyukiBuildModifier()
                    .GetStatMultipliers(awakening));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void EverySupportedAwakeningResolves(int awakening)
        {
            Assert.DoesNotThrow(() =>
                ShimotsukiMiyukiCombatRuleResolver.Resolve(
                    config,
                    awakening));
        }

        [TestCase(0, 0.55d, 0.90d, 0.08d)]
        [TestCase(3, 0.55d, 0.90d, 0.10d)]
        [TestCase(4, 0.60d, 1.00d, 0.10d)]
        [TestCase(6, 0.60d, 1.00d, 0.10d)]
        public void RulesResolveAwakeningThresholds(
            int awakening,
            double match3,
            double match4,
            double passive)
        {
            ShimotsukiMiyukiResolvedCombatRules rules =
                ShimotsukiMiyukiCombatRuleResolver.Resolve(config, awakening);

            Assert.That(rules.Match3Coefficient, Is.EqualTo(match3));
            Assert.That(rules.Match4Coefficient, Is.EqualTo(match4));
            Assert.That(rules.Match5Coefficient, Is.EqualTo(1.45d));
            Assert.That(rules.PassiveWaterDamageIncreaseRate,
                Is.EqualTo(passive));
            Assert.That(rules.ActiveCooldownTurns, Is.EqualTo(8));
            Assert.That(rules.ActiveBlockConversionCount,
                Is.EqualTo(awakening >= 5 ? 5 : 4));
            Assert.That(rules.HasFrostEcho, Is.EqualTo(awakening >= 6));
            Assert.That(rules.FrostEchoExtraWaterAmount, Is.EqualTo(1));
        }
    }
}
