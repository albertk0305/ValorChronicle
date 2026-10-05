using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Build;
using ValorChronicle.Characters.Stats;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Stats
{
    public sealed class CharacterStatCalculatorTests
    {
        private CharacterDefinition definition;

        [SetUp]
        public void SetUp()
        {
            definition = ScriptableObject.CreateInstance<CharacterDefinition>();
            SetField(typeof(GameDefinition), "id", "character_marea_bluefang");
            SetField(typeof(CharacterDefinition), "element", ElementType.Water);
            SetField(typeof(CharacterDefinition), "level1Hp", 900);
            SetField(typeof(CharacterDefinition), "level1Attack", 180);
            SetField(typeof(CharacterDefinition), "level100Hp", 3400);
            SetField(typeof(CharacterDefinition), "level100Attack", 1050);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(definition);
        }

        [TestCase(1, 900, 180)]
        [TestCase(10, 1127, 259)]
        [TestCase(40, 1885, 523)]
        [TestCase(70, 2642, 786)]
        [TestCase(100, 3400, 1050)]
        public void CalculateReturnsDirectlyInterpolatedMareaStats(
            int level,
            long expectedHp,
            long expectedAttack)
        {
            CharacterStatValues result =
                CharacterStatCalculator.Calculate(definition, level);

            Assert.That(result.MaxHp, Is.EqualTo(expectedHp));
            Assert.That(result.Attack, Is.EqualTo(expectedAttack));
        }

        [TestCase(0)]
        [TestCase(101)]
        public void CalculateRejectsLevelOutsideOneToOneHundred(int level)
        {
            Assert.That(
                () => CharacterStatCalculator.Calculate(definition, level),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void RoundFinalStatUsesAwayFromZeroAtHalfBoundary()
        {
            Assert.That(
                CharacterStatCalculator.RoundFinalStat(10.5d),
                Is.EqualTo(11));
        }

        [Test]
        public void CalculateUsesEndpointsInsteadOfRoundedPerLevelGrowth()
        {
            SetField(typeof(CharacterDefinition), "level1Hp", 10);
            SetField(typeof(CharacterDefinition), "level100Hp", 20);

            CharacterStatValues result =
                CharacterStatCalculator.Calculate(definition, 50);

            Assert.That(result.MaxHp, Is.EqualTo(15));
        }

        [Test]
        public void BattleStateFactoryPreservesDefinitionAndUsesResolvedBuild()
        {
            ResolvedCharacterBuild build =
                CharacterBuildResolverFactory.CreateDefault().Resolve(
                    definition,
                    40,
                    2);
            CharacterBattleState state = CharacterBattleStateFactory.Create(
                definition,
                build,
                3);

            Assert.That(state.CharacterId,
                Is.EqualTo("character_marea_bluefang"));
            Assert.That(state.PartySlotIndex, Is.EqualTo(3));
            Assert.That(state.Element, Is.EqualTo(ElementType.Water));
            Assert.That(state.MaxHp, Is.EqualTo(1979));
            Assert.That(state.Attack, Is.EqualTo(549d));
            Assert.That(definition.Level1Hp, Is.EqualTo(900));
            Assert.That(definition.Level1Attack, Is.EqualTo(180));
            Assert.That(definition.Level100Hp, Is.EqualTo(3400));
            Assert.That(definition.Level100Attack, Is.EqualTo(1050));
        }

        private void SetField(
            System.Type declaringType,
            string fieldName,
            object value)
        {
            FieldInfo field = declaringType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(definition, value);
        }
    }
}
