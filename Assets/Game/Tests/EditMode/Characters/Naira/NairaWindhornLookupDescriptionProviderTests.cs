using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using ValorChronicle.Characters.Naira;
using ValorChronicle.Characters.Presentation;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Tests.EditMode.Characters.Naira
{
    public sealed class NairaWindhornLookupDescriptionProviderTests
    {
        private NairaWindhornLookupDescriptionProvider provider;
        private CharacterDefinition definition;

        [SetUp]
        public void SetUp()
        {
            provider = new NairaWindhornLookupDescriptionProvider();
            definition = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(
                "Assets/Data/Characters/naira_windhorn.asset");
            Assert.That(definition, Is.Not.Null);
        }

        [Test]
        public void MatchDescriptionsUseResolvedShieldAndDamageRules()
        {
            IReadOnlyDictionary<string, object> match3Before = Arguments(
                provider.ResolveSkill(
                    definition,
                    awakening: 0,
                    CharacterLookupSkillType.Match3));
            IReadOnlyDictionary<string, object> match3After = Arguments(
                provider.ResolveSkill(
                    definition,
                    awakening: 4,
                    CharacterLookupSkillType.Match3));
            IReadOnlyDictionary<string, object> match4 = Arguments(
                provider.ResolveSkill(
                    definition,
                    awakening: 4,
                    CharacterLookupSkillType.Match4));

            Assert.That(match3Before["shield_rate"], Is.EqualTo("7"));
            Assert.That(match3After["shield_rate"], Is.EqualTo("8"));
            Assert.That(match3After["duration"], Is.EqualTo(3));
            Assert.That(match4["shield_rate"], Is.EqualTo("13"));
            Assert.That(match4["base_coefficient"], Is.EqualTo("120"));
            Assert.That(match4["charge_bonus"], Is.EqualTo("50"));
        }

        [Test]
        public void MatchFiveAddsAncientGuardianOnlyAtAwakeningSix()
        {
            CharacterLookupDescriptionDefinition before =
                provider.ResolveSkill(
                    definition,
                    awakening: 5,
                    CharacterLookupSkillType.Match5);
            CharacterLookupDescriptionDefinition after =
                provider.ResolveSkill(
                    definition,
                    awakening: 6,
                    CharacterLookupSkillType.Match5);

            Assert.That(before.Sections[0].Key,
                Is.EqualTo("naira.skill.match5.description"));
            Assert.That(after.Sections[0].Key,
                Is.EqualTo(
                    "naira.skill.match5.awakening6_description"));
            Assert.That(Arguments(after)["shield_rate"],
                Is.EqualTo("20"));
            Assert.That(Arguments(after)["base_coefficient"],
                Is.EqualTo("220"));
            Assert.That(Arguments(after)["charge_bonus"],
                Is.EqualTo("100"));
            Assert.That(Arguments(after)["guardian_reduction"],
                Is.EqualTo("15"));
        }

        [Test]
        public void PassiveAndActiveDescriptionsTrackAwakenings()
        {
            Assert.That(Arguments(provider.ResolveSkill(
                    definition,
                    awakening: 2,
                    CharacterLookupSkillType.Passive))["damage_increase"],
                Is.EqualTo("5"));
            Assert.That(Arguments(provider.ResolveSkill(
                    definition,
                    awakening: 3,
                    CharacterLookupSkillType.Passive))["damage_increase"],
                Is.EqualTo("6"));
            Assert.That(Arguments(provider.ResolveSkill(
                    definition,
                    awakening: 4,
                    CharacterLookupSkillType.Active))["grace_reduction"],
                Is.EqualTo("35"));

            IReadOnlyDictionary<string, object> awakenedActive = Arguments(
                provider.ResolveSkill(
                    definition,
                    awakening: 5,
                    CharacterLookupSkillType.Active));
            Assert.That(awakenedActive["shield_rate"],
                Is.EqualTo("35"));
            Assert.That(awakenedActive["duration"], Is.EqualTo(3));
            Assert.That(awakenedActive["grace_reduction"],
                Is.EqualTo("40"));
            Assert.That(awakenedActive["cooldown"], Is.EqualTo(12));
        }

        [Test]
        public void AwakeningDescriptionsUseBuildAndResolvedRuleChanges()
        {
            Assert.That(Arguments(provider.ResolveAwakening(
                    definition,
                    awakeningStage: 1))["multiplier_after"],
                Is.EqualTo("105"));
            Assert.That(Arguments(provider.ResolveAwakening(
                    definition,
                    awakeningStage: 2))["multiplier_after"],
                Is.EqualTo("105"));
            Assert.That(Arguments(provider.ResolveAwakening(
                    definition,
                    awakeningStage: 3))["damage_after"],
                Is.EqualTo("6"));
            Assert.That(Arguments(provider.ResolveAwakening(
                    definition,
                    awakeningStage: 4))["match4_after"],
                Is.EqualTo("13"));
            Assert.That(Arguments(provider.ResolveAwakening(
                    definition,
                    awakeningStage: 5))["reduction_after"],
                Is.EqualTo("40"));

            IReadOnlyDictionary<string, object> awakening6 = Arguments(
                provider.ResolveAwakening(
                    definition,
                    awakeningStage: 6));
            Assert.That(awakening6["charge"], Is.EqualTo("100"));
            Assert.That(awakening6["damage_reduction"],
                Is.EqualTo("15"));
        }

        private static IReadOnlyDictionary<string, object> Arguments(
            CharacterLookupDescriptionDefinition description)
        {
            return description.Sections[0].Arguments;
        }
    }
}
