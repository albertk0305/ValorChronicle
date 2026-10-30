using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Characters.BaekHyeon;
using ValorChronicle.Characters.Build;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.BaekHyeon
{
    public sealed class BaekHyeonBuildModifierTests
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
                BaekHyeonRules.CharacterId);
            SetField(definition, "level1Hp", 870);
            SetField(definition, "level1Attack", 170);
            SetField(definition, "level100Hp", 3200);
            SetField(definition, "level100Attack", 980);
            resolver = new CharacterBuildResolver(
                new ICharacterBuildModifier[]
                {
                    new BaekHyeonBuildModifier()
                });
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }

        [TestCase(1, 0, 870L, 170L)]
        [TestCase(10, 0, 1082L, 244L)]
        [TestCase(40, 0, 1788L, 489L)]
        [TestCase(70, 0, 2494L, 735L)]
        [TestCase(100, 0, 3200L, 980L)]
        [TestCase(1, 1, 870L, 179L)]
        [TestCase(100, 1, 3200L, 1029L)]
        [TestCase(1, 2, 914L, 179L)]
        [TestCase(100, 2, 3360L, 1029L)]
        [TestCase(100, 6, 3360L, 1029L)]
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
                Is.EqualTo(BaekHyeonRules.CharacterId));
            Assert.That(result.MaxHp, Is.EqualTo(expectedMaxHp));
            Assert.That(result.Attack, Is.EqualTo(expectedAttack));
            Assert.That(result.MaxHpMultiplier,
                Is.EqualTo(awakening >= 2 ? 1.05d : 1d));
            Assert.That(result.AttackMultiplier,
                Is.EqualTo(awakening >= 1 ? 1.05d : 1d));
        }

        [Test]
        public void AwakeningBonusesUseCommonAwayFromZeroRounding()
        {
            ResolvedCharacterBuild result = resolver.Resolve(
                definition,
                1,
                2);

            Assert.That(870d * 1.05d, Is.EqualTo(913.5d));
            Assert.That(170d * 1.05d, Is.EqualTo(178.5d));
            Assert.That(result.MaxHp, Is.EqualTo(914L));
            Assert.That(result.Attack, Is.EqualTo(179L));
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
            var modifier = new BaekHyeonBuildModifier();

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
            var modifier = new BaekHyeonBuildModifier();

            Assert.That(
                () => modifier.GetStatMultipliers(awakening),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName").EqualTo("awakening"));
        }

        [TestCase(0)]
        [TestCase(101)]
        public void ResolveRejectsLevelOutsideSupportedRange(int level)
        {
            Assert.That(
                () => resolver.Resolve(definition, level, 0),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName").EqualTo("level"));
        }

        private static void SetField(
            CharacterDefinition target,
            string fieldName,
            object value)
        {
            SetField(target, typeof(CharacterDefinition), fieldName, value);
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
