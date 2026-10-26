using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Characters.Brina;
using ValorChronicle.Characters.Build;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Brina
{
    public sealed class BrinaAshwoodBuildModifierTests
    {
        private CharacterDefinition definition;
        private CharacterBuildResolver resolver;

        [SetUp]
        public void SetUp()
        {
            definition = ScriptableObject.CreateInstance<
                CharacterDefinition>();
            SetField(
                definition,
                typeof(GameDefinition),
                "id",
                BrinaAshwoodRules.CharacterId);
            SetField(definition, "level1Hp", 900);
            SetField(definition, "level1Attack", 180);
            SetField(definition, "level100Hp", 3400);
            SetField(definition, "level100Attack", 1050);
            resolver = new CharacterBuildResolver(
                new ICharacterBuildModifier[]
                {
                    new BrinaAshwoodBuildModifier()
                });
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }

        [TestCase(1, 0, 900L, 180L)]
        [TestCase(40, 0, 1885L, 523L)]
        [TestCase(100, 0, 3400L, 1050L)]
        [TestCase(100, 1, 3400L, 1103L)]
        [TestCase(100, 2, 3570L, 1103L)]
        [TestCase(100, 6, 3570L, 1103L)]
        public void ResolveReturnsExpectedStats(
            int level,
            int awakening,
            long expectedMaxHp,
            long expectedAttack)
        {
            ResolvedCharacterBuild result = resolver.Resolve(
                definition,
                level,
                awakening);

            Assert.That(result.CharacterId,
                Is.EqualTo(BrinaAshwoodRules.CharacterId));
            Assert.That(result.MaxHp, Is.EqualTo(expectedMaxHp));
            Assert.That(result.Attack, Is.EqualTo(expectedAttack));
            Assert.That(result.MaxHpMultiplier,
                Is.EqualTo(awakening >= 2 ? 1.05d : 1d));
            Assert.That(result.AttackMultiplier,
                Is.EqualTo(awakening >= 1 ? 1.05d : 1d));
        }

        [Test]
        public void AwakeningOneUsesCommonAwayFromZeroRounding()
        {
            ResolvedCharacterBuild result = resolver.Resolve(
                definition,
                100,
                1);

            Assert.That(1050d * 1.05d, Is.EqualTo(1102.5d));
            Assert.That(result.Attack, Is.EqualTo(1103L));
        }

        [TestCase(0, 1.00d, 1.00d)]
        [TestCase(1, 1.00d, 1.05d)]
        [TestCase(2, 1.05d, 1.05d)]
        [TestCase(3, 1.05d, 1.05d)]
        [TestCase(4, 1.05d, 1.05d)]
        [TestCase(5, 1.05d, 1.05d)]
        [TestCase(6, 1.05d, 1.05d)]
        public void ModifierSeparatesAwakeningThresholds(
            int awakening,
            double expectedMaxHpMultiplier,
            double expectedAttackMultiplier)
        {
            var modifier = new BrinaAshwoodBuildModifier();

            CharacterBuildStatMultipliers result =
                modifier.GetStatMultipliers(awakening);

            Assert.That(result.MaxHpMultiplier,
                Is.EqualTo(expectedMaxHpMultiplier));
            Assert.That(result.AttackMultiplier,
                Is.EqualTo(expectedAttackMultiplier));
        }

        [TestCase(-1)]
        [TestCase(7)]
        public void ModifierRejectsAwakeningOutsideSupportedRange(
            int awakening)
        {
            var modifier = new BrinaAshwoodBuildModifier();

            Assert.That(
                () => modifier.GetStatMultipliers(awakening),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName").EqualTo("awakening"));
        }

        private static void SetField(
            CharacterDefinition target,
            string fieldName,
            object value)
        {
            SetField(
                target,
                typeof(CharacterDefinition),
                fieldName,
                value);
        }

        private static void SetField(
            CharacterDefinition target,
            Type declaringType,
            string fieldName,
            object value)
        {
            FieldInfo field = declaringType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
