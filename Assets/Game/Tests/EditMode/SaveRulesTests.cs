using NUnit.Framework;
using ValorChronicle.Save.DTO;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class SaveRulesTests
    {
        [Test]
        public void Constants_MatchConfirmedSaveRules()
        {
            Assert.That(SaveRules.CurrentSaveVersion, Is.EqualTo(SaveSchema.CurrentVersion));
            Assert.That(SaveRules.PartySlotCount, Is.EqualTo(5));
            Assert.That(SaveRules.CharacterMinLevel, Is.EqualTo(1));
            Assert.That(SaveRules.CharacterMaxLevel, Is.EqualTo(100));
            Assert.That(SaveRules.CharacterMinAwakening, Is.Zero);
            Assert.That(SaveRules.CharacterMaxAwakening, Is.EqualTo(6));
            Assert.That(SaveRules.RelicMinAwakening, Is.Zero);
            Assert.That(SaveRules.RelicMaxAwakening, Is.EqualTo(5));
            Assert.That(SaveRules.RelicSlotCountPerCharacter, Is.EqualTo(4));
            Assert.That(SaveRules.EquippedRelicSlotMinIndex, Is.Zero);
            Assert.That(SaveRules.EquippedRelicSlotMaxIndex, Is.EqualTo(3));
            Assert.That(SaveRules.UnequippedRelicSlotIndex, Is.EqualTo(-1));
            Assert.That(SaveRules.EmptyId, Is.EqualTo(string.Empty));
            Assert.That(SaveRules.DefaultPartyPresetId, Is.EqualTo("party_default"));
        }
    }
}
