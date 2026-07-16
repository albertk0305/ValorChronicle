using NUnit.Framework;
using ValorChronicle.Core.IDs;

namespace ValorChronicle.Tests.EditMode
{
    public sealed class ContentIdValidatorTests
    {
        [TestCase("character_marea_bluefang")]
        [TestCase("boss_kragmor")]
        [TestCase("skill_01")]
        public void TryValidate_AcceptsValidIds(string id)
        {
            bool isValid = ContentIdValidator.TryValidate(id, out string errorMessage);

            Assert.That(isValid, Is.True);
            Assert.That(errorMessage, Is.Null);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Character_Marea")]
        [TestCase("character marea")]
        [TestCase("character-marea")]
        [TestCase("character__marea")]
        [TestCase("_character_marea")]
        [TestCase("character_marea_")]
        [TestCase(" character_marea")]
        [TestCase("character_marea ")]
        public void TryValidate_RejectsInvalidIds(string id)
        {
            bool isValid = ContentIdValidator.TryValidate(id, out string errorMessage);

            Assert.That(isValid, Is.False);
            Assert.That(errorMessage, Is.Not.Empty);
        }
    }
}
