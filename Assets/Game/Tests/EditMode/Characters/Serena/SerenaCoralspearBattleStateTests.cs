using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Serena;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Serena
{
    public sealed class SerenaCoralspearBattleStateTests
    {
        [Test]
        public void NewBattleStateStartsWithNoEmpoweredChase()
        {
            SerenaCoralspearBattleState state = CreateState();

            Assert.That(state.EmpoweredChaseCharges, Is.Zero);
            Assert.That(state.HasEmpoweredChase, Is.False);
            Assert.That(state.EmpoweredChaseMaxCharges, Is.EqualTo(3));
            ICharacterCombatStatusSource source = state;
            Assert.That(source.StatusId,
                Is.EqualTo(SerenaCoralspearRules.EmpoweredChaseStatusId));
            Assert.That(source.CharacterId,
                Is.EqualTo(SerenaCoralspearRules.CharacterId));
            Assert.That(source.PartySlotIndex, Is.Zero);
            Assert.That(source.CurrentCount, Is.Zero);
            Assert.That(source.IsVisible, Is.False);
        }

        [Test]
        public void RepeatedGrantStopsAtConfiguredMaximum()
        {
            SerenaCoralspearBattleState state = CreateState();

            Assert.That(state.TryGrantEmpoweredChase(), Is.True);
            Assert.That(state.TryGrantEmpoweredChase(), Is.True);
            Assert.That(state.TryGrantEmpoweredChase(), Is.True);
            Assert.That(state.TryGrantEmpoweredChase(), Is.False);
            Assert.That(state.EmpoweredChaseCharges, Is.EqualTo(3));
            ICharacterCombatStatusSource source = state;
            Assert.That(source.CurrentCount, Is.EqualTo(3));
            Assert.That(source.IsVisible, Is.True);
        }

        [Test]
        public void ConsumeRemovesOneChargeWithoutChangingMaximum()
        {
            SerenaCoralspearBattleState state = CreateState();
            state.TryGrantEmpoweredChase();
            state.TryGrantEmpoweredChase();

            Assert.That(state.TryConsumeEmpoweredChase(), Is.True);
            Assert.That(state.EmpoweredChaseCharges, Is.EqualTo(1));
            Assert.That(state.EmpoweredChaseMaxCharges, Is.EqualTo(3));
            Assert.That(state.TryConsumeEmpoweredChase(), Is.True);
            Assert.That(state.TryConsumeEmpoweredChase(), Is.False);
        }

        [Test]
        public void SeparateBattleInstancesKeepIndependentCharges()
        {
            SerenaCoralspearBattleState first = CreateState();
            SerenaCoralspearBattleState second = CreateState();

            first.TryGrantEmpoweredChase();
            first.TryGrantEmpoweredChase();

            Assert.That(first.EmpoweredChaseCharges, Is.EqualTo(2));
            Assert.That(second.EmpoweredChaseCharges, Is.Zero);
            Assert.That(first.Owner, Is.Not.SameAs(second.Owner));
        }

        [TestCase(0, 0, "empoweredChaseMaxCharges")]
        [TestCase(3, -1, "initialEmpoweredChaseCharges")]
        [TestCase(3, 4, "initialEmpoweredChaseCharges")]
        public void InvalidChargeConfigurationIsRejected(
            int maximum,
            int initial,
            string expectedParameter)
        {
            CharacterBattleState owner = CreateCharacter(
                SerenaCoralspearRules.CharacterId);

            Assert.That(
                () => new SerenaCoralspearBattleState(
                    owner,
                    maximum,
                    initial),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName")
                    .EqualTo(expectedParameter));
        }

        [Test]
        public void OtherCharacterCannotOwnSerenaState()
        {
            CharacterBattleState other = CreateCharacter(
                "character_other");

            Assert.That(
                () => new SerenaCoralspearBattleState(other, 3),
                Throws.TypeOf<ArgumentException>()
                    .With.Property("ParamName").EqualTo("owner"));
        }

        private static SerenaCoralspearBattleState CreateState()
        {
            return new SerenaCoralspearBattleState(
                CreateCharacter(SerenaCoralspearRules.CharacterId),
                3);
        }

        private static CharacterBattleState CreateCharacter(
            string characterId)
        {
            return new CharacterBattleState(
                characterId,
                0,
                ElementType.Water,
                870,
                170d);
        }
    }
}
