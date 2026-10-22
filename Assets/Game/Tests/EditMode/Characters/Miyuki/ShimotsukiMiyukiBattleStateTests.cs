using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Characters.Miyuki;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Miyuki
{
    public sealed class ShimotsukiMiyukiBattleStateTests
    {
        [Test]
        public void EnabledStateGrantsAtMostOneAndReportsStatus()
        {
            CharacterBattleState owner = Character();
            var state = new ShimotsukiMiyukiBattleState(owner, true);

            Assert.That(state.TryGrantFrostEcho(), Is.True);
            Assert.That(state.TryGrantFrostEcho(), Is.False);
            Assert.That(state.FrostEchoCount, Is.EqualTo(1));
            Assert.That(state.CurrentCount, Is.EqualTo(1));
            Assert.That(state.IsVisible, Is.True);
            Assert.That(state.StatusId,
                Is.EqualTo(ShimotsukiMiyukiRules.FrostEchoStatusId));
            Assert.That(state.CharacterId, Is.EqualTo(owner.CharacterId));
            Assert.That(state.PartySlotIndex, Is.EqualTo(2));
        }

        [Test]
        public void DisabledStateCannotGrantOrConsume()
        {
            var state = new ShimotsukiMiyukiBattleState(Character(), false);

            Assert.That(state.TryGrantFrostEcho(), Is.False);
            Assert.That(state.TryConsumeFrostEcho(), Is.False);
            Assert.That(state.IsVisible, Is.False);
        }

        [Test]
        public void ConsumeRemovesVisibleStatus()
        {
            var state = new ShimotsukiMiyukiBattleState(Character(), true);
            state.TryGrantFrostEcho();

            Assert.That(state.TryConsumeFrostEcho(), Is.True);
            Assert.That(state.HasFrostEcho, Is.False);
            Assert.That(state.CurrentCount, Is.Zero);
            Assert.That(state.IsVisible, Is.False);
        }

        [Test]
        public void TurnEndDoesNotClearFrostEcho()
        {
            CharacterBattleState owner = Character();
            var state = new ShimotsukiMiyukiBattleState(owner, true);
            state.TryGrantFrostEcho();
            var party = new PartyBattleState(new[] { owner });
            var boss = new BossBattleState(
                "turn_end_test_boss",
                ElementType.Fire,
                1000,
                100d);

            new BattleTurnEndProcessor(party, boss).ProcessTurnEnd();

            Assert.That(state.HasFrostEcho, Is.True);
        }

        [Test]
        public void NewBattleStateDoesNotInheritFrostEcho()
        {
            var first = new ShimotsukiMiyukiBattleState(Character(), true);
            var second = new ShimotsukiMiyukiBattleState(Character(), true);
            first.TryGrantFrostEcho();

            Assert.That(first.HasFrostEcho, Is.True);
            Assert.That(second.HasFrostEcho, Is.False);
        }

        [Test]
        public void StateRejectsAnotherCharacter()
        {
            var other = new CharacterBattleState(
                "other_character",
                0,
                ElementType.Water,
                1000,
                100d);

            Assert.Throws<ArgumentException>(() =>
                new ShimotsukiMiyukiBattleState(other, true));
        }

        private static CharacterBattleState Character()
        {
            return new CharacterBattleState(
                ShimotsukiMiyukiRules.CharacterId,
                2,
                ElementType.Water,
                1000,
                100d);
        }
    }
}
