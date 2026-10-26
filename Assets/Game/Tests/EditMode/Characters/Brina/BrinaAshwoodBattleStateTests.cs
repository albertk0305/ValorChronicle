using System;
using NUnit.Framework;
using ValorChronicle.Battle.Combat.Integration;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Characters.Brina;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Brina
{
    public sealed class BrinaAshwoodBattleStateTests
    {
        [Test]
        public void NewStateStartsAtZeroAndAlwaysExposesLiveStatus()
        {
            BrinaAshwoodBattleState state = CreateState(slotIndex: 3);
            ICharacterCombatStatusSource status = state;

            Assert.That(state.WoodcutCount, Is.Zero);
            Assert.That(status.StatusId,
                Is.EqualTo(BrinaAshwoodRules.WoodcutStatusId));
            Assert.That(status.CharacterId,
                Is.EqualTo(BrinaAshwoodRules.CharacterId));
            Assert.That(status.PartySlotIndex, Is.EqualTo(3));
            Assert.That(status.CurrentCount, Is.Zero);
            Assert.That(status.IsVisible, Is.True);

            state.GainWoodcut(4);

            Assert.That(status.CurrentCount, Is.EqualTo(4));
        }

        [TestCase(8, 1, 0, 9)]
        [TestCase(9, 1, 1, 0)]
        [TestCase(9, 2, 1, 1)]
        [TestCase(0, 22, 2, 2)]
        public void GainConsumesEveryCompleteThreshold(
            int initial,
            int gain,
            int expectedTriggers,
            int expectedRemainder)
        {
            BrinaAshwoodBattleState state = CreateState();
            state.GainWoodcut(initial);

            int triggers = state.GainWoodcut(gain);

            Assert.That(triggers, Is.EqualTo(expectedTriggers));
            Assert.That(state.WoodcutCount,
                Is.EqualTo(expectedRemainder));
        }

        [Test]
        public void MaximumIntGainDoesNotOverflow()
        {
            BrinaAshwoodBattleState state = CreateState();

            int triggers = state.GainWoodcut(int.MaxValue);

            Assert.That(triggers, Is.EqualTo(214748364));
            Assert.That(state.WoodcutCount, Is.EqualTo(7));
        }

        [Test]
        public void NegativeGainIsRejectedWithoutMutation()
        {
            BrinaAshwoodBattleState state = CreateState();
            state.GainWoodcut(4);

            Assert.That(
                () => state.GainWoodcut(-1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(state.WoodcutCount, Is.EqualTo(4));
        }

        [Test]
        public void FirstGainMarkerIsTrackedPerTurn()
        {
            BrinaAshwoodBattleState state = CreateState();
            state.GainWoodcut(4);

            Assert.That(state.TryMarkFirstWoodcutGainForTurn(2), Is.True);
            Assert.That(state.TryMarkFirstWoodcutGainForTurn(2), Is.False);
            Assert.That(state.TryMarkFirstWoodcutGainForTurn(3), Is.True);
            Assert.That(state.LastWoodcutGainTurn, Is.EqualTo(3));
            Assert.That(state.WoodcutCount, Is.EqualTo(4));
        }

        [Test]
        public void SeparateStatesDoNotShareWoodcut()
        {
            BrinaAshwoodBattleState first = CreateState();
            BrinaAshwoodBattleState second = CreateState();

            first.GainWoodcut(6);

            Assert.That(first.WoodcutCount, Is.EqualTo(6));
            Assert.That(second.WoodcutCount, Is.Zero);
        }

        private static BrinaAshwoodBattleState CreateState(
            int slotIndex = 0)
        {
            return new BrinaAshwoodBattleState(
                new CharacterBattleState(
                    BrinaAshwoodRules.CharacterId,
                    slotIndex,
                    ElementType.Grass,
                    1000L,
                    1000d),
                10);
        }
    }
}
