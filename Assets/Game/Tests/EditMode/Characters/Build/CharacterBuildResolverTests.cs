using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Marea;
using ValorChronicle.Characters.Stats;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Build
{
    public sealed class CharacterBuildResolverTests
    {
        private CharacterDefinition definition;
        private CharacterBuildResolver resolver;

        [SetUp]
        public void SetUp()
        {
            definition = CreateDefinition(
                MareaBluefangRules.CharacterId,
                900,
                180,
                3400,
                1050);
            resolver = new CharacterBuildResolver(
                new ICharacterBuildModifier[]
                {
                    new MareaBluefangBuildModifier()
                });
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }

        [TestCase(1, 0, 900L, 180L)]
        [TestCase(100, 0, 3400L, 1050L)]
        [TestCase(1, 1, 900L, 189L)]
        [TestCase(100, 1, 3400L, 1103L)]
        [TestCase(1, 2, 945L, 189L)]
        [TestCase(100, 2, 3570L, 1103L)]
        [TestCase(100, 3, 3570L, 1103L)]
        [TestCase(100, 4, 3570L, 1103L)]
        [TestCase(100, 5, 3570L, 1103L)]
        [TestCase(100, 6, 3570L, 1103L)]
        public void Resolve_ReturnsExpectedMareaStatsByAwakening(
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
                Is.EqualTo(MareaBluefangRules.CharacterId));
            Assert.That(result.Level, Is.EqualTo(level));
            Assert.That(result.Awakening, Is.EqualTo(awakening));
            Assert.That(result.MaxHp, Is.EqualTo(expectedMaxHp));
            Assert.That(result.Attack, Is.EqualTo(expectedAttack));
        }

        [Test]
        public void Resolve_MiddleLevel_ModifiesCalculatedBaseStats()
        {
            CharacterStatValues baseStats =
                CharacterStatCalculator.Calculate(definition, 40);

            ResolvedCharacterBuild result = resolver.Resolve(
                definition,
                40,
                2);

            Assert.That(baseStats.MaxHp, Is.EqualTo(1885));
            Assert.That(baseStats.Attack, Is.EqualTo(523));
            Assert.That(result.MaxHp, Is.EqualTo(1979));
            Assert.That(result.Attack, Is.EqualTo(549));
        }

        [Test]
        public void Resolve_AwakeningMultiplierUsesAwayFromZeroRounding()
        {
            SetField(
                definition,
                typeof(CharacterDefinition),
                "level1Attack",
                210);

            ResolvedCharacterBuild result = resolver.Resolve(
                definition,
                1,
                1);

            Assert.That(result.Attack, Is.EqualTo(221));
        }

        [TestCase(-1)]
        [TestCase(7)]
        public void Resolve_RejectsAwakeningOutsideZeroToSix(
            int awakening)
        {
            Assert.That(
                () => resolver.Resolve(definition, 1, awakening),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName")
                    .EqualTo("awakening"));
        }

        [TestCase(0)]
        [TestCase(101)]
        public void Resolve_RejectsLevelOutsideOneToOneHundred(int level)
        {
            Assert.That(
                () => resolver.Resolve(definition, level, 0),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName")
                    .EqualTo("level"));
        }

        [Test]
        public void Resolve_RejectsNullDefinition()
        {
            Assert.That(
                () => resolver.Resolve(null, 1, 0),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Property("ParamName")
                    .EqualTo("definition"));
        }

        [Test]
        public void Resolve_DoesNotMutateDefinition()
        {
            resolver.Resolve(definition, 100, 6);

            Assert.That(definition.Id,
                Is.EqualTo(MareaBluefangRules.CharacterId));
            Assert.That(definition.Level1Hp, Is.EqualTo(900));
            Assert.That(definition.Level1Attack, Is.EqualTo(180));
            Assert.That(definition.Level100Hp, Is.EqualTo(3400));
            Assert.That(definition.Level100Attack, Is.EqualTo(1050));
        }

        [Test]
        public void Resolve_SameInputAlwaysReturnsSameValues()
        {
            ResolvedCharacterBuild first = resolver.Resolve(
                definition,
                40,
                2);
            ResolvedCharacterBuild second = resolver.Resolve(
                definition,
                40,
                2);

            Assert.That(second.CharacterId, Is.EqualTo(first.CharacterId));
            Assert.That(second.Level, Is.EqualTo(first.Level));
            Assert.That(second.Awakening, Is.EqualTo(first.Awakening));
            Assert.That(second.MaxHp, Is.EqualTo(first.MaxHp));
            Assert.That(second.Attack, Is.EqualTo(first.Attack));
        }

        [Test]
        public void Resolve_UnregisteredCharacterUsesUnmodifiedBaseStats()
        {
            CharacterDefinition unregistered = CreateDefinition(
                "character_unregistered",
                101,
                51,
                201,
                151);
            try
            {
                ResolvedCharacterBuild result = resolver.Resolve(
                    unregistered,
                    1,
                    6);

                Assert.That(result.MaxHp, Is.EqualTo(101));
                Assert.That(result.Attack, Is.EqualTo(51));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(unregistered);
            }
        }

        [Test]
        public void DefaultFactoryRegistersMareaModifier()
        {
            ResolvedCharacterBuild result =
                CharacterBuildResolverFactory.CreateDefault().Resolve(
                    definition,
                    100,
                    2);

            Assert.That(result.MaxHp, Is.EqualTo(3570));
            Assert.That(result.Attack, Is.EqualTo(1103));
        }

        [Test]
        public void DefaultFactoryKeepsUnregisteredCharacterIdentityPolicy()
        {
            CharacterDefinition unregistered = CreateDefinition(
                "character_unregistered",
                101,
                51,
                201,
                151);
            try
            {
                ResolvedCharacterBuild result =
                    CharacterBuildResolverFactory.CreateDefault().Resolve(
                        unregistered,
                        1,
                        6);

                Assert.That(result.MaxHp, Is.EqualTo(101));
                Assert.That(result.Attack, Is.EqualTo(51));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(unregistered);
            }
        }

        private static CharacterDefinition CreateDefinition(
            string characterId,
            int level1Hp,
            int level1Attack,
            int level100Hp,
            int level100Attack)
        {
            CharacterDefinition created =
                ScriptableObject.CreateInstance<CharacterDefinition>();
            SetField(created, typeof(GameDefinition), "id", characterId);
            SetField(
                created,
                typeof(CharacterDefinition),
                "level1Hp",
                level1Hp);
            SetField(
                created,
                typeof(CharacterDefinition),
                "level1Attack",
                level1Attack);
            SetField(
                created,
                typeof(CharacterDefinition),
                "level100Hp",
                level100Hp);
            SetField(
                created,
                typeof(CharacterDefinition),
                "level100Attack",
                level100Attack);
            return created;
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
