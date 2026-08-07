using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.State
{
    public sealed class CharacterBattleStateTests
    {
        [Test]
        public void Constructor_CreatesStatSnapshot()
        {
            var state = new CharacterBattleState(
                "hero_fire",
                2,
                ElementType.Fire,
                1500,
                275.5d);

            Assert.That(state.CharacterId, Is.EqualTo("hero_fire"));
            Assert.That(state.PartySlotIndex, Is.EqualTo(2));
            Assert.That(state.Element, Is.EqualTo(ElementType.Fire));
            Assert.That(state.MaxHp, Is.EqualTo(1500L));
            Assert.That(state.Attack, Is.EqualTo(275.5d));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_RejectsMissingCharacterId(string characterId)
        {
            Assert.Throws<ArgumentException>(() =>
                CreateCharacter(characterId: characterId));
        }

        [TestCase(-1)]
        [TestCase(5)]
        public void Constructor_RejectsPartySlotOutsideZeroToFour(int slot)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateCharacter(partySlotIndex: slot));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_RejectsNonPositiveMaximumHp(long maxHp)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateCharacter(maxHp: maxHp));
        }

        [TestCase(-0.01d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void Constructor_RejectsInvalidAttack(double attack)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateCharacter(attack: attack));
        }

        private static CharacterBattleState CreateCharacter(
            string characterId = "hero",
            int partySlotIndex = 0,
            long maxHp = 100,
            double attack = 10d)
        {
            return new CharacterBattleState(
                characterId,
                partySlotIndex,
                ElementType.Fire,
                maxHp,
                attack);
        }
    }
}
