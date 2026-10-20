using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Serena;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Serena
{
    public sealed class SerenaCoralspearBuildModifierTests
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
                SerenaCoralspearRules.CharacterId);
            SetField(definition, "level1Hp", 870);
            SetField(definition, "level1Attack", 170);
            SetField(definition, "level100Hp", 3200);
            SetField(definition, "level100Attack", 980);
            resolver = new CharacterBuildResolver(
                new ICharacterBuildModifier[]
                {
                    new SerenaCoralspearBuildModifier()
                });
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }

        [TestCase(1, 0, 870L, 170L)]
        [TestCase(100, 0, 3200L, 980L)]
        [TestCase(1, 1, 870L, 179L)]
        [TestCase(100, 1, 3200L, 1029L)]
        [TestCase(100, 6, 3200L, 1029L)]
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
                Is.EqualTo(SerenaCoralspearRules.CharacterId));
            Assert.That(result.MaxHp, Is.EqualTo(expectedMaxHp));
            Assert.That(result.Attack, Is.EqualTo(expectedAttack));
            Assert.That(result.MaxHpMultiplier, Is.EqualTo(1d));
            Assert.That(result.AttackMultiplier,
                Is.EqualTo(awakening >= 1 ? 1.05d : 1d));
        }

        [TestCase(-1)]
        [TestCase(7)]
        public void ModifierRejectsAwakeningOutsideSupportedRange(
            int awakening)
        {
            var modifier = new SerenaCoralspearBuildModifier();

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
