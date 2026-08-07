using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.State
{
    public sealed class PartyBattleStateTests
    {
        [Test]
        public void Constructor_SumsCharacterHpAndStartsAtMaximum()
        {
            var party = new PartyBattleState(new[]
            {
                Character("third", 2, 2500),
                Character("first", 0, 1000)
            });

            Assert.That(party.Characters, Has.Count.EqualTo(2));
            Assert.That(party.Characters[0].PartySlotIndex, Is.Zero);
            Assert.That(party.Characters[1].PartySlotIndex, Is.EqualTo(2));
            Assert.That(party.MaxHp, Is.EqualTo(3500L));
            Assert.That(party.CurrentHp, Is.EqualTo(3500L));
            Assert.That(party.IsIncapacitated, Is.False);
        }

        [Test]
        public void Constructor_AllowsEmptySlotsButRejectsDuplicateSlot()
        {
            Assert.Throws<ArgumentException>(() =>
                new PartyBattleState(new[]
                {
                    Character("first", 1),
                    Character("second", 1)
                }));
        }

        [Test]
        public void Constructor_RejectsDuplicateCharacterId()
        {
            Assert.Throws<ArgumentException>(() =>
                new PartyBattleState(new[]
                {
                    Character("same", 0),
                    Character("same", 1)
                }));
        }

        [Test]
        public void Constructor_RejectsMoreThanFiveCharacters()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PartyBattleState(new[]
                {
                    Character("a", 0),
                    Character("b", 1),
                    Character("c", 2),
                    Character("d", 3),
                    Character("e", 4),
                    Character("f", 0)
                }));
        }

        [Test]
        public void Constructor_RejectsEmptyParty()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PartyBattleState(Array.Empty<CharacterBattleState>()));
        }

        [Test]
        public void Constructor_DefensivelyCopiesCharacterCollection()
        {
            var source = new List<CharacterBattleState>
            {
                Character("first", 0)
            };
            var party = new PartyBattleState(source);

            source[0] = Character("replacement", 1);
            source.Add(Character("added", 2));

            Assert.That(party.Characters, Has.Count.EqualTo(1));
            Assert.That(party.Characters[0].CharacterId,
                Is.EqualTo("first"));
            var exposed = party.Characters as IList<CharacterBattleState>;
            Assert.That(exposed, Is.Not.Null);
            Assert.Throws<NotSupportedException>(() =>
                exposed.Add(Character("blocked", 3)));
        }

        private static CharacterBattleState Character(
            string id,
            int slot,
            long maxHp = 100)
        {
            return new CharacterBattleState(
                id,
                slot,
                ElementType.Fire,
                maxHp,
                10d);
        }
    }
}
