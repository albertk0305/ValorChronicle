using NUnit.Framework;
using ValorChronicle.Characters.Brina;

namespace ValorChronicle.Tests.EditMode.Characters.Brina
{
    public sealed class BrinaAshwoodRulesTests
    {
        [Test]
        public void StableIdsMatchContentNamingContract()
        {
            Assert.That(BrinaAshwoodRules.CharacterId,
                Is.EqualTo("character_brina_ashwood"));
            Assert.That(BrinaAshwoodRules.Match3SkillId,
                Is.EqualTo("skill_brina_ashwood_match3"));
            Assert.That(BrinaAshwoodRules.Match4SkillId,
                Is.EqualTo("skill_brina_ashwood_match4"));
            Assert.That(BrinaAshwoodRules.Match5SkillId,
                Is.EqualTo("skill_brina_ashwood_match5"));
            Assert.That(BrinaAshwoodRules.PassiveSkillId,
                Is.EqualTo("passive_brina_ten_chops"));
            Assert.That(BrinaAshwoodRules.ActiveAbilityId,
                Is.EqualTo("active_brina_swing_hard"));
            Assert.That(BrinaAshwoodRules.ActiveEffectId,
                Is.EqualTo("effect_brina_swing_hard"));
            Assert.That(BrinaAshwoodRules.WoodcutStatusId,
                Is.EqualTo("status_brina_woodcut"));
        }

        [Test]
        public void WoodcutStatusIdIsIndependentFromPassiveSkillId()
        {
            Assert.That(
                BrinaAshwoodRules.WoodcutStatusId,
                Is.Not.EqualTo(BrinaAshwoodRules.PassiveSkillId));
        }
    }
}
