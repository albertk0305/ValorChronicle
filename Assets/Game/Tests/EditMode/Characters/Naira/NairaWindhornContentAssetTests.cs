using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Characters.Naira;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Naira
{
    public sealed class NairaWindhornContentAssetTests
    {
        private const string DefinitionPath =
            "Assets/Data/Characters/naira_windhorn.asset";

        [Test]
        public void DefinitionUsesConfirmedIdentityStatsAndConfig()
        {
            CharacterDefinition definition =
                AssetDatabase.LoadAssetAtPath<CharacterDefinition>(
                    DefinitionPath);

            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.Id,
                Is.EqualTo(NairaWindhornRules.CharacterId));
            Assert.That(definition.Id,
                Does.Match("^character_[a-z0-9]+(?:_[a-z0-9]+)*$"));
            Assert.That(definition.DisplayNameKey, Is.EqualTo("naira"));
            Assert.That(definition.Element, Is.EqualTo(ElementType.Grass));
            Assert.That(definition.Level1Hp, Is.EqualTo(1500));
            Assert.That(definition.Level1Attack, Is.EqualTo(85));
            Assert.That(definition.Level100Hp, Is.EqualTo(6200));
            Assert.That(definition.Level100Attack, Is.EqualTo(425));
            Assert.That(definition.SkillIds, Is.EqualTo(new[]
            {
                NairaWindhornRules.Match3AbilityId,
                NairaWindhornRules.Match4AbilityId,
                NairaWindhornRules.Match5AbilityId,
                NairaWindhornRules.PassiveAbilityId,
                NairaWindhornRules.ActiveAbilityId
            }));
            Assert.That(definition.CombatConfig,
                Is.SameAs(NairaWindhornTestConfig.Canonical));
            Assert.That(definition.CombatConfig.TryValidate(out _),
                Is.True);
        }
    }
}

