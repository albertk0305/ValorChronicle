using NUnit.Framework;
using ValorChronicle.Battle.Combat.State;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Battle.Combat.Effects
{
    public sealed class EffectStateOwnershipTests
    {
        [Test]
        public void BattleStatesOwnIndependentEffectCollections()
        {
            var character = new CharacterBattleState(
                "hero",
                0,
                ElementType.Water,
                1000,
                100d);
            var party = new PartyBattleState(new[] { character });
            var boss = new BossBattleState(
                "boss",
                ElementType.Fire,
                5000,
                500d);

            Assert.That(character.Effects, Is.Not.Null);
            Assert.That(party.Effects, Is.Not.Null);
            Assert.That(boss.Effects, Is.Not.Null);
            Assert.That(character.Effects, Is.Not.SameAs(party.Effects));
            Assert.That(character.Effects, Is.Not.SameAs(boss.Effects));
            Assert.That(party.Effects, Is.Not.SameAs(boss.Effects));
        }
    }
}
