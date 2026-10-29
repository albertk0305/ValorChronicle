using System;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Naira;
using ValorChronicle.Data.Definitions;
using ValorChronicle.Relics.Stats;

namespace ValorChronicle.Tests.EditMode.Characters.Naira
{
    public sealed class NairaWindhornBuildModifierTests
    {
        private const string DefinitionPath =
            "Assets/Data/Characters/naira_windhorn.asset";

        private CharacterDefinition definition;
        private CharacterBuildResolver resolver;

        [SetUp]
        public void SetUp()
        {
            definition = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(
                DefinitionPath);
            resolver = new CharacterBuildResolver(
                new ICharacterBuildModifier[]
                {
                    new NairaWindhornBuildModifier()
                });
        }

        [TestCase(1, 1500L, 85L)]
        [TestCase(10, 1927L, 116L)]
        [TestCase(40, 3352L, 219L)]
        [TestCase(70, 4776L, 322L)]
        [TestCase(100, 6200L, 425L)]
        public void AwakeningZeroUsesLinearBaseStats(
            int level,
            long expectedMaxHp,
            long expectedAttack)
        {
            Assert.That(definition, Is.Not.Null);

            ResolvedCharacterBuild result = resolver.Resolve(
                definition,
                level,
                awakening: 0);

            Assert.That(result.MaxHp, Is.EqualTo(expectedMaxHp));
            Assert.That(result.Attack, Is.EqualTo(expectedAttack));
        }

        [TestCase(0, 6200L, 425L)]
        [TestCase(1, 6510L, 425L)]
        [TestCase(2, 6510L, 446L)]
        [TestCase(3, 6510L, 446L)]
        [TestCase(4, 6510L, 446L)]
        [TestCase(5, 6510L, 446L)]
        [TestCase(6, 6510L, 446L)]
        public void Level100AppliesOnlyFirstTwoPermanentStatAwakenings(
            int awakening,
            long expectedMaxHp,
            long expectedAttack)
        {
            ResolvedCharacterBuild result = resolver.Resolve(
                definition,
                level: 100,
                awakening: awakening);

            Assert.That(result.MaxHp, Is.EqualTo(expectedMaxHp));
            Assert.That(result.Attack, Is.EqualTo(expectedAttack));
        }

        [Test]
        public void AwakeningAndRelicMaxHpRatesAreAppliedOnce()
        {
            ResolvedCharacterBuild result = resolver.Resolve(
                definition,
                level: 100,
                awakening: 2,
                new RelicStatBonusSummary(
                    maxHpIncreaseRateSum: 0.10d,
                    attackIncreaseRateSum: 0d));

            Assert.That(result.MaxHp, Is.EqualTo(7161L));
            Assert.That(result.Attack, Is.EqualTo(446L));
            Assert.That(result.MaxHpMultiplier, Is.EqualTo(1.05d));
            Assert.That(result.AttackMultiplier, Is.EqualTo(1.05d));
        }

        [TestCase(-1)]
        [TestCase(7)]
        public void ModifierRejectsAwakeningOutsideSupportedRange(
            int awakening)
        {
            var modifier = new NairaWindhornBuildModifier();

            Assert.That(
                () => modifier.GetStatMultipliers(awakening),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName").EqualTo("awakening"));
        }
    }
}

